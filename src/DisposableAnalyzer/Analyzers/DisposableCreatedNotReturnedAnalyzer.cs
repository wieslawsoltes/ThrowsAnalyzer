using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects disposable resources created in helper methods that are not returned to caller.
/// DISP022: Disposable created but not returned
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableCreatedNotReturnedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposableCreatedNotReturned,
        title: "Disposable created in method but not returned",
        messageFormat: "Method '{0}' creates disposable '{1}' but does not return it. The resource will leak unless disposed locally",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Helper methods that create disposable resources should either dispose them locally or return them to the caller for disposal.");

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

        // Skip if method returns disposable (it's a factory method)
        if (DisposableHelper.IsAnyDisposableType(method.ReturnType))
            return;

        // Skip if method is Dispose or finalizer
        if (method.Name == "Dispose" || method.Name == "Finalize")
            return;

        var createdDisposables = new List<ILocalSymbol>();
        var disposedLocals = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        var passedToOthers = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        context.RegisterOperationAction(operationContext =>
        {
            var creation = (IObjectCreationOperation)operationContext.Operation;

            if (!DisposableHelper.IsAnyDisposableType(creation.Type))
                return;

            // Check if assigned to local variable
            var parent = creation.Parent;
            while (parent != null)
            {
                if (parent is IVariableDeclaratorOperation declarator)
                {
                    if (declarator.Symbol is ILocalSymbol local)
                    {
                        createdDisposables.Add(local);
                    }
                    break;
                }
                else if (parent is IAssignmentOperation assignment)
                {
                    if (assignment.Target is ILocalReferenceOperation localRef)
                    {
                        createdDisposables.Add(localRef.Local);
                    }
                    break;
                }
                parent = parent.Parent;
            }
        }, OperationKind.ObjectCreation);

        context.RegisterOperationAction(operationContext =>
        {
            var invocation = (IInvocationOperation)operationContext.Operation;

            // Track Dispose calls
            if (DisposableHelper.IsDisposalCall(invocation, out _))
            {
                if (invocation.Instance is ILocalReferenceOperation localRef)
                {
                    disposedLocals.Add(localRef.Local);
                }
            }

            // Track if passed to other methods
            foreach (var argument in invocation.Arguments)
            {
                if (argument.Value is ILocalReferenceOperation argLocalRef)
                {
                    // Check if method takes ownership (by name convention)
                    var parameter = argument.Parameter;
                    if (parameter != null)
                    {
                        var paramName = parameter.Name.ToLowerInvariant();
                        if (paramName.Contains("take") || paramName.Contains("own") ||
                            paramName.Contains("adopt") || paramName.Contains("add"))
                        {
                            passedToOthers.Add(argLocalRef.Local);
                        }
                    }
                }
            }
        }, OperationKind.Invocation);

        context.RegisterOperationBlockEndAction(blockEndContext =>
        {
            foreach (var local in createdDisposables)
            {
                // Check if this local was disposed or passed to ownership-taking method
                if (!disposedLocals.Contains(local) && !passedToOthers.Contains(local))
                {
                    // Find the creation location
                    var location = local.Locations.FirstOrDefault();
                    if (location != null)
                    {
                        var diagnostic = Diagnostic.Create(
                            Rule,
                            location,
                            method.Name,
                            local.Type.Name);
                        blockEndContext.ReportDiagnostic(diagnostic);
                    }
                }
            }
        });
    }
}
