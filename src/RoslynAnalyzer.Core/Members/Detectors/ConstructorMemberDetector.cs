using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace RoslynAnalyzer.Core.Members.Detectors
{
    /// <summary>
    /// Detector for constructors (ConstructorDeclarationSyntax).
    /// Handles both block-bodied and expression-bodied constructors.
    /// </summary>
    public class ConstructorMemberDetector : IExecutableMemberDetector
    {
        public bool SupportsNode(SyntaxNode node)
        {
            return node is ConstructorDeclarationSyntax;
        }

        public IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node)
        {
            if (node is not ConstructorDeclarationSyntax constructor)
            {
                yield break;
            }

            if (constructor.Body != null)
            {
                yield return constructor.Body;
            }

            if (constructor.ExpressionBody != null)
            {
                yield return constructor.ExpressionBody;
            }
        }

        public string GetMemberDisplayName(SyntaxNode node)
        {
            if (node is ConstructorDeclarationSyntax constructor)
            {
                return $"Constructor '{constructor.Identifier.Text}'";
            }

            return "Constructor";
        }
    }
}
