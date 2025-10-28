using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

namespace RoslynAnalyzer.Core.Performance.Caching
{
    /// <summary>
    /// Provides thread-safe caching for expensive symbol-related computations.
    /// </summary>
    /// <typeparam name="TValue">The type of computed value to cache.</typeparam>
    /// <remarks>
    /// This cache is useful for storing results of expensive computations on symbols,
    /// such as:
    /// - Inheritance depth calculations
    /// - Interface implementation checks
    /// - Type hierarchy traversals
    /// - Method signature analysis
    ///
    /// The cache uses symbol display strings as keys to ensure uniqueness across
    /// different symbols.
    ///
    /// Thread safety: All operations are thread-safe via ConcurrentDictionary.
    ///
    /// Usage example:
    /// <code>
    /// var cache = new SymbolCache&lt;int&gt;();
    /// var depth = cache.GetOrAdd(
    ///     exceptionType,
    ///     symbol => CalculateInheritanceDepth(symbol));
    /// </code>
    /// </remarks>
    public class SymbolCache<TValue>
    {
        private readonly ConcurrentDictionary<string, TValue> _cache = new();

        /// <summary>
        /// Gets a cached value or computes and caches it if not present.
        /// </summary>
        /// <param name="symbol">The symbol to cache a value for.</param>
        /// <param name="valueFactory">Factory function to create the value if not cached.</param>
        /// <returns>The cached or newly computed value.</returns>
        /// <remarks>
        /// Uses the symbol's display string as the cache key. This ensures that
        /// the same symbol (even if obtained through different queries) will use
        /// the same cached value.
        /// </remarks>
        public TValue GetOrAdd(ISymbol symbol, Func<ISymbol, TValue> valueFactory)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));

            var key = GetSymbolKey(symbol);
            return _cache.GetOrAdd(key, _ => valueFactory(symbol));
        }

        /// <summary>
        /// Gets a cached value or computes and caches it if not present (typed variant).
        /// </summary>
        /// <typeparam name="TSymbol">The specific symbol type.</typeparam>
        /// <param name="symbol">The symbol to cache a value for.</param>
        /// <param name="valueFactory">Factory function to create the value if not cached.</param>
        /// <returns>The cached or newly computed value.</returns>
        /// <remarks>
        /// This overload allows the value factory to work with strongly-typed symbols,
        /// making it easier to access symbol-specific properties without casting.
        ///
        /// Example:
        /// <code>
        /// var cache = new SymbolCache&lt;bool&gt;();
        /// var isAbstract = cache.GetOrAdd(
        ///     (INamedTypeSymbol)typeSymbol,
        ///     type => type.IsAbstract);
        /// </code>
        /// </remarks>
        public TValue GetOrAdd<TSymbol>(TSymbol symbol, Func<TSymbol, TValue> valueFactory)
            where TSymbol : ISymbol
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));

            var key = GetSymbolKey(symbol);
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
            return _cache.TryGetValue(key, out value);
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
        /// Clears all cached values.
        /// </summary>
        /// <remarks>
        /// Useful for testing or when you need to free memory.
        /// </remarks>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Gets the current number of cached items.
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Gets a unique string key for a symbol.
        /// </summary>
        /// <param name="symbol">The symbol to get a key for.</param>
        /// <returns>A string that uniquely identifies the symbol.</returns>
        /// <remarks>
        /// Uses ToDisplayString() to create a fully-qualified representation of the symbol.
        /// This ensures that symbols with the same name but different namespaces or
        /// type parameters are cached separately.
        /// </remarks>
        private static string GetSymbolKey(ISymbol symbol)
        {
            return symbol.ToDisplayString();
        }
    }
}
