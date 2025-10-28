using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using RoslynAnalyzer.Core.Analysis.CallGraph;

namespace RoslynAnalyzer.Core.Analysis.Flow
{
    /// <summary>
    /// Base class for flow analyzers providing common infrastructure.
    /// </summary>
    /// <typeparam name="TFlow">The type of flow data being analyzed.</typeparam>
    /// <typeparam name="TInfo">The type of flow information produced.</typeparam>
    /// <remarks>
    /// This base class provides:
    /// - Call graph integration
    /// - Caching of analysis results
    /// - Common traversal patterns
    ///
    /// Derived classes implement the specific analysis logic for their flow type.
    /// </remarks>
    public abstract class FlowAnalyzerBase<TFlow, TInfo> : IFlowAnalyzer<TFlow, TInfo>
        where TInfo : IFlowInfo<TFlow>
    {
        private readonly Dictionary<IMethodSymbol, TInfo> _cache;
        private readonly Compilation _compilation;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlowAnalyzerBase{TFlow, TInfo}"/> class.
        /// </summary>
        /// <param name="compilation">The compilation to analyze.</param>
        protected FlowAnalyzerBase(Compilation compilation)
        {
            _compilation = compilation;
            _cache = new Dictionary<IMethodSymbol, TInfo>(SymbolEqualityComparer.Default);
        }

        /// <summary>
        /// Gets the compilation being analyzed.
        /// </summary>
        protected Compilation Compilation => _compilation;

        /// <summary>
        /// Analyzes flow for a specific method.
        /// </summary>
        /// <param name="method">The method to analyze.</param>
        /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
        /// <returns>Flow information for the method.</returns>
        public async Task<TInfo> AnalyzeAsync(IMethodSymbol method, CancellationToken cancellationToken = default)
        {
            // Check cache first
            if (_cache.TryGetValue(method, out var cached))
                return cached;

            // Perform analysis
            var info = await AnalyzeMethodAsync(method, cancellationToken).ConfigureAwait(false);

            // Cache result
            _cache[method] = info;

            return info;
        }

        /// <summary>
        /// Analyzes flow for an entire compilation.
        /// </summary>
        /// <param name="compilation">The compilation to analyze.</param>
        /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
        /// <returns>Flow information for all methods in the compilation.</returns>
        public async Task<IEnumerable<TInfo>> AnalyzeCompilationAsync(Compilation compilation, CancellationToken cancellationToken = default)
        {
            var results = new List<TInfo>();

            // Build call graph first for inter-procedural analysis
            var builder = new CallGraphBuilder(compilation, cancellationToken);
            var callGraph = await builder.BuildAsync().ConfigureAwait(false);

            // Analyze each method
            foreach (var node in callGraph.Nodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var info = await AnalyzeAsync(node.Method, cancellationToken).ConfigureAwait(false);
                results.Add(info);
            }

            return results;
        }

        /// <summary>
        /// Combines flow information from multiple sources.
        /// </summary>
        /// <param name="flows">The flow information to combine.</param>
        /// <returns>Combined flow information.</returns>
        /// <remarks>
        /// Default implementation performs a union. Override for custom merge strategies.
        /// </remarks>
        public virtual IEnumerable<TFlow> CombineFlows(params IEnumerable<TFlow>[] flows)
        {
            return flows.SelectMany(f => f).Distinct();
        }

        /// <summary>
        /// Performs the actual analysis for a method.
        /// </summary>
        /// <param name="method">The method to analyze.</param>
        /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
        /// <returns>Flow information for the method.</returns>
        /// <remarks>
        /// Derived classes implement this method to provide specific analysis logic.
        /// </remarks>
        protected abstract Task<TInfo> AnalyzeMethodAsync(IMethodSymbol method, CancellationToken cancellationToken);

        /// <summary>
        /// Clears the analysis cache.
        /// </summary>
        /// <remarks>
        /// Call this method if the compilation has changed and cached results are invalid.
        /// </remarks>
        protected void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Gets cached flow information if available.
        /// </summary>
        /// <param name="method">The method to look up.</param>
        /// <param name="info">The cached information if found.</param>
        /// <returns>True if cached information was found; otherwise, false.</returns>
        protected bool TryGetCached(IMethodSymbol method, out TInfo? info)
        {
            return _cache.TryGetValue(method, out info);
        }
    }
}
