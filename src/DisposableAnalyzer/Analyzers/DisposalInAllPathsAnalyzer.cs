using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DisposableAnalyzer.Helpers;
using RoslynAnalyzer.Core.ControlFlow;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that ensures disposable resources are disposed on all execution paths.
/// DISP025: Disposal in all paths
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposalInAllPathsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposalInAllPaths,
        title: "Disposable not disposed on all code paths",
        messageFormat: "Disposable '{0}' is not disposed on all execution paths. Ensure disposal in finally block or using statement",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Disposable resources must be disposed on all possible execution paths, including exceptional paths.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationBlockStartAction(AnalyzeOperationBlockStart);
    }

    private static readonly DisposalFlowAnalyzer _disposalAnalyzer = new DisposalFlowAnalyzer(
        operation => DisposableHelper.IsDisposalCall(operation, out _)
    );

    private void AnalyzeOperationBlockStart(OperationBlockStartAnalysisContext context)
    {
        if (context.OwningSymbol is not IMethodSymbol)
            return;

        var disposableCreations = new List<(ILocalSymbol local, Location identifierLocation)>();
        var localsNeedingFinally = new HashSet<string>();

        // Track disposable creations
        context.RegisterOperationAction(operationContext =>
        {
            var creation = (IObjectCreationOperation)operationContext.Operation;

            if (!DisposableHelper.IsAnyDisposableType(creation.Type))
                return;

            // Find associated local variable
            var localSymbol = GetLocalSymbol(creation);
            if (localSymbol != null)
            {
                var declaratorReference = localSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                var declaratorSyntax = declaratorReference?.GetSyntax(operationContext.CancellationToken) as VariableDeclaratorSyntax;

                // Fall back to syntax walk if needed (e.g., for synthesized locals)
                declaratorSyntax ??= creation.Syntax.Ancestors().OfType<VariableDeclaratorSyntax>().FirstOrDefault();

                Location identifierLocation;
                if (declaratorSyntax != null)
                {
                    var identifierSpan = declaratorSyntax.Identifier.Span;
                    identifierLocation = Location.Create(declaratorSyntax.SyntaxTree, identifierSpan);
                }
                else
                {
                    identifierLocation = creation.Syntax.GetLocation();
                }

                disposableCreations.Add((localSymbol, identifierLocation));
            }
        }, OperationKind.ObjectCreation);

        context.RegisterOperationAction(operationContext =>
        {
            var tryOperation = (ITryOperation)operationContext.Operation;
            var referencedLocals = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);

            foreach (var localRef in tryOperation.Descendants().OfType<ILocalReferenceOperation>())
            {
                var local = localRef.Local;
                if (local != null && DisposableHelper.IsAnyDisposableType(local.Type))
                {
                    referencedLocals.Add(local);
                }
            }

            foreach (var local in referencedLocals)
            {
                if (tryOperation.Finally == null ||
                    !FinallyDisposesLocal(tryOperation.Finally, local))
                {
                    localsNeedingFinally.Add(local.Name);
                }
            }
        }, OperationKind.Try);

        context.RegisterOperationBlockEndAction(blockEndContext =>
        {
            var methodOperation = blockEndContext.OperationBlocks.FirstOrDefault();
            if (methodOperation == null)
                return;

            foreach (var (local, identifierLocation) in disposableCreations)
            {
                // Use the comprehensive disposal flow analyzer from core
                var analysis = _disposalAnalyzer.AnalyzeDisposal(
                    methodOperation,
                    local,
                    blockEndContext.Compilation.GetSemanticModel(methodOperation.Syntax.SyntaxTree)
                );

                if (!analysis.IsDisposedOnAllPaths || localsNeedingFinally.Contains(local.Name))
                {
                    var reportLocation = identifierLocation;

                    var diagnostic = Diagnostic.Create(
                        Rule,
                        reportLocation,
                        local.Name);
                    blockEndContext.ReportDiagnostic(diagnostic);
                }
            }
        });
    }

    private ILocalSymbol? GetLocalSymbol(IObjectCreationOperation creation)
    {
        var parent = creation.Parent;
        while (parent != null)
        {
            if (parent is IVariableDeclaratorOperation declarator)
            {
                return declarator.Symbol as ILocalSymbol;
            }
            else if (parent is IAssignmentOperation assignment)
            {
                if (assignment.Target is ILocalReferenceOperation localRef)
                {
                    return localRef.Local;
                }
            }
            parent = parent.Parent;
        }
        return null;
    }

    private static bool FinallyDisposesLocal(IBlockOperation finallyBlock, ILocalSymbol local)
    {
        foreach (var operation in finallyBlock.Descendants())
        {
            switch (operation)
            {
                case IInvocationOperation invocation
                    when invocation.Instance is ILocalReferenceOperation localRef &&
                         SymbolEqualityComparer.Default.Equals(localRef.Local, local) &&
                         DisposableHelper.IsDisposalCall(invocation, out _):
                    return true;

                case IConditionalAccessOperation conditional
                    when conditional.Operation is ILocalReferenceOperation conditionalLocal &&
                         SymbolEqualityComparer.Default.Equals(conditionalLocal.Local, local) &&
                         conditional.WhenNotNull is IInvocationOperation innerInvocation &&
                         DisposableHelper.IsDisposalCall(innerInvocation, out _):
                    return true;
            }
        }

        return false;
    }
}
