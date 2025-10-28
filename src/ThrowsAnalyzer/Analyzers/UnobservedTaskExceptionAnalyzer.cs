using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ThrowsAnalyzer.Analysis;
using CoreAsyncDetector = RoslynAnalyzer.Core.Analysis.Patterns.Async.AsyncMethodDetector;

namespace ThrowsAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer that detects Task-returning method calls that are not awaited and may throw unobserved exceptions.
    /// Reports THROWS022: "Task-returning call to '{0}' is not awaited - exceptions may be unobserved"
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnobservedTaskExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS022";

        private static readonly LocalizableString Title = "Unawaited Task may have unobserved exception";
        private static readonly LocalizableString MessageFormat = "Task-returning call to '{0}' is not awaited - exceptions may be unobserved";
        private static readonly LocalizableString Description = "Task-returning methods that are not awaited may throw exceptions that go unobserved. Unobserved exceptions can cause application crashes or unexpected behavior. Consider awaiting the Task, or explicitly handle exceptions using .ContinueWith() or try-catch around .Wait(). If the fire-and-forget behavior is intentional, consider using Task.Run with explicit exception handling.";
        private const string Category = "Exception";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethod,
                SyntaxKind.MethodDeclaration,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.LocalFunctionStatement);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            // Get method symbol
            IMethodSymbol methodSymbol = null;

            if (context.Node is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax methodDecl)
            {
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken);
            }
            else if (context.Node is Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax ctorDecl)
            {
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(ctorDecl, context.CancellationToken);
            }
            else if (context.Node is Microsoft.CodeAnalysis.CSharp.Syntax.LocalFunctionStatementSyntax localFunc)
            {
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(localFunc, context.CancellationToken);
            }

            if (methodSymbol == null)
                return;

            // Get method body
            var body = CoreAsyncDetector.GetMethodBody(context.Node);
            if (body == null)
                return;

            // Find unawaited task invocations
            var unawaitedInvocations = CoreAsyncDetector.GetUnawaitedTaskInvocations(
                body,
                context.SemanticModel);

            foreach (var invocation in unawaitedInvocations)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
                if (symbolInfo.Symbol is IMethodSymbol calledMethod)
                {
                    var methodName = GetMethodDisplayName(calledMethod);

                    var diagnostic = Diagnostic.Create(
                        Rule,
                        invocation.GetLocation(),
                        methodName);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static string GetMethodDisplayName(IMethodSymbol method)
        {
            if (method.ContainingType != null)
            {
                return $"{method.ContainingType.Name}.{method.Name}";
            }
            return method.Name;
        }
    }
}
