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
/// Code fix provider that adds return statement for disposable or adds disposal.
/// Fixes: DISP021, DISP022
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddReturnDisposableCodeFixProvider))]
[Shared]
public class AddReturnDisposableCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            DiagnosticIds.DisposalNotPropagated,
            DiagnosticIds.DisposableCreatedNotReturned);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the object creation or variable declaration
        var node = root.FindNode(diagnosticSpan);
        var variableDeclarator = node.AncestorsAndSelf()
            .OfType<VariableDeclaratorSyntax>()
            .FirstOrDefault();

        if (variableDeclarator == null)
            return;

        // Offer two fixes: add disposal or return the disposable
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add disposal at end of method",
                createChangedDocument: c => AddDisposalAsync(context.Document, variableDeclarator, root, c),
                equivalenceKey: "AddDisposal"),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Return disposable to caller",
                createChangedDocument: c => ReturnDisposableAsync(context.Document, variableDeclarator, root, c),
                equivalenceKey: "ReturnDisposable"),
            diagnostic);
    }

    private async Task<Document> ReturnDisposableAsync(
        Document document,
        VariableDeclaratorSyntax variableDeclarator,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        var methodDeclaration = variableDeclarator.AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (methodDeclaration == null)
            return document;

        var variableName = variableDeclarator.Identifier.Text;

        // Add return statement at the end of the method
        var returnStatement = SyntaxFactory.ReturnStatement(
            SyntaxFactory.IdentifierName(variableName));

        var body = methodDeclaration.Body;
        if (body == null)
            return document;

        var newBody = body.AddStatements(returnStatement);

        // Update method return type if it's void
        var newMethod = methodDeclaration;
        if (methodDeclaration.ReturnType is PredefinedTypeSyntax predefined &&
            predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
        {
            // Get the type of the variable
            var localSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);
            if (localSymbol is ILocalSymbol local)
            {
                var typeName = local.Type.ToDisplayString();
                var newReturnType = SyntaxFactory.ParseTypeName(typeName);
                newMethod = methodDeclaration.WithReturnType(newReturnType);
            }
        }

        newMethod = newMethod.WithBody(newBody)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
        return document.WithSyntaxRoot(newRoot);
    }

    private async Task<Document> AddDisposalAsync(
        Document document,
        VariableDeclaratorSyntax variableDeclarator,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        var methodDeclaration = variableDeclarator.AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (methodDeclaration?.Body == null)
            return document;

        var variableName = variableDeclarator.Identifier.Text;

        // Create disposal statement: variableName?.Dispose();
        var disposeStatement = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.ConditionalAccessExpression(
                SyntaxFactory.IdentifierName(variableName),
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberBindingExpression(
                        SyntaxFactory.IdentifierName("Dispose")))));

        var body = methodDeclaration.Body;
        var closeBrace = body.CloseBraceToken;

        // Preserve trailing trivia (e.g., comments) by moving it to the new statement
        var disposeLeadingTrivia = closeBrace.LeadingTrivia;
        disposeStatement = disposeStatement.WithLeadingTrivia(disposeLeadingTrivia);

        var updatedCloseBrace = closeBrace.WithLeadingTrivia(SyntaxFactory.TriviaList());

        // Add disposal at the end of the method
        var newBody = body.WithCloseBraceToken(updatedCloseBrace).AddStatements(disposeStatement);
        var newMethod = methodDeclaration.WithBody(newBody)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
        return document.WithSyntaxRoot(newRoot);
    }
}
