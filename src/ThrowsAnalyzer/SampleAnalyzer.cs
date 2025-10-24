using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

        if (HasThrowStatements(methodDeclaration))
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.Text);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool HasThrowStatements(MethodDeclarationSyntax methodDeclaration)
    {
        var nodesToCheck = GetNodesToAnalyze(methodDeclaration);

        foreach (var node in nodesToCheck)
        {
            if (ContainsThrowSyntax(node))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<SyntaxNode> GetNodesToAnalyze(MethodDeclarationSyntax methodDeclaration)
    {
        if (methodDeclaration.Body != null)
        {
            yield return methodDeclaration.Body;
        }

        if (methodDeclaration.ExpressionBody != null)
        {
            yield return methodDeclaration.ExpressionBody;
        }
    }

    private static bool ContainsThrowSyntax(SyntaxNode node)
    {
        return node.DescendantNodes().Any(n => n is ThrowStatementSyntax or ThrowExpressionSyntax);
    }
}
}