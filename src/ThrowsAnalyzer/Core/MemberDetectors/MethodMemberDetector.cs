using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ThrowsAnalyzer.Core.MemberDetectors
{
    /// <summary>
    /// Detector for regular methods (MethodDeclarationSyntax).
    /// Handles both block-bodied and expression-bodied methods.
    /// </summary>
    public class MethodMemberDetector : IExecutableMemberDetector
    {
        public bool SupportsNode(SyntaxNode node)
        {
            return node is MethodDeclarationSyntax;
        }

        public IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node)
        {
            if (node is not MethodDeclarationSyntax method)
            {
                yield break;
            }

            if (method.Body != null)
            {
                yield return method.Body;
            }

            if (method.ExpressionBody != null)
            {
                yield return method.ExpressionBody;
            }
        }

        public string GetMemberDisplayName(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax method)
            {
                return $"Method '{method.Identifier.Text}'";
            }

            return "Method";
        }
    }
}
