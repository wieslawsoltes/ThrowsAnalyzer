using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer.CodeFixes
{
    /// <summary>
    /// Code fix provider for THROWS004: Rethrow Anti-Pattern.
    /// Replaces 'throw ex;' with 'throw;' to preserve stack trace.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RethrowAntiPatternCodeFixProvider))]
    [Shared]
    public class RethrowAntiPatternCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.DiagnosticId004);

        protected override string Title => "Replace with bare rethrow";

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the throw statement identified by the diagnostic
            var throwStatement = root.FindNode(diagnosticSpan) as ThrowStatementSyntax;
            if (throwStatement?.Expression == null)
                return;

            // Register the code fix
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Replace 'throw ex;' with 'throw;'",
                    c => ReplaceWithBareRethrowAsync(context.Document, throwStatement, c),
                    nameof(ReplaceWithBareRethrowAsync)),
                diagnostic);
        }

        private async Task<Document> ReplaceWithBareRethrowAsync(
            Document document,
            ThrowStatementSyntax throwStatement,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            // Create new throw statement without expression (bare rethrow)
            var newThrow = SyntaxFactory.ThrowStatement()
                .WithThrowKeyword(throwStatement.ThrowKeyword)
                .WithSemicolonToken(throwStatement.SemicolonToken)
                .WithTriviaFrom(throwStatement);

            // Replace the old throw statement with the new one
            var newRoot = root.ReplaceNode(throwStatement, newThrow);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
