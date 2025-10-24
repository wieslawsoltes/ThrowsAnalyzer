using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ThrowsAnalyzer.Core.MemberDetectors
{
    /// <summary>
    /// Detector for lambda expressions.
    /// Handles SimpleLambdaExpressionSyntax and ParenthesizedLambdaExpressionSyntax.
    /// </summary>
    public class LambdaMemberDetector : IExecutableMemberDetector
    {
        public bool SupportsNode(SyntaxNode node)
        {
            return node is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax;
        }

        public IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node)
        {
            if (node is SimpleLambdaExpressionSyntax simpleLambda)
            {
                if (simpleLambda.Block != null)
                {
                    yield return simpleLambda.Block;
                }
                else if (simpleLambda.ExpressionBody != null)
                {
                    yield return simpleLambda.ExpressionBody;
                }
            }
            else if (node is ParenthesizedLambdaExpressionSyntax parenLambda)
            {
                if (parenLambda.Block != null)
                {
                    yield return parenLambda.Block;
                }
                else if (parenLambda.ExpressionBody != null)
                {
                    yield return parenLambda.ExpressionBody;
                }
            }
        }

        public string GetMemberDisplayName(SyntaxNode node)
        {
            return "Lambda expression";
        }
    }
}
