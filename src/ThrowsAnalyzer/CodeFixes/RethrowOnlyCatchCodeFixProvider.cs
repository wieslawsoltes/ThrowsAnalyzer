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
    /// Code fix provider for THROWS009: Catch block only rethrows exception.
    /// Removes the unnecessary catch clause.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RethrowOnlyCatchCodeFixProvider))]
    [Shared]
    public class RethrowOnlyCatchCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        private const string RemoveTitle = "Remove unnecessary catch clause";

        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can fix.
        /// </summary>
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.DiagnosticId009);

        /// <summary>
        /// Registers code fixes for rethrow-only catch diagnostics.
        /// </summary>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var (document, root) = await GetDocumentAndRootAsync(context, context.CancellationToken)
                .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            // Find the catch clause
            var catchClause = root.FindNode(diagnostic.Location.SourceSpan)
                .FirstAncestorOrSelf<CatchClauseSyntax>();

            if (catchClause == null)
            {
                return;
            }

            // Register code fix to remove the catch clause
            context.RegisterCodeFix(
                CreateCodeAction(
                    RemoveTitle,
                    cancellationToken => RemoveCatchClauseAsync(document, root, catchClause, cancellationToken),
                    nameof(RemoveCatchClauseAsync)),
                diagnostic);
        }

        /// <summary>
        /// Removes the rethrow-only catch clause from the try statement.
        /// </summary>
        private static Task<Document> RemoveCatchClauseAsync(
            Document document,
            SyntaxNode root,
            CatchClauseSyntax catchClause,
            CancellationToken cancellationToken)
        {
            // Find the containing try statement
            var tryStatement = catchClause.FirstAncestorOrSelf<TryStatementSyntax>();
            if (tryStatement == null)
            {
                return Task.FromResult(document);
            }

            // Remove the catch clause
            var newCatches = tryStatement.Catches.Remove(catchClause);

            SyntaxNode newNode;
            if (newCatches.Count == 0 && tryStatement.Finally == null)
            {
                // No catches or finally left - unwrap the try block completely
                if (tryStatement.Block.Statements.Count == 1)
                {
                    newNode = tryStatement.Block.Statements[0]
                        .WithTriviaFrom(tryStatement);
                }
                else
                {
                    newNode = tryStatement.Block
                        .WithTriviaFrom(tryStatement);
                }
            }
            else
            {
                // Keep the try statement with remaining catches/finally
                newNode = tryStatement.WithCatches(newCatches);
            }

            var newRoot = root.ReplaceNode(tryStatement, newNode);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}
