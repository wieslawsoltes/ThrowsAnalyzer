using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynAnalyzer.Core.Analysis.CallGraph;
using ThrowsAnalyzer.Analysis;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Analyzer that detects uncaught exceptions in event handler lambda expressions.
    ///
    /// Event handlers are a special case where uncaught exceptions can crash the application.
    /// Unlike regular methods, exceptions thrown from event handlers cannot be caught by the
    /// event raiser. This is particularly dangerous in UI applications where an uncaught
    /// exception in a button click handler can crash the entire application.
    ///
    /// This analyzer reports errors for uncaught exceptions in event handler lambdas.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EventHandlerLambdaExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS026";

        private static readonly LocalizableString Title =
            "Event handler lambda throws exception without catching it";

        private static readonly LocalizableString MessageFormat =
            "Event handler lambda throws {0} which is not caught - exception may crash application";

        private static readonly LocalizableString Description =
            "Event handler lambdas that throw exceptions without catching them can crash the application. " +
            "Exceptions thrown from event handlers cannot be caught by the event raiser. " +
            "Always handle exceptions within event handler lambdas to prevent application crashes.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Exception",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.SimpleLambdaExpression);
            context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.ParenthesizedLambdaExpression);
        }

        private void AnalyzeLambda(SyntaxNodeAnalysisContext context)
        {
            var lambda = (LambdaExpressionSyntax)context.Node;

            var analyzer = new LambdaExceptionAnalyzer(context.SemanticModel, context.CancellationToken);
            var result = analyzer.Analyze(lambda);

            // Skip if no throws
            if (result.Throws.Count == 0)
                return;

            // Only analyze event handlers
            if (!result.LambdaInfo.IsEventHandler)
                return;

            // Report uncaught exceptions
            foreach (var throwInfo in result.UncaughtThrows)
            {
                var exceptionTypeName = throwInfo.ExceptionType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

                var diagnostic = Diagnostic.Create(
                    Rule,
                    throwInfo.Location,
                    exceptionTypeName);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
