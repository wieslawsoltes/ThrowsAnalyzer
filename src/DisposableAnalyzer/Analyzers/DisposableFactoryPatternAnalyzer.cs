using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that ensures factory methods returning disposable instances
/// clearly document the caller's disposal responsibilities.
/// DISP027: Factory method disposal responsibility.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableFactoryPatternAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposableFactoryPattern,
        title: "Factory method should document disposal ownership",
        messageFormat: "Method '{0}' returns a disposable type but XML documentation does not describe caller disposal responsibility",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Factory methods that create disposable instances should document that the caller must dispose the returned value.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol method)
            return;

        if (method.MethodKind != MethodKind.Ordinary)
            return;

        if (method.ReturnsVoid)
            return;

        if (!DisposableHelper.IsAnyDisposableType(method.ReturnType))
            return;

        // Skip methods that return the disposable interface directly (self-descriptive)
        var returnTypeName = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (returnTypeName is "System.IDisposable" or "System.IAsyncDisposable")
            return;

        // Only flag accessible factory-like methods
        if (method.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.Internal or Accessibility.ProtectedOrInternal))
            return;

        var documentation = method.GetDocumentationCommentXml(
            preferredCulture: null,
            expandIncludes: true,
            cancellationToken: context.CancellationToken);

        if (string.IsNullOrWhiteSpace(documentation))
        {
            Report(context, method);
            return;
        }

        var hasReturnsElement = documentation.IndexOf("<returns", StringComparison.OrdinalIgnoreCase) >= 0;
        var mentionsDispose = documentation.IndexOf("dispose", StringComparison.OrdinalIgnoreCase) >= 0;

        if (!hasReturnsElement || !mentionsDispose)
        {
            Report(context, method);
        }
    }

    private static void Report(SymbolAnalysisContext context, IMethodSymbol method)
    {
        var location = method.Locations.FirstOrDefault();
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, method.Name));
    }
}
