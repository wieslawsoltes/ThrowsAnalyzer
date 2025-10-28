using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that validates factory method naming for disposable returns.
/// DISP027: Factory method disposal responsibility
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableFactoryPatternAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposableFactoryPattern,
        title: "Factory method should use clear naming for disposal ownership",
        messageFormat: "Method '{0}' returns IDisposable. Consider naming it 'Create{0}' to indicate caller owns disposal",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Factory methods returning disposables should use naming conventions (Create*, Build*, Make*) to indicate the caller is responsible for disposal.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        // Skip special methods
        if (method.MethodKind != MethodKind.Ordinary)
            return;

        // Skip private/internal methods
        if (method.DeclaredAccessibility != Accessibility.Public &&
            method.DeclaredAccessibility != Accessibility.Protected)
            return;

        // Check if return type is disposable
        if (!DisposableHelper.IsAnyDisposableType(method.ReturnType))
            return;

        var methodName = method.Name;

        // Check if method has clear factory naming
        var hasFactoryNaming = methodName.StartsWith("Create") ||
                              methodName.StartsWith("Build") ||
                              methodName.StartsWith("Make") ||
                              methodName.StartsWith("New") ||
                              methodName.StartsWith("Open") ||
                              methodName.StartsWith("Initialize");

        // Check for non-factory naming that might be confusing
        var hasConfusingNaming = methodName.StartsWith("Get") ||
                                methodName.StartsWith("Find") ||
                                methodName.StartsWith("Retrieve") ||
                                methodName.StartsWith("Fetch");

        if (hasConfusingNaming && !hasFactoryNaming)
        {
            // Get* methods with disposable returns are confusing
            // Did they already exist (Get) or create new (should be Create)?
            var diagnostic = Diagnostic.Create(
                Rule,
                method.Locations.FirstOrDefault(),
                methodName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
