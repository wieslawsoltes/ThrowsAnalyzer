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
    /// Code fix provider for THROWS017: Unhandled Method Call Exception.
    /// Offers fixes to wrap the call in try-catch or add documentation.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnhandledMethodCallCodeFixProvider))]
    [Shared]
    public class UnhandledMethodCallCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("THROWS017");

        protected override string Title => "Handle unhandled method call exception";

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the invocation or object creation expression
            var node = root.FindNode(diagnosticSpan);
            var invocation = node as InvocationExpressionSyntax ?? node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            var objectCreation = node as ObjectCreationExpressionSyntax ?? node.FirstAncestorOrSelf<ObjectCreationExpressionSyntax>();

            if (invocation == null && objectCreation == null)
                return;

            // Register code fix to wrap in try-catch
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Wrap in try-catch block",
                    c => WrapInTryCatchAsync(context.Document, node, diagnostic, c),
                    nameof(WrapInTryCatchAsync)),
                diagnostic);

            // Register code fix to add documentation
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Document that method propagates exception",
                    c => AddPropagationDocumentationAsync(context.Document, node, diagnostic, c),
                    nameof(AddPropagationDocumentationAsync)),
                diagnostic);
        }

        private async Task<Document> WrapInTryCatchAsync(
            Document document,
            SyntaxNode node,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            // Find the statement containing the invocation
            var statement = node.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null)
                return document;

            // Extract exception type from diagnostic message
            // Message format: "Method calls '{0}' which may throw {1}, but does not handle it"
            var exceptionType = ExtractExceptionTypeFromMessage(diagnostic.GetMessage());
            
            // Create try-catch block using parsing for simplicity
            var tryCatchCode = $@"try
{{
    {statement.ToFullString()}
}}
catch ({exceptionType} ex)
{{
    // TODO: Handle exception
    throw;
}}";

            var tryStatement = SyntaxFactory.ParseStatement(tryCatchCode)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(statement, tryStatement);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> AddPropagationDocumentationAsync(
            Document document,
            SyntaxNode node,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            // Find the containing method
            var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null)
                return document;

            // Extract exception type from diagnostic message
            var exceptionType = ExtractExceptionTypeFromMessage(diagnostic.GetMessage());

            // Create or update XML documentation
            var leadingTrivia = method.GetLeadingTrivia();
            var docComment = CreateOrUpdateDocumentation(leadingTrivia, exceptionType);

            var newMethod = method.WithLeadingTrivia(docComment);
            var newRoot = root.ReplaceNode(method, newMethod);

            return document.WithSyntaxRoot(newRoot);
        }

        private string ExtractExceptionTypeFromMessage(string message)
        {
            // Message format: "Method calls 'MethodName' which may throw ExceptionType, but does not handle it"
            var parts = message.Split(new[] { "may throw ", ", but" }, System.StringSplitOptions.None);
            if (parts.Length >= 2)
                return parts[1].Trim();
            
            return "System.Exception";
        }

        private SyntaxTriviaList CreateOrUpdateDocumentation(SyntaxTriviaList leadingTrivia, string exceptionType)
        {
            // Simple implementation: add exception documentation as comment trivia
            var docComment = $"/// <exception cref=\"{exceptionType}\">Propagated from called method</exception>\n";

            var commentTrivia = SyntaxFactory.ParseLeadingTrivia(docComment);

            return leadingTrivia.AddRange(commentTrivia);
        }
    }
}
