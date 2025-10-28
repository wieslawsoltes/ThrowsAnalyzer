using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace DisposableAnalyzer.CodeFixes;

/// <summary>
/// Code fix provider that narrows using statement scope.
/// Fixes: DISP005
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NarrowUsingScopeCodeFixProvider))]
[Shared]
public class NarrowUsingScopeCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.UsingStatementScopeToBroad);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the using statement
        var usingStatement = root.FindNode(diagnosticSpan)
            .AncestorsAndSelf()
            .OfType<UsingStatementSyntax>()
            .FirstOrDefault();

        if (usingStatement == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Narrow using statement scope",
                createChangedDocument: c => NarrowScopeAsync(context.Document, usingStatement, root, c),
                equivalenceKey: "NarrowUsingScope"),
            diagnostic);
    }

    private async Task<Document> NarrowScopeAsync(
        Document document,
        UsingStatementSyntax usingStatement,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        // Get the variable declared in the using statement
        if (usingStatement.Declaration == null)
            return document;

        var variable = usingStatement.Declaration.Variables.FirstOrDefault();
        if (variable == null)
            return document;

        var variableName = variable.Identifier.Text;

        // Find all usages of the variable within the using block
        var usingBlock = usingStatement.Statement as BlockSyntax;
        if (usingBlock == null)
            return document;

        // Find the last statement that uses the variable
        var lastUsageIndex = -1;
        for (int i = 0; i < usingBlock.Statements.Count; i++)
        {
            var statement = usingBlock.Statements[i];
            if (StatementUsesVariable(statement, variableName))
            {
                lastUsageIndex = i;
            }
        }

        if (lastUsageIndex < 0 || lastUsageIndex >= usingBlock.Statements.Count - 1)
        {
            // Variable used throughout or not at all - can't narrow
            return document;
        }

        // Split the block: statements up to and including last usage in new using,
        // remaining statements outside
        var statementsInUsing = usingBlock.Statements.Take(lastUsageIndex + 1).ToArray();
        var statementsAfterUsing = usingBlock.Statements.Skip(lastUsageIndex + 1).ToArray();

        // Create new narrowed using statement
        var newUsingBlock = SyntaxFactory.Block(statementsInUsing);
        var newUsing = usingStatement.WithStatement(newUsingBlock);

        // Create new block with narrowed using + remaining statements
        var newStatements = new[] { newUsing }.Concat(statementsAfterUsing);
        var newBlock = SyntaxFactory.Block(newStatements)
            .WithAdditionalAnnotations(Formatter.Annotation);

        // Find parent block and replace
        var parentBlock = usingStatement.Parent as BlockSyntax;
        if (parentBlock != null)
        {
            var usingIndex = parentBlock.Statements.IndexOf(usingStatement);
            if (usingIndex >= 0)
            {
                // Replace using statement with new block
                var newParentStatements = parentBlock.Statements
                    .RemoveAt(usingIndex)
                    .InsertRange(usingIndex, newBlock.Statements);

                var newParentBlock = parentBlock.WithStatements(
                    SyntaxFactory.List(newParentStatements));

                var newRoot = root.ReplaceNode(parentBlock, newParentBlock);
                return document.WithSyntaxRoot(newRoot);
            }
        }

        return document;
    }

    private bool StatementUsesVariable(StatementSyntax statement, string variableName)
    {
        // Simple check: does the statement contain an identifier with the variable name
        return statement.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Any(id => id.Identifier.Text == variableName);
    }
}
