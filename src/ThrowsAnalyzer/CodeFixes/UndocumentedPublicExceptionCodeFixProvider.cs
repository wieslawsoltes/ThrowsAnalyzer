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
using RoslynAnalyzer.Core.Members;
using RoslynAnalyzer.Core.Helpers;

namespace ThrowsAnalyzer.CodeFixes
{
    /// <summary>
    /// Code fix provider for THROWS019: Undocumented Public Exception.
    /// Adds comprehensive XML documentation for all exceptions thrown by public methods.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UndocumentedPublicExceptionCodeFixProvider))]
    [Shared]
    public class UndocumentedPublicExceptionCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("THROWS019");

        protected override string Title => "Add XML exception documentation";

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the method declaration
            var node = root.FindNode(diagnosticSpan);
            var method = node as MethodDeclarationSyntax ?? node.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            if (method == null)
                return;

            // Register code fix
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Add exception documentation",
                    c => AddExceptionDocumentationAsync(context.Document, method, diagnostic, c),
                    nameof(AddExceptionDocumentationAsync)),
                diagnostic);
        }

        private async Task<Document> AddExceptionDocumentationAsync(
            Document document,
            MethodDeclarationSyntax method,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
                return document;

            // Find all throw statements in the method
            var throwStatements = method.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Where(t => t.Expression != null)
                .ToList();

            // Get exception types
            var exceptionTypes = throwStatements
                .Select(t => semanticModel.GetTypeInfo(t.Expression!, cancellationToken).Type)
                .Where(t => t != null)
                .Select(t => t!.ToDisplayString())
                .Distinct()
                .ToList();

            if (!exceptionTypes.Any())
                return document;

            // Create documentation
            var documentation = CreateDocumentationComment(method, exceptionTypes);
            
            var newMethod = method.WithLeadingTrivia(documentation)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(method, newMethod);
            return document.WithSyntaxRoot(newRoot);
        }

        private SyntaxTriviaList CreateDocumentationComment(
            MethodDeclarationSyntax method,
            System.Collections.Generic.List<string> exceptionTypes)
        {
            var triviaList = SyntaxFactory.TriviaList();

            // Check if there's existing documentation
            var existingDoc = method.GetLeadingTrivia()
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));

            if (existingDoc.HasStructure)
            {
                // Append to existing documentation
                triviaList = triviaList.AddRange(method.GetLeadingTrivia());
            }
            else
            {
                // Create new documentation
                triviaList = triviaList.Add(
                    SyntaxFactory.Trivia(
                        SyntaxFactory.DocumentationCommentTrivia(
                            SyntaxKind.SingleLineDocumentationCommentTrivia,
                            SyntaxFactory.List<XmlNodeSyntax>(
                                new[]
                                {
                                    SyntaxFactory.XmlText("/// <summary>"),
                                    SyntaxFactory.XmlNewLine("\n"),
                                    SyntaxFactory.XmlText($"/// {method.Identifier.Text}"),
                                    SyntaxFactory.XmlNewLine("\n"),
                                    SyntaxFactory.XmlText("/// </summary>"),
                                    SyntaxFactory.XmlNewLine("\n")
                                }))));
            }

            // Add exception documentation
            foreach (var exceptionType in exceptionTypes)
            {
                var exceptionDoc = $"/// <exception cref=\"{exceptionType}\">Thrown when an error occurs</exception>\n";
                triviaList = triviaList.Add(SyntaxFactory.Comment(exceptionDoc));
            }

            return triviaList;
        }
    }
}
