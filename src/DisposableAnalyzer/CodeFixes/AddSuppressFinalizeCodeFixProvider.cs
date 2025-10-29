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
using DisposableAnalyzer.Analyzers;

namespace DisposableAnalyzer.CodeFixes;

/// <summary>
/// Code fix that adds or removes GC.SuppressFinalize calls based on finalizer presence.
/// Fixes DISP030: SuppressFinalizerPerformance
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddSuppressFinalizeCodeFixProvider))]
[Shared]
public class AddSuppressFinalizeCodeFixProvider : CodeFixProvider
{
    private const string TitleAdd = "Add GC.SuppressFinalize(this)";
    private const string TitleRemove = "Remove unnecessary GC.SuppressFinalize";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.SuppressFinalizerPerformance);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var methodDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (methodDeclaration == null) return;

        // Check diagnostic message to determine if we need to add or remove
        if (diagnostic.Descriptor == SuppressFinalizerPerformanceAnalyzer.MissingCallRule)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: TitleAdd,
                    createChangedDocument: c => AddSuppressFinalizeAsync(context.Document, methodDeclaration, c),
                    equivalenceKey: TitleAdd),
                diagnostic);
        }
        else if (diagnostic.Descriptor == SuppressFinalizerPerformanceAnalyzer.UnnecessaryCallRule)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: TitleRemove,
                    createChangedDocument: c => RemoveSuppressFinalizeAsync(context.Document, methodDeclaration, c),
                    equivalenceKey: TitleRemove),
                diagnostic);
        }
    }

    private async Task<Document> AddSuppressFinalizeAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        if (methodDeclaration.Body == null) return document;

        if (ContainsSuppressFinalizeCall(methodDeclaration))
            return document;

        var body = methodDeclaration.Body;
        var statements = body.Statements;

        var statementIndent = GetIndentation(statements.LastOrDefault()) ?? "        ";
        var braceIndent = GetIndentation(body.CloseBraceToken) ?? "    ";

        SyntaxTrivia? commentIndentTrivia = null;
        SyntaxTrivia? commentTrivia = null;
        SyntaxTrivia? commentEndOfLineTrivia = null;

        if (statements.Count > 0)
        {
            var lastStatement = statements.Last();
            var trailingTrivia = lastStatement.GetTrailingTrivia();
            var commentIndex = IndexOfComment(trailingTrivia);
            if (commentIndex >= 0)
            {
                if (commentIndex > 0 && trailingTrivia[commentIndex - 1].IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    commentIndentTrivia ??= trailingTrivia[commentIndex - 1];
                }

                commentTrivia = trailingTrivia[commentIndex];

                var nextIndex = commentIndex + 1;
                if (nextIndex < trailingTrivia.Count && trailingTrivia[nextIndex].IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    commentEndOfLineTrivia ??= trailingTrivia[nextIndex];
                }

                var builder = ImmutableArray.CreateBuilder<SyntaxTrivia>();
                for (int i = 0; i < trailingTrivia.Count; i++)
                {
                    if (i == commentIndex)
                        continue;

                    if (commentIndentTrivia.HasValue && i == commentIndex - 1)
                        continue;

                    if (commentEndOfLineTrivia.HasValue && i == nextIndex)
                        continue;

                    builder.Add(trailingTrivia[i]);
                }

                statements = statements.Replace(
                    lastStatement,
                    lastStatement.WithTrailingTrivia(SyntaxFactory.TriviaList(builder.ToImmutable())));
            }
        }

        if (commentTrivia is null)
        {
            var closeBrace = body.CloseBraceToken;
            var leadingTrivia = closeBrace.LeadingTrivia;
            var commentIndex = IndexOfComment(leadingTrivia);
            if (commentIndex >= 0)
            {
                if (commentIndex > 0 && leadingTrivia[commentIndex - 1].IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    commentIndentTrivia ??= leadingTrivia[commentIndex - 1];
                }

                commentTrivia = leadingTrivia[commentIndex];

                SyntaxTrivia? potentialEndOfLine = null;
                var nextIndex = commentIndex + 1;
                if (nextIndex < leadingTrivia.Count && leadingTrivia[nextIndex].IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    potentialEndOfLine = leadingTrivia[nextIndex];
                }

                if (!commentEndOfLineTrivia.HasValue && potentialEndOfLine.HasValue)
                {
                    commentEndOfLineTrivia = potentialEndOfLine;
                }

                var builder = ImmutableArray.CreateBuilder<SyntaxTrivia>();
                for (int i = 0; i < leadingTrivia.Count; i++)
                {
                    if (i == commentIndex)
                        continue;

                    if (commentIndentTrivia.HasValue && i == commentIndex - 1)
                        continue;

                    if (potentialEndOfLine.HasValue && i == nextIndex)
                        continue;

                    builder.Add(leadingTrivia[i]);
                }

                body = body.WithCloseBraceToken(closeBrace.WithLeadingTrivia(SyntaxFactory.TriviaList(builder.ToImmutable())));
            }
        }

        var indentTrivia = SyntaxFactory.Whitespace(statementIndent);
        var leadingTriviaBuilder = ImmutableArray.CreateBuilder<SyntaxTrivia>();

        if (commentTrivia is SyntaxTrivia commentTriviaValue)
        {
            var needsLeadingNewline = statements.Count == 0 ||
                                      !EndsWithNewLine(statements.Last().GetTrailingTrivia());

            if (needsLeadingNewline)
            {
                leadingTriviaBuilder.Add(SyntaxFactory.CarriageReturnLineFeed);
            }

            var commentIndent = commentIndentTrivia ?? indentTrivia;
            leadingTriviaBuilder.Add(commentIndent);
            leadingTriviaBuilder.Add(commentTriviaValue);

            var newlineTrivia = commentEndOfLineTrivia ?? SyntaxFactory.CarriageReturnLineFeed;
            leadingTriviaBuilder.Add(newlineTrivia);

            leadingTriviaBuilder.Add(indentTrivia);
        }
        else
        {
            leadingTriviaBuilder.Add(SyntaxFactory.CarriageReturnLineFeed);
            leadingTriviaBuilder.Add(indentTrivia);
        }

        var suppressStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("GC"),
                            SyntaxFactory.IdentifierName("SuppressFinalize")))
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.ThisExpression())))
            .WithLeadingTrivia(SyntaxFactory.TriviaList(leadingTriviaBuilder.ToImmutable()))
            .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed));

        statements = statements.Add(suppressStatement);
        body = body.WithStatements(statements)
            .WithCloseBraceToken(body.CloseBraceToken.WithLeadingTrivia(
                SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(braceIndent))));

        var newMethod = methodDeclaration.WithBody(body);
        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
        return document.WithSyntaxRoot(newRoot);
    }

    private async Task<Document> RemoveSuppressFinalizeAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        if (methodDeclaration.Body == null) return document;

        // Find and remove GC.SuppressFinalize statements
        var statementsToRemove = methodDeclaration.Body.Statements
            .OfType<ExpressionStatementSyntax>()
            .Where(stmt =>
            {
                if (stmt.Expression is InvocationExpressionSyntax invocation)
                {
                    if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                    {
                        return memberAccess.Expression is IdentifierNameSyntax identifier &&
                               identifier.Identifier.Text == "GC" &&
                               memberAccess.Name.Identifier.Text == "SuppressFinalize";
                    }
                }
                return false;
            })
            .ToList();

        if (statementsToRemove.Count == 0) return document;

        var newStatements = methodDeclaration.Body.Statements;
        foreach (var stmt in statementsToRemove)
        {
            newStatements = newStatements.Remove(stmt);
        }

        var newBody = methodDeclaration.Body.WithStatements(newStatements);
        var newMethod = methodDeclaration.WithBody(newBody);

        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool ContainsSuppressFinalizeCall(MethodDeclarationSyntax method)
    {
        return method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(IsSuppressFinalizeInvocation);
    }

    private static bool IsSuppressFinalizeInvocation(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is IdentifierNameSyntax identifier &&
            identifier.Identifier.Text == "GC" &&
            memberAccess.Name.Identifier.Text == "SuppressFinalize" &&
            invocation.ArgumentList.Arguments.Count == 1 &&
            invocation.ArgumentList.Arguments[0].Expression is ThisExpressionSyntax)
        {
            return true;
        }

        return false;
    }

    private static int IndexOfComment(SyntaxTriviaList triviaList)
    {
        for (int i = 0; i < triviaList.Count; i++)
        {
            if (triviaList[i].IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool EndsWithNewLine(SyntaxTriviaList trivia)
    {
        for (int i = trivia.Count - 1; i >= 0; i--)
        {
            if (trivia[i].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return true;
            }

            if (!trivia[i].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                break;
            }
        }

        return false;
    }

    private static string? GetIndentation(SyntaxNode? node)
    {
        if (node == null)
            return null;

        foreach (var trivia in node.GetLeadingTrivia())
        {
            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                return trivia.ToString();
        }

        return null;
    }

    private static string? GetIndentation(SyntaxToken token)
    {
        foreach (var trivia in token.LeadingTrivia)
        {
            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                return trivia.ToString();
        }

        return null;
    }
}
