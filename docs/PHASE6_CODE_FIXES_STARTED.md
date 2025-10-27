# Phase 6: Code Fixes Implementation - Progress Report

**Date**: 2025-10-27
**Status**: In Progress
**Phase**: 6 - Code Fixes for Basic Analyzers (THROWS001-010)

## Overview

Phase 6 focuses on implementing automated code fix providers for all ThrowsAnalyzer diagnostics. This phase adds IDE integration that allows developers to automatically fix issues detected by the analyzers with a single click.

## Completed Work

### 1. Base Infrastructure ✅

**ThrowsAnalyzerCodeFixProvider** (Base Class)
- Location: `src/ThrowsAnalyzer/CodeFixes/ThrowsAnalyzerCodeFixProvider.cs`
- Purpose: Provides common functionality for all code fix providers
- Features:
  - Helper method `CreateCodeAction` for consistent code action creation
  - Support for both document-level and solution-level fixes
  - Batch fixing enabled by default via `WellKnownFixAllProviders.BatchFixer`
  - Helper method `GetDocumentAndRootAsync` for common document operations
  - Virtual `Title` property for code fix titles
  - Automatic equivalence key generation based on provider type name

### 2. Code Fix Providers ✅

The following code fix providers have been discovered/implemented in the CodeFixes directory:

#### THROWS001: Method Contains Throw Statement
**Provider**: `MethodThrowsCodeFixProvider.cs`
**Status**: Exists (implementation from previous work)

Code fixes offered:
1. Add XML exception documentation
2. Wrap in try-catch block

#### THROWS002: Unhandled Throw Statement  
**Provider**: `UnhandledThrowsCodeFixProvider.cs`
**Status**: Exists (implementation from previous work)

Code fixes offered:
1. Wrap in try-catch block
2. Move to existing try block

#### THROWS003: Method Contains Try-Catch Block
**Provider**: `TryCatchCodeFixProvider.cs`
**Status**: Exists (implementation from previous work)

Code fixes offered:
1. Remove try-catch and propagate
2. Add logging to catch blocks

#### THROWS004: Rethrow Anti-Pattern ✅ NEW
**Provider**: `RethrowAntiPatternCodeFixProvider.cs`
**Status**: Newly Implemented

Code fix offered:
- Replace `throw ex;` with `throw;` to preserve stack trace

Implementation:
```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RethrowAntiPatternCodeFixProvider))]
[Shared]
public class RethrowAntiPatternCodeFixProvider : ThrowsAnalyzerCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.DiagnosticId004);

    protected override string Title => "Replace with bare rethrow";

    // Replaces 'throw ex;' with 'throw;' using SyntaxFactory
    private async Task<Document> ReplaceWithBareRethrowAsync(...)
    {
        var newThrow = SyntaxFactory.ThrowStatement()
            .WithThrowKeyword(throwStatement.ThrowKeyword)
            .WithSemicolonToken(throwStatement.SemicolonToken)
            .WithTriviaFrom(throwStatement);
        // ...
    }
}
```

#### THROWS007: Unreachable Catch Clause
**Provider**: `CatchClauseOrderingCodeFixProvider.cs`
**Status**: Exists (implementation from previous work)

Code fix offered:
- Reorder catch clauses from specific to general

#### THROWS008: Empty Catch Block
**Provider**: `EmptyCatchCodeFixProvider.cs`
**Status**: Exists (implementation from previous work)

Code fixes offered:
1. Remove empty catch
2. Add logging
3. Add TODO comment

#### THROWS009: Catch Block Only Rethrows
**Provider**: `RethrowOnlyCatchCodeFixProvider.cs`
**Status**: Exists (implementation from previous work)

Code fix offered:
- Remove unnecessary catch block

#### THROWS010: Overly Broad Exception Catch
**Provider**: `OverlyBroadCatchCodeFixProvider.cs`
**Status**: Exists (implementation from previous work)

Code fixes offered:
1. Replace with specific exception
2. Add filter clause

## Build and Test Status

### Build Status: ✅ SUCCESS

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Test Status: ✅ ALL PASSING

```
Passed!  - Failed:     0, Passed:   269, Skipped:     0, Total:   269
```

All 269 existing tests pass with the new code fix infrastructure in place.

## Technical Implementation Details

### Base Class Features

1. **Consistent Code Action Creation**
   ```csharp
   protected CodeAction CreateCodeAction(
       string title,
       Func<CancellationToken, Task<Document>> createChangedDocument,
       string equivalenceKey)
   ```
   - Automatically prefixes equivalence keys with provider name
   - Ensures consistent naming across all code fixes

2. **Batch Fixing Support**
   ```csharp
   public override FixAllProvider GetFixAllProvider()
   {
       return WellKnownFixAllProviders.BatchFixer;
   }
   ```
   - Enables "Fix All" functionality in IDE
   - Allows fixing all instances of a diagnostic in a document, project, or solution

3. **Helper Methods**
   ```csharp
   protected static async Task<(Document, SyntaxNode)> GetDocumentAndRootAsync(
       CodeFixContext context,
       CancellationToken cancellationToken)
   ```
   - Reduces boilerplate in derived classes
   - Provides consistent error handling

### THROWS004 Implementation Highlights

The rethrow anti-pattern fix is a simple but important transformation:

**Before:**
```csharp
catch (Exception ex)
{
    throw ex; // ❌ Resets stack trace
}
```

**After:**
```csharp
catch (Exception ex)
{
    throw; // ✅ Preserves stack trace
}
```

Key implementation points:
- Uses `SyntaxFactory.ThrowStatement()` to create bare rethrow
- Preserves trivia (whitespace, comments) with `WithTriviaFrom()`
- Maintains semicolon token for consistent formatting

## Directory Structure

```
src/ThrowsAnalyzer/
└── CodeFixes/
    ├── ThrowsAnalyzerCodeFixProvider.cs          (Base class)
    ├── RethrowAntiPatternCodeFixProvider.cs       (THROWS004) ✅ NEW
    ├── MethodThrowsCodeFixProvider.cs             (THROWS001)
    ├── UnhandledThrowsCodeFixProvider.cs          (THROWS002)
    ├── TryCatchCodeFixProvider.cs                 (THROWS003)
    ├── CatchClauseOrderingCodeFixProvider.cs      (THROWS007)
    ├── EmptyCatchCodeFixProvider.cs               (THROWS008)
    ├── RethrowOnlyCatchCodeFixProvider.cs         (THROWS009)
    └── OverlyBroadCatchCodeFixProvider.cs         (THROWS010)
```

## Next Steps

### Phase 6.1: Exception Flow Code Fixes (THROWS017-019)

1. **THROWS017: Unhandled Method Call Exception**
   - Wrap call in try-catch
   - Add exception to method signature documentation
   - Propagate to caller

2. **THROWS018: Deep Exception Propagation**
   - Add exception handling at intermediate level
   - Document propagation chain

3. **THROWS019: Undocumented Public Exception**
   - Add comprehensive XML documentation

### Phase 6.2: Async Exception Code Fixes (THROWS020-022)

1. **THROWS020: Async Synchronous Throw**
   - Move validation before async
   - Add Task.Yield before throw

2. **THROWS021: Async Void Exception**
   - Change to async Task
   - Wrap in try-catch

3. **THROWS022: Unobserved Task Exception**
   - Add await
   - Assign to variable
   - Add continuation

### Phase 6.3: Iterator Exception Code Fixes (THROWS023-024)

1. **THROWS023: Deferred Iterator Exception**
   - Move validation before yield
   - Use wrapper method pattern

2. **THROWS024: Iterator Try-Finally Timing**
   - Add disposal documentation

### Phase 6.4: Best Practices Code Fixes (THROWS027-030)

1. **THROWS027: Exception Control Flow**
   - Convert to return value
   - Convert to Result<T>

2. **THROWS028: Custom Exception Naming**
   - Rename with "Exception" suffix

3. **THROWS029: Exception in Hot Path**
   - Move validation outside loop
   - Use Try pattern

4. **THROWS030: Result Pattern Suggestion**
   - Implement Result<T> pattern

### Phase 6.5: Lambda Exception Code Fixes (THROWS025-026)

1. **THROWS025: Lambda Uncaught Exception**
   - Wrap lambda body in try-catch
   - Use defensive coding

2. **THROWS026: Event Handler Lambda Exception**
   - Wrap in try-catch with logging

## Code Fix Testing Strategy

### Test Categories

1. **Transformation Tests**
   - Verify code fixes produce correct output
   - Preserve formatting and trivia
   - Handle edge cases

2. **Multiple Fix Tests**
   - Test behavior with multiple available fixes
   - Verify equivalence keys work correctly

3. **Batch Fixing Tests**
   - Test "Fix All in Document"
   - Test "Fix All in Project"
   - Test "Fix All in Solution"

4. **No-Op Tests**
   - Verify fixes don't apply when inappropriate
   - Handle malformed code gracefully

### Test Infrastructure Needed

- Code fix test helper methods
- Before/after code comparison utilities
- IDE integration tests (Visual Studio, VS Code)
- Performance benchmarks for code fix application

## Success Metrics

### Completed ✅
- [x] Base code fix provider infrastructure implemented
- [x] THROWS004 code fix provider implemented
- [x] All existing tests passing (269/269)
- [x] Build successful with no errors
- [x] Code fixes compile and integrate with existing infrastructure

### In Progress
- [ ] Implement remaining basic code fixes (THROWS017-030)
- [ ] Create comprehensive test suite for code fixes
- [ ] Test in Visual Studio IDE
- [ ] Test in VS Code with C# extension
- [ ] Performance testing (< 100ms per fix)
- [ ] Documentation updates

### Pending
- [ ] Advanced code fixes (THROWS017-030)
- [ ] Batch fixing tests
- [ ] IDE integration validation
- [ ] Code fix examples in documentation

## Notes

- The CodeFixes directory contains both previously implemented and newly created code fix providers
- All code fix providers inherit from the new base class for consistency
- The base class provides common functionality to reduce duplication
- Code fixes are automatically discoverable by the IDE through `[ExportCodeFixProvider]` attribute
- Batch fixing is enabled by default for all code fixes

## Conclusion

Phase 6 has successfully begun with the implementation of the base infrastructure and the first code fix provider (THROWS004). The foundation is now in place to rapidly implement the remaining code fixes for all 30 diagnostics (THROWS001-030).

The modular design of the base class and the consistent patterns across code fix providers will ensure maintainability and extensibility as we continue through Phase 6.
