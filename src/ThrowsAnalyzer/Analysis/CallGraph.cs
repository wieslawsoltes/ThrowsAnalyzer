using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ThrowsAnalyzer.Analysis
{
    /// <summary>
    /// Represents a call graph showing method invocation relationships.
    /// </summary>
    public class CallGraph
    {
        private readonly Dictionary<IMethodSymbol, CallGraphNode> _nodes;

        public CallGraph()
        {
            _nodes = new Dictionary<IMethodSymbol, CallGraphNode>(SymbolEqualityComparer.Default);
        }

        /// <summary>
        /// Gets or adds a node for the specified method.
        /// </summary>
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
        public IEnumerable<CallGraphNode> Nodes => _nodes.Values;

        /// <summary>
        /// Tries to get a node for the specified method.
        /// </summary>
        public bool TryGetNode(IMethodSymbol method, out CallGraphNode node)
        {
            return _nodes.TryGetValue(method, out node);
        }

        /// <summary>
        /// Gets the number of nodes in the call graph.
        /// </summary>
        public int NodeCount => _nodes.Count;

        /// <summary>
        /// Gets the total number of edges (calls) in the call graph.
        /// </summary>
        public int EdgeCount => _nodes.Values.Sum(n => n.Callees.Count);
    }

    /// <summary>
    /// Represents a node in the call graph (a method).
    /// </summary>
    public class CallGraphNode
    {
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
        public List<CallGraphEdge> Callees { get; }

        /// <summary>
        /// Methods that call this method.
        /// </summary>
        public List<CallGraphEdge> Callers { get; }

        /// <summary>
        /// Gets the call depth from the root (0 if no callers).
        /// </summary>
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
    public class CallGraphEdge
    {
        /// <summary>
        /// The target node (callee or caller).
        /// </summary>
        public CallGraphNode Target { get; set; }

        /// <summary>
        /// The location of the call site in source code.
        /// </summary>
        public Location CallSite { get; set; }
    }
}
