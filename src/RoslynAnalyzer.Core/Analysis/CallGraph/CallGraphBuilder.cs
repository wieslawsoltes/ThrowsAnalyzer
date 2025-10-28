using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalyzer.Core.Analysis.CallGraph
{
    /// <summary>
    /// Builds a call graph for methods in a compilation.
    /// </summary>
    /// <remarks>
    /// This builder analyzes a Roslyn compilation to construct a call graph showing
    /// method invocation relationships. It supports:
    /// - Regular methods, constructors, destructors, operators
    /// - Local functions
    /// - Object creation (constructor calls)
    /// - Async/await (analyzes the underlying calls)
    /// - Expression-bodied members
    ///
    /// The builder is thread-safe when used with proper cancellation token handling.
    /// For large compilations, consider using cancellation tokens to support responsive cancellation.
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = new CallGraphBuilder(compilation, cancellationToken);
    /// var graph = await builder.BuildAsync();
    ///
    /// // Analyze the graph
    /// foreach (var node in graph.Nodes)
    /// {
    ///     Console.WriteLine($"{node.Method.Name} calls {node.Callees.Count} methods");
    /// }
    /// </code>
    /// </example>
    public class CallGraphBuilder
    {
        private readonly Compilation _compilation;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallGraphBuilder"/> class.
        /// </summary>
        /// <param name="compilation">The compilation to analyze.</param>
        /// <param name="cancellationToken">A cancellation token for long-running operations.</param>
        public CallGraphBuilder(Compilation compilation, CancellationToken cancellationToken = default)
        {
            _compilation = compilation;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Builds a call graph for all methods in the compilation.
        /// </summary>
        /// <returns>
        /// A <see cref="CallGraph"/> containing all method invocation relationships
        /// found in the compilation.
        /// </returns>
        /// <remarks>
        /// This method processes all syntax trees in the compilation and analyzes:
        /// 1. All method declarations (methods, constructors, operators, etc.)
        /// 2. All local functions
        /// 3. All method invocations within these members
        /// 4. All constructor calls (object creation expressions)
        ///
        /// Time complexity: O(M * I) where M is number of methods and I is average invocations per method.
        /// Space complexity: O(M + E) where M is methods and E is edges (invocations).
        ///
        /// For very large compilations, this operation may take significant time.
        /// Use the cancellation token to support cancellation.
        /// </remarks>
        public async Task<CallGraph> BuildAsync()
        {
            var callGraph = new CallGraph();

            // 1. Process each syntax tree in the compilation
            foreach (var syntaxTree in _compilation.SyntaxTrees)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var semanticModel = _compilation.GetSemanticModel(syntaxTree);
                var root = await syntaxTree.GetRootAsync(_cancellationToken).ConfigureAwait(false);

                // 2. Find all method declarations
                var methodDeclarations = root.DescendantNodes()
                    .OfType<BaseMethodDeclarationSyntax>();

                foreach (var methodDecl in methodDeclarations)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl, _cancellationToken);
                    if (methodSymbol == null)
                        continue;

                    // 3. Analyze method body for invocations
                    await AnalyzeMethodAsync(methodSymbol, methodDecl, semanticModel, callGraph)
                        .ConfigureAwait(false);
                }

                // 4. Also process local functions
                var localFunctions = root.DescendantNodes()
                    .OfType<LocalFunctionStatementSyntax>();

                foreach (var localFunc in localFunctions)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var localSymbol = semanticModel.GetDeclaredSymbol(localFunc, _cancellationToken);
                    if (localSymbol == null)
                        continue;

                    await AnalyzeLocalFunctionAsync(localSymbol, localFunc, semanticModel, callGraph)
                        .ConfigureAwait(false);
                }
            }

            return callGraph;
        }

        /// <summary>
        /// Builds a call graph for a specific method.
        /// </summary>
        /// <param name="method">The method symbol to analyze.</param>
        /// <returns>
        /// A <see cref="CallGraph"/> containing the method and its direct callees.
        /// </returns>
        /// <remarks>
        /// This method only analyzes the specified method, not the entire compilation.
        /// The resulting graph will contain:
        /// - The specified method as a node
        /// - All methods directly called by the specified method
        /// - Edges representing the direct calls
        ///
        /// This is more efficient than BuildAsync() when you only need to analyze a single method.
        /// </remarks>
        public async Task<CallGraph> BuildForMethodAsync(IMethodSymbol method)
        {
            var callGraph = new CallGraph();

            // Find the method's syntax node
            var syntaxReferences = method.DeclaringSyntaxReferences;
            if (!syntaxReferences.Any())
                return callGraph;

            foreach (var syntaxRef in syntaxReferences)
            {
                var syntaxNode = await syntaxRef.GetSyntaxAsync(_cancellationToken).ConfigureAwait(false);
                var semanticModel = _compilation.GetSemanticModel(syntaxNode.SyntaxTree);

                if (syntaxNode is BaseMethodDeclarationSyntax methodDecl)
                {
                    await AnalyzeMethodAsync(method, methodDecl, semanticModel, callGraph)
                        .ConfigureAwait(false);
                }
                else if (syntaxNode is LocalFunctionStatementSyntax localFunc)
                {
                    await AnalyzeLocalFunctionAsync(method, localFunc, semanticModel, callGraph)
                        .ConfigureAwait(false);
                }
            }

            return callGraph;
        }

        private async Task AnalyzeMethodAsync(
            IMethodSymbol methodSymbol,
            BaseMethodDeclarationSyntax methodDecl,
            SemanticModel semanticModel,
            CallGraph callGraph)
        {
            // Ensure the method node exists in the graph
            callGraph.GetOrAddNode(methodSymbol);

            // Get method body
            SyntaxNode? body = methodDecl.Body;
            if (body == null && methodDecl is MethodDeclarationSyntax methodDeclSyntax && methodDeclSyntax.ExpressionBody != null)
            {
                body = methodDeclSyntax.ExpressionBody;
            }

            if (body == null)
                return;

            // Find all invocations in the method body
            var invocations = body.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var symbolInfo = semanticModel.GetSymbolInfo(invocation, _cancellationToken);
                if (symbolInfo.Symbol is IMethodSymbol calleeSymbol)
                {
                    // Add edge from caller to callee
                    callGraph.AddEdge(methodSymbol, calleeSymbol, invocation.GetLocation());
                }
            }

            // Also check for object creation expressions (constructor calls)
            var objectCreations = body.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();

            foreach (var creation in objectCreations)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var symbolInfo = semanticModel.GetSymbolInfo(creation, _cancellationToken);
                if (symbolInfo.Symbol is IMethodSymbol constructorSymbol)
                {
                    callGraph.AddEdge(methodSymbol, constructorSymbol, creation.GetLocation());
                }
            }
        }

        private async Task AnalyzeLocalFunctionAsync(
            IMethodSymbol localSymbol,
            LocalFunctionStatementSyntax localFunc,
            SemanticModel semanticModel,
            CallGraph callGraph)
        {
            // Ensure the local function node exists in the graph
            callGraph.GetOrAddNode(localSymbol);

            // Get function body
            SyntaxNode? body = localFunc.Body ?? (SyntaxNode?)localFunc.ExpressionBody?.Expression;
            if (body == null)
                return;

            // Find all invocations in the function body
            var invocations = body.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var symbolInfo = semanticModel.GetSymbolInfo(invocation, _cancellationToken);
                if (symbolInfo.Symbol is IMethodSymbol calleeSymbol)
                {
                    callGraph.AddEdge(localSymbol, calleeSymbol, invocation.GetLocation());
                }
            }

            // Also check for object creation expressions
            var objectCreations = body.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();

            foreach (var creation in objectCreations)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var symbolInfo = semanticModel.GetSymbolInfo(creation, _cancellationToken);
                if (symbolInfo.Symbol is IMethodSymbol constructorSymbol)
                {
                    callGraph.AddEdge(localSymbol, constructorSymbol, creation.GetLocation());
                }
            }
        }

        /// <summary>
        /// Gets all methods transitively called by the specified method.
        /// </summary>
        /// <param name="callGraph">The call graph to analyze.</param>
        /// <param name="method">The method to find transitive callees for.</param>
        /// <param name="maxDepth">Maximum depth to traverse (default: 10). Prevents infinite recursion.</param>
        /// <returns>
        /// An enumerable of all methods transitively called by the specified method,
        /// up to the maximum depth.
        /// </returns>
        /// <remarks>
        /// This method performs a depth-first search to find all methods reachable from the specified method.
        /// Cycles are automatically detected and handled. The maxDepth parameter prevents excessive
        /// traversal in deeply nested call chains.
        ///
        /// Time complexity: O(V + E) where V is reachable vertices and E is edges.
        /// </remarks>
        public static IEnumerable<IMethodSymbol> GetTransitiveCallees(
            CallGraph callGraph,
            IMethodSymbol method,
            int maxDepth = 10)
        {
            if (!callGraph.TryGetNode(method, out var node))
                return Enumerable.Empty<IMethodSymbol>();

            var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            var result = new List<IMethodSymbol>();

            GetTransitiveCalleesRecursive(node, visited, result, 0, maxDepth);

            return result;
        }

        private static void GetTransitiveCalleesRecursive(
            CallGraphNode node,
            HashSet<IMethodSymbol> visited,
            List<IMethodSymbol> result,
            int currentDepth,
            int maxDepth)
        {
            if (currentDepth >= maxDepth)
                return;

            if (visited.Contains(node.Method))
                return; // Cycle detected

            visited.Add(node.Method);

            foreach (var edge in node.Callees)
            {
                result.Add(edge.Target.Method);
                GetTransitiveCalleesRecursive(edge.Target, visited, result, currentDepth + 1, maxDepth);
            }
        }

        /// <summary>
        /// Gets all methods that transitively call the specified method.
        /// </summary>
        /// <param name="callGraph">The call graph to analyze.</param>
        /// <param name="method">The method to find transitive callers for.</param>
        /// <param name="maxDepth">Maximum depth to traverse (default: 10). Prevents infinite recursion.</param>
        /// <returns>
        /// An enumerable of all methods that transitively call the specified method,
        /// up to the maximum depth.
        /// </returns>
        /// <remarks>
        /// This method performs a depth-first search to find all methods that can reach the specified method.
        /// Cycles are automatically detected and handled. The maxDepth parameter prevents excessive
        /// traversal in deeply nested call chains.
        ///
        /// Time complexity: O(V + E) where V is reachable vertices and E is edges.
        /// </remarks>
        public static IEnumerable<IMethodSymbol> GetTransitiveCallers(
            CallGraph callGraph,
            IMethodSymbol method,
            int maxDepth = 10)
        {
            if (!callGraph.TryGetNode(method, out var node))
                return Enumerable.Empty<IMethodSymbol>();

            var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            var result = new List<IMethodSymbol>();

            GetTransitiveCallersRecursive(node, visited, result, 0, maxDepth);

            return result;
        }

        private static void GetTransitiveCallersRecursive(
            CallGraphNode node,
            HashSet<IMethodSymbol> visited,
            List<IMethodSymbol> result,
            int currentDepth,
            int maxDepth)
        {
            if (currentDepth >= maxDepth)
                return;

            if (visited.Contains(node.Method))
                return; // Cycle detected

            visited.Add(node.Method);

            foreach (var edge in node.Callers)
            {
                result.Add(edge.Target.Method);
                GetTransitiveCallersRecursive(edge.Target, visited, result, currentDepth + 1, maxDepth);
            }
        }
    }
}
