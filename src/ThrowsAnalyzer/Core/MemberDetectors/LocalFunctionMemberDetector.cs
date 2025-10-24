using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ThrowsAnalyzer.Core.MemberDetectors
{
    /// <summary>
    /// Detector for local functions (LocalFunctionStatementSyntax).
    /// Handles both block-bodied and expression-bodied local functions.
    /// </summary>
    public class LocalFunctionMemberDetector : IExecutableMemberDetector
    {
        public bool SupportsNode(SyntaxNode node)
        {
            return node is LocalFunctionStatementSyntax;
        }

        public IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node)
        {
            if (node is not LocalFunctionStatementSyntax localFunction)
            {
                yield break;
            }

            if (localFunction.Body != null)
            {
                yield return localFunction.Body;
            }

            if (localFunction.ExpressionBody != null)
            {
                yield return localFunction.ExpressionBody;
            }
        }

        public string GetMemberDisplayName(SyntaxNode node)
        {
            if (node is LocalFunctionStatementSyntax localFunction)
            {
                return $"Local function '{localFunction.Identifier.Text}'";
            }

            return "Local function";
        }
    }
}
