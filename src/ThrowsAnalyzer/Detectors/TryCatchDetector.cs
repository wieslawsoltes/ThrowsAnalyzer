using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer
{
    public static class TryCatchDetector
    {
        public static bool HasTryCatchBlocks(MethodDeclarationSyntax methodDeclaration)
        {
            var nodesToCheck = GetNodesToAnalyze(methodDeclaration);

            foreach (var node in nodesToCheck)
            {
                if (ContainsTryCatch(node))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<TryStatementSyntax> GetTryCatchBlocks(MethodDeclarationSyntax methodDeclaration)
        {
            var nodesToCheck = GetNodesToAnalyze(methodDeclaration);

            foreach (var node in nodesToCheck)
            {
                var tryBlocks = node.DescendantNodes().OfType<TryStatementSyntax>();
                foreach (var tryBlock in tryBlocks)
                {
                    yield return tryBlock;
                }
            }
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

        private static bool ContainsTryCatch(SyntaxNode node)
        {
            return node.DescendantNodes().Any(n => n is TryStatementSyntax);
        }
    }
}
