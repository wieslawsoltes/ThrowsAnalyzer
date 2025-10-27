namespace ThrowsAnalyzer.TypeAnalysis.Models
{
    /// <summary>
    /// Represents a catch clause ordering issue where a catch is unreachable.
    /// </summary>
    public class CatchClauseOrderingIssue
    {
        /// <summary>
        /// The catch clause that is unreachable.
        /// </summary>
        public CatchClauseInfo UnreachableClause { get; set; }

        /// <summary>
        /// The catch clause that masks the unreachable clause.
        /// </summary>
        public CatchClauseInfo MaskedByClause { get; set; }

        /// <summary>
        /// Human-readable explanation of why the clause is unreachable.
        /// </summary>
        public string Reason { get; set; }
    }
}
