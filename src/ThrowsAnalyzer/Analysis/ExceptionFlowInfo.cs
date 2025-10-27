using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ThrowsAnalyzer.Analysis
{
    /// <summary>
    /// Contains information about exception flow for a method.
    /// </summary>
    public class ExceptionFlowInfo
    {
        public ExceptionFlowInfo(IMethodSymbol method)
        {
            Method = method;
            ThrownExceptions = new List<ThrownExceptionInfo>();
            CaughtExceptions = new List<CaughtExceptionInfo>();
            PropagatedExceptions = new List<ThrownExceptionInfo>();
        }

        /// <summary>
        /// The method being analyzed.
        /// </summary>
        public IMethodSymbol Method { get; }

        /// <summary>
        /// All exceptions thrown in this method (directly or from called methods).
        /// </summary>
        public List<ThrownExceptionInfo> ThrownExceptions { get; }

        /// <summary>
        /// All exceptions caught by this method.
        /// </summary>
        public List<CaughtExceptionInfo> CaughtExceptions { get; }

        /// <summary>
        /// Exceptions that propagate out of this method (thrown but not caught).
        /// </summary>
        public List<ThrownExceptionInfo> PropagatedExceptions { get; }

        /// <summary>
        /// Gets the number of unhandled exceptions.
        /// </summary>
        public int UnhandledCount => PropagatedExceptions.Count;

        /// <summary>
        /// Checks if this method has unhandled exceptions.
        /// </summary>
        public bool HasUnhandledExceptions => PropagatedExceptions.Count > 0;
    }

    /// <summary>
    /// Information about a thrown exception.
    /// </summary>
    public class ThrownExceptionInfo
    {
        /// <summary>
        /// The exception type being thrown.
        /// </summary>
        public ITypeSymbol ExceptionType { get; set; }

        /// <summary>
        /// The location where the exception is thrown.
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// True if the exception is thrown directly in this method (vs. from a called method).
        /// </summary>
        public bool IsDirect { get; set; }

        /// <summary>
        /// If not direct, the method that throws this exception.
        /// </summary>
        public IMethodSymbol OriginMethod { get; set; }

        /// <summary>
        /// The call chain depth (0 = direct, 1 = one level deep, etc.).
        /// </summary>
        public int PropagationDepth { get; set; }

        /// <summary>
        /// The call chain from the current method to the origin method.
        /// </summary>
        public List<IMethodSymbol> CallChain { get; set; } = new List<IMethodSymbol>();
    }

    /// <summary>
    /// Information about a caught exception.
    /// </summary>
    public class CaughtExceptionInfo
    {
        /// <summary>
        /// The exception type being caught.
        /// </summary>
        public ITypeSymbol ExceptionType { get; set; }

        /// <summary>
        /// The location of the catch clause.
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// The catch clause syntax node.
        /// </summary>
        public Microsoft.CodeAnalysis.CSharp.Syntax.CatchClauseSyntax CatchClause { get; set; }

        /// <summary>
        /// True if the catch clause has a filter (when clause).
        /// </summary>
        public bool HasFilter { get; set; }
    }

    /// <summary>
    /// Information about exception propagation across multiple levels.
    /// </summary>
    public class ExceptionPropagationChain
    {
        /// <summary>
        /// The exception type being propagated.
        /// </summary>
        public ITypeSymbol ExceptionType { get; set; }

        /// <summary>
        /// The methods in the propagation chain (from origin to final propagation point).
        /// </summary>
        public List<IMethodSymbol> PropagationPath { get; set; } = new List<IMethodSymbol>();

        /// <summary>
        /// The depth of the propagation chain.
        /// </summary>
        public int Depth => PropagationPath.Count;

        /// <summary>
        /// The original method that throws the exception.
        /// </summary>
        public IMethodSymbol OriginMethod => PropagationPath.Count > 0 ? PropagationPath[0] : null;

        /// <summary>
        /// The final method in the chain (where it propagates out).
        /// </summary>
        public IMethodSymbol FinalMethod => PropagationPath.Count > 0 ? PropagationPath[PropagationPath.Count - 1] : null;
    }
}
