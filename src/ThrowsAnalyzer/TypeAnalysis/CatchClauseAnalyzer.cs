using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ThrowsAnalyzer.TypeAnalysis.Models;

namespace ThrowsAnalyzer.TypeAnalysis
{
    /// <summary>
    /// Analyzes catch clauses with type information.
    /// Detects ordering issues, dead clauses, and overly broad catches.
    /// </summary>
    public static class CatchClauseAnalyzer
    {
        /// <summary>
        /// Gets all catch clauses in a try statement with type information.
        /// </summary>
        /// <param name="tryStatement">The try statement to analyze</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <returns>Enumerable of CatchClauseInfo for each catch clause</returns>
        public static IEnumerable<CatchClauseInfo> GetCatchClauses(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel)
        {
            var catches = new List<CatchClauseInfo>();

            foreach (var catchClause in tryStatement.Catches)
            {
                var exceptionType = ExceptionTypeAnalyzer
                    .GetCaughtExceptionType(catchClause, semanticModel);

                var isGeneralCatch = catchClause.Declaration == null;
                var hasFilter = catchClause.Filter != null;

                catches.Add(new CatchClauseInfo
                {
                    CatchClause = catchClause,
                    ExceptionType = exceptionType,
                    IsGeneralCatch = isGeneralCatch,
                    HasFilter = hasFilter,
                    Filter = catchClause.Filter,
                    Location = catchClause.GetLocation()
                });
            }

            return catches;
        }

        /// <summary>
        /// Detects catch clause ordering issues.
        /// Returns clauses that are unreachable due to previous broader catches.
        /// </summary>
        /// <param name="tryStatement">The try statement to analyze</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <returns>Enumerable of ordering issues found</returns>
        public static IEnumerable<CatchClauseOrderingIssue> DetectOrderingIssues(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel)
        {
            var catches = GetCatchClauses(tryStatement, semanticModel).ToList();
            var issues = new List<CatchClauseOrderingIssue>();

            for (int i = 0; i < catches.Count; i++)
            {
                var current = catches[i];

                // Check if any previous catch already handles this type
                for (int j = 0; j < i; j++)
                {
                    var previous = catches[j];

                    // Skip if either has filter - filters change reachability
                    if (current.HasFilter || previous.HasFilter)
                        continue;

                    if (IsCaughtBy(current, previous, semanticModel.Compilation))
                    {
                        issues.Add(new CatchClauseOrderingIssue
                        {
                            UnreachableClause = current,
                            MaskedByClause = previous,
                            Reason = $"This catch is unreachable because " +
                                    $"'{previous.ExceptionTypeName}' is caught first"
                        });
                        break;
                    }
                }
            }

            return issues;
        }

        /// <summary>
        /// Checks if currentCatch is made unreachable by previousCatch.
        /// </summary>
        /// <param name="currentCatch">The catch clause being checked</param>
        /// <param name="previousCatch">The potentially masking catch clause</param>
        /// <param name="compilation">Compilation context</param>
        /// <returns>True if currentCatch is unreachable due to previousCatch</returns>
        private static bool IsCaughtBy(
            CatchClauseInfo currentCatch,
            CatchClauseInfo previousCatch,
            Compilation compilation)
        {
            // General catch catches everything
            if (previousCatch.IsGeneralCatch)
                return true;

            // If current is general catch but previous is not, it's reachable
            if (currentCatch.IsGeneralCatch)
                return false;

            // Check type hierarchy
            if (currentCatch.ExceptionType != null && previousCatch.ExceptionType != null)
            {
                return ExceptionTypeAnalyzer.IsAssignableTo(
                    currentCatch.ExceptionType,
                    previousCatch.ExceptionType,
                    compilation);
            }

            return false;
        }

        /// <summary>
        /// Detects empty catch blocks (exception swallowing).
        /// </summary>
        /// <param name="tryStatement">The try statement to analyze</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <returns>Enumerable of empty catch clauses</returns>
        public static IEnumerable<CatchClauseInfo> DetectEmptyCatches(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel)
        {
            var catches = GetCatchClauses(tryStatement, semanticModel);

            return catches.Where(c => IsEmptyCatch(c.CatchClause));
        }

        /// <summary>
        /// Checks if a catch clause has an empty block.
        /// </summary>
        /// <param name="catchClause">The catch clause to check</param>
        /// <returns>True if the catch block is empty</returns>
        private static bool IsEmptyCatch(CatchClauseSyntax catchClause)
        {
            var block = catchClause.Block;

            // No statements or only comments
            return block.Statements.Count == 0;
        }

        /// <summary>
        /// Detects catch blocks that only rethrow (unnecessary catch).
        /// </summary>
        /// <param name="tryStatement">The try statement to analyze</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <returns>Enumerable of rethrow-only catch clauses</returns>
        public static IEnumerable<CatchClauseInfo> DetectRethrowOnlyCatches(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel)
        {
            var catches = GetCatchClauses(tryStatement, semanticModel);

            return catches.Where(c => IsRethrowOnly(c.CatchClause));
        }

        /// <summary>
        /// Checks if a catch clause only contains a bare rethrow statement.
        /// </summary>
        /// <param name="catchClause">The catch clause to check</param>
        /// <returns>True if the catch block only rethrows</returns>
        private static bool IsRethrowOnly(CatchClauseSyntax catchClause)
        {
            var block = catchClause.Block;

            // Only one statement and it's a bare rethrow
            if (block.Statements.Count == 1)
            {
                var stmt = block.Statements[0];
                if (stmt is ThrowStatementSyntax throwStmt && throwStmt.Expression == null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Detects overly broad catches (catching Exception or SystemException).
        /// </summary>
        /// <param name="tryStatement">The try statement to analyze</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <returns>Enumerable of overly broad catch clauses</returns>
        public static IEnumerable<CatchClauseInfo> DetectOverlyBroadCatches(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel)
        {
            var catches = GetCatchClauses(tryStatement, semanticModel);
            var compilation = semanticModel.Compilation;

            var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
            var systemExceptionType = compilation.GetTypeByMetadataName("System.SystemException");

            return catches.Where(c =>
            {
                if (c.ExceptionType == null)
                    return false;

                return SymbolEqualityComparer.Default.Equals(c.ExceptionType, exceptionType)
                    || SymbolEqualityComparer.Default.Equals(c.ExceptionType, systemExceptionType);
            });
        }
    }
}
