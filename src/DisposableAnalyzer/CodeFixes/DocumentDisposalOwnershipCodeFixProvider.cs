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
/// Code fix that adds XML documentation comment to clarify disposal ownership.
/// Fixes DISP016: DisposableReturned
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DocumentDisposalOwnershipCodeFixProvider))]
[Shared]
public class DocumentDisposalOwnershipCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add disposal ownership documentation";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DisposableReturned);

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

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => AddDisposalDocumentationAsync(context.Document, methodDeclaration, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private async Task<Document> AddDisposalDocumentationAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        // Create XML documentation comment
        var docComment = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List(new XmlNodeSyntax[]
            {
                SyntaxFactory.XmlText("/// "),
                SyntaxFactory.XmlElement(
                    SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName("returns")),
                    SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName("returns")))
                    .WithContent(SyntaxFactory.SingletonList<XmlNodeSyntax>(
                        SyntaxFactory.XmlText(
                            "A disposable resource. The caller is responsible for disposing the returned object."))),
                SyntaxFactory.XmlText("\n")
            }));

        var leadingTrivia = methodDeclaration.GetLeadingTrivia();
        var newLeadingTrivia = leadingTrivia.Insert(0, SyntaxFactory.Trivia(docComment));

        var newMethodDeclaration = methodDeclaration.WithLeadingTrivia(newLeadingTrivia);
        var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);

        return document.WithSyntaxRoot(newRoot);
    }
}
