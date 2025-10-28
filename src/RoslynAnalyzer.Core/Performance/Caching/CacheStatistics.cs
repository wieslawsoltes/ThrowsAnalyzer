using System;

namespace RoslynAnalyzer.Core.Performance.Caching
{
    /// <summary>
    /// Statistics about cache usage and performance.
    /// </summary>
    /// <remarks>
    /// This class provides insights into cache behavior:
    /// - Size: How many items are cached
    /// - Hit/Miss counts: How effective the cache is
    /// - Hit ratio: Percentage of lookups that find cached values
    ///
    /// Use these statistics to:
    /// - Monitor cache effectiveness
    /// - Tune cache strategies
    /// - Identify performance bottlenecks
    /// - Decide when to clear or resize caches
    ///
    /// Example usage:
    /// <code>
    /// var stats = cacheWithStats.GetStatistics();
    /// Console.WriteLine($"Cache hit ratio: {stats.HitRatio:P2}");
    /// Console.WriteLine($"Total lookups: {stats.TotalLookups}");
    /// </code>
    /// </remarks>
    public class CacheStatistics
    {
        /// <summary>
        /// Gets or sets the current number of items in the cache.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the number of cache hits (successful lookups).
        /// </summary>
        /// <remarks>
        /// A cache hit occurs when a requested value is found in the cache,
        /// avoiding the need to recompute it.
        /// </remarks>
        public long Hits { get; set; }

        /// <summary>
        /// Gets or sets the number of cache misses (unsuccessful lookups).
        /// </summary>
        /// <remarks>
        /// A cache miss occurs when a requested value is not in the cache,
        /// requiring it to be computed and added to the cache.
        /// </remarks>
        public long Misses { get; set; }

        /// <summary>
        /// Gets the total number of cache lookups (hits + misses).
        /// </summary>
        public long TotalLookups => Hits + Misses;

        /// <summary>
        /// Gets the cache hit ratio (hits / total lookups).
        /// </summary>
        /// <remarks>
        /// Returns a value between 0.0 and 1.0 indicating the percentage
        /// of lookups that found cached values.
        ///
        /// - 1.0 (100%): All lookups found cached values
        /// - 0.5 (50%): Half of lookups found cached values
        /// - 0.0 (0%): No lookups found cached values
        ///
        /// Returns 0 if no lookups have occurred yet.
        ///
        /// A higher hit ratio indicates better cache effectiveness.
        /// </remarks>
        public double HitRatio
        {
            get
            {
                if (TotalLookups == 0)
                    return 0.0;

                return (double)Hits / TotalLookups;
            }
        }

        /// <summary>
        /// Returns a formatted string representation of the cache statistics.
        /// </summary>
        /// <returns>A human-readable summary of cache performance.</returns>
        public override string ToString()
        {
            return $"Cache Statistics: Size={Size}, Hits={Hits}, Misses={Misses}, Hit Ratio={HitRatio:P2}";
        }
    }

    /// <summary>
    /// Interface for caches that support statistics tracking.
    /// </summary>
    /// <remarks>
    /// Implement this interface in cache classes that want to expose
    /// performance metrics.
    ///
    /// Example implementation:
    /// <code>
    /// public class MyCache&lt;T&gt; : ICacheWithStatistics
    /// {
    ///     private long _hits;
    ///     private long _misses;
    ///
    ///     public CacheStatistics GetStatistics()
    ///     {
    ///         return new CacheStatistics
    ///         {
    ///             Size = _cache.Count,
    ///             Hits = _hits,
    ///             Misses = _misses
    ///         };
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public interface ICacheWithStatistics
    {
        /// <summary>
        /// Gets the current cache statistics.
        /// </summary>
        /// <returns>A snapshot of current cache performance metrics.</returns>
        CacheStatistics GetStatistics();

        /// <summary>
        /// Resets the cache statistics counters.
        /// </summary>
        /// <remarks>
        /// This does not clear the cache contents, only the hit/miss counters.
        /// Useful for measuring cache performance over specific time periods.
        /// </remarks>
        void ResetStatistics();
    }
}
