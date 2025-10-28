using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace RoslynAnalyzer.Core.Performance.Caching
{
    /// <summary>
    /// Compilation cache with statistics tracking for performance monitoring.
    /// </summary>
    /// <typeparam name="TValue">The type of value to cache.</typeparam>
    /// <remarks>
    /// This is an enhanced version of <see cref="CompilationCache{TValue}"/> that
    /// tracks cache hits and misses for performance analysis.
    ///
    /// Use this when you need to:
    /// - Monitor cache effectiveness
    /// - Tune cache strategies
    /// - Identify performance bottlenecks
    ///
    /// The statistics tracking adds minimal overhead (atomic operations only).
    ///
    /// Example usage:
    /// <code>
    /// var cache = new CompilationCacheWithStatistics&lt;INamedTypeSymbol?&gt;();
    /// var type = cache.GetOrAdd(compilation, "System.Exception",
    ///     (comp, name) => comp.GetTypeByMetadataName(name));
    ///
    /// var stats = cache.GetStatistics();
    /// Console.WriteLine($"Hit ratio: {stats.HitRatio:P2}");
    /// </code>
    /// </remarks>
    public class CompilationCacheWithStatistics<TValue> : ICacheWithStatistics
    {
        private readonly ConcurrentDictionary<string, TValue> _cache = new();
        private long _hits;
        private long _misses;

        /// <summary>
        /// Gets a cached value or computes and caches it if not present.
        /// </summary>
        /// <param name="compilation">The compilation context.</param>
        /// <param name="key">The cache key (will be combined with compilation name).</param>
        /// <param name="valueFactory">Factory function to create the value if not cached.</param>
        /// <returns>The cached or newly computed value.</returns>
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

            // Check if value exists (for hit/miss tracking)
            if (_cache.TryGetValue(compositeKey, out var existingValue))
            {
                Interlocked.Increment(ref _hits);
                return existingValue;
            }

            // Value not in cache - this is a miss
            Interlocked.Increment(ref _misses);

            // Compute and add the value
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
            var found = _cache.TryGetValue(compositeKey, out value);

            if (found)
                Interlocked.Increment(ref _hits);
            else
                Interlocked.Increment(ref _misses);

            return found;
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
        /// Clears all cached values and resets statistics.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            ResetStatistics();
        }

        /// <summary>
        /// Clears cached values for a specific compilation.
        /// </summary>
        /// <param name="compilation">The compilation whose cached values should be removed.</param>
        public void ClearCompilation(Compilation compilation)
        {
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));

            var prefix = compilation.AssemblyName + "::";

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
        /// Gets the current cache statistics.
        /// </summary>
        /// <returns>A snapshot of current cache performance metrics.</returns>
        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                Size = _cache.Count,
                Hits = Interlocked.Read(ref _hits),
                Misses = Interlocked.Read(ref _misses)
            };
        }

        /// <summary>
        /// Resets the cache statistics counters.
        /// </summary>
        /// <remarks>
        /// This does not clear the cache contents, only the hit/miss counters.
        /// </remarks>
        public void ResetStatistics()
        {
            Interlocked.Exchange(ref _hits, 0);
            Interlocked.Exchange(ref _misses, 0);
        }

        /// <summary>
        /// Creates a composite cache key combining compilation and key.
        /// </summary>
        private static string CreateCompositeKey(Compilation compilation, string key)
        {
            return $"{compilation.AssemblyName}::{key}";
        }
    }
}
