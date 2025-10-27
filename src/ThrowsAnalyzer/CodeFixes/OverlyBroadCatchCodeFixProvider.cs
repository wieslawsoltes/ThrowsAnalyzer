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
    /// Code fix provider for THROWS010: Overly broad exception catch.
    /// Offers to add a filter clause to make the catch more specific.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(OverlyBroadCatchCodeFixProvider))]
    [Shared]
    public class OverlyBroadCatchCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        private const string AddFilterTitle = "Add exception filter (when clause)";

        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can fix.
        /// </summary>
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.DiagnosticId010);

        /// <summary>
        /// Registers code fixes for overly broad catch diagnostics.
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

            // Only offer fix if there's no filter already
            if (catchClause.Filter == null)
            {
                context.RegisterCodeFix(
                    CreateCodeAction(
                        AddFilterTitle,
                        cancellationToken => AddFilterClauseAsync(document, root, catchClause, cancellationToken),
                        nameof(AddFilterClauseAsync)),
                    diagnostic);
            }
        }

        /// <summary>
        /// Adds a filter clause (when) to the catch block.
        /// </summary>
        private static Task<Document> AddFilterClauseAsync(
            Document document,
            SyntaxNode root,
            CatchClauseSyntax catchClause,
            CancellationToken cancellationToken)
        {
            // Ensure the catch has a declaration with a variable name
            CatchClauseSyntax catchWithDeclaration;
            string exceptionVarName;

            if (catchClause.Declaration == null || string.IsNullOrEmpty(catchClause.Declaration.Identifier.Text))
            {
                // Add declaration: catch (Exception ex)
                var exceptionTypeName = catchClause.Declaration?.Type.ToString() ?? "Exception";
                var declaration = SyntaxFactory.CatchDeclaration(
                    SyntaxFactory.IdentifierName(exceptionTypeName),
                    SyntaxFactory.Identifier("ex"));

                exceptionVarName = "ex";
                catchWithDeclaration = catchClause.WithDeclaration(declaration);
            }
            else
            {
                exceptionVarName = catchClause.Declaration.Identifier.Text;
                catchWithDeclaration = catchClause;
            }

            // Create filter clause: when (true)
            // Note: We use ParseExpression to get natural formatting
            var filterExpression = SyntaxFactory.ParseExpression("true /* TODO: Add condition */");
            var filterClause = SyntaxFactory.CatchFilterClause(filterExpression);

            var newCatch = catchWithDeclaration.WithFilter(filterClause);

            var newRoot = root.ReplaceNode(catchClause, newCatch);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}
