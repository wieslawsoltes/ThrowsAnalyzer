using System.Collections.Generic;
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
/// Code fix provider that moves disposal to finally block to ensure disposal on all paths.
/// Fixes: DISP025
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveDisposalToFinallyCodeFixProvider))]
[Shared]
public class MoveDisposalToFinallyCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DisposalInAllPaths);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the local variable declaration
        var node = root.FindNode(diagnosticSpan);
        var variableDeclarator = node.AncestorsAndSelf()
            .OfType<VariableDeclaratorSyntax>()
            .FirstOrDefault();

        if (variableDeclarator == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Wrap in try-finally with disposal",
                createChangedDocument: c => WrapInTryFinallyAsync(context.Document, variableDeclarator, root, c),
                equivalenceKey: "WrapInTryFinally"),
            diagnostic);
    }

    private async Task<Document> WrapInTryFinallyAsync(
        Document document,
        VariableDeclaratorSyntax variableDeclarator,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        // Find the containing method
        var methodDeclaration = variableDeclarator.AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (methodDeclaration?.Body == null)
            return document;

        // Find the variable declaration statement
        var declarationStatement = variableDeclarator.AncestorsAndSelf()
            .OfType<LocalDeclarationStatementSyntax>()
            .FirstOrDefault();

        if (declarationStatement == null)
            return document;

        var methodBody = methodDeclaration.Body;
        var declarationIndex = methodBody.Statements.IndexOf(declarationStatement);
        if (declarationIndex < 0)
            return document;

        var variableName = variableDeclarator.Identifier.Text;
        var disposeStatement = CreateDisposeStatement(variableName);

        var tryStatement = methodBody.Statements
            .Skip(declarationIndex + 1)
            .OfType<TryStatementSyntax>()
            .FirstOrDefault();

        // If no try exists, wrap subsequent statements in try/finally
        if (tryStatement == null)
        {
            var trailingStatements = methodBody.Statements.Skip(declarationIndex + 1).ToList();
            if (!trailingStatements.Any())
                return document;

            var newTry = SyntaxFactory.TryStatement()
                .WithBlock(SyntaxFactory.Block(trailingStatements))
                .WithFinally(SyntaxFactory.FinallyClause(SyntaxFactory.Block(disposeStatement)))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var wrappedStatements = methodBody.Statements
                .Take(declarationIndex + 1)
                .Concat(new[] { newTry })
                .ToArray();

            var updatedBody = SyntaxFactory.Block(wrappedStatements);
            var updatedMethod = methodDeclaration.WithBody(updatedBody);
            var updatedRoot = root.ReplaceNode(methodDeclaration, updatedMethod);
            return document.WithSyntaxRoot(updatedRoot);
        }

        var statements = methodBody.Statements;
        var tryIndex = -1;
        for (var i = 0; i < statements.Count; i++)
        {
            if (statements[i].Span == tryStatement.Span)
            {
                tryIndex = i;
                break;
            }
        }

        if (tryIndex < 0)
            return document;

        var cleanedTry = CleanCatchClauses(tryStatement, variableName);

        var tryWithFinally = cleanedTry.Finally == null
            ? cleanedTry.WithFinally(
                SyntaxFactory.FinallyClause(
                    SyntaxFactory.Block(disposeStatement)))
            : cleanedTry.WithFinally(
                cleanedTry.Finally!.WithBlock(cleanedTry.Finally.Block.AddStatements(disposeStatement)));

        var updatedStatements = new List<StatementSyntax>(statements.Count);
        for (var i = 0; i < statements.Count; i++)
        {
            if (i == tryIndex)
            {
                updatedStatements.Add(tryWithFinally.WithAdditionalAnnotations(Formatter.Annotation));
                continue;
            }

            var statement = statements[i];
            if (i > tryIndex && IsDisposeStatement(statement, variableName))
                continue;

            updatedStatements.Add(statement);
        }

        var newBody = methodBody.WithStatements(SyntaxFactory.List(updatedStatements));
        var newMethod = methodDeclaration.WithBody(newBody)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
        return document.WithSyntaxRoot(newRoot);
    }

    private static ExpressionStatementSyntax CreateDisposeStatement(string variableName)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.ConditionalAccessExpression(
                SyntaxFactory.IdentifierName(variableName),
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberBindingExpression(
                        SyntaxFactory.IdentifierName("Dispose")))))
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    private static bool IsDisposeStatement(StatementSyntax statement, string variableName)
    {
        if (statement is not ExpressionStatementSyntax exprStmt)
            return false;

        return IsDisposeInvocation(exprStmt.Expression, variableName);
    }

    private static bool IsDisposeInvocation(ExpressionSyntax expression, string variableName)
    {
        switch (expression)
        {
            case ConditionalAccessExpressionSyntax conditional
                when conditional.Expression is IdentifierNameSyntax identifier &&
                     identifier.Identifier.Text == variableName &&
                     conditional.WhenNotNull is InvocationExpressionSyntax invocation &&
                     invocation.Expression is MemberBindingExpressionSyntax memberBinding &&
                     memberBinding.Name.Identifier.Text == "Dispose":
                return true;

            case InvocationExpressionSyntax invocation
                when invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                     memberAccess.Expression is IdentifierNameSyntax identifierName &&
                     identifierName.Identifier.Text == variableName &&
                     memberAccess.Name.Identifier.Text == "Dispose":
                return true;
        }

        return false;
    }

    private static TryStatementSyntax CleanCatchClauses(TryStatementSyntax tryStatement, string variableName)
    {
        if (!tryStatement.Catches.Any())
            return tryStatement;

        var updatedCatches = new List<CatchClauseSyntax>();
        foreach (var catchClause in tryStatement.Catches)
        {
            if (catchClause.Block == null)
            {
                updatedCatches.Add(catchClause);
                continue;
            }

            var remainingStatements = catchClause.Block.Statements
                .Where(stmt => !IsDisposeStatement(stmt, variableName))
                .ToList();

            if (remainingStatements.Count == catchClause.Block.Statements.Count)
            {
                updatedCatches.Add(catchClause);
                continue;
            }

            var newBlock = catchClause.Block.WithStatements(SyntaxFactory.List(remainingStatements));
            updatedCatches.Add(catchClause.WithBlock(newBlock));
        }

        return tryStatement.WithCatches(SyntaxFactory.List(updatedCatches));
    }
}
