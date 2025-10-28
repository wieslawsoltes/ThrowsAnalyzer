using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace RoslynAnalyzer.Core.Members.Detectors
{
    /// <summary>
    /// Detector for operator overloads (OperatorDeclarationSyntax).
    /// Handles both block-bodied and expression-bodied operators.
    /// </summary>
    public class OperatorMemberDetector : IExecutableMemberDetector
    {
        public bool SupportsNode(SyntaxNode node)
        {
            return node is OperatorDeclarationSyntax;
        }

        public IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node)
        {
            if (node is not OperatorDeclarationSyntax operatorDecl)
            {
                yield break;
            }

            if (operatorDecl.Body != null)
            {
                yield return operatorDecl.Body;
            }

            if (operatorDecl.ExpressionBody != null)
            {
                yield return operatorDecl.ExpressionBody;
            }
        }

        public string GetMemberDisplayName(SyntaxNode node)
        {
            if (node is OperatorDeclarationSyntax operatorDecl)
            {
                return $"Operator '{operatorDecl.OperatorToken.Text}'";
            }

            return "Operator";
        }
    }
}
