using System.Collections.Generic;
using System.Collections.Immutable;
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

        if (LambdaEscapes(lambda))
            return;

        var trackedLocals = new Dictionary<ILocalSymbol, DisposableCreation>(SymbolEqualityComparer.Default);
        var disposedLocals = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);
        var directCreations = new List<DisposableCreation>();

        AnalyzeOperation(lambda.Body, trackedLocals, disposedLocals, directCreations);

        foreach (var kvp in trackedLocals)
        {
            if (!disposedLocals.Contains(kvp.Key))
            {
                ReportDiagnostic(context, kvp.Value);
            }
        }

        foreach (var creation in directCreations)
        {
            ReportDiagnostic(context, creation);
        }
    }

    private void AnalyzeOperation(
        IOperation operation,
        Dictionary<ILocalSymbol, DisposableCreation> trackedLocals,
        HashSet<ILocalSymbol> disposedLocals,
        List<DisposableCreation> directCreations)
    {
        switch (operation)
        {
            case IVariableDeclaratorOperation declarator:
                HandleVariableDeclarator(declarator, trackedLocals);
                break;
            case IInvocationOperation invocation:
                HandleInvocation(invocation, disposedLocals);
                break;
            case IConditionalAccessOperation conditional:
                HandleConditionalAccess(conditional, disposedLocals);
                break;
            case IUsingOperation usingOperation:
                MarkUsingLocalsAsDisposed(usingOperation, disposedLocals);
                break;
            case IObjectCreationOperation creation:
                HandleDirectCreation(creation, directCreations);
                break;
        }

        foreach (var child in operation.Children)
        {
            AnalyzeOperation(child, trackedLocals, disposedLocals, directCreations);
        }
    }

    private void HandleVariableDeclarator(
        IVariableDeclaratorOperation declarator,
        Dictionary<ILocalSymbol, DisposableCreation> trackedLocals)
    {
        if (declarator.Initializer?.Value is not IObjectCreationOperation creation)
            return;

        if (!DisposableHelper.IsAnyDisposableType(creation.Type))
            return;

        if (DisposableHelper.IsInUsingStatement(declarator.Syntax))
            return;

        var typeName = creation.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var location = creation.Syntax.GetLocation();
        trackedLocals[declarator.Symbol] = new DisposableCreation(location, typeName);
    }

    private void HandleInvocation(
        IInvocationOperation invocation,
        HashSet<ILocalSymbol> disposedLocals)
    {
        if (invocation.Instance is ILocalReferenceOperation localRef &&
            DisposableHelper.IsDisposalCall(invocation, out _))
        {
            disposedLocals.Add(localRef.Local);
        }
    }

    private void HandleConditionalAccess(
        IConditionalAccessOperation conditional,
        HashSet<ILocalSymbol> disposedLocals)
    {
        if (conditional.Operation is ILocalReferenceOperation localRef &&
            conditional.WhenNotNull is IInvocationOperation invocation &&
            DisposableHelper.IsDisposalCall(invocation, out _))
        {
            disposedLocals.Add(localRef.Local);
        }
    }

    private void HandleDirectCreation(
        IObjectCreationOperation creation,
        List<DisposableCreation> directCreations)
    {
        if (!DisposableHelper.IsAnyDisposableType(creation.Type))
            return;

        if (DisposableHelper.IsInUsingStatement(creation.Syntax))
            return;

        var parent = SkipImplicitOperations(creation.Parent);

        if (parent is IVariableInitializerOperation or IArgumentOperation)
            return;

        if (parent is IAssignmentOperation assignment &&
            assignment.Target is ILocalReferenceOperation)
        {
            return;
        }

        var typeName = creation.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var location = creation.Syntax.GetLocation();
        directCreations.Add(new DisposableCreation(location, typeName));
    }

    private static IOperation? SkipImplicitOperations(IOperation? operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } ||
               operation is IDelegateCreationOperation { IsImplicit: true })
        {
            operation = operation?.Parent;
        }

        return operation;
    }

    private bool LambdaEscapes(IAnonymousFunctionOperation lambda)
    {
        var parent = SkipImplicitOperations(lambda.Parent);
        return parent is IReturnOperation;
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

    private void ReportDiagnostic(OperationAnalysisContext context, DisposableCreation creation)
    {
        var diagnostic = Diagnostic.Create(
            Rule,
            creation.Location,
            creation.TypeName);
        context.ReportDiagnostic(diagnostic);
    }

    private readonly struct DisposableCreation
    {
        public DisposableCreation(Location location, string typeName)
        {
            Location = location;
            TypeName = typeName;
        }

        public Location Location { get; }
        public string TypeName { get; }
    }
}
