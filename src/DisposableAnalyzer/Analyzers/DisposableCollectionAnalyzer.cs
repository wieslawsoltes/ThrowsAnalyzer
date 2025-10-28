using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects collections of disposable objects without proper disposal.
/// DISP020: Collection of disposables not disposed
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableCollectionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposableCollection,
        title: "Collection of disposables not disposed",
        messageFormat: "Field '{0}' is a collection of disposable objects. Ensure all elements are disposed",
        category: "Resource Management",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Collections containing disposable objects should dispose all elements when the collection is no longer needed.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
    }

    private void AnalyzeField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;

        // Skip static fields
        if (field.IsStatic)
            return;

        // Check if field is a collection type
        if (!IsCollectionType(field.Type, out var elementType))
            return;

        // Check if element type is disposable
        if (!DisposableHelper.IsAnyDisposableType(elementType))
            return;

        // Check if containing type implements IDisposable
        var containingType = field.ContainingType;
        if (!DisposableHelper.IsDisposableType(containingType) &&
            !DisposableHelper.IsAsyncDisposableType(containingType))
        {
            // Type doesn't implement IDisposable but has disposable collection - report diagnostic
            var diagnostic = Diagnostic.Create(
                Rule,
                field.Locations.FirstOrDefault(),
                field.Name);
            context.ReportDiagnostic(diagnostic);
        }

        // If type implements IDisposable, assume developer is handling disposal correctly
        // TODO: In future, could verify disposal in Dispose method for more thorough checking
    }

    private bool IsCollectionType(ITypeSymbol type, out ITypeSymbol? elementType)
    {
        elementType = null;

        if (type is not INamedTypeSymbol namedType)
            return false;

        // Check for common collection types
        var originalDefinition = namedType.OriginalDefinition.ToDisplayString();

        // Generic collections
        if (originalDefinition.StartsWith("System.Collections.Generic.List<") ||
            originalDefinition.StartsWith("System.Collections.Generic.IList<") ||
            originalDefinition.StartsWith("System.Collections.Generic.ICollection<") ||
            originalDefinition.StartsWith("System.Collections.Generic.IEnumerable<") ||
            originalDefinition.StartsWith("System.Collections.Generic.HashSet<") ||
            originalDefinition.StartsWith("System.Collections.Generic.LinkedList<") ||
            originalDefinition.StartsWith("System.Collections.Generic.Queue<") ||
            originalDefinition.StartsWith("System.Collections.Generic.Stack<") ||
            originalDefinition.StartsWith("System.Collections.ObjectModel.Collection<") ||
            originalDefinition.StartsWith("System.Collections.ObjectModel.ObservableCollection<"))
        {
            if (namedType.TypeArguments.Length > 0)
            {
                elementType = namedType.TypeArguments[0];
                return true;
            }
        }

        // Array types
        if (type is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
            return true;
        }

        return false;
    }
}
