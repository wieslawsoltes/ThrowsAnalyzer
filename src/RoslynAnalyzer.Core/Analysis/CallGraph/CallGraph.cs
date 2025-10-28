using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace RoslynAnalyzer.Core.Analysis.CallGraph
{
    /// <summary>
    /// Represents a call graph showing method invocation relationships.
    /// </summary>
    /// <remarks>
    /// A call graph is a directed graph where nodes represent methods and edges represent
    /// method invocations. This implementation supports:
    /// - Bidirectional edges (tracks both callers and callees)
    /// - Multiple edges between the same pair of methods
    /// - Cycle detection
    /// - Depth calculation
    ///
    /// Use CallGraphBuilder to construct call graphs from Roslyn compilations.
    /// </remarks>
    public class CallGraph
    {
        private readonly Dictionary<IMethodSymbol, CallGraphNode> _nodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallGraph"/> class.
        /// </summary>
        public CallGraph()
        {
            _nodes = new Dictionary<IMethodSymbol, CallGraphNode>(SymbolEqualityComparer.Default);
        }

        /// <summary>
        /// Gets or adds a node for the specified method.
        /// </summary>
        /// <param name="method">The method symbol to get or create a node for.</param>
        /// <returns>The node representing the method, either existing or newly created.</returns>
        public CallGraphNode GetOrAddNode(IMethodSymbol method)
        {
            if (!_nodes.TryGetValue(method, out var node))
            {
                node = new CallGraphNode(method);
                _nodes[method] = node;
            }
            return node;
        }

        /// <summary>
        /// Adds an edge from caller to callee.
        /// </summary>
        /// <param name="caller">The method symbol that makes the call.</param>
        /// <param name="callee">The method symbol being called.</param>
        /// <param name="callSite">The source location of the call site.</param>
        /// <remarks>
        /// This method automatically creates nodes for both caller and callee if they don't exist,
        /// and updates both the caller's callees list and the callee's callers list.
        /// Multiple edges can exist between the same pair of methods (e.g., multiple call sites).
        /// </remarks>
        public void AddEdge(IMethodSymbol caller, IMethodSymbol callee, Location callSite)
        {
            var callerNode = GetOrAddNode(caller);
            var calleeNode = GetOrAddNode(callee);

            callerNode.Callees.Add(new CallGraphEdge
            {
                Target = calleeNode,
                CallSite = callSite
            });

            calleeNode.Callers.Add(new CallGraphEdge
            {
                Target = callerNode,
                CallSite = callSite
            });
        }

        /// <summary>
        /// Gets all nodes in the call graph.
        /// </summary>
        /// <value>An enumerable of all nodes in the graph.</value>
        public IEnumerable<CallGraphNode> Nodes => _nodes.Values;

        /// <summary>
        /// Tries to get a node for the specified method.
        /// </summary>
        /// <param name="method">The method symbol to look up.</param>
        /// <param name="node">When this method returns, contains the node if found; otherwise, null.</param>
        /// <returns>True if the node was found; otherwise, false.</returns>
        public bool TryGetNode(IMethodSymbol method, out CallGraphNode? node)
        {
            return _nodes.TryGetValue(method, out node);
        }

        /// <summary>
        /// Gets the number of nodes in the call graph.
        /// </summary>
        /// <value>The total number of methods in the graph.</value>
        public int NodeCount => _nodes.Count;

        /// <summary>
        /// Gets the total number of edges (calls) in the call graph.
        /// </summary>
        /// <value>The total number of method calls represented in the graph.</value>
        /// <remarks>
        /// This counts each edge once (from caller to callee). Since edges are bidirectional,
        /// this count represents the total number of call sites, not the total number of edge objects.
        /// </remarks>
        public int EdgeCount => _nodes.Values.Sum(n => n.Callees.Count);
    }

    /// <summary>
    /// Represents a node in the call graph (a method).
    /// </summary>
    /// <remarks>
    /// Each node maintains bidirectional edge lists:
    /// - Callees: methods called by this method
    /// - Callers: methods that call this method
    ///
    /// This allows efficient traversal in both directions (bottom-up or top-down).
    /// </remarks>
    public class CallGraphNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallGraphNode"/> class.
        /// </summary>
        /// <param name="method">The method symbol this node represents.</param>
        public CallGraphNode(IMethodSymbol method)
        {
            Method = method;
            Callees = new List<CallGraphEdge>();
            Callers = new List<CallGraphEdge>();
        }

        /// <summary>
        /// The method this node represents.
        /// </summary>
        public IMethodSymbol Method { get; }

        /// <summary>
        /// Methods called by this method.
        /// </summary>
        /// <remarks>
        /// Each edge includes the target node and the call site location.
        /// Multiple edges may exist for methods called from multiple locations.
        /// </remarks>
        public List<CallGraphEdge> Callees { get; }

        /// <summary>
        /// Methods that call this method.
        /// </summary>
        /// <remarks>
        /// Each edge includes the caller node and the call site location.
        /// Multiple edges may exist if this method is called from multiple locations.
        /// </remarks>
        public List<CallGraphEdge> Callers { get; }

        /// <summary>
        /// Gets the call depth from the root (0 if no callers).
        /// </summary>
        /// <returns>
        /// The depth of this node in the call graph.
        /// Returns 0 if this is a root node (no callers).
        /// Returns the maximum depth if called through multiple paths.
        /// Returns 0 if a cycle is detected.
        /// </returns>
        /// <remarks>
        /// This method performs a depth-first search up the call chain to compute depth.
        /// Cycles are detected and handled by returning 0 for the cyclic portion.
        /// The algorithm uses backtracking to allow nodes to appear in multiple paths.
        /// Time complexity: O(V + E) where V is vertices and E is edges.
        /// </remarks>
        public int GetDepth()
        {
            if (!Callers.Any())
                return 0;

            var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            return GetDepthRecursive(this, visited);
        }

        private static int GetDepthRecursive(CallGraphNode node, HashSet<IMethodSymbol> visited)
        {
            if (!node.Callers.Any())
                return 0;

            if (visited.Contains(node.Method))
                return 0; // Cycle detected

            visited.Add(node.Method);

            var maxDepth = 0;
            foreach (var caller in node.Callers)
            {
                var depth = GetDepthRecursive(caller.Target, visited);
                if (depth > maxDepth)
                    maxDepth = depth;
            }

            visited.Remove(node.Method);
            return maxDepth + 1;
        }
    }

    /// <summary>
    /// Represents an edge in the call graph (a method call).
    /// </summary>
    /// <remarks>
    /// Edges are bidirectional - each edge appears in both:
    /// - The caller's Callees list
    /// - The callee's Callers list
    /// </remarks>
    public class CallGraphEdge
    {
        /// <summary>
        /// The target node (callee or caller).
        /// </summary>
        /// <remarks>
        /// If this edge is in a node's Callees list, Target is the callee.
        /// If this edge is in a node's Callers list, Target is the caller.
        /// </remarks>
        public CallGraphNode Target { get; set; } = null!;

        /// <summary>
        /// The location of the call site in source code.
        /// </summary>
        /// <remarks>
        /// This location can be used for diagnostic reporting, navigation, or debugging.
        /// </remarks>
        public Location CallSite { get; set; } = null!;
    }
}
