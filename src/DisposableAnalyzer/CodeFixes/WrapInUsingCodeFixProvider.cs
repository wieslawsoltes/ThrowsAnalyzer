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
/// Code fix provider that wraps disposable variables in using statements.
/// Fixes: DISP001, DISP004
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WrapInUsingCodeFixProvider))]
[Shared]
public class WrapInUsingCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.UndisposedLocal, DiagnosticIds.MissingUsingStatement);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the variable declaration
        var declaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<VariableDeclaratorSyntax>()
            .FirstOrDefault();

        if (declaration == null)
            return;

        // Register code fixes
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Wrap in using statement",
                createChangedDocument: c => WrapInUsingStatementAsync(context.Document, declaration, c),
                equivalenceKey: "WrapInUsingStatement"),
            diagnostic);

        // Check if C# 8+ for using declaration
        var tree = await context.Document.GetSyntaxTreeAsync(context.CancellationToken).ConfigureAwait(false);
        if (tree != null)
        {
            var languageVersion = ((CSharpParseOptions)tree.Options).LanguageVersion;
            if (languageVersion >= LanguageVersion.CSharp8)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Use using declaration",
                        createChangedDocument: c => UseUsingDeclarationAsync(context.Document, declaration, c),
                        equivalenceKey: "UseUsingDeclaration"),
                    diagnostic);
            }
        }
    }

    private async Task<Document> WrapInUsingStatementAsync(
        Document document,
        VariableDeclaratorSyntax declarator,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Find the local declaration statement
        var localDeclaration = declarator.Ancestors()
            .OfType<LocalDeclarationStatementSyntax>()
            .FirstOrDefault();

        if (localDeclaration == null)
            return document;

        // Find the containing block and the statements after this declaration
        var block = localDeclaration.Parent as BlockSyntax;
        if (block == null)
            return document;

        var declarationIndex = block.Statements.IndexOf(localDeclaration);
        if (declarationIndex == -1)
            return document;

        // Take all statements after the declaration
        var remainingStatements = block.Statements
            .Skip(declarationIndex + 1)
            .ToList();

        // Create using statement
        var declaration = localDeclaration.Declaration.WithoutLeadingTrivia();

        var usingStatement = SyntaxFactory.UsingStatement(
            declaration: declaration,
            expression: null,
            statement: SyntaxFactory.Block(remainingStatements))
            .WithLeadingTrivia(localDeclaration.GetLeadingTrivia())
            .WithTrailingTrivia(localDeclaration.GetTrailingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation);

        // Create new block with using statement
        var newStatements = block.Statements
            .Take(declarationIndex)
            .Append(usingStatement)
            .ToList();

    var newBlock = block.WithStatements(SyntaxFactory.List(newStatements));

    var newRoot = root.ReplaceNode(block, newBlock);
    var formattedRoot = Formatter.Format(newRoot, Formatter.Annotation, document.Project.Solution.Workspace);
    return document.WithSyntaxRoot(formattedRoot);
    }

    private async Task<Document> UseUsingDeclarationAsync(
        Document document,
        VariableDeclaratorSyntax declarator,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Find the local declaration statement
        var localDeclaration = declarator.Ancestors()
            .OfType<LocalDeclarationStatementSyntax>()
            .FirstOrDefault();

        if (localDeclaration == null)
            return document;

        // Add 'using' modifier to the declaration
        var usingDeclaration = localDeclaration
            .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space))
            .WithLeadingTrivia(localDeclaration.GetLeadingTrivia())
            .WithTrailingTrivia(localDeclaration.GetTrailingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(localDeclaration, usingDeclaration);
        var formattedRoot = Formatter.Format(newRoot, Formatter.Annotation, document.Project.Solution.Workspace);
        return document.WithSyntaxRoot(formattedRoot);
    }
}
