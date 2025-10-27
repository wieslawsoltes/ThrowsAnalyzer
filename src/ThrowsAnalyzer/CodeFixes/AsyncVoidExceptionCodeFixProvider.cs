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
    /// Code fix provider for THROWS021: Async Void Exception.
    /// Offers to convert async void to async Task or wrap in try-catch.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncVoidExceptionCodeFixProvider))]
    [Shared]
    public class AsyncVoidExceptionCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("THROWS021");

        protected override string Title => "Fix async void exception";

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the method
            var method = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null)
                return;

            // Check if it's truly async void
            if (!method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)) ||
                method.ReturnType is not PredefinedTypeSyntax predefined ||
                !predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
                return;

            // Register code fix to convert to async Task
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Change to 'async Task'",
                    c => ConvertToAsyncTaskAsync(context.Document, method, c),
                    nameof(ConvertToAsyncTaskAsync)),
                diagnostic);

            // Register code fix to wrap in try-catch
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Wrap body in try-catch",
                    c => WrapBodyInTryCatchAsync(context.Document, method, c),
                    nameof(WrapBodyInTryCatchAsync)),
                diagnostic);
        }

        private async Task<Document> ConvertToAsyncTaskAsync(
            Document document,
            MethodDeclarationSyntax method,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            // Replace void with Task
            var taskType = SyntaxFactory.IdentifierName("Task");
            var newMethod = method.WithReturnType(taskType)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(method, newMethod);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> WrapBodyInTryCatchAsync(
            Document document,
            MethodDeclarationSyntax method,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var methodBody = method.Body;
            if (methodBody == null)
                return document;

            // Create wrapped try-catch using parsing
            var wrappedCode = $@"try
{methodBody.ToFullString()}
catch (Exception ex)
{{
    // TODO: Log error appropriately
    Console.WriteLine($""Error: {{ex.Message}}"");
}}";

            var newBody = SyntaxFactory.ParseStatement(wrappedCode) as BlockSyntax;
            if (newBody == null)
                return document;

            newBody = newBody.WithAdditionalAnnotations(Formatter.Annotation);

            var newMethod = method.WithBody(newBody);
            var newRoot = root.ReplaceNode(method, newMethod);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
