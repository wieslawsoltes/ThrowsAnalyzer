using System;
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
/// Code fix that adds XML documentation comment to clarify disposal ownership.
/// Fixes DISP016: DisposableReturned
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DocumentDisposalOwnershipCodeFixProvider))]
[Shared]
public class DocumentDisposalOwnershipCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add disposal ownership documentation";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DisposableReturned);

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
                createChangedDocument: c => AddDisposalDocumentationAsync(context.Document, methodDeclaration, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private async Task<Document> AddDisposalDocumentationAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var leadingTrivia = methodDeclaration.GetLeadingTrivia();
        var documentationTrivia = leadingTrivia
            .Select(trivia => trivia.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        var indent = GetIndentation(methodDeclaration);
        var returnTypeName = methodDeclaration.ReturnType.ToString();

        if (documentationTrivia == null)
        {
            var docCommentText = BuildCommentText(indent, returnTypeName);
            var leadingWithDoc = BuildLeadingTrivia(methodDeclaration, docCommentText);
            var newMethodDeclaration = methodDeclaration.WithLeadingTrivia(leadingWithDoc);
            var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }

        var docTextLines = documentationTrivia.ToFullString()
            .Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None)
            .ToList();

        if (!docTextLines.Any(line => line.Contains("<returns")))
        {
            docTextLines.Add($"{indent}/// <returns>A {returnTypeName} that the caller must dispose.</returns>");
        }

        if (!docTextLines.Any(line => line.Contains("<remarks")))
        {
            docTextLines.Add($"{indent}/// <remarks>");
            docTextLines.Add($"{indent}/// The caller is responsible for disposing the returned resource.");
            docTextLines.Add($"{indent}/// </remarks>");
        }

        if (!docTextLines.Any(line => line.Contains("<summary")))
        {
            docTextLines.Insert(0, $"{indent}/// <summary>");
            docTextLines.Insert(1, $"{indent}/// Creates a {returnTypeName}.");
            docTextLines.Insert(2, $"{indent}/// </summary>");
        }

        var updatedDocComment = string.Join("\n", docTextLines) + "\n";
        var updatedLeadingTrivia = BuildLeadingTrivia(methodDeclaration, updatedDocComment);

        var updatedMethod = methodDeclaration.WithLeadingTrivia(updatedLeadingTrivia);
        var replacedRoot = root.ReplaceNode(methodDeclaration, updatedMethod);
        return document.WithSyntaxRoot(replacedRoot);
    }

    private static SyntaxTriviaList BuildLeadingTrivia(MethodDeclarationSyntax methodDeclaration, string docCommentText)
    {
        var leadingTrivia = methodDeclaration.GetLeadingTrivia();

        var baseTrivia = leadingTrivia.Where(t => t.GetStructure() is not DocumentationCommentTriviaSyntax).ToList();
        if (baseTrivia.Count > 0)
        {
            var last = baseTrivia[baseTrivia.Count - 1];
            if (last.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                baseTrivia.RemoveAt(baseTrivia.Count - 1);
            }
        }

        var indentTrivia = SyntaxFactory.Whitespace(GetIndentation(methodDeclaration));
        var docTrivia = SyntaxFactory.ParseLeadingTrivia(docCommentText);

        return SyntaxFactory.TriviaList(baseTrivia).AddRange(docTrivia).Add(indentTrivia);
    }

    private static string BuildCommentText(string indent, string returnTypeName)
    {
        return $"{indent}/// <summary>\n" +
               $"{indent}/// Creates a {returnTypeName}.\n" +
               $"{indent}/// </summary>\n" +
               $"{indent}/// <returns>A {returnTypeName} that the caller must dispose.</returns>\n" +
               $"{indent}/// <remarks>\n" +
               $"{indent}/// The caller is responsible for disposing the returned resource.\n" +
               $"{indent}/// </remarks>\n";
    }

    private static string GetIndentation(MethodDeclarationSyntax methodDeclaration)
    {
        var token = methodDeclaration.GetFirstToken();
        var lineSpan = token.GetLocation().GetLineSpan();
        var column = Math.Max(0, lineSpan.StartLinePosition.Character);
        if (column == 0)
        {
            var whitespaceTrivia = methodDeclaration.GetLeadingTrivia()
                .LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
            if (whitespaceTrivia != default)
                return whitespaceTrivia.ToString();
        }
        return column > 0 ? new string(' ', column) : string.Empty;
    }
}
