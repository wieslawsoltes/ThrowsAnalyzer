using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Helper methods for Roslyn analyzers.
    /// </summary>
    public static class AnalyzerHelper
    {
        /// <summary>
        /// Gets the appropriate location for reporting diagnostics based on member type.
        /// Returns the identifier or keyword location for better diagnostic precision.
        /// </summary>
        public static Location GetMemberLocation(SyntaxNode node)
        {
            return node switch
            {
                MethodDeclarationSyntax method => method.Identifier.GetLocation(),
                ConstructorDeclarationSyntax ctor => ctor.Identifier.GetLocation(),
                DestructorDeclarationSyntax dtor => dtor.Identifier.GetLocation(),
                OperatorDeclarationSyntax op => op.OperatorToken.GetLocation(),
                ConversionOperatorDeclarationSyntax conv => conv.Type.GetLocation(),
                PropertyDeclarationSyntax property => property.Identifier.GetLocation(),
                AccessorDeclarationSyntax accessor => accessor.Keyword.GetLocation(),
                LocalFunctionStatementSyntax local => local.Identifier.GetLocation(),
                _ => node.GetLocation()
            };
        }
    }
}
