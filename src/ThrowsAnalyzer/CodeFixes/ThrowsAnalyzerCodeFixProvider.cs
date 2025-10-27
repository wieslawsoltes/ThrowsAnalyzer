using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ThrowsAnalyzer.CodeFixes
{
    /// <summary>
    /// Base class for all ThrowsAnalyzer code fix providers.
    /// Provides common utilities for code fix implementations.
    /// </summary>
    public abstract class ThrowsAnalyzerCodeFixProvider : CodeFixProvider
    {
        /// <summary>
        /// Gets the title for the code fix action.
        /// Override this in derived classes if needed.
        /// </summary>
        protected virtual string Title => string.Empty;

        /// <summary>
        /// Gets the equivalence key prefix for this code fix provider.
        /// Used to identify and group related code fixes.
        /// </summary>
        protected virtual string EquivalenceKeyPrefix => GetType().Name;

        /// <summary>
        /// Creates a code action with consistent naming and behavior.
        /// </summary>
        /// <param name="title">The title displayed to the user in the IDE.</param>
        /// <param name="createChangedDocument">Function that creates the modified document.</param>
        /// <param name="equivalenceKey">Unique key for this code action.</param>
        /// <returns>A code action that can be registered with the code fix context.</returns>
        protected CodeAction CreateCodeAction(
            string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey)
        {
            return CodeAction.Create(
                title: title,
                createChangedDocument: createChangedDocument,
                equivalenceKey: $"{EquivalenceKeyPrefix}:{equivalenceKey}");
        }

        /// <summary>
        /// Creates a code action with consistent naming and behavior for solution-wide changes.
        /// </summary>
        /// <param name="title">The title displayed to the user in the IDE.</param>
        /// <param name="createChangedSolution">Function that creates the modified solution.</param>
        /// <param name="equivalenceKey">Unique key for this code action.</param>
        /// <returns>A code action that can be registered with the code fix context.</returns>
        protected CodeAction CreateCodeAction(
            string title,
            Func<CancellationToken, Task<Solution>> createChangedSolution,
            string equivalenceKey)
        {
            return CodeAction.Create(
                title: title,
                createChangedSolution: createChangedSolution,
                equivalenceKey: $"{EquivalenceKeyPrefix}:{equivalenceKey}");
        }

        /// <summary>
        /// Override this to specify the maximum number of times to automatically fix the same diagnostic.
        /// Returns null by default (no automatic fixing).
        /// </summary>
        public override FixAllProvider GetFixAllProvider()
        {
            // Enable batch fixing for all code fix providers by default
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <summary>
        /// Helper method to get document and root syntax node from context.
        /// </summary>
        protected static async Task<(Document document, SyntaxNode root)> GetDocumentAndRootAsync(
            CodeFixContext context,
            CancellationToken cancellationToken)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                throw new InvalidOperationException("Unable to get syntax root");

            return (document, root);
        }
    }
}
