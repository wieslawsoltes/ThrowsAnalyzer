using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects potential issues with disposable structs.
/// DISP029: IDisposable struct patterns
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableStructAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor BoxingWarningRule = new(
        id: DiagnosticIds.DisposableStruct,
        title: "Disposable struct may cause boxing",
        messageFormat: "Struct '{0}' implements IDisposable. Be aware of boxing when passing to methods or storing in collections",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Disposable structs can be boxed when cast to IDisposable, which can lead to disposal not being called on the original struct.");

    public static readonly DiagnosticDescriptor FinalizerWarningRule = new(
        id: DiagnosticIds.DisposableStruct,
        title: "Struct cannot have finalizer",
        messageFormat: "Struct '{0}' implements IDisposable but cannot have a finalizer. Ensure all cleanup is done in Dispose()",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Structs cannot have finalizers, so all cleanup must be done through explicit Dispose() calls.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(BoxingWarningRule, FinalizerWarningRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // Only analyze structs
        if (namedType.TypeKind != TypeKind.Struct)
            return;

        // Must implement IDisposable
        if (!DisposableHelper.IsDisposableType(namedType))
            return;

        // Report boxing warning (always for disposable structs)
        var boxingDiagnostic = Diagnostic.Create(
            BoxingWarningRule,
            namedType.Locations.FirstOrDefault(),
            namedType.Name);
        context.ReportDiagnostic(boxingDiagnostic);

        // Note: Structs cannot have finalizers by design (compiler prevents it),
        // so we don't need to report a separate diagnostic about this.
        // The boxing warning is sufficient to alert developers about disposal concerns.
    }
}
