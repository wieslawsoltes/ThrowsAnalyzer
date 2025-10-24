# Generic Member Support Implementation

## Overview

Implemented extensible architecture for detecting throw statements and try/catch blocks across **all executable member types** in C#, not just methods.

## Architecture Components

### 1. IExecutableMemberDetector Interface
**Location:** `src/ThrowsAnalyzer/Core/IExecutableMemberDetector.cs`

Core abstraction for detecting executable members:

```csharp
public interface IExecutableMemberDetector
{
    bool SupportsNode(SyntaxNode node);
    IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node);
    string GetMemberDisplayName(SyntaxNode node);
}
```

### 2. Member-Specific Detectors
**Location:** `src/ThrowsAnalyzer/Core/MemberDetectors/`

Implemented 9 specialized detectors:

1. **MethodMemberDetector** - Regular methods
   - Supports: `MethodDeclarationSyntax`
   - Handles: Block bodies and expression bodies

2. **ConstructorMemberDetector** - Constructors
   - Supports: `ConstructorDeclarationSyntax`
   - Handles: Block bodies and expression bodies

3. **DestructorMemberDetector** - Finalizers/Destructors
   - Supports: `DestructorDeclarationSyntax`
   - Handles: Block bodies and expression bodies
   - Note: Throwing in finalizers is dangerous

4. **OperatorMemberDetector** - Operator overloads
   - Supports: `OperatorDeclarationSyntax`
   - Handles: Block bodies and expression bodies

5. **ConversionOperatorMemberDetector** - Conversion operators
   - Supports: `ConversionOperatorDeclarationSyntax`
   - Handles: Both implicit and explicit conversions

6. **AccessorMemberDetector** - Property/Indexer/Event accessors
   - Supports: `AccessorDeclarationSyntax`
   - Handles: get, set, init, add, remove accessors
   - Smart naming: "Property 'Name' getter", "Indexer setter", etc.

7. **LocalFunctionMemberDetector** - Local functions
   - Supports: `LocalFunctionStatementSyntax`
   - Handles: Block bodies and expression bodies

8. **LambdaMemberDetector** - Lambda expressions
   - Supports: `SimpleLambdaExpressionSyntax`, `ParenthesizedLambdaExpressionSyntax`
   - Handles: Block lambdas and expression lambdas

9. **AnonymousMethodMemberDetector** - Anonymous methods
   - Supports: `AnonymousMethodExpressionSyntax`
   - Handles: Legacy `delegate { }` syntax

### 3. ExecutableMemberHelper
**Location:** `src/ThrowsAnalyzer/Core/ExecutableMemberHelper.cs`

Central utility managing all detectors:

```csharp
public static class ExecutableMemberHelper
{
    public static bool IsExecutableMember(SyntaxNode node);
    public static IEnumerable<SyntaxNode> GetExecutableBlocks(SyntaxNode node);
    public static string GetMemberDisplayName(SyntaxNode node);
    public static IReadOnlyList<IExecutableMemberDetector> GetAllDetectors();
}
```

## Refactored Detectors

### ThrowStatementDetector
**Location:** `src/ThrowsAnalyzer/Detectors/ThrowStatementDetector.cs`

**Before:**
- Only supported `MethodDeclarationSyntax`
- Hardcoded body extraction

**After:**
- Supports all 9 member types
- Uses `ExecutableMemberHelper.GetExecutableBlocks()`
- Maintains backward compatibility with method-only API
- New overload: `HasThrowStatements(SyntaxNode node)`

### TryCatchDetector
**Location:** `src/ThrowsAnalyzer/Detectors/TryCatchDetector.cs`

**Before:**
- Only supported `MethodDeclarationSyntax`
- Hardcoded body extraction

**After:**
- Supports all 9 member types
- Uses `ExecutableMemberHelper.GetExecutableBlocks()`
- Maintains backward compatibility
- New overloads:
  - `HasTryCatchBlocks(SyntaxNode node)`
  - `GetTryCatchBlocks(SyntaxNode node)`

### UnhandledThrowDetector
**Location:** `src/ThrowsAnalyzer/Detectors/UnhandledThrowDetector.cs`

**Before:**
- Only supported `MethodDeclarationSyntax`
- Separate handling for body vs expression body

**After:**
- Supports all 9 member types
- Unified handling through `ExecutableMemberHelper`
- Maintains backward compatibility
- New overload: `HasUnhandledThrows(SyntaxNode node)`

## Backward Compatibility

All existing APIs maintained:
- `HasThrowStatements(MethodDeclarationSyntax)` - ✓ Works
- `HasTryCatchBlocks(MethodDeclarationSyntax)` - ✓ Works
- `GetTryCatchBlocks(MethodDeclarationSyntax)` - ✓ Works
- `HasUnhandledThrows(MethodDeclarationSyntax)` - ✓ Works

All 18 existing tests pass without modification!

## Supported Member Types

| Member Type | Syntax Node | Block Body | Expression Body | Example |
|------------|-------------|------------|-----------------|---------|
| Method | `MethodDeclarationSyntax` | ✓ | ✓ | `void Foo() { }` |
| Constructor | `ConstructorDeclarationSyntax` | ✓ | ✓ | `public C() { }` |
| Destructor | `DestructorDeclarationSyntax` | ✓ | ✓ | `~C() { }` |
| Operator | `OperatorDeclarationSyntax` | ✓ | ✓ | `operator +(A a, B b)` |
| Conversion | `ConversionOperatorDeclarationSyntax` | ✓ | ✓ | `implicit operator T(...)` |
| Property Accessor | `AccessorDeclarationSyntax` | ✓ | ✓ | `get { }` / `set => ...` |
| Indexer Accessor | `AccessorDeclarationSyntax` | ✓ | ✓ | `this[int i] { get; set; }` |
| Event Accessor | `AccessorDeclarationSyntax` | ✓ | ✓ | `add { }` / `remove { }` |
| Local Function | `LocalFunctionStatementSyntax` | ✓ | ✓ | `void Local() { }` |
| Lambda | `SimpleLambdaExpressionSyntax` | ✓ | ✓ | `x => x + 1` / `x => { }` |
| Lambda | `ParenthesizedLambdaExpressionSyntax` | ✓ | ✓ | `(x, y) => x + y` |
| Anonymous Method | `AnonymousMethodExpressionSyntax` | ✓ | ✗ | `delegate { }` |

## Benefits

### 1. Extensibility
- Add new member types by implementing `IExecutableMemberDetector`
- No changes required to existing detectors
- Plugin-style architecture

### 2. Code Reuse
- All detectors use same `ExecutableMemberHelper`
- No duplicated body extraction logic
- Consistent behavior across all member types

### 3. Future-Proof
- New analyzers can support all member types immediately
- Easy to add support for future C# syntax
- Testable in isolation

### 4. Backward Compatible
- All existing code continues to work
- No breaking changes
- Incremental adoption possible

## Usage Examples

### Example 1: Detect throws in constructor
```csharp
var constructor = GetConstructorNode();
if (ThrowStatementDetector.HasThrowStatements(constructor))
{
    // Constructor throws exceptions
}
```

### Example 2: Detect try/catch in property getter
```csharp
var propertyGetter = GetAccessorNode();
if (TryCatchDetector.HasTryCatchBlocks(propertyGetter))
{
    // Property getter has exception handling
}
```

### Example 3: Detect unhandled throws in lambda
```csharp
var lambda = GetLambdaNode();
if (UnhandledThrowDetector.HasUnhandledThrows(lambda))
{
    // Lambda has unhandled exceptions
}
```

### Example 4: Get display name for diagnostics
```csharp
var node = GetAnyExecutableMember();
var displayName = ExecutableMemberHelper.GetMemberDisplayName(node);
// Returns: "Method 'Foo'" or "Property 'Name' getter" or "Lambda expression"
```

## Testing

All 18 existing tests pass:
- 4 MethodThrowsAnalyzerTests
- 6 UnhandledThrowsAnalyzerTests
- 8 TryCatchAnalyzerTests

Tests verify backward compatibility with method-only detection.

## Next Steps

### Immediate: Extend Analyzers
Update existing analyzers to support all member types:
- `MethodThrowsAnalyzer` → Register for all executable members
- `UnhandledThrowsAnalyzer` → Register for all executable members
- `TryCatchAnalyzer` → Register for all executable members

### Short-term: Add Tests
Add test coverage for new member types:
- Constructor tests
- Property accessor tests
- Local function tests
- Lambda expression tests
- Operator tests

### Future: New Analyzers
Build new analyzers leveraging generic support:
- Detect throws in destructors (THROWS026 from analysis)
- Detect throws in property getters vs setters
- Analyze exception handling in local functions

## File Structure

```
src/ThrowsAnalyzer/
├── Core/
│   ├── IExecutableMemberDetector.cs
│   ├── ExecutableMemberHelper.cs
│   └── MemberDetectors/
│       ├── MethodMemberDetector.cs
│       ├── ConstructorMemberDetector.cs
│       ├── DestructorMemberDetector.cs
│       ├── OperatorMemberDetector.cs
│       ├── ConversionOperatorMemberDetector.cs
│       ├── AccessorMemberDetector.cs
│       ├── LocalFunctionMemberDetector.cs
│       ├── LambdaMemberDetector.cs
│       └── AnonymousMethodMemberDetector.cs
├── Detectors/
│   ├── ThrowStatementDetector.cs (refactored)
│   ├── TryCatchDetector.cs (refactored)
│   └── UnhandledThrowDetector.cs (refactored)
└── ... (existing folders)
```

## Summary

Successfully implemented Phase 2 (Extended Member Support) from the analysis document:
- ✅ Generic member detection architecture
- ✅ Support for 9 member types (methods, constructors, properties, lambdas, etc.)
- ✅ Refactored all existing detectors
- ✅ Maintained backward compatibility
- ✅ All tests passing
- ✅ Ready for analyzer extensions
