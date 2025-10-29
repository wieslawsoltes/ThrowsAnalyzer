using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects methods returning IDisposable without documentation.
/// DISP016: Disposable returned without transfer documentation
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableReturnedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DisposableReturned,
        title: "Disposable returned without transfer documentation",
        messageFormat: "Method '{0}' returns IDisposable but lacks XML documentation about disposal responsibility",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Methods returning IDisposable should document who is responsible for disposal using XML documentation comments.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
    }

    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        // Skip special methods
        if (method.MethodKind != MethodKind.Ordinary)
            return;

        // Check if return type implements IDisposable
        if (!DisposableHelper.IsAnyDisposableType(method.ReturnType))
            return;

        // Check for XML documentation
        var xmlDoc = method.GetDocumentationCommentXml();
        if (string.IsNullOrEmpty(xmlDoc))
        {
            // No documentation at all
            var diagnostic = Diagnostic.Create(
                Rule,
                method.Locations.FirstOrDefault(),
                method.Name);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Check if documentation mentions disposal, ownership, or caller responsibility
        var docLower = xmlDoc.ToLowerInvariant();
        var hasDisposalDoc = docLower.Contains("dispose") ||
                            docLower.Contains("ownership") ||
                            docLower.Contains("caller") && docLower.Contains("responsible");

        if (!hasDisposalDoc)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                method.Locations.FirstOrDefault(),
                method.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var property = (IPropertySymbol)context.Symbol;

        // Check if property type implements IDisposable
        if (!DisposableHelper.IsAnyDisposableType(property.Type))
            return;

        // Check for XML documentation
        var xmlDoc = property.GetDocumentationCommentXml();
        if (string.IsNullOrEmpty(xmlDoc))
        {
            // No documentation at all
            var diagnostic = Diagnostic.Create(
                Rule,
                property.Locations.FirstOrDefault(),
                property.Name);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Check if documentation mentions disposal, ownership, or caller responsibility
        var docLower = xmlDoc.ToLowerInvariant();
        var hasDisposalDoc = docLower.Contains("dispose") ||
                            docLower.Contains("ownership") ||
                            docLower.Contains("caller") && docLower.Contains("responsible");

        if (!hasDisposalDoc)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                property.Locations.FirstOrDefault(),
                property.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
