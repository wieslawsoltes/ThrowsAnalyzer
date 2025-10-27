using System.Collections.Generic;
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
    /// Code fix provider for THROWS003: Method contains try-catch block.
    /// Offers options to remove try-catch or add logging to empty catches.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TryCatchCodeFixProvider))]
    [Shared]
    public class TryCatchCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        private const string RemoveTitle = "Remove try-catch and propagate exceptions";
        private const string AddLoggingTitle = "Add logging to empty catch blocks";

        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can fix.
        /// </summary>
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.DiagnosticId003);

        /// <summary>
        /// Registers code fixes for try-catch block diagnostics.
        /// </summary>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var (document, root) = await GetDocumentAndRootAsync(context, context.CancellationToken)
                .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            // Find the member containing the try statement
            var memberNode = root.FindNode(diagnostic.Location.SourceSpan);
            var tryStatements = memberNode.DescendantNodes()
                .OfType<TryStatementSyntax>()
                .ToList();

            if (tryStatements.Count == 0)
            {
                return;
            }

            // Option 1: Remove try-catch blocks and let exceptions propagate
            context.RegisterCodeFix(
                CreateCodeAction(
                    RemoveTitle,
                    cancellationToken => RemoveTryCatchAsync(document, root, memberNode, tryStatements, cancellationToken),
                    nameof(RemoveTryCatchAsync)),
                diagnostic);

            // Option 2: Add logging to empty catch blocks (if any exist)
            if (HasEmptyCatchBlocks(tryStatements))
            {
                context.RegisterCodeFix(
                    CreateCodeAction(
                        AddLoggingTitle,
                        cancellationToken => AddLoggingToEmptyCatchesAsync(document, root, tryStatements, cancellationToken),
                        nameof(AddLoggingToEmptyCatchesAsync)),
                    diagnostic);
            }
        }

        /// <summary>
        /// Checks if any try statement has empty catch blocks.
        /// </summary>
        private static bool HasEmptyCatchBlocks(IEnumerable<TryStatementSyntax> tryStatements)
        {
            return tryStatements.Any(tryStmt =>
                tryStmt.Catches.Any(catchClause => catchClause.Block.Statements.Count == 0));
        }

        /// <summary>
        /// Removes all try-catch blocks and replaces them with just the try block contents.
        /// </summary>
        private static Task<Document> RemoveTryCatchAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode memberNode,
            IEnumerable<TryStatementSyntax> tryStatements,
            CancellationToken cancellationToken)
        {
            var newRoot = root;

            // Process each try statement
            foreach (var tryStatement in tryStatements)
            {
                // Keep only the try block statements, remove catch and finally
                var tryBlockStatements = tryStatement.Block.Statements;

                // If the try block has only one statement and it's a block, unwrap it
                SyntaxNode replacement;
                if (tryBlockStatements.Count == 1)
                {
                    replacement = tryBlockStatements[0];
                }
                else
                {
                    // Multiple statements - create a new block or use the statements directly
                    // depending on context
                    var parent = tryStatement.Parent;
                    if (parent is BlockSyntax)
                    {
                        // Inside a block, we can replace with multiple statements
                        // But SyntaxNode.ReplaceNode doesn't support one-to-many replacement
                        // So we keep the block structure
                        replacement = SyntaxFactory.Block(tryBlockStatements)
                            .WithTriviaFrom(tryStatement);
                    }
                    else
                    {
                        replacement = SyntaxFactory.Block(tryBlockStatements)
                            .WithTriviaFrom(tryStatement);
                    }
                }

                // For simplicity, just unwrap the try block
                var unwrappedStatements = tryStatement.Block.Statements;

                // Replace the try statement with its block content
                if (unwrappedStatements.Count == 1)
                {
                    newRoot = newRoot.ReplaceNode(tryStatement, unwrappedStatements[0].WithTriviaFrom(tryStatement));
                }
                else if (unwrappedStatements.Count > 1)
                {
                    // Create a block with the try block's statements
                    var newBlock = SyntaxFactory.Block(unwrappedStatements)
                        .WithTriviaFrom(tryStatement);
                    newRoot = newRoot.ReplaceNode(tryStatement, newBlock);
                }
                else
                {
                    // Empty try block - just remove it
                    newRoot = newRoot.RemoveNode(tryStatement, SyntaxRemoveOptions.KeepNoTrivia);
                }
            }

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        /// <summary>
        /// Adds basic logging to all empty catch blocks.
        /// </summary>
        private static Task<Document> AddLoggingToEmptyCatchesAsync(
            Document document,
            SyntaxNode root,
            IEnumerable<TryStatementSyntax> tryStatements,
            CancellationToken cancellationToken)
        {
            var newRoot = root;

            foreach (var tryStatement in tryStatements)
            {
                var newCatches = new SyntaxList<CatchClauseSyntax>();

                foreach (var catchClause in tryStatement.Catches)
                {
                    if (catchClause.Block.Statements.Count == 0)
                    {
                        // Add logging statement
                        var exceptionVarName = catchClause.Declaration?.Identifier.Text ?? "ex";

                        // Ensure the catch has a declaration with a variable name
                        CatchClauseSyntax newCatch;
                        if (catchClause.Declaration == null || string.IsNullOrEmpty(catchClause.Declaration.Identifier.Text))
                        {
                            // Add declaration: catch (Exception ex)
                            var declaration = SyntaxFactory.CatchDeclaration(
                                SyntaxFactory.IdentifierName("Exception"),
                                SyntaxFactory.Identifier("ex"));

                            var loggingStatement = CreateLoggingStatement("ex");
                            var newBlock = SyntaxFactory.Block(loggingStatement);

                            newCatch = catchClause
                                .WithDeclaration(declaration)
                                .WithBlock(newBlock);
                        }
                        else
                        {
                            var loggingStatement = CreateLoggingStatement(exceptionVarName);
                            var newBlock = SyntaxFactory.Block(loggingStatement);
                            newCatch = catchClause.WithBlock(newBlock);
                        }

                        newCatches = newCatches.Add(newCatch);
                    }
                    else
                    {
                        newCatches = newCatches.Add(catchClause);
                    }
                }

                var newTryStatement = tryStatement.WithCatches(newCatches);
                newRoot = newRoot.ReplaceNode(tryStatement, newTryStatement);
            }

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
