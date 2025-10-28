using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CoreLambdaDetector = RoslynAnalyzer.Core.Analysis.Patterns.Lambda.LambdaDetector;
using LambdaContext = RoslynAnalyzer.Core.Analysis.Patterns.Lambda.LambdaContext;

namespace ThrowsAnalyzer.Analysis
{
    /// <summary>
    /// Detects and analyzes lambda expressions with exception-specific functionality.
    /// Delegates generic lambda detection to RoslynAnalyzer.Core.
    /// </summary>
    public static class LambdaExceptionDetector
    {
        // Generic methods delegated to RoslynAnalyzer.Core

        /// <summary>
        /// Gets all lambda expressions in a syntax node.
        /// </summary>
        public static IEnumerable<LambdaExpressionSyntax> GetLambdaExpressions(SyntaxNode node)
            => CoreLambdaDetector.GetLambdaExpressions(node);

        /// <summary>
        /// Gets all simple lambda expressions (single parameter).
        /// </summary>
        public static IEnumerable<SimpleLambdaExpressionSyntax> GetSimpleLambdas(SyntaxNode node)
            => CoreLambdaDetector.GetSimpleLambdas(node);

        /// <summary>
        /// Gets all parenthesized lambda expressions (multiple or typed parameters).
        /// </summary>
        public static IEnumerable<ParenthesizedLambdaExpressionSyntax> GetParenthesizedLambdas(SyntaxNode node)
            => CoreLambdaDetector.GetParenthesizedLambdas(node);

        /// <summary>
        /// Gets the body of a lambda expression.
        /// </summary>
        public static SyntaxNode GetLambdaBody(LambdaExpressionSyntax lambda)
            => CoreLambdaDetector.GetLambdaBody(lambda);

        /// <summary>
        /// Checks if a lambda has a block body.
        /// </summary>
        public static bool HasBlockBody(LambdaExpressionSyntax lambda)
            => CoreLambdaDetector.HasBlockBody(lambda);

        /// <summary>
        /// Checks if a lambda has an expression body.
        /// </summary>
        public static bool HasExpressionBody(LambdaExpressionSyntax lambda)
            => CoreLambdaDetector.HasExpressionBody(lambda);

        /// <summary>
        /// Gets all throw statements in a lambda.
        /// </summary>
        public static IEnumerable<ThrowStatementSyntax> GetThrowStatements(LambdaExpressionSyntax lambda)
        {
            var body = GetLambdaBody(lambda);
            if (body == null)
                return Enumerable.Empty<ThrowStatementSyntax>();

            return body.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Where(t => t.Expression != null); // Exclude bare rethrows
        }

        /// <summary>
        /// Gets all throw expressions in a lambda.
        /// </summary>
        public static IEnumerable<ThrowExpressionSyntax> GetThrowExpressions(LambdaExpressionSyntax lambda)
        {
            var body = GetLambdaBody(lambda);
            if (body == null)
                return Enumerable.Empty<ThrowExpressionSyntax>();

            // Check if body itself is a throw expression (expression-bodied lambda)
            if (body is ThrowExpressionSyntax throwExpr)
                return new[] { throwExpr };

            // Otherwise, search descendants
            return body.DescendantNodes()
                .OfType<ThrowExpressionSyntax>();
        }

        // Exception-specific methods (throws and throw expressions)

        /// <summary>
        /// Checks if a lambda is async.
        /// </summary>
        public static bool IsAsyncLambda(LambdaExpressionSyntax lambda)
            => CoreLambdaDetector.IsAsyncLambda(lambda);

        /// <summary>
        /// Determines if a lambda is used as an event handler.
        /// </summary>
        public static bool IsEventHandlerLambda(LambdaExpressionSyntax lambda, SemanticModel semanticModel)
            => CoreLambdaDetector.IsEventHandlerLambda(lambda, semanticModel);

        /// <summary>
        /// Determines the context in which a lambda is used.
        /// </summary>
        public static LambdaContext GetLambdaContext(LambdaExpressionSyntax lambda, SemanticModel semanticModel)
            => CoreLambdaDetector.GetLambdaContext(lambda, semanticModel);

        /// <summary>
        /// Gets all try-catch statements in a lambda.
        /// </summary>
        public static IEnumerable<TryStatementSyntax> GetTryCatchStatements(LambdaExpressionSyntax lambda)
            => CoreLambdaDetector.GetTryCatchStatements(lambda);

        /// <summary>
        /// Checks if a lambda contains any try-catch blocks.
        /// </summary>
        public static bool HasTryCatch(LambdaExpressionSyntax lambda)
            => CoreLambdaDetector.HasTryCatch(lambda);

        // Exception-specific method

        /// <summary>
        /// Gets comprehensive information about a lambda expression with exception details.
        /// </summary>
        public static LambdaExceptionInfo GetLambdaExceptionInfo(
            LambdaExpressionSyntax lambda,
            SemanticModel semanticModel)
        {
            var coreInfo = CoreLambdaDetector.GetLambdaInfo(lambda, semanticModel);

            return new LambdaExceptionInfo
            {
                Lambda = coreInfo.Lambda,
                IsAsync = coreInfo.IsAsync,
                HasBlockBody = coreInfo.HasBlockBody,
                HasExpressionBody = coreInfo.HasExpressionBody,
                ThrowCount = GetThrowStatements(lambda).Count() + GetThrowExpressions(lambda).Count(),
                HasTryCatch = coreInfo.HasTryCatch,
                Context = coreInfo.Context,
                IsEventHandler = coreInfo.IsEventHandler
            };
        }
    }

    /// <summary>
    /// Contains information about a lambda expression.
    /// </summary>
    public class LambdaExceptionInfo
    {
        /// <summary>
        /// The lambda expression.
        /// </summary>
        public LambdaExpressionSyntax Lambda { get; set; }

        /// <summary>
        /// True if the lambda is async.
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// True if the lambda has a block body.
        /// </summary>
        public bool HasBlockBody { get; set; }

        /// <summary>
        /// True if the lambda has an expression body.
        /// </summary>
        public bool HasExpressionBody { get; set; }

        /// <summary>
        /// Number of throw statements/expressions.
        /// </summary>
        public int ThrowCount { get; set; }

        /// <summary>
        /// True if the lambda contains try-catch.
        /// </summary>
        public bool HasTryCatch { get; set; }

        /// <summary>
        /// The context in which the lambda is used.
        /// </summary>
        public LambdaContext Context { get; set; }

        /// <summary>
        /// True if the lambda is an event handler.
        /// </summary>
        public bool IsEventHandler { get; set; }
    }
}
