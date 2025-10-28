using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace RoslynAnalyzer.Core.Analysis.Flow
{
    /// <summary>
    /// Represents flow information for a method or code element.
    /// </summary>
    /// <typeparam name="TFlow">The type of flow data being tracked (e.g., exception types, data values).</typeparam>
    /// <remarks>
    /// This interface provides a generic pattern for tracking data flow through methods.
    /// Implementations can track various types of flow:
    /// - Exception flow (what exceptions propagate through methods)
    /// - Data flow (what data values flow through variables)
    /// - Taint flow (what data is tainted/untrusted)
    /// - Null flow (what variables may be null)
    ///
    /// The generic pattern allows building reusable flow analysis infrastructure.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Example: Exception flow
    /// public class ExceptionFlowInfo : IFlowInfo&lt;ITypeSymbol&gt;
    /// {
    ///     public IMethodSymbol Method { get; }
    ///     public IEnumerable&lt;ITypeSymbol&gt; IncomingFlow { get; }
    ///     public IEnumerable&lt;ITypeSymbol&gt; OutgoingFlow { get; }
    /// }
    /// </code>
    /// </example>
    public interface IFlowInfo<TFlow>
    {
        /// <summary>
        /// Gets the method or code element this flow information applies to.
        /// </summary>
        ISymbol Element { get; }

        /// <summary>
        /// Gets the flow data entering this element.
        /// </summary>
        /// <remarks>
        /// This represents data flowing into the element from callers, assignments, etc.
        /// For exception flow: exceptions that may be thrown by called methods.
        /// For data flow: values assigned to variables.
        /// </remarks>
        IEnumerable<TFlow> IncomingFlow { get; }

        /// <summary>
        /// Gets the flow data leaving this element.
        /// </summary>
        /// <remarks>
        /// This represents data flowing out of the element to callers, return values, etc.
        /// For exception flow: exceptions that propagate out of the method.
        /// For data flow: values returned or assigned to output parameters.
        /// </remarks>
        IEnumerable<TFlow> OutgoingFlow { get; }

        /// <summary>
        /// Gets a value indicating whether this element has unhandled flow.
        /// </summary>
        /// <remarks>
        /// Interpretation depends on the flow type:
        /// - Exception flow: true if exceptions propagate without being caught
        /// - Data flow: true if data escapes the intended scope
        /// - Taint flow: true if tainted data reaches sensitive sinks
        /// </remarks>
        bool HasUnhandledFlow { get; }
    }
}
