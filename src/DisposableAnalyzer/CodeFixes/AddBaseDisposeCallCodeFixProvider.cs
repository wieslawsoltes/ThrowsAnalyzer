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
/// Code fix provider that adds base.Dispose() call to Dispose methods.
/// Fixes: DISP009
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddBaseDisposeCallCodeFixProvider))]
[Shared]
public class AddBaseDisposeCallCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DisposableBaseCall);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the method declaration
        var methodDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (methodDeclaration == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add base.Dispose() call",
                createChangedDocument: c => AddBaseDisposeCallAsync(context.Document, methodDeclaration, c),
                equivalenceKey: "AddBaseDisposeCall"),
            diagnostic);
    }

    private async Task<Document> AddBaseDisposeCallAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Determine if this is Dispose() or Dispose(bool)
        bool isDisposeBool = methodDeclaration.ParameterList.Parameters.Count == 1 &&
                             methodDeclaration.ParameterList.Parameters[0].Type?.ToString() == "bool";

        // Create base.Dispose() or base.Dispose(disposing) call
        var baseDisposeCall = CreateBaseDisposeCall(isDisposeBool);

        // Get existing body
        var body = methodDeclaration.Body;
        if (body == null)
            return document;

        // Add base.Dispose() call at the end of the method
        var newBody = body.AddStatements(baseDisposeCall);

        var newMethod = methodDeclaration.WithBody(newBody)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
        return document.WithSyntaxRoot(newRoot);
    }

    private StatementSyntax CreateBaseDisposeCall(bool isDisposeBool)
    {
        // base.Dispose() or base.Dispose(disposing)
        var invocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.BaseExpression(),
                SyntaxFactory.IdentifierName("Dispose")));

        if (isDisposeBool)
        {
            // Add 'disposing' parameter
            invocation = invocation.WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.IdentifierName("disposing")))));
        }
        else
        {
            invocation = invocation.WithArgumentList(SyntaxFactory.ArgumentList());
        }

        return SyntaxFactory.ExpressionStatement(invocation);
    }
}
