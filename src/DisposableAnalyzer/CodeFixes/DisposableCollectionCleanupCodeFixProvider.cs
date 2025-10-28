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
/// Code fix provider that adds disposal loop for collection elements.
/// Fixes: DISP020
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposableCollectionCleanupCodeFixProvider))]
[Shared]
public class DisposableCollectionCleanupCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DisposableCollection);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the type declaration
        var typeDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();

        if (typeDeclaration == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add disposal loop in Dispose method",
                createChangedDocument: c => AddDisposalLoopAsync(context.Document, typeDeclaration, diagnostic, c),
                equivalenceKey: "AddDisposalLoop"),
            diagnostic);
    }

    private async Task<Document> AddDisposalLoopAsync(
        Document document,
        TypeDeclarationSyntax typeDeclaration,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) as INamedTypeSymbol;
        if (typeSymbol == null)
            return document;

        // Get the collection field from diagnostic message
        // Extract field name from message format
        var fieldName = ExtractFieldNameFromDiagnostic(diagnostic);
        if (fieldName == null)
            return document;

        var field = typeSymbol.GetMembers(fieldName).OfType<IFieldSymbol>().FirstOrDefault();
        if (field == null)
            return document;

        // Find or create Dispose method
        var disposeMethod = typeDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == "Dispose" && m.ParameterList.Parameters.Count == 0);

        if (disposeMethod != null)
        {
            // Add disposal loop to existing Dispose method
            var newMethod = AddDisposalLoopToMethod(disposeMethod, fieldName);
            var newTypeDeclaration = typeDeclaration.ReplaceNode(disposeMethod, newMethod);
            var newRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }
        else
        {
            // Create new Dispose method with disposal loop
            var newMethod = CreateDisposeMethodWithLoop(fieldName);
            var newTypeDeclaration = typeDeclaration.AddMembers(newMethod)
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }
    }

    private MethodDeclarationSyntax AddDisposalLoopToMethod(
        MethodDeclarationSyntax method,
        string fieldName)
    {
        var body = method.Body;
        if (body == null)
            return method;

        // Create disposal loop
        var disposalLoop = CreateDisposalLoop(fieldName);

        // Add to beginning of method
        var newBody = body.WithStatements(
            body.Statements.Insert(0, disposalLoop));

        return method.WithBody(newBody)
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    private MethodDeclarationSyntax CreateDisposeMethodWithLoop(string fieldName)
    {
        var disposalLoop = CreateDisposalLoop(fieldName);

        return SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            "Dispose")
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBody(SyntaxFactory.Block(disposalLoop));
    }

    private StatementSyntax CreateDisposalLoop(string fieldName)
    {
        // if (fieldName != null)
        // {
        //     foreach (var item in fieldName)
        //     {
        //         item?.Dispose();
        //     }
        //     fieldName.Clear();
        // }

        var disposeStatement = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.ConditionalAccessExpression(
                SyntaxFactory.IdentifierName("item"),
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberBindingExpression(
                        SyntaxFactory.IdentifierName("Dispose")))));

        var foreachLoop = SyntaxFactory.ForEachStatement(
            SyntaxFactory.IdentifierName("var"),
            "item",
            SyntaxFactory.IdentifierName(fieldName),
            SyntaxFactory.Block(disposeStatement));

        var clearStatement = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.IdentifierName("Clear"))));

        var ifStatement = SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                SyntaxFactory.IdentifierName(fieldName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            SyntaxFactory.Block(foreachLoop, clearStatement));

        return ifStatement;
    }

    private string? ExtractFieldNameFromDiagnostic(Diagnostic diagnostic)
    {
        // The diagnostic message format includes the field name
        // Example: "Collection field '_items' contains disposable elements..."
        var message = diagnostic.GetMessage();

        // Simple extraction - look for text between single quotes
        var startIndex = message.IndexOf('\'');
        if (startIndex >= 0)
        {
            var endIndex = message.IndexOf('\'', startIndex + 1);
            if (endIndex > startIndex)
            {
                return message.Substring(startIndex + 1, endIndex - startIndex - 1);
            }
        }

        return null;
    }
}
