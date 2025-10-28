using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

namespace RoslynAnalyzer.Core.Performance.Caching
{
    /// <summary>
    /// Provides thread-safe caching for expensive compilation-related queries.
    /// </summary>
    /// <typeparam name="TValue">The type of value to cache.</typeparam>
    /// <remarks>
    /// This cache is useful for storing results of expensive semantic model queries
    /// that need to be looked up multiple times during analysis. Examples include:
    /// - Type symbols obtained via GetTypeByMetadataName
    /// - Computed properties of types (hierarchy depth, interface implementations)
    /// - Analysis results that are expensive to compute
    ///
    /// The cache uses compilation assembly name as part of the key to ensure correct
    /// isolation between different compilations.
    ///
    /// Thread safety: All operations are thread-safe via ConcurrentDictionary.
    ///
    /// Usage example:
    /// <code>
    /// var cache = new CompilationCache&lt;INamedTypeSymbol?&gt;();
    /// var exceptionType = cache.GetOrAdd(
    ///     compilation,
    ///     "System.Exception",
    ///     (comp, name) => comp.GetTypeByMetadataName(name));
    /// </code>
    /// </remarks>
    public class CompilationCache<TValue>
    {
        private readonly ConcurrentDictionary<string, TValue> _cache = new();

        /// <summary>
        /// Gets a cached value or computes and caches it if not present.
        /// </summary>
        /// <param name="compilation">The compilation context.</param>
        /// <param name="key">The cache key (will be combined with compilation name).</param>
        /// <param name="valueFactory">Factory function to create the value if not cached.</param>
        /// <returns>The cached or newly computed value.</returns>
        /// <remarks>
        /// The actual cache key is constructed as: "{compilation.AssemblyName}::{key}"
        /// This ensures that values from different compilations don't collide.
        ///
        /// The valueFactory receives both the compilation and the original key,
        /// allowing flexible value computation strategies.
        /// </remarks>
        public TValue GetOrAdd(
            Compilation compilation,
            string key,
            Func<Compilation, string, TValue> valueFactory)
        {
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));

            var compositeKey = CreateCompositeKey(compilation, key);

            return _cache.GetOrAdd(compositeKey, _ => valueFactory(compilation, key));
        }

        /// <summary>
        /// Tries to get a cached value.
        /// </summary>
        /// <param name="compilation">The compilation context.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cached value if found.</param>
        /// <returns>True if the value was found in cache; otherwise, false.</returns>
        public bool TryGetValue(Compilation compilation, string key, out TValue value)
        {
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var compositeKey = CreateCompositeKey(compilation, key);
            return _cache.TryGetValue(compositeKey, out value);
        }

        /// <summary>
        /// Adds or updates a value in the cache.
        /// </summary>
        /// <param name="compilation">The compilation context.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <returns>The value that was added or updated.</returns>
        public TValue AddOrUpdate(Compilation compilation, string key, TValue value)
        {
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var compositeKey = CreateCompositeKey(compilation, key);
            return _cache.AddOrUpdate(compositeKey, value, (_, __) => value);
        }

        /// <summary>
        /// Clears all cached values.
        /// </summary>
        /// <remarks>
        /// Useful for testing or when you need to free memory.
        /// In production analyzers, clearing the cache may cause performance degradation
        /// as values will need to be recomputed.
        /// </remarks>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Clears cached values for a specific compilation.
        /// </summary>
        /// <param name="compilation">The compilation whose cached values should be removed.</param>
        /// <remarks>
        /// This is more selective than Clear() - it only removes entries for the specified
        /// compilation, allowing other compilations' cached values to remain.
        /// </remarks>
        public void ClearCompilation(Compilation compilation)
        {
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));

            var prefix = compilation.AssemblyName + "::";

            // Find and remove all keys for this compilation
            foreach (var key in _cache.Keys)
            {
                if (key.StartsWith(prefix))
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// Gets the current number of cached items.
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Creates a composite cache key combining compilation and key.
        /// </summary>
        private static string CreateCompositeKey(Compilation compilation, string key)
        {
            return $"{compilation.AssemblyName}::{key}";
        }
    }
}
