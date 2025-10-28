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
/// Code fix provider that implements the Dispose(bool) pattern.
/// Fixes: DISP008
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeBoolPatternCodeFixProvider))]
[Shared]
public class DisposeBoolPatternCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DisposeBoolPattern);

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
                title: "Implement Dispose(bool) pattern",
                createChangedDocument: c => ImplementDisposeBoolPatternAsync(context.Document, typeDeclaration, c),
                equivalenceKey: "ImplementDisposeBoolPattern"),
            diagnostic);
    }

    private async Task<Document> ImplementDisposeBoolPatternAsync(
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

        // Check if type has finalizer
        bool hasFinalizer = typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(m => m.MethodKind == MethodKind.Destructor);

        // Get disposable fields
        var disposableFields = typeSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsStatic && IsDisposable(f.Type))
            .ToList();

        var newTypeDeclaration = typeDeclaration;

        // Find existing Dispose() method
        var disposeMethod = typeDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == "Dispose" && m.ParameterList.Parameters.Count == 0);

        if (disposeMethod != null)
        {
            // Replace existing Dispose() to call Dispose(bool)
            var newDisposeMethod = CreatePublicDisposeMethod(hasFinalizer);
            newTypeDeclaration = newTypeDeclaration.ReplaceNode(disposeMethod, newDisposeMethod);
        }
        else
        {
            // Add new public Dispose() method
            var newDisposeMethod = CreatePublicDisposeMethod(hasFinalizer);
            newTypeDeclaration = newTypeDeclaration.AddMembers(newDisposeMethod);
        }

        // Add protected virtual Dispose(bool) method
        var disposeBoolMethod = CreateDisposeBoolMethod(disposableFields);
        newTypeDeclaration = newTypeDeclaration.AddMembers(disposeBoolMethod);

        // Add finalizer if needed and not present
        if (!hasFinalizer && NeedsFinalizer(disposableFields))
        {
            var finalizer = CreateFinalizer(typeDeclaration.Identifier.Text);
            newTypeDeclaration = newTypeDeclaration.AddMembers(finalizer);
        }

        // Format the new type declaration
        newTypeDeclaration = newTypeDeclaration.WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    private MethodDeclarationSyntax CreatePublicDisposeMethod(bool hasFinalizer)
    {
        var statements = new List<StatementSyntax>();

        // Dispose(true);
        statements.Add(SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName("Dispose"))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.TrueLiteralExpression)))))));

        // GC.SuppressFinalize(this);
        if (hasFinalizer)
        {
            statements.Add(SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("GC"),
                        SyntaxFactory.IdentifierName("SuppressFinalize")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.ThisExpression()))))));
        }

        return SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            "Dispose")
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBody(SyntaxFactory.Block(statements));
    }

    private MethodDeclarationSyntax CreateDisposeBoolMethod(List<IFieldSymbol> disposableFields)
    {
        var statements = new List<StatementSyntax>();

        // if (disposing)
        // {
        //     // Dispose managed resources
        // }
        var managedStatements = new List<StatementSyntax>();
        foreach (var field in disposableFields)
        {
            // fieldName?.Dispose();
            managedStatements.Add(SyntaxFactory.ExpressionStatement(
                SyntaxFactory.ConditionalAccessExpression(
                    SyntaxFactory.IdentifierName(field.Name),
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberBindingExpression(
                            SyntaxFactory.IdentifierName("Dispose"))))));
        }

        if (managedStatements.Count > 0)
        {
            statements.Add(SyntaxFactory.IfStatement(
                SyntaxFactory.IdentifierName("disposing"),
                SyntaxFactory.Block(managedStatements)));
        }

        // Add comment for unmanaged resources
        // TODO: Free unmanaged resources (unmanaged objects) and override finalizer
        // TODO: Set large fields to null

        return SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            "Dispose")
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(
                            SyntaxFactory.Identifier("disposing"))
                        .WithType(
                            SyntaxFactory.PredefinedType(
                                SyntaxFactory.Token(SyntaxKind.BoolKeyword))))))
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                    SyntaxFactory.Token(SyntaxKind.VirtualKeyword)))
            .WithBody(SyntaxFactory.Block(statements));
    }

    private DestructorDeclarationSyntax CreateFinalizer(string typeName)
    {
        // ~TypeName()
        // {
        //     Dispose(false);
        // }
        return SyntaxFactory.DestructorDeclaration(typeName)
            .WithBody(
                SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName("Dispose"))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.FalseLiteralExpression))))))));
    }

    private bool NeedsFinalizer(List<IFieldSymbol> disposableFields)
    {
        // Finalizer is typically only needed if type holds unmanaged resources
        // For now, we conservatively don't add one unless explicitly needed
        return false;
    }

    private bool IsDisposable(ITypeSymbol type)
    {
        return type.AllInterfaces.Any(i => i.ToDisplayString() == "System.IDisposable");
    }
}
