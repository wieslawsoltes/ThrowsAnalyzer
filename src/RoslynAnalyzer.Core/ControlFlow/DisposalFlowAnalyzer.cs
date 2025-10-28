using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace RoslynAnalyzer.Core.ControlFlow
{
    /// <summary>
    /// Specialized control flow analyzer for tracking IDisposable disposal patterns.
    /// Handles edge cases, loops, and interprocedural analysis.
    /// </summary>
    public class DisposalFlowAnalyzer
    {
        private readonly ControlFlowAnalyzer _cfAnalyzer;
        private readonly Func<IOperation, bool> _isDisposalCall;

        public DisposalFlowAnalyzer(Func<IOperation, bool> isDisposalCall)
        {
            _cfAnalyzer = new ControlFlowAnalyzer();
            _isDisposalCall = isDisposalCall ?? throw new ArgumentNullException(nameof(isDisposalCall));
        }

        /// <summary>
        /// Analyzes if a local variable is disposed on all execution paths.
        /// Handles edge cases like fall-through, loops, and early returns.
        /// </summary>
        public DisposalAnalysisResult AnalyzeDisposal(
            IOperation methodOperation,
            ILocalSymbol local,
            SemanticModel? semanticModel = null)
        {
            var result = new DisposalAnalysisResult { Variable = local };

            // Check for using statement FIRST (always safe, check before CFG which may not handle it well)
            if (IsInUsingStatement(local, methodOperation))
            {
                result.AnalysisSucceeded = true;
                result.IsDisposedOnAllPaths = true;
                result.Reason = "Managed by using statement";
                result.DisposalPattern = DisposalPattern.UsingStatement;
                return result;
            }

            // Get control flow graph
            var cfg = _cfAnalyzer.GetControlFlowGraph(methodOperation);
            if (cfg == null)
            {
                result.AnalysisSucceeded = false;
                result.Reason = "Could not create control flow graph";
                return result;
            }

            result.AnalysisSucceeded = true;

            // Find all disposal operations for this variable
            var disposalOps = FindDisposalOperations(cfg, local);
            result.DisposalLocations.AddRange(disposalOps);

            // Check for ownership transfer (return) - this is valid even without disposal
            if (CheckOwnershipTransfer(cfg, local))
            {
                result.IsDisposedOnAllPaths = true;
                result.Reason = "Ownership transferred (returned from method)";
                result.DisposalPattern = DisposalPattern.OwnershipTransfer;
                return result;
            }

            if (disposalOps.Count == 0)
            {
                result.IsDisposedOnAllPaths = false;
                result.Reason = "No disposal operations found";
                return result;
            }

            // Check for finally block disposal (always safe)
            if (IsDisposedInFinally(cfg, local, disposalOps))
            {
                result.IsDisposedOnAllPaths = true;
                result.Reason = "Disposed in finally block";
                result.DisposalPattern = DisposalPattern.Finally;
                return result;
            }

            // Perform path-sensitive analysis
            var pathAnalysis = AnalyzeAllPaths(cfg, local, disposalOps);
            result.IsDisposedOnAllPaths = pathAnalysis.AllPathsDispose;
            result.ProblematicPaths.AddRange(pathAnalysis.PathsWithoutDisposal);
            result.TotalPaths = pathAnalysis.TotalPaths;

            if (pathAnalysis.AllPathsDispose)
            {
                result.Reason = $"Disposed on all {pathAnalysis.TotalPaths} execution paths";
                result.DisposalPattern = DisposalPattern.ExplicitAllPaths;
            }
            else
            {
                result.Reason = $"{pathAnalysis.PathsWithoutDisposal.Count} of {pathAnalysis.TotalPaths} paths lack disposal";
                result.DisposalPattern = DisposalPattern.Incomplete;
            }

            // Analyze loops
            var loopAnalysis = _cfAnalyzer.AnalyzeLoopDisposal(cfg, local);
            if (loopAnalysis.HasLoops)
            {
                result.HasLoops = true;
                result.LoopAnalysis = loopAnalysis;

                // Special handling: disposal in loop may not be safe
                if (loopAnalysis.DisposedInLoop && !loopAnalysis.CreatedInLoop)
                {
                    result.IsDisposedOnAllPaths = false;
                    result.Reason = "Variable disposed inside loop but created outside";
                }
            }

            // Interprocedural analysis
            if (semanticModel != null)
            {
                var interprocedural = _cfAnalyzer.AnalyzeMethodCalls(cfg, local, semanticModel);
                result.InterproceduralAnalysis = interprocedural;

                if (interprocedural.PotentialDisposalMethods.Count > 0)
                {
                    result.MayBeDisposedInMethod = true;
                    result.Reason += " (possibly disposed in called method)";
                }
            }

            return result;
        }

        private List<IOperation> FindDisposalOperations(ControlFlowGraph cfg, ILocalSymbol local)
        {
            var operations = new List<IOperation>();

            foreach (var block in cfg.Blocks)
            {
                foreach (var operation in block.Operations)
                {
                    if (_isDisposalCall(operation) && ReferencesLocal(operation, local))
                    {
                        operations.Add(operation);
                    }

                    // Check descendants for disposal calls
                    foreach (var descendant in operation.Descendants())
                    {
                        if (_isDisposalCall(descendant) && ReferencesLocal(descendant, local))
                        {
                            operations.Add(descendant);
                        }
                    }
                }
            }

            return operations;
        }

        private bool IsDisposedInFinally(ControlFlowGraph cfg, ILocalSymbol local, List<IOperation> disposalOps)
        {
            foreach (var disposalOp in disposalOps)
            {
                // Find which block contains this disposal
                foreach (var block in cfg.Blocks)
                {
                    if (BlockContainsOperation(block, disposalOp))
                    {
                        // Check if block is in a finally region
                        if (block.EnclosingRegion != null)
                        {
                            var region = block.EnclosingRegion;
                            while (region != null)
                            {
                                if (region.Kind == ControlFlowRegionKind.Finally)
                                    return true;
                                region = region.EnclosingRegion;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private bool IsInUsingStatement(ILocalSymbol local, IOperation methodOperation)
        {
            // Check syntax tree - using statements may not show up clearly in operations
            var declaringSyntax = local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (declaringSyntax != null)
            {
                var current = declaringSyntax.Parent;
                while (current != null)
                {
                    // Check for using statement: using (var x = ...)
                    if (current.IsKind(SyntaxKind.UsingStatement))
                        return true;

                    // Check for using declaration: using var x = ...
                    if (current is LocalDeclarationStatementSyntax localDecl)
                    {
                        if (localDecl.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
                            return true;
                    }

                    current = current.Parent;
                }
            }

            // Also check operation tree (for completeness)
            foreach (var operation in methodOperation.Descendants())
            {
                if (operation is IUsingOperation usingOp)
                {
                    if (usingOp.Resources is IVariableDeclarationGroupOperation declGroup)
                    {
                        foreach (var decl in declGroup.Declarations)
                        {
                            foreach (var declarator in decl.Declarators)
                            {
                                if (SymbolEqualityComparer.Default.Equals(declarator.Symbol, local))
                                    return true;
                            }
                        }
                    }
                    else if (usingOp.Resources is ILocalReferenceOperation localRef)
                    {
                        if (SymbolEqualityComparer.Default.Equals(localRef.Local, local))
                            return true;
                    }
                }
            }

            return false;
        }

        private PathAnalysisResult AnalyzeAllPaths(
            ControlFlowGraph cfg,
            ILocalSymbol local,
            List<IOperation> disposalOps)
        {
            var result = new PathAnalysisResult();

            // Find all execution paths
            var paths = _cfAnalyzer.FindAllPaths(cfg);
            result.TotalPaths = paths.Count;

            // Map disposal operations to their blocks
            var disposalBlocks = new HashSet<BasicBlock>();
            foreach (var block in cfg.Blocks)
            {
                if (disposalOps.Any(op => BlockContainsOperation(block, op)))
                {
                    disposalBlocks.Add(block);
                }
            }

            // Check each path
            foreach (var path in paths)
            {
                bool pathHasDisposal = path.Blocks.Any(block => disposalBlocks.Contains(block));
                bool pathReturnsVariable = path.Blocks.Any(block => BlockReturnsLocal(block, local));

                if (!pathHasDisposal && !pathReturnsVariable)
                {
                    result.PathsWithoutDisposal.Add(path);
                }
            }

            result.AllPathsDispose = result.PathsWithoutDisposal.Count == 0;
            return result;
        }

        private bool CheckOwnershipTransfer(ControlFlowGraph cfg, ILocalSymbol local)
        {
            // Check if ALL paths return the variable
            var paths = _cfAnalyzer.FindAllPaths(cfg);
            if (paths.Count == 0)
                return false;

            foreach (var path in paths)
            {
                bool pathReturnsVariable = false;

                foreach (var block in path.Blocks)
                {
                    if (BlockReturnsLocal(block, local))
                    {
                        pathReturnsVariable = true;
                        break;
                    }
                }

                if (!pathReturnsVariable)
                    return false; // At least one path doesn't return
            }

            return true; // All paths return the variable
        }

        private bool ReferencesLocal(IOperation operation, ILocalSymbol local)
        {
            // Direct reference
            if (operation is IInvocationOperation invocation &&
                invocation.Instance is ILocalReferenceOperation localRef &&
                SymbolEqualityComparer.Default.Equals(localRef.Local, local))
            {
                return true;
            }

            // Conditional access (stream?.Dispose())
            if (operation is IConditionalAccessOperation conditionalAccess &&
                conditionalAccess.Operation is ILocalReferenceOperation conditionalLocalRef &&
                SymbolEqualityComparer.Default.Equals(conditionalLocalRef.Local, local))
            {
                return true;
            }

            return false;
        }

        private bool BlockContainsOperation(BasicBlock block, IOperation operation)
        {
            foreach (var blockOp in block.Operations)
            {
                if (blockOp == operation)
                    return true;

                if (blockOp.Descendants().Contains(operation))
                    return true;
            }

            return false;
        }

        private bool BlockReturnsLocal(BasicBlock block, ILocalSymbol local)
        {
            foreach (var operation in block.Operations)
            {
                if (operation is IReturnOperation returnOp &&
                    returnOp.ReturnedValue is ILocalReferenceOperation localRef &&
                    SymbolEqualityComparer.Default.Equals(localRef.Local, local))
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearCache()
        {
            _cfAnalyzer.ClearCache();
        }
    }

    /// <summary>
    /// Result of disposal flow analysis.
    /// </summary>
    public class DisposalAnalysisResult
    {
        public ILocalSymbol? Variable { get; set; }
        public bool AnalysisSucceeded { get; set; }
        public bool IsDisposedOnAllPaths { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DisposalPattern DisposalPattern { get; set; }
        public List<IOperation> DisposalLocations { get; } = new List<IOperation>();
        public int TotalPaths { get; set; }
        public List<ExecutionPath> ProblematicPaths { get; } = new List<ExecutionPath>();
        public bool HasLoops { get; set; }
        public LoopDisposalAnalysis? LoopAnalysis { get; set; }
        public bool MayBeDisposedInMethod { get; set; }
        public InterproceduralDisposalAnalysis? InterproceduralAnalysis { get; set; }
    }

    /// <summary>
    /// Pattern of disposal detected.
    /// </summary>
    public enum DisposalPattern
    {
        None,
        UsingStatement,
        Finally,
        ExplicitAllPaths,
        OwnershipTransfer,
        Incomplete
    }

    /// <summary>
    /// Result of analyzing all execution paths.
    /// </summary>
    public class PathAnalysisResult
    {
        public int TotalPaths { get; set; }
        public bool AllPathsDispose { get; set; }
        public List<ExecutionPath> PathsWithoutDisposal { get; } = new List<ExecutionPath>();
    }
}
