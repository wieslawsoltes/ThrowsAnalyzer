using Microsoft.CodeAnalysis;

namespace ThrowsAnalyzer.TypeAnalysis.Models
{
    /// <summary>
    /// Contains information about a throw statement including its type.
    /// </summary>
    public class TypedThrowInfo
    {
        /// <summary>
        /// The throw statement or throw expression syntax node.
        /// </summary>
        public SyntaxNode ThrowNode { get; set; }

        /// <summary>
        /// The exception type being thrown, or null if it cannot be determined.
        /// </summary>
        public ITypeSymbol ExceptionType { get; set; }

        /// <summary>
        /// True if this is a bare rethrow (throw; with no expression).
        /// </summary>
        public bool IsRethrow { get; set; }

        /// <summary>
        /// The location of the throw statement for diagnostic reporting.
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
                return IsRethrow ? "(rethrow)" : "(unknown)";
            }
        }
    }
}
