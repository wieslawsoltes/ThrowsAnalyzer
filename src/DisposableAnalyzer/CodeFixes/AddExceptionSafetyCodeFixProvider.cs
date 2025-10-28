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
/// Code fix that adds try-finally blocks for exception safety in constructors.
/// Fixes DISP018: DisposableInConstructor
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddExceptionSafetyCodeFixProvider))]
[Shared]
public class AddExceptionSafetyCodeFixProvider : CodeFixProvider
{
    private const string Title = "Wrap in try-finally for exception safety";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DisposableInConstructor);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var localDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<LocalDeclarationStatementSyntax>()
            .FirstOrDefault();

        if (localDeclaration == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => AddTryFinallyAsync(context.Document, localDeclaration, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private async Task<Document> AddTryFinallyAsync(
        Document document,
        LocalDeclarationStatementSyntax localDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        // Get the variable name
        var variable = localDeclaration.Declaration.Variables.FirstOrDefault();
        if (variable == null) return document;

        var variableName = variable.Identifier.Text;

        // Find constructor body
        var constructor = localDeclaration.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
        if (constructor?.Body == null) return document;

        // Get statements after the local declaration
        var statements = constructor.Body.Statements;
        var declarationIndex = statements.IndexOf(localDeclaration);
        if (declarationIndex == -1) return document;

        // Create try-finally block
        var tryBlock = SyntaxFactory.Block(
            statements.Skip(declarationIndex + 1)
        );

        var finallyBlock = SyntaxFactory.FinallyClause(
            SyntaxFactory.Block(
                SyntaxFactory.ParseStatement($"{variableName}?.Dispose();")
            )
        );

        var tryFinally = SyntaxFactory.TryStatement(tryBlock, default, finallyBlock);

        // Rebuild constructor body
        var newStatements = statements.Take(declarationIndex + 1)
            .Append(tryFinally)
            .ToList();

        var newBody = constructor.Body.WithStatements(
            SyntaxFactory.List(newStatements)
        );

        var newConstructor = constructor.WithBody(newBody);
        var newRoot = root.ReplaceNode(constructor, newConstructor);

        return document.WithSyntaxRoot(newRoot);
    }
}
