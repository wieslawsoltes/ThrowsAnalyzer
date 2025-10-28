using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using RoslynAnalyzer.Core.Members.Detectors;

namespace RoslynAnalyzer.Core.Members
{
    /// <summary>
    /// Central utility for working with executable members across different syntax node types.
    /// Provides unified access to all member types that can contain executable code.
    /// </summary>
    /// <remarks>
    /// This class serves as the main entry point for member detection. It maintains a registry
    /// of all available detectors and provides convenience methods for common operations.
    /// Supports: methods, constructors, destructors, operators, properties, accessors,
    /// local functions, lambdas, and anonymous methods.
    /// </remarks>
    public static class ExecutableMemberHelper
    {
        private static readonly IExecutableMemberDetector[] Detectors = new IExecutableMemberDetector[]
        {
            new MethodMemberDetector(),
            new ConstructorMemberDetector(),
            new DestructorMemberDetector(),
            new OperatorMemberDetector(),
            new ConversionOperatorMemberDetector(),
            new PropertyMemberDetector(),
            new AccessorMemberDetector(),
            new LocalFunctionMemberDetector(),
            new LambdaMemberDetector(),
            new AnonymousMethodMemberDetector()
        };

        /// <summary>
        /// Determines if the node is an executable member (method, constructor, property, etc.).
        /// </summary>
        /// <param name="node">The syntax node to check.</param>
        /// <returns>True if the node represents an executable member; otherwise, false.</returns>
        public static bool IsExecutableMember(SyntaxNode node)
        {
            return GetDetectorForNode(node) != null;
        }

        /// <summary>
        /// Gets all executable blocks from the node.
        /// Returns empty if node is not an executable member.
        /// </summary>
        /// <param name="node">The syntax node to extract blocks from.</param>
        /// <returns>
        /// An enumerable of syntax nodes representing executable code blocks.
        /// Returns empty enumerable if the node is not an executable member.
        /// </returns>
        /// <remarks>
        /// This method handles both block-bodied and expression-bodied members.
        /// For example, a method can have both a block body and an expression body
        /// (though typically only one is present).
        /// </remarks>
        public static IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node)
        {
            var detector = GetDetectorForNode(node);
            if (detector == null)
            {
                return Enumerable.Empty<SyntaxNode>();
            }

            return detector.GetExecutableBlocks(node);
        }

        /// <summary>
        /// Gets a friendly display name for the member.
        /// </summary>
        /// <param name="node">The syntax node to get the display name for.</param>
        /// <returns>
        /// A human-readable string describing the member.
        /// Examples: "Method 'Foo'", "Constructor", "Property 'Name' getter"
        /// Returns "Member" if the node type is not recognized.
        /// </returns>
        public static string GetMemberDisplayName(SyntaxNode node)
        {
            var detector = GetDetectorForNode(node);
            if (detector == null)
            {
                return "Member";
            }

            return detector.GetMemberDisplayName(node);
        }

        /// <summary>
        /// Gets the appropriate detector for the given node, or null if unsupported.
        /// </summary>
        /// <param name="node">The syntax node to find a detector for.</param>
        /// <returns>
        /// The first detector that supports the node, or null if no detector matches.
        /// </returns>
        private static IExecutableMemberDetector? GetDetectorForNode(SyntaxNode node)
        {
            foreach (var detector in Detectors)
            {
                if (detector.SupportsNode(node))
                {
                    return detector;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets all registered detectors. Useful for extending functionality.
        /// </summary>
        /// <returns>A read-only list of all registered detectors.</returns>
        public static IReadOnlyList<IExecutableMemberDetector> GetAllDetectors()
        {
            return Detectors;
        }
    }
}
