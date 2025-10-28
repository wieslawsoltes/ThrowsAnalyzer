using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using DisposableAnalyzer.Helpers;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that detects using statements where the scope is too broad.
/// DISP005: Using statement scope too broad
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UsingStatementScopeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.UsingStatementScopeToBroad,
        title: "Using statement scope too broad",
        messageFormat: "Using statement for '{0}' has unnecessarily broad scope. Resource is only used in the first {1} of {2} statements",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Using statements should have minimal scope to release resources as early as possible. Consider narrowing the scope when resources are only used in a small portion of the using block.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeUsingStatement, SyntaxKind.UsingStatement);
    }

    private void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context)
    {
        var usingStatement = (UsingStatementSyntax)context.Node;

        // Only analyze using statements with blocks
        if (usingStatement.Statement is not BlockSyntax block)
            return;

        // Need at least 3 statements to consider scope narrowing worthwhile
        if (block.Statements.Count < 3)
            return;

        // Get the variable declared in the using statement
        if (usingStatement.Declaration == null)
            return;

        var variables = usingStatement.Declaration.Variables;
        if (variables.Count == 0)
            return;

        // Analyze each variable
        foreach (var variable in variables)
        {
            AnalyzeVariableScope(context, usingStatement, block, variable);
        }
    }

    private void AnalyzeVariableScope(
        SyntaxNodeAnalysisContext context,
        UsingStatementSyntax usingStatement,
        BlockSyntax block,
        VariableDeclaratorSyntax variable)
    {
        var variableName = variable.Identifier.Text;

        // Find all usages of the variable within the block
        var usageIndices = new System.Collections.Generic.List<int>();

        for (int i = 0; i < block.Statements.Count; i++)
        {
            var statement = block.Statements[i];
            if (StatementUsesVariable(statement, variableName))
            {
                usageIndices.Add(i);
            }
        }

        // If variable is not used at all, that's a different issue (covered by other analyzers)
        if (usageIndices.Count == 0)
            return;

        // Find the last statement index where the variable is used
        var lastUsageIndex = usageIndices.Max();

        // Calculate how much of the block actually uses the resource
        var totalStatements = block.Statements.Count;
        var statementsUsingResource = lastUsageIndex + 1;
        var unusedStatements = totalStatements - statementsUsingResource;

        // Only report if:
        // 1. There are at least 2 statements after the last usage
        // 2. At least 40% of statements don't use the resource
        if (unusedStatements >= 2 && (unusedStatements * 100.0 / totalStatements) >= 40)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                usingStatement.UsingKeyword.GetLocation(),
                variableName,
                statementsUsingResource,
                totalStatements);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private bool StatementUsesVariable(StatementSyntax statement, string variableName)
    {
        // Check all identifiers in the statement
        var identifiers = statement.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(id => id.Identifier.Text == variableName);

        return identifiers.Any();
    }
}
