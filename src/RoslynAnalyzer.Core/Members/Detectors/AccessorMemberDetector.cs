using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace RoslynAnalyzer.Core.Members.Detectors
{
    /// <summary>
    /// Detector for property, indexer, and event accessors (AccessorDeclarationSyntax).
    /// Handles get, set, init, add, remove accessors.
    /// </summary>
    public class AccessorMemberDetector : IExecutableMemberDetector
    {
        public bool SupportsNode(SyntaxNode node)
        {
            return node is AccessorDeclarationSyntax;
        }

        public IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node)
        {
            if (node is not AccessorDeclarationSyntax accessor)
            {
                yield break;
            }

            if (accessor.Body != null)
            {
                yield return accessor.Body;
            }

            if (accessor.ExpressionBody != null)
            {
                yield return accessor.ExpressionBody;
            }
        }

        public string GetMemberDisplayName(SyntaxNode node)
        {
            if (node is not AccessorDeclarationSyntax accessor)
            {
                return "Accessor";
            }

            var accessorKind = accessor.Kind() switch
            {
                SyntaxKind.GetAccessorDeclaration => "getter",
                SyntaxKind.SetAccessorDeclaration => "setter",
                SyntaxKind.InitAccessorDeclaration => "init accessor",
                SyntaxKind.AddAccessorDeclaration => "add accessor",
                SyntaxKind.RemoveAccessorDeclaration => "remove accessor",
                _ => "accessor"
            };

            // Try to find the parent property/indexer/event
            var parent = accessor.Parent?.Parent;
            if (parent is PropertyDeclarationSyntax property)
            {
                return $"Property '{property.Identifier.Text}' {accessorKind}";
            }
            else if (parent is IndexerDeclarationSyntax)
            {
                return $"Indexer {accessorKind}";
            }
            else if (parent is EventDeclarationSyntax eventDecl)
            {
                return $"Event '{eventDecl.Identifier.Text}' {accessorKind}";
            }

            return $"Accessor {accessorKind}";
        }
    }
}
