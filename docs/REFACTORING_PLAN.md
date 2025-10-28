# ThrowsAnalyzer Refactoring Plan: Extracting Reusable Analyzer Infrastructure

## Executive Summary

This document outlines a comprehensive plan to refactor reusable components from the ThrowsAnalyzer project into a standalone library (`RoslynAnalyzer.Core` or `AnalyzerToolkit`) that can be leveraged by other Roslyn analyzers and code fix providers.

**Project Stats:**
- Current codebase: ~78 C# files
- Target framework: netstandard2.0
- Roslyn version: 4.12.0

**Key Benefits:**
- Reduce code duplication across analyzer projects
- Provide battle-tested infrastructure for common analyzer patterns
- Enable faster development of new analyzers
- Share performance optimizations (caching, graph algorithms)
- Standardize analyzer configuration and member detection

---

## 1. Analysis of Reusable Components

### 1.1 Call Graph Infrastructure (High Priority)

**Location:** `src/ThrowsAnalyzer/Analysis/`

**Components to Extract:**

#### 1.1.1 Core Call Graph Data Structures
- **File:** `CallGraph.cs`
- **Classes:**
  - `CallGraph` - Main graph container with node/edge management
  - `CallGraphNode` - Represents methods with caller/callee relationships
  - `CallGraphEdge` - Represents method invocations with location tracking
- **Reusability:** 95% - Generic, no exception-specific logic
- **Value:** High - Essential for inter-procedural analysis in any analyzer

#### 1.1.2 Call Graph Builder
- **File:** `CallGraphBuilder.cs`
- **Key Methods:**
  - `BuildAsync()` - Builds call graph for entire compilation
  - `BuildForMethodAsync()` - Builds graph for specific method
  - `GetTransitiveCallees()` - Gets all methods called transitively
  - `GetTransitiveCallers()` - Gets all callers transitively
- **Reusability:** 90% - Minor coupling to exception analysis
- **Features:**
  - Handles method invocations and constructor calls
  - Supports local functions
  - Cycle detection in recursive calls
  - Configurable depth limits

#### 1.1.3 Exception Flow Tracking (Domain-Specific, Consider Generalization)
- **File:** `ExceptionFlowInfo.cs`
- **Classes:**
  - `ExceptionFlowInfo` - Exception flow for a method
  - `ThrownExceptionInfo` - Information about thrown exceptions
  - `CaughtExceptionInfo` - Information about caught exceptions
  - `ExceptionPropagationChain` - Multi-level propagation tracking
- **Reusability:** 60% - Exception-specific but pattern is generalizable
- **Generalization Opportunity:** Create generic `FlowInfo<T>` pattern for tracking any data flow

**Refactoring Strategy:**
```
New Library: RoslynAnalyzer.Core.Analysis
├── CallGraph/
│   ├── CallGraph.cs
│   ├── CallGraphNode.cs
│   ├── CallGraphEdge.cs
│   ├── CallGraphBuilder.cs
│   └── CallGraphExtensions.cs
└── Flow/
    ├── IFlowInfo.cs (generic interface)
    ├── FlowAnalyzer.cs (generic base)
    └── Examples/
        └── ExceptionFlowInfo.cs (example implementation)
```

---

### 1.2 Executable Member Detection System (High Priority)

**Location:** `src/ThrowsAnalyzer/Core/`

**Components to Extract:**

#### 1.2.1 Member Detection Abstraction
- **File:** `IExecutableMemberDetector.cs`
- **Interface Methods:**
  - `SupportsNode(SyntaxNode)` - Type checking
  - `GetExecutableBlocks(SyntaxNode)` - Extract executable code regions
  - `GetMemberDisplayName(SyntaxNode)` - User-friendly naming
- **Reusability:** 100% - Fully generic
- **Value:** Very High - Eliminates boilerplate in analyzers

#### 1.2.2 Member Detector Implementations
- **Location:** `src/ThrowsAnalyzer/Core/MemberDetectors/`
- **Detectors (10 implementations):**
  1. `MethodMemberDetector.cs` - Regular methods
  2. `ConstructorMemberDetector.cs` - Constructors
  3. `DestructorMemberDetector.cs` - Finalizers
  4. `OperatorMemberDetector.cs` - Operator overloads
  5. `ConversionOperatorMemberDetector.cs` - Conversion operators
  6. `PropertyMemberDetector.cs` - Properties
  7. `AccessorMemberDetector.cs` - get/set/init/add/remove
  8. `LocalFunctionMemberDetector.cs` - Local functions
  9. `LambdaMemberDetector.cs` - Lambda expressions
  10. `AnonymousMethodMemberDetector.cs` - Anonymous methods

- **Reusability:** 100% - No domain-specific logic
- **Value:** Very High - Handles all C# executable contexts

#### 1.2.3 Central Helper
- **File:** `ExecutableMemberHelper.cs`
- **Key Features:**
  - Registry of all detectors
  - Unified API for member detection
  - Display name generation
  - Block extraction across member types
- **Reusability:** 100%

**Refactoring Strategy:**
```
New Library: RoslynAnalyzer.Core.Members
├── IExecutableMemberDetector.cs
├── ExecutableMemberHelper.cs
├── Detectors/
│   ├── MethodMemberDetector.cs
│   ├── ConstructorMemberDetector.cs
│   ├── PropertyMemberDetector.cs
│   ├── LambdaMemberDetector.cs
│   └── ... (all 10 detectors)
└── Extensions/
    └── SyntaxNodeExtensions.cs (helper methods)
```

---

### 1.3 Type Analysis Infrastructure (High Priority)

**Location:** `src/ThrowsAnalyzer/TypeAnalysis/`

**Components to Extract:**

#### 1.3.1 Exception Type Analyzer
- **File:** `ExceptionTypeAnalyzer.cs`
- **Key Methods:**
  - `GetThrownExceptionType()` - Extracts exception type from throw
  - `GetCaughtExceptionType()` - Extracts type from catch clause
  - `IsExceptionType()` - Verifies exception inheritance
  - `IsAssignableTo()` - Generic type hierarchy checking
  - `GetExceptionHierarchy()` - Inheritance chain extraction
- **Reusability:** 70% (exception-specific) / 100% (generic type methods)
- **Generalization:** Extract generic type hierarchy methods

#### 1.3.2 Catch Clause Analyzer
- **File:** `CatchClauseAnalyzer.cs`
- **Key Methods:**
  - `GetCatchClauses()` - Extract catch clause information
  - `DetectOrderingIssues()` - Find unreachable catches
  - `DetectEmptyCatches()` - Find exception swallowing
  - `DetectRethrowOnlyCatches()` - Find unnecessary catches
  - `DetectOverlyBroadCatches()` - Find broad exception catches
- **Reusability:** 80% - Patterns applicable to other analyzers
- **Value:** High - Comprehensive catch clause analysis

#### 1.3.3 Type Analysis Models
- **Location:** `TypeAnalysis/Models/`
- **Files:**
  - `TypedThrowInfo.cs` - Throw statement with type info
  - `CatchClauseInfo.cs` - Catch clause with type info
  - `CatchClauseOrderingIssue.cs` - Ordering problems
- **Reusability:** 60% exception-specific, 40% generalizable pattern

**Refactoring Strategy:**
```
New Library: RoslynAnalyzer.Core.TypeAnalysis
├── TypeHierarchyAnalyzer.cs (generic type operations)
├── Exception/
│   ├── ExceptionTypeAnalyzer.cs
│   ├── CatchClauseAnalyzer.cs
│   └── Models/
│       ├── TypedThrowInfo.cs
│       ├── CatchClauseInfo.cs
│       └── CatchClauseOrderingIssue.cs
└── Extensions/
    └── TypeSymbolExtensions.cs
```

---

### 1.4 Async Pattern Detection (Medium Priority)

**Location:** `src/ThrowsAnalyzer/Analysis/AsyncMethodDetector.cs`

**Components to Extract:**

#### 1.4.1 Async Method Detection
- **Key Methods:**
  - `IsAsyncMethod()` - Detects async methods
  - `ReturnsTask()` - Checks Task/Task&lt;T&gt; return types
  - `IsAsyncVoid()` - Detects async void anti-pattern
  - `GetFirstAwaitExpression()` - Finds first await
  - `GetAllAwaitExpressions()` - Finds all awaits
  - `IsThrowBeforeFirstAwait()` - Synchronous throw detection
  - `GetUnawaitedTaskInvocations()` - Fire-and-forget detection
  - `GetMethodBody()` - Extracts body from various syntax types
  - `HasAsyncModifier()` - Checks for async keyword

- **Reusability:** 95% - Generic async pattern detection
- **Value:** High - Async/await is complex, error-prone
- **Data Structure:** `AsyncMethodInfo` - Comprehensive async metadata

**Refactoring Strategy:**
```
New Library: RoslynAnalyzer.Core.Async
├── AsyncMethodDetector.cs
├── AsyncMethodInfo.cs
└── AsyncPatternAnalyzer.cs
```

---

### 1.5 Iterator Pattern Detection (Medium Priority)

**Location:** `src/ThrowsAnalyzer/Analysis/IteratorMethodDetector.cs`

**Components to Extract:**

#### 1.5.1 Iterator Method Detection
- **Key Methods:**
  - `IsIteratorMethod()` - Detects yield-based iterators
  - `ReturnsEnumerable()` - Checks IEnumerable/IEnumerator returns
  - `GetYieldReturnStatements()` - Finds yield returns
  - `GetYieldBreakStatements()` - Finds yield breaks
  - `HasYieldStatements()` - Quick yield detection
  - `GetThrowStatements()` - Finds throws in iterators
  - `GetTryFinallyStatements()` - Finds try-finally (disposal)
  - `IsThrowBeforeFirstYield()` - Eager validation detection
  - `HasYieldInTryBlock()` - Deferred execution analysis
  - `ImplementsInterface()` - Generic interface checking

- **Reusability:** 90% - Generic iterator analysis
- **Value:** Medium-High - Iterators are tricky with deferred execution
- **Data Structure:** `IteratorMethodInfo` - Comprehensive iterator metadata

**Refactoring Strategy:**
```
New Library: RoslynAnalyzer.Core.Iterators
├── IteratorMethodDetector.cs
├── IteratorMethodInfo.cs
└── IteratorPatternAnalyzer.cs
```

---

### 1.6 Configuration Infrastructure (Medium Priority)

**Location:** `src/ThrowsAnalyzer/Configuration/` and `src/ThrowsAnalyzer/Analyzers/`

**Components to Extract:**

#### 1.6.1 Analyzer Options Reader
- **File:** `AnalyzerOptionsReader.cs`
- **Features:**
  - .editorconfig integration
  - Hierarchical configuration (analyzer level, member type level)
  - Type-safe option reading
  - Default value handling
  - Member type filtering
- **Key Methods:**
  - `IsAnalyzerEnabled()` - Check if analyzer is enabled
  - `IsMemberTypeEnabled()` - Check if member type should be analyzed
  - `GetEnabledMemberTypes()` - Get all enabled member types
  - `GetMemberTypeKey()` - Map SyntaxKind to config key
- **Reusability:** 95% - Generic configuration pattern

#### 1.6.2 Analyzer Configuration
- **File:** `AnalyzerConfiguration.cs`
- **Features:**
  - Centralized syntax kind definitions
  - `ExecutableMemberSyntaxKinds` - All analyzable member types
- **Reusability:** 100%

#### 1.6.3 Suppression Helper
- **File:** `SuppressionHelper.cs`
- **Features:**
  - Attribute-based suppression
  - Wildcard support (`THROWS*`)
  - Member and type-level suppression
  - Symbol-based suppression checking
- **Key Methods:**
  - `IsSuppressed()` - Check if diagnostic is suppressed
  - `GetMemberSymbol()` - Extract symbol from various node types
  - `HasSuppressionAttribute()` - Check for suppression attributes
- **Reusability:** 85% - Needs abstraction for attribute name
- **Generalization:** Make attribute name configurable

**Refactoring Strategy:**
```
New Library: RoslynAnalyzer.Core.Configuration
├── Options/
│   ├── AnalyzerOptionsReader.cs
│   ├── AnalyzerConfiguration.cs
│   └── ConfigurationExtensions.cs
└── Suppression/
    ├── SuppressionHelper.cs
    ├── ISuppressionAttribute.cs
    └── SuppressionChecker.cs
```

---

### 1.7 Performance Optimizations (Medium Priority)

**Location:** `src/ThrowsAnalyzer/Performance/`

**Components to Extract:**

#### 1.7.1 Exception Type Cache
- **File:** `ExceptionTypeCache.cs`
- **Features:**
  - Concurrent dictionary-based caching
  - Compilation-scoped caching
  - Type symbol caching
  - Inheritance depth caching
  - Cache statistics
- **Key Methods:**
  - `GetExceptionType()` - Cached type lookup
  - `GetInheritanceDepth()` - Cached depth calculation
  - `Clear()` - Cache invalidation
  - `GetStatistics()` - Performance monitoring
- **Reusability:** 60% exception-specific, 40% generic caching pattern
- **Generalization:** Create `TypeSymbolCache<TMetadata>` generic

**Refactoring Strategy:**
```
New Library: RoslynAnalyzer.Core.Performance
├── Caching/
│   ├── TypeSymbolCache.cs (generic)
│   ├── CompilationCache.cs (generic)
│   └── CacheStatistics.cs
└── Examples/
    └── ExceptionTypeCache.cs (specialized implementation)
```

---

### 1.8 Helper Utilities (Low-Medium Priority)

**Location:** `src/ThrowsAnalyzer/Analyzers/` and `src/ThrowsAnalyzer/Detectors/`

**Components to Extract:**

#### 1.8.1 Analyzer Helper
- **File:** `AnalyzerHelper.cs`
- **Features:**
  - `GetMemberLocation()` - Gets precise diagnostic location
  - Handles all member types
  - Returns identifier/keyword locations for better UX
- **Reusability:** 100%

#### 1.8.2 Throw Statement Detector
- **File:** `ThrowStatementDetector.cs`
- **Features:**
  - Detects throw statements and expressions
  - Excludes nested executable members (avoids double reporting)
  - Supports all member types via `ExecutableMemberHelper`
- **Key Methods:**
  - `HasThrowStatements()` - Check for throws
  - `ContainsThrowSyntax()` - Recursive throw detection
  - `IsNestedExecutableMember()` - Prevents double analysis
- **Reusability:** 70% - Throw-specific but pattern is useful

**Refactoring Strategy:**
```
New Library: RoslynAnalyzer.Core.Helpers
├── DiagnosticHelpers.cs (GetMemberLocation, etc.)
├── SyntaxHelpers.cs (common syntax operations)
└── Detectors/
    ├── IStatementDetector.cs (generic interface)
    └── ThrowStatementDetector.cs (example)
```

---

### 1.9 Domain-Specific Components (Keep in ThrowsAnalyzer)

These components are tightly coupled to exception analysis and should remain in the original project:

#### 1.9.1 Analyzers
- All 30 diagnostic analyzers (THROWS001-030)
- Domain-specific business logic
- Exception-specific rules

#### 1.9.2 Code Fix Providers
- All 16 code fix providers
- Exception-specific refactorings
- Domain knowledge about fixes

#### 1.9.3 Detectors
- `UnhandledThrowDetector.cs` - Exception-specific
- `TryCatchDetector.cs` - Exception-specific
- `TypedThrowDetector.cs` - Exception-specific

#### 1.9.4 Diagnostics
- `MethodThrowsDiagnosticsBuilder.cs` - Exception-specific

---

## 2. Proposed Library Structure

### 2.1 New Library: RoslynAnalyzer.Core

**Target Framework:** netstandard2.0
**Dependencies:**
- Microsoft.CodeAnalysis.CSharp (4.12.0 or compatible)
- Microsoft.CodeAnalysis.CSharp.Workspaces (4.12.0 or compatible)

**Project Structure:**

```
RoslynAnalyzer.Core/
├── RoslynAnalyzer.Core.csproj
├── README.md
├── CHANGELOG.md
│
├── Analysis/
│   ├── CallGraph/
│   │   ├── CallGraph.cs
│   │   ├── CallGraphNode.cs
│   │   ├── CallGraphEdge.cs
│   │   ├── CallGraphBuilder.cs
│   │   └── CallGraphExtensions.cs
│   │
│   ├── Flow/
│   │   ├── IFlowInfo.cs
│   │   ├── IFlowAnalyzer.cs
│   │   └── FlowAnalyzerBase.cs
│   │
│   └── Patterns/
│       ├── Async/
│       │   ├── AsyncMethodDetector.cs
│       │   ├── AsyncMethodInfo.cs
│       │   └── AsyncPatternAnalyzer.cs
│       │
│       └── Iterators/
│           ├── IteratorMethodDetector.cs
│           ├── IteratorMethodInfo.cs
│           └── IteratorPatternAnalyzer.cs
│
├── Members/
│   ├── IExecutableMemberDetector.cs
│   ├── ExecutableMemberHelper.cs
│   │
│   └── Detectors/
│       ├── MethodMemberDetector.cs
│       ├── ConstructorMemberDetector.cs
│       ├── DestructorMemberDetector.cs
│       ├── OperatorMemberDetector.cs
│       ├── ConversionOperatorMemberDetector.cs
│       ├── PropertyMemberDetector.cs
│       ├── AccessorMemberDetector.cs
│       ├── LocalFunctionMemberDetector.cs
│       ├── LambdaMemberDetector.cs
│       └── AnonymousMethodMemberDetector.cs
│
├── TypeAnalysis/
│   ├── TypeHierarchyAnalyzer.cs
│   ├── InterfaceAnalyzer.cs
│   └── TypeSymbolExtensions.cs
│
├── Configuration/
│   ├── Options/
│   │   ├── AnalyzerOptionsReader.cs
│   │   ├── AnalyzerConfiguration.cs
│   │   └── ConfigurationExtensions.cs
│   │
│   └── Suppression/
│       ├── SuppressionHelper.cs
│       ├── ISuppressionAttribute.cs
│       └── SuppressionConfiguration.cs
│
├── Performance/
│   ├── Caching/
│   │   ├── TypeSymbolCache.cs
│   │   ├── CompilationCache.cs
│   │   ├── CacheStatistics.cs
│   │   └── ICacheStrategy.cs
│   │
│   └── Pooling/
│       └── (Future: Object pooling utilities)
│
├── Helpers/
│   ├── DiagnosticHelpers.cs
│   ├── SyntaxHelpers.cs
│   ├── SymbolHelpers.cs
│   └── CompilationHelpers.cs
│
└── Extensions/
    ├── SyntaxNodeExtensions.cs
    ├── SemanticModelExtensions.cs
    └── SymbolExtensions.cs
```

---

### 2.2 Updated ThrowsAnalyzer Structure

After refactoring, ThrowsAnalyzer will depend on RoslynAnalyzer.Core:

```
ThrowsAnalyzer/
├── ThrowsAnalyzer.csproj
│   └── <PackageReference Include="RoslynAnalyzer.Core" Version="1.0.0" />
│
├── Analyzers/
│   ├── MethodThrowsAnalyzer.cs (uses RoslynAnalyzer.Core.Members)
│   ├── UnhandledThrowsAnalyzer.cs (uses RoslynAnalyzer.Core.Analysis)
│   ├── TryCatchAnalyzer.cs
│   ├── RethrowAntiPatternAnalyzer.cs
│   ├── CatchClauseOrderingAnalyzer.cs
│   ├── AsyncSynchronousThrowAnalyzer.cs (uses RoslynAnalyzer.Core.Async)
│   ├── AsyncVoidThrowAnalyzer.cs (uses RoslynAnalyzer.Core.Async)
│   ├── UnobservedTaskExceptionAnalyzer.cs
│   ├── IteratorDeferredExceptionAnalyzer.cs (uses RoslynAnalyzer.Core.Iterators)
│   ├── IteratorTryFinallyAnalyzer.cs
│   ├── DeepExceptionPropagationAnalyzer.cs (uses RoslynAnalyzer.Core.Analysis.CallGraph)
│   ├── UndocumentedPublicExceptionAnalyzer.cs
│   └── ... (all other analyzers)
│
├── CodeFixes/
│   └── ... (all code fix providers)
│
├── TypeAnalysis/
│   ├── ExceptionTypeAnalyzer.cs (uses RoslynAnalyzer.Core.TypeAnalysis)
│   ├── CatchClauseAnalyzer.cs (uses RoslynAnalyzer.Core.TypeAnalysis)
│   └── Models/
│       └── ... (exception-specific models)
│
├── Detectors/
│   └── ... (exception-specific detectors)
│
├── Analysis/
│   ├── ExceptionFlowInfo.cs (exception-specific flow analysis)
│   ├── AsyncExceptionAnalyzer.cs (exception + async)
│   └── IteratorExceptionAnalyzer.cs (exception + iterators)
│
└── Diagnostics/
    └── ... (diagnostic builders)
```

---

## 3. Migration Strategy

### 3.1 Phase 1: Foundation (Week 1-2)

**Goal:** Establish core infrastructure library with minimal dependencies.

**Steps:**

1. **Create RoslynAnalyzer.Core Project**
   ```bash
   dotnet new classlib -f netstandard2.0 -n RoslynAnalyzer.Core
   cd RoslynAnalyzer.Core
   dotnet add package Microsoft.CodeAnalysis.CSharp --version 4.12.0
   dotnet add package Microsoft.CodeAnalysis.CSharp.Workspaces --version 4.12.0
   ```

2. **Copy and Adapt: Executable Member Detection System**
   - Copy `IExecutableMemberDetector.cs`
   - Copy `ExecutableMemberHelper.cs`
   - Copy all 10 member detectors from `Core/MemberDetectors/`
   - Update namespaces from `ThrowsAnalyzer.Core` to `RoslynAnalyzer.Core.Members`
   - Add XML documentation
   - Create unit tests

3. **Copy and Adapt: Basic Helpers**
   - Copy `AnalyzerHelper.cs` → `DiagnosticHelpers.cs`
   - Extract generic methods to `SyntaxHelpers.cs`
   - Update namespaces
   - Add comprehensive XML documentation

4. **Validation**
   - Build RoslynAnalyzer.Core
   - Run unit tests
   - Verify no breaking changes

### 3.2 Phase 2: Call Graph Infrastructure (Week 2-3)

**Goal:** Extract and generalize call graph analysis.

**Steps:**

1. **Copy Core Call Graph Components**
   - Copy `CallGraph.cs`
   - Copy `CallGraphNode.cs`
   - Copy `CallGraphEdge.cs`
   - Update namespace to `RoslynAnalyzer.Core.Analysis.CallGraph`

2. **Copy and Refactor Call Graph Builder**
   - Copy `CallGraphBuilder.cs`
   - Remove exception-specific coupling
   - Make extension points for custom analysis
   - Add interface `ICallGraphBuilder` for extensibility

3. **Create Generic Flow Analysis Pattern**
   - Design `IFlowInfo` interface
   - Design `IFlowAnalyzer<T>` interface
   - Create `FlowAnalyzerBase<T>` abstract class
   - Document flow analysis patterns

4. **Validation**
   - Build and test
   - Create example flow analyzer
   - Performance benchmark (ensure no regression)

### 3.3 Phase 3: Type Analysis (Week 3-4)

**Goal:** Extract reusable type analysis utilities.

**Steps:**

1. **Extract Generic Type Hierarchy Methods**
   - Create `TypeHierarchyAnalyzer.cs`
   - Extract `IsAssignableTo()` → make generic
   - Extract `GetExceptionHierarchy()` → `GetTypeHierarchy()`
   - Extract interface implementation checking
   - Create `TypeSymbolExtensions.cs`

2. **Create Generic Interface Analyzer**
   - Extract interface checking logic
   - Create `InterfaceAnalyzer.cs`
   - Methods: `ImplementsInterface()`, `ImplementsGenericInterface()`

3. **Keep Exception-Specific Components in ThrowsAnalyzer**
   - Keep `ExceptionTypeAnalyzer` exception-specific methods
   - Keep `CatchClauseAnalyzer` as-is
   - But make them use the new generic type infrastructure

4. **Validation**
   - Unit tests for type hierarchy operations
   - Integration tests with ThrowsAnalyzer

### 3.4 Phase 4: Async and Iterator Patterns (Week 4-5)

**Goal:** Extract pattern detection for async and iterator methods.

**Steps:**

1. **Extract Async Pattern Detection**
   - Copy `AsyncMethodDetector.cs` to `RoslynAnalyzer.Core.Analysis.Patterns.Async`
   - Copy `AsyncMethodInfo.cs`
   - Verify no exception-specific coupling
   - Add more helper methods if needed

2. **Extract Iterator Pattern Detection**
   - Copy `IteratorMethodDetector.cs` to `RoslynAnalyzer.Core.Analysis.Patterns.Iterators`
   - Copy `IteratorMethodInfo.cs`
   - Verify no exception-specific coupling

3. **Create Pattern Analyzer Base Classes**
   - Create `IMethodPatternAnalyzer` interface
   - Create `MethodPatternAnalyzerBase` abstract class
   - Provide extensibility for custom patterns

4. **Validation**
   - Unit tests for async detection
   - Unit tests for iterator detection
   - Integration with ThrowsAnalyzer

### 3.5 Phase 5: Configuration and Suppression (Week 5-6)

**Goal:** Extract configuration and suppression infrastructure.

**Steps:**

1. **Extract Configuration Infrastructure**
   - Copy `AnalyzerOptionsReader.cs`
   - Copy `AnalyzerConfiguration.cs`
   - Make prefix configurable (not hardcoded to "throws_analyzer")
   - Create configuration builder pattern

2. **Generalize Suppression Helper**
   - Copy `SuppressionHelper.cs`
   - Make attribute name configurable
   - Create `ISuppressionAttribute` interface
   - Create `SuppressionConfiguration` class
   - Support multiple suppression attributes

3. **Documentation**
   - Document .editorconfig integration
   - Document suppression patterns
   - Provide configuration examples

4. **Validation**
   - Test with different attribute names
   - Test hierarchical suppression
   - Integration tests

### 3.6 Phase 6: Performance Optimization (Week 6)

**Goal:** Extract caching and performance utilities.

**Steps:**

1. **Generalize Type Symbol Cache**
   - Create `TypeSymbolCache<TMetadata>`
   - Create `CompilationCache<TKey, TValue>`
   - Extract caching strategies
   - Create `CacheStatistics` generic class

2. **Create Cache Interfaces**
   - `ICacheStrategy<TKey, TValue>`
   - `ICompilationScopedCache`
   - Enable custom caching strategies

3. **Keep Exception-Specific Cache**
   - Keep `ExceptionTypeCache` in ThrowsAnalyzer
   - Make it use generic `TypeSymbolCache<T>`

4. **Performance Testing**
   - Benchmark caching effectiveness
   - Memory profiling
   - Performance comparison before/after

### 3.7 Phase 7: Integration and Migration (Week 7)

**Goal:** Update ThrowsAnalyzer to use RoslynAnalyzer.Core.

**Steps:**

1. **Update ThrowsAnalyzer.csproj**
   ```xml
   <ItemGroup>
     <PackageReference Include="RoslynAnalyzer.Core" Version="1.0.0" />
   </ItemGroup>
   ```

2. **Update Using Statements**
   - Find/replace namespaces:
     - `ThrowsAnalyzer.Core` → `RoslynAnalyzer.Core.Members`
     - `ThrowsAnalyzer.Analysis.CallGraph` → `RoslynAnalyzer.Core.Analysis.CallGraph`
     - etc.

3. **Remove Duplicated Files**
   - Delete extracted files from ThrowsAnalyzer
   - Keep only exception-specific components

4. **Update Imports**
   - Update all analyzer files
   - Update all code fix providers
   - Update all tests

5. **Validation**
   - Run full test suite (269 tests)
   - Verify all tests pass
   - Integration testing
   - Build NuGet packages

### 3.8 Phase 8: Documentation and Publishing (Week 8)

**Goal:** Document library and publish to NuGet.

**Steps:**

1. **Create Comprehensive Documentation**
   - README.md with examples
   - API documentation (XML comments → docs)
   - Usage guides for each component
   - Migration guide for analyzer developers
   - Best practices guide

2. **Create Example Projects**
   - Simple analyzer using RoslynAnalyzer.Core
   - Advanced analyzer with call graph
   - Configuration examples

3. **NuGet Package Configuration**
   - Configure RoslynAnalyzer.Core.csproj for packaging
   - Set appropriate metadata
   - Create package icon
   - Write package description

4. **Publishing**
   - Publish to NuGet.org
   - Create GitHub release
   - Announce in Roslyn community

---

## 4. API Design Principles

### 4.1 Core Design Guidelines

1. **Backward Compatibility**
   - Follow semantic versioning strictly
   - Mark deprecated APIs clearly
   - Provide migration paths

2. **Performance First**
   - Minimize allocations
   - Use `IEnumerable` for lazy evaluation
   - Provide cancellation token support
   - Cache expensive operations

3. **Extensibility**
   - Use interfaces for key abstractions
   - Provide virtual methods in base classes
   - Support custom implementations
   - Don't seal classes unless necessary

4. **Thread Safety**
   - Document thread safety guarantees
   - Use immutable data structures where possible
   - Use concurrent collections for caches
   - Avoid shared mutable state

5. **Roslyn Best Practices**
   - Follow Roslyn analyzer guidelines
   - Use `SymbolEqualityComparer` for symbol comparisons
   - Register syntax node actions efficiently
   - Handle partial classes correctly

### 4.2 Naming Conventions

- **Analyzers:** `*Analyzer` (e.g., `CallGraphBuilder`, `TypeHierarchyAnalyzer`)
- **Detectors:** `*Detector` (e.g., `AsyncMethodDetector`, `IExecutableMemberDetector`)
- **Info Classes:** `*Info` (e.g., `AsyncMethodInfo`, `CallGraphNode`)
- **Helpers:** `*Helper` or `*Helpers` (e.g., `DiagnosticHelpers`)
- **Extensions:** `*Extensions` (e.g., `SyntaxNodeExtensions`)

### 4.3 Documentation Standards

All public APIs must have:
1. XML summary describing purpose
2. Parameter descriptions
3. Return value description
4. Example usage (for complex APIs)
5. Remarks for important behavior
6. Thread safety notes (if applicable)

Example:
```csharp
/// <summary>
/// Builds a call graph for all methods in the compilation.
/// </summary>
/// <param name="compilation">The compilation to analyze.</param>
/// <param name="cancellationToken">Cancellation token for long-running operations.</param>
/// <returns>
/// A <see cref="CallGraph"/> containing all method invocation relationships.
/// </returns>
/// <remarks>
/// This method analyzes all syntax trees in the compilation and builds a directed graph
/// of method calls. The operation can be expensive for large compilations.
/// Use <paramref name="cancellationToken"/> to support cancellation.
/// </remarks>
/// <example>
/// <code>
/// var builder = new CallGraphBuilder(compilation);
/// var graph = await builder.BuildAsync(cancellationToken);
/// var node = graph.GetOrAddNode(methodSymbol);
/// </code>
/// </example>
public async Task<CallGraph> BuildAsync(
    Compilation compilation,
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

---

## 5. Testing Strategy

### 5.1 Unit Test Coverage

**Target:** 90%+ code coverage for RoslynAnalyzer.Core

**Test Categories:**

1. **Member Detection Tests**
   - Each detector: positive and negative cases
   - Edge cases: expression bodies, local functions, nested lambdas
   - Display name generation

2. **Call Graph Tests**
   - Graph construction
   - Transitive closure
   - Cycle detection
   - Depth limiting
   - Performance tests (large graphs)

3. **Type Analysis Tests**
   - Type hierarchy walking
   - Interface implementation checking
   - Generic type handling
   - Nullability

4. **Async Pattern Tests**
   - Async method detection
   - Task return type checking
   - Async void detection
   - Await expression finding

5. **Iterator Pattern Tests**
   - Iterator detection
   - Yield statement finding
   - Enumerable return checking

6. **Configuration Tests**
   - Option reading
   - Default values
   - Hierarchical configuration
   - Suppression checking

7. **Caching Tests**
   - Cache hit/miss
   - Cache invalidation
   - Concurrent access
   - Memory leaks

### 5.2 Integration Tests

**Goal:** Verify ThrowsAnalyzer works correctly with RoslynAnalyzer.Core.

1. **Regression Tests**
   - All 269 existing tests must pass
   - No performance regression
   - No diagnostic changes

2. **End-to-End Tests**
   - Build ThrowsAnalyzer with new dependency
   - Run on sample projects
   - Verify all diagnostics work
   - Verify all code fixes work

### 5.3 Performance Tests

**Benchmarks to track:**

1. Call graph construction time
2. Member detection performance
3. Type analysis speed
4. Cache effectiveness
5. Memory allocation

**Tools:**
- BenchmarkDotNet
- Memory profilers (dotMemory, PerfView)
- Roslyn performance analyzer

### 5.4 Test Project Structure

```
RoslynAnalyzer.Core.Tests/
├── Members/
│   ├── ExecutableMemberHelperTests.cs
│   ├── MethodMemberDetectorTests.cs
│   ├── LambdaMemberDetectorTests.cs
│   └── ...
│
├── Analysis/
│   ├── CallGraph/
│   │   ├── CallGraphTests.cs
│   │   ├── CallGraphBuilderTests.cs
│   │   └── CallGraphPerformanceTests.cs
│   │
│   └── Patterns/
│       ├── AsyncMethodDetectorTests.cs
│       └── IteratorMethodDetectorTests.cs
│
├── TypeAnalysis/
│   ├── TypeHierarchyAnalyzerTests.cs
│   └── InterfaceAnalyzerTests.cs
│
├── Configuration/
│   ├── AnalyzerOptionsReaderTests.cs
│   └── SuppressionHelperTests.cs
│
├── Performance/
│   ├── CachingTests.cs
│   └── BenchmarkTests.cs
│
└── Helpers/
    └── TestHelper.cs (shared test utilities)
```

---

## 6. Risk Assessment and Mitigation

### 6.1 Technical Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Breaking API Changes** | High | Medium | Comprehensive test suite, semantic versioning, beta releases |
| **Performance Regression** | High | Low | Continuous benchmarking, performance tests in CI |
| **Increased Complexity** | Medium | Medium | Clear documentation, examples, simple defaults |
| **Version Conflicts** | Medium | Low | Pin Roslyn version, test against multiple versions |
| **Memory Leaks** | High | Low | Memory profiling, static analysis, careful cache design |
| **Thread Safety Issues** | High | Low | Use immutable structures, document thread safety, tests |

### 6.2 Process Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Scope Creep** | Medium | Medium | Phased approach, clear boundaries, MVP mindset |
| **Migration Effort** | Medium | High | Automated refactoring, comprehensive tests, staged rollout |
| **Adoption Resistance** | Low | Low | Good documentation, clear benefits, easy migration |

### 6.3 Mitigation Strategy Details

1. **Breaking Changes**
   - Maintain compatibility layer during transition
   - Create migration tool/script
   - Document all breaking changes
   - Support older versions temporarily

2. **Performance**
   - Benchmark every phase
   - Compare before/after metrics
   - Optimize hot paths
   - Profile in real-world scenarios

3. **Quality Assurance**
   - Code review for all changes
   - Automated testing in CI/CD
   - Static analysis tools
   - Community feedback (beta program)

---

## 7. Success Metrics

### 7.1 Quantitative Metrics

1. **Code Reusability**
   - Target: 50-60% of ThrowsAnalyzer code extracted
   - Measure: Lines of code in RoslynAnalyzer.Core vs ThrowsAnalyzer

2. **Test Coverage**
   - Target: 90%+ for RoslynAnalyzer.Core
   - Measure: Code coverage reports

3. **Performance**
   - Target: No regression (within 5% of current performance)
   - Measure: Benchmark suite results

4. **Documentation**
   - Target: 100% public API documentation
   - Measure: XML doc coverage, doc generation

5. **Adoption**
   - Target: 5+ external projects using RoslynAnalyzer.Core within 6 months
   - Measure: NuGet download stats, GitHub dependents

### 7.2 Qualitative Metrics

1. **Developer Experience**
   - Easy to understand API
   - Clear documentation with examples
   - Helpful error messages
   - Positive community feedback

2. **Maintainability**
   - Clean separation of concerns
   - Well-organized codebase
   - Consistent patterns
   - Easy to extend

3. **Reliability**
   - Stable API
   - No critical bugs
   - Backward compatibility maintained
   - Good test coverage

---

## 8. Long-Term Vision

### 8.1 Future Enhancements (Post v1.0)

1. **Additional Analyzers** (v1.1+)
   - Control flow analysis
   - Data flow analysis
   - Taint analysis infrastructure
   - Null reference analysis helpers

2. **Code Generation Support** (v1.2+)
   - Source generator base classes
   - Syntax factory helpers
   - Code generation patterns

3. **Advanced Caching** (v1.3+)
   - Cross-compilation caching
   - Persistent caching
   - Distributed caching for CI

4. **Diagnostic Helpers** (v1.4+)
   - Diagnostic descriptor builders
   - Severity configuration
   - Diagnostic formatting utilities

5. **Fix Provider Infrastructure** (v1.5+)
   - Code fix provider base classes
   - Fix all provider helpers
   - Preview generation utilities

### 8.2 Community Building

1. **Open Source Strategy**
   - MIT license (permissive)
   - Accept community contributions
   - Clear contribution guidelines
   - Active issue triage

2. **Documentation Site**
   - API reference (auto-generated)
   - Tutorials and guides
   - Real-world examples
   - Best practices

3. **Community Engagement**
   - Blog posts about patterns
   - Conference talks
   - YouTube tutorials
   - Discord/Slack channel

4. **Ecosystem Growth**
   - Encourage other analyzer authors
   - Showcase projects using the library
   - Collaborate with Roslyn team
   - Integration with popular tools

---

## 9. Detailed Implementation Examples

### 9.1 Example: Using Call Graph in a New Analyzer

```csharp
using RoslynAnalyzer.Core.Analysis.CallGraph;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RecursionDepthAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "RECUR001",
        "Deep recursion detected",
        "Method '{0}' has recursion depth of {1}",
        "Performance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        // Build call graph for entire compilation
        var builder = new CallGraphBuilder(
            context.Compilation,
            context.CancellationToken);

        var graph = builder.BuildAsync().GetAwaiter().GetResult();

        // Analyze each method
        foreach (var node in graph.Nodes)
        {
            var depth = node.GetDepth();
            if (depth > 10) // configurable threshold
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    node.Method.Locations[0],
                    node.Method.Name,
                    depth);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
```

### 9.2 Example: Using Member Detection

```csharp
using RoslynAnalyzer.Core.Members;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EmptyMemberAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "EMPTY001",
        "Empty member detected",
        "{0} is empty",
        "Design",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register for all executable member types using configuration
        context.RegisterSyntaxNodeAction(
            AnalyzeMember,
            RoslynAnalyzer.Core.Configuration.AnalyzerConfiguration
                .ExecutableMemberSyntaxKinds);
    }

    private void AnalyzeMember(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;

        // Check if this is an executable member
        if (!ExecutableMemberHelper.IsExecutableMember(node))
            return;

        // Get all executable blocks
        var blocks = ExecutableMemberHelper.GetExecutableBlocks(node);

        // Check if all blocks are empty
        bool isEmpty = true;
        foreach (var block in blocks)
        {
            if (block.DescendantNodes().Any())
            {
                isEmpty = false;
                break;
            }
        }

        if (isEmpty)
        {
            var displayName = ExecutableMemberHelper
                .GetMemberDisplayName(node);

            var diagnostic = Diagnostic.Create(
                Rule,
                DiagnosticHelpers.GetMemberLocation(node),
                displayName);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

### 9.3 Example: Using Async Pattern Detection

```csharp
using RoslynAnalyzer.Core.Analysis.Patterns.Async;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnawaitedAsyncAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "ASYNC001",
        "Async method not awaited",
        "Async method call is not awaited",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(
            AnalyzeMethod,
            SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDecl = (MethodDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        var methodBody = AsyncMethodDetector.GetMethodBody(methodDecl);
        if (methodBody == null)
            return;

        // Find all unawaited task invocations
        var unawaitedInvocations = AsyncMethodDetector
            .GetUnawaitedTaskInvocations(methodBody, semanticModel);

        foreach (var invocation in unawaitedInvocations)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                invocation.GetLocation());

            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

### 9.4 Example: Using Configuration

```csharp
using RoslynAnalyzer.Core.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConfigurableAnalyzer : DiagnosticAnalyzer
{
    // Analyzer implementation...

    private void AnalyzeMember(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;
        var options = context.Options;
        var tree = node.SyntaxTree;

        // Check if this analyzer is enabled via .editorconfig
        if (!AnalyzerOptionsReader.IsAnalyzerEnabled(
            options, tree, "configurable_analyzer"))
        {
            return;
        }

        // Check if this member type should be analyzed
        var memberTypeKey = AnalyzerOptionsReader
            .GetMemberTypeKey(node.Kind());

        if (!AnalyzerOptionsReader.IsMemberTypeEnabled(
            options, tree, memberTypeKey))
        {
            return;
        }

        // Check if suppressed via attribute
        if (SuppressionHelper.IsSuppressed(
            context.SemanticModel,
            node,
            "CONFIG001"))
        {
            return;
        }

        // Perform analysis...
    }
}
```

**Corresponding .editorconfig:**
```ini
# Enable the analyzer
configurable_analyzer_enable = true

# Disable analysis for lambdas
configurable_analyzer_analyze_lambdas = false

# Enable analysis for methods
configurable_analyzer_analyze_methods = true
```

---

## 10. Estimated Effort and Timeline

### 10.1 Effort Breakdown (Person-Days)

| Phase | Tasks | Estimated Days |
|-------|-------|----------------|
| **Phase 1: Foundation** | Project setup, member detection, helpers | 8-10 days |
| **Phase 2: Call Graph** | Call graph extraction, testing | 7-9 days |
| **Phase 3: Type Analysis** | Type hierarchy, interface analysis | 5-7 days |
| **Phase 4: Patterns** | Async and iterator detection | 5-6 days |
| **Phase 5: Configuration** | Options and suppression | 5-7 days |
| **Phase 6: Performance** | Caching and optimization | 4-5 days |
| **Phase 7: Integration** | ThrowsAnalyzer migration | 6-8 days |
| **Phase 8: Documentation** | Docs, examples, publishing | 6-8 days |
| **Testing & QA** | Ongoing throughout | 10-12 days |
| **Buffer** | Unexpected issues | 8-10 days |
| **Total** | | **64-82 days** |

### 10.2 Timeline (Calendar Weeks)

**With 1 full-time developer:** ~13-17 weeks (3-4 months)
**With 0.5 FTE (part-time):** ~26-34 weeks (6-8 months)

### 10.3 Milestones

| Milestone | Deliverable | Target Date |
|-----------|------------|-------------|
| **M1: Core Infrastructure** | RoslynAnalyzer.Core v0.1 (member detection) | End of Week 2 |
| **M2: Call Graph** | Call graph analysis complete | End of Week 4 |
| **M3: Type & Pattern Analysis** | Type, async, iterator analysis | End of Week 7 |
| **M4: Configuration** | Full configuration support | End of Week 9 |
| **M5: Integration** | ThrowsAnalyzer using RoslynAnalyzer.Core | End of Week 11 |
| **M6: Beta Release** | RoslynAnalyzer.Core v0.9 on NuGet | End of Week 13 |
| **M7: Stable Release** | RoslynAnalyzer.Core v1.0 | End of Week 17 |

---

## 11. Conclusion and Next Steps

### 11.1 Summary

This refactoring plan outlines the extraction of ~50-60% of ThrowsAnalyzer's infrastructure into a reusable library (RoslynAnalyzer.Core). The library will provide:

1. **Call Graph Analysis** - Build and analyze method invocation graphs
2. **Executable Member Detection** - Unified handling of all C# executable contexts
3. **Type Analysis** - Type hierarchy and interface checking utilities
4. **Async/Iterator Pattern Detection** - Specialized pattern analyzers
5. **Configuration Infrastructure** - .editorconfig integration and suppression
6. **Performance Optimizations** - Caching and optimization utilities

**Key Benefits:**
- Accelerates development of new analyzers
- Reduces boilerplate code
- Shares battle-tested infrastructure
- Improves consistency across analyzers
- Enables community contributions

### 11.2 Immediate Next Steps

1. **Get Approval**
   - Review this plan with stakeholders
   - Validate technical approach
   - Confirm timeline and resources

2. **Set Up Infrastructure**
   - Create GitHub repository for RoslynAnalyzer.Core
   - Set up CI/CD pipeline
   - Configure NuGet package publishing

3. **Start Phase 1**
   - Create RoslynAnalyzer.Core project
   - Extract member detection system
   - Write initial tests and documentation

4. **Communication**
   - Announce the project to Roslyn community
   - Create discussion forum/channel
   - Invite early feedback

### 11.3 Success Criteria

The refactoring will be considered successful when:

1. ✅ RoslynAnalyzer.Core v1.0 published to NuGet
2. ✅ ThrowsAnalyzer successfully migrated with no regressions
3. ✅ All 269 tests passing
4. ✅ 90%+ code coverage for new library
5. ✅ Comprehensive documentation available
6. ✅ At least 2 example analyzers created
7. ✅ No performance regression (within 5%)
8. ✅ Positive community feedback

### 11.4 Contact and Support

**Project Lead:** [Your Name]
**Repository:** https://github.com/wieslawsoltes/ThrowsAnalyzer
**Future Core Library:** https://github.com/[org]/RoslynAnalyzer.Core
**Discussion:** GitHub Discussions / Discord / Slack

---

## Appendix A: File Extraction Checklist

### Files to Extract (46 files total)

#### Call Graph & Flow Analysis (3 files)
- [ ] `Analysis/CallGraph.cs` → `RoslynAnalyzer.Core/Analysis/CallGraph/CallGraph.cs`
- [ ] `Analysis/CallGraphBuilder.cs` → `RoslynAnalyzer.Core/Analysis/CallGraph/CallGraphBuilder.cs`
- [ ] `Analysis/ExceptionFlowInfo.cs` → Keep in ThrowsAnalyzer (exception-specific)

#### Member Detection (12 files)
- [ ] `Core/IExecutableMemberDetector.cs` → `RoslynAnalyzer.Core/Members/IExecutableMemberDetector.cs`
- [ ] `Core/ExecutableMemberHelper.cs` → `RoslynAnalyzer.Core/Members/ExecutableMemberHelper.cs`
- [ ] `Core/MemberDetectors/MethodMemberDetector.cs` → `RoslynAnalyzer.Core/Members/Detectors/`
- [ ] `Core/MemberDetectors/ConstructorMemberDetector.cs` → `RoslynAnalyzer.Core/Members/Detectors/`
- [ ] `Core/MemberDetectors/DestructorMemberDetector.cs` → `RoslynAnalyzer.Core/Members/Detectors/`
- [ ] `Core/MemberDetectors/OperatorMemberDetector.cs` → `RoslynAnalyzer.Core/Members/Detectors/`
- [ ] `Core/MemberDetectors/ConversionOperatorMemberDetector.cs` → `RoslynAnalyzer.Core/Members/Detectors/`
- [ ] `Core/MemberDetectors/PropertyMemberDetector.cs` → `RoslynAnalyzer.Core/Members/Detectors/`
- [ ] `Core/MemberDetectors/AccessorMemberDetector.cs` → `RoslynAnalyzer.Core/Members/Detectors/`
- [ ] `Core/MemberDetectors/LocalFunctionMemberDetector.cs` → `RoslynAnalyzer.Core/Members/Detectors/`
- [ ] `Core/MemberDetectors/LambdaMemberDetector.cs` → `RoslynAnalyzer.Core/Members/Detectors/`
- [ ] `Core/MemberDetectors/AnonymousMethodMemberDetector.cs` → `RoslynAnalyzer.Core/Members/Detectors/`

#### Type Analysis (6 files)
- [ ] `TypeAnalysis/ExceptionTypeAnalyzer.cs` → Split: generic methods to Core, keep exception-specific
- [ ] `TypeAnalysis/CatchClauseAnalyzer.cs` → Keep in ThrowsAnalyzer (exception-specific)
- [ ] `TypeAnalysis/Models/TypedThrowInfo.cs` → Keep in ThrowsAnalyzer
- [ ] `TypeAnalysis/Models/CatchClauseInfo.cs` → Keep in ThrowsAnalyzer
- [ ] `TypeAnalysis/Models/CatchClauseOrderingIssue.cs` → Keep in ThrowsAnalyzer
- [ ] Create new: `RoslynAnalyzer.Core/TypeAnalysis/TypeHierarchyAnalyzer.cs`

#### Async Pattern Detection (3 files)
- [ ] `Analysis/AsyncMethodDetector.cs` → `RoslynAnalyzer.Core/Analysis/Patterns/Async/AsyncMethodDetector.cs`
- [ ] `Analysis/AsyncExceptionAnalyzer.cs` → Keep in ThrowsAnalyzer (exception-specific)
- [ ] Create new: `RoslynAnalyzer.Core/Analysis/Patterns/Async/AsyncMethodInfo.cs`

#### Iterator Pattern Detection (3 files)
- [ ] `Analysis/IteratorMethodDetector.cs` → `RoslynAnalyzer.Core/Analysis/Patterns/Iterators/IteratorMethodDetector.cs`
- [ ] `Analysis/IteratorExceptionAnalyzer.cs` → Keep in ThrowsAnalyzer (exception-specific)
- [ ] Create new: `RoslynAnalyzer.Core/Analysis/Patterns/Iterators/IteratorMethodInfo.cs`

#### Configuration (4 files)
- [ ] `Configuration/AnalyzerOptionsReader.cs` → `RoslynAnalyzer.Core/Configuration/Options/AnalyzerOptionsReader.cs`
- [ ] `Analyzers/AnalyzerConfiguration.cs` → `RoslynAnalyzer.Core/Configuration/Options/AnalyzerConfiguration.cs`
- [ ] `Configuration/SuppressionHelper.cs` → `RoslynAnalyzer.Core/Configuration/Suppression/SuppressionHelper.cs`
- [ ] `Attributes/SuppressThrowsAnalysisAttribute.cs` → Keep in ThrowsAnalyzer

#### Performance (2 files)
- [ ] `Performance/ExceptionTypeCache.cs` → Split: generic cache to Core, keep exception-specific
- [ ] Create new: `RoslynAnalyzer.Core/Performance/Caching/TypeSymbolCache.cs`

#### Helpers (3 files)
- [ ] `Analyzers/AnalyzerHelper.cs` → `RoslynAnalyzer.Core/Helpers/DiagnosticHelpers.cs`
- [ ] `Detectors/ThrowStatementDetector.cs` → Keep pattern, create generic in Core
- [ ] Create new: `RoslynAnalyzer.Core/Helpers/SyntaxHelpers.cs`

---

## Appendix B: API Surface Preview

### RoslynAnalyzer.Core Public API (Major Classes and Methods)

```csharp
// Members
namespace RoslynAnalyzer.Core.Members
{
    public interface IExecutableMemberDetector { }
    public static class ExecutableMemberHelper { }
    // + 10 detector implementations
}

// Call Graph
namespace RoslynAnalyzer.Core.Analysis.CallGraph
{
    public class CallGraph { }
    public class CallGraphNode { }
    public class CallGraphEdge { }
    public class CallGraphBuilder
    {
        Task<CallGraph> BuildAsync();
        Task<CallGraph> BuildForMethodAsync(IMethodSymbol method);
        static IEnumerable<IMethodSymbol> GetTransitiveCallees(CallGraph, IMethodSymbol, int maxDepth);
        static IEnumerable<IMethodSymbol> GetTransitiveCallers(CallGraph, IMethodSymbol, int maxDepth);
    }
}

// Type Analysis
namespace RoslynAnalyzer.Core.TypeAnalysis
{
    public static class TypeHierarchyAnalyzer
    {
        bool IsAssignableTo(ITypeSymbol derived, ITypeSymbol base, Compilation);
        IEnumerable<ITypeSymbol> GetTypeHierarchy(ITypeSymbol type);
        bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol interface);
    }
}

// Async Patterns
namespace RoslynAnalyzer.Core.Analysis.Patterns.Async
{
    public static class AsyncMethodDetector
    {
        bool IsAsyncMethod(IMethodSymbol method);
        bool ReturnsTask(IMethodSymbol method, Compilation);
        bool IsAsyncVoid(IMethodSymbol method, Compilation);
        AwaitExpressionSyntax GetFirstAwaitExpression(SyntaxNode body);
        IEnumerable<AwaitExpressionSyntax> GetAllAwaitExpressions(SyntaxNode body);
        bool IsThrowBeforeFirstAwait(SyntaxNode throw, SyntaxNode body);
        IEnumerable<InvocationExpressionSyntax> GetUnawaitedTaskInvocations(SyntaxNode body, SemanticModel);
        AsyncMethodInfo GetAsyncMethodInfo(IMethodSymbol, SyntaxNode, SemanticModel);
    }

    public class AsyncMethodInfo { }
}

// Iterator Patterns
namespace RoslynAnalyzer.Core.Analysis.Patterns.Iterators
{
    public static class IteratorMethodDetector
    {
        bool IsIteratorMethod(IMethodSymbol method, SyntaxNode node);
        bool ReturnsEnumerable(IMethodSymbol method, Compilation);
        IEnumerable<YieldStatementSyntax> GetYieldReturnStatements(SyntaxNode body);
        bool IsThrowBeforeFirstYield(SyntaxNode throw, SyntaxNode body);
        IteratorMethodInfo GetIteratorMethodInfo(IMethodSymbol, SyntaxNode, Compilation);
    }

    public class IteratorMethodInfo { }
}

// Configuration
namespace RoslynAnalyzer.Core.Configuration
{
    public static class AnalyzerOptionsReader
    {
        bool IsAnalyzerEnabled(AnalyzerOptions, SyntaxTree, string analyzerName);
        bool IsMemberTypeEnabled(AnalyzerOptions, SyntaxTree, string memberType);
        HashSet<string> GetEnabledMemberTypes(AnalyzerOptions, SyntaxTree);
    }

    public static class AnalyzerConfiguration
    {
        static readonly SyntaxKind[] ExecutableMemberSyntaxKinds;
    }

    public static class SuppressionHelper
    {
        bool IsSuppressed(SemanticModel, SyntaxNode, string diagnosticId);
    }
}

// Performance
namespace RoslynAnalyzer.Core.Performance.Caching
{
    public static class TypeSymbolCache<TMetadata>
    {
        TValue GetOrAdd<TValue>(string key, Func<string, TValue> factory);
        void Clear();
        CacheStatistics GetStatistics();
    }
}

// Helpers
namespace RoslynAnalyzer.Core.Helpers
{
    public static class DiagnosticHelpers
    {
        Location GetMemberLocation(SyntaxNode node);
    }

    public static class SyntaxHelpers
    {
        // Common syntax operations
    }
}
```

---

**Document Version:** 1.0
**Last Updated:** 2025-01-27
**Author:** Claude (Anthropic AI)
**Status:** Draft for Review
