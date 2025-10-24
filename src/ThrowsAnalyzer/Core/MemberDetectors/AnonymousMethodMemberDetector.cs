using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ThrowsAnalyzer.Core.MemberDetectors
{
    /// <summary>
    /// Detector for anonymous methods (AnonymousMethodExpressionSyntax).
    /// Legacy syntax: delegate { ... } or delegate(int x) { ... }
    /// </summary>
    public class AnonymousMethodMemberDetector : IExecutableMemberDetector
    {
        public bool SupportsNode(SyntaxNode node)
        {
            return node is AnonymousMethodExpressionSyntax;
        }

        public IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node)
        {
            if (node is not AnonymousMethodExpressionSyntax anonymousMethod)
            {
                yield break;
            }

            if (anonymousMethod.Block != null)
            {
                yield return anonymousMethod.Block;
            }

            // Note: Anonymous methods cannot have expression bodies
        }

        public string GetMemberDisplayName(SyntaxNode node)
        {
            return "Anonymous method";
        }
    }
}
