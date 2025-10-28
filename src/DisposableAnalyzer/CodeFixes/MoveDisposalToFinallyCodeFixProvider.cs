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

        // Get all statements after the variable declaration
        var methodBody = methodDeclaration.Body;
        var declarationIndex = methodBody.Statements.IndexOf(declarationStatement);

        if (declarationIndex < 0)
            return document;

        // Statements to wrap in try block (everything after declaration)
        var tryStatements = methodBody.Statements.Skip(declarationIndex + 1).ToArray();

        // Create disposal statement: variableName?.Dispose();
        var variableName = variableDeclarator.Identifier.Text;
        var disposeStatement = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.ConditionalAccessExpression(
                SyntaxFactory.IdentifierName(variableName),
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberBindingExpression(
                        SyntaxFactory.IdentifierName("Dispose")))));

        // Create finally block with disposal
        var finallyBlock = SyntaxFactory.FinallyClause(
            SyntaxFactory.Block(disposeStatement));

        // Create try-finally statement
        var tryFinally = SyntaxFactory.TryStatement()
            .WithBlock(SyntaxFactory.Block(tryStatements))
            .WithFinally(finallyBlock)
            .WithAdditionalAnnotations(Formatter.Annotation);

        // Build new method body: keep statements up to and including declaration, then add try-finally
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
