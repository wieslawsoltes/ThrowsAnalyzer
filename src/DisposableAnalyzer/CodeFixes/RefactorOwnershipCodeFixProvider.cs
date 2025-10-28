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
/// Code fix provider that refactors conditional ownership to clear ownership.
/// Fixes: DISP024
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RefactorOwnershipCodeFixProvider))]
[Shared]
public class RefactorOwnershipCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.ConditionalOwnership);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the variable declaration
        var node = root.FindNode(diagnosticSpan);
        var variableDeclarator = node.AncestorsAndSelf()
            .OfType<VariableDeclaratorSyntax>()
            .FirstOrDefault();

        if (variableDeclarator == null)
            return;

        // Offer to use using statement (unconditional disposal)
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use using statement for unconditional disposal",
                createChangedDocument: c => ConvertToUsingAsync(context.Document, variableDeclarator, root, c),
                equivalenceKey: "ConvertToUsing"),
            diagnostic);

        // Offer to add finally block for unconditional disposal
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Move disposal to finally block",
                createChangedDocument: c => MoveToFinallyAsync(context.Document, variableDeclarator, root, c),
                equivalenceKey: "MoveToFinally"),
            diagnostic);
    }

    private async Task<Document> ConvertToUsingAsync(
        Document document,
        VariableDeclaratorSyntax variableDeclarator,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        // Find the variable declaration statement
        var declarationStatement = variableDeclarator.AncestorsAndSelf()
            .OfType<LocalDeclarationStatementSyntax>()
            .FirstOrDefault();

        if (declarationStatement == null)
            return document;

        // Convert to using declaration (C# 8+)
        var usingDeclaration = declarationStatement
            .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(declarationStatement, usingDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    private async Task<Document> MoveToFinallyAsync(
        Document document,
        VariableDeclaratorSyntax variableDeclarator,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        var methodDeclaration = variableDeclarator.AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (methodDeclaration?.Body == null)
            return document;

        var declarationStatement = variableDeclarator.AncestorsAndSelf()
            .OfType<LocalDeclarationStatementSyntax>()
            .FirstOrDefault();

        if (declarationStatement == null)
            return document;

        var variableName = variableDeclarator.Identifier.Text;
        var methodBody = methodDeclaration.Body;

        // Find the index of the declaration
        var declarationIndex = methodBody.Statements.IndexOf(declarationStatement);
        if (declarationIndex < 0)
            return document;

        // Get all statements after declaration (these go in try)
        var tryStatements = methodBody.Statements.Skip(declarationIndex + 1).ToArray();

        // Create disposal statement for finally block
        var disposeStatement = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.ConditionalAccessExpression(
                SyntaxFactory.IdentifierName(variableName),
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberBindingExpression(
                        SyntaxFactory.IdentifierName("Dispose")))));

        // Create try-finally
        var finallyClause = SyntaxFactory.FinallyClause(
            SyntaxFactory.Block(disposeStatement));

        var tryFinally = SyntaxFactory.TryStatement()
            .WithBlock(SyntaxFactory.Block(tryStatements))
            .WithFinally(finallyClause)
            .WithAdditionalAnnotations(Formatter.Annotation);

        // Build new method body: statements before declaration + declaration + try-finally
        var newStatements = methodBody.Statements
            .Take(declarationIndex + 1)
            .Append(tryFinally)
            .ToArray();

        var newBody = SyntaxFactory.Block(newStatements);
        var newMethod = methodDeclaration.WithBody(newBody);

        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
        return document.WithSyntaxRoot(newRoot);
    }
}
