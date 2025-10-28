using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalyzer.Core.Helpers
{
    /// <summary>
    /// Helper methods for working with Roslyn diagnostics.
    /// </summary>
    public static class DiagnosticHelpers
    {
        /// <summary>
        /// Gets the appropriate location for reporting diagnostics based on member type.
        /// Returns the identifier or keyword location for better diagnostic precision.
        /// </summary>
        /// <param name="node">The syntax node representing a member.</param>
        /// <returns>
        /// The location of the member's identifier or keyword.
        /// For methods, constructors, properties: returns the identifier location.
        /// For operators: returns the operator token location.
        /// For accessors: returns the keyword location (get/set/init).
        /// For other nodes: returns the node's location.
        /// </returns>
        /// <remarks>
        /// Using the identifier/keyword location instead of the entire node location
        /// provides better user experience by highlighting the most relevant part
        /// of the declaration in IDEs and diagnostic messages.
        /// </remarks>
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
