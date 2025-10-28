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
/// Code fix provider that removes redundant disposal calls or adds null checks.
/// Fixes: DISP003
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveDoubleDisposeCodeFixProvider))]
[Shared]
public class RemoveDoubleDisposeCodeFixProvider : CodeFixProvider
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

        // Find the disposal invocation
        var invocation = root.FindNode(diagnosticSpan)
            .AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

        if (invocation == null)
            return;

        // Register two options: remove or add null check
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Remove redundant Dispose call",
                createChangedDocument: c => RemoveDisposeCallAsync(context.Document, invocation, root, c),
                equivalenceKey: "RemoveDisposeCall"),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add null check before Dispose",
                createChangedDocument: c => AddNullCheckAsync(context.Document, invocation, root, c),
                equivalenceKey: "AddNullCheckBeforeDispose"),
            diagnostic);
    }

    private async Task<Document> RemoveDisposeCallAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        // Find the statement containing the invocation
        var statement = invocation.AncestorsAndSelf()
            .OfType<StatementSyntax>()
            .FirstOrDefault();

        if (statement == null)
            return document;

        // Remove the entire statement
        var newRoot = root.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
        if (newRoot == null)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }

    private async Task<Document> AddNullCheckAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        // Find the statement containing the invocation
        var statement = invocation.AncestorsAndSelf()
            .OfType<ExpressionStatementSyntax>()
            .FirstOrDefault();

        if (statement == null)
            return document;

        // Get the variable being disposed
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return document;

        var variableExpression = memberAccess.Expression;

        // Create null-conditional disposal: variable?.Dispose();
        var nullConditionalDispose = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.ConditionalAccessExpression(
                variableExpression,
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberBindingExpression(
                        SyntaxFactory.IdentifierName("Dispose")))))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(statement, nullConditionalDispose);
        return document.WithSyntaxRoot(newRoot);
    }
}
