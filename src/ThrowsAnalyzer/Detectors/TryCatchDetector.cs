using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynAnalyzer.Core.Members;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Detects try/catch/finally blocks in executable members.
    /// Now supports methods, constructors, properties, local functions, lambdas, etc.
    /// Excludes nested executable members to avoid double-reporting.
    /// </summary>
    public static class TryCatchDetector
    {
        /// <summary>
        /// Checks if a method contains try/catch/finally blocks.
        /// </summary>
        public static bool HasTryCatchBlocks(MethodDeclarationSyntax methodDeclaration)
        {
            return HasTryCatchBlocks((SyntaxNode)methodDeclaration);
        }

        /// <summary>
        /// Checks if any executable member contains try/catch/finally blocks.
        /// Supports methods, constructors, properties, operators, local functions, lambdas, etc.
        /// Only checks direct try/catch blocks, excludes nested executable members.
        /// </summary>
        public static bool HasTryCatchBlocks(SyntaxNode node)
        {
            var executableBlocks = ExecutableMemberHelper.GetExecutableBlocks(node);

            foreach (var block in executableBlocks)
            {
                if (ContainsTryCatch(block))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all try/catch/finally blocks from a method.
        /// </summary>
        public static IEnumerable<TryStatementSyntax> GetTryCatchBlocks(MethodDeclarationSyntax methodDeclaration)
        {
            return GetTryCatchBlocks((SyntaxNode)methodDeclaration);
        }

        /// <summary>
        /// Gets all try/catch/finally blocks from any executable member.
        /// Supports methods, constructors, properties, operators, local functions, lambdas, etc.
        /// Only gets direct try/catch blocks, excludes nested executable members.
        /// </summary>
        public static IEnumerable<TryStatementSyntax> GetTryCatchBlocks(SyntaxNode node)
        {
            var executableBlocks = ExecutableMemberHelper.GetExecutableBlocks(node);

            foreach (var block in executableBlocks)
            {
                // Get try blocks but exclude nested executable members
                var tryBlocks = block.DescendantNodes(n => !IsNestedExecutableMember(n))
                    .OfType<TryStatementSyntax>();

                foreach (var tryBlock in tryBlocks)
                {
                    yield return tryBlock;
                }
            }
        }

        private static bool ContainsTryCatch(SyntaxNode node)
        {
            // Get all descendant nodes but exclude nested executable members
            return node.DescendantNodes(n => !IsNestedExecutableMember(n))
                .Any(n => n is TryStatementSyntax);
        }

        private static bool IsNestedExecutableMember(SyntaxNode node)
        {
            // Don't descend into nested local functions or lambdas
            // They will be analyzed separately
            return node is LocalFunctionStatementSyntax
                or SimpleLambdaExpressionSyntax
                or ParenthesizedLambdaExpressionSyntax
                or AnonymousMethodExpressionSyntax;
        }
    }
}
