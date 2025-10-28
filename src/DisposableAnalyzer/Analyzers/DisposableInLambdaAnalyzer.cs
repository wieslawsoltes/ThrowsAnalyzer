using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects disposable resource creation in lambda expressions.
/// DISP014: Disposable resource in lambda
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableInLambdaAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposableInLambda,
        title: "Disposable resource in lambda",
        messageFormat: "Disposable object '{0}' created in lambda expression may not be properly disposed",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Creating disposable resources in lambda expressions can lead to resource leaks if the lambda doesn't ensure proper disposal.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzeLambda, OperationKind.AnonymousFunction);
    }

    private void AnalyzeLambda(OperationAnalysisContext context)
    {
        var lambda = (IAnonymousFunctionOperation)context.Operation;

        // Track disposable locals created in the lambda
        var disposableLocals = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);
        var disposedLocals = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);

        // Analyze the lambda body
        AnalyzeOperation(lambda.Body, disposableLocals, disposedLocals);

        // Report undisposed locals
        foreach (var local in disposableLocals)
        {
            if (!disposedLocals.Contains(local))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    lambda.Syntax.GetLocation(),
                    local.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private void AnalyzeOperation(
        IOperation operation,
        HashSet<ILocalSymbol> disposableLocals,
        HashSet<ILocalSymbol> disposedLocals)
    {
        if (operation is IVariableDeclaratorOperation declarator)
        {
            var local = declarator.Symbol;
            if (declarator.Initializer?.Value != null)
            {
                var initType = declarator.Initializer.Value.Type;
                if (DisposableHelper.IsAnyDisposableType(initType))
                {
                    disposableLocals.Add(local);
                }
            }
        }

        if (operation is IInvocationOperation invocation)
        {
            if (invocation.Instance is ILocalReferenceOperation localRef)
            {
                if (DisposableHelper.IsDisposalCall(invocation, out _))
                {
                    disposedLocals.Add(localRef.Local);
                }
            }
        }

        if (operation is IUsingOperation)
        {
            // Variables in using are automatically disposed
            MarkUsingLocalsAsDisposed(operation, disposedLocals);
        }

        foreach (var child in operation.Children)
        {
            AnalyzeOperation(child, disposableLocals, disposedLocals);
        }
    }

    private void MarkUsingLocalsAsDisposed(IOperation usingOp, HashSet<ILocalSymbol> disposedLocals)
    {
        foreach (var descendant in usingOp.Descendants())
        {
            if (descendant is ILocalReferenceOperation localRef)
            {
                disposedLocals.Add(localRef.Local);
            }
        }
    }
}
