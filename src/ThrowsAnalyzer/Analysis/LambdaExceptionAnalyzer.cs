using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ThrowsAnalyzer.TypeAnalysis;

namespace ThrowsAnalyzer.Analysis
{
    /// <summary>
    /// Analyzes exception handling patterns in lambda expressions.
    /// </summary>
    public class LambdaExceptionAnalyzer
    {
        private readonly SemanticModel _semanticModel;
        private readonly CancellationToken _cancellationToken;

        public LambdaExceptionAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken = default)
        {
            _semanticModel = semanticModel;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Analyzes a lambda expression for exception handling issues.
        /// </summary>
        public LambdaAnalysisResult Analyze(LambdaExpressionSyntax lambda)
        {
            var result = new LambdaAnalysisResult
            {
                Lambda = lambda,
                LambdaInfo = LambdaExceptionDetector.GetLambdaExceptionInfo(lambda, _semanticModel)
            };

            // Analyze throws in lambda
            AnalyzeThrows(result);

            return result;
        }

        private void AnalyzeThrows(LambdaAnalysisResult result)
        {
            var lambda = result.Lambda;

            // Get all throw statements
            var throwStatements = LambdaExceptionDetector.GetThrowStatements(lambda);
            foreach (var throwStmt in throwStatements)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(
                    throwStmt,
                    _semanticModel);

                if (exceptionType != null)
                {
                    var isCaught = IsExceptionCaught(throwStmt, exceptionType, lambda);

                    result.Throws.Add(new LambdaThrowInfo
                    {
                        ThrowNode = throwStmt,
                        Location = throwStmt.GetLocation(),
                        ExceptionType = exceptionType,
                        IsCaught = isCaught
                    });
                }
            }

            // Get all throw expressions
            var throwExpressions = LambdaExceptionDetector.GetThrowExpressions(lambda);
            foreach (var throwExpr in throwExpressions)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(
                    throwExpr,
                    _semanticModel);

                if (exceptionType != null)
                {
                    var isCaught = IsExceptionCaught(throwExpr, exceptionType, lambda);

                    result.Throws.Add(new LambdaThrowInfo
                    {
                        ThrowNode = throwExpr,
                        Location = throwExpr.GetLocation(),
                        ExceptionType = exceptionType,
                        IsCaught = isCaught
                    });
                }
            }
        }

        private bool IsExceptionCaught(SyntaxNode throwNode, ITypeSymbol exceptionType, LambdaExpressionSyntax lambda)
        {
            // Walk up the syntax tree to find enclosing try-catch blocks within the lambda
            var current = throwNode.Parent;
            var lambdaBody = LambdaExceptionDetector.GetLambdaBody(lambda);

            while (current != null && current != lambdaBody.Parent)
            {
                if (current is TryStatementSyntax tryStatement)
                {
                    foreach (var catchClause in tryStatement.Catches)
                    {
                        var caughtType = ExceptionTypeAnalyzer.GetCaughtExceptionType(
                            catchClause,
                            _semanticModel);

                        if (caughtType == null)
                        {
                            // Catch-all (catch without type)
                            return true;
                        }

                        // Check if thrown exception is assignable to caught exception
                        if (ExceptionTypeAnalyzer.IsAssignableTo(exceptionType, caughtType, _semanticModel.Compilation))
                        {
                            // Check if the catch block doesn't rethrow
                            if (!HasRethrow(catchClause))
                            {
                                return true;
                            }
                        }
                    }
                }

                current = current.Parent;
            }

            return false;
        }

        private bool HasRethrow(CatchClauseSyntax catchClause)
        {
            if (catchClause.Block == null)
                return false;

            // Look for bare throw statements (rethrow)
            return catchClause.Block.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Any(t => t.Expression == null);
        }

        /// <summary>
        /// Gets a description of the lambda exception issue.
        /// </summary>
        public static string GetIssueDescription(LambdaThrowInfo throwInfo, LambdaContext context)
        {
            if (throwInfo.IsCaught)
            {
                return "Exception is handled within lambda";
            }

            switch (context)
            {
                case LambdaContext.EventHandler:
                    return "Exception will escape event handler and may crash application";

                case LambdaContext.LinqQuery:
                    return "Exception will propagate through LINQ query evaluation";

                case LambdaContext.TaskRun:
                    return "Exception will be captured in Task - ensure Task is awaited or observed";

                case LambdaContext.Callback:
                    return "Exception will propagate to callback invoker";

                default:
                    return "Exception will propagate to lambda invoker";
            }
        }
    }

    /// <summary>
    /// Contains the results of lambda exception analysis.
    /// </summary>
    public class LambdaAnalysisResult
    {
        public LambdaAnalysisResult()
        {
            Throws = new List<LambdaThrowInfo>();
        }

        /// <summary>
        /// The lambda being analyzed.
        /// </summary>
        public LambdaExpressionSyntax Lambda { get; set; }

        /// <summary>
        /// Lambda information.
        /// </summary>
        public LambdaExceptionInfo LambdaInfo { get; set; }

        /// <summary>
        /// Throws in the lambda.
        /// </summary>
        public List<LambdaThrowInfo> Throws { get; set; }

        /// <summary>
        /// Gets uncaught throws.
        /// </summary>
        public IEnumerable<LambdaThrowInfo> UncaughtThrows => Throws.Where(t => !t.IsCaught);
    }

    /// <summary>
    /// Information about a throw in a lambda.
    /// </summary>
    public class LambdaThrowInfo
    {
        /// <summary>
        /// The throw statement or expression.
        /// </summary>
        public SyntaxNode ThrowNode { get; set; }

        /// <summary>
        /// Location of the throw.
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// The exception type being thrown.
        /// </summary>
        public ITypeSymbol ExceptionType { get; set; }

        /// <summary>
        /// True if the exception is caught within the lambda.
        /// </summary>
        public bool IsCaught { get; set; }
    }
}
