using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects resource leaks when disposable objects are passed across method boundaries without proper disposal.
/// DISP023: Resource leak across methods
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ResourceLeakAcrossMethodsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ResourceLeakAcrossMethod,
        title: "Potential resource leak across method calls",
        messageFormat: "Disposable '{0}' from '{1}' may not be disposed. Ensure caller handles disposal",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When a method returns a disposable resource, the caller must ensure it is properly disposed.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;

        // Check if method returns disposable
        if (!DisposableHelper.IsAnyDisposableType(invocation.Type))
            return;

        // Check if the result is used
        var parent = invocation.Parent;
        if (parent == null)
        {
            // Result discarded - definite leak
            ReportLeak(context, invocation);
            return;
        }

        // Check various safe usages
        switch (parent)
        {
            case IUsingOperation:
                // Safe: used in using statement
                return;

            case IReturnOperation:
                // Safe: returned to caller (transfers ownership)
                return;

            case IAssignmentOperation assignment:
                // Check if assigned to field (ownership transfer)
                if (assignment.Target is IFieldReferenceOperation ||
                    assignment.Target is IPropertyReferenceOperation)
                {
                    return; // Assuming class will dispose
                }

                // Assigned to local - check if local is disposed
                if (assignment.Target is ILocalReferenceOperation localRef)
                {
                    if (IsLocalDisposed(context, localRef.Local, assignment))
                        return;
                }
                break;

            case IVariableInitializerOperation initializer:
                // Check if variable is disposed
                if (initializer.Parent is IVariableDeclaratorOperation declarator)
                {
                    if (declarator.Symbol is ILocalSymbol local)
                    {
                        if (IsLocalDisposed(context, local, initializer))
                            return;
                    }
                }
                break;

            case IArgumentOperation argument:
                // Passed to another method - check if ownership transfers
                var parameter = argument.Parameter;
                if (parameter != null)
                {
                    var paramName = parameter.Name.ToLowerInvariant();
                    if (paramName.Contains("take") || paramName.Contains("own") ||
                        paramName.Contains("adopt") || paramName.Contains("add") ||
                        paramName.Contains("register"))
                    {
                        return; // Ownership transferred
                    }
                }
                break;
        }

        // If we get here, potential leak
        ReportLeak(context, invocation);
    }

    private bool IsLocalDisposed(OperationAnalysisContext context, ILocalSymbol local, IOperation startPoint)
    {
        // Find the containing method
        var methodOperation = startPoint;
        while (methodOperation != null && methodOperation is not IMethodBodyOperation and not IConstructorBodyOperation)
        {
            methodOperation = methodOperation.Parent;
        }

        if (methodOperation == null)
            return false;

        // Look for disposal of this local in subsequent operations
        var foundDisposal = false;
        foreach (var operation in methodOperation.Descendants())
        {
            if (operation is IInvocationOperation inv)
            {
                if (DisposableHelper.IsDisposalCall(inv, out _))
                {
                    if (inv.Instance is ILocalReferenceOperation localRefInv &&
                        SymbolEqualityComparer.Default.Equals(localRefInv.Local, local))
                    {
                        foundDisposal = true;
                        break;
                    }
                }
            }
            else if (operation is IUsingOperation usingOp)
            {
                if (usingOp.Resources is IVariableDeclarationGroupOperation declGroup)
                {
                    foreach (var decl in declGroup.Declarations)
                    {
                        foreach (var declarator in decl.Declarators)
                        {
                            if (SymbolEqualityComparer.Default.Equals(declarator.Symbol, local))
                            {
                                foundDisposal = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        return foundDisposal;
    }

    private void ReportLeak(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        var methodName = invocation.TargetMethod.Name;
        var typeName = invocation.Type?.Name ?? "resource";

        var diagnostic = Diagnostic.Create(
            Rule,
            invocation.Syntax.GetLocation(),
            typeName,
            methodName);
        context.ReportDiagnostic(diagnostic);
    }
}
