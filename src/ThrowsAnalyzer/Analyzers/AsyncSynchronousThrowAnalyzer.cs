using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ThrowsAnalyzer.Analysis;

namespace ThrowsAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer that detects async methods that throw exceptions synchronously before the first await.
    /// Reports THROWS020: "Async method '{0}' throws {1} synchronously before first await"
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsyncSynchronousThrowAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS020";

        private static readonly LocalizableString Title = "Async method throws synchronously before first await";
        private static readonly LocalizableString MessageFormat = "Async method '{0}' throws {1} synchronously before first await";
        private static readonly LocalizableString Description = "Async methods should not throw exceptions synchronously before the first await. This causes the exception to be thrown directly to the caller instead of being wrapped in the returned Task, which can lead to inconsistent exception handling. Consider validating parameters and throwing before making the method async, or ensure all code is after the first await.";
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
                SyntaxKind.MethodDeclaration);

            context.RegisterSyntaxNodeAction(AnalyzeLocalFunction,
                SyntaxKind.LocalFunctionStatement);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDecl = (Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax)context.Node;

            // Get method symbol
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken);
            if (methodSymbol == null)
                return;

            // Only analyze async methods
            if (!AsyncMethodDetector.IsAsyncMethod(methodSymbol))
                return;

            // Don't analyze async void (covered by THROWS021)
            if (AsyncMethodDetector.IsAsyncVoid(methodSymbol, context.Compilation))
                return;

            AnalyzeAsyncMethod(context, methodSymbol, methodDecl);
        }

        private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
        {
            var localFunc = (Microsoft.CodeAnalysis.CSharp.Syntax.LocalFunctionStatementSyntax)context.Node;

            // Get method symbol
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(localFunc, context.CancellationToken);
            if (methodSymbol == null)
                return;

            // Only analyze async local functions
            if (!AsyncMethodDetector.IsAsyncMethod(methodSymbol))
                return;

            // Don't analyze async void
            if (AsyncMethodDetector.IsAsyncVoid(methodSymbol, context.Compilation))
                return;

            AnalyzeAsyncMethod(context, methodSymbol, localFunc);
        }

        private static void AnalyzeAsyncMethod(
            SyntaxNodeAnalysisContext context,
            IMethodSymbol methodSymbol,
            SyntaxNode methodNode)
        {
            var analyzer = new AsyncExceptionAnalyzer(context.SemanticModel, context.CancellationToken);

            // Analyze the method
            var analysisTask = analyzer.AnalyzeAsync(methodSymbol, methodNode);
            var info = Task.Run(async () => await analysisTask).GetAwaiter().GetResult();

            // Check for throws before first await
            if (info.ThrowsBeforeAwait.Count == 0)
                return;

            // Report diagnostic for each throw before await
            foreach (var throwInfo in info.ThrowsBeforeAwait)
            {
                var methodName = GetMethodDisplayName(methodSymbol);
                var exceptionName = throwInfo.ExceptionType.Name;

                var diagnostic = Diagnostic.Create(
                    Rule,
                    throwInfo.Location,
                    methodName,
                    exceptionName);

                context.ReportDiagnostic(diagnostic);
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
