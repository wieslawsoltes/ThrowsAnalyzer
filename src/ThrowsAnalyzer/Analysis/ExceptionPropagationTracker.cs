using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ThrowsAnalyzer.TypeAnalysis;

namespace ThrowsAnalyzer.Analysis
{
    /// <summary>
    /// Tracks exception propagation through method call chains.
    /// </summary>
    public class ExceptionPropagationTracker
    {
        private readonly Compilation _compilation;
        private readonly CancellationToken _cancellationToken;
        private readonly Dictionary<IMethodSymbol, ExceptionFlowInfo> _cache;

        public ExceptionPropagationTracker(Compilation compilation, CancellationToken cancellationToken = default)
        {
            _compilation = compilation;
            _cancellationToken = cancellationToken;
            _cache = new Dictionary<IMethodSymbol, ExceptionFlowInfo>(SymbolEqualityComparer.Default);
        }

        /// <summary>
        /// Analyzes exception flow for a method.
        /// </summary>
        public async Task<ExceptionFlowInfo> AnalyzeMethodAsync(IMethodSymbol method)
        {
            // Check cache first
            if (_cache.TryGetValue(method, out var cached))
                return cached;

            var flowInfo = new ExceptionFlowInfo(method);

            // Add placeholder to cache immediately to prevent infinite recursion
            // in case of circular method calls
            _cache[method] = flowInfo;

            // Find the method's syntax node
            var syntaxReferences = method.DeclaringSyntaxReferences;
            if (!syntaxReferences.Any())
            {
                return flowInfo;
            }

            var syntaxRef = syntaxReferences.First();
            var syntaxNode = await syntaxRef.GetSyntaxAsync(_cancellationToken);
            var semanticModel = _compilation.GetSemanticModel(syntaxNode.SyntaxTree);

            // 1. Find exceptions thrown directly in this method
            await AnalyzeDirectThrowsAsync(method, syntaxNode, semanticModel, flowInfo);

            // 2. Find exceptions from called methods
            await AnalyzeIndirectThrowsAsync(method, syntaxNode, semanticModel, flowInfo);

            // 3. Find exceptions caught by this method
            AnalyzeCaughtExceptions(syntaxNode, semanticModel, flowInfo);

            // 4. Calculate propagated exceptions (thrown - caught)
            CalculatePropagatedExceptions(flowInfo, semanticModel);

            return flowInfo;
        }

        private async Task AnalyzeDirectThrowsAsync(
            IMethodSymbol method,
            SyntaxNode syntaxNode,
            SemanticModel semanticModel,
            ExceptionFlowInfo flowInfo)
        {
            // Get method body
            SyntaxNode body = null;
            if (syntaxNode is BaseMethodDeclarationSyntax baseMethod)
            {
                body = baseMethod.Body;
                if (body == null && baseMethod is MethodDeclarationSyntax methodDecl)
                {
                    body = methodDecl.ExpressionBody?.Expression;
                }
            }
            else if (syntaxNode is LocalFunctionStatementSyntax localFunc)
            {
                body = localFunc.Body ?? (SyntaxNode)localFunc.ExpressionBody?.Expression;
            }

            if (body == null)
                return;

            // Find all throw statements/expressions
            var throwStatements = body.DescendantNodes().OfType<ThrowStatementSyntax>();
            var throwExpressions = body.DescendantNodes().OfType<ThrowExpressionSyntax>();

            foreach (var throwStmt in throwStatements)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // Skip bare rethrows (throw;)
                if (throwStmt.Expression == null)
                    continue;

                var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwStmt, semanticModel);
                if (exceptionType != null)
                {
                    flowInfo.ThrownExceptions.Add(new ThrownExceptionInfo
                    {
                        ExceptionType = exceptionType,
                        Location = throwStmt.GetLocation(),
                        IsDirect = true,
                        OriginMethod = method,
                        PropagationDepth = 0,
                        CallChain = new List<IMethodSymbol> { method }
                    });
                }
            }

            foreach (var throwExpr in throwExpressions)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwExpr, semanticModel);
                if (exceptionType != null)
                {
                    flowInfo.ThrownExceptions.Add(new ThrownExceptionInfo
                    {
                        ExceptionType = exceptionType,
                        Location = throwExpr.GetLocation(),
                        IsDirect = true,
                        OriginMethod = method,
                        PropagationDepth = 0,
                        CallChain = new List<IMethodSymbol> { method }
                    });
                }
            }
        }

        private async Task AnalyzeIndirectThrowsAsync(
            IMethodSymbol method,
            SyntaxNode syntaxNode,
            SemanticModel semanticModel,
            ExceptionFlowInfo flowInfo)
        {
            // Get method body
            SyntaxNode body = null;
            if (syntaxNode is BaseMethodDeclarationSyntax baseMethod)
            {
                body = baseMethod.Body;
                if (body == null && baseMethod is MethodDeclarationSyntax methodDecl)
                {
                    body = methodDecl.ExpressionBody?.Expression;
                }
            }
            else if (syntaxNode is LocalFunctionStatementSyntax localFunc)
            {
                body = localFunc.Body ?? (SyntaxNode)localFunc.ExpressionBody?.Expression;
            }

            if (body == null)
                return;

            // Find all invocations
            var invocations = body.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var symbolInfo = semanticModel.GetSymbolInfo(invocation, _cancellationToken);
                if (symbolInfo.Symbol is IMethodSymbol calleeSymbol)
                {
                    // Recursively analyze the called method
                    var calleeFlowInfo = await AnalyzeMethodAsync(calleeSymbol);

                    // Add exceptions propagated from the callee
                    foreach (var thrownEx in calleeFlowInfo.PropagatedExceptions)
                    {
                        var callChain = new List<IMethodSymbol> { method };
                        callChain.AddRange(thrownEx.CallChain);

                        flowInfo.ThrownExceptions.Add(new ThrownExceptionInfo
                        {
                            ExceptionType = thrownEx.ExceptionType,
                            Location = invocation.GetLocation(),
                            IsDirect = false,
                            OriginMethod = thrownEx.OriginMethod,
                            PropagationDepth = thrownEx.PropagationDepth + 1,
                            CallChain = callChain
                        });
                    }
                }
            }

            // Also check object creation expressions (constructor calls)
            var objectCreations = body.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();

            foreach (var creation in objectCreations)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var symbolInfo = semanticModel.GetSymbolInfo(creation, _cancellationToken);
                if (symbolInfo.Symbol is IMethodSymbol constructorSymbol)
                {
                    // Recursively analyze the constructor
                    var constructorFlowInfo = await AnalyzeMethodAsync(constructorSymbol);

                    // Add exceptions propagated from the constructor
                    foreach (var thrownEx in constructorFlowInfo.PropagatedExceptions)
                    {
                        var callChain = new List<IMethodSymbol> { method };
                        callChain.AddRange(thrownEx.CallChain);

                        flowInfo.ThrownExceptions.Add(new ThrownExceptionInfo
                        {
                            ExceptionType = thrownEx.ExceptionType,
                            Location = creation.GetLocation(),
                            IsDirect = false,
                            OriginMethod = thrownEx.OriginMethod,
                            PropagationDepth = thrownEx.PropagationDepth + 1,
                            CallChain = callChain
                        });
                    }
                }
            }
        }

        private void AnalyzeCaughtExceptions(
            SyntaxNode syntaxNode,
            SemanticModel semanticModel,
            ExceptionFlowInfo flowInfo)
        {
            // Find all try statements
            var tryStatements = syntaxNode.DescendantNodes().OfType<TryStatementSyntax>();

            foreach (var tryStmt in tryStatements)
            {
                foreach (var catchClause in tryStmt.Catches)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var exceptionType = ExceptionTypeAnalyzer.GetCaughtExceptionType(catchClause, semanticModel);
                    if (exceptionType != null)
                    {
                        flowInfo.CaughtExceptions.Add(new CaughtExceptionInfo
                        {
                            ExceptionType = exceptionType,
                            Location = catchClause.GetLocation(),
                            CatchClause = catchClause,
                            HasFilter = catchClause.Filter != null
                        });
                    }
                }
            }
        }

        private void CalculatePropagatedExceptions(
            ExceptionFlowInfo flowInfo,
            SemanticModel semanticModel)
        {
            // For each thrown exception, check if it's caught
            foreach (var thrownEx in flowInfo.ThrownExceptions)
            {
                bool isCaught = false;

                foreach (var caughtEx in flowInfo.CaughtExceptions)
                {
                    // Check if the thrown exception is assignable to the caught type
                    if (ExceptionTypeAnalyzer.IsAssignableTo(thrownEx.ExceptionType, caughtEx.ExceptionType, semanticModel.Compilation))
                    {
                        // Note: This is a simplified analysis. In reality, we'd need to check
                        // if the throw is actually within the scope of the try block.
                        // For now, we assume any caught exception type catches all throws of that type.
                        isCaught = true;
                        break;
                    }
                }

                if (!isCaught)
                {
                    flowInfo.PropagatedExceptions.Add(thrownEx);
                }
            }
        }

        /// <summary>
        /// Finds all exception propagation chains with depth >= minDepth.
        /// </summary>
        public async Task<List<ExceptionPropagationChain>> FindPropagationChainsAsync(
            IMethodSymbol method,
            int minDepth = 3)
        {
            var flowInfo = await AnalyzeMethodAsync(method);
            var chains = new List<ExceptionPropagationChain>();

            foreach (var propagatedEx in flowInfo.PropagatedExceptions)
            {
                if (propagatedEx.PropagationDepth >= minDepth)
                {
                    chains.Add(new ExceptionPropagationChain
                    {
                        ExceptionType = propagatedEx.ExceptionType,
                        PropagationPath = new List<IMethodSymbol>(propagatedEx.CallChain)
                    });
                }
            }

            return chains;
        }

        /// <summary>
        /// Gets the call chain for a thrown exception as a formatted string.
        /// </summary>
        public static string FormatCallChain(ThrownExceptionInfo exceptionInfo)
        {
            var parts = exceptionInfo.CallChain
                .Select(m => $"{m.ContainingType?.Name}.{m.Name}");
            return string.Join(" -> ", parts);
        }
    }
}
