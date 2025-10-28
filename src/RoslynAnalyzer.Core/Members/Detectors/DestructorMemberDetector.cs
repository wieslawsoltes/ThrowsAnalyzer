using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace RoslynAnalyzer.Core.Members.Detectors
{
    /// <summary>
    /// Detector for destructors/finalizers (DestructorDeclarationSyntax).
    /// Note: Throwing in finalizers is dangerous and should generally be avoided.
    /// </summary>
    public class DestructorMemberDetector : IExecutableMemberDetector
    {
        public bool SupportsNode(SyntaxNode node)
        {
            return node is DestructorDeclarationSyntax;
        }

        public IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node)
        {
            if (node is not DestructorDeclarationSyntax destructor)
            {
                yield break;
            }

            if (destructor.Body != null)
            {
                yield return destructor.Body;
            }

            if (destructor.ExpressionBody != null)
            {
                yield return destructor.ExpressionBody;
            }
        }

        public string GetMemberDisplayName(SyntaxNode node)
        {
            if (node is DestructorDeclarationSyntax destructor)
            {
                return $"Destructor '~{destructor.Identifier.Text}'";
            }

            return "Destructor";
        }
    }
}
