using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ThrowsAnalyzer.Core;
using ThrowsAnalyzer.TypeAnalysis.Models;

namespace ThrowsAnalyzer.TypeAnalysis.Detectors
{
    /// <summary>
    /// Enhanced throw detector that includes type information.
    /// Builds on ThrowStatementDetector with semantic analysis.
    /// </summary>
    public static class TypedThrowDetector
    {
        /// <summary>
        /// Gets all throws in a member with type information.
        /// </summary>
        /// <param name="node">The executable member node to analyze</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <returns>Enumerable of TypedThrowInfo for each throw found</returns>
        public static IEnumerable<TypedThrowInfo> GetTypedThrows(
            SyntaxNode node,
            SemanticModel semanticModel)
        {
            var executableBlocks = ExecutableMemberHelper.GetExecutableBlocks(node);
            var throws = new List<TypedThrowInfo>();

            foreach (var block in executableBlocks)
            {
                var throwNodes = GetThrowNodes(block);

                foreach (var throwNode in throwNodes)
                {
                    var exceptionType = ExceptionTypeAnalyzer
                        .GetThrownExceptionType(throwNode, semanticModel);

                    var isRethrow = IsRethrow(throwNode);

                    throws.Add(new TypedThrowInfo
                    {
                        ThrowNode = throwNode,
                        ExceptionType = exceptionType,
                        IsRethrow = isRethrow,
                        Location = throwNode.GetLocation()
                    });
                }
            }

            return throws;
        }

        /// <summary>
        /// Gets all throw statement and expression nodes from a syntax node,
        /// excluding those in nested executable members.
        /// </summary>
        private static IEnumerable<SyntaxNode> GetThrowNodes(SyntaxNode node)
        {
            // Check the node itself first (for throw expressions at the top level)
            if (node is ThrowStatementSyntax or ThrowExpressionSyntax)
            {
                yield return node;
            }

            // Get throw statements and expressions, excluding nested members
            var descendants = node.DescendantNodes(n => !IsNestedExecutableMember(n))
                .Where(n => n is ThrowStatementSyntax or ThrowExpressionSyntax);

            foreach (var throwNode in descendants)
            {
                yield return throwNode;
            }
        }

        /// <summary>
        /// Checks if a throw node is a bare rethrow (throw; with no expression).
        /// </summary>
        private static bool IsRethrow(SyntaxNode throwNode)
        {
            // throw; (bare rethrow has null expression)
            return throwNode is ThrowStatementSyntax stmt && stmt.Expression == null;
        }

        /// <summary>
        /// Checks if a node is a nested executable member that should be analyzed separately.
        /// </summary>
        private static bool IsNestedExecutableMember(SyntaxNode node)
        {
            return node is LocalFunctionStatementSyntax
                or SimpleLambdaExpressionSyntax
                or ParenthesizedLambdaExpressionSyntax
                or AnonymousMethodExpressionSyntax;
        }
    }
}
