using Microsoft.CodeAnalysis.CSharp;

namespace RoslynAnalyzer.Core.Configuration
{
    /// <summary>
    /// Configuration for Roslyn analyzers.
    /// Centralizes common settings like which syntax kinds to analyze.
    /// </summary>
    /// <remarks>
    /// This class provides a central location for analyzer configuration that is commonly
    /// used across different analyzer implementations:
    ///
    /// - <see cref="ExecutableMemberSyntaxKinds"/>: All syntax kinds representing executable members
    ///   (methods, constructors, properties, operators, accessors, local functions, lambdas)
    ///
    /// Analyzers typically use this when registering syntax node actions:
    /// <code>
    /// context.RegisterSyntaxNodeAction(
    ///     AnalyzeNode,
    ///     AnalyzerConfiguration.ExecutableMemberSyntaxKinds);
    /// </code>
    ///
    /// This ensures consistent analysis across all executable member types without duplicating
    /// the list of syntax kinds in every analyzer.
    /// </remarks>
    public static class AnalyzerConfiguration
    {
        /// <summary>
        /// All executable member syntax kinds that analyzers should register for.
        /// </summary>
        /// <remarks>
        /// This array includes all C# syntax kinds that represent executable code:
        ///
        /// <list type="bullet">
        /// <item><description>MethodDeclaration - Regular methods</description></item>
        /// <item><description>ConstructorDeclaration - Instance and static constructors</description></item>
        /// <item><description>DestructorDeclaration - Finalizers</description></item>
        /// <item><description>OperatorDeclaration - Operator overloads (e.g., +, -, ==)</description></item>
        /// <item><description>ConversionOperatorDeclaration - Implicit and explicit conversions</description></item>
        /// <item><description>PropertyDeclaration - Property declarations (for expression-bodied properties)</description></item>
        /// <item><description>GetAccessorDeclaration - Property getters</description></item>
        /// <item><description>SetAccessorDeclaration - Property setters</description></item>
        /// <item><description>InitAccessorDeclaration - Init-only setters (C# 9.0+)</description></item>
        /// <item><description>AddAccessorDeclaration - Event add accessors</description></item>
        /// <item><description>RemoveAccessorDeclaration - Event remove accessors</description></item>
        /// <item><description>LocalFunctionStatement - Local functions (C# 7.0+)</description></item>
        /// <item><description>SimpleLambdaExpression - Lambda expressions with single parameter (x => x * 2)</description></item>
        /// <item><description>ParenthesizedLambdaExpression - Lambda expressions with parameter list ((x, y) => x + y)</description></item>
        /// <item><description>AnonymousMethodExpression - Anonymous methods (delegate { })</description></item>
        /// </list>
        ///
        /// Use this array when registering syntax node actions to analyze all executable members:
        /// <code>
        /// context.RegisterSyntaxNodeAction(
        ///     AnalyzeExecutableMember,
        ///     AnalyzerConfiguration.ExecutableMemberSyntaxKinds);
        /// </code>
        /// </remarks>
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
