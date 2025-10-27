using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThrowsAnalyzer.TypeAnalysis.Models
{
    /// <summary>
    /// Contains information about a catch clause including its type.
    /// </summary>
    public class CatchClauseInfo
    {
        /// <summary>
        /// The catch clause syntax node.
        /// </summary>
        public CatchClauseSyntax CatchClause { get; set; }

        /// <summary>
        /// The exception type caught by this clause, or null for general catch.
        /// </summary>
        public ITypeSymbol ExceptionType { get; set; }

        /// <summary>
        /// True if this is a general catch clause (catch { }).
        /// </summary>
        public bool IsGeneralCatch { get; set; }

        /// <summary>
        /// True if this catch clause has a when filter.
        /// </summary>
        public bool HasFilter { get; set; }

        /// <summary>
        /// The filter clause if present, or null.
        /// </summary>
        public CatchFilterClauseSyntax Filter { get; set; }

        /// <summary>
        /// The location of the catch clause for diagnostic reporting.
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// Gets a display name for the exception type.
        /// </summary>
        public string ExceptionTypeName
        {
            get
            {
                if (ExceptionType != null)
                    return ExceptionType.ToDisplayString();
                return "(general catch)";
            }
        }
    }
}
