using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects when disposal responsibility is conditional based on runtime conditions.
/// DISP024: Conditional ownership
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConditionalOwnershipAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ConditionalOwnership,
        title: "Conditional disposal creates unclear ownership",
        messageFormat: "Disposable '{0}' has conditional ownership. Ensure clear disposal responsibility in all code paths",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When disposal responsibility depends on runtime conditions, it creates unclear ownership semantics and potential resource leaks.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationBlockStartAction(AnalyzeOperationBlockStart);
    }

    private void AnalyzeOperationBlockStart(OperationBlockStartAnalysisContext context)
    {
        if (context.OwningSymbol is not IMethodSymbol method)
            return;

        var disposableLocals = new Dictionary<ILocalSymbol, Location>(SymbolEqualityComparer.Default);
        var conditionalDisposals = new Dictionary<ILocalSymbol, List<Location>>(SymbolEqualityComparer.Default);
        var unconditionalDisposals = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        // Track disposable creations
        context.RegisterOperationAction(operationContext =>
        {
            var creation = (IObjectCreationOperation)operationContext.Operation;

            if (!DisposableHelper.IsAnyDisposableType(creation.Type))
                return;

            // Find associated local variable
            var parent = creation.Parent;
            while (parent != null)
            {
                if (parent is IVariableDeclaratorOperation declarator)
                {
                    if (declarator.Symbol is ILocalSymbol local)
                    {
                        disposableLocals[local] = creation.Syntax.GetLocation();
                    }
                    break;
                }
                else if (parent is IAssignmentOperation assignment)
                {
                    if (assignment.Target is ILocalReferenceOperation localRef)
                    {
                        disposableLocals[localRef.Local] = creation.Syntax.GetLocation();
                    }
                    break;
                }
                parent = parent.Parent;
            }
        }, OperationKind.ObjectCreation);

        // Track disposal calls
        context.RegisterOperationAction(operationContext =>
        {
            var invocation = (IInvocationOperation)operationContext.Operation;

            if (!DisposableHelper.IsDisposalCall(invocation, out _))
                return;

            if (invocation.Instance is not ILocalReferenceOperation localRef)
                return;

            // Check if disposal is inside conditional
            if (IsInsideConditional(invocation))
            {
                if (!conditionalDisposals.ContainsKey(localRef.Local))
                {
                    conditionalDisposals[localRef.Local] = new List<Location>();
                }
                conditionalDisposals[localRef.Local].Add(invocation.Syntax.GetLocation());
            }
            else
            {
                unconditionalDisposals.Add(localRef.Local);
            }
        }, OperationKind.Invocation);

        context.RegisterOperationBlockEndAction(blockEndContext =>
        {
            foreach (var kvp in disposableLocals)
            {
                var local = kvp.Key;
                var creationLocation = kvp.Value;

                // Skip if unconditionally disposed (e.g., in finally block)
                if (unconditionalDisposals.Contains(local))
                    continue;

                // Check if disposal is conditional
                if (conditionalDisposals.TryGetValue(local, out var disposalLocations))
                {
                    // Check if there are multiple conditional disposal paths
                    // or if disposal is only in some branches
                    if (disposalLocations.Count > 0)
                    {
                        // Report at creation site
                        var diagnostic = Diagnostic.Create(
                            Rule,
                            creationLocation,
                            local.Name);
                        blockEndContext.ReportDiagnostic(diagnostic);
                    }
                }
            }
        });
    }

    private bool IsInsideConditional(IOperation operation)
    {
        var current = operation.Parent;
        while (current != null)
        {
            switch (current)
            {
                case IConditionalOperation:
                case ISwitchOperation:
                case IConditionalAccessOperation:
                    // Inside conditional - check if not in finally
                    if (!IsInFinallyBlock(operation))
                        return true;
                    break;

                case IMethodBodyOperation:
                case IConstructorBodyOperation:
                    // Reached method boundary
                    return false;
            }
            current = current.Parent;
        }

        return false;
    }

    private bool IsInFinallyBlock(IOperation operation)
    {
        var current = operation.Parent;
        while (current != null)
        {
            if (current is ITryOperation tryOp)
            {
                // Check if operation is inside finally block
                if (tryOp.Finally != null)
                {
                    var finallyOps = tryOp.Finally.Descendants();
                    if (finallyOps.Contains(operation))
                        return true;
                }
            }

            if (current is IMethodBodyOperation or IConstructorBodyOperation)
                break;

            current = current.Parent;
        }

        return false;
    }
}
