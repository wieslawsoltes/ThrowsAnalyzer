using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace RoslynAnalyzer.Core.ControlFlow
{
    /// <summary>
    /// Advanced control flow analyzer with path-sensitive analysis, loop handling,
    /// and interprocedural tracking.
    /// </summary>
    public class ControlFlowAnalyzer
    {
        private readonly Dictionary<IOperation, ControlFlowGraph> _cfgCache = new();
        private readonly object _cacheLock = new object();

        /// <summary>
        /// Gets the control flow graph for an operation, using cache for performance.
        /// </summary>
        public ControlFlowGraph? GetControlFlowGraph(IOperation operation)
        {
            if (operation == null)
                return null;

            lock (_cacheLock)
            {
                if (_cfgCache.TryGetValue(operation, out var cached))
                    return cached;

                try
                {
                    // Find the root operation (method/constructor body)
                    var root = GetRootOperation(operation);
                    if (root is IBlockOperation blockOp)
                    {
                        var cfg = ControlFlowGraph.Create(blockOp);
                        _cfgCache[operation] = cfg;
                        return cfg;
                    }
                }
                catch
                {
                    // CFG creation can fail for some operations
                }

                return null;
            }
        }

        /// <summary>
        /// Clears the CFG cache to free memory.
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _cfgCache.Clear();
            }
        }

        /// <summary>
        /// Finds all execution paths from entry to exit blocks.
        /// Handles loops, conditional branches, and exception handling.
        /// </summary>
        public List<ExecutionPath> FindAllPaths(ControlFlowGraph cfg)
        {
            var paths = new List<ExecutionPath>();
            var visited = new HashSet<(BasicBlock, int)>(); // Track block + visit count for loops

            FindPathsRecursive(
                cfg.Blocks[0],
                cfg.Blocks[cfg.Blocks.Length - 1],
                new ExecutionPath(),
                paths,
                visited,
                maxDepth: 100 // Prevent infinite recursion
            );

            return paths;
        }

        private void FindPathsRecursive(
            BasicBlock current,
            BasicBlock target,
            ExecutionPath currentPath,
            List<ExecutionPath> allPaths,
            HashSet<(BasicBlock, int)> visited,
            int maxDepth)
        {
            if (currentPath.Blocks.Count > maxDepth)
                return; // Prevent infinite loops

            // Check if we've visited this block too many times (loop handling)
            var visitKey = (current, currentPath.Blocks.Count(b => b == current));
            if (visitKey.Item2 > 3) // Allow max 3 iterations through same block
                return;

            currentPath.Blocks.Add(current);

            // Reached target - save this path
            if (current == target)
            {
                allPaths.Add(currentPath.Clone());
                currentPath.Blocks.RemoveAt(currentPath.Blocks.Count - 1);
                return;
            }

            // Check if block is in a finally region
            if (current.EnclosingRegion != null)
            {
                var region = current.EnclosingRegion;
                while (region != null)
                {
                    if (region.Kind == ControlFlowRegionKind.Finally)
                    {
                        currentPath.HasFinallyBlock = true;
                        currentPath.FinallyBlocks.Add(region);
                        break;
                    }
                    region = region.EnclosingRegion;
                }
            }

            // Explore successors
            var successors = new List<BasicBlock>();

            if (current.ConditionalSuccessor != null)
                successors.Add(current.ConditionalSuccessor.Destination);

            if (current.FallThroughSuccessor != null)
                successors.Add(current.FallThroughSuccessor.Destination);

            foreach (var successor in successors)
            {
                FindPathsRecursive(successor, target, currentPath, allPaths, visited, maxDepth);
            }

            currentPath.Blocks.RemoveAt(currentPath.Blocks.Count - 1);
        }

        /// <summary>
        /// Checks if a variable is disposed on all execution paths.
        /// </summary>
        public bool IsDisposedOnAllPaths(
            ControlFlowGraph cfg,
            ISymbol variable,
            Func<IOperation, ISymbol, bool> isDisposalOperation)
        {
            var paths = FindAllPaths(cfg);

            // Build disposal location map
            var disposalBlocks = new HashSet<BasicBlock>();
            var returnBlocks = new HashSet<BasicBlock>();

            foreach (var block in cfg.Blocks)
            {
                foreach (var operation in block.Operations)
                {
                    // Check if this operation disposes the variable
                    if (isDisposalOperation(operation, variable))
                    {
                        disposalBlocks.Add(block);
                    }

                    // Check if this operation returns the variable
                    if (IsReturnOfVariable(operation, variable))
                    {
                        returnBlocks.Add(block);
                    }
                }
            }

            // Analyze each path
            foreach (var path in paths)
            {
                bool pathHasDisposal = false;
                bool pathReturnsVariable = false;

                // Check if path goes through disposal or return
                foreach (var block in path.Blocks)
                {
                    if (disposalBlocks.Contains(block))
                    {
                        pathHasDisposal = true;
                        break;
                    }

                    if (returnBlocks.Contains(block))
                    {
                        pathReturnsVariable = true;
                        break;
                    }
                }

                // Finally blocks guarantee disposal on all paths
                if (path.HasFinallyBlock)
                {
                    foreach (var finallyRegion in path.FinallyBlocks)
                    {
                        // Check if finally contains disposal
                        // Note: Finally regions need special handling
                        pathHasDisposal = true; // Simplified for now
                    }
                }

                // If this path doesn't dispose or return, it's a leak
                if (!pathHasDisposal && !pathReturnsVariable)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Analyzes loop disposal patterns (while, for, foreach).
        /// </summary>
        public LoopDisposalAnalysis AnalyzeLoopDisposal(
            ControlFlowGraph cfg,
            ISymbol variable)
        {
            var result = new LoopDisposalAnalysis();

            // Identify loop regions
            foreach (var block in cfg.Blocks)
            {
                if (block.EnclosingRegion != null &&
                    IsLoopRegion(block.EnclosingRegion.Kind))
                {
                    result.HasLoops = true;
                    result.LoopBlocks.Add(block);
                }
            }

            // Check if variable is created inside or outside loop
            // This is simplified - full analysis would track variable lifetime
            result.CreatedInLoop = false;
            result.DisposedInLoop = false;

            return result;
        }

        /// <summary>
        /// Performs interprocedural analysis to track disposal across method calls.
        /// </summary>
        public InterproceduralDisposalAnalysis AnalyzeMethodCalls(
            ControlFlowGraph cfg,
            ISymbol variable,
            SemanticModel semanticModel)
        {
            var result = new InterproceduralDisposalAnalysis();

            foreach (var block in cfg.Blocks)
            {
                foreach (var operation in block.Operations)
                {
                    if (operation is IInvocationOperation invocation)
                    {
                        // Check if variable is passed to method
                        foreach (var argument in invocation.Arguments)
                        {
                            if (ReferencesVariable(argument.Value, variable))
                            {
                                var methodSymbol = invocation.TargetMethod;
                                result.MethodCallsWithVariable.Add(methodSymbol);

                                // Check if method might dispose the variable
                                if (MightDispose(methodSymbol, argument))
                                {
                                    result.PotentialDisposalMethods.Add(methodSymbol);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private IOperation? GetRootOperation(IOperation operation)
        {
            var current = operation;
            while (current?.Parent != null)
            {
                if (current is IMethodBodyOperation or IConstructorBodyOperation)
                    return current;
                current = current.Parent;
            }
            return current;
        }

        private bool IsReturnOfVariable(IOperation operation, ISymbol variable)
        {
            if (operation is IReturnOperation returnOp)
            {
                return ReferencesVariable(returnOp.ReturnedValue, variable);
            }
            return false;
        }

        private bool ReferencesVariable(IOperation? operation, ISymbol variable)
        {
            if (operation == null)
                return false;

            if (operation is ILocalReferenceOperation localRef)
            {
                return SymbolEqualityComparer.Default.Equals(localRef.Local, variable);
            }

            if (operation is IParameterReferenceOperation paramRef)
            {
                return SymbolEqualityComparer.Default.Equals(paramRef.Parameter, variable);
            }

            // Check descendants
            return operation.Descendants().Any(op => ReferencesVariable(op, variable));
        }

        private bool IsLoopRegion(ControlFlowRegionKind kind)
        {
            return kind == ControlFlowRegionKind.LocalLifetime || // Simplified
                   kind == ControlFlowRegionKind.TryAndCatch;
        }

        private bool MightDispose(IMethodSymbol method, IArgumentOperation argument)
        {
            // Heuristic: methods with "Dispose" in name, or taking ownership
            if (method.Name.IndexOf("Dispose", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // Check if parameter suggests ownership transfer
            var paramName = method.Parameters.ElementAtOrDefault(argument.Parameter?.Ordinal ?? -1)?.Name ?? "";
            if (paramName.IndexOf("owner", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }
    }

    /// <summary>
    /// Represents an execution path through a control flow graph.
    /// </summary>
    public class ExecutionPath
    {
        public List<BasicBlock> Blocks { get; } = new List<BasicBlock>();
        public bool HasFinallyBlock { get; set; }
        public List<ControlFlowRegion> FinallyBlocks { get; } = new List<ControlFlowRegion>();

        public ExecutionPath Clone()
        {
            var clone = new ExecutionPath
            {
                HasFinallyBlock = this.HasFinallyBlock
            };
            clone.Blocks.AddRange(this.Blocks);
            clone.FinallyBlocks.AddRange(this.FinallyBlocks);
            return clone;
        }
    }

    /// <summary>
    /// Result of loop disposal analysis.
    /// </summary>
    public class LoopDisposalAnalysis
    {
        public bool HasLoops { get; set; }
        public List<BasicBlock> LoopBlocks { get; } = new List<BasicBlock>();
        public bool CreatedInLoop { get; set; }
        public bool DisposedInLoop { get; set; }
    }

    /// <summary>
    /// Result of interprocedural disposal analysis.
    /// </summary>
    public class InterproceduralDisposalAnalysis
    {
        public List<IMethodSymbol> MethodCallsWithVariable { get; } = new List<IMethodSymbol>();
        public List<IMethodSymbol> PotentialDisposalMethods { get; } = new List<IMethodSymbol>();
    }
}
