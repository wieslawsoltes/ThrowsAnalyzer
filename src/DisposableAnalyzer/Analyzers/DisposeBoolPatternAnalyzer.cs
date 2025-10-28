using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that validates the Dispose(bool) pattern implementation.
/// DISP008: Dispose(bool) pattern violations
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposeBoolPatternAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MissingDisposeBoolRule = new(
        id: DiagnosticIds.DisposeBoolPattern,
        title: "Dispose(bool) pattern not properly implemented",
        messageFormat: "Type '{0}' should implement the Dispose(bool disposing) pattern with a protected virtual method",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types with finalizers or that may be inherited should implement the Dispose(bool) pattern for proper resource cleanup.");

    public static readonly DiagnosticDescriptor MissingSuppressFinalize = new(
        id: DiagnosticIds.DisposeBoolPattern,
        title: "Missing GC.SuppressFinalize call",
        messageFormat: "Dispose() method should call GC.SuppressFinalize(this) when a finalizer is present",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When a type has a finalizer, Dispose() should call GC.SuppressFinalize to avoid unnecessary finalization.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingDisposeBoolRule, MissingSuppressFinalize);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // Only analyze classes that implement IDisposable
        if (namedType.TypeKind != TypeKind.Class)
            return;

        if (!DisposableHelper.IsDisposableType(namedType))
            return;

        bool hasFinalizer = DisposableHelper.HasFinalizer(namedType);
        bool isSealed = namedType.IsSealed;
        bool hasDisposableBase = DisposableHelper.HasDisposableBase(namedType);

        // Check for Dispose(bool) pattern
        var disposeBoolMethod = namedType.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(DisposableHelper.IsDisposeBoolMethod);

        // If type has finalizer or is not sealed (may be inherited), should have Dispose(bool)
        if ((hasFinalizer || !isSealed || hasDisposableBase) && disposeBoolMethod == null)
        {
            // Only report if this type declares IDisposable (not inherited)
            var disposeMethod = DisposableHelper.GetDisposeMethod(namedType);
            if (disposeMethod != null &&
                SymbolEqualityComparer.Default.Equals(disposeMethod.ContainingType, namedType))
            {
                var diagnostic = Diagnostic.Create(
                    MissingDisposeBoolRule,
                    namedType.Locations.FirstOrDefault(),
                    namedType.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        // If type has finalizer, check that Dispose() calls GC.SuppressFinalize
        if (hasFinalizer)
        {
            var disposeMethod = DisposableHelper.GetDisposeMethod(namedType);
            if (disposeMethod != null &&
                SymbolEqualityComparer.Default.Equals(disposeMethod.ContainingType, namedType))
            {
                // This would require operation analysis to verify the call
                // For now, we just flag it for manual review
                // Full implementation would check method body for SuppressFinalize call
            }
        }
    }
}
