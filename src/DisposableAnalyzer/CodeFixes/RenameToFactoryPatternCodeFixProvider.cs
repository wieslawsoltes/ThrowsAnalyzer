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
/// Code fix that renames methods to use factory pattern naming conventions.
/// Fixes DISP027: DisposableFactoryPattern
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RenameToFactoryPatternCodeFixProvider))]
[Shared]
public class RenameToFactoryPatternCodeFixProvider : CodeFixProvider
{
    private const string TitleCreate = "Rename to 'Create...'";
    private const string TitleBuild = "Rename to 'Build...'";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DisposableFactoryPattern);

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

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return;

        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);
        if (methodSymbol == null) return;

        var oldName = methodSymbol.Name;
        var newNameCreate = GetFactoryName(oldName, "Create");
        var newNameBuild = GetFactoryName(oldName, "Build");

        context.RegisterCodeFix(
            CodeAction.Create(
                title: TitleCreate,
                createChangedDocument: c => RenameMethodAsync(context.Document, methodDeclaration, newNameCreate, c),
                equivalenceKey: TitleCreate),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: TitleBuild,
                createChangedDocument: c => RenameMethodAsync(context.Document, methodDeclaration, newNameBuild, c),
                equivalenceKey: TitleBuild),
            diagnostic);
    }

    private string GetFactoryName(string originalName, string prefix)
    {
        // Remove common non-factory prefixes
        var name = originalName;
        if (name.StartsWith("Get"))
            name = name.Substring(3);
        else if (name.StartsWith("Find"))
            name = name.Substring(4);
        else if (name.StartsWith("Retrieve"))
            name = name.Substring(8);
        else if (name.StartsWith("Fetch"))
            name = name.Substring(5);

        return prefix + name;
    }

    private async Task<Document> RenameMethodAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        string newName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        // Simple syntax-based rename (doesn't handle all references, but works for basic cases)
        var newMethod = methodDeclaration.WithIdentifier(SyntaxFactory.Identifier(newName));
        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);

        return document.WithSyntaxRoot(newRoot);
    }
}
