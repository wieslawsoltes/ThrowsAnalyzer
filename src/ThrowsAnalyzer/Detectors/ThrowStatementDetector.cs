using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ThrowsAnalyzer.Core;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Detects throw statements and throw expressions in executable members.
    /// Now supports methods, constructors, properties, local functions, lambdas, etc.
    /// Excludes nested executable members to avoid double-reporting.
    /// </summary>
    public static class ThrowStatementDetector
    {
        /// <summary>
        /// Checks if a method contains throw statements or throw expressions.
        /// </summary>
        public static bool HasThrowStatements(MethodDeclarationSyntax methodDeclaration)
        {
            return HasThrowStatements((SyntaxNode)methodDeclaration);
        }

        /// <summary>
        /// Checks if any executable member contains throw statements or throw expressions.
        /// Supports methods, constructors, properties, operators, local functions, lambdas, etc.
        /// Only checks direct throws, excludes nested executable members (local functions, lambdas).
        /// </summary>
        public static bool HasThrowStatements(SyntaxNode node)
        {
            var executableBlocks = ExecutableMemberHelper.GetExecutableBlocks(node);

            foreach (var block in executableBlocks)
            {
                if (ContainsThrowSyntax(block))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsThrowSyntax(SyntaxNode node)
        {
            // Check the node itself first (for throw expressions at the top level)
            if (node is ThrowStatementSyntax or ThrowExpressionSyntax)
            {
                return true;
            }

            // Get all descendant nodes but exclude nested executable members
            return node.DescendantNodes(n => !IsNestedExecutableMember(n))
                .Any(n => n is ThrowStatementSyntax or ThrowExpressionSyntax);
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
