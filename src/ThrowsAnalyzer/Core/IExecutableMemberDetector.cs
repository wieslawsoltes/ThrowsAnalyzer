using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace ThrowsAnalyzer.Core
{
    /// <summary>
    /// Interface for detecting executable members that can contain throw statements and try/catch blocks.
    /// Supports methods, constructors, properties, local functions, lambdas, etc.
    /// </summary>
    public interface IExecutableMemberDetector
    {
        /// <summary>
        /// Determines if this detector supports the given syntax node type.
        /// </summary>
        bool SupportsNode(SyntaxNode node);

        /// <summary>
        /// Gets all executable blocks from the syntax node (method body, expression body, etc.).
        /// These are the blocks that should be analyzed for throw statements and try/catch.
        /// </summary>
        IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node);

        /// <summary>
        /// Gets a friendly name for the member (for diagnostic messages).
        /// Examples: "Method 'Foo'", "Constructor", "Property 'Name' getter"
        /// </summary>
        string GetMemberDisplayName(SyntaxNode node);
    }
}
