using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that validates wrapper classes properly implement disposal.
/// DISP028: Wrapper class disposal
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableWrapperAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposableWrapper,
        title: "Wrapper class should implement IDisposable",
        messageFormat: "Wrapper class '{0}' should implement IDisposable and dispose the wrapped resource",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Wrapper classes that wrap disposable objects should implement IDisposable to properly dispose the wrapped resource.");

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

        // Skip if already implements IDisposable
        if (DisposableHelper.IsDisposableType(namedType))
            return;

        // Look for wrapper patterns (single private readonly disposable field)
        var disposableFields = namedType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsStatic &&
                       DisposableHelper.IsAnyDisposableType(f.Type) &&
                       f.DeclaredAccessibility == Accessibility.Private &&
                       f.IsReadOnly)
            .ToList();

        if (disposableFields.Count != 1)
            return;

        var field = disposableFields[0];

        // Check if constructor suggests ownership is transferred
        var wrapsOwnInstance = namedType.InstanceConstructors
            .Where(c => !c.IsImplicitlyDeclared)
            .Any(ctor => ctor.Parameters.Any(p => SymbolEqualityComparer.Default.Equals(p.Type, field.Type)));

        // Check if class name suggests wrapping
        var className = namedType.Name;
        var isWrapperName = className.Contains("Wrapper") ||
                           className.Contains("Adapter") ||
                           className.Contains("Decorator") ||
                           className.Contains("Proxy") ||
                           className.EndsWith("Manager") ||
                           className.EndsWith("Handler");

        if (isWrapperName && wrapsOwnInstance)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                namedType.Locations.FirstOrDefault(),
                namedType.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
