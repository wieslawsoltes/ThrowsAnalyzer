using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer
{
    public static class MethodThrowAnalyzer
    {
        public static bool HasThrowStatements(MethodDeclarationSyntax methodDeclaration)
        {
            var nodesToCheck = GetNodesToAnalyze(methodDeclaration);

            foreach (var node in nodesToCheck)
            {
                if (ContainsThrowSyntax(node))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<SyntaxNode> GetNodesToAnalyze(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.Body != null)
            {
                yield return methodDeclaration.Body;
            }

            if (methodDeclaration.ExpressionBody != null)
            {
                yield return methodDeclaration.ExpressionBody;
            }
        }

        private static bool ContainsThrowSyntax(SyntaxNode node)
        {
            return node.DescendantNodes().Any(n => n is ThrowStatementSyntax or ThrowExpressionSyntax);
        }
    }
}
