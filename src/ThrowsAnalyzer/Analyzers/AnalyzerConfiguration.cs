using Microsoft.CodeAnalysis.CSharp;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Configuration for Roslyn analyzers.
    /// Centralizes common settings like which syntax kinds to analyze.
    /// </summary>
    public static class AnalyzerConfiguration
    {
        /// <summary>
        /// All executable member syntax kinds that analyzers should register for.
        /// Includes methods, constructors, properties, operators, accessors, local functions, and lambdas.
        /// </summary>
        public static readonly SyntaxKind[] ExecutableMemberSyntaxKinds = new[]
        {
            SyntaxKind.MethodDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.DestructorDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.ConversionOperatorDeclaration,
            SyntaxKind.PropertyDeclaration,
            SyntaxKind.GetAccessorDeclaration,
            SyntaxKind.SetAccessorDeclaration,
            SyntaxKind.InitAccessorDeclaration,
            SyntaxKind.AddAccessorDeclaration,
            SyntaxKind.RemoveAccessorDeclaration,
            SyntaxKind.LocalFunctionStatement,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.AnonymousMethodExpression
        };
    }
}
