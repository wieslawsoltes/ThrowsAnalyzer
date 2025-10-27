# Exception Patterns Sample

This sample project demonstrates all **ThrowsAnalyzer** diagnostics and their automated code fixes.

## Purpose

This project contains intentionally problematic code that triggers all ThrowsAnalyzer diagnostic rules. It serves as:
- A demonstration of the analyzer's capabilities
- A testing ground for code fixes
- Documentation through working examples

## Running the Sample

### Build and See Diagnostics

```bash
dotnet build
```

You'll see warnings for all THROWS001-010 diagnostics (except THROWS005, THROWS006 which are not implemented).

### View in IDE

1. Open the solution in Visual Studio or VS Code
2. Open `Program.cs`
3. You'll see:
   - Squiggly underlines on problematic code
   - Light bulb suggestions for code fixes
   - Diagnostic messages in the Error List/Problems panel

### Try Code Fixes

In your IDE:
1. Place cursor on a diagnostic (squiggly underline)
2. Press `Ctrl+.` (or `Cmd+.` on Mac)
3. Select a code fix from the list
4. The analyzer will automatically refactor the code

## Diagnostics Demonstrated

### Basic Exception Handling (THROWS001-010)

| Diagnostic | Example Method | Description |
|------------|----------------|-------------|
| **THROWS001** | `MethodWithThrow()` | Method contains throw statement |
| **THROWS002** | `MethodWithUnhandledThrow()` | Unhandled throw statement |
| **THROWS003** | `MethodWithTryCatch()` | Method contains try-catch block |
| **THROWS004** | `RethrowAntiPattern()` | Rethrow anti-pattern (`throw ex;`) |
| **THROWS007** | (See note below) | Unreachable catch clause |
| **THROWS008** | `EmptyCatchBlock()` | Empty catch block swallows exceptions |
| **THROWS009** | `RethrowOnlyCatch()` | Catch block only rethrows |
| **THROWS010** | `OverlyBroadCatch()` | Overly broad exception catch |

### Lambda Exception Patterns (THROWS025)

| Diagnostic | Example Method | Description |
|------------|----------------|-------------|
| **THROWS025** | `LinqLambdaUncaught()` | Lambda in LINQ throws uncaught exception |
| **THROWS025** | `ThrowExpressionInLambda()` | Throw expression in lambda |

### Event Handler Patterns (THROWS026)

| Diagnostic | Example Method | Description |
|------------|----------------|-------------|
| **THROWS026** | `EventHandlerLambdaUncaught()` | Event handler lambda throws without catching |
| **THROWS026** | `EventHandlerWithThrowExpression()` | Event handler with throw expression |
| **THROWS026** | `EventHandlerWithRethrow()` | Event handler rethrows exception |
| **THROWS026** | `ButtonClickHandlerExample()` | Real-world button click handler |
| **THROWS026** | `CustomEventHandlerPattern()` | Custom event handler delegate |

**Note**: Event handler **method references** (like `DataReceived += OnDataReceived;`) are analyzed by THROWS001/THROWS002, not THROWS026. THROWS026 is specifically for lambda expressions.

### Note on THROWS007

THROWS007 requires catch clauses to be in the wrong order. The `CatchOrderingIssue()` method shows the *correct* ordering (specific to general). To see THROWS007:

1. Manually reorder the catch clauses to put `Exception` first
2. Build the project
3. Use the code fix to automatically reorder them

## Member Type Coverage

The sample demonstrates that ThrowsAnalyzer works across all member types:

- **Methods**: `MethodWithThrow()`
- **Constructors**: `Program()`
- **Properties**: `PropertyWithThrow`
- **Operators**: `operator +`
- **Local Functions**: `MethodWithLocalFunction()`
- **Lambdas**: `MethodWithLambda()`

## Code Fixes Available

Each diagnostic has one or more automated code fix options:

### THROWS001 - Method Contains Throw
- ✅ Wrap throw in try-catch block

### THROWS002 - Unhandled Throw
- ✅ Wrap unhandled throws in try-catch block

### THROWS003 - Try-Catch Block
- ✅ Remove try-catch and propagate
- ✅ Add logging to empty catches

### THROWS004 - Rethrow Anti-Pattern
- ✅ Replace `throw ex;` with `throw;`

### THROWS007 - Unreachable Catch
- ✅ Reorder catch clauses (specific to general)

### THROWS008 - Empty Catch Block
- ✅ Remove empty catch
- ✅ Add logging to catch

### THROWS009 - Rethrow-Only Catch
- ✅ Remove unnecessary catch

### THROWS010 - Overly Broad Catch
- ✅ Add exception filter (`when` clause)

## Configuration

The `.editorconfig` in this directory sets all ThrowsAnalyzer diagnostics to `warning` severity for demonstration purposes. In real projects, you can configure severity per diagnostic:

```ini
[*.cs]
dotnet_diagnostic.THROWS001.severity = suggestion
dotnet_diagnostic.THROWS002.severity = warning
# ... etc
```

## Learning Resources

- [ThrowsAnalyzer README](../../README.md) - Full documentation
- [Phase 4 Completion Summary](../../docs/PHASE4_COMPLETION_SUMMARY.md) - Implementation details
- [Analysis Document](../../docs/ANALYSIS.md) - Technical analysis

## Next Steps

1. Try applying code fixes to see the transformations
2. Experiment with the `.editorconfig` to change diagnostic severities
3. Add your own exception handling patterns
4. Use this as a reference for proper exception handling in C#
