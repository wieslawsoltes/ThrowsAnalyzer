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
/// Code fix provider that converts synchronous using to await using.
/// Fixes: DISP011
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertToAwaitUsingCodeFixProvider))]
[Shared]
public class ConvertToAwaitUsingCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.AsyncDisposableNotUsed);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the using statement
        var usingStatement = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<UsingStatementSyntax>()
            .FirstOrDefault();

        if (usingStatement == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Convert to 'await using'",
                createChangedDocument: c => ConvertToAwaitUsingAsync(context.Document, usingStatement, c),
                equivalenceKey: "ConvertToAwaitUsing"),
            diagnostic);
    }

    private async Task<Document> ConvertToAwaitUsingAsync(
        Document document,
        UsingStatementSyntax usingStatement,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Add await keyword to using statement
        var awaitUsing = usingStatement.WithAwaitKeyword(
            SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space));

        // Also need to ensure the containing method is async
        var containingMethod = usingStatement.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod != null && !containingMethod.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            // Add async modifier to method
            var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);

            var newModifiers = containingMethod.Modifiers.Add(asyncModifier);
            var asyncMethod = containingMethod.WithModifiers(newModifiers);

            // Update return type if needed (void -> Task, T -> Task<T>)
            var returnType = containingMethod.ReturnType;
            TypeSyntax newReturnType;

            if (returnType.ToString() == "void")
            {
                newReturnType = SyntaxFactory.ParseTypeName("System.Threading.Tasks.Task");
            }
            else if (!returnType.ToString().StartsWith("Task"))
            {
                newReturnType = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("Task"),
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(returnType)));
            }
            else
            {
                newReturnType = returnType;
            }

            asyncMethod = asyncMethod.WithReturnType(newReturnType);

            var newRoot = root.ReplaceNode(containingMethod, asyncMethod);
            root = newRoot;
        }

        var finalRoot = root.ReplaceNode(usingStatement, awaitUsing);
        return document.WithSyntaxRoot(finalRoot);
    }
}
