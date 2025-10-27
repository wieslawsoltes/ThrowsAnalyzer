using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer.Configuration;

/// <summary>
/// Helper class to check if diagnostics are suppressed via attributes.
/// </summary>
public static class SuppressionHelper
{
    private const string SuppressThrowsAnalysisAttributeName = "SuppressThrowsAnalysisAttribute";
    private const string SuppressThrowsAnalysisShortName = "SuppressThrowsAnalysis";

    /// <summary>
    /// Checks if a diagnostic is suppressed for a given syntax node.
    /// </summary>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="node">The syntax node to check.</param>
    /// <param name="diagnosticId">The diagnostic ID to check (e.g., "THROWS001").</param>
    /// <returns>True if the diagnostic is suppressed; otherwise, false.</returns>
    public static bool IsSuppressed(
        SemanticModel semanticModel,
        SyntaxNode node,
        string diagnosticId)
    {
        // Get the symbol for the member containing this node
        var memberSymbol = GetMemberSymbol(semanticModel, node);
        if (memberSymbol == null)
        {
            return false;
        }

        // Check for suppression on the member itself
        if (HasSuppressionAttribute(memberSymbol, diagnosticId))
        {
            return true;
        }

        // Check for suppression on the containing type
        var containingType = memberSymbol.ContainingType;
        if (containingType != null && HasSuppressionAttribute(containingType, diagnosticId))
        {
            return true;
        }

        return false;
    }

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

    private static bool HasSuppressionAttribute(ISymbol symbol, string diagnosticId)
    {
        var attributes = symbol.GetAttributes();

        foreach (var attribute in attributes)
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
            {
                continue;
            }

            // Check if this is our suppression attribute
            var name = attributeClass.Name;
            if (name != SuppressThrowsAnalysisAttributeName &&
                name != SuppressThrowsAnalysisShortName)
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
                        if (ruleValue == diagnosticId || ruleValue == "THROWS*")
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}
