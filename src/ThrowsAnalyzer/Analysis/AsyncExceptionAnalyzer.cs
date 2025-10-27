using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ThrowsAnalyzer.TypeAnalysis;

namespace ThrowsAnalyzer.Analysis
{
    /// <summary>
    /// Analyzes exception handling patterns in async methods.
    /// </summary>
    public class AsyncExceptionAnalyzer
    {
        private readonly SemanticModel _semanticModel;
        private readonly CancellationToken _cancellationToken;

        public AsyncExceptionAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken = default)
        {
            _semanticModel = semanticModel;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Analyzes an async method for exception handling issues.
        /// </summary>
        public async Task<AsyncExceptionInfo> AnalyzeAsync(
            IMethodSymbol method,
            SyntaxNode methodNode)
        {
            var info = new AsyncExceptionInfo
            {
                Method = method,
                AsyncInfo = AsyncMethodDetector.GetAsyncMethodInfo(method, methodNode, _semanticModel)
            };

            var body = AsyncMethodDetector.GetMethodBody(methodNode);
            if (body == null)
                return info;

            // Find all throw statements/expressions
            var throwStatements = body.DescendantNodes().OfType<ThrowStatementSyntax>().ToList();
            var throwExpressions = body.DescendantNodes().OfType<ThrowExpressionSyntax>().ToList();

            // Analyze throws before first await
            AnalyzeThrowsBeforeAwait(info, throwStatements, throwExpressions, body);

            // Analyze unawaited task invocations
            AnalyzeUnawaitedTasks(info, body);

            return info;
        }

        private void AnalyzeThrowsBeforeAwait(
            AsyncExceptionInfo info,
            List<ThrowStatementSyntax> throwStatements,
            List<ThrowExpressionSyntax> throwExpressions,
            SyntaxNode body)
        {
            foreach (var throwStmt in throwStatements)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // Skip bare rethrows
                if (throwStmt.Expression == null)
                    continue;

                if (AsyncMethodDetector.IsThrowBeforeFirstAwait(throwStmt, body))
                {
                    var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwStmt, _semanticModel);
                    if (exceptionType != null)
                    {
                        info.ThrowsBeforeAwait.Add(new ThrowBeforeAwaitInfo
                        {
                            ThrowNode = throwStmt,
                            Location = throwStmt.GetLocation(),
                            ExceptionType = exceptionType
                        });
                    }
                }
            }

            foreach (var throwExpr in throwExpressions)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (AsyncMethodDetector.IsThrowBeforeFirstAwait(throwExpr, body))
                {
                    var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwExpr, _semanticModel);
                    if (exceptionType != null)
                    {
                        info.ThrowsBeforeAwait.Add(new ThrowBeforeAwaitInfo
                        {
                            ThrowNode = throwExpr,
                            Location = throwExpr.GetLocation(),
                            ExceptionType = exceptionType
                        });
                    }
                }
            }
        }

        private void AnalyzeUnawaitedTasks(AsyncExceptionInfo info, SyntaxNode body)
        {
            var unawaitedInvocations = AsyncMethodDetector.GetUnawaitedTaskInvocations(body, _semanticModel);

            foreach (var invocation in unawaitedInvocations)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var symbolInfo = _semanticModel.GetSymbolInfo(invocation, _cancellationToken);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    info.UnawaitedTaskInvocations.Add(new UnawaitedTaskInfo
                    {
                        Invocation = invocation,
                        Location = invocation.GetLocation(),
                        Method = methodSymbol
                    });
                }
            }
        }

        /// <summary>
        /// Checks if a method may throw exceptions that could be unobserved.
        /// </summary>
        public async Task<bool> MayThrowUnobservedExceptionsAsync(
            IMethodSymbol method,
            SyntaxNode methodNode)
        {
            var info = await AnalyzeAsync(method, methodNode);

            // Async void methods always risk unobserved exceptions
            if (info.AsyncInfo.IsAsyncVoid)
                return true;

            // Check for unawaited task-returning calls
            if (info.UnawaitedTaskInvocations.Any())
                return true;

            return false;
        }

        /// <summary>
        /// Gets a description of the async exception issue.
        /// </summary>
        public static string GetIssueDescription(AsyncExceptionInfo info)
        {
            if (info.AsyncInfo.IsAsyncVoid && info.ThrowsBeforeAwait.Any())
            {
                return "Async void method throws exceptions that cannot be caught by callers";
            }

            if (info.ThrowsBeforeAwait.Any())
            {
                return "Throws synchronously before first await, which may bypass async exception handling";
            }

            if (info.UnawaitedTaskInvocations.Any())
            {
                return "Contains unawaited Task-returning calls that may throw unobserved exceptions";
            }

            return "No async exception issues detected";
        }
    }

    /// <summary>
    /// Contains information about async exception handling in a method.
    /// </summary>
    public class AsyncExceptionInfo
    {
        public AsyncExceptionInfo()
        {
            ThrowsBeforeAwait = new List<ThrowBeforeAwaitInfo>();
            UnawaitedTaskInvocations = new List<UnawaitedTaskInfo>();
        }

        /// <summary>
        /// The method being analyzed.
        /// </summary>
        public IMethodSymbol Method { get; set; }

        /// <summary>
        /// Async method information.
        /// </summary>
        public AsyncMethodInfo AsyncInfo { get; set; }

        /// <summary>
        /// Throws that occur before the first await.
        /// </summary>
        public List<ThrowBeforeAwaitInfo> ThrowsBeforeAwait { get; set; }

        /// <summary>
        /// Task-returning invocations that are not awaited.
        /// </summary>
        public List<UnawaitedTaskInfo> UnawaitedTaskInvocations { get; set; }
    }

    /// <summary>
    /// Information about a throw before the first await.
    /// </summary>
    public class ThrowBeforeAwaitInfo
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
    }

    /// <summary>
    /// Information about an unawaited task invocation.
    /// </summary>
    public class UnawaitedTaskInfo
    {
        /// <summary>
        /// The invocation expression.
        /// </summary>
        public InvocationExpressionSyntax Invocation { get; set; }

        /// <summary>
        /// Location of the invocation.
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// The method being invoked.
        /// </summary>
        public IMethodSymbol Method { get; set; }
    }
}
