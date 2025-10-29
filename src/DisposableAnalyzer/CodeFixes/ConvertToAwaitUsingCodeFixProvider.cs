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

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        var containingMethod = usingStatement.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

        var nodesToTrack = containingMethod != null
            ? new SyntaxNode[] { usingStatement, containingMethod }
            : new SyntaxNode[] { usingStatement };

        var trackedRoot = root.TrackNodes(nodesToTrack);

        if (containingMethod != null)
        {
            var trackedMethod = trackedRoot.GetCurrentNode(containingMethod);
            if (trackedMethod != null && !trackedMethod.Modifiers.Any(SyntaxKind.AsyncKeyword))
            {
                var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space);

                var asyncMethod = trackedMethod.WithModifiers(trackedMethod.Modifiers.Add(asyncModifier));

                var returnTypeSymbol = semanticModel.GetTypeInfo(containingMethod.ReturnType, cancellationToken).Type;
                var updatedReturnType = GetUpdatedReturnType(trackedMethod.ReturnType, returnTypeSymbol);

                if (updatedReturnType != null)
                {
                    asyncMethod = asyncMethod.WithReturnType(updatedReturnType
                        .WithTriviaFrom(trackedMethod.ReturnType));
                }

                trackedRoot = trackedRoot.ReplaceNode(trackedMethod, asyncMethod.WithAdditionalAnnotations(Formatter.Annotation));
            }
        }

        var currentUsing = trackedRoot.GetCurrentNode(usingStatement);
        if (currentUsing == null)
        {
            return document;
        }

        var awaitUsing = currentUsing.WithAwaitKeyword(
                SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var finalRoot = trackedRoot.ReplaceNode(currentUsing, awaitUsing);
        return document.WithSyntaxRoot(finalRoot);
    }

    private static TypeSyntax? GetUpdatedReturnType(TypeSyntax originalReturnType, ITypeSymbol? returnTypeSymbol)
    {
        if (returnTypeSymbol == null)
        {
            if (originalReturnType is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
            {
                return SyntaxFactory.ParseTypeName("System.Threading.Tasks.Task");
            }

            var returnTypeText = originalReturnType.ToString();
            if (IsTaskLike(returnTypeText))
            {
                return null;
            }

            return SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("Task"),
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(originalReturnType.WithoutTrivia())));
        }

        if (returnTypeSymbol.SpecialType == SpecialType.System_Void)
        {
            return SyntaxFactory.ParseTypeName("System.Threading.Tasks.Task");
        }

        if (IsTaskLike(returnTypeSymbol))
        {
            return null;
        }

        return SyntaxFactory.GenericName(
            SyntaxFactory.Identifier("Task"),
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList(originalReturnType.WithoutTrivia())));
    }

    private static bool IsTaskLike(ITypeSymbol symbol)
    {
        if (symbol is INamedTypeSymbol named)
        {
            var name = named.Name;
            var @namespace = named.ContainingNamespace?.ToDisplayString();
            if ((name == "Task" || name == "ValueTask") && @namespace == "System.Threading.Tasks")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTaskLike(string returnTypeText)
    {
        return returnTypeText.StartsWith("Task")
               || returnTypeText.StartsWith("System.Threading.Tasks.Task")
               || returnTypeText.StartsWith("ValueTask")
               || returnTypeText.StartsWith("System.Threading.Tasks.ValueTask");
    }
}
