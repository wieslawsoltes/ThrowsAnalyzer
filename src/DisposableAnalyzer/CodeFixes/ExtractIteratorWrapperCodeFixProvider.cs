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
/// Code fix that extracts iterator logic into a separate method to ensure proper disposal.
/// Fixes DISP015: DisposableInIterator
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExtractIteratorWrapperCodeFixProvider))]
[Shared]
public class ExtractIteratorWrapperCodeFixProvider : CodeFixProvider
{
    private const string Title = "Extract to wrapper with using statement";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DisposableInIterator);

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
                createChangedDocument: c => ExtractIteratorWrapperAsync(context.Document, methodDeclaration, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private async Task<Document> ExtractIteratorWrapperAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return document;

        // Create inner iterator method name
        var innerMethodName = methodDeclaration.Identifier.Text + "Core";

        // Create inner method (rename current method)
        var innerMethod = methodDeclaration
            .WithIdentifier(SyntaxFactory.Identifier(innerMethodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));

        // Create wrapper method that calls inner method with using statement
        var wrapperBody = SyntaxFactory.Block(
            SyntaxFactory.ParseStatement($"// TODO: Wrap disposable creation in using statement"),
            SyntaxFactory.ParseStatement($"// Then call {innerMethodName}() and yield return results")
        );

        var wrapperMethod = SyntaxFactory.MethodDeclaration(
            methodDeclaration.ReturnType,
            methodDeclaration.Identifier)
            .WithModifiers(methodDeclaration.Modifiers)
            .WithParameterList(methodDeclaration.ParameterList)
            .WithBody(wrapperBody);

        // Get containing type
        var containingType = methodDeclaration.Parent as TypeDeclarationSyntax;
        if (containingType == null) return document;

        // Replace old method with both new methods
        var memberIndex = containingType.Members.IndexOf(methodDeclaration);
        var newMembers = containingType.Members
            .RemoveAt(memberIndex)
            .Insert(memberIndex, wrapperMethod)
            .Insert(memberIndex + 1, innerMethod);

        var newContainingType = containingType.WithMembers(newMembers);
        var newRoot = root.ReplaceNode(containingType, newContainingType);

        return document.WithSyntaxRoot(newRoot);
    }
}
