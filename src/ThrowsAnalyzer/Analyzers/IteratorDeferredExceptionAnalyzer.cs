using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ThrowsAnalyzer.Analysis;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Analyzer that detects exceptions thrown in iterator methods that will be deferred until enumeration.
    ///
    /// Iterator methods (using yield return/yield break) have special exception timing:
    /// - Exceptions before first yield are thrown immediately during iterator creation
    /// - Exceptions after first yield are deferred until MoveNext() is called during enumeration
    ///
    /// This can lead to confusing behavior where exceptions are thrown far from where the
    /// iterator was created, making debugging difficult.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IteratorDeferredExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS023";

        private static readonly LocalizableString Title =
            "Exception in iterator will be deferred until enumeration";

        private static readonly LocalizableString MessageFormat =
            "Iterator method '{0}' throws {1} after first yield - exception will be deferred until enumeration";

        private static readonly LocalizableString Description =
            "Exceptions thrown after the first yield in an iterator method are deferred until MoveNext() is called during enumeration. " +
            "This can make debugging difficult as exceptions occur far from where the iterator was created. " +
            "Consider validating parameters before the first yield, or wrapping the iterator in a non-iterator method that validates immediately.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Exception",
            DiagnosticSeverity.Warning,
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

            // Report throws that occur after the first yield
            foreach (var throwInfo in info.ThrowsInIterator)
            {
                // Skip throws before first yield (they execute immediately)
                if (throwInfo.IsBeforeFirstYield)
                    continue;

                // Report deferred exception
                var exceptionTypeName = throwInfo.ExceptionType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

                var diagnostic = Diagnostic.Create(
                    Rule,
                    throwInfo.Location,
                    method.Name,
                    exceptionTypeName);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
