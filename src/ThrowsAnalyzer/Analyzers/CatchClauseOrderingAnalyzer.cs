using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ThrowsAnalyzer.Core;
using ThrowsAnalyzer.TypeAnalysis;

namespace ThrowsAnalyzer
{
    /// <summary>
    /// Analyzer that detects issues with catch clause ordering and catch block patterns.
    /// Reports THROWS007 (ordering), THROWS008 (empty), THROWS009 (rethrow-only), and THROWS010 (overly broad).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CatchClauseOrderingAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                MethodThrowsDiagnosticsBuilder.CatchClauseOrdering,
                MethodThrowsDiagnosticsBuilder.EmptyCatchBlock,
                MethodThrowsDiagnosticsBuilder.RethrowOnlyCatch,
                MethodThrowsDiagnosticsBuilder.OverlyBroadCatch);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeTryStatement,
                SyntaxKind.TryStatement);
        }

        private static void AnalyzeTryStatement(SyntaxNodeAnalysisContext context)
        {
            var tryStmt = (TryStatementSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            // Get containing member for error messages
            var memberNode = GetContainingMember(tryStmt);
            if (memberNode == null)
                return;

            var memberName = ExecutableMemberHelper.GetMemberDisplayName(memberNode);

            // Check ordering issues (THROWS007)
            var orderingIssues = CatchClauseAnalyzer
                .DetectOrderingIssues(tryStmt, semanticModel);

            foreach (var issue in orderingIssues)
            {
                var diagnostic = Diagnostic.Create(
                    MethodThrowsDiagnosticsBuilder.CatchClauseOrdering,
                    issue.UnreachableClause.Location,
                    issue.UnreachableClause.ExceptionTypeName,
                    issue.MaskedByClause.ExceptionTypeName);

                context.ReportDiagnostic(diagnostic);
            }

            // Check empty catches (THROWS008)
            var emptyCatches = CatchClauseAnalyzer
                .DetectEmptyCatches(tryStmt, semanticModel);

            foreach (var catchInfo in emptyCatches)
            {
                var diagnostic = Diagnostic.Create(
                    MethodThrowsDiagnosticsBuilder.EmptyCatchBlock,
                    catchInfo.Location,
                    memberName);

                context.ReportDiagnostic(diagnostic);
            }

            // Check rethrow-only catches (THROWS009)
            var rethrowOnlyCatches = CatchClauseAnalyzer
                .DetectRethrowOnlyCatches(tryStmt, semanticModel);

            foreach (var catchInfo in rethrowOnlyCatches)
            {
                var diagnostic = Diagnostic.Create(
                    MethodThrowsDiagnosticsBuilder.RethrowOnlyCatch,
                    catchInfo.Location,
                    memberName);

                context.ReportDiagnostic(diagnostic);
            }

            // Check overly broad catches (THROWS010)
            var broadCatches = CatchClauseAnalyzer
                .DetectOverlyBroadCatches(tryStmt, semanticModel);

            foreach (var catchInfo in broadCatches)
            {
                var diagnostic = Diagnostic.Create(
                    MethodThrowsDiagnosticsBuilder.OverlyBroadCatch,
                    catchInfo.Location,
                    memberName,
                    catchInfo.ExceptionTypeName);

                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Gets the containing executable member for a syntax node.
        /// </summary>
        private static SyntaxNode GetContainingMember(SyntaxNode node)
        {
            return node.Ancestors()
                .FirstOrDefault(n => ExecutableMemberHelper.IsExecutableMember(n));
        }
    }
}
