using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynAnalyzer.Core.Analysis.CallGraph;
using ThrowsAnalyzer.Analysis;

namespace ThrowsAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer that detects method calls to throwing methods without handling exceptions.
    /// Reports THROWS017: "Method calls '{0}' which may throw {1}, but does not handle it"
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnhandledMethodCallAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS017";

        private static readonly LocalizableString Title = "Method calls throwing method without handling";
        private static readonly LocalizableString MessageFormat = "Method calls '{0}' which may throw {1}, but does not handle it";
        private static readonly LocalizableString Description = "This method calls another method that may throw exceptions, but does not handle them. Consider adding exception handling or documenting that this method propagates exceptions.";
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

            context.RegisterCompilationStartAction(compilationContext =>
            {
                // Create tracker once per compilation
                var tracker = new ExceptionPropagationTracker(
                    compilationContext.Compilation,
                    compilationContext.CancellationToken);

                compilationContext.RegisterSyntaxNodeAction(
                    nodeContext => AnalyzeInvocation(nodeContext, tracker),
                    SyntaxKind.InvocationExpression);

                compilationContext.RegisterSyntaxNodeAction(
                    nodeContext => AnalyzeObjectCreation(nodeContext, tracker),
                    SyntaxKind.ObjectCreationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, ExceptionPropagationTracker tracker)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // Get the invoked method symbol
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
            if (symbolInfo.Symbol is not IMethodSymbol calleeMethod)
                return;

            // Skip if this invocation is already inside a try block that might handle the exception
            if (IsInsideTryBlock(invocation))
                return;

            // Analyze the callee method for exceptions
            var flowInfoTask = tracker.AnalyzeMethodAsync(calleeMethod);

            // Note: We need to wait for the async operation
            // In a real analyzer, you'd want to use RegisterOperationAction or a different approach
            // For now, we'll use Task.Run with Wait which is not ideal but works
            var flowInfo = Task.Run(async () => await flowInfoTask).GetAwaiter().GetResult();

            // Check if the callee has unhandled exceptions
            if (!flowInfo.HasUnhandledExceptions)
                return;

            // Get the containing method
            var containingMethod = GetContainingMethod(invocation);
            if (containingMethod == null)
                return;

            // Report diagnostic for each unhandled exception type
            var exceptionTypes = flowInfo.PropagatedExceptions
                .Select(ex => ex.ExceptionType.Name)
                .Distinct()
                .ToList();

            if (exceptionTypes.Count == 0)
                return;

            var exceptionList = exceptionTypes.Count == 1
                ? exceptionTypes[0]
                : string.Join(", ", exceptionTypes.Take(exceptionTypes.Count - 1)) +
                  " or " + exceptionTypes.Last();

            var methodName = GetMethodDisplayName(calleeMethod);

            var diagnostic = Diagnostic.Create(
                Rule,
                invocation.GetLocation(),
                methodName,
                exceptionList);

            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context, ExceptionPropagationTracker tracker)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;

            // Get the constructor symbol
            var symbolInfo = context.SemanticModel.GetSymbolInfo(creation, context.CancellationToken);
            if (symbolInfo.Symbol is not IMethodSymbol constructorMethod)
                return;

            // Skip if this creation is already inside a try block
            if (IsInsideTryBlock(creation))
                return;

            // Analyze the constructor for exceptions
            var flowInfoTask = tracker.AnalyzeMethodAsync(constructorMethod);
            var flowInfo = Task.Run(async () => await flowInfoTask).GetAwaiter().GetResult();

            // Check if the constructor has unhandled exceptions
            if (!flowInfo.HasUnhandledExceptions)
                return;

            // Get the containing method
            var containingMethod = GetContainingMethod(creation);
            if (containingMethod == null)
                return;

            // Report diagnostic
            var exceptionTypes = flowInfo.PropagatedExceptions
                .Select(ex => ex.ExceptionType.Name)
                .Distinct()
                .ToList();

            if (exceptionTypes.Count == 0)
                return;

            var exceptionList = exceptionTypes.Count == 1
                ? exceptionTypes[0]
                : string.Join(", ", exceptionTypes.Take(exceptionTypes.Count - 1)) +
                  " or " + exceptionTypes.Last();

            var typeName = constructorMethod.ContainingType?.Name ?? "unknown";

            var diagnostic = Diagnostic.Create(
                Rule,
                creation.GetLocation(),
                $"new {typeName}()",
                exceptionList);

            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsInsideTryBlock(SyntaxNode node)
        {
            var tryStatement = node.Ancestors().OfType<TryStatementSyntax>().FirstOrDefault();
            if (tryStatement == null)
                return false;

            // Check if the node is inside the try block (not in catch or finally)
            return tryStatement.Block.Span.Contains(node.Span);
        }

        private static SyntaxNode GetContainingMethod(SyntaxNode node)
        {
            return node.Ancestors().FirstOrDefault(n =>
                n is MethodDeclarationSyntax ||
                n is ConstructorDeclarationSyntax ||
                n is LocalFunctionStatementSyntax);
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
