using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ThrowsAnalyzer.Analysis;

namespace ThrowsAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer that detects exception propagation across 3 or more method call levels.
    /// Reports THROWS018: "Exception {0} propagates through {1} method levels: {2}"
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DeepExceptionPropagationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS018";

        private static readonly LocalizableString Title = "Exception propagates across multiple method levels";
        private static readonly LocalizableString MessageFormat = "Exception {0} propagates through {1} method levels: {2}";
        private static readonly LocalizableString Description = "This exception propagates through multiple method call levels, which may make it harder to trace and handle appropriately. Consider handling the exception closer to its source or documenting the propagation clearly.";
        private const string Category = "Exception";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private const int MinPropagationDepth = 3; // Report if exception propagates through 3+ levels

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
                    nodeContext => AnalyzeMethod(nodeContext, tracker),
                    SyntaxKind.MethodDeclaration,
                    SyntaxKind.ConstructorDeclaration,
                    SyntaxKind.LocalFunctionStatement);
            });
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, ExceptionPropagationTracker tracker)
        {
            // Get the method symbol
            IMethodSymbol methodSymbol = null;

            if (context.Node is MethodDeclarationSyntax methodDecl)
            {
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken);
            }
            else if (context.Node is ConstructorDeclarationSyntax ctorDecl)
            {
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(ctorDecl, context.CancellationToken);
            }
            else if (context.Node is LocalFunctionStatementSyntax localFunc)
            {
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(localFunc, context.CancellationToken);
            }

            if (methodSymbol == null)
                return;

            // Analyze exception flow for this method
            var flowInfoTask = tracker.AnalyzeMethodAsync(methodSymbol);
            var flowInfo = Task.Run(async () => await flowInfoTask).GetAwaiter().GetResult();

            // Find propagation chains with depth >= MinPropagationDepth
            var chainsTask = tracker.FindPropagationChainsAsync(methodSymbol, MinPropagationDepth);
            var chains = Task.Run(async () => await chainsTask).GetAwaiter().GetResult();

            if (chains.Count == 0)
                return;

            // Report diagnostic for each deep propagation chain
            foreach (var chain in chains)
            {
                // Find the location where this exception originates (first throw in the chain)
                var originException = flowInfo.PropagatedExceptions
                    .FirstOrDefault(ex =>
                        SymbolEqualityComparer.Default.Equals(ex.ExceptionType, chain.ExceptionType) &&
                        ex.PropagationDepth >= MinPropagationDepth);

                if (originException == null)
                    continue;

                var exceptionName = chain.ExceptionType.Name;
                var depth = chain.Depth;
                var callChainStr = ExceptionPropagationTracker.FormatCallChain(originException);

                var diagnostic = Diagnostic.Create(
                    Rule,
                    originException.Location,
                    exceptionName,
                    depth,
                    callChainStr);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
