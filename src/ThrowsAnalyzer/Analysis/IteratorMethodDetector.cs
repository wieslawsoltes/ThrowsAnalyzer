using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CoreIteratorDetector = RoslynAnalyzer.Core.Analysis.Patterns.Iterators.IteratorMethodDetector;

namespace ThrowsAnalyzer.Analysis
{
    /// <summary>
    /// Detects and analyzes iterator methods with exception-specific functionality.
    /// Delegates generic iterator detection to RoslynAnalyzer.Core.
    /// </summary>
    public static class IteratorMethodDetector
    {
        // Generic methods delegated to RoslynAnalyzer.Core

        /// <summary>
        /// Checks if a method is an iterator method (uses yield return or yield break).
        /// </summary>
        public static bool IsIteratorMethod(IMethodSymbol method, SyntaxNode methodNode)
            => CoreIteratorDetector.IsIteratorMethod(method, methodNode);

        /// <summary>
        /// Checks if a method returns IEnumerable or IEnumerator.
        /// </summary>
        public static bool ReturnsEnumerable(IMethodSymbol method, Compilation compilation)
            => CoreIteratorDetector.ReturnsEnumerable(method, compilation);

        /// <summary>
        /// Gets all yield return statements in a method.
        /// </summary>
        public static IEnumerable<YieldStatementSyntax> GetYieldReturnStatements(SyntaxNode methodBody)
            => CoreIteratorDetector.GetYieldReturnStatements(methodBody);

        /// <summary>
        /// Gets all yield break statements in a method.
        /// </summary>
        public static IEnumerable<YieldStatementSyntax> GetYieldBreakStatements(SyntaxNode methodBody)
            => CoreIteratorDetector.GetYieldBreakStatements(methodBody);

        /// <summary>
        /// Checks if method body contains any yield statements.
        /// </summary>
        public static bool HasYieldStatements(SyntaxNode methodBody)
            => CoreIteratorDetector.HasYieldStatements(methodBody);

        /// <summary>
        /// Gets all throw statements in an iterator method.
        /// </summary>
        public static IEnumerable<ThrowStatementSyntax> GetThrowStatements(SyntaxNode methodBody)
            => CoreIteratorDetector.GetThrowStatements(methodBody);

        /// <summary>
        /// Gets all try-finally statements in an iterator method.
        /// </summary>
        public static IEnumerable<TryStatementSyntax> GetTryFinallyStatements(SyntaxNode methodBody)
            => CoreIteratorDetector.GetTryFinallyStatements(methodBody);

        /// <summary>
        /// Checks if a try-finally contains yield statements in the try block.
        /// </summary>
        public static bool HasYieldInTryBlock(TryStatementSyntax tryStatement)
            => CoreIteratorDetector.HasYieldInTryBlock(tryStatement);

        /// <summary>
        /// Gets the method body for analysis, handling both block and expression bodies.
        /// </summary>
        public static SyntaxNode GetMethodBody(SyntaxNode methodNode)
            => CoreIteratorDetector.GetMethodBody(methodNode);

        // Exception-specific methods (not in Core)

        /// <summary>
        /// Checks if a throw statement is before the first yield.
        /// Exception-specific wrapper around IsBeforeFirstYield.
        /// </summary>
        public static bool IsThrowBeforeFirstYield(SyntaxNode throwNode, SyntaxNode methodBody)
            => CoreIteratorDetector.IsBeforeFirstYield(throwNode, methodBody);

        /// <summary>
        /// Gets comprehensive information about an iterator method.
        /// </summary>
        public static IteratorMethodInfo GetIteratorMethodInfo(
            IMethodSymbol method,
            SyntaxNode methodNode,
            Compilation compilation)
        {
            var coreInfo = CoreIteratorDetector.GetIteratorMethodInfo(method, methodNode, compilation);
            var body = GetMethodBody(methodNode);

            return new IteratorMethodInfo
            {
                Method = coreInfo.Method,
                IsIterator = coreInfo.IsIterator,
                ReturnsEnumerable = coreInfo.ReturnsEnumerable,
                YieldReturnCount = coreInfo.YieldReturnCount,
                YieldBreakCount = coreInfo.YieldBreakCount,
                ThrowCount = body != null ? GetThrowStatements(body).Count() : 0,
                TryFinallyCount = body != null ? GetTryFinallyStatements(body).Count() : 0
            };
        }
    }

    /// <summary>
    /// Contains information about an iterator method.
    /// </summary>
    public class IteratorMethodInfo
    {
        /// <summary>
        /// The method symbol.
        /// </summary>
        public IMethodSymbol Method { get; set; }

        /// <summary>
        /// True if the method uses yield return or yield break.
        /// </summary>
        public bool IsIterator { get; set; }

        /// <summary>
        /// True if the method returns IEnumerable or IEnumerator.
        /// </summary>
        public bool ReturnsEnumerable { get; set; }

        /// <summary>
        /// Number of yield return statements.
        /// </summary>
        public int YieldReturnCount { get; set; }

        /// <summary>
        /// Number of yield break statements.
        /// </summary>
        public int YieldBreakCount { get; set; }

        /// <summary>
        /// Number of throw statements.
        /// </summary>
        public int ThrowCount { get; set; }

        /// <summary>
        /// Number of try-finally statements.
        /// </summary>
        public int TryFinallyCount { get; set; }
    }
}
