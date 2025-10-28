using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DisposableAnalyzer.Analyzers;

/// <summary>
/// Analyzer that recommends using declarations (C# 8+) over traditional using statements.
/// DISP006: Use using declaration (C# 8+)
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UsingDeclarationRecommendedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.UsingDeclarationRecommended,
        title: "Use using declaration (C# 8+)",
        messageFormat: "Consider using a using declaration instead of a using statement for cleaner code",
        category: "Style",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Using declarations (using var x = ...) provide cleaner code than traditional using statements when C# 8.0 or later is available.");

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

        // Check if language version supports using declarations (C# 8.0+)
        var languageVersion = ((CSharpParseOptions)context.Node.SyntaxTree.Options).LanguageVersion;
        if (languageVersion < LanguageVersion.CSharp8)
            return;

        // Only suggest for using statements with declarations (not expressions)
        if (usingStatement.Declaration == null)
            return;

        // Don't suggest if the using statement has multiple variables
        if (usingStatement.Declaration.Variables.Count > 1)
            return;

        // Don't suggest if the statement body is complex (multiple statements)
        if (usingStatement.Statement is BlockSyntax block && block.Statements.Count > 5)
            return;

        // Don't suggest if there are nested using statements (better to keep them as statements)
        if (HasNestedUsing(usingStatement.Statement))
            return;

        // Don't suggest if this using statement is part of a chain (parent is also a using)
        if (usingStatement.Parent is UsingStatementSyntax)
            return;

        // Don't suggest if using statement is inside a try-catch block
        if (IsInTryCatchBlock(usingStatement))
            return;

        // Don't suggest if using statement is inside a conditional block (if, else, etc.)
        if (IsInConditionalBlock(usingStatement))
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            usingStatement.UsingKeyword.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private bool HasNestedUsing(StatementSyntax statement)
    {
        if (statement is UsingStatementSyntax)
            return true;

        if (statement is BlockSyntax block)
        {
            foreach (var innerStatement in block.Statements)
            {
                if (innerStatement is UsingStatementSyntax)
                    return true;
            }
        }

        return false;
    }

    private bool IsInTryCatchBlock(UsingStatementSyntax usingStatement)
    {
        var parent = usingStatement.Parent;
        while (parent != null)
        {
            if (parent is TryStatementSyntax)
                return true;

            // Stop searching at method/type boundaries
            if (parent is MemberDeclarationSyntax)
                break;

            parent = parent.Parent;
        }
        return false;
    }

    private bool IsInConditionalBlock(UsingStatementSyntax usingStatement)
    {
        var parent = usingStatement.Parent;

        // Check immediate parent
        if (parent is IfStatementSyntax ||
            parent is ElseClauseSyntax ||
            parent is WhileStatementSyntax ||
            parent is ForStatementSyntax ||
            parent is ForEachStatementSyntax)
        {
            return true;
        }

        // Check if inside a block that's part of a conditional
        if (parent is BlockSyntax block)
        {
            var blockParent = block.Parent;
            if (blockParent is IfStatementSyntax ||
                blockParent is ElseClauseSyntax ||
                blockParent is WhileStatementSyntax ||
                blockParent is ForStatementSyntax ||
                blockParent is ForEachStatementSyntax)
            {
                return true;
            }
        }

        return false;
    }
}
