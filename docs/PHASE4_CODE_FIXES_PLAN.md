# Phase 4: Code Fixes Implementation Plan

## Overview

This document outlines the implementation plan for adding code fix providers to ThrowsAnalyzer. Code fixes provide automated refactoring options that appear in the IDE when diagnostics are reported, significantly improving the developer experience.

## Scope

Code fixes will be implemented for **all 8 diagnostic rules**:
- **THROWS001-003**: Basic exception detection (existing analyzers)
- **THROWS004, 007-010**: Type-aware exception analysis (newly implemented)

## Architecture

### Base Infrastructure

```
src/ThrowsAnalyzer/
├── CodeFixes/
│   ├── ThrowsAnalyzerCodeFixProvider.cs (Base class)
│   ├── Basic/
│   │   ├── MethodThrowsCodeFixProvider.cs (THROWS001)
│   │   ├── UnhandledThrowsCodeFixProvider.cs (THROWS002)
│   │   └── TryCatchCodeFixProvider.cs (THROWS003)
│   └── TypeAware/
│       ├── RethrowAntiPatternCodeFixProvider.cs (THROWS004)
│       ├── CatchClauseOrderingCodeFixProvider.cs (THROWS007)
│       ├── EmptyCatchCodeFixProvider.cs (THROWS008)
│       ├── RethrowOnlyCatchCodeFixProvider.cs (THROWS009)
│       └── OverlyBroadCatchCodeFixProvider.cs (THROWS010)
```

## Code Fix Catalog

### THROWS001: Method Contains Throw Statement

**Options:**
1. Wrap in try-catch block
2. Add XML documentation (`/// <exception cref="...">`)

**Complexity:** Medium
- Requires inserting try-catch around throw statements
- Must preserve existing code structure
- XML doc requires finding method declaration

---

### THROWS002: Unhandled Throw Statement

**Options:**
1. Wrap in try-catch block
2. Move to existing try block (if nearby)

**Complexity:** Medium
- Similar to THROWS001 but more targeted
- May need to detect existing try blocks

---

### THROWS003: Method Contains Try-Catch Block

**Options:**
1. Remove try-catch and propagate
2. Add logging to catch blocks

**Complexity:** Low-Medium
- Removal is straightforward
- Logging requires code generation

---

### THROWS004: Rethrow Anti-Pattern

**Option:**
1. Replace `throw ex;` with `throw;`

**Complexity:** Low ⭐ **EASIEST** - Start here!
- Simple syntax node replacement
- No semantic analysis needed
- Clear transformation

---

### THROWS007: Unreachable Catch Clause

**Option:**
1. Reorder catch clauses (specific to general)

**Complexity:** High
- Requires semantic analysis to determine correct order
- Must preserve catch block content
- Need to handle filters correctly

---

### THROWS008: Empty Catch Block

**Options:**
1. Remove empty catch
2. Add logging
3. Add TODO comment

**Complexity:** Medium
- Removal requires careful handling of try-finally
- Multiple options = more code

---

### THROWS009: Catch Block Only Rethrows

**Option:**
1. Remove unnecessary catch

**Complexity:** Medium
- Must verify catch only contains `throw;`
- May need to unwrap entire try statement

---

### THROWS010: Overly Broad Exception Catch

**Options:**
1. Replace with specific exception
2. Add filter clause (`when`)

**Complexity:** High
- Suggesting specific exceptions requires context analysis
- Multiple valid options

## Implementation Order (Recommended)

### Phase 4A: Simple Fixes (1 week)
1. ✅ **THROWS004** - Rethrow anti-pattern (simplest)
2. **THROWS003** - Remove try-catch
3. **THROWS008** - Add logging/comment

### Phase 4B: Medium Complexity (1 week)
4. **THROWS001** - Wrap in try-catch
5. **THROWS002** - Wrap unhandled throws
6. **THROWS009** - Remove rethrow-only catch

### Phase 4C: Advanced Fixes (1 week)
7. **THROWS007** - Reorder catch clauses
8. **THROWS010** - Replace with specific exception

## Testing Strategy

### Test Structure

```
tests/ThrowsAnalyzer.Tests/
├── CodeFixes/
│   ├── RethrowAntiPatternCodeFixTests.cs (5-8 tests)
│   ├── MethodThrowsCodeFixTests.cs (5-8 tests)
│   ├── UnhandledThrowsCodeFixTests.cs (5-8 tests)
│   ├── TryCatchCodeFixTests.cs (5-8 tests)
│   ├── CatchClauseOrderingCodeFixTests.cs (5-8 tests)
│   ├── EmptyCatchCodeFixTests.cs (5-8 tests)
│   ├── RethrowOnlyCatchCodeFixTests.cs (5-8 tests)
│   └── OverlyBroadCatchCodeFixTests.cs (5-8 tests)
```

### Test Categories

Each code fix provider should have:
1. **Basic transformation** - Simple before/after
2. **Preserve formatting** - Comments, whitespace
3. **Multiple fixes** - When applicable
4. **Edge cases** - Nested structures, invalid syntax
5. **No-op** - When fix shouldn't apply

**Total Tests:** ~50-64 tests

## Success Criteria

- [ ] All code fixes compile without errors
- [ ] All code fixes preserve formatting and comments
- [ ] All tests pass (50+ new tests)
- [ ] Code fixes work in Visual Studio
- [ ] Code fixes work in VS Code
- [ ] Average fix application < 100ms
- [ ] Documentation includes code fix examples

## Dependencies

### NuGet Packages Required

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.8.0" />
  <PackageReference Include="Microsoft.CodeAnalysis.Workspaces.Common" Version="4.8.0" />
</ItemGroup>
```

### References Needed

- `Microsoft.CodeAnalysis.CodeFixes`
- `Microsoft.CodeAnalysis.CSharp.Syntax`
- `System.Composition` (for `[ExportCodeFixProvider]`)

## Configuration Options

Add to `.editorconfig`:

```ini
[*.cs]

# Code fix behavior preferences
throws_analyzer_codefix_prefer_logging = true
throws_analyzer_codefix_logging_method = Console.WriteLine
throws_analyzer_codefix_add_todo_comments = true
throws_analyzer_codefix_preserve_empty_catches = false
```

## Risk Assessment

### Low Risk
- THROWS004 (rethrow anti-pattern) - Simple transformation
- THROWS008 (add logging) - Straightforward code generation

### Medium Risk
- THROWS001/002 (wrap in try-catch) - Must preserve structure
- THROWS009 (remove catch) - Must handle try-finally correctly

### High Risk
- THROWS007 (reorder catches) - Complex semantic analysis
- THROWS010 (suggest specific exception) - Context-dependent

## Deliverables

1. ✅ Base `ThrowsAnalyzerCodeFixProvider` class
2. 8 code fix provider implementations
3. 50+ unit tests
4. Updated documentation
5. Configuration examples
6. Sample project demonstrating all fixes

## Timeline

- **Week 1:** Phase 4A (THROWS004, 003, 008) + base infrastructure
- **Week 2:** Phase 4B (THROWS001, 002, 009) + testing
- **Week 3:** Phase 4C (THROWS007, 010) + integration testing + docs

**Total:** 2-3 weeks

## Next Steps

1. Create base `ThrowsAnalyzerCodeFixProvider` class
2. Implement THROWS004 code fix (simplest, good starting point)
3. Add code fix tests
4. Verify in Visual Studio
5. Continue with remaining fixes in priority order
