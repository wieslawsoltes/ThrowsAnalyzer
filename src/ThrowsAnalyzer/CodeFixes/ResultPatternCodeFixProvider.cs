using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ThrowsAnalyzer.CodeFixes
{
    /// <summary>
    /// Code fix provider for THROWS030: Result Pattern Suggestion.
    /// Adds comments suggesting the Result pattern for expected errors.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ResultPatternCodeFixProvider))]
    [Shared]
    public class ResultPatternCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("THROWS030");

        protected override string Title => "Add Result pattern suggestion";

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the method declaration
            var method = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null)
                return;

            // Register code fix to add Result pattern comment
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Add comment suggesting Result<T> pattern",
                    c => AddResultPatternCommentAsync(context.Document, method, c),
                    nameof(AddResultPatternCommentAsync)),
                diagnostic);

            // Register code fix to convert to bool return
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Convert to bool return value",
                    c => ConvertToBoolReturnAsync(context.Document, method, c),
                    nameof(ConvertToBoolReturnAsync)),
                diagnostic);
        }

        private async Task<Document> AddResultPatternCommentAsync(
            Document document,
            MethodDeclarationSyntax method,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            // Add comment before method suggesting Result pattern
            var commentText =
                "// TODO: Consider using Result<T> pattern instead of exceptions for expected validation errors\n" +
                "// Example: Result<T> ValidateInput(...) { return Result.Success(...); or Result.Failure(...); }\n";

            var commentTrivia = SyntaxFactory.ParseLeadingTrivia(commentText);

            var newMethod = method.WithLeadingTrivia(
                method.GetLeadingTrivia().AddRange(commentTrivia))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(method, newMethod);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> ConvertToBoolReturnAsync(
            Document document,
            MethodDeclarationSyntax method,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            // Change return type to bool
            var newReturnType = SyntaxFactory.PredefinedType(
                SyntaxFactory.Token(SyntaxKind.BoolKeyword));

            // Find all throw statements and replace with return false
            var throwStatements = method.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Where(t => t.Expression != null)
                .ToList();

            var newMethod = method.WithReturnType(newReturnType);

            // Replace each throw with return false
            foreach (var throwStmt in throwStatements)
            {
                var returnFalse = SyntaxFactory.ReturnStatement(
                    SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                    .WithLeadingTrivia(throwStmt.GetLeadingTrivia())
                    .WithTrailingTrivia(throwStmt.GetTrailingTrivia());

                newMethod = newMethod.ReplaceNode(
                    newMethod.DescendantNodes().OfType<ThrowStatementSyntax>()
                        .FirstOrDefault(t => t.Span == throwStmt.Span)!,
                    returnFalse);
            }

            // Add return true at end if void method
            if (method.ReturnType is PredefinedTypeSyntax predefined &&
                predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
            {
                var body = newMethod.Body;
                if (body != null)
                {
                    var returnTrue = SyntaxFactory.ReturnStatement(
                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));

                    newMethod = newMethod.WithBody(
                        body.AddStatements(returnTrue));
                }
            }

            newMethod = newMethod.WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(method, newMethod);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
