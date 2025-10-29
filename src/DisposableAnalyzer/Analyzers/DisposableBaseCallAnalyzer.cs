using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects missing base.Dispose() calls in derived classes.
/// DISP009: Missing base.Dispose() call
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableBaseCallAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposableBaseCall,
        title: "Missing base.Dispose() call",
        messageFormat: "Dispose method in '{0}' should call base.Dispose() to ensure base class resources are released",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Derived classes that override Dispose should call base.Dispose() to ensure proper cleanup of base class resources.");

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

        // Check if this is a Dispose() or Dispose(bool) method
        bool isDisposeMethod = method.Name == "Dispose" && method.Parameters.Length == 0;
        bool isDisposeBoolMethod = DisposableHelper.IsDisposeBoolMethod(method);

        if (!isDisposeMethod && !isDisposeBoolMethod)
            return;

        // Check if this type has a disposable base class
        var containingType = method.ContainingType;
        var baseType = containingType.BaseType;
        if (baseType == null)
            return;

        var baseImplementsDisposal = DisposableHelper.IsAnyDisposableType(baseType) ||
                                      baseType.GetMembers("Dispose").OfType<IMethodSymbol>().Any();

        if (!baseImplementsDisposal)
            return;

        var hasBaseCall = false;

        context.RegisterOperationAction(operationContext =>
        {
            if (operationContext.Operation is IInvocationOperation invocation)
            {
                // Check for base.Dispose() or base.Dispose(disposing) call
                var targetMethod = invocation.TargetMethod;
                if (targetMethod.Name == "Dispose" &&
                    baseType != null &&
                    SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, baseType))
                {
                    hasBaseCall = true;
                }
            }
        }, OperationKind.Invocation);

        context.RegisterOperationBlockEndAction(blockEndContext =>
        {
            if (!hasBaseCall)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    method.Locations.FirstOrDefault(),
                    containingType.Name);
                blockEndContext.ReportDiagnostic(diagnostic);
            }
        });
    }
}
