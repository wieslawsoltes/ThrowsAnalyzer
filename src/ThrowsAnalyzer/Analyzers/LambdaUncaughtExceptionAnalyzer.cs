using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynAnalyzer.Core.Analysis.CallGraph;
using ThrowsAnalyzer.Analysis;
using LambdaContext = RoslynAnalyzer.Core.Analysis.Patterns.Lambda.LambdaContext;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Analyzer that detects uncaught exceptions in lambda expressions.
    ///
    /// Lambda expressions that throw exceptions without catching them can cause issues:
    /// - In LINQ queries: exceptions propagate during query evaluation
    /// - In event handlers: uncaught exceptions may crash the application
    /// - In callbacks: exceptions propagate to the caller, which may not expect them
    /// - In Task.Run: exceptions are captured in the Task and need to be observed
    ///
    /// This analyzer warns about uncaught exceptions in non-event-handler lambdas.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LambdaUncaughtExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "THROWS025";

        private static readonly LocalizableString Title =
            "Lambda throws exception without catching it";

        private static readonly LocalizableString MessageFormat =
            "Lambda expression throws {0} which is not caught within the lambda - exception will propagate to {1}";

        private static readonly LocalizableString Description =
            "Lambda expressions that throw exceptions without catching them can cause unexpected behavior. " +
            "In LINQ queries, exceptions propagate during evaluation. In callbacks, exceptions propagate to the invoker. " +
            "Consider catching exceptions within the lambda or documenting the exception behavior.";

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

            // Skip event handlers (covered by THROWS026)
            if (result.LambdaInfo.IsEventHandler)
                return;

            // Report uncaught exceptions
            foreach (var throwInfo in result.UncaughtThrows)
            {
                var exceptionTypeName = throwInfo.ExceptionType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                var contextDescription = GetContextDescription(result.LambdaInfo.Context);

                var diagnostic = Diagnostic.Create(
                    Rule,
                    throwInfo.Location,
                    exceptionTypeName,
                    contextDescription);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private string GetContextDescription(LambdaContext context)
        {
            switch (context)
            {
                case LambdaContext.LinqQuery:
                    return "LINQ query evaluator";

                case LambdaContext.TaskRun:
                    return "Task (ensure Task is observed)";

                case LambdaContext.Callback:
                    return "callback invoker";

                default:
                    return "lambda invoker";
            }
        }
    }
}
