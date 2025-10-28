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
/// Code fix that implements IAsyncDisposable interface for types that need async disposal.
/// Fixes DISP012: AsyncDisposableNotImplemented
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementIAsyncDisposableCodeFixProvider))]
[Shared]
public class ImplementIAsyncDisposableCodeFixProvider : CodeFixProvider
{
    private const string Title = "Implement IAsyncDisposable";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.AsyncDisposableNotImplemented);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var typeDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();

        if (typeDeclaration == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => ImplementIAsyncDisposableAsync(context.Document, typeDeclaration, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private async Task<Document> ImplementIAsyncDisposableAsync(
        Document document,
        TypeDeclarationSyntax typeDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return document;

        // Add IAsyncDisposable to base list
        var asyncDisposableType = SyntaxFactory.SimpleBaseType(
            SyntaxFactory.ParseTypeName("IAsyncDisposable"));

        var newBaseList = typeDeclaration.BaseList ??
            SyntaxFactory.BaseList(SyntaxFactory.SeparatedList<BaseTypeSyntax>());

        newBaseList = newBaseList.AddTypes(asyncDisposableType);

        // Create DisposeAsync method
        var disposeAsyncMethod = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.ParseTypeName("ValueTask"),
            "DisposeAsync")
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.ParseStatement("// TODO: Dispose async resources here"),
                SyntaxFactory.ParseStatement("await Task.CompletedTask;")
            ));

        var newTypeDeclaration = typeDeclaration
            .WithBaseList(newBaseList)
            .AddMembers(disposeAsyncMethod);

        var newRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
}
