using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace RoslynAnalyzer.Core.Performance.Caching
{
    /// <summary>
    /// Symbol cache with statistics tracking for performance monitoring.
    /// </summary>
    /// <typeparam name="TValue">The type of computed value to cache.</typeparam>
    /// <remarks>
    /// This is an enhanced version of <see cref="SymbolCache{TValue}"/> that
    /// tracks cache hits and misses for performance analysis.
    ///
    /// Use this when you need to:
    /// - Monitor how effectively symbol computations are being cached
    /// - Identify frequently computed vs rarely used values
    /// - Tune your caching strategy based on actual usage patterns
    ///
    /// Example usage:
    /// <code>
    /// var cache = new SymbolCacheWithStatistics&lt;int&gt;();
    /// var depth = cache.GetOrAdd(exceptionType,
    ///     symbol => CalculateInheritanceDepth(symbol));
    ///
    /// var stats = cache.GetStatistics();
    /// if (stats.HitRatio &lt; 0.5)
    /// {
    ///     // Cache isn't very effective, might need different strategy
    /// }
    /// </code>
    /// </remarks>
    public class SymbolCacheWithStatistics<TValue> : ICacheWithStatistics
    {
        private readonly ConcurrentDictionary<string, TValue> _cache = new();
        private long _hits;
        private long _misses;

        /// <summary>
        /// Gets a cached value or computes and caches it if not present.
        /// </summary>
        /// <param name="symbol">The symbol to cache a value for.</param>
        /// <param name="valueFactory">Factory function to create the value if not cached.</param>
        /// <returns>The cached or newly computed value.</returns>
        public TValue GetOrAdd(ISymbol symbol, Func<ISymbol, TValue> valueFactory)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));

            var key = GetSymbolKey(symbol);

            // Check if value exists (for hit/miss tracking)
            if (_cache.TryGetValue(key, out var existingValue))
            {
                Interlocked.Increment(ref _hits);
                return existingValue;
            }

            // Value not in cache - this is a miss
            Interlocked.Increment(ref _misses);

            // Compute and add the value
            return _cache.GetOrAdd(key, _ => valueFactory(symbol));
        }

        /// <summary>
        /// Gets a cached value or computes and caches it if not present (typed variant).
        /// </summary>
        /// <typeparam name="TSymbol">The specific symbol type.</typeparam>
        /// <param name="symbol">The symbol to cache a value for.</param>
        /// <param name="valueFactory">Factory function to create the value if not cached.</param>
        /// <returns>The cached or newly computed value.</returns>
        public TValue GetOrAdd<TSymbol>(TSymbol symbol, Func<TSymbol, TValue> valueFactory)
            where TSymbol : ISymbol
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));

            var key = GetSymbolKey(symbol);

            // Check if value exists (for hit/miss tracking)
            if (_cache.TryGetValue(key, out var existingValue))
            {
                Interlocked.Increment(ref _hits);
                return existingValue;
            }

            // Value not in cache - this is a miss
            Interlocked.Increment(ref _misses);

            // Compute and add the value
            return _cache.GetOrAdd(key, _ => valueFactory(symbol));
        }

        /// <summary>
        /// Tries to get a cached value.
        /// </summary>
        /// <param name="symbol">The symbol to look up.</param>
        /// <param name="value">The cached value if found.</param>
        /// <returns>True if the value was found in cache; otherwise, false.</returns>
        public bool TryGetValue(ISymbol symbol, out TValue value)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            var key = GetSymbolKey(symbol);
            var found = _cache.TryGetValue(key, out value);

            if (found)
                Interlocked.Increment(ref _hits);
            else
                Interlocked.Increment(ref _misses);

            return found;
        }

        /// <summary>
        /// Adds or updates a value in the cache.
        /// </summary>
        /// <param name="symbol">The symbol to cache a value for.</param>
        /// <param name="value">The value to cache.</param>
        /// <returns>The value that was added or updated.</returns>
        public TValue AddOrUpdate(ISymbol symbol, TValue value)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            var key = GetSymbolKey(symbol);
            return _cache.AddOrUpdate(key, value, (_, __) => value);
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
        /// Gets a unique string key for a symbol.
        /// </summary>
        private static string GetSymbolKey(ISymbol symbol)
        {
            return symbol.ToDisplayString();
        }
    }
}
