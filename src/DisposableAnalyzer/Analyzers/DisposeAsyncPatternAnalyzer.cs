using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that validates DisposeAsync pattern implementation.
/// DISP013: DisposeAsync pattern violations
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposeAsyncPatternAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MissingDisposeAsyncCoreRule = new(
        id: DiagnosticIds.DisposeAsyncPattern,
        title: "DisposeAsync pattern not properly implemented",
        messageFormat: "Type '{0}' should implement the DisposeAsync pattern with protected virtual DisposeAsyncCore method",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Types implementing IAsyncDisposable should follow the DisposeAsync pattern for proper async disposal in derived classes.");

    public static readonly DiagnosticDescriptor WrongReturnTypeRule = new(
        id: DiagnosticIds.DisposeAsyncPattern,
        title: "DisposeAsync should return ValueTask",
        messageFormat: "DisposeAsync method should return ValueTask, not Task",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "DisposeAsync should return ValueTask for better performance and allocation characteristics.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingDisposeAsyncCoreRule, WrongReturnTypeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // Only analyze classes that implement IAsyncDisposable
        if (namedType.TypeKind != TypeKind.Class)
            return;

        if (!DisposableHelper.IsAsyncDisposableType(namedType))
            return;

        var disposeAsyncMethod = DisposableHelper.GetDisposeAsyncMethod(namedType);
        if (disposeAsyncMethod == null)
            return;

        // Check return type (should be ValueTask, not Task)
        var returnType = disposeAsyncMethod.ReturnType.ToDisplayString();
        if (returnType == "System.Threading.Tasks.Task")
        {
            var diagnostic = Diagnostic.Create(
                WrongReturnTypeRule,
                disposeAsyncMethod.Locations.FirstOrDefault());
            context.ReportDiagnostic(diagnostic);
        }

        // Check for DisposeAsyncCore pattern (for non-sealed classes)
        if (!namedType.IsSealed)
        {
            var disposeAsyncCoreMethod = namedType.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m =>
                    m.Name == "DisposeAsyncCore" &&
                    m.Parameters.Length == 0 &&
                    m.DeclaredAccessibility == Accessibility.Protected &&
                    m.IsVirtual);

            if (disposeAsyncCoreMethod == null)
            {
                // Only report if this type declares the DisposeAsync (not inherited)
                if (SymbolEqualityComparer.Default.Equals(disposeAsyncMethod.ContainingType, namedType))
                {
                    var diagnostic = Diagnostic.Create(
                        MissingDisposeAsyncCoreRule,
                        namedType.Locations.FirstOrDefault(),
                        namedType.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
