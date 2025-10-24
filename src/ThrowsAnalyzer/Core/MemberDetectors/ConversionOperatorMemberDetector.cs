using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ThrowsAnalyzer.Core.MemberDetectors
{
    /// <summary>
    /// Detector for conversion operators (ConversionOperatorDeclarationSyntax).
    /// Handles both implicit and explicit conversion operators.
    /// </summary>
    public class ConversionOperatorMemberDetector : IExecutableMemberDetector
    {
        public bool SupportsNode(SyntaxNode node)
        {
            return node is ConversionOperatorDeclarationSyntax;
        }

        public IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node)
        {
            if (node is not ConversionOperatorDeclarationSyntax conversion)
            {
                yield break;
            }

            if (conversion.Body != null)
            {
                yield return conversion.Body;
            }

            if (conversion.ExpressionBody != null)
            {
                yield return conversion.ExpressionBody;
            }
        }

        public string GetMemberDisplayName(SyntaxNode node)
        {
            if (node is ConversionOperatorDeclarationSyntax conversion)
            {
                var kind = conversion.ImplicitOrExplicitKeyword.Text;
                return $"Conversion operator '{kind} operator {conversion.Type}'";
            }

            return "Conversion operator";
        }
    }
}
