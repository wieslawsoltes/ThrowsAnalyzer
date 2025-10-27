# Phase 5: Advanced Analysis - Detailed Implementation Plan

## Executive Summary

Phase 5 extends ThrowsAnalyzer with advanced interprocedural analysis capabilities including:
- Exception flow analysis across method calls
- Async/await exception detection
- Iterator (yield) exception analysis
- XML documentation validation

This phase requires call graph analysis and control flow tracking, making it the most complex phase of ThrowsAnalyzer development.

## Prerequisites

**Completed Phases:**
- ✅ Phase 1: Foundation - Exception Type Detection
- ✅ Phase 2: Catch Clause Analysis
- ✅ Phase 3: Configuration & Documentation
- ✅ Phase 4: Code Fixes (all 8 diagnostics)

**Required Knowledge:**
- Roslyn semantic model and symbol analysis
- Call graph construction
- Control flow analysis
- Async/await internals
- Iterator method compilation

## Phase 5.1: Exception Flow Analysis (4-5 weeks)

### Objectives

Enable tracking of exception propagation across method boundaries to detect:
- Methods calling throwing methods without handling
- Exception propagation chains
- Undocumented exceptions in public APIs

### 5.1.1: Call Graph Builder

**Purpose:** Build a call graph for analyzing exception flow between methods.

**New Component:** `CallGraphBuilder.cs`

```csharp
namespace ThrowsAnalyzer.FlowAnalysis
{
    /// <summary>
    /// Builds a call graph for exception flow analysis.
    /// Maps methods to their callees and callers.
    /// </summary>
    public class CallGraphBuilder
    {
        private readonly SemanticModel _semanticModel;
        private readonly Dictionary<IMethodSymbol, CallGraphNode> _nodes;

        public CallGraphBuilder(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
            _nodes = new Dictionary<IMethodSymbol, CallGraphNode>(
                SymbolEqualityComparer.Default);
        }

        /// <summary>
        /// Builds call graph for all methods in the semantic model.
        /// </summary>
        public CallGraph Build()
        {
            // 1. Find all method declarations
            // 2. For each method, find all invocations
            // 3. Resolve invocation targets
            // 4. Build edges between callers and callees
            // 5. Return complete call graph
        }

        /// <summary>
        /// Builds call graph starting from a specific method.
        /// </summary>
        public CallGraph BuildFrom(IMethodSymbol rootMethod, int maxDepth = 5)
        {
            // Focused call graph for specific analysis
        }
    }

    /// <summary>
    /// Represents a node in the call graph.
    /// </summary>
    public class CallGraphNode
    {
        public IMethodSymbol Method { get; init; }
        public List<CallSite> Callees { get; init; } = new();
        public List<CallSite> Callers { get; init; } = new();
        public Location Location { get; init; }
    }

    /// <summary>
    /// Represents a call site (invocation).
    /// </summary>
    public class CallSite
    {
        public IMethodSymbol Caller { get; init; }
        public IMethodSymbol Callee { get; init; }
        public InvocationExpressionSyntax Invocation { get; init; }
        public Location Location { get; init; }
    }

    /// <summary>
    /// Complete call graph with nodes and edges.
    /// </summary>
    public class CallGraph
    {
        public Dictionary<IMethodSymbol, CallGraphNode> Nodes { get; init; }

        /// <summary>
        /// Gets all methods that call the specified method.
        /// </summary>
        public IEnumerable<IMethodSymbol> GetCallers(IMethodSymbol method)
        {
            if (Nodes.TryGetValue(method, out var node))
            {
                return node.Callers.Select(c => c.Caller);
            }
            return Enumerable.Empty<IMethodSymbol>();
        }

        /// <summary>
        /// Gets all methods called by the specified method.
        /// </summary>
        public IEnumerable<IMethodSymbol> GetCallees(IMethodSymbol method)
        {
            if (Nodes.TryGetValue(method, out var node))
            {
                return node.Callees.Select(c => c.Callee);
            }
            return Enumerable.Empty<IMethodSymbol>();
        }

        /// <summary>
        /// Finds all paths from source to target.
        /// </summary>
        public IEnumerable<List<IMethodSymbol>> FindPaths(
            IMethodSymbol source,
            IMethodSymbol target,
            int maxDepth = 10)
        {
            // BFS/DFS to find call paths
        }
    }
}
```

**Implementation Steps:**

1. **Task 5.1.1.1:** Create `CallGraphBuilder` class
   - Implement constructor taking `SemanticModel`
   - Add internal `Dictionary` for tracking nodes

2. **Task 5.1.1.2:** Implement method discovery
   - Find all `MethodDeclarationSyntax` nodes in syntax tree
   - Resolve to `IMethodSymbol` using semantic model
   - Create `CallGraphNode` for each method

3. **Task 5.1.1.3:** Implement invocation detection
   - Find all `InvocationExpressionSyntax` in each method
   - Resolve invocation targets using `GetSymbolInfo`
   - Handle virtual/interface method calls
   - Handle delegate invocations

4. **Task 5.1.1.4:** Build graph edges
   - Create `CallSite` objects for each invocation
   - Link callers to callees bidirectionally
   - Handle recursive calls

5. **Task 5.1.1.5:** Implement path finding
   - BFS algorithm for shortest paths
   - DFS algorithm for all paths
   - Cycle detection for recursive calls

6. **Task 5.1.1.6:** Add unit tests
   - Test simple call chains (A → B → C)
   - Test recursive calls (A → B → A)
   - Test virtual method dispatch
   - Test delegate invocations

### 5.1.2: Exception Propagation Tracker

**Purpose:** Track how exceptions propagate through the call graph.

**New Component:** `ExceptionPropagationTracker.cs`

```csharp
namespace ThrowsAnalyzer.FlowAnalysis
{
    /// <summary>
    /// Tracks exception propagation through call graphs.
    /// </summary>
    public class ExceptionPropagationTracker
    {
        private readonly CallGraph _callGraph;
        private readonly SemanticModel _semanticModel;

        public ExceptionPropagationTracker(
            CallGraph callGraph,
            SemanticModel semanticModel)
        {
            _callGraph = callGraph;
            _semanticModel = semanticModel;
        }

        /// <summary>
        /// Analyzes which exceptions can propagate from a method.
        /// </summary>
        public ExceptionFlowInfo AnalyzeMethod(IMethodSymbol method)
        {
            var thrownExceptions = new List<ExceptionTypeInfo>();
            var caughtExceptions = new List<ExceptionTypeInfo>();
            var propagatedExceptions = new List<ExceptionTypeInfo>();

            // 1. Find exceptions thrown directly
            var directThrows = FindDirectThrows(method);
            thrownExceptions.AddRange(directThrows);

            // 2. Find exceptions from called methods
            var callees = _callGraph.GetCallees(method);
            foreach (var callee in callees)
            {
                var calleeExceptions = AnalyzeMethod(callee); // Recursive
                thrownExceptions.AddRange(calleeExceptions.PropagatedExceptions);
            }

            // 3. Find exceptions caught
            caughtExceptions = FindCaughtExceptions(method);

            // 4. Calculate propagated exceptions (thrown - caught)
            propagatedExceptions = thrownExceptions
                .Where(thrown => !IsCaught(thrown, caughtExceptions))
                .ToList();

            return new ExceptionFlowInfo
            {
                Method = method,
                ThrownExceptions = thrownExceptions,
                CaughtExceptions = caughtExceptions,
                PropagatedExceptions = propagatedExceptions
            };
        }

        /// <summary>
        /// Finds all exception types thrown directly in a method.
        /// </summary>
        private List<ExceptionTypeInfo> FindDirectThrows(IMethodSymbol method)
        {
            // Use TypedThrowDetector to find throws
            // Use ExceptionTypeAnalyzer to get exception types
        }

        /// <summary>
        /// Finds all exception types caught in a method.
        /// </summary>
        private List<ExceptionTypeInfo> FindCaughtExceptions(IMethodSymbol method)
        {
            // Use CatchClauseAnalyzer to find catches
            // Build list of caught exception types
        }

        /// <summary>
        /// Determines if an exception is caught by any catch clause.
        /// </summary>
        private bool IsCaught(
            ExceptionTypeInfo thrownException,
            List<ExceptionTypeInfo> caughtExceptions)
        {
            foreach (var caught in caughtExceptions)
            {
                if (ExceptionTypeAnalyzer.IsAssignableTo(
                    thrownException.ExceptionType,
                    caught.ExceptionType,
                    _semanticModel.Compilation))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Traces exception propagation from source to target.
        /// </summary>
        public IEnumerable<ExceptionPropagationPath> TraceException(
            ITypeSymbol exceptionType,
            IMethodSymbol sourceMethod,
            IMethodSymbol targetMethod)
        {
            var paths = _callGraph.FindPaths(sourceMethod, targetMethod);
            var exceptionPaths = new List<ExceptionPropagationPath>();

            foreach (var path in paths)
            {
                // Check if exception propagates through this path
                if (PropagatesThroughPath(exceptionType, path))
                {
                    exceptionPaths.Add(new ExceptionPropagationPath
                    {
                        ExceptionType = exceptionType,
                        Path = path,
                        Source = sourceMethod,
                        Target = targetMethod
                    });
                }
            }

            return exceptionPaths;
        }

        private bool PropagatesThroughPath(
            ITypeSymbol exceptionType,
            List<IMethodSymbol> path)
        {
            // For each method in path, check if exception is caught
            for (int i = 0; i < path.Count - 1; i++)
            {
                var method = path[i];
                var flowInfo = AnalyzeMethod(method);

                // If exception is caught in this method, it doesn't propagate
                if (flowInfo.CaughtExceptions.Any(c =>
                    ExceptionTypeAnalyzer.IsAssignableTo(
                        exceptionType, c.ExceptionType,
                        _semanticModel.Compilation)))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Information about exception flow in a method.
    /// </summary>
    public class ExceptionFlowInfo
    {
        public IMethodSymbol Method { get; init; }
        public List<ExceptionTypeInfo> ThrownExceptions { get; init; }
        public List<ExceptionTypeInfo> CaughtExceptions { get; init; }
        public List<ExceptionTypeInfo> PropagatedExceptions { get; init; }
    }

    /// <summary>
    /// Information about an exception type in flow analysis.
    /// </summary>
    public class ExceptionTypeInfo
    {
        public ITypeSymbol ExceptionType { get; init; }
        public Location ThrowLocation { get; init; }
        public SyntaxNode ThrowNode { get; init; }
    }

    /// <summary>
    /// Represents a path through which an exception propagates.
    /// </summary>
    public class ExceptionPropagationPath
    {
        public ITypeSymbol ExceptionType { get; init; }
        public List<IMethodSymbol> Path { get; init; }
        public IMethodSymbol Source { get; init; }
        public IMethodSymbol Target { get; init; }

        public int Depth => Path.Count;

        public string GetPathString()
        {
            return string.Join(" → ", Path.Select(m => m.Name));
        }
    }
}
```

**Implementation Steps:**

1. **Task 5.1.2.1:** Create `ExceptionPropagationTracker` class
   - Implement constructor taking call graph and semantic model
   - Add caching for analyzed methods (avoid re-analysis)

2. **Task 5.1.2.2:** Implement `FindDirectThrows`
   - Use existing `TypedThrowDetector`
   - Extract exception type information
   - Map to `ExceptionTypeInfo`

3. **Task 5.1.2.3:** Implement `FindCaughtExceptions`
   - Use existing `CatchClauseAnalyzer`
   - Handle general catches (`catch {}`)
   - Handle filtered catches (`when` clauses)

4. **Task 5.1.2.4:** Implement `AnalyzeMethod`
   - Combine direct throws with callee throws
   - Calculate propagated exceptions
   - Handle recursive call scenarios

5. **Task 5.1.2.5:** Implement `TraceException`
   - Find paths in call graph
   - Check propagation through each path
   - Return complete propagation paths

6. **Task 5.1.2.6:** Add unit tests
   - Test direct throw propagation
   - Test propagation through call chain
   - Test exception being caught mid-chain
   - Test multiple exception types

### 5.1.3: New Diagnostics for Exception Flow

**THROWS017: Method Calls Throwing Method Without Handling**

**Purpose:** Detect when a method calls another method that throws, but doesn't handle the exception.

```csharp
namespace ThrowsAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExceptionFlowAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId017 = "THROWS017";

        private static readonly LocalizableString Title017 =
            "Method calls throwing method without handling";
        private static readonly LocalizableString MessageFormat017 =
            "Method '{0}' calls '{1}' which throws '{2}' without handling it";
        private static readonly LocalizableString Description017 =
            "Detects methods that call throwing methods without handling the exceptions.";

        public static readonly DiagnosticDescriptor CallsThrowingMethod =
            new DiagnosticDescriptor(
                DiagnosticId017,
                Title017,
                MessageFormat017,
                "Usage",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                description: Description017);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(CallsThrowingMethod);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        }

        private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var root = semanticModel.SyntaxTree.GetRoot();

            // Build call graph for this file
            var callGraphBuilder = new CallGraphBuilder(semanticModel);
            var callGraph = callGraphBuilder.Build();

            // Create propagation tracker
            var tracker = new ExceptionPropagationTracker(callGraph, semanticModel);

            // Analyze each method
            var methods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var methodSyntax in methods)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax);
                if (methodSymbol == null)
                    continue;

                AnalyzeMethodCalls(context, methodSymbol, tracker);
            }
        }

        private static void AnalyzeMethodCalls(
            SemanticModelAnalysisContext context,
            IMethodSymbol method,
            ExceptionPropagationTracker tracker)
        {
            var flowInfo = tracker.AnalyzeMethod(method);

            // Find calls to methods that throw
            var invocations = GetInvocations(method, context.SemanticModel);

            foreach (var (invocation, callee) in invocations)
            {
                var calleeFlowInfo = tracker.AnalyzeMethod(callee);

                // Check if callee propagates exceptions
                foreach (var exception in calleeFlowInfo.PropagatedExceptions)
                {
                    // Check if caller handles this exception
                    if (!IsHandledAtCallSite(invocation, exception, context.SemanticModel))
                    {
                        var diagnostic = Diagnostic.Create(
                            CallsThrowingMethod,
                            invocation.GetLocation(),
                            method.Name,
                            callee.Name,
                            exception.ExceptionType.Name);

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static IEnumerable<(InvocationExpressionSyntax, IMethodSymbol)>
            GetInvocations(IMethodSymbol method, SemanticModel semanticModel)
        {
            // Get method syntax
            var methodSyntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (methodSyntax == null)
                yield break;

            // Find all invocations
            var invocations = methodSyntax.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol callee)
                {
                    yield return (invocation, callee);
                }
            }
        }

        private static bool IsHandledAtCallSite(
            InvocationExpressionSyntax invocation,
            ExceptionTypeInfo exception,
            SemanticModel semanticModel)
        {
            // Check if invocation is inside try-catch
            var tryStatement = invocation.Ancestors()
                .OfType<TryStatementSyntax>()
                .FirstOrDefault();

            if (tryStatement == null)
                return false;

            // Check if any catch handles this exception
            var catches = CatchClauseAnalyzer.GetCatchClauses(tryStatement, semanticModel);

            return catches.Any(c =>
                c.ExceptionType != null &&
                ExceptionTypeAnalyzer.IsAssignableTo(
                    exception.ExceptionType,
                    c.ExceptionType,
                    semanticModel.Compilation));
        }
    }
}
```

**Implementation Steps:**

1. **Task 5.1.3.1:** Create `ExceptionFlowAnalyzer` diagnostic descriptor
   - Define THROWS017 with appropriate severity
   - Add localizable strings

2. **Task 5.1.3.2:** Implement `AnalyzeSemanticModel`
   - Build call graph for semantic model
   - Create propagation tracker
   - Iterate through methods

3. **Task 5.1.3.3:** Implement `AnalyzeMethodCalls`
   - Get invocations in method
   - Analyze each callee
   - Check if exceptions are handled

4. **Task 5.1.3.4:** Implement `IsHandledAtCallSite`
   - Find enclosing try-catch
   - Check catch clauses
   - Handle exception hierarchy

5. **Task 5.1.3.5:** Add unit tests
   - Test direct call to throwing method
   - Test call with try-catch handling
   - Test call with partial handling
   - Test call chain (A → B → C)

**THROWS018: Exception Propagation Across 3+ Levels**

**Purpose:** Detect deep exception propagation chains that may indicate design issues.

```csharp
public const string DiagnosticId018 = "THROWS018";

private static readonly LocalizableString Title018 =
    "Exception propagates across multiple levels";
private static readonly LocalizableString MessageFormat018 =
    "Exception '{0}' propagates through {1} method calls: {2}";
private static readonly LocalizableString Description018 =
    "Detects exceptions that propagate through 3 or more method calls.";

public static readonly DiagnosticDescriptor LongPropagationChain =
    new DiagnosticDescriptor(
        DiagnosticId018,
        Title018,
        MessageFormat018,
        "Design",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description018);
```

**Implementation Steps:**

1. **Task 5.1.3.6:** Create THROWS018 diagnostic
   - Add to diagnostic descriptors
   - Configure default severity as Info

2. **Task 5.1.3.7:** Implement propagation chain detection
   - Use `ExceptionPropagationTracker.TraceException`
   - Find paths with depth >= 3
   - Report diagnostic with path information

3. **Task 5.1.3.8:** Add configuration
   - `throws_analyzer_max_propagation_depth` (default: 3)
   - Allow users to customize threshold

4. **Task 5.1.3.9:** Add unit tests
   - Test propagation through 3 levels
   - Test propagation through 5+ levels
   - Test exception caught at level 2 (should not report)

**THROWS019: Public API Throws Undocumented Exception**

**Purpose:** Detect public methods that throw exceptions not documented in XML comments.

```csharp
public const string DiagnosticId019 = "THROWS019";

private static readonly LocalizableString Title019 =
    "Public API throws undocumented exception";
private static readonly LocalizableString MessageFormat019 =
    "Public method '{0}' throws '{1}' but doesn't document it with /// <exception>";
private static readonly LocalizableString Description019 =
    "Detects public methods that throw exceptions without XML documentation.";

public static readonly DiagnosticDescriptor UndocumentedException =
    new DiagnosticDescriptor(
        DiagnosticId019,
        Title019,
        MessageFormat019,
        "Documentation",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description019);
```

**Implementation Steps:**

1. **Task 5.1.3.10:** Create XML documentation parser
   - Extract `<exception>` tags from documentation comments
   - Parse `cref` attributes
   - Resolve exception types

2. **Task 5.1.3.11:** Implement documentation validator
   - Compare propagated exceptions with documented exceptions
   - Handle inherited documentation
   - Check interface implementations

3. **Task 5.1.3.12:** Add unit tests
   - Test method with matching documentation
   - Test method missing documentation
   - Test inherited documentation
   - Test interface implementations

### 5.1.4: Configuration and Testing

**Configuration Options:**

```ini
[*.cs]

# Exception flow analysis
throws_analyzer_enable_exception_flow = true
throws_analyzer_max_call_graph_depth = 5
throws_analyzer_max_propagation_depth = 3
throws_analyzer_analyze_cross_assembly = false

# THROWS017: Method calls throwing method
throws_analyzer_enable_calls_throwing_method = true
dotnet_diagnostic.THROWS017.severity = suggestion

# THROWS018: Long propagation chain
throws_analyzer_enable_long_propagation = true
dotnet_diagnostic.THROWS018.severity = suggestion

# THROWS019: Undocumented exception
throws_analyzer_enable_undocumented_exception = true
dotnet_diagnostic.THROWS019.severity = warning
throws_analyzer_require_public_api_docs = true
```

**Testing Strategy:**

1. **Task 5.1.4.1:** Call graph tests (20 tests)
   - Simple call chains
   - Recursive calls
   - Virtual method dispatch
   - Delegate invocations
   - Edge cases (null, missing symbols)

2. **Task 5.1.4.2:** Propagation tracking tests (25 tests)
   - Direct throw propagation
   - Multi-level propagation
   - Exception caught mid-chain
   - Multiple exception types
   - Partial handling

3. **Task 5.1.4.3:** Analyzer integration tests (30 tests)
   - THROWS017 with various scenarios
   - THROWS018 with different depths
   - THROWS019 with documentation variations
   - Performance tests on large codebases

4. **Task 5.1.4.4:** Sample project updates
   - Add exception flow examples to LibraryManagement
   - Demonstrate each new diagnostic
   - Show configuration options

**Performance Considerations:**

- **Task 5.1.4.5:** Implement caching strategy
  - Cache call graph per compilation
  - Cache flow analysis results per method
  - Invalidate on code changes

- **Task 5.1.4.6:** Add performance limits
  - Limit call graph depth (default: 5)
  - Limit path enumeration (max 100 paths)
  - Timeout for expensive analyses (5 seconds)

## Phase 5.2: Async Exception Analysis (2-3 weeks)

### Objectives

Detect exception handling issues specific to async/await:
- Synchronous throws before first await
- Unobserved task exceptions
- ConfigureAwait and exception handling
- Async void methods

### 5.2.1: Async Method Detector

**Purpose:** Identify async methods and analyze their exception behavior.

**New Component:** `AsyncMethodAnalyzer.cs`

```csharp
namespace ThrowsAnalyzer.AsyncAnalysis
{
    /// <summary>
    /// Analyzes async methods for exception handling issues.
    /// </summary>
    public static class AsyncMethodAnalyzer
    {
        /// <summary>
        /// Determines if a method is async.
        /// </summary>
        public static bool IsAsyncMethod(IMethodSymbol method)
        {
            return method.IsAsync ||
                   method.ReturnType.Name == "Task" ||
                   method.ReturnType.Name == "ValueTask";
        }

        /// <summary>
        /// Checks if method is async void (dangerous pattern).
        /// </summary>
        public static bool IsAsyncVoid(IMethodSymbol method)
        {
            return method.IsAsync &&
                   method.ReturnsVoid;
        }

        /// <summary>
        /// Finds the first await expression in a method.
        /// </summary>
        public static AwaitExpressionSyntax? FindFirstAwait(
            MethodDeclarationSyntax method)
        {
            return method.DescendantNodes()
                .OfType<AwaitExpressionSyntax>()
                .FirstOrDefault();
        }

        /// <summary>
        /// Determines if a throw occurs before the first await.
        /// </summary>
        public static bool ThrowsBeforeFirstAwait(
            MethodDeclarationSyntax method,
            SyntaxNode throwNode)
        {
            var firstAwait = FindFirstAwait(method);
            if (firstAwait == null)
            {
                // No await, so any throw is "before" await
                return true;
            }

            // Check if throw comes before await in execution order
            return throwNode.SpanStart < firstAwait.SpanStart;
        }

        /// <summary>
        /// Finds all unobserved task creations.
        /// </summary>
        public static IEnumerable<InvocationExpressionSyntax>
            FindUnobservedTasks(MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            var invocations = method.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    // Check if returns Task/ValueTask
                    var returnType = methodSymbol.ReturnType;
                    if (IsTaskType(returnType))
                    {
                        // Check if result is observed (awaited, stored, etc.)
                        if (!IsTaskObserved(invocation))
                        {
                            yield return invocation;
                        }
                    }
                }
            }
        }

        private static bool IsTaskType(ITypeSymbol type)
        {
            return type.Name == "Task" || type.Name == "ValueTask";
        }

        private static bool IsTaskObserved(InvocationExpressionSyntax invocation)
        {
            // Check if invocation is:
            // 1. Awaited: await invocation
            // 2. Assigned: var task = invocation
            // 3. Returned: return invocation
            // 4. Passed as argument: Method(invocation)

            var parent = invocation.Parent;

            return parent is AwaitExpressionSyntax
                || parent is EqualsValueClauseSyntax
                || parent is ReturnStatementSyntax
                || parent is ArgumentSyntax;
        }
    }
}
```

**Implementation Steps:**

1. **Task 5.2.1.1:** Create `AsyncMethodAnalyzer` class
   - Implement `IsAsyncMethod`
   - Implement `IsAsyncVoid`

2. **Task 5.2.1.2:** Implement await detection
   - Find first await expression
   - Compare positions with throw statements

3. **Task 5.2.1.3:** Implement unobserved task detection
   - Find task-returning invocations
   - Check if results are observed
   - Handle fire-and-forget patterns

4. **Task 5.2.1.4:** Add unit tests (15 tests)
   - Test async method detection
   - Test async void detection
   - Test throw before/after await
   - Test unobserved task detection

### 5.2.2: New Diagnostics for Async

**THROWS020: Async Method Throws Synchronously**

```csharp
public const string DiagnosticId020 = "THROWS020";

private static readonly LocalizableString Title020 =
    "Async method throws synchronously before first await";
private static readonly LocalizableString MessageFormat020 =
    "Async method '{0}' throws '{1}' before first await - exception won't be captured in Task";
private static readonly LocalizableString Description020 =
    "Detects async methods that throw exceptions before the first await, " +
    "which means the exception is thrown synchronously rather than captured in the returned Task.";

public static readonly DiagnosticDescriptor AsyncThrowsBeforeAwait =
    new DiagnosticDescriptor(
        DiagnosticId020,
        Title020,
        MessageFormat020,
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description020);
```

**Implementation:**

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncExceptionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(AsyncThrowsBeforeAwait, AsyncVoidThrows, UnobservedTask);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(
            AnalyzeAsyncMethod,
            SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeAsyncMethod(SyntaxNodeAnalysisContext context)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax);

        if (methodSymbol == null || !AsyncMethodAnalyzer.IsAsyncMethod(methodSymbol))
            return;

        // Check for throws before first await
        var throws = TypedThrowDetector.GetTypedThrows(methodSyntax, context.SemanticModel);

        foreach (var throwInfo in throws)
        {
            if (AsyncMethodAnalyzer.ThrowsBeforeFirstAwait(methodSyntax, throwInfo.ThrowNode))
            {
                var diagnostic = Diagnostic.Create(
                    AsyncThrowsBeforeAwait,
                    throwInfo.Location,
                    methodSymbol.Name,
                    throwInfo.ExceptionTypeName);

                context.ReportDiagnostic(diagnostic);
            }
        }

        // Check for async void
        if (AsyncMethodAnalyzer.IsAsyncVoid(methodSymbol))
        {
            // Report THROWS021: Async void method
        }

        // Check for unobserved tasks
        var unobservedTasks = AsyncMethodAnalyzer.FindUnobservedTasks(
            methodSyntax, context.SemanticModel);

        foreach (var task in unobservedTasks)
        {
            // Report THROWS022: Unobserved task
        }
    }
}
```

**Implementation Steps:**

1. **Task 5.2.2.1:** Create `AsyncExceptionAnalyzer`
   - Add diagnostic descriptors
   - Register syntax node action

2. **Task 5.2.2.2:** Implement THROWS020 detection
   - Find throws in async methods
   - Check position relative to first await
   - Report diagnostic

3. **Task 5.2.2.3:** Add code fix for THROWS020
   - Move validation before async
   - Or wrap in Task.FromException

4. **Task 5.2.2.4:** Add unit tests (10 tests)
   - Test throw before await
   - Test throw after await
   - Test no await in method
   - Test multiple throws

**THROWS021: Async Void Method Throws**

```csharp
public const string DiagnosticId021 = "THROWS021";

private static readonly LocalizableString Title021 =
    "Async void method can throw unhandled exceptions";
private static readonly LocalizableString MessageFormat021 =
    "Async void method '{0}' can throw exceptions that cannot be caught - " +
    "consider returning Task instead";
private static readonly LocalizableString Description021 =
    "Detects async void methods which can throw exceptions that crash the application.";

public static readonly DiagnosticDescriptor AsyncVoidThrows =
    new DiagnosticDescriptor(
        DiagnosticId021,
        Title021,
        MessageFormat021,
        "Reliability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description021);
```

**Implementation Steps:**

1. **Task 5.2.2.5:** Implement THROWS021 detection
   - Detect async void methods
   - Check if they have throws
   - Report diagnostic

2. **Task 5.2.2.6:** Add code fix
   - Change return type to Task
   - Update method signature

3. **Task 5.2.2.7:** Add unit tests (8 tests)
   - Test async void with throw
   - Test async Task (should not report)
   - Test event handlers (exception allowed)

**THROWS022: Unobserved Task Exception**

```csharp
public const string DiagnosticId022 = "THROWS022";

private static readonly LocalizableString Title022 =
    "Task result not observed - exceptions may be silently lost";
private static readonly LocalizableString MessageFormat022 =
    "Task from '{0}' is not awaited or observed - exceptions will be lost";
private static readonly LocalizableString Description022 =
    "Detects task-returning methods whose results are not observed.";

public static readonly DiagnosticDescriptor UnobservedTask =
    new DiagnosticDescriptor(
        DiagnosticId022,
        Title022,
        MessageFormat022,
        "Reliability",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description022);
```

**Implementation Steps:**

1. **Task 5.2.2.8:** Implement THROWS022 detection
   - Find task-returning invocations
   - Check if results are observed
   - Report diagnostic

2. **Task 5.2.2.9:** Add code fix
   - Add await keyword
   - Or assign to variable
   - Or add `.ContinueWith` handler

3. **Task 5.2.2.10:** Add unit tests (12 tests)
   - Test unobserved task
   - Test awaited task
   - Test assigned task
   - Test fire-and-forget scenarios

### 5.2.3: Configuration and Testing

**Configuration:**

```ini
[*.cs]

# Async exception analysis
throws_analyzer_enable_async_analysis = true

# THROWS020: Throw before await
throws_analyzer_enable_async_sync_throw = true
dotnet_diagnostic.THROWS020.severity = warning

# THROWS021: Async void
throws_analyzer_enable_async_void = true
dotnet_diagnostic.THROWS021.severity = warning
throws_analyzer_allow_async_void_event_handlers = true

# THROWS022: Unobserved task
throws_analyzer_enable_unobserved_task = true
dotnet_diagnostic.THROWS022.severity = suggestion
throws_analyzer_allow_fire_and_forget = false
```

**Testing:**

1. **Task 5.2.3.1:** Unit tests (30 tests total)
   - 10 tests for THROWS020
   - 8 tests for THROWS021
   - 12 tests for THROWS022

2. **Task 5.2.3.2:** Integration tests
   - Real async/await scenarios
   - Complex control flow
   - ConfigureAwait variations

3. **Task 5.2.3.3:** Performance tests
   - Large async methods
   - Many await expressions
   - Deep async call chains

## Phase 5.3: Iterator Exception Analysis (1-2 weeks)

### Objectives

Detect exception issues in iterator methods (yield return/yield break):
- Exceptions thrown during enumeration vs definition
- Try-finally behavior in iterators
- Multiple enumerations

### 5.3.1: Iterator Method Detector

**New Component:** `IteratorMethodAnalyzer.cs`

```csharp
namespace ThrowsAnalyzer.IteratorAnalysis
{
    /// <summary>
    /// Analyzes iterator methods (yield return/break) for exception issues.
    /// </summary>
    public static class IteratorMethodAnalyzer
    {
        /// <summary>
        /// Determines if a method is an iterator.
        /// </summary>
        public static bool IsIteratorMethod(MethodDeclarationSyntax method)
        {
            return method.DescendantNodes()
                .Any(n => n is YieldStatementSyntax);
        }

        /// <summary>
        /// Determines if method returns IEnumerable or IAsyncEnumerable.
        /// </summary>
        public static bool ReturnsEnumerable(IMethodSymbol method)
        {
            var returnType = method.ReturnType;
            return returnType.Name == "IEnumerable" ||
                   returnType.Name == "IEnumerator" ||
                   returnType.Name == "IAsyncEnumerable" ||
                   returnType.Name == "IAsyncEnumerator";
        }

        /// <summary>
        /// Finds throws that occur before any yield.
        /// These execute during MoveNext(), not during method call.
        /// </summary>
        public static IEnumerable<SyntaxNode> FindThrowsBeforeYield(
            MethodDeclarationSyntax method)
        {
            var firstYield = method.DescendantNodes()
                .OfType<YieldStatementSyntax>()
                .FirstOrDefault();

            if (firstYield == null)
                yield break;

            var throws = method.DescendantNodes()
                .Where(n => n is ThrowStatementSyntax or ThrowExpressionSyntax)
                .Where(t => t.SpanStart < firstYield.SpanStart);

            foreach (var throwNode in throws)
            {
                yield return throwNode;
            }
        }

        /// <summary>
        /// Finds try-finally blocks in iterators.
        /// Finally blocks in iterators have special behavior.
        /// </summary>
        public static IEnumerable<TryStatementSyntax> FindTryFinallyInIterator(
            MethodDeclarationSyntax method)
        {
            if (!IsIteratorMethod(method))
                yield break;

            var tryStatements = method.DescendantNodes()
                .OfType<TryStatementSyntax>()
                .Where(t => t.Finally != null);

            foreach (var tryStatement in tryStatements)
            {
                yield return tryStatement;
            }
        }
    }
}
```

**Implementation Steps:**

1. **Task 5.3.1.1:** Create `IteratorMethodAnalyzer` class
   - Implement iterator detection
   - Implement enumerable type detection

2. **Task 5.3.1.2:** Implement yield-relative analysis
   - Find throws before/after yield
   - Analyze execution timing

3. **Task 5.3.1.3:** Implement try-finally detection
   - Find try-finally in iterators
   - Analyze finally execution guarantees

4. **Task 5.3.1.4:** Add unit tests (10 tests)
   - Test iterator detection
   - Test throw timing analysis
   - Test try-finally detection

### 5.3.2: New Diagnostics for Iterators

**THROWS023: Exception In Iterator Deferred Until Enumeration**

```csharp
public const string DiagnosticId023 = "THROWS023";

private static readonly LocalizableString Title023 =
    "Exception in iterator method deferred until enumeration";
private static readonly LocalizableString MessageFormat023 =
    "Exception '{0}' in iterator method '{1}' won't be thrown until enumeration starts";
private static readonly LocalizableString Description023 =
    "Detects exceptions in iterator methods that are deferred until enumeration.";

public static readonly DiagnosticDescriptor IteratorDeferredException =
    new DiagnosticDescriptor(
        DiagnosticId023,
        Title023,
        MessageFormat023,
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description023);
```

**THROWS024: Try-Finally In Iterator May Not Execute As Expected**

```csharp
public const string DiagnosticId024 = "THROWS024";

private static readonly LocalizableString Title024 =
    "Try-finally in iterator has special behavior";
private static readonly LocalizableString MessageFormat024 =
    "Finally block in iterator method '{0}' only executes when enumeration completes or is disposed";
private static readonly LocalizableString Description024 =
    "Detects try-finally blocks in iterators which have different execution semantics.";

public static readonly DiagnosticDescriptor IteratorTryFinally =
    new DiagnosticDescriptor(
        DiagnosticId024,
        Title024,
        MessageFormat024,
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description024);
```

**Implementation Steps:**

1. **Task 5.3.2.1:** Implement THROWS023 detector
   - Find throws in iterator methods
   - Check if before/after yield
   - Report deferred exceptions

2. **Task 5.3.2.2:** Implement THROWS024 detector
   - Find try-finally in iterators
   - Report special behavior warning

3. **Task 5.3.2.3:** Add code fixes
   - Suggest validation before yield
   - Suggest wrapper method pattern

4. **Task 5.3.2.4:** Add unit tests (15 tests)
   - Test various iterator scenarios
   - Test try-finally behavior
   - Test wrapper pattern

## Phase 5.4: Best Practices & Design Patterns (1-2 weeks)

### Objectives

Detect exception anti-patterns and suggest better alternatives:
- Exception used for control flow
- Result<T> pattern suggestions
- Custom exception naming conventions
- Performance considerations

### 5.4.1: Exception Design Pattern Analyzer

**THROWS025: Exception Used For Control Flow**

```csharp
public const string DiagnosticId025 = "THROWS025";

private static readonly LocalizableString Title025 =
    "Exception used for control flow";
private static readonly LocalizableString MessageFormat025 =
    "Exception '{0}' appears to be used for control flow - consider using return values instead";
private static readonly LocalizableString Description025 =
    "Detects exceptions that are caught immediately and used for control flow.";
```

**THROWS026: Consider Result<T> Pattern**

```csharp
public const string DiagnosticId026 = "THROWS026";

private static readonly LocalizableString Title026 =
    "Consider using Result<T> pattern instead of exceptions";
private static readonly LocalizableString MessageFormat026 =
    "Method '{0}' frequently throws '{1}' - consider using Result<T> pattern";
private static readonly LocalizableString Description026 =
    "Suggests using Result<T> pattern for expected errors.";
```

**THROWS027: Custom Exception Naming Convention**

```csharp
public const string DiagnosticId027 = "THROWS027";

private static readonly LocalizableString Title027 =
    "Custom exception doesn't follow naming convention";
private static readonly LocalizableString MessageFormat027 =
    "Exception type '{0}' should end with 'Exception'";
private static readonly LocalizableString Description027 =
    "Detects custom exception types that don't follow naming conventions.";
```

**THROWS028: Exception In Hot Path**

```csharp
public const string DiagnosticId028 = "THROWS028";

private static readonly LocalizableString Title028 =
    "Exception in potential hot path";
private static readonly LocalizableString MessageFormat028 =
    "Method '{0}' throws '{1}' in loop or frequently called method - performance concern";
private static readonly LocalizableString Description028 =
    "Detects exceptions in loops or frequently called methods.";
```

**Implementation Steps:**

1. **Task 5.4.1.1:** Implement control flow detection
   - Find throw/catch in same method
   - Detect exception-based branching

2. **Task 5.4.1.2:** Implement Result<T> suggestion
   - Analyze throw frequency
   - Suggest Result<T> for validation errors

3. **Task 5.4.1.3:** Implement naming convention checker
   - Find custom exception types
   - Check naming patterns

4. **Task 5.4.1.4:** Implement hot path detection
   - Find throws in loops
   - Find throws in high-frequency methods

5. **Task 5.4.1.5:** Add unit tests (20 tests)
   - Test each diagnostic
   - Test code fix suggestions

## Timeline and Milestones

### Phase 5.1: Exception Flow Analysis (4-5 weeks)
- Week 1: Call graph builder
- Week 2: Propagation tracker
- Week 3: THROWS017, THROWS018, THROWS019 diagnostics
- Week 4: Testing and optimization
- Week 5: Documentation and samples

### Phase 5.2: Async Exception Analysis (2-3 weeks)
- Week 1: Async method analyzer + THROWS020
- Week 2: THROWS021, THROWS022 + code fixes
- Week 3: Testing and integration

### Phase 5.3: Iterator Exception Analysis (1-2 weeks)
- Week 1: Iterator analyzer + THROWS023, THROWS024
- Week 2: Testing and documentation

### Phase 5.4: Best Practices (1-2 weeks)
- Week 1: THROWS025-028 diagnostics
- Week 2: Code fixes and testing

**Total Phase 5 Duration: 8-12 weeks**

## Success Criteria

### Functional
- [ ] Call graph correctly built for all scenarios
- [ ] Exception propagation accurately traced
- [ ] Async exceptions properly detected
- [ ] Iterator exceptions correctly identified
- [ ] All 150+ new tests passing

### Performance
- [ ] Call graph construction < 500ms for 1000-method file
- [ ] Flow analysis < 100ms per method
- [ ] No analyzer timeouts on large codebases

### Quality
- [ ] Zero false positives in sample projects
- [ ] All diagnostics have code fixes
- [ ] Documentation complete
- [ ] Sample code demonstrates all features

## Next Steps

After Phase 5 completion, proceed to **Phase 6: IDE Integration** for enhanced user experience.
