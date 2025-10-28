using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;
using DisposableAnalyzer.Analysis;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects local disposable variables that are not properly disposed.
/// DISP001: Local disposable not disposed
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UndisposedLocalAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.UndisposedLocal,
        title: "Local disposable not disposed",
        messageFormat: "Local variable '{0}' implements IDisposable but is not disposed",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Local variables that implement IDisposable should be disposed using a using statement or explicit Dispose() call to prevent resource leaks.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationBlockAction(AnalyzeOperationBlock);
    }

    private void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
    {
        // Track all local variables that are disposable
        var disposableLocals = new Dictionary<ILocalSymbol, IVariableDeclaratorOperation>(SymbolEqualityComparer.Default);
        var disposedLocals = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);

        foreach (var operation in context.OperationBlocks)
        {
            AnalyzeOperation(operation, disposableLocals, disposedLocals, context.OwningSymbol);
        }

        // Report diagnostics for undisposed locals
        foreach (var kvp in disposableLocals)
        {
            var local = kvp.Key;
            var declarator = kvp.Value;
            if (!disposedLocals.Contains(local))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    declarator.Syntax.GetLocation(),
                    local.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private void AnalyzeOperation(
        IOperation operation,
        Dictionary<ILocalSymbol, IVariableDeclaratorOperation> disposableLocals,
        HashSet<ILocalSymbol> disposedLocals,
        ISymbol owningSymbol)
    {
        switch (operation)
        {
            case IVariableDeclaratorOperation declarator:
                AnalyzeVariableDeclarator(declarator, disposableLocals);
                break;

            case IInvocationOperation invocation:
                AnalyzeInvocation(invocation, disposedLocals);
                break;

            case IReturnOperation returnOp:
                AnalyzeReturn(returnOp, disposableLocals, disposedLocals);
                break;

            case IAssignmentOperation assignment:
                AnalyzeAssignment(assignment, disposableLocals, disposedLocals);
                break;

            case IUsingOperation:
                // Variables in using statements are automatically disposed
                MarkUsingVariablesAsDisposed(operation, disposedLocals);
                break;
        }

        // Recursively analyze child operations
        foreach (var child in operation.Children)
        {
            AnalyzeOperation(child, disposableLocals, disposedLocals, owningSymbol);
        }
    }

    private void AnalyzeVariableDeclarator(
        IVariableDeclaratorOperation declarator,
        Dictionary<ILocalSymbol, IVariableDeclaratorOperation> disposableLocals)
    {
        var local = declarator.Symbol;
        if (DisposableHelper.IsAnyDisposableType(local.Type))
        {
            // Check if variable is in a using statement/declaration
            if (!DisposableHelper.IsInUsingStatement(declarator.Syntax))
            {
                disposableLocals[local] = declarator;
            }
        }
    }

    private void AnalyzeInvocation(
        IInvocationOperation invocation,
        HashSet<ILocalSymbol> disposedLocals)
    {
        // Check if this is a Dispose() or DisposeAsync() call on a local
        if (invocation.Instance is ILocalReferenceOperation localRef)
        {
            if (DisposableHelper.IsDisposalCall(invocation, out _))
            {
                disposedLocals.Add(localRef.Local);
            }
        }

        // Also check for conditional access (e.g., obj?.Dispose())
        if (invocation.Parent is IConditionalAccessOperation conditionalAccess &&
            conditionalAccess.Operation is ILocalReferenceOperation condLocalRef)
        {
            if (DisposableHelper.IsDisposalCall(invocation, out _))
            {
                disposedLocals.Add(condLocalRef.Local);
            }
        }
    }

    private void AnalyzeReturn(
        IReturnOperation returnOp,
        Dictionary<ILocalSymbol, IVariableDeclaratorOperation> disposableLocals,
        HashSet<ILocalSymbol> disposedLocals)
    {
        // If a disposable local is returned, remove it from tracking
        // (ownership is transferred to caller)
        if (returnOp.ReturnedValue is ILocalReferenceOperation localRef &&
            disposableLocals.ContainsKey(localRef.Local))
        {
            disposedLocals.Add(localRef.Local); // Mark as "handled"
        }
    }

    private void AnalyzeAssignment(
        IAssignmentOperation assignment,
        Dictionary<ILocalSymbol, IVariableDeclaratorOperation> disposableLocals,
        HashSet<ILocalSymbol> disposedLocals)
    {
        // If a disposable local is assigned to a field/property, ownership is transferred
        if (assignment.Value is ILocalReferenceOperation localRef &&
            disposableLocals.ContainsKey(localRef.Local))
        {
            var target = assignment.Target;
            if (target is IFieldReferenceOperation or IPropertyReferenceOperation)
            {
                disposedLocals.Add(localRef.Local); // Mark as "handled"
            }
        }
    }

    private void MarkUsingVariablesAsDisposed(IOperation usingOp, HashSet<ILocalSymbol> disposedLocals)
    {
        // Find all locals used in the using statement
        var localsList = new List<ILocalSymbol>();
        foreach (var descendant in usingOp.Descendants())
        {
            if (descendant is ILocalReferenceOperation localRef && !localsList.Contains(localRef.Local, SymbolEqualityComparer.Default))
            {
                localsList.Add(localRef.Local);
            }
        }

        foreach (var local in localsList)
        {
            disposedLocals.Add(local);
        }
    }
}
