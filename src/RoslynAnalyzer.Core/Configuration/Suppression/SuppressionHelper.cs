using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalyzer.Core.Configuration.Suppression
{
    /// <summary>
    /// Helper class to check if diagnostics are suppressed via custom attributes.
    /// </summary>
    /// <remarks>
    /// This class provides utilities for checking if specific diagnostics are suppressed
    /// using custom attributes on members or types.
    ///
    /// Supports:
    /// - Per-diagnostic suppression (e.g., [SuppressAnalysis("RULE001")])
    /// - Wildcard suppression (e.g., [SuppressAnalysis("RULE*")])
    /// - Member-level suppression (method, property, constructor, etc.)
    /// - Type-level suppression (suppresses for all members in the type)
    ///
    /// Example attribute usage:
    /// <code>
    /// [SuppressAnalysis("RULE001", "RULE002")]
    /// public void MyMethod() { }
    ///
    /// [SuppressAnalysis("RULE*")]  // Suppress all rules starting with RULE
    /// public class MyClass { }
    /// </code>
    ///
    /// The attribute name can be customized to support different analyzers.
    /// </remarks>
    public static class SuppressionHelper
    {
        /// <summary>
        /// Checks if a diagnostic is suppressed for a given syntax node.
        /// </summary>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="node">The syntax node to check.</param>
        /// <param name="diagnosticId">The diagnostic ID to check (e.g., "RULE001").</param>
        /// <param name="attributeName">The suppression attribute name (e.g., "SuppressAnalysisAttribute").</param>
        /// <returns>True if the diagnostic is suppressed; otherwise, false.</returns>
        /// <remarks>
        /// This method checks for suppression in the following order:
        /// 1. On the member itself (method, property, constructor, etc.)
        /// 2. On the containing type (class, struct, interface, etc.)
        ///
        /// Supports both exact matches and wildcard patterns:
        /// - "RULE001" matches only "RULE001"
        /// - "RULE*" matches "RULE001", "RULE002", etc.
        ///
        /// Example:
        /// <code>
        /// if (SuppressionHelper.IsSuppressed(semanticModel, node, "RULE001", "SuppressAnalysisAttribute"))
        /// {
        ///     // Skip this diagnostic
        ///     return;
        /// }
        /// </code>
        /// </remarks>
        public static bool IsSuppressed(
            SemanticModel semanticModel,
            SyntaxNode node,
            string diagnosticId,
            string attributeName)
        {
            // Get the symbol for the member containing this node
            var memberSymbol = GetMemberSymbol(semanticModel, node);
            if (memberSymbol == null)
            {
                return false;
            }

            // Check for suppression on the member itself
            if (HasSuppressionAttribute(memberSymbol, diagnosticId, attributeName))
            {
                return true;
            }

            // Check for suppression on the containing type
            var containingType = memberSymbol.ContainingType;
            if (containingType != null && HasSuppressionAttribute(containingType, diagnosticId, attributeName))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a diagnostic is suppressed using multiple possible attribute names.
        /// </summary>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="node">The syntax node to check.</param>
        /// <param name="diagnosticId">The diagnostic ID to check.</param>
        /// <param name="attributeNames">Array of possible attribute names to check.</param>
        /// <returns>True if the diagnostic is suppressed by any of the attributes; otherwise, false.</returns>
        /// <remarks>
        /// This overload is useful when supporting multiple suppression attribute names,
        /// such as both the full name and a short form:
        ///
        /// <code>
        /// var suppressed = SuppressionHelper.IsSuppressed(
        ///     semanticModel,
        ///     node,
        ///     "RULE001",
        ///     new[] { "SuppressAnalysisAttribute", "SuppressAnalysis" });
        /// </code>
        /// </remarks>
        public static bool IsSuppressed(
            SemanticModel semanticModel,
            SyntaxNode node,
            string diagnosticId,
            params string[] attributeNames)
        {
            foreach (var attributeName in attributeNames)
            {
                if (IsSuppressed(semanticModel, node, diagnosticId, attributeName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the member symbol for a syntax node, walking up the syntax tree as needed.
        /// </summary>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="node">The syntax node to start from.</param>
        /// <returns>The member symbol, or null if not found.</returns>
        /// <remarks>
        /// This method walks up the syntax tree to find the containing member declaration.
        /// It supports all executable member types:
        /// - Methods, constructors, destructors
        /// - Properties and accessors
        /// - Operators and conversion operators
        /// - Local functions
        /// - Anonymous functions (walks up to find the containing member)
        ///
        /// For anonymous functions (lambdas, anonymous methods), it continues walking up
        /// the tree since they don't have their own symbols.
        /// </remarks>
        private static ISymbol GetMemberSymbol(SemanticModel semanticModel, SyntaxNode node)
        {
            // Try to get the symbol for various member types
            var current = node;
            while (current != null)
            {
                switch (current)
                {
                    case MethodDeclarationSyntax methodDecl:
                        return semanticModel.GetDeclaredSymbol(methodDecl);

                    case ConstructorDeclarationSyntax ctorDecl:
                        return semanticModel.GetDeclaredSymbol(ctorDecl);

                    case DestructorDeclarationSyntax dtorDecl:
                        return semanticModel.GetDeclaredSymbol(dtorDecl);

                    case PropertyDeclarationSyntax propertyDecl:
                        return semanticModel.GetDeclaredSymbol(propertyDecl);

                    case AccessorDeclarationSyntax accessorDecl:
                        return semanticModel.GetDeclaredSymbol(accessorDecl);

                    case OperatorDeclarationSyntax operatorDecl:
                        return semanticModel.GetDeclaredSymbol(operatorDecl);

                    case ConversionOperatorDeclarationSyntax conversionDecl:
                        return semanticModel.GetDeclaredSymbol(conversionDecl);

                    case LocalFunctionStatementSyntax localFunc:
                        return semanticModel.GetDeclaredSymbol(localFunc);

                    case AnonymousFunctionExpressionSyntax _:
                        // Anonymous functions don't have symbols, check parent
                        current = current.Parent;
                        continue;
                }

                current = current.Parent;
            }

            return null;
        }

        /// <summary>
        /// Checks if a symbol has a suppression attribute for the given diagnostic.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <param name="diagnosticId">The diagnostic ID to check.</param>
        /// <param name="attributeName">The suppression attribute name.</param>
        /// <returns>True if the symbol has a matching suppression attribute; otherwise, false.</returns>
        /// <remarks>
        /// This method examines all attributes on the symbol and checks if any match the suppression
        /// attribute name and contain the diagnostic ID in their constructor arguments.
        ///
        /// Supports wildcards: if the attribute contains "RULE*", it will match any diagnostic
        /// starting with "RULE".
        ///
        /// The attribute can be specified with or without the "Attribute" suffix:
        /// - "SuppressAnalysis" and "SuppressAnalysisAttribute" both match
        /// </remarks>
        private static bool HasSuppressionAttribute(ISymbol symbol, string diagnosticId, string attributeName)
        {
            var attributes = symbol.GetAttributes();

            // Normalize attribute name - support both "Name" and "NameAttribute" forms
            var shortName = attributeName.EndsWith("Attribute")
                ? attributeName.Substring(0, attributeName.Length - "Attribute".Length)
                : attributeName;
            var fullName = attributeName.EndsWith("Attribute")
                ? attributeName
                : attributeName + "Attribute";

            foreach (var attribute in attributes)
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass == null)
                {
                    continue;
                }

                // Check if this is our suppression attribute
                var name = attributeClass.Name;
                if (name != fullName && name != shortName)
                {
                    continue;
                }

                // Check if the diagnostic ID is in the suppression list
                if (attribute.ConstructorArguments.Length > 0)
                {
                    var rulesArg = attribute.ConstructorArguments[0];
                    if (rulesArg.Kind == TypedConstantKind.Array)
                    {
                        foreach (var rule in rulesArg.Values)
                        {
                            var ruleValue = rule.Value?.ToString();
                            if (ruleValue == null)
                            {
                                continue;
                            }

                            // Check for exact match
                            if (ruleValue == diagnosticId)
                            {
                                return true;
                            }

                            // Check for wildcard match (e.g., "RULE*" matches "RULE001")
                            if (ruleValue.EndsWith("*"))
                            {
                                var prefix = ruleValue.Substring(0, ruleValue.Length - 1);
                                if (diagnosticId.StartsWith(prefix))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
