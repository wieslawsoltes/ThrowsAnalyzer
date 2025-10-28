using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using RoslynAnalyzer.Core.TypeAnalysis;

namespace RoslynAnalyzer.Core.Extensions
{
    /// <summary>
    /// Extension methods for ITypeSymbol providing convenient access to type analysis operations.
    /// </summary>
    /// <remarks>
    /// These extension methods provide a fluent API over the static methods in
    /// <see cref="TypeHierarchyAnalyzer"/>, making type analysis code more readable:
    ///
    /// Instead of: TypeHierarchyAnalyzer.IsAssignableTo(derivedType, baseType)
    /// Use: derivedType.IsAssignableTo(baseType)
    ///
    /// All methods delegate to TypeHierarchyAnalyzer for actual implementation.
    /// </remarks>
    public static class TypeSymbolExtensions
    {
        /// <summary>
        /// Checks if this type is assignable to the specified base type (inheritance check).
        /// </summary>
        /// <param name="derivedType">The type to check (this).</param>
        /// <param name="baseType">The potential base type to check against.</param>
        /// <returns>True if this type is assignable to baseType; otherwise, false.</returns>
        /// <remarks>
        /// This is a convenience extension method that calls
        /// <see cref="TypeHierarchyAnalyzer.IsAssignableTo"/>.
        ///
        /// Example usage:
        /// <code>
        /// if (exceptionType.IsAssignableTo(systemException))
        /// {
        ///     // exceptionType inherits from System.Exception
        /// }
        /// </code>
        /// </remarks>
        public static bool IsAssignableTo(this ITypeSymbol derivedType, ITypeSymbol baseType)
        {
            return TypeHierarchyAnalyzer.IsAssignableTo(derivedType, baseType);
        }

        /// <summary>
        /// Gets the full type hierarchy for this type, from most derived to System.Object.
        /// </summary>
        /// <param name="type">The type to analyze (this).</param>
        /// <returns>An enumerable of type symbols in the inheritance hierarchy.</returns>
        /// <remarks>
        /// This is a convenience extension method that calls
        /// <see cref="TypeHierarchyAnalyzer.GetTypeHierarchy"/>.
        ///
        /// Example usage:
        /// <code>
        /// foreach (var baseType in exceptionType.GetTypeHierarchy())
        /// {
        ///     Console.WriteLine(baseType.Name);
        /// }
        /// </code>
        /// </remarks>
        public static IEnumerable<ITypeSymbol> GetTypeHierarchy(this ITypeSymbol type)
        {
            return TypeHierarchyAnalyzer.GetTypeHierarchy(type);
        }

        /// <summary>
        /// Checks if this type implements a specific interface (non-generic).
        /// </summary>
        /// <param name="type">The type to check (this).</param>
        /// <param name="interfaceType">The interface type to check for.</param>
        /// <returns>True if this type implements the interface; otherwise, false.</returns>
        /// <remarks>
        /// This is a convenience extension method that calls
        /// <see cref="TypeHierarchyAnalyzer.ImplementsInterface"/>.
        ///
        /// Example usage:
        /// <code>
        /// if (collectionType.ImplementsInterface(ienumerableInterface))
        /// {
        ///     // collectionType implements IEnumerable
        /// }
        /// </code>
        /// </remarks>
        public static bool ImplementsInterface(this ITypeSymbol type, INamedTypeSymbol interfaceType)
        {
            return TypeHierarchyAnalyzer.ImplementsInterface(type, interfaceType);
        }

        /// <summary>
        /// Checks if this type implements a specific generic interface definition.
        /// </summary>
        /// <param name="type">The type to check (this).</param>
        /// <param name="genericInterfaceType">
        /// The generic interface definition to check for (e.g., IEnumerable&lt;&gt;).
        /// </param>
        /// <returns>True if this type implements the generic interface; otherwise, false.</returns>
        /// <remarks>
        /// This is a convenience extension method that calls
        /// <see cref="TypeHierarchyAnalyzer.ImplementsGenericInterface"/>.
        ///
        /// Example usage:
        /// <code>
        /// var ienumerableOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        /// if (listType.ImplementsGenericInterface(ienumerableOfT))
        /// {
        ///     // listType implements IEnumerable&lt;T&gt;
        /// }
        /// </code>
        /// </remarks>
        public static bool ImplementsGenericInterface(this ITypeSymbol type, INamedTypeSymbol genericInterfaceType)
        {
            return TypeHierarchyAnalyzer.ImplementsGenericInterface(type, genericInterfaceType);
        }

        /// <summary>
        /// Finds the common base type between this type and another type.
        /// </summary>
        /// <param name="type1">The first type (this).</param>
        /// <param name="type2">The second type.</param>
        /// <returns>The most derived common base type, or null if none exists.</returns>
        /// <remarks>
        /// This is a convenience extension method that calls
        /// <see cref="TypeHierarchyAnalyzer.FindCommonBaseType"/>.
        ///
        /// Example usage:
        /// <code>
        /// var commonBase = argumentException.FindCommonBaseType(invalidOperationException);
        /// // Returns: SystemException
        /// </code>
        /// </remarks>
        public static ITypeSymbol FindCommonBaseType(this ITypeSymbol type1, ITypeSymbol type2)
        {
            return TypeHierarchyAnalyzer.FindCommonBaseType(type1, type2);
        }

        /// <summary>
        /// Checks if this type is a specific type by its fully qualified metadata name.
        /// </summary>
        /// <param name="type">The type to check (this).</param>
        /// <param name="metadataName">
        /// The fully qualified metadata name to check (e.g., "System.String", "System.Int32").
        /// </param>
        /// <returns>True if this type matches the specified metadata name; otherwise, false.</returns>
        /// <remarks>
        /// This is a convenience method for checking if a type matches a known type by name.
        /// It's useful when you don't have access to a Compilation to get the type symbol directly.
        ///
        /// Example usage:
        /// <code>
        /// if (parameterType.IsType("System.String"))
        /// {
        ///     // parameterType is string
        /// }
        /// </code>
        /// </remarks>
        public static bool IsType(this ITypeSymbol type, string metadataName)
        {
            if (type == null || string.IsNullOrEmpty(metadataName))
                return false;

            // Use metadata name from the type symbol
            var typeMetadataName = type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

            // Check exact match
            return typeMetadataName == metadataName ||
                   type.MetadataName == metadataName ||
                   type.ToDisplayString() == metadataName;
        }

        /// <summary>
        /// Checks if this type is a value type (struct or enum).
        /// </summary>
        /// <param name="type">The type to check (this).</param>
        /// <returns>True if this type is a value type; otherwise, false.</returns>
        /// <remarks>
        /// This is a convenience wrapper around the IsValueType property that
        /// handles null checks.
        ///
        /// Example usage:
        /// <code>
        /// if (returnType.IsValueType())
        /// {
        ///     // returnType is a struct or enum
        /// }
        /// </code>
        /// </remarks>
        public static bool IsValueType(this ITypeSymbol type)
        {
            return type?.IsValueType == true;
        }

        /// <summary>
        /// Checks if this type is a reference type (class, interface, delegate, or array).
        /// </summary>
        /// <param name="type">The type to check (this).</param>
        /// <returns>True if this type is a reference type; otherwise, false.</returns>
        /// <remarks>
        /// This is a convenience wrapper around the IsReferenceType property that
        /// handles null checks.
        ///
        /// Example usage:
        /// <code>
        /// if (returnType.IsReferenceType())
        /// {
        ///     // returnType is a class, interface, delegate, or array
        /// }
        /// </code>
        /// </remarks>
        public static bool IsReferenceType(this ITypeSymbol type)
        {
            return type?.IsReferenceType == true;
        }

        /// <summary>
        /// Checks if this type is nullable (either Nullable&lt;T&gt; or a reference type with nullable annotation).
        /// </summary>
        /// <param name="type">The type to check (this).</param>
        /// <returns>True if this type is nullable; otherwise, false.</returns>
        /// <remarks>
        /// This method checks two cases:
        /// 1. Value types wrapped in Nullable&lt;T&gt; (e.g., int?)
        /// 2. Reference types with NullableAnnotation.Annotated (e.g., string? in C# 8.0+)
        ///
        /// Example usage:
        /// <code>
        /// if (parameterType.IsNullable())
        /// {
        ///     // parameterType is nullable
        /// }
        /// </code>
        /// </remarks>
        public static bool IsNullable(this ITypeSymbol type)
        {
            if (type == null)
                return false;

            // Check for Nullable<T>
            if (type is INamedTypeSymbol namedType)
            {
                if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    return true;
            }

            // Check for nullable reference types (C# 8.0+)
            return type.NullableAnnotation == NullableAnnotation.Annotated;
        }
    }
}
