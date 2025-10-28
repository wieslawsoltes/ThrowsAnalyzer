using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.FlowAnalysis;
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

        var disposableCreations = new List<(ILocalSymbol local, Location location)>();

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
                disposableCreations.Add((localSymbol, creation.Syntax.GetLocation()));
            }
        }, OperationKind.ObjectCreation);

        context.RegisterOperationBlockEndAction(blockEndContext =>
        {
            var methodOperation = blockEndContext.OperationBlocks.FirstOrDefault();
            if (methodOperation == null)
                return;

            foreach (var (local, location) in disposableCreations)
            {
                // Use the comprehensive disposal flow analyzer from core
                var analysis = _disposalAnalyzer.AnalyzeDisposal(
                    methodOperation,
                    local,
                    blockEndContext.Compilation.GetSemanticModel(methodOperation.Syntax.SyntaxTree)
                );

                if (!analysis.IsDisposedOnAllPaths)
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        location,
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
}
