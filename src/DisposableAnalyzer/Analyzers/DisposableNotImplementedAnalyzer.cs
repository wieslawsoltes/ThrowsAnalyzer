using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects types with disposable fields that don't implement IDisposable.
/// DISP007: Type has disposable field but doesn't implement IDisposable
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableNotImplementedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposableNotImplemented,
        title: "Type has disposable field but doesn't implement IDisposable",
        messageFormat: "Type '{0}' contains disposable field(s) but does not implement IDisposable",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types that own disposable resources (fields) should implement IDisposable to ensure proper cleanup.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // Only analyze classes and structs
        if (namedType.TypeKind != TypeKind.Class && namedType.TypeKind != TypeKind.Struct)
            return;

        // Skip if type already implements IDisposable or IAsyncDisposable
        if (DisposableHelper.IsDisposableType(namedType) ||
            DisposableHelper.IsAsyncDisposableType(namedType))
            return;

        // Get all disposable fields owned by this type (not inherited)
        var disposableFields = namedType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsStatic && DisposableHelper.IsAnyDisposableType(f.Type))
            .ToList();

        if (!disposableFields.Any())
            return;

        // Report diagnostic on the type declaration
        var diagnostic = Diagnostic.Create(
            Rule,
            namedType.Locations.FirstOrDefault(),
            namedType.Name);
        context.ReportDiagnostic(diagnostic);
    }
}
