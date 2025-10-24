using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ThrowsAnalyzer.Core;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Composable detector that combines ThrowStatementDetector and TryCatchDetector
    /// to identify executable members with unhandled throw statements.
    /// Now supports methods, constructors, properties, local functions, lambdas, etc.
    /// </summary>
    public static class UnhandledThrowDetector
    {
        /// <summary>
        /// Checks if a method has throw statements that are not wrapped in try/catch blocks.
        /// </summary>
        public static bool HasUnhandledThrows(MethodDeclarationSyntax methodDeclaration)
        {
            return HasUnhandledThrows((SyntaxNode)methodDeclaration);
        }

        /// <summary>
        /// Checks if any executable member has throw statements that are not wrapped in try/catch blocks.
        /// Supports methods, constructors, properties, operators, local functions, lambdas, etc.
        /// </summary>
        public static bool HasUnhandledThrows(SyntaxNode node)
        {
            // Reuse ThrowStatementDetector - if no throws, nothing to check
            if (!ThrowStatementDetector.HasThrowStatements(node))
            {
                return false;
            }

            // Reuse TryCatchDetector - if has try/catch, throws might be handled
            if (!TryCatchDetector.HasTryCatchBlocks(node))
            {
                // Has throws but no try/catch = definitely unhandled
                return true;
            }

            // Complex case: has both throws and try/catch
            // Check if all throws are inside try blocks
            return HasThrowsOutsideTryBlocks(node);
        }

        private static bool HasThrowsOutsideTryBlocks(SyntaxNode node)
        {
            var tryBlocks = TryCatchDetector.GetTryCatchBlocks(node).ToList();
            var executableBlocks = ExecutableMemberHelper.GetExecutableBlocks(node);

            foreach (var block in executableBlocks)
            {
                // Check throw statements
                var throwStatements = block.DescendantNodes()
                    .OfType<ThrowStatementSyntax>()
                    .ToList();

                foreach (var throwStatement in throwStatements)
                {
                    if (!IsInsideAnyTryBlock(throwStatement, tryBlocks))
                    {
                        return true;
                    }
                }

                // Check throw expressions
                var throwExpressions = block.DescendantNodes()
                    .OfType<ThrowExpressionSyntax>()
                    .ToList();

                foreach (var throwExpression in throwExpressions)
                {
                    if (!IsInsideAnyTryBlock(throwExpression, tryBlocks))
                    {
                        return true;
                    }
                }
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
