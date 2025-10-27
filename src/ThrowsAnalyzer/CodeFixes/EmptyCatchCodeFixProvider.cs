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
    /// Code fix provider for THROWS008: Empty catch block.
    /// Offers options to remove catch or add logging.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyCatchCodeFixProvider))]
    [Shared]
    public class EmptyCatchCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        private const string RemoveTitle = "Remove empty catch block";
        private const string AddLoggingTitle = "Add logging to catch block";

        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can fix.
        /// </summary>
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.DiagnosticId008);

        /// <summary>
        /// Registers code fixes for empty catch diagnostics.
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

            // Option 1: Remove empty catch
            context.RegisterCodeFix(
                CreateCodeAction(
                    RemoveTitle,
                    cancellationToken => RemoveCatchClauseAsync(document, root, catchClause, cancellationToken),
                    nameof(RemoveCatchClauseAsync)),
                diagnostic);

            // Option 2: Add logging
            context.RegisterCodeFix(
                CreateCodeAction(
                    AddLoggingTitle,
                    cancellationToken => AddLoggingAsync(document, root, catchClause, cancellationToken),
                    nameof(AddLoggingAsync)),
                diagnostic);
        }

        /// <summary>
        /// Removes the empty catch clause from the try statement.
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

        /// <summary>
        /// Adds logging to the empty catch block.
        /// </summary>
        private static Task<Document> AddLoggingAsync(
            Document document,
            SyntaxNode root,
            CatchClauseSyntax catchClause,
            CancellationToken cancellationToken)
        {
            // Ensure the catch has a declaration with a variable name
            CatchClauseSyntax newCatch;
            string exceptionVarName;

            if (catchClause.Declaration == null || string.IsNullOrEmpty(catchClause.Declaration.Identifier.Text))
            {
                // Add declaration: catch (ExceptionType ex)
                // Use existing type if available, otherwise use Exception
                TypeSyntax exceptionType;
                if (catchClause.Declaration?.Type != null)
                {
                    exceptionType = catchClause.Declaration.Type;
                }
                else
                {
                    exceptionType = SyntaxFactory.IdentifierName("Exception");
                }

                var declaration = SyntaxFactory.CatchDeclaration(
                    exceptionType,
                    SyntaxFactory.Identifier("ex"));

                exceptionVarName = "ex";
                var loggingStatement = CreateLoggingStatement(exceptionVarName);
                var newBlock = SyntaxFactory.Block(loggingStatement);

                newCatch = catchClause
                    .WithDeclaration(declaration)
                    .WithBlock(newBlock);
            }
            else
            {
                exceptionVarName = catchClause.Declaration.Identifier.Text;
                var loggingStatement = CreateLoggingStatement(exceptionVarName);
                var newBlock = SyntaxFactory.Block(loggingStatement);
                newCatch = catchClause.WithBlock(newBlock);
            }

            var newRoot = root.ReplaceNode(catchClause, newCatch);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        /// <summary>
        /// Creates a logging statement: Console.WriteLine($"Error: {ex.Message}");
        /// </summary>
        private static StatementSyntax CreateLoggingStatement(string exceptionVarName)
        {
            // Create: Console.WriteLine($"Error: {ex.Message}");
            var statement = SyntaxFactory.ParseStatement($"Console.WriteLine($\"Error: {{{exceptionVarName}.Message}}\");");

            // Add comment on its own line
            var commentTrivia = SyntaxFactory.TriviaList(
                SyntaxFactory.Comment("// TODO: Replace with proper logging"),
                SyntaxFactory.CarriageReturnLineFeed);

            return statement.WithLeadingTrivia(commentTrivia);
        }
    }
}
