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
        var message = diagnostic.GetMessage();
        if (message.Contains("Missing"))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: TitleAdd,
                    createChangedDocument: c => AddSuppressFinalizeAsync(context.Document, methodDeclaration, c),
                    equivalenceKey: TitleAdd),
                diagnostic);
        }
        else if (message.Contains("Unnecessary"))
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

        // Add GC.SuppressFinalize(this) as the last statement
        var suppressStatement = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("GC"),
                    SyntaxFactory.IdentifierName("SuppressFinalize")))
            .AddArgumentListArguments(
                SyntaxFactory.Argument(SyntaxFactory.ThisExpression())
            )
        );

        var newBody = methodDeclaration.Body.AddStatements(suppressStatement);
        var newMethod = methodDeclaration.WithBody(newBody);

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
}
