using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace DisposableAnalyzer.Helpers;

/// <summary>
/// Helper methods for IDisposable and IAsyncDisposable detection and analysis.
/// </summary>
public static class DisposableHelper
{
    private const string DisposableInterfaceName = "System.IDisposable";
    private const string AsyncDisposableInterfaceName = "System.IAsyncDisposable";
    private const string DisposeMethodName = "Dispose";
    private const string DisposeAsyncMethodName = "DisposeAsync";

    /// <summary>
    /// Determines if a type implements IDisposable.
    /// </summary>
    public static bool IsDisposableType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return false;

        return typeSymbol.AllInterfaces.Any(i =>
            i.ToDisplayString() == DisposableInterfaceName);
    }

    /// <summary>
    /// Determines if a type implements IAsyncDisposable.
    /// </summary>
    public static bool IsAsyncDisposableType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return false;

        return typeSymbol.AllInterfaces.Any(i =>
            i.ToDisplayString() == AsyncDisposableInterfaceName);
    }

    /// <summary>
    /// Determines if a type implements either IDisposable or IAsyncDisposable.
    /// </summary>
    public static bool IsAnyDisposableType(ITypeSymbol? typeSymbol)
    {
        return IsDisposableType(typeSymbol) || IsAsyncDisposableType(typeSymbol);
    }

    /// <summary>
    /// Gets the Dispose method from a type symbol.
    /// </summary>
    public static IMethodSymbol? GetDisposeMethod(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return null;

        return typeSymbol.GetMembers(DisposeMethodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Parameters.Length == 0);
    }

    /// <summary>
    /// Gets the DisposeAsync method from a type symbol.
    /// </summary>
    public static IMethodSymbol? GetDisposeAsyncMethod(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return null;

        return typeSymbol.GetMembers(DisposeAsyncMethodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Parameters.Length == 0);
    }

    /// <summary>
    /// Determines if an operation is a disposal call (Dispose() or DisposeAsync()).
    /// </summary>
    public static bool IsDisposalCall(IOperation? operation, out bool isAsync)
    {
        isAsync = false;

        // Handle direct invocation (stream.Dispose())
        if (operation is IInvocationOperation invocation)
        {
            var methodName = invocation.TargetMethod.Name;

            if (methodName == DisposeMethodName && invocation.TargetMethod.Parameters.Length == 0)
            {
                return true;
            }

            if (methodName == DisposeAsyncMethodName && invocation.TargetMethod.Parameters.Length == 0)
            {
                isAsync = true;
                return true;
            }
        }

        // Handle conditional access (stream?.Dispose())
        if (operation is IConditionalAccessOperation conditionalAccess)
        {
            if (conditionalAccess.WhenNotNull is IInvocationOperation conditionalInvocation)
            {
                var methodName = conditionalInvocation.TargetMethod.Name;

                if (methodName == DisposeMethodName && conditionalInvocation.TargetMethod.Parameters.Length == 0)
                {
                    return true;
                }

                if (methodName == DisposeAsyncMethodName && conditionalInvocation.TargetMethod.Parameters.Length == 0)
                {
                    isAsync = true;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a syntax node is within a using statement or using declaration.
    /// </summary>
    public static bool IsInUsingStatement(SyntaxNode? node)
    {
        if (node == null)
            return false;

        var current = node;
        while (current != null)
        {
            if (current is UsingStatementSyntax)
                return true;

            // Check for using declaration (C# 8+)
            if (current is LocalDeclarationStatementSyntax localDecl &&
                localDecl.UsingKeyword.Kind() == SyntaxKind.UsingKeyword)
                return true;

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Determines if a variable declaration has a using modifier.
    /// </summary>
    public static bool HasUsingModifier(LocalDeclarationStatementSyntax? declaration)
    {
        if (declaration == null)
            return false;

        return declaration.UsingKeyword.Kind() == SyntaxKind.UsingKeyword;
    }

    /// <summary>
    /// Checks if a type inherits from a base type that implements IDisposable.
    /// </summary>
    public static bool HasDisposableBase(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol?.BaseType == null)
            return false;

        return IsDisposableType(typeSymbol.BaseType);
    }

    /// <summary>
    /// Gets all disposable fields in a type.
    /// </summary>
    public static IEnumerable<IFieldSymbol> GetDisposableFields(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            yield break;

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IFieldSymbol field && IsAnyDisposableType(field.Type))
            {
                yield return field;
            }
        }
    }

    /// <summary>
    /// Determines if a method is a Dispose(bool) method.
    /// </summary>
    public static bool IsDisposeBoolMethod(IMethodSymbol? method)
    {
        if (method == null || method.Name != DisposeMethodName)
            return false;

        return method.Parameters.Length == 1 &&
               method.Parameters[0].Type.SpecialType == SpecialType.System_Boolean &&
               method.DeclaredAccessibility == Accessibility.Protected &&
               method.IsVirtual;
    }

    /// <summary>
    /// Checks if a type has a finalizer.
    /// </summary>
    public static bool HasFinalizer(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return false;

        return typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(m => m.MethodKind == MethodKind.Destructor);
    }

    /// <summary>
    /// Determines if an invocation is a GC.SuppressFinalize call.
    /// </summary>
    public static bool IsSuppressFinalizeCall(IInvocationOperation? invocation)
    {
        if (invocation == null)
            return false;

        return invocation.TargetMethod.ContainingType?.ToDisplayString() == "System.GC" &&
               invocation.TargetMethod.Name == "SuppressFinalize";
    }

    /// <summary>
    /// Determines if a variable escapes its scope (returned, assigned to field, etc.).
    /// </summary>
    public static bool DoesVariableEscape(ILocalSymbol local, SemanticModel semanticModel, SyntaxNode scope)
    {
        var references = scope.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(id => semanticModel.GetSymbolInfo(id).Symbol?.Equals(local, SymbolEqualityComparer.Default) == true);

        foreach (var reference in references)
        {
            var parent = reference.Parent;

            // Check for return statement
            if (parent is ReturnStatementSyntax)
                return true;

            // Check for assignment to field or property
            if (parent is AssignmentExpressionSyntax assignment &&
                assignment.Right == reference)
            {
                var leftSymbol = semanticModel.GetSymbolInfo(assignment.Left).Symbol;
                if (leftSymbol is IFieldSymbol || leftSymbol is IPropertySymbol)
                    return true;
            }

            // Check for passed as argument (might transfer ownership)
            if (parent is ArgumentSyntax)
                return true;
        }

        return false;
    }
}
