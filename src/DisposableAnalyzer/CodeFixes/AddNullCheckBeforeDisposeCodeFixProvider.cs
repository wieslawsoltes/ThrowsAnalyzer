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

namespace DisposableAnalyzer.CodeFixes;

/// <summary>
/// Code fix provider that adds null checks before disposal to prevent double disposal.
/// Fixes: DISP003
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddNullCheckBeforeDisposeCodeFixProvider))]
[Shared]
public class AddNullCheckBeforeDisposeCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DoubleDispose);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the invocation expression (Dispose call)
        var invocation = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

        if (invocation == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use null-conditional operator (?.)",
                createChangedDocument: c => UseNullConditionalAsync(context.Document, invocation, c),
                equivalenceKey: "UseNullConditional"),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add null check before disposal",
                createChangedDocument: c => AddNullCheckAsync(context.Document, invocation, c),
                equivalenceKey: "AddNullCheck"),
            diagnostic);
    }

    private async Task<Document> UseNullConditionalAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Convert obj.Dispose() to obj?.Dispose()
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var conditionalAccess = SyntaxFactory.ConditionalAccessExpression(
                memberAccess.Expression,
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberBindingExpression(memberAccess.Name)));

            var newRoot = root.ReplaceNode(invocation, conditionalAccess);
            return document.WithSyntaxRoot(newRoot);
        }

        return document;
    }

    private async Task<Document> AddNullCheckAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Find the statement containing the invocation
        var statement = invocation.Ancestors()
            .OfType<StatementSyntax>()
            .FirstOrDefault();

        if (statement == null)
            return document;

        // Get the object being disposed
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return document;

        var objectExpression = memberAccess.Expression;

        // Create if (obj != null) { obj.Dispose(); }
        var condition = SyntaxFactory.BinaryExpression(
            SyntaxKind.NotEqualsExpression,
            objectExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

        var ifStatement = SyntaxFactory.IfStatement(
            condition,
            SyntaxFactory.Block(statement))
            .WithLeadingTrivia(statement.GetLeadingTrivia())
            .WithTrailingTrivia(statement.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(statement, ifStatement);
        return document.WithSyntaxRoot(newRoot);
    }
}
