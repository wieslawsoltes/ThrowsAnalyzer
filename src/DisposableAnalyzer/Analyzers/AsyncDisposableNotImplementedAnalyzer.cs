using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects types that should implement IAsyncDisposable.
/// DISP012: Should implement IAsyncDisposable
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncDisposableNotImplementedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.AsyncDisposableNotImplemented,
        title: "Should implement IAsyncDisposable",
        messageFormat: "Type '{0}' contains async disposal operations but only implements IDisposable. Consider implementing IAsyncDisposable",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Types that perform async operations during disposal should implement IAsyncDisposable for proper async cleanup.");

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

        // Skip structs - they have different disposal semantics
        if (namedType.TypeKind != TypeKind.Class)
            return;

        // Skip if already implements IAsyncDisposable
        if (DisposableHelper.IsAsyncDisposableType(namedType))
            return;

        // Skip if already implements IDisposable - they can use synchronous disposal
        if (DisposableHelper.IsDisposableType(namedType))
            return;

        // Check if type has any fields that are IAsyncDisposable
        var hasAsyncDisposableFields = namedType.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(field => DisposableHelper.IsAsyncDisposableType(field.Type));

        if (hasAsyncDisposableFields)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                namedType.Locations.FirstOrDefault(),
                namedType.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
