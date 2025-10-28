using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using RoslynAnalyzer.Core.Analysis.Flow;

namespace DisposableAnalyzer.Analysis;

/// <summary>
/// Represents the disposal state of a resource at a specific point in the control flow.
/// </summary>
public class DisposableFlowInfo : IFlowInfo<DisposalState>
{
    /// <summary>
    /// Gets or sets the disposal state of the resource.
    /// </summary>
    public DisposalState State { get; set; }

    /// <summary>
    /// Gets or sets whether the resource has been null-checked before disposal.
    /// </summary>
    public bool HasNullCheck { get; set; }

    /// <summary>
    /// Gets or sets the list of disposal locations in the flow.
    /// </summary>
    public List<DisposalLocation> DisposalLocations { get; set; } = new();

    /// <summary>
    /// Gets the element this flow information applies to.
    /// </summary>
    public ISymbol Element { get; set; } = null!;

    /// <summary>
    /// Gets the flow data entering this element.
    /// </summary>
    public IEnumerable<DisposalState> IncomingFlow { get; set; } = new[] { DisposalState.NotDisposed };

    /// <summary>
    /// Gets the flow data leaving this element.
    /// </summary>
    public IEnumerable<DisposalState> OutgoingFlow => new[] { State };

    /// <summary>
    /// Gets a value indicating whether this element has unhandled flow (undisposed resources).
    /// </summary>
    public bool HasUnhandledFlow => State == DisposalState.NotDisposed;

    /// <summary>
    /// Creates a copy of this flow info.
    /// </summary>
    public DisposableFlowInfo Clone()
    {
        return new DisposableFlowInfo
        {
            State = State,
            HasNullCheck = HasNullCheck,
            DisposalLocations = new List<DisposalLocation>(DisposalLocations),
            Element = Element
        };
    }

    /// <summary>
    /// Merges another flow info into this one (for control flow join points).
    /// </summary>
    public void Merge(DisposableFlowInfo other)
    {
        if (other == null)
            return;

        // If one path disposed and another didn't, the result is "maybe disposed"
        if (State != other.State)
        {
            State = DisposalState.MaybeDisposed;
        }

        // Null check must be present in both paths
        HasNullCheck = HasNullCheck && other.HasNullCheck;

        // Merge disposal locations
        DisposalLocations.AddRange(other.DisposalLocations);
    }
}

/// <summary>
/// Represents the disposal state of a resource.
/// </summary>
public enum DisposalState
{
    /// <summary>
    /// The resource has been created but not disposed.
    /// </summary>
    NotDisposed,

    /// <summary>
    /// The resource has been disposed.
    /// </summary>
    Disposed,

    /// <summary>
    /// The resource may or may not be disposed (conditional disposal).
    /// </summary>
    MaybeDisposed,

    /// <summary>
    /// The resource is managed by a using statement.
    /// </summary>
    ManagedByUsing
}

/// <summary>
/// Represents a location where disposal occurs.
/// </summary>
public class DisposalLocation
{
    /// <summary>
    /// Gets or sets the line number where disposal occurs.
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets whether this is an async disposal (DisposeAsync).
    /// </summary>
    public bool IsAsync { get; set; }

    /// <summary>
    /// Gets or sets whether this disposal is in a conditional block.
    /// </summary>
    public bool IsConditional { get; set; }
}
