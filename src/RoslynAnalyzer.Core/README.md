# RoslynAnalyzer.Core

A comprehensive, production-ready infrastructure library for building Roslyn analyzers and code fix providers. Extract reusable components from the ThrowsAnalyzer project.

[![NuGet](https://img.shields.io/nuget/v/RoslynAnalyzer.Core.svg)](https://www.nuget.org/packages/RoslynAnalyzer.Core/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

### 🔍 Executable Member Detection
Unified handling of all C# executable contexts with a consistent API:
- Methods, Constructors, Destructors
- Operators (regular and conversion)
- Property accessors (get, set, init, add, remove)
- Local functions
- Lambda expressions (simple and parenthesized)
- Anonymous methods

### 📊 Call Graph Analysis
Build and analyze method invocation graphs:
- Automatic call graph construction from compilation
- Cycle detection and depth limiting
- Transitive caller/callee relationships
- Support for all member types

### ⚡ Async/Await Pattern Detection
Comprehensive async/await analysis:
- Async method detection (Task, Task<T>, ValueTask, ValueTask<T>)
- Async void detection
- Unawaited task invocation detection
- Custom awaitable type support

### 🔄 Iterator Pattern Detection
Analyze yield-based iterator methods:
- Iterator method detection (IEnumerable, IEnumerator, IAsyncEnumerable)
- Yield statement detection
- Generic and non-generic iterator support

### 🏗️ Type Hierarchy Analysis
Type relationship utilities:
- Type assignability checking
- Interface implementation detection (including generics)
- Type hierarchy traversal
- Common base type finding

### ⚙️ Configuration Infrastructure
.editorconfig integration:
- Generic option reading (bool, string, int)
- Analyzer enable/disable support
- Feature flags with defaults
- Prefix-based organization

### 🛡️ Suppression Support
Custom attribute-based diagnostic suppression:
- Single and multiple attribute name support
- Wildcard pattern matching (e.g., "RULE*")
- Member and type-level suppression

### 🚀 Performance Optimizations
Caching utilities for expensive operations:
- Compilation-scoped caching
- Symbol-scoped caching
- Thread-safe implementations
- Optional statistics tracking

### 🛠️ Diagnostic Helpers
Common diagnostic operations:
- Location resolution for all member types
- Identifier location extraction

## Installation

```bash
dotnet add package RoslynAnalyzer.Core
```

Or add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="RoslynAnalyzer.Core" Version="1.0.0" />
</ItemGroup>
```

## Quick Start

### Detecting Executable Members

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynAnalyzer.Core.Members;

public class MyAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMember,
            AnalyzerConfiguration.ExecutableMemberSyntaxKinds);
    }

    private void AnalyzeMember(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;

        // Check if this is an executable member
        if (!ExecutableMemberHelper.IsExecutableMember(node))
            return;

        // Get all executable blocks (handles both block and expression bodies)
        var blocks = ExecutableMemberHelper.GetExecutableBlocks(node);

        // Get a display name for diagnostics
        string displayName = ExecutableMemberHelper.GetMemberDisplayName(node);

        // Report diagnostic
        var location = DiagnosticHelpers.GetMemberLocation(node);
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, displayName));
    }
}
```

### Building and Analyzing Call Graphs

```csharp
using RoslynAnalyzer.Core.Analysis.CallGraph;

// Build call graph from compilation
var builder = new CallGraphBuilder(compilation, cancellationToken);
var graph = await builder.BuildAsync();

// Get call graph node for a method
var node = graph.GetOrAddNode(methodSymbol);

// Analyze direct callees
Console.WriteLine($"Method {methodSymbol.Name} calls:");
foreach (var callee in node.Callees)
{
    Console.WriteLine($"  - {callee.Target.Method.Name}");
}

// Get transitive callees (up to depth 10)
var transitiveMethods = CallGraphBuilder.GetTransitiveCallees(
    graph, methodSymbol, maxDepth: 10);

Console.WriteLine($"Transitive callees (count: {transitiveMethods.Count})");

// Detect cycles
if (CallGraphBuilder.HasCycle(graph, methodSymbol))
{
    Console.WriteLine("⚠️ Cycle detected!");
}
```

### Custom Flow Analysis

```csharp
using RoslynAnalyzer.Core.Analysis.Flow;

// Define your flow type (e.g., exceptions, null values, resources)
public enum ExceptionFlow
{
    ThrowsException,
    CatchesException,
    PropagatesException
}

// Define flow information
public class ExceptionFlowInfo : IFlowInfo<ExceptionFlow>
{
    public ExceptionFlow Flow { get; set; }
    public ITypeSymbol? ExceptionType { get; set; }

    public IFlowInfo<ExceptionFlow> Merge(IFlowInfo<ExceptionFlow> other)
    {
        // Your merge logic
        return new ExceptionFlowInfo { /* ... */ };
    }
}

// Implement flow analyzer
public class ExceptionFlowAnalyzer : FlowAnalyzerBase<ExceptionFlow, ExceptionFlowInfo>
{
    protected override ExceptionFlowInfo AnalyzeMethodCore(IMethodSymbol method)
    {
        // Your analysis logic
        return new ExceptionFlowInfo { /* ... */ };
    }
}

// Use the analyzer
var analyzer = new ExceptionFlowAnalyzer(callGraph, semanticModel);
var flowInfo = analyzer.AnalyzeMethod(methodSymbol);
```

### Async Pattern Detection

```csharp
using RoslynAnalyzer.Core.Analysis.Patterns.Async;

// Check if method is async
if (AsyncMethodDetector.IsAsyncMethod(methodSymbol))
{
    Console.WriteLine($"{methodSymbol.Name} is async");

    // Check for async void (dangerous pattern)
    if (AsyncMethodDetector.IsAsyncVoid(methodSymbol))
    {
        Console.WriteLine("⚠️ Async void detected!");
    }
}

// Find unawaited task invocations
var methodBody = methodDeclaration.Body ?? (SyntaxNode)methodDeclaration.ExpressionBody;
var unawaited = AsyncMethodDetector.GetUnawaitedTaskInvocations(
    methodBody, semanticModel);

foreach (var invocation in unawaited)
{
    Console.WriteLine($"⚠️ Unawaited task: {invocation}");
}

// Custom awaitable types
var customAwaitableTypes = new[] { "MyCustomTask", "CustomAwaitable" };
bool isCustomAsync = AsyncMethodDetector.IsAsyncMethod(
    methodSymbol, customAwaitableTypes);
```

### Iterator Pattern Detection

```csharp
using RoslynAnalyzer.Core.Analysis.Patterns.Iterators;

// Check if method is an iterator
if (IteratorMethodDetector.IsIteratorMethod(methodSymbol))
{
    Console.WriteLine($"{methodSymbol.Name} is an iterator");

    // Check for async iterator
    if (IteratorMethodDetector.IsAsyncIterator(methodSymbol))
    {
        Console.WriteLine("Async iterator (IAsyncEnumerable)");
    }
}

// Find yield statements
var methodBody = methodDeclaration.Body;
var yieldStatements = IteratorMethodDetector.GetYieldStatements(methodBody);

Console.WriteLine($"Found {yieldStatements.Count()} yield statements");
```

### Type Hierarchy Analysis

```csharp
using RoslynAnalyzer.Core.TypeAnalysis;

var typeAnalyzer = new TypeHierarchyAnalyzer(compilation);

// Check type assignability
if (typeAnalyzer.IsAssignableTo(sourceType, targetType))
{
    Console.WriteLine($"{sourceType.Name} is assignable to {targetType.Name}");
}

// Check interface implementation
if (typeAnalyzer.ImplementsInterface(classType, "IDisposable"))
{
    Console.WriteLine("Implements IDisposable");
}

// Check generic interface implementation
if (typeAnalyzer.ImplementsGenericInterface(listType, "IEnumerable", 1))
{
    Console.WriteLine("Implements IEnumerable<T>");
}

// Get type hierarchy
var hierarchy = typeAnalyzer.GetTypeHierarchy(exceptionType);
foreach (var type in hierarchy)
{
    Console.WriteLine($"  - {type.Name}");
}

// Find common base type
var commonBase = typeAnalyzer.FindCommonBaseType(type1, type2);
Console.WriteLine($"Common base: {commonBase?.Name ?? "object"}");
```

### Configuration Reading

```csharp
using RoslynAnalyzer.Core.Configuration.Options;

public override void Initialize(AnalysisContext context)
{
    context.RegisterSyntaxNodeAction(ctx =>
    {
        // Check if analyzer is enabled
        if (!AnalyzerOptionsReader.IsAnalyzerEnabled(
            ctx.Options, ctx.Node.SyntaxTree, "my_analyzer", "specific_rule"))
        {
            return;
        }

        // Read configuration options
        var threshold = AnalyzerOptionsReader.GetIntOption(
            ctx.Options, ctx.Node.SyntaxTree, "my_analyzer.threshold", defaultValue: 10);

        var mode = AnalyzerOptionsReader.GetStringOption(
            ctx.Options, ctx.Node.SyntaxTree, "my_analyzer.mode", defaultValue: "strict");

        var enabled = AnalyzerOptionsReader.GetBoolOption(
            ctx.Options, ctx.Node.SyntaxTree, "my_analyzer.feature_x", defaultValue: true);

        // Check feature flag
        if (AnalyzerOptionsReader.IsFeatureEnabled(
            ctx.Options, ctx.Node.SyntaxTree, "my_analyzer", "advanced_checks"))
        {
            // Advanced analysis
        }

    }, SyntaxKind.MethodDeclaration);
}
```

### Suppression Support

```csharp
using RoslynAnalyzer.Core.Configuration.Suppression;

private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
{
    var methodNode = (MethodDeclarationSyntax)context.Node;
    var semanticModel = context.SemanticModel;

    // Check for suppression attribute
    if (SuppressionHelper.IsSuppressed(
        semanticModel, methodNode, "MY001", "SuppressMyAnalysisAttribute"))
    {
        return; // Diagnostic suppressed
    }

    // Support multiple attribute names
    if (SuppressionHelper.IsSuppressed(
        semanticModel, methodNode, "MY001",
        "SuppressMyAnalysis", "SuppressMyAnalysisAttribute"))
    {
        return;
    }

    // Wildcard support - suppress all MY* diagnostics
    // User can use: [SuppressMyAnalysis("MY*")]
}
```

### Performance - Caching

```csharp
using RoslynAnalyzer.Core.Performance.Caching;

// Compilation-scoped cache
private static readonly CompilationCache<INamedTypeSymbol?> TypeCache = new();

var exceptionType = TypeCache.GetOrAdd(
    compilation,
    "System.Exception",
    (comp, typeName) => comp.GetTypeByMetadataName(typeName));

// Symbol-scoped cache
private static readonly SymbolCache<int> HierarchyDepthCache = new();

var depth = HierarchyDepthCache.GetOrAdd(typeSymbol, symbol =>
{
    int depth = 0;
    var current = symbol.BaseType;
    while (current != null && current.Name != "Object")
    {
        depth++;
        current = current.BaseType;
    }
    return depth;
});

// Cache with statistics
private static readonly CompilationCacheWithStatistics<INamedTypeSymbol?> MonitoredCache = new();

// Use cache...
var stats = MonitoredCache.GetStatistics();
Console.WriteLine($"Cache hit ratio: {stats.HitRatio:P2}");
Console.WriteLine($"Total lookups: {stats.TotalLookups}");

// Reset statistics for performance measurement window
MonitoredCache.ResetStatistics();
```

## Architecture

### Component Organization

```
RoslynAnalyzer.Core/
├── Members/                          # Executable member detection
│   ├── IExecutableMemberDetector     # Interface for member detectors
│   ├── ExecutableMemberHelper        # Main API for member operations
│   └── Detectors/                    # Specific member type detectors
├── Analysis/
│   ├── CallGraph/                    # Call graph infrastructure
│   │   ├── CallGraph                 # Graph data structure
│   │   └── CallGraphBuilder          # Graph construction
│   ├── Flow/                         # Generic flow analysis pattern
│   │   ├── IFlowInfo                 # Flow information interface
│   │   ├── IFlowAnalyzer             # Flow analyzer interface
│   │   └── FlowAnalyzerBase          # Base implementation
│   └── Patterns/                     # Common code patterns
│       ├── Async/                    # Async/await patterns
│       └── Iterators/                # Iterator patterns
├── TypeAnalysis/                     # Type hierarchy analysis
│   ├── TypeHierarchyAnalyzer         # Main type analysis API
│   └── Extensions/                   # ITypeSymbol extensions
├── Configuration/                    # Configuration infrastructure
│   ├── AnalyzerConfiguration         # Common configuration
│   ├── Options/                      # .editorconfig reading
│   └── Suppression/                  # Suppression support
├── Performance/
│   └── Caching/                      # Caching utilities
└── Helpers/                          # Common helpers
    └── DiagnosticHelpers             # Diagnostic utilities
```

### Design Principles

1. **Generic and Reusable**: All components are designed to work with any analyzer, not tied to specific diagnostic rules
2. **Thread-Safe**: All caching and shared state is thread-safe for concurrent analysis
3. **Well-Documented**: Comprehensive XML documentation on all public APIs
4. **Tested**: Extracted from production code with 187 passing unit tests
5. **Zero Dependencies**: Only depends on official Roslyn packages

## Real-World Usage

RoslynAnalyzer.Core powers [ThrowsAnalyzer](https://github.com/wieslawsoltes/ThrowsAnalyzer), a comprehensive exception analysis tool with:
- 30 diagnostic rules
- 16 code fix providers
- 274 unit tests
- Full support for all C# executable member types

## API Reference

### Member Detection

#### ExecutableMemberHelper

```csharp
public static class ExecutableMemberHelper
{
    // Check if node is an executable member
    bool IsExecutableMember(SyntaxNode node);

    // Get all executable blocks from a member
    IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode memberNode);

    // Get identifier token
    SyntaxToken? GetIdentifierToken(SyntaxNode node);

    // Get display name for diagnostics
    string GetMemberDisplayName(SyntaxNode node);

    // Get specific detector for a syntax kind
    IExecutableMemberDetector? GetDetector(SyntaxKind kind);
}
```

### Call Graph

#### CallGraph

```csharp
public class CallGraph
{
    // Get or add node for method
    CallGraphNode GetOrAddNode(IMethodSymbol method);

    // Get node if exists
    CallGraphNode? GetNode(IMethodSymbol method);

    // Check if method exists in graph
    bool Contains(IMethodSymbol method);

    // Get all nodes
    IEnumerable<CallGraphNode> Nodes { get; }
}
```

#### CallGraphBuilder

```csharp
public class CallGraphBuilder
{
    // Build complete call graph
    Task<CallGraph> BuildAsync();

    // Build from specific method
    Task<CallGraph> BuildFromMethodAsync(IMethodSymbol method, int maxDepth = 50);

    // Static helpers
    static IReadOnlySet<IMethodSymbol> GetTransitiveCallees(
        CallGraph graph, IMethodSymbol method, int maxDepth);

    static IReadOnlySet<IMethodSymbol> GetTransitiveCallers(
        CallGraph graph, IMethodSymbol method, int maxDepth);

    static bool HasCycle(CallGraph graph, IMethodSymbol method);
}
```

### Async Patterns

#### AsyncMethodDetector

```csharp
public static class AsyncMethodDetector
{
    // Check if method is async
    bool IsAsyncMethod(IMethodSymbol method, IEnumerable<string>? customAwaitableTypes = null);

    // Check for async void
    bool IsAsyncVoid(IMethodSymbol method);

    // Find unawaited task invocations
    IEnumerable<InvocationExpressionSyntax> GetUnawaitedTaskInvocations(
        SyntaxNode body, SemanticModel semanticModel);
}
```

### Iterator Patterns

#### IteratorMethodDetector

```csharp
public static class IteratorMethodDetector
{
    // Check if method is iterator
    bool IsIteratorMethod(IMethodSymbol method);

    // Check for async iterator
    bool IsAsyncIterator(IMethodSymbol method);

    // Find yield statements
    IEnumerable<YieldStatementSyntax> GetYieldStatements(SyntaxNode? body);
}
```

### Type Analysis

#### TypeHierarchyAnalyzer

```csharp
public class TypeHierarchyAnalyzer
{
    // Check assignability
    bool IsAssignableTo(ITypeSymbol source, ITypeSymbol target);

    // Get type hierarchy
    IReadOnlyList<INamedTypeSymbol> GetTypeHierarchy(ITypeSymbol type);

    // Check interface implementation
    bool ImplementsInterface(ITypeSymbol type, string interfaceName);
    bool ImplementsGenericInterface(ITypeSymbol type, string interfaceName, int arity);

    // Find common base
    INamedTypeSymbol? FindCommonBaseType(ITypeSymbol type1, ITypeSymbol type2);
}
```

## Performance Considerations

### Caching Best Practices

```csharp
// ✅ Good: Use static cache for cross-invocation reuse
private static readonly CompilationCache<INamedTypeSymbol?> _cache = new();

// ❌ Bad: Creating new cache on each invocation
private void Analyze()
{
    var cache = new CompilationCache<INamedTypeSymbol?>(); // Don't do this
}

// ✅ Good: Clear compilation cache when compilation changes
_cache.ClearCompilation(oldCompilation);

// ✅ Good: Monitor cache effectiveness
var stats = _cacheWithStats.GetStatistics();
if (stats.HitRatio < 0.5)
{
    // Consider different caching strategy
}
```

### Call Graph Performance

```csharp
// ✅ Good: Limit depth for transitive operations
var callees = CallGraphBuilder.GetTransitiveCallees(graph, method, maxDepth: 10);

// ❌ Bad: Unlimited depth can cause exponential time
var callees = CallGraphBuilder.GetTransitiveCallees(graph, method, maxDepth: int.MaxValue);

// ✅ Good: Check for cycles before deep traversal
if (CallGraphBuilder.HasCycle(graph, method))
{
    // Handle cyclic call chains
}
```

## Migration Guide

Migrating from custom implementations to RoslynAnalyzer.Core:

### Before

```csharp
// Custom member detection
if (node is MethodDeclarationSyntax ||
    node is ConstructorDeclarationSyntax ||
    node is PropertyDeclarationSyntax ||
    /* ... many more cases ... */)
{
    // Analyze
}
```

### After

```csharp
using RoslynAnalyzer.Core.Members;

if (ExecutableMemberHelper.IsExecutableMember(node))
{
    // Analyze - handles all member types automatically
}
```

See [MIGRATION.md](../../docs/MIGRATION.md) for detailed migration guidance.

## Contributing

Contributions are welcome! This library was extracted from real-world analyzer development, and we welcome improvements and additional utilities.

## Versioning

We use [SemVer](http://semver.org/) for versioning.

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

## Acknowledgments

- Extracted from [ThrowsAnalyzer](https://github.com/wieslawsoltes/ThrowsAnalyzer)
- Built on top of [Roslyn](https://github.com/dotnet/roslyn)
- Inspired by the Roslyn analyzer community

## Support

- 📖 [Documentation](../../docs/)
- 🐛 [Issue Tracker](https://github.com/wieslawsoltes/ThrowsAnalyzer/issues)
- 💬 [Discussions](https://github.com/wieslawsoltes/ThrowsAnalyzer/discussions)
