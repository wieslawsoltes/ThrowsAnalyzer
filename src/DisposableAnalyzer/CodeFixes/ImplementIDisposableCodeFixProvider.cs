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
/// Code fix provider that implements IDisposable for types with disposable fields.
/// Fixes: DISP002, DISP007
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementIDisposableCodeFixProvider))]
[Shared]
public class ImplementIDisposableCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.UndisposedField, DiagnosticIds.DisposableNotImplemented);

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
                title: "Implement IDisposable",
                createChangedDocument: c => ImplementIDisposableAsync(context.Document, typeDeclaration, c),
                equivalenceKey: "ImplementIDisposable"),
            diagnostic);
    }

    private async Task<Document> ImplementIDisposableAsync(
        Document document,
        TypeDeclarationSyntax typeDeclaration,
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

        // Get disposable fields
        var disposableFields = typeSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsStatic && IsDisposable(f.Type))
            .ToList();

        // Add IDisposable to base list
        var disposableInterface = SyntaxFactory.SimpleBaseType(
            SyntaxFactory.ParseTypeName("System.IDisposable"));

        var newBaseList = typeDeclaration.BaseList != null
            ? typeDeclaration.BaseList.AddTypes(disposableInterface)
            : SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(disposableInterface));

        var newTypeDeclaration = typeDeclaration.WithBaseList(newBaseList);

        // Create Dispose method
        var disposeMethod = CreateDisposeMethod(disposableFields);

        // Add Dispose method to type
        newTypeDeclaration = newTypeDeclaration.AddMembers(disposeMethod);

        // Format the new type declaration
        newTypeDeclaration = newTypeDeclaration.WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    private MethodDeclarationSyntax CreateDisposeMethod(List<IFieldSymbol> disposableFields)
    {
        var statements = new List<StatementSyntax>();

        // Add disposal for each field
        foreach (var field in disposableFields)
        {
            // fieldName?.Dispose();
            var disposeStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.ConditionalAccessExpression(
                    SyntaxFactory.IdentifierName(field.Name),
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberBindingExpression(
                            SyntaxFactory.IdentifierName("Dispose")))));

            statements.Add(disposeStatement);
        }

        // Create method
        var method = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            "Dispose")
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBody(SyntaxFactory.Block(statements));

        return method;
    }

    private bool IsDisposable(ITypeSymbol type)
    {
        return type.AllInterfaces.Any(i => i.ToDisplayString() == "System.IDisposable");
    }
}
