using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

namespace ThrowsAnalyzer.Performance;

/// <summary>
/// Provides caching for expensive semantic model queries related to exception types.
/// Reduces allocations and improves performance when analyzing large codebases.
/// </summary>
public static class ExceptionTypeCache
{
    private static readonly ConcurrentDictionary<string, INamedTypeSymbol?> _typeCache = new();
    private static readonly ConcurrentDictionary<string, int> _hierarchyDepthCache = new();

    /// <summary>
    /// Gets an exception type symbol with caching.
    /// </summary>
    /// <param name="compilation">The compilation context.</param>
    /// <param name="fullyQualifiedName">The fully qualified type name (e.g., "System.Exception").</param>
    /// <returns>The type symbol, or null if not found.</returns>
    public static INamedTypeSymbol? GetExceptionType(Compilation compilation, string fullyQualifiedName)
    {
        // Create cache key combining compilation name and type name
        string key = $"{compilation.AssemblyName}::{fullyQualifiedName}";

        return _typeCache.GetOrAdd(key, _ =>
            compilation.GetTypeByMetadataName(fullyQualifiedName));
    }

    /// <summary>
    /// Gets the inheritance depth of an exception type with caching.
    /// </summary>
    /// <param name="exceptionType">The exception type symbol.</param>
    /// <returns>The depth in the inheritance hierarchy (0 for System.Exception).</returns>
    public static int GetInheritanceDepth(ITypeSymbol exceptionType)
    {
        // Use fully qualified name as cache key
        string key = exceptionType.ToDisplayString();

        return _hierarchyDepthCache.GetOrAdd(key, _ =>
        {
            int depth = 0;
            var current = exceptionType.BaseType;

            while (current != null && current.Name != "Object")
            {
                depth++;
                current = current.BaseType;
            }

            return depth;
        });
    }

    /// <summary>
    /// Clears all cached data. Useful for testing or when analyzing different compilations.
    /// </summary>
    public static void Clear()
    {
        _typeCache.Clear();
        _hierarchyDepthCache.Clear();
    }

    /// <summary>
    /// Gets cache statistics for diagnostic purposes.
    /// </summary>
    public static CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            TypeCacheSize = _typeCache.Count,
            HierarchyDepthCacheSize = _hierarchyDepthCache.Count
        };
    }
}

/// <summary>
/// Statistics about cache usage.
/// </summary>
public class CacheStatistics
{
    public int TypeCacheSize { get; set; }
    public int HierarchyDepthCacheSize { get; set; }
}
