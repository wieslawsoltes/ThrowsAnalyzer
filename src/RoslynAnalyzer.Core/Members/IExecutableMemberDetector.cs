using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace RoslynAnalyzer.Core.Members
{
    /// <summary>
    /// Interface for detecting executable members that can contain executable code.
    /// Supports methods, constructors, properties, local functions, lambdas, and more.
    /// </summary>
    /// <remarks>
    /// This interface provides a unified abstraction for working with all types of C# members
    /// that can contain executable code. Implementers can detect specific member types and
    /// extract their executable regions (method bodies, expression bodies, etc.).
    /// </remarks>
    public interface IExecutableMemberDetector
    {
        /// <summary>
        /// Determines if this detector supports the given syntax node type.
        /// </summary>
        /// <param name="node">The syntax node to check.</param>
        /// <returns>True if this detector can handle the node; otherwise, false.</returns>
        bool SupportsNode(SyntaxNode node);

        /// <summary>
        /// Gets all executable blocks from the syntax node (method body, expression body, etc.).
        /// These are the blocks that should be analyzed for executable code patterns.
        /// </summary>
        /// <param name="node">The syntax node to extract executable blocks from.</param>
        /// <returns>
        /// An enumerable of syntax nodes representing executable blocks.
        /// May return multiple blocks for members with both block and expression bodies.
        /// </returns>
        IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node);

        /// <summary>
        /// Gets a friendly name for the member (for diagnostic messages).
        /// </summary>
        /// <param name="node">The syntax node to get the display name for.</param>
        /// <returns>
        /// A human-readable string describing the member.
        /// Examples: "Method 'Foo'", "Constructor", "Property 'Name' getter"
        /// </returns>
        string GetMemberDisplayName(SyntaxNode node);
    }
}
