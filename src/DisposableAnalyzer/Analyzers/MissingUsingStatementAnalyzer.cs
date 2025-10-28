using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects disposable object creation without using statements.
/// DISP004: Should use 'using' statement
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingUsingStatementAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.MissingUsingStatement,
        title: "Should use 'using' statement",
        messageFormat: "Disposable object '{0}' should be wrapped in a using statement or using declaration",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Disposable objects should be wrapped in using statements to ensure proper disposal even when exceptions occur.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationBlockAction(AnalyzeOperationBlock);
    }

    private void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
    {
        // Track disposable locals and whether they are explicitly disposed or in using
        var disposableLocals = new Dictionary<ILocalSymbol, IVariableDeclaratorOperation>(SymbolEqualityComparer.Default);
        var disposableAssignments = new Dictionary<ILocalSymbol, ISimpleAssignmentOperation>(SymbolEqualityComparer.Default);
        var explicitlyDisposed = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);
        var inUsing = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);
        var escaped = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);

        foreach (var operation in context.OperationBlocks)
        {
            CollectDisposableLocals(operation, disposableLocals, disposableAssignments, inUsing);
            CollectExplicitDisposals(operation, explicitlyDisposed);
            CollectEscapedVariables(operation, escaped);
        }

        // Report diagnostics for disposables not in using (even if explicitly disposed, using is safer)
        foreach (var kvp in disposableLocals)
        {
            var local = kvp.Key;
            var declarator = kvp.Value;

            if (!inUsing.Contains(local) && !escaped.Contains(local))
            {
                // Get the location of just the variable identifier, not the entire declaration
                var location = declarator.Syntax is VariableDeclaratorSyntax declaratorSyntax
                    ? declaratorSyntax.Identifier.GetLocation()
                    : declarator.Syntax.GetLocation();

                var diagnostic = Diagnostic.Create(
                    Rule,
                    location,
                    local.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        // Report diagnostics for locals assigned disposables (but not declared with them)
        foreach (var kvp in disposableAssignments)
        {
            var local = kvp.Key;

            // Skip if already handled by declarators
            if (disposableLocals.ContainsKey(local))
                continue;

            if (!inUsing.Contains(local) && !escaped.Contains(local))
            {
                var assignment = kvp.Value;

                // Find the variable declarator for this local to report at declaration site
                var declarator = FindDeclaratorForLocal(context, local);
                if (declarator != null)
                {
                    var location = declarator.Syntax is VariableDeclaratorSyntax declaratorSyntax
                        ? declaratorSyntax.Identifier.GetLocation()
                        : declarator.Syntax.GetLocation();

                    var diagnostic = Diagnostic.Create(
                        Rule,
                        location,
                        local.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private IVariableDeclaratorOperation? FindDeclaratorForLocal(OperationBlockAnalysisContext context, ILocalSymbol local)
    {
        foreach (var operation in context.OperationBlocks)
        {
            var declarator = FindDeclaratorInOperation(operation, local);
            if (declarator != null)
                return declarator;
        }
        return null;
    }

    private IVariableDeclaratorOperation? FindDeclaratorInOperation(IOperation operation, ILocalSymbol local)
    {
        if (operation is IVariableDeclaratorOperation declarator && SymbolEqualityComparer.Default.Equals(declarator.Symbol, local))
            return declarator;

        foreach (var child in operation.Children)
        {
            var result = FindDeclaratorInOperation(child, local);
            if (result != null)
                return result;
        }

        return null;
    }

    private void CollectDisposableLocals(
        IOperation operation,
        Dictionary<ILocalSymbol, IVariableDeclaratorOperation> disposableLocals,
        Dictionary<ILocalSymbol, ISimpleAssignmentOperation> disposableAssignments,
        HashSet<ILocalSymbol> inUsing)
    {
        // Check if this is inside a using statement
        bool isInUsing = IsInUsingOperation(operation);

        // Check for variable declarator with disposable initializer
        if (operation is IVariableDeclaratorOperation declarator)
        {
            var local = declarator.Symbol;

            // Check if initializer creates a disposable object
            if (declarator.Initializer?.Value != null &&
                IsDisposableCreation(declarator.Initializer.Value))
            {
                if (isInUsing || DisposableHelper.IsInUsingStatement(declarator.Syntax))
                {
                    inUsing.Add(local);
                }
                else
                {
                    disposableLocals[local] = declarator;
                }
            }
        }

        // Also check for assignments to locals (e.g., stream = new FileStream(...))
        if (operation is ISimpleAssignmentOperation assignment)
        {
            if (assignment.Target is ILocalReferenceOperation localRef &&
                IsDisposableCreation(assignment.Value))
            {
                var local = localRef.Local;

                // Track the assignment if not in using
                if (!inUsing.Contains(local) && !isInUsing)
                {
                    disposableAssignments[local] = assignment;
                }
            }
        }

        foreach (var child in operation.Children)
        {
            CollectDisposableLocals(child, disposableLocals, disposableAssignments, inUsing);
        }
    }

    private bool IsDisposableCreation(IOperation operation)
    {
        // Check for object creation of disposable type
        if (operation is IObjectCreationOperation creation)
        {
            return DisposableHelper.IsAnyDisposableType(creation.Type);
        }

        // Check for invocation that returns disposable
        if (operation is IInvocationOperation invocation)
        {
            return DisposableHelper.IsAnyDisposableType(invocation.Type);
        }

        // Check for conversion operations
        if (operation is IConversionOperation conversion)
        {
            return IsDisposableCreation(conversion.Operand);
        }

        return false;
    }

    private bool IsInUsingOperation(IOperation operation)
    {
        var current = operation.Parent;
        while (current != null)
        {
            if (current is IUsingOperation)
                return true;
            current = current.Parent;
        }
        return false;
    }

    private void CollectExplicitDisposals(IOperation operation, HashSet<ILocalSymbol> explicitlyDisposed)
    {
        if (operation is IInvocationOperation invocation)
        {
            if (invocation.Instance is ILocalReferenceOperation localRef)
            {
                if (DisposableHelper.IsDisposalCall(invocation, out _))
                {
                    explicitlyDisposed.Add(localRef.Local);
                }
            }
        }

        foreach (var child in operation.Children)
        {
            CollectExplicitDisposals(child, explicitlyDisposed);
        }
    }

    private void CollectEscapedVariables(IOperation operation, HashSet<ILocalSymbol> escaped)
    {
        // Check for returns
        if (operation is IReturnOperation returnOp &&
            returnOp.ReturnedValue is ILocalReferenceOperation localRef)
        {
            escaped.Add(localRef.Local);
        }

        // Check for field/property assignments
        if (operation is IAssignmentOperation assignment)
        {
            if (assignment.Value is ILocalReferenceOperation assignedLocal)
            {
                if (assignment.Target is IFieldReferenceOperation or IPropertyReferenceOperation)
                {
                    escaped.Add(assignedLocal.Local);
                }
            }
        }

        // Check for passed as argument (potential ownership transfer)
        if (operation is IArgumentOperation argument &&
            argument.Value is ILocalReferenceOperation argLocal)
        {
            escaped.Add(argLocal.Local);
        }

        foreach (var child in operation.Children)
        {
            CollectEscapedVariables(child, escaped);
        }
    }
}
