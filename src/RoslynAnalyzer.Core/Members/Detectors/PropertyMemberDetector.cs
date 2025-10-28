using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace RoslynAnalyzer.Core.Members.Detectors
{
    /// <summary>
    /// Detector for expression-bodied properties (PropertyDeclarationSyntax with ExpressionBody).
    /// Regular properties with block-bodied accessors are handled by AccessorMemberDetector.
    /// </summary>
    public class PropertyMemberDetector : IExecutableMemberDetector
    {
        public bool SupportsNode(SyntaxNode node)
        {
            // Only support properties with expression bodies
            // Properties with accessor declarations are handled by AccessorMemberDetector
            return node is PropertyDeclarationSyntax property && property.ExpressionBody != null;
        }

        public IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node)
        {
            if (node is not PropertyDeclarationSyntax property)
            {
                yield break;
            }

            if (property.ExpressionBody != null)
            {
                yield return property.ExpressionBody;
            }
        }

        public string GetMemberDisplayName(SyntaxNode node)
        {
            if (node is PropertyDeclarationSyntax property)
            {
                return $"Property '{property.Identifier.Text}'";
            }

            return "Property";
        }
    }
}
