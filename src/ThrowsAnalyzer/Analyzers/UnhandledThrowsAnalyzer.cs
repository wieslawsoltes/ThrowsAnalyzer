using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer
{

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnhandledThrowsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.MethodContainsUnhandledThrow);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register syntax node action for method declarations
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Use composable detector
        if (UnhandledThrowDetector.HasUnhandledThrows(methodDeclaration))
        {
            var diagnostic = Diagnostic.Create(
                MethodThrowsDiagnosticsBuilder.MethodContainsUnhandledThrow,
                methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.Text);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
}
