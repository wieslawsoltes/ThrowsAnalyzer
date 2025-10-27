using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ThrowsAnalyzer.TypeAnalysis;

namespace ThrowsAnalyzer.Analysis
{
    /// <summary>
    /// Analyzes exception handling patterns in iterator methods.
    /// </summary>
    public class IteratorExceptionAnalyzer
    {
        private readonly SemanticModel _semanticModel;
        private readonly CancellationToken _cancellationToken;

        public IteratorExceptionAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken = default)
        {
            _semanticModel = semanticModel;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Analyzes an iterator method for exception handling issues.
        /// </summary>
        public IteratorExceptionInfo Analyze(IMethodSymbol method, SyntaxNode methodNode)
        {
            var info = new IteratorExceptionInfo
            {
                Method = method,
                IteratorInfo = IteratorMethodDetector.GetIteratorMethodInfo(
                    method,
                    methodNode,
                    _semanticModel.Compilation)
            };

            var body = IteratorMethodDetector.GetMethodBody(methodNode);
            if (body == null)
                return info;

            // Analyze throws in iterator
            AnalyzeThrows(info, body);

            // Analyze try-finally in iterator
            AnalyzeTryFinally(info, body);

            return info;
        }

        private void AnalyzeThrows(IteratorExceptionInfo info, SyntaxNode body)
        {
            var throwStatements = IteratorMethodDetector.GetThrowStatements(body);

            foreach (var throwStmt in throwStatements)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(
                    throwStmt,
                    _semanticModel);

                if (exceptionType != null)
                {
                    var isBeforeFirstYield = IteratorMethodDetector.IsThrowBeforeFirstYield(
                        throwStmt,
                        body);

                    info.ThrowsInIterator.Add(new ThrowInIteratorInfo
                    {
                        ThrowStatement = throwStmt,
                        Location = throwStmt.GetLocation(),
                        ExceptionType = exceptionType,
                        IsBeforeFirstYield = isBeforeFirstYield
                    });
                }
            }

            // Also check throw expressions
            var throwExpressions = body.DescendantNodes()
                .OfType<ThrowExpressionSyntax>();

            foreach (var throwExpr in throwExpressions)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(
                    throwExpr,
                    _semanticModel);

                if (exceptionType != null)
                {
                    var isBeforeFirstYield = IteratorMethodDetector.IsThrowBeforeFirstYield(
                        throwExpr,
                        body);

                    info.ThrowsInIterator.Add(new ThrowInIteratorInfo
                    {
                        ThrowStatement = throwExpr,
                        Location = throwExpr.GetLocation(),
                        ExceptionType = exceptionType,
                        IsBeforeFirstYield = isBeforeFirstYield
                    });
                }
            }
        }

        private void AnalyzeTryFinally(IteratorExceptionInfo info, SyntaxNode body)
        {
            var tryFinallyStatements = IteratorMethodDetector.GetTryFinallyStatements(body);

            foreach (var tryStmt in tryFinallyStatements)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var hasYieldInTry = IteratorMethodDetector.HasYieldInTryBlock(tryStmt);

                if (hasYieldInTry)
                {
                    info.TryFinallyWithYield.Add(new TryFinallyInIteratorInfo
                    {
                        TryStatement = tryStmt,
                        Location = tryStmt.GetLocation(),
                        HasYieldInTryBlock = true,
                        FinallyBlock = tryStmt.Finally
                    });
                }
            }
        }

        /// <summary>
        /// Gets a description of the iterator exception timing issue.
        /// </summary>
        public static string GetExceptionTimingDescription(ThrowInIteratorInfo throwInfo)
        {
            if (throwInfo.IsBeforeFirstYield)
            {
                return "Exception thrown during iterator creation (immediate)";
            }
            else
            {
                return "Exception thrown during iteration (deferred until MoveNext)";
            }
        }

        /// <summary>
        /// Gets a description of the try-finally timing issue.
        /// </summary>
        public static string GetTryFinallyDescription()
        {
            return "Finally block executes when iterator is disposed, not when try block exits";
        }
    }

    /// <summary>
    /// Contains information about exception handling in an iterator method.
    /// </summary>
    public class IteratorExceptionInfo
    {
        public IteratorExceptionInfo()
        {
            ThrowsInIterator = new List<ThrowInIteratorInfo>();
            TryFinallyWithYield = new List<TryFinallyInIteratorInfo>();
        }

        /// <summary>
        /// The method being analyzed.
        /// </summary>
        public IMethodSymbol Method { get; set; }

        /// <summary>
        /// Iterator method information.
        /// </summary>
        public IteratorMethodInfo IteratorInfo { get; set; }

        /// <summary>
        /// Throws in the iterator method.
        /// </summary>
        public List<ThrowInIteratorInfo> ThrowsInIterator { get; set; }

        /// <summary>
        /// Try-finally statements that contain yield.
        /// </summary>
        public List<TryFinallyInIteratorInfo> TryFinallyWithYield { get; set; }
    }

    /// <summary>
    /// Information about a throw in an iterator method.
    /// </summary>
    public class ThrowInIteratorInfo
    {
        /// <summary>
        /// The throw statement or expression.
        /// </summary>
        public SyntaxNode ThrowStatement { get; set; }

        /// <summary>
        /// Location of the throw.
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// The exception type being thrown.
        /// </summary>
        public ITypeSymbol ExceptionType { get; set; }

        /// <summary>
        /// True if the throw is before the first yield (immediate exception).
        /// False if after yield (deferred until enumeration).
        /// </summary>
        public bool IsBeforeFirstYield { get; set; }
    }

    /// <summary>
    /// Information about a try-finally in an iterator method.
    /// </summary>
    public class TryFinallyInIteratorInfo
    {
        /// <summary>
        /// The try statement.
        /// </summary>
        public TryStatementSyntax TryStatement { get; set; }

        /// <summary>
        /// Location of the try statement.
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// True if the try block contains yield statements.
        /// </summary>
        public bool HasYieldInTryBlock { get; set; }

        /// <summary>
        /// The finally block.
        /// </summary>
        public FinallyClauseSyntax FinallyBlock { get; set; }
    }
}
