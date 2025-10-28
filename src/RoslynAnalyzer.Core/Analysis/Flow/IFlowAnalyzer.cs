using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace RoslynAnalyzer.Core.Analysis.Flow
{
    /// <summary>
    /// Provides flow analysis for methods and code elements.
    /// </summary>
    /// <typeparam name="TFlow">The type of flow data being analyzed.</typeparam>
    /// <typeparam name="TInfo">The type of flow information produced.</typeparam>
    /// <remarks>
    /// Implement this interface to create custom flow analyzers for different analysis types:
    /// - Exception flow analysis
    /// - Data flow analysis
    /// - Taint analysis
    /// - Null reference analysis
    ///
    /// The analyzer uses the call graph to perform inter-procedural analysis.
    /// </remarks>
    public interface IFlowAnalyzer<TFlow, TInfo>
        where TInfo : IFlowInfo<TFlow>
    {
        /// <summary>
        /// Analyzes flow for a specific method.
        /// </summary>
        /// <param name="method">The method to analyze.</param>
        /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
        /// <returns>Flow information for the method.</returns>
        Task<TInfo> AnalyzeAsync(IMethodSymbol method, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes flow for an entire compilation.
        /// </summary>
        /// <param name="compilation">The compilation to analyze.</param>
        /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
        /// <returns>Flow information for all methods in the compilation.</returns>
        Task<IEnumerable<TInfo>> AnalyzeCompilationAsync(Compilation compilation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Combines flow information from multiple sources.
        /// </summary>
        /// <param name="flows">The flow information to combine.</param>
        /// <returns>Combined flow information.</returns>
        /// <remarks>
        /// This is used to merge flow from multiple paths (e.g., different branches, called methods).
        /// The merge strategy depends on the flow type:
        /// - Exception flow: union of all possible exceptions
        /// - Data flow: join of all possible values
        /// - Taint flow: any tainted value taints the result
        /// </remarks>
        IEnumerable<TFlow> CombineFlows(params IEnumerable<TFlow>[] flows);
    }
}
