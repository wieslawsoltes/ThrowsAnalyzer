using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace ThrowsAnalyzer.Configuration
{
    /// <summary>
    /// Reads analyzer configuration options from .editorconfig
    /// </summary>
    public static class AnalyzerOptionsReader
    {
        private const string OptionPrefix = "throws_analyzer";

        /// <summary>
        /// Checks if a specific member type should be analyzed
        /// </summary>
        public static bool IsMemberTypeEnabled(AnalyzerOptions options, SyntaxTree tree, string memberType)
        {
            var key = $"{OptionPrefix}_analyze_{memberType}";
            var provider = options.AnalyzerConfigOptionsProvider.GetOptions(tree);

            if (provider.TryGetValue(key, out var value))
            {
                return value.Trim().ToLowerInvariant() == "true";
            }

            // Default: all member types enabled
            return true;
        }

        /// <summary>
        /// Gets all enabled member types from configuration
        /// </summary>
        public static HashSet<string> GetEnabledMemberTypes(AnalyzerOptions options, SyntaxTree tree)
        {
            var memberTypes = new[]
            {
                "methods",
                "constructors",
                "destructors",
                "operators",
                "conversion_operators",
                "properties",
                "accessors",
                "local_functions",
                "lambdas",
                "anonymous_methods"
            };

            var enabled = new HashSet<string>();
            foreach (var memberType in memberTypes)
            {
                if (IsMemberTypeEnabled(options, tree, memberType))
                {
                    enabled.Add(memberType);
                }
            }

            return enabled;
        }

        /// <summary>
        /// Maps SyntaxKind to member type configuration key
        /// </summary>
        public static string GetMemberTypeKey(Microsoft.CodeAnalysis.CSharp.SyntaxKind kind)
        {
            return kind switch
            {
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration => "methods",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConstructorDeclaration => "constructors",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.DestructorDeclaration => "destructors",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.OperatorDeclaration => "operators",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConversionOperatorDeclaration => "conversion_operators",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration => "properties",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.GetAccessorDeclaration => "accessors",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SetAccessorDeclaration => "accessors",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.InitAccessorDeclaration => "accessors",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.AddAccessorDeclaration => "accessors",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.RemoveAccessorDeclaration => "accessors",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.LocalFunctionStatement => "local_functions",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleLambdaExpression => "lambdas",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ParenthesizedLambdaExpression => "lambdas",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.AnonymousMethodExpression => "anonymous_methods",
                _ => "unknown"
            };
        }
    }
}
