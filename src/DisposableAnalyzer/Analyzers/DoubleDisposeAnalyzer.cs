using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;
using DisposableAnalyzer.Analysis;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects potential double disposal of IDisposable objects.
/// DISP003: Potential double disposal
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DoubleDisposeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DoubleDispose,
        title: "Potential double disposal",
        messageFormat: "Object '{0}' may be disposed multiple times, which could cause ObjectDisposedException",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Disposing an object multiple times can cause ObjectDisposedException. Ensure disposal only happens once, or add null checks before disposal.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationBlockAction(AnalyzeOperationBlock);
    }

    private void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
    {
        // Track disposal calls for each local and field
        var disposalCalls = new Dictionary<ISymbol, List<IInvocationOperation>>(SymbolEqualityComparer.Default);
        var nullAssignments = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        foreach (var operation in context.OperationBlocks)
        {
            CollectDisposalCalls(operation, disposalCalls);
            CollectNullAssignments(operation, nullAssignments);
        }

        // Check for potential double disposals
        foreach (var kvp in disposalCalls)
        {
            var symbol = kvp.Key;
            var calls = kvp.Value;
            if (calls.Count > 1)
            {
                // If the symbol is assigned to null between disposals, it's safe
                if (nullAssignments.Contains(symbol))
                    continue;

                // Report on the second and subsequent disposal calls that don't have null checks
                // If a disposal has a null check (e.g., if (x != null) x.Dispose() or x?.Dispose()),
                // we consider it safe even if previous disposals didn't have null checks
                for (int i = 1; i < calls.Count; i++)
                {
                    if (!HasNullCheckBeforeDisposal(calls[i]))
                    {
                        var diagnostic = Diagnostic.Create(
                            Rule,
                            calls[i].Syntax.GetLocation(),
                            symbol.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }

    private void CollectDisposalCalls(
        IOperation operation,
        Dictionary<ISymbol, List<IInvocationOperation>> disposalCalls)
    {
        if (operation is IInvocationOperation invocation)
        {
            ISymbol? targetSymbol = null;

            // Check for direct disposal (obj.Dispose())
            if (invocation.Instance is ILocalReferenceOperation localRef)
            {
                targetSymbol = localRef.Local;
            }
            else if (invocation.Instance is IFieldReferenceOperation fieldRef)
            {
                targetSymbol = fieldRef.Field;
            }
            else if (invocation.Instance is IParameterReferenceOperation paramRef)
            {
                targetSymbol = paramRef.Parameter;
            }

            // Check for conditional access disposal (obj?.Dispose())
            if (invocation.Parent is IConditionalAccessOperation conditionalAccess)
            {
                if (conditionalAccess.Operation is ILocalReferenceOperation condLocalRef)
                {
                    targetSymbol = condLocalRef.Local;
                }
                else if (conditionalAccess.Operation is IFieldReferenceOperation condFieldRef)
                {
                    targetSymbol = condFieldRef.Field;
                }
            }

            if (targetSymbol != null && DisposableHelper.IsDisposalCall(invocation, out _))
            {
                if (!disposalCalls.ContainsKey(targetSymbol))
                {
                    disposalCalls[targetSymbol] = new List<IInvocationOperation>();
                }
                disposalCalls[targetSymbol].Add(invocation);
            }
        }

        // Recursively process child operations
        foreach (var child in operation.Children)
        {
            CollectDisposalCalls(child, disposalCalls);
        }
    }

    private void CollectNullAssignments(IOperation operation, HashSet<ISymbol> nullAssignments)
    {
        // Check for assignments to null (stream = null)
        if (operation is ISimpleAssignmentOperation assignment)
        {
            if (IsNullLiteral(assignment.Value))
            {
                ISymbol? targetSymbol = null;
                if (assignment.Target is ILocalReferenceOperation localRef)
                {
                    targetSymbol = localRef.Local;
                }
                else if (assignment.Target is IFieldReferenceOperation fieldRef)
                {
                    targetSymbol = fieldRef.Field;
                }
                else if (assignment.Target is IParameterReferenceOperation paramRef)
                {
                    targetSymbol = paramRef.Parameter;
                }

                if (targetSymbol != null)
                {
                    nullAssignments.Add(targetSymbol);
                }
            }
        }

        // Recursively process child operations
        foreach (var child in operation.Children)
        {
            CollectNullAssignments(child, nullAssignments);
        }
    }

    private bool HasNullCheckBeforeDisposal(IInvocationOperation disposal)
    {
        // Check if disposal is inside a null-conditional operator (?.)
        if (disposal.Parent is IConditionalAccessOperation)
            return true;

        // Check if disposal is inside an if statement with null check
        var disposedSymbol = GetDisposedSymbol(disposal);
        var conditionalParent = FindConditionalParent(disposal);
        if (conditionalParent != null && disposedSymbol != null)
        {
            // Check if condition contains null check for the disposed symbol
            var condition = conditionalParent.Condition;
            if (ContainsNullCheck(condition, disposedSymbol))
                return true;
        }

        return false;
    }

    private IConditionalOperation? FindConditionalParent(IOperation operation)
    {
        var current = operation.Parent;
        while (current != null)
        {
            // Look for IConditionalOperation (if statement)
            if (current is IConditionalOperation conditional)
                return conditional;

            current = current.Parent;
        }
        return null;
    }

    private bool ContainsNullCheck(IOperation operation, ISymbol? symbol)
    {
        if (symbol == null)
            return false;

        // Check for != null or is not null
        if (operation is IBinaryOperation binary)
        {
            if (binary.OperatorKind == BinaryOperatorKind.NotEquals)
            {
                if (IsSymbolReference(binary.LeftOperand, symbol) && IsNullLiteral(binary.RightOperand))
                    return true;
                if (IsSymbolReference(binary.RightOperand, symbol) && IsNullLiteral(binary.LeftOperand))
                    return true;
            }
        }

        // Check for is not null pattern
        if (operation is IIsPatternOperation isPattern)
        {
            if (IsSymbolReference(isPattern.Value, symbol))
                return true;
        }

        // Recursively check child operations
        foreach (var child in operation.Children)
        {
            if (ContainsNullCheck(child, symbol))
                return true;
        }

        return false;
    }

    private ISymbol? GetDisposedSymbol(IInvocationOperation disposal)
    {
        if (disposal.Instance is ILocalReferenceOperation localRef)
            return localRef.Local;
        if (disposal.Instance is IFieldReferenceOperation fieldRef)
            return fieldRef.Field;
        if (disposal.Instance is IParameterReferenceOperation paramRef)
            return paramRef.Parameter;

        return null;
    }

    private bool IsSymbolReference(IOperation operation, ISymbol symbol)
    {
        // Unwrap conversion operations (e.g., implicit conversions)
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation switch
        {
            ILocalReferenceOperation localRef => SymbolEqualityComparer.Default.Equals(localRef.Local, symbol),
            IFieldReferenceOperation fieldRef => SymbolEqualityComparer.Default.Equals(fieldRef.Field, symbol),
            IParameterReferenceOperation paramRef => SymbolEqualityComparer.Default.Equals(paramRef.Parameter, symbol),
            _ => false
        };
    }

    private bool IsNullLiteral(IOperation operation)
    {
        return operation.ConstantValue.HasValue && operation.ConstantValue.Value == null;
    }
}
