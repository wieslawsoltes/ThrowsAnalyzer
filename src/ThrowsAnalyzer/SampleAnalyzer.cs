using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer
{

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SampleAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "THROWS001";
    private const string Category = "Usage";

    private static readonly LocalizableString Title = "Method contains throw statement";
    private static readonly LocalizableString MessageFormat = "Method '{0}' contains throw statement(s)";
    private static readonly LocalizableString Description = "Detects methods that contain throw statements.";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

        if (MethodThrowAnalyzer.HasThrowStatements(methodDeclaration))
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.Text);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
}