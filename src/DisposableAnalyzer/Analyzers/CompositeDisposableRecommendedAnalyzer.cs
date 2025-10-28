using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that suggests using CompositeDisposable pattern for multiple disposable fields.
/// DISP026: Suggest CompositeDisposable
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CompositeDisposableRecommendedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.CompositeDisposableRecommended,
        title: "Consider using CompositeDisposable pattern",
        messageFormat: "Type '{0}' has {1} disposable fields. Consider using CompositeDisposable pattern for cleaner disposal",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Types with multiple disposable fields can benefit from the CompositeDisposable pattern for centralized disposal management.");

    private const int MinimumFieldsForRecommendation = 3;

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

        // Only analyze classes
        if (namedType.TypeKind != TypeKind.Class)
            return;

        // Must implement IDisposable
        if (!DisposableHelper.IsDisposableType(namedType))
            return;

        // Count disposable fields
        var disposableFields = namedType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsStatic && DisposableHelper.IsAnyDisposableType(f.Type))
            .ToList();

        if (disposableFields.Count < MinimumFieldsForRecommendation)
            return;

        // Check if already using CompositeDisposable-like pattern
        var hasCompositeField = namedType.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(f => f.Type.Name.Contains("CompositeDisposable") ||
                      f.Type.Name.Contains("DisposableCollection"));

        if (hasCompositeField)
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            namedType.Locations.FirstOrDefault(),
            namedType.Name,
            disposableFields.Count);
        context.ReportDiagnostic(diagnostic);
    }
}
