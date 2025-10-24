using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Composable detector that combines ThrowStatementDetector and TryCatchDetector
    /// to identify methods with unhandled throw statements.
    /// </summary>
    public static class UnhandledThrowDetector
    {
        /// <summary>
        /// Checks if a method has throw statements that are not wrapped in try/catch blocks.
        /// </summary>
        public static bool HasUnhandledThrows(MethodDeclarationSyntax methodDeclaration)
        {
            // Reuse ThrowStatementDetector - if no throws, nothing to check
            if (!ThrowStatementDetector.HasThrowStatements(methodDeclaration))
            {
                return false;
            }

            // Reuse TryCatchDetector - if has try/catch, throws might be handled
            if (!TryCatchDetector.HasTryCatchBlocks(methodDeclaration))
            {
                // Has throws but no try/catch = definitely unhandled
                return true;
            }

            // Complex case: has both throws and try/catch
            // Check if all throws are inside try blocks
            return HasThrowsOutsideTryBlocks(methodDeclaration);
        }

        private static bool HasThrowsOutsideTryBlocks(MethodDeclarationSyntax methodDeclaration)
        {
            var tryBlocks = TryCatchDetector.GetTryCatchBlocks(methodDeclaration).ToList();

            if (methodDeclaration.Body != null)
            {
                var throwStatements = methodDeclaration.Body.DescendantNodes()
                    .OfType<ThrowStatementSyntax>()
                    .ToList();

                foreach (var throwStatement in throwStatements)
                {
                    if (!IsInsideAnyTryBlock(throwStatement, tryBlocks))
                    {
                        return true;
                    }
                }
            }

            if (methodDeclaration.ExpressionBody != null)
            {
                var throwExpressions = methodDeclaration.ExpressionBody.DescendantNodes()
                    .OfType<ThrowExpressionSyntax>()
                    .ToList();

                // Expression-bodied methods can't have try/catch, so any throw is unhandled
                return throwExpressions.Any();
            }

            return false;
        }

        private static bool IsInsideAnyTryBlock(SyntaxNode throwNode, System.Collections.Generic.List<TryStatementSyntax> tryBlocks)
        {
            foreach (var tryBlock in tryBlocks)
            {
                if (IsInsideTryBlock(throwNode, tryBlock))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInsideTryBlock(SyntaxNode throwNode, TryStatementSyntax tryBlock)
        {
            var current = throwNode.Parent;
            while (current != null)
            {
                if (current == tryBlock.Block)
                {
                    return true;
                }
                current = current.Parent;
            }

            return false;
        }
    }
}
