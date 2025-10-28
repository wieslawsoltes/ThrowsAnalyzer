using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that validates GC.SuppressFinalize usage for performance.
/// DISP030: GC.SuppressFinalize usage
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SuppressFinalizerPerformanceAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MissingCallRule = new(
        id: DiagnosticIds.SuppressFinalizerPerformance,
        title: "Missing GC.SuppressFinalize call",
        messageFormat: "Dispose() should call GC.SuppressFinalize(this) when type '{0}' has a finalizer",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types with finalizers should call GC.SuppressFinalize in Dispose() to avoid unnecessary finalization overhead.");

    public static readonly DiagnosticDescriptor UnnecessaryCallRule = new(
        id: DiagnosticIds.SuppressFinalizerPerformance,
        title: "Unnecessary GC.SuppressFinalize call",
        messageFormat: "GC.SuppressFinalize called but type '{0}' has no finalizer",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Calling GC.SuppressFinalize is unnecessary if the type has no finalizer.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingCallRule, UnnecessaryCallRule);

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

        // Only analyze Dispose() methods
        if (method.Name != "Dispose" || method.Parameters.Length != 0)
            return;

        var containingType = method.ContainingType;
        var hasFinalizer = DisposableHelper.HasFinalizer(containingType);
        var hasSuppressFinalize = false;

        context.RegisterOperationAction(operationContext =>
        {
            if (operationContext.Operation is IInvocationOperation invocation)
            {
                if (DisposableHelper.IsSuppressFinalizeCall(invocation))
                {
                    hasSuppressFinalize = true;
                }
            }
        }, OperationKind.Invocation);

        context.RegisterOperationBlockEndAction(blockEndContext =>
        {
            if (hasFinalizer && !hasSuppressFinalize)
            {
                // Has finalizer but missing SuppressFinalize call
                var diagnostic = Diagnostic.Create(
                    MissingCallRule,
                    method.Locations.FirstOrDefault(),
                    containingType.Name);
                blockEndContext.ReportDiagnostic(diagnostic);
            }
            else if (!hasFinalizer && hasSuppressFinalize)
            {
                // Has SuppressFinalize but no finalizer (unnecessary)
                var diagnostic = Diagnostic.Create(
                    UnnecessaryCallRule,
                    method.Locations.FirstOrDefault(),
                    containingType.Name);
                blockEndContext.ReportDiagnostic(diagnostic);
            }
        });
    }
}
