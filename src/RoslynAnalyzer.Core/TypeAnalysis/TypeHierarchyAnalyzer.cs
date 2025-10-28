using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace RoslynAnalyzer.Core.TypeAnalysis
{
    /// <summary>
    /// Provides type hierarchy analysis for Roslyn type symbols.
    /// </summary>
    /// <remarks>
    /// This analyzer provides generic methods for analyzing type relationships:
    /// - Inheritance checking (IsAssignableTo)
    /// - Type hierarchy traversal (GetTypeHierarchy)
    /// - Interface implementation checking (ImplementsInterface, ImplementsGenericInterface)
    ///
    /// These methods work with any types, not just exceptions, making them reusable
    /// across different analyzer scenarios.
    /// </remarks>
    public static class TypeHierarchyAnalyzer
    {
        /// <summary>
        /// Checks if a derived type is assignable to a base type (inheritance check).
        /// </summary>
        /// <param name="derivedType">The potentially derived type to check.</param>
        /// <param name="baseType">The potential base type to check against.</param>
        /// <returns>True if derivedType is assignable to baseType (same type or inherits from); otherwise, false.</returns>
        /// <remarks>
        /// This method checks:
        /// 1. Direct type equality using SymbolEqualityComparer
        /// 2. Inheritance chain traversal up to System.Object
        ///
        /// This method only checks class inheritance. For interface checking, use
        /// <see cref="ImplementsInterface"/> or <see cref="ImplementsGenericInterface"/>.
        ///
        /// Time complexity: O(d) where d is the depth of the inheritance hierarchy.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Check if ArgumentException is assignable to Exception
        /// var argEx = compilation.GetTypeByMetadataName("System.ArgumentException");
        /// var ex = compilation.GetTypeByMetadataName("System.Exception");
        /// bool result = TypeHierarchyAnalyzer.IsAssignableTo(argEx, ex); // true
        /// </code>
        /// </example>
        public static bool IsAssignableTo(
            ITypeSymbol derivedType,
            ITypeSymbol baseType)
        {
            if (derivedType == null || baseType == null)
                return false;

            // Direct match
            if (SymbolEqualityComparer.Default.Equals(derivedType, baseType))
                return true;

            // Walk up the inheritance chain
            var currentType = derivedType.BaseType;
            while (currentType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(currentType, baseType))
                    return true;
                currentType = currentType.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Gets all types in the inheritance hierarchy from most derived to System.Object.
        /// </summary>
        /// <param name="type">The type to analyze.</param>
        /// <returns>
        /// An enumerable of type symbols in the inheritance hierarchy, ordered from most derived
        /// (the input type) to least derived (System.Object or the root type).
        /// </returns>
        /// <remarks>
        /// The returned collection includes the input type as the first element.
        /// The hierarchy is traversed using BaseType until null is reached.
        ///
        /// This method is useful for:
        /// - Finding all potential catch clause matches for an exception
        /// - Analyzing type compatibility
        /// - Understanding type relationships
        ///
        /// Time complexity: O(d) where d is the depth of the inheritance hierarchy.
        /// Space complexity: O(d) for the returned list.
        /// </remarks>
        /// <example>
        /// <code>
        /// var argEx = compilation.GetTypeByMetadataName("System.ArgumentException");
        /// var hierarchy = TypeHierarchyAnalyzer.GetTypeHierarchy(argEx);
        /// // Returns: ArgumentException -> SystemException -> Exception -> Object
        /// </code>
        /// </example>
        public static IEnumerable<ITypeSymbol> GetTypeHierarchy(ITypeSymbol type)
        {
            var hierarchy = new List<ITypeSymbol>();
            var current = type;

            while (current != null)
            {
                hierarchy.Add(current);
                current = current.BaseType;
            }

            return hierarchy;
        }

        /// <summary>
        /// Checks if a type implements a specific interface (non-generic).
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="interfaceType">The interface type to check for.</param>
        /// <returns>True if the type implements the interface; otherwise, false.</returns>
        /// <remarks>
        /// This method checks:
        /// 1. Direct type equality (if the type itself is the interface)
        /// 2. All interfaces implemented by the type (using AllInterfaces property)
        ///
        /// For generic interfaces like IEnumerable&lt;T&gt;, use <see cref="ImplementsGenericInterface"/>
        /// which compares OriginalDefinition to handle generic type parameters correctly.
        ///
        /// Time complexity: O(i) where i is the number of implemented interfaces.
        /// </remarks>
        /// <example>
        /// <code>
        /// var listType = compilation.GetTypeByMetadataName("System.Collections.ArrayList");
        /// var ienumerable = compilation.GetTypeByMetadataName("System.Collections.IEnumerable");
        /// bool result = TypeHierarchyAnalyzer.ImplementsInterface(listType, ienumerable); // true
        /// </code>
        /// </example>
        public static bool ImplementsInterface(
            ITypeSymbol type,
            INamedTypeSymbol interfaceType)
        {
            if (type == null || interfaceType == null)
                return false;

            // Direct match (if type is the interface itself)
            if (SymbolEqualityComparer.Default.Equals(type, interfaceType))
                return true;

            // Check all implemented interfaces
            foreach (var implementedInterface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implementedInterface, interfaceType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a type implements a specific generic interface definition.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="genericInterfaceType">
        /// The generic interface definition to check for (e.g., IEnumerable&lt;&gt; unbound generic).
        /// </param>
        /// <returns>
        /// True if the type implements the generic interface (with any type arguments); otherwise, false.
        /// </returns>
        /// <remarks>
        /// This method compares the OriginalDefinition of types to match generic interfaces
        /// regardless of their type parameters. For example:
        /// - List&lt;int&gt; implements IEnumerable&lt;&gt;
        /// - List&lt;string&gt; implements IEnumerable&lt;&gt;
        ///
        /// Both would return true when checked against IEnumerable&lt;&gt;.
        ///
        /// The method checks:
        /// 1. If the type itself is a generic type matching the definition
        /// 2. All interfaces implemented by the type, comparing OriginalDefinition
        ///
        /// Time complexity: O(i) where i is the number of implemented interfaces.
        /// </remarks>
        /// <example>
        /// <code>
        /// var listOfInt = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")
        ///     .Construct(compilation.GetSpecialType(SpecialType.System_Int32));
        /// var ienumerableOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        /// bool result = TypeHierarchyAnalyzer.ImplementsGenericInterface(listOfInt, ienumerableOfT); // true
        /// </code>
        /// </example>
        public static bool ImplementsGenericInterface(
            ITypeSymbol type,
            INamedTypeSymbol genericInterfaceType)
        {
            if (type == null || genericInterfaceType == null)
                return false;

            // Check if type itself is the generic interface
            if (type is INamedTypeSymbol namedType)
            {
                if (SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, genericInterfaceType))
                    return true;
            }

            // Check all implemented interfaces
            foreach (var implementedInterface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, genericInterfaceType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the common base type between two types.
        /// </summary>
        /// <param name="type1">The first type.</param>
        /// <param name="type2">The second type.</param>
        /// <returns>
        /// The most derived common base type, or null if no common base type exists
        /// (which should only happen if either type is null).
        /// </returns>
        /// <remarks>
        /// This method finds the closest common ancestor in the type hierarchy.
        /// For example:
        /// - ArgumentException and InvalidOperationException -> SystemException
        /// - ArgumentNullException and ArgumentException -> ArgumentException
        /// - string and int -> object
        ///
        /// Algorithm:
        /// 1. Get the full hierarchy for type1
        /// 2. Walk up type2's hierarchy
        /// 3. Return the first type from type2's hierarchy found in type1's hierarchy
        ///
        /// Time complexity: O(d1 + d2) where d1 and d2 are hierarchy depths.
        /// Space complexity: O(d1) for storing type1's hierarchy.
        /// </remarks>
        /// <example>
        /// <code>
        /// var argEx = compilation.GetTypeByMetadataName("System.ArgumentException");
        /// var invalidOpEx = compilation.GetTypeByMetadataName("System.InvalidOperationException");
        /// var common = TypeHierarchyAnalyzer.FindCommonBaseType(argEx, invalidOpEx);
        /// // Returns: SystemException
        /// </code>
        /// </example>
        public static ITypeSymbol FindCommonBaseType(
            ITypeSymbol type1,
            ITypeSymbol type2)
        {
            if (type1 == null || type2 == null)
                return null;

            // Get type1's full hierarchy
            var type1Hierarchy = new HashSet<ITypeSymbol>(
                GetTypeHierarchy(type1),
                SymbolEqualityComparer.Default);

            // Walk type2's hierarchy and find first match
            var current = type2;
            while (current != null)
            {
                if (type1Hierarchy.Contains(current))
                    return current;
                current = current.BaseType;
            }

            return null;
        }
    }
}
