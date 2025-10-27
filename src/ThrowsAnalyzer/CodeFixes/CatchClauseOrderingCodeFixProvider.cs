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
using ThrowsAnalyzer.TypeAnalysis;
using ThrowsAnalyzer.Performance;

namespace ThrowsAnalyzer.CodeFixes
{
    /// <summary>
    /// Code fix provider for THROWS007: Unreachable catch clause due to ordering.
    /// Reorders catch clauses from most specific to most general.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CatchClauseOrderingCodeFixProvider))]
    [Shared]
    public class CatchClauseOrderingCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        private const string ReorderTitle = "Reorder catch clauses (specific to general)";

        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can fix.
        /// </summary>
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.DiagnosticId007);

        /// <summary>
        /// Registers code fixes for catch clause ordering diagnostics.
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

            // Find the containing try statement
            var tryStatement = catchClause.FirstAncestorOrSelf<TryStatementSyntax>();
            if (tryStatement == null)
            {
                return;
            }

            // Register code fix to reorder catch clauses
            context.RegisterCodeFix(
                CreateCodeAction(
                    ReorderTitle,
                    cancellationToken => ReorderCatchClausesAsync(document, root, tryStatement, cancellationToken),
                    nameof(ReorderCatchClausesAsync)),
                diagnostic);
        }

        /// <summary>
        /// Reorders catch clauses from most specific to most general.
        /// </summary>
        private static async Task<Document> ReorderCatchClausesAsync(
            Document document,
            SyntaxNode root,
            TryStatementSyntax tryStatement,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken)
                .ConfigureAwait(false);

            if (semanticModel == null)
            {
                return document;
            }

            // Get exception type information for each catch clause
            var catchInfos = new List<(CatchClauseSyntax Clause, ITypeSymbol? ExceptionType, int OriginalIndex)>();

            for (int i = 0; i < tryStatement.Catches.Count; i++)
            {
                var catchClause = tryStatement.Catches[i];
                var exceptionType = ExceptionTypeAnalyzer.GetCaughtExceptionType(catchClause, semanticModel);
                catchInfos.Add((catchClause, exceptionType, i));
            }

            // Sort catch clauses by specificity (most specific first)
            // General catch (no type) goes last
            // Then sort by inheritance hierarchy depth
            var sortedCatches = catchInfos
                .OrderBy(info =>
                {
                    if (info.ExceptionType == null)
                    {
                        // General catch - goes last
                        return int.MaxValue;
                    }

                    // Use cached inheritance depth calculation for better performance
                    int depth = ExceptionTypeCache.GetInheritanceDepth(info.ExceptionType);

                    // Return negative depth so more specific (deeper) types come first
                    return -depth;
                })
                .ThenBy(info => info.OriginalIndex) // Preserve original order for same depth
                .Select(info => info.Clause)
                .ToList();

            // Create new try statement with reordered catches
            var newTryStatement = tryStatement.WithCatches(
                SyntaxFactory.List(sortedCatches));

            var newRoot = root.ReplaceNode(tryStatement, newTryStatement);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
