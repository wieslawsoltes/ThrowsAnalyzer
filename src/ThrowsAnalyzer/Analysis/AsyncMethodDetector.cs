using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CoreAsyncDetector = RoslynAnalyzer.Core.Analysis.Patterns.Async.AsyncMethodDetector;

namespace ThrowsAnalyzer.Analysis
{
    /// <summary>
    /// Detects and analyzes async methods with exception-specific functionality.
    /// For generic async detection, use RoslynAnalyzer.Core.Analysis.Patterns.Async.AsyncMethodDetector directly.
    /// </summary>
    /// <remarks>
    /// This class only contains exception-specific wrappers.
    /// For all other async detection methods, use the Core library directly:
    /// - IsAsyncMethod() → CoreAsyncDetector.IsAsyncMethod()
    /// - ReturnsTask() → CoreAsyncDetector.ReturnsTask()
    /// - IsAsyncVoid() → CoreAsyncDetector.IsAsyncVoid()
    /// - GetFirstAwaitExpression() → CoreAsyncDetector.GetFirstAwaitExpression()
    /// - GetAllAwaitExpressions() → CoreAsyncDetector.GetAllAwaitExpressions()
    /// - GetUnawaitedTaskInvocations() → CoreAsyncDetector.GetUnawaitedTaskInvocations()
    /// - GetMethodBody() → CoreAsyncDetector.GetMethodBody()
    /// - HasAsyncModifier() → CoreAsyncDetector.HasAsyncModifier()
    /// - GetAsyncMethodInfo() → CoreAsyncDetector.GetAsyncMethodInfo()
    /// </remarks>
    public static class AsyncMethodDetector
    {
        /// <summary>
        /// Checks if a throw statement occurs before the first await.
        /// Exception-specific wrapper around Core's IsBeforeFirstAwait.
        /// </summary>
        public static bool IsThrowBeforeFirstAwait(
            SyntaxNode throwNode,
            SyntaxNode methodBody)
            => CoreAsyncDetector.IsBeforeFirstAwait(throwNode, methodBody);

        /// <summary>
        /// Gets information about a task-returning method that may throw.
        /// Wraps Core's AsyncMethodInfo for backward compatibility.
        /// </summary>
        public static AsyncMethodInfo GetAsyncMethodInfo(
            IMethodSymbol method,
            SyntaxNode methodNode,
            SemanticModel semanticModel)
        {
            var coreInfo = CoreAsyncDetector.GetAsyncMethodInfo(method, methodNode, semanticModel);

            return new AsyncMethodInfo
            {
                Method = coreInfo.Method,
                IsAsync = coreInfo.IsAsync,
                IsAsyncVoid = coreInfo.IsAsyncVoid,
                ReturnsTask = coreInfo.ReturnsTask,
                HasAwaitExpressions = coreInfo.HasAwaitExpressions,
                FirstAwaitExpression = coreInfo.FirstAwaitExpression
            };
        }
    }

    /// <summary>
    /// Contains information about an async method.
    /// </summary>
    public class AsyncMethodInfo
    {
        /// <summary>
        /// The method symbol.
        /// </summary>
        public IMethodSymbol Method { get; set; }

        /// <summary>
        /// True if the method has the async modifier.
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// True if the method is async void.
        /// </summary>
        public bool IsAsyncVoid { get; set; }

        /// <summary>
        /// True if the method returns Task or Task&lt;T&gt;.
        /// </summary>
        public bool ReturnsTask { get; set; }

        /// <summary>
        /// True if the method contains any await expressions.
        /// </summary>
        public bool HasAwaitExpressions { get; set; }

        /// <summary>
        /// The first await expression in the method, or null if none.
        /// </summary>
        public AwaitExpressionSyntax FirstAwaitExpression { get; set; }
    }
}
