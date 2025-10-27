using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ThrowsAnalyzer.Analysis;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Analyzer that detects try-finally blocks in iterator methods which have special execution timing.
    ///
    /// In normal methods, finally blocks execute when the try block exits.
    /// In iterator methods with yield in the try block:
    /// - The finally block does NOT execute when try block exits
    /// - The finally block executes when the iterator is disposed (via Dispose() or when enumeration completes)
    ///
    /// This can lead to resource leaks if the iterator is not fully enumerated or properly disposed.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IteratorTryFinallyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS024";

        private static readonly LocalizableString Title =
            "Try-finally in iterator has special disposal timing";

        private static readonly LocalizableString MessageFormat =
            "Finally block in iterator method '{0}' will execute when iterator is disposed, not when try block exits";

        private static readonly LocalizableString Description =
            "When a try-finally block contains yield statements, the finally block does not execute when the try block exits. " +
            "Instead, the finally block is deferred and executes when the iterator is disposed (either explicitly via Dispose() or when enumeration completes). " +
            "This can lead to resource leaks if the iterator is not fully enumerated. Consider using a using statement or ensuring disposal.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Exception",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDecl = (MethodDeclarationSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl);

            if (methodSymbol == null)
                return;

            AnalyzeMethod(context, methodSymbol, methodDecl);
        }

        private void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
        {
            var localFunc = (LocalFunctionStatementSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(localFunc);

            if (methodSymbol == null)
                return;

            AnalyzeMethod(context, methodSymbol, localFunc);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context, IMethodSymbol method, SyntaxNode methodNode)
        {
            // Only analyze iterator methods
            if (!IteratorMethodDetector.IsIteratorMethod(method, methodNode))
                return;

            // Don't analyze if it doesn't return IEnumerable/IEnumerator
            if (!IteratorMethodDetector.ReturnsEnumerable(method, context.SemanticModel.Compilation))
                return;

            var analyzer = new IteratorExceptionAnalyzer(context.SemanticModel, context.CancellationToken);
            var info = analyzer.Analyze(method, methodNode);

            // Report try-finally blocks that contain yield
            foreach (var tryFinallyInfo in info.TryFinallyWithYield)
            {
                // The try block contains yield, so finally has deferred execution
                if (!tryFinallyInfo.HasYieldInTryBlock)
                    continue;

                // Report at the finally clause location
                var location = tryFinallyInfo.FinallyBlock?.GetLocation() ?? tryFinallyInfo.Location;

                var diagnostic = Diagnostic.Create(
                    Rule,
                    location,
                    method.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
