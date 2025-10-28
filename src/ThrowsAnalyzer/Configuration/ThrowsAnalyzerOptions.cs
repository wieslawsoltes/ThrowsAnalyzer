using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynAnalyzer.Core.Configuration.Options;

namespace ThrowsAnalyzer.Configuration
{
    /// <summary>
    /// ThrowsAnalyzer-specific configuration helpers.
    /// </summary>
    public static class ThrowsAnalyzerOptions
    {
        private const string Prefix = "throws_analyzer";

        public static bool IsAnalyzerEnabled(AnalyzerOptions options, SyntaxTree tree, string analyzerName)
        {
            return AnalyzerOptionsReader.IsAnalyzerEnabled(options, tree, Prefix, analyzerName);
        }

        public static string GetMemberTypeKey(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.MethodDeclaration => "methods",
                SyntaxKind.ConstructorDeclaration => "constructors",
                SyntaxKind.DestructorDeclaration => "destructors",
                SyntaxKind.OperatorDeclaration => "operators",
                SyntaxKind.ConversionOperatorDeclaration => "conversion_operators",
                SyntaxKind.PropertyDeclaration => "properties",
                SyntaxKind.GetAccessorDeclaration => "accessors",
                SyntaxKind.SetAccessorDeclaration => "accessors",
                SyntaxKind.InitAccessorDeclaration => "accessors",
                SyntaxKind.AddAccessorDeclaration => "accessors",
                SyntaxKind.RemoveAccessorDeclaration => "accessors",
                SyntaxKind.LocalFunctionStatement => "local_functions",
                SyntaxKind.SimpleLambdaExpression => "lambdas",
                SyntaxKind.ParenthesizedLambdaExpression => "lambdas",
                SyntaxKind.AnonymousMethodExpression => "anonymous_methods",
                _ => "unknown"
            };
        }

        public static bool IsMemberTypeEnabled(AnalyzerOptions options, SyntaxTree tree, string memberTypeKey)
        {
            return AnalyzerOptionsReader.IsFeatureEnabled(options, tree, Prefix, $"analyze_{memberTypeKey}", defaultValue: true);
        }
    }
}
