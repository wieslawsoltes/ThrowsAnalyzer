using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects field access after disposal.
/// DISP010: Access to disposed field
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposedFieldAccessAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposedFieldAccess,
        title: "Access to disposed field",
        messageFormat: "Field '{0}' may be accessed after disposal, which could cause ObjectDisposedException",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Accessing fields after they have been disposed can cause ObjectDisposedException. Ensure proper ordering and null checks.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationBlockAction(AnalyzeOperationBlock);
    }

    private void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
    {
        // Track disposable fields and their disposal points
        var disposedFields = new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default);
        var fieldAccesses = new List<(IFieldSymbol field, IOperation operation)>();

        foreach (var operation in context.OperationBlocks)
        {
            CollectFieldDisposals(operation, disposedFields);
            CollectFieldAccesses(operation, fieldAccesses);
        }

        // Simple heuristic: if a field is disposed in the operation block,
        // check if it's accessed after any disposal call
        if (disposedFields.Count == 0)
            return;

        foreach (var operation in context.OperationBlocks)
        {
            AnalyzeAccessAfterDisposal(operation, disposedFields, context);
        }
    }

    private void CollectFieldDisposals(IOperation operation, HashSet<IFieldSymbol> disposedFields)
    {
        if (operation is IInvocationOperation invocation)
        {
            if (invocation.Instance is IFieldReferenceOperation fieldRef)
            {
                if (DisposableHelper.IsDisposalCall(invocation, out _))
                {
                    disposedFields.Add(fieldRef.Field);
                }
            }
        }

        foreach (var child in operation.Children)
        {
            CollectFieldDisposals(child, disposedFields);
        }
    }

    private void CollectFieldAccesses(IOperation operation, List<(IFieldSymbol, IOperation)> fieldAccesses)
    {
        if (operation is IFieldReferenceOperation fieldRef)
        {
            // Skip if this is the disposal call itself
            if (operation.Parent is IInvocationOperation inv &&
                DisposableHelper.IsDisposalCall(inv, out _))
                return;

            fieldAccesses.Add((fieldRef.Field, operation));
        }

        foreach (var child in operation.Children)
        {
            CollectFieldAccesses(child, fieldAccesses);
        }
    }

    private void AnalyzeAccessAfterDisposal(
        IOperation operation,
        HashSet<IFieldSymbol> disposedFields,
        OperationBlockAnalysisContext context)
    {
        // Track if we've seen disposal for each field
        var seenDisposal = new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default);

        AnalyzeSequentially(operation, disposedFields, seenDisposal, context);
    }

    private void AnalyzeSequentially(
        IOperation operation,
        HashSet<IFieldSymbol> disposedFields,
        HashSet<IFieldSymbol> seenDisposal,
        OperationBlockAnalysisContext context)
    {
        // Check if this is a disposal call
        if (operation is IInvocationOperation invocation &&
            invocation.Instance is IFieldReferenceOperation disposeFieldRef)
        {
            if (DisposableHelper.IsDisposalCall(invocation, out _))
            {
                seenDisposal.Add(disposeFieldRef.Field);
            }
        }

        // Check if this is a field access after disposal
        if (operation is IFieldReferenceOperation fieldRef)
        {
            if (seenDisposal.Contains(fieldRef.Field) &&
                !(operation.Parent is IInvocationOperation parentInv &&
                  DisposableHelper.IsDisposalCall(parentInv, out _)))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    operation.Syntax.GetLocation(),
                    fieldRef.Field.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        // Recursively analyze children in order
        foreach (var child in operation.Children)
        {
            AnalyzeSequentially(child, disposedFields, seenDisposal, context);
        }
    }
}
