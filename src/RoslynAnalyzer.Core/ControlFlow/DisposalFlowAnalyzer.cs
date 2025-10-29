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

            if (semanticModel != null && IsDisposedInFinallySyntax(methodOperation.Syntax, semanticModel, local))
            {
                result.AnalysisSucceeded = true;
                result.IsDisposedOnAllPaths = true;
                result.Reason = "Disposed in finally block";
                result.DisposalPattern = DisposalPattern.Finally;
                return result;
            }

            // Get control flow graph
            var cfg = _cfAnalyzer.GetControlFlowGraph(methodOperation);

            // Check for ownership transfer (return) - this is valid even without disposal
            if (CheckOwnershipTransfer(cfg, local, methodOperation, semanticModel))
            {
                result.IsDisposedOnAllPaths = true;
                result.Reason = "Ownership transferred (returned from method)";
                result.DisposalPattern = DisposalPattern.OwnershipTransfer;
                return result;
            }

            if (cfg == null)
            {
                if (semanticModel != null && IsDisposedInAllPathsBySyntax(methodOperation, local, semanticModel))
                {
                    result.AnalysisSucceeded = true;
                    result.IsDisposedOnAllPaths = true;
                    result.Reason = "Disposed on all execution paths (syntax analysis)";
                    result.DisposalPattern = DisposalPattern.ExplicitAllPaths;
                    return result;
                }

                result.AnalysisSucceeded = false;
                result.Reason = "Could not create control flow graph";
                return result;
            }

            result.AnalysisSucceeded = true;

            // Find all disposal operations for this variable
            var disposalOps = FindDisposalOperations(cfg, local);
            result.DisposalLocations.AddRange(disposalOps);

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

        private bool IsDisposedInFinallySyntax(SyntaxNode methodSyntax, SemanticModel semanticModel, ILocalSymbol local)
        {
            foreach (var finallyClause in methodSyntax.DescendantNodes().OfType<FinallyClauseSyntax>())
            {
                if (finallyClause.Block == null)
                    continue;

                foreach (var statement in finallyClause.Block.Statements)
                {
                    var statementOperation = semanticModel.GetOperation(statement);
                    if (statementOperation != null && ContainsDisposalOperation(statementOperation, local))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsDisposedInAllPathsBySyntax(IOperation methodOperation, ILocalSymbol local, SemanticModel semanticModel)
        {
            if (methodOperation.Syntax is not BlockSyntax blockSyntax)
            {
                return false;
            }

            var controlFlow = semanticModel.AnalyzeControlFlow(blockSyntax);
            if (!controlFlow.Succeeded)
            {
                return false;
            }

            foreach (var exit in controlFlow.ReturnStatements)
            {
                if (exit is ReturnStatementSyntax returnStatement)
                {
                    if (!IsStatementPrecededByDisposal(returnStatement, local, semanticModel))
                    {
                        return false;
                    }
                }
            }

            if (controlFlow.EndPointIsReachable)
            {
                if (!ContainsDisposeBeforeEndpoint(blockSyntax, local, semanticModel))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsStatementPrecededByDisposal(StatementSyntax statement, ILocalSymbol local, SemanticModel semanticModel)
        {
            if (statement.Parent is not BlockSyntax block)
            {
                return false;
            }

            var index = block.Statements.IndexOf(statement);
            for (int i = index - 1; i >= 0; i--)
            {
                var candidate = block.Statements[i];
                if (StatementDisposesLocal(candidate, local, semanticModel))
                {
                    return true;
                }

                if (candidate is ReturnStatementSyntax or ThrowStatementSyntax)
                {
                    // A prior exit without disposal means this branch is unsafe.
                    return false;
                }
            }

            return false;
        }

        private bool ContainsDisposeBeforeEndpoint(BlockSyntax blockSyntax, ILocalSymbol local, SemanticModel semanticModel)
        {
            for (int i = blockSyntax.Statements.Count - 1; i >= 0; i--)
            {
                var statement = blockSyntax.Statements[i];

                if (statement is ReturnStatementSyntax or ThrowStatementSyntax)
                {
                    continue;
                }

                if (statement is IfStatementSyntax or SwitchStatementSyntax or ForEachStatementSyntax or ForStatementSyntax or WhileStatementSyntax or DoStatementSyntax)
                {
                    return false;
                }

                if (StatementDisposesLocal(statement, local, semanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        private bool StatementDisposesLocal(StatementSyntax statement, ILocalSymbol local, SemanticModel semanticModel)
        {
            var operation = semanticModel.GetOperation(statement);
            if (operation == null)
            {
                return false;
            }

            return ContainsDisposalOperation(operation, local);
        }

        private bool ContainsDisposalOperation(IOperation operation, ILocalSymbol local)
        {
            if (_isDisposalCall(operation) && ReferencesLocal(operation, local))
            {
                return true;
            }

            foreach (var descendant in operation.Descendants())
            {
                if (_isDisposalCall(descendant) && ReferencesLocal(descendant, local))
                {
                    return true;
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

            var allPathsDispose = AreAllPathsDisposed(cfg, local, disposalBlocks);

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

            result.AllPathsDispose = allPathsDispose;
            return result;
        }

        private bool AreAllPathsDisposed(ControlFlowGraph cfg, ILocalSymbol local, HashSet<BasicBlock> disposalBlocks)
        {
            var worklist = new Queue<(BasicBlock Block, bool Disposed)>();
            var visited = new Dictionary<BasicBlock, HashSet<bool>>();

            var entry = cfg.Blocks[0];
            worklist.Enqueue((entry, false));
            visited[entry] = new HashSet<bool> { false };

            while (worklist.Count > 0)
            {
                var (block, disposed) = worklist.Dequeue();
                var disposedAfterBlock = disposed || disposalBlocks.Contains(block);

                foreach (var operation in block.Operations)
                {
                    if (operation is IReturnOperation returnOperation)
                    {
                        if (!ReturnsLocalToCaller(returnOperation.ReturnedValue, local) && !disposedAfterBlock)
                        {
                            return false;
                        }
                    }
                    else if (operation is IThrowOperation && !disposedAfterBlock)
                    {
                        // Treat exceptions like early exits that require disposal beforehand.
                        return false;
                    }
                }

                if (block.Kind == BasicBlockKind.Exit)
                {
                    if (!disposedAfterBlock)
                    {
                        return false;
                    }
                    continue;
                }

                var successors = new List<BasicBlock>();

                if (block.ConditionalSuccessor?.Destination != null)
                {
                    successors.Add(block.ConditionalSuccessor.Destination);
                }

                if (block.FallThroughSuccessor?.Destination != null)
                {
                    successors.Add(block.FallThroughSuccessor.Destination);
                }

                if (successors.Count == 0)
                {
                    if (!disposedAfterBlock)
                    {
                        return false;
                    }
                    continue;
                }

                foreach (var successor in successors)
                {
                    if (!visited.TryGetValue(successor, out var states))
                    {
                        states = new HashSet<bool>();
                        visited[successor] = states;
                    }

                    if (!states.Contains(disposedAfterBlock))
                    {
                        states.Add(disposedAfterBlock);
                        worklist.Enqueue((successor, disposedAfterBlock));
                    }
                }
            }

            return true;
        }

        private bool CheckOwnershipTransfer(ControlFlowGraph? cfg, ILocalSymbol local, IOperation methodOperation, SemanticModel? semanticModel)
        {
            if (semanticModel != null)
            {
                var returnStatements = GetExplicitReturnStatements(methodOperation.Syntax);
                if (returnStatements.Count > 0)
                {
                    foreach (var returnStatement in returnStatements)
                    {
                        if (returnStatement.Expression is null)
                        {
                            return false;
                        }

                        var returnOperation = semanticModel.GetOperation(returnStatement.Expression);
                        if (!ReturnsLocalToCaller(returnOperation, local))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return cfg != null && CheckOwnershipViaControlFlow(cfg, local);
        }

        private bool CheckOwnershipViaControlFlow(ControlFlowGraph cfg, ILocalSymbol local)
        {
            var foundReturn = false;
            foreach (var block in cfg.Blocks)
            {
                foreach (var operation in block.Operations)
                {
                    if (operation is IReturnOperation returnOperation && returnOperation.ReturnedValue is not null)
                    {
                        foundReturn = true;

                        if (!ReturnsLocalToCaller(returnOperation.ReturnedValue, local))
                        {
                            return false;
                        }
                    }
                }
            }

            return foundReturn;
        }

        private static IReadOnlyList<ReturnStatementSyntax> GetExplicitReturnStatements(SyntaxNode methodSyntax)
        {
            var statements = new List<ReturnStatementSyntax>();

            foreach (var returnStatement in methodSyntax.DescendantNodes().OfType<ReturnStatementSyntax>())
            {
                if (returnStatement.Expression is null)
                {
                    continue;
                }

                if (returnStatement.Ancestors().Any(static ancestor =>
                        ancestor is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax))
                {
                    continue;
                }

                statements.Add(returnStatement);
            }

            return statements;
        }

        private bool ReturnsLocalToCaller(IOperation? returnedValue, ILocalSymbol local)
        {
            if (returnedValue is null)
            {
                return false;
            }

            returnedValue = UnwrapTrivialOperations(returnedValue);
            if (returnedValue is null)
            {
                return false;
            }

            switch (returnedValue)
            {
                case ILocalReferenceOperation localReference:
                    return SymbolEqualityComparer.Default.Equals(localReference.Local, local);

                case IReturnOperation nestedReturn:
                    return ReturnsLocalToCaller(nestedReturn.ReturnedValue, local);

                case IConditionalOperation conditional:
                    return ReturnsLocalOrNonReturning(conditional.WhenTrue, local) &&
                           ReturnsLocalOrNonReturning(conditional.WhenFalse, local);

                case ICoalesceOperation coalesce:
                    return ReturnsLocalToCaller(coalesce.Value, local) &&
                           ReturnsLocalOrNonReturning(coalesce.WhenNull, local);

                case ISwitchExpressionOperation switchExpression:
                    foreach (var arm in switchExpression.Arms)
                    {
                        if (arm.Value is null)
                        {
                            return false;
                        }

                        if (!ReturnsLocalOrNonReturning(arm.Value, local))
                        {
                            return false;
                        }
                    }

                    return true;

                default:
                    return false;
            }
        }

        private bool ReturnsLocalOrNonReturning(IOperation? operation, ILocalSymbol local)
        {
            if (operation is null)
            {
                return false;
            }

            operation = UnwrapTrivialOperations(operation);
            if (operation is null)
            {
                return false;
            }

            if (operation is IThrowOperation)
            {
                return true;
            }

            if (operation is IReturnOperation nestedReturn)
            {
                return ReturnsLocalToCaller(nestedReturn.ReturnedValue, local);
            }

            return ReturnsLocalToCaller(operation, local);
        }

        private static IOperation? UnwrapTrivialOperations(IOperation? operation)
        {
            var current = operation;
            while (current != null)
            {
                switch (current)
                {
                    case IConversionOperation conversion when conversion.Operand is not null:
                        current = conversion.Operand;
                        continue;
                    case IParenthesizedOperation parenthesized when parenthesized.Operand is not null:
                        current = parenthesized.Operand;
                        continue;
                    case IAwaitOperation awaitOperation when awaitOperation.Operation is not null:
                        current = awaitOperation.Operation;
                        continue;
                    default:
                        return current;
                }
            }

            return current;
        }

        private bool ReferencesLocal(IOperation operation, ILocalSymbol local)
        {
            // Direct reference
            if (operation is IInvocationOperation invocation &&
                invocation.Instance is ILocalReferenceOperation directLocalRef &&
                SymbolEqualityComparer.Default.Equals(directLocalRef.Local, local))
            {
                return true;
            }

            if (operation is IInvocationOperation conditionalInvocation &&
                conditionalInvocation.Instance is IConditionalAccessInstanceOperation &&
                conditionalInvocation.Parent is IConditionalAccessOperation conditionalParent &&
                conditionalParent.Operation is ILocalReferenceOperation conditionalTarget &&
                SymbolEqualityComparer.Default.Equals(conditionalTarget.Local, local))
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
                    returnOp.ReturnedValue is not null &&
                    ReturnsLocalToCaller(returnOp.ReturnedValue, local))
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
