using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using ThrowsAnalyzer.Core.MemberDetectors;

namespace ThrowsAnalyzer.Core
{
    /// <summary>
    /// Central utility for working with executable members across different syntax node types.
    /// Provides unified access to all member types that can contain executable code.
    /// </summary>
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
        public static bool IsExecutableMember(SyntaxNode node)
        {
            return GetDetectorForNode(node) != null;
        }

        /// <summary>
        /// Gets all executable blocks from the node.
        /// Returns empty if node is not an executable member.
        /// </summary>
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
        /// Examples: "Method 'Foo'", "Constructor", "Property 'Name' getter"
        /// </summary>
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
        private static IExecutableMemberDetector GetDetectorForNode(SyntaxNode node)
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
        public static IReadOnlyList<IExecutableMemberDetector> GetAllDetectors()
        {
            return Detectors;
        }
    }
}
