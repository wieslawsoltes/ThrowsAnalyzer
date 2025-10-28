using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analysis;

/// <summary>
/// Flow analyzer for tracking IDisposable resource lifetime and disposal state.
/// </summary>
public class DisposableFlowAnalyzer
{
    private readonly ILocalSymbol _trackedLocal;
    private readonly SemanticModel _semanticModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableFlowAnalyzer"/> class.
    /// </summary>
    /// <param name="trackedLocal">The local variable to track disposal for.</param>
    /// <param name="semanticModel">The semantic model for symbol resolution.</param>
    public DisposableFlowAnalyzer(ILocalSymbol trackedLocal, SemanticModel semanticModel)
    {
        _trackedLocal = trackedLocal ?? throw new ArgumentNullException(nameof(trackedLocal));
        _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
    }

    /// <summary>
    /// Analyzes the method and returns the final disposal flow information.
    /// </summary>
    public DisposableFlowInfo Analyze(IOperation rootOperation)
    {
        CurrentFlowInfo = GetInitialFlowInfo();
        AnalyzeOperation(rootOperation, CurrentFlowInfo);
        return CurrentFlowInfo;
    }

    /// <summary>
    /// Gets the initial flow state (resource not disposed).
    /// </summary>
    private DisposableFlowInfo GetInitialFlowInfo()
    {
        return new DisposableFlowInfo
        {
            State = DisposalState.NotDisposed,
            HasNullCheck = false,
            Element = _trackedLocal
        };
    }

    /// <summary>
    /// Analyzes an operation and updates the disposal state.
    /// </summary>
    private void AnalyzeOperation(IOperation operation, DisposableFlowInfo flowInfo)
    {
        switch (operation)
        {
            case IInvocationOperation invocation:
                AnalyzeInvocation(invocation, flowInfo);
                break;

            case IConditionalAccessOperation conditionalAccess:
                AnalyzeConditionalAccess(conditionalAccess, flowInfo);
                break;

            case IIsPatternOperation:
                // Null check detected
                flowInfo.HasNullCheck = true;
                break;
        }
    }

    private void AnalyzeInvocation(IInvocationOperation invocation, DisposableFlowInfo flowInfo)
    {
        // Check if this is a disposal call on our tracked variable
        if (invocation.Instance is ILocalReferenceOperation localRef &&
            SymbolEqualityComparer.Default.Equals(localRef.Local, _trackedLocal))
        {
            if (DisposableHelper.IsDisposalCall(invocation, out bool isAsync))
            {
                // Check current state for potential double disposal
                if (flowInfo.State == DisposalState.Disposed)
                {
                    // Potential double disposal - will be caught by DoubleDisposeAnalyzer
                }

                flowInfo.State = DisposalState.Disposed;
                flowInfo.DisposalLocations.Add(new DisposalLocation
                {
                    Line = invocation.Syntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    IsAsync = isAsync,
                    IsConditional = IsInConditionalBlock(invocation)
                });
            }
        }
    }

    private void AnalyzeConditionalAccess(IConditionalAccessOperation conditionalAccess, DisposableFlowInfo flowInfo)
    {
        // Check for ?. disposal pattern (e.g., obj?.Dispose())
        if (conditionalAccess.Operation is ILocalReferenceOperation localRef &&
            SymbolEqualityComparer.Default.Equals(localRef.Local, _trackedLocal))
        {
            if (conditionalAccess.WhenNotNull is IInvocationOperation invocation &&
                DisposableHelper.IsDisposalCall(invocation, out bool isAsync))
            {
                flowInfo.State = DisposalState.MaybeDisposed; // Conditional disposal
                flowInfo.HasNullCheck = true;
                flowInfo.DisposalLocations.Add(new DisposalLocation
                {
                    Line = conditionalAccess.Syntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    IsAsync = isAsync,
                    IsConditional = true
                });
            }
        }
    }

    private bool IsInConditionalBlock(IOperation operation)
    {
        var current = operation.Parent;
        while (current != null)
        {
            if (current is IConditionalOperation or ISwitchOperation)
                return true;
            current = current.Parent;
        }
        return false;
    }

    private DisposableFlowInfo CurrentFlowInfo { get; set; } = null!;

    /// <summary>
    /// Gets the final disposal state after analyzing the entire method.
    /// </summary>
    public DisposalState GetFinalState()
    {
        return CurrentFlowInfo?.State ?? DisposalState.NotDisposed;
    }

    /// <summary>
    /// Gets all disposal locations found during analysis.
    /// </summary>
    public IReadOnlyList<DisposalLocation> GetDisposalLocations()
    {
        return CurrentFlowInfo?.DisposalLocations ?? new List<DisposalLocation>();
    }
}
