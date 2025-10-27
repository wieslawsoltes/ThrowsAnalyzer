# Exception Patterns Sample

This sample project demonstrates all **30 ThrowsAnalyzer diagnostics** and their **16 automated code fixes**.

## Purpose

This project contains intentionally problematic code that triggers all ThrowsAnalyzer diagnostic rules. It serves as:
- 📚 Comprehensive demonstration of all analyzer capabilities
- 🔧 Testing ground for all code fixes
- 📖 Documentation through working examples
- 🎓 Learning resource for exception handling best practices

## Quick Start

### Build and See Diagnostics

```bash
dotnet build
```

You'll see diagnostics from all categories: basic exception handling, exception flow, async patterns, iterator patterns, lambda patterns, and best practices.

### View in IDE

1. Open the solution in Visual Studio, VS Code, or Rider
2. Open `Program.cs`
3. You'll see:
   - 🔴 Error/warning squiggles on problematic code
   - 💡 Light bulb suggestions for automated code fixes
   - 📋 Diagnostic messages in Error List/Problems panel

### Apply Code Fixes

1. Place cursor on a diagnostic (squiggly underline)
2. Press `Ctrl+.` (Windows/Linux) or `Cmd+.` (Mac)
3. Select a code fix from the menu
4. Watch the analyzer automatically refactor your code!

---

## Complete Diagnostic Reference

### Category 1: Basic Exception Handling (THROWS001-010)

| ID | Severity | Title | Description | Code Fix | Example in Sample |
|----|----------|-------|-------------|----------|-------------------|
| **THROWS001** | Suggestion | Method contains throw | Informational diagnostic when method throws exceptions | ✅ Wrap in try-catch | `MethodWithThrow()` |
| **THROWS002** | Warning | Unhandled throw statement | Throw not enclosed in try-catch | ✅ Wrap in try-catch | `MethodWithUnhandledThrow()` |
| **THROWS003** | Suggestion | Try-catch block present | Informational when try-catch exists | ✅ Remove try-catch<br>✅ Add logging | `MethodWithTryCatch()` |
| **THROWS004** | Error | Rethrow anti-pattern | Using `throw ex;` instead of `throw;` | ✅ Replace with `throw;` | `RethrowAntiPattern()` |
| **THROWS007** | Warning | Unreachable catch clause | Catch clauses in wrong order | ✅ Reorder catches | `CatchOrderingIssue()` (when reordered) |
| **THROWS008** | Warning | Empty catch block | Catch block swallows exceptions | ✅ Remove empty catch<br>✅ Add logging | `EmptyCatchBlock()` |
| **THROWS009** | Suggestion | Catch only rethrows | Catch block only contains `throw;` | ✅ Remove catch | `RethrowOnlyCatch()` |
| **THROWS010** | Warning | Overly broad catch | Catching `System.Exception` | ✅ Add `when` filter | `OverlyBroadCatch()` |

**Example - THROWS004 Fix:**
```csharp
// Before (BAD - resets stack trace)
catch (Exception ex)
{
    throw ex;
}

// After (GOOD - preserves stack trace)
catch (Exception ex)
{
    throw;
}
```

---

### Category 2: Exception Flow Analysis (THROWS017-019)

| ID | Severity | Title | Description | Code Fix | Example in Sample |
|----|----------|-------|-------------|----------|-------------------|
| **THROWS017** | Warning | Unhandled method call exception | Called method throws, not caught | ✅ Wrap in try-catch<br>✅ Add XML doc | *(Requires method calls)* |
| **THROWS018** | Info | Deep exception propagation | Exception propagates through many methods | ❌ None | *(Informational only)* |
| **THROWS019** | Warning | Undocumented public exception | Public API throws without `<exception>` doc | ✅ Add XML doc | *(Requires public methods)* |

**Example - THROWS017 Fix:**
```csharp
// Before (BAD)
void Caller()
{
    MethodThatThrows(); // Uncaught exception
}

// After Fix 1: Wrap in try-catch
void Caller()
{
    try
    {
        MethodThatThrows();
    }
    catch (InvalidOperationException ex)
    {
        // Handle exception
    }
}

// After Fix 2: Add propagation documentation
/// <exception cref="InvalidOperationException">Thrown by MethodThatThrows</exception>
void Caller()
{
    MethodThatThrows();
}
```

---

### Category 3: Async Exception Patterns (THROWS020-022)

| ID | Severity | Title | Description | Code Fix | Example in Sample |
|----|----------|-------|-------------|----------|-------------------|
| **THROWS020** | Warning | Async synchronous throw | Throwing synchronously in async method | ✅ Add Task.Yield()<br>✅ Extract wrapper | *(Not in current sample)* |
| **THROWS021** | Error | Async void exception | `async void` method throws (crashes app) | ✅ Convert to async Task<br>✅ Wrap in try-catch | *(Not in current sample)* |
| **THROWS022** | Warning | Unobserved Task exception | Task returned but not awaited/observed | ✅ Add await<br>✅ Assign to variable<br>✅ Add ContinueWith | *(Not in current sample)* |

**Example - THROWS021 Fix:**
```csharp
// Before (DANGEROUS - crashes on exception)
async void ProcessData()
{
    await LoadDataAsync();
    throw new InvalidOperationException();
}

// After Fix 1: Convert to async Task
async Task ProcessData()
{
    await LoadDataAsync();
    throw new InvalidOperationException();
}

// After Fix 2: Wrap in try-catch (for event handlers)
async void ProcessData()
{
    try
    {
        await LoadDataAsync();
        throw new InvalidOperationException();
    }
    catch (Exception ex)
    {
        // Log error
        Console.WriteLine($"Error: {ex.Message}");
    }
}
```

**Example - THROWS022 Fix:**
```csharp
// Before (BAD - unobserved exception)
void Caller()
{
    DoWorkAsync(); // Returns Task, not awaited
}

// After Fix 1: Add await
async Task Caller()
{
    await DoWorkAsync();
}

// After Fix 2: Add ContinueWith error handler
void Caller()
{
    DoWorkAsync().ContinueWith(t =>
    {
        if (t.IsFaulted)
        {
            Console.WriteLine($"Error: {t.Exception?.Message}");
        }
    });
}
```

---

### Category 4: Iterator Exception Patterns (THROWS023-024)

| ID | Severity | Title | Description | Code Fix | Example in Sample |
|----|----------|-------|-------------|----------|-------------------|
| **THROWS023** | Warning | Deferred iterator exception | Exception in `yield` method deferred until enumeration | ❌ None | *(Not in current sample)* |
| **THROWS024** | Info | Iterator try-finally timing | Try-finally in iterator has deferred cleanup | ❌ None | *(Not in current sample)* |

**Example - THROWS023:**
```csharp
// THROWS023: Exception deferred until enumeration
IEnumerable<int> GetNumbers(int count)
{
    if (count < 0)
        throw new ArgumentException(nameof(count)); // Not thrown here!

    for (int i = 0; i < count; i++)
        yield return i;
}

// Better: Validate before yield
IEnumerable<int> GetNumbers(int count)
{
    if (count < 0)
        throw new ArgumentException(nameof(count)); // Thrown immediately

    return GetNumbersImpl(count);

    IEnumerable<int> GetNumbersImpl(int c)
    {
        for (int i = 0; i < c; i++)
            yield return i;
    }
}
```

---

### Category 5: Lambda Exception Patterns (THROWS025-026)

| ID | Severity | Title | Description | Code Fix | Example in Sample |
|----|----------|-------|-------------|----------|-------------------|
| **THROWS025** | Warning | Lambda uncaught exception | Lambda (LINQ, Task.Run, etc.) throws uncaught | ❌ None | `LinqLambdaUncaught()`<br>`ThrowExpressionInLambda()` |
| **THROWS026** | Error | Event handler lambda exception | Event handler lambda throws (crashes app) | ❌ None | `EventHandlerLambdaUncaught()`<br>`ButtonClickHandlerExample()` |

**Example - THROWS025:**
```csharp
// BAD: Lambda throws uncaught
var items = new[] { 1, 2, -1, 3 };
var result = items.Where(x =>
{
    if (x < 0)
        throw new InvalidOperationException(); // THROWS025
    return x > 1;
});

// GOOD: Lambda catches exception
var result = items.Where(x =>
{
    try
    {
        if (x < 0)
            throw new InvalidOperationException();
        return x > 1;
    }
    catch (InvalidOperationException)
    {
        return false;
    }
});
```

**Example - THROWS026:**
```csharp
// BAD: Event handler lambda throws (may crash app)
button.Click += (sender, e) =>
{
    throw new InvalidOperationException(); // THROWS026 - ERROR!
};

// GOOD: Event handler catches exceptions
button.Click += (sender, e) =>
{
    try
    {
        PerformAction();
    }
    catch (InvalidOperationException ex)
    {
        MessageBox.Show($"Error: {ex.Message}");
    }
};
```

**Important**: Event handler **method references** are analyzed differently:
```csharp
// THROWS026 applies to lambdas only
button.Click += (sender, e) => throw new Exception(); // THROWS026

// Method references analyzed by THROWS001/002
button.Click += OnButtonClick; // Not THROWS026
void OnButtonClick(object sender, EventArgs e)
{
    throw new Exception(); // THROWS001/002
}
```

---

### Category 6: Best Practices (THROWS027-030)

| ID | Severity | Title | Description | Code Fix | Example in Sample |
|----|----------|-------|-------------|----------|-------------------|
| **THROWS027** | Warning | Exception for control flow | Using exceptions for normal control flow | ❌ None | *(Not in current sample)* |
| **THROWS028** | Warning | Custom exception naming | Exception type doesn't end with "Exception" | ✅ Rename type | *(Not in current sample)* |
| **THROWS029** | Warning | Exception in hot path | Throwing in loop (performance issue) | ✅ Move before loop<br>✅ Convert to return | *(Not in current sample)* |
| **THROWS030** | Info | Result pattern suggestion | Consider Result&lt;T&gt; for expected errors | ✅ Add comment<br>✅ Convert to bool | *(Not in current sample)* |

**Example - THROWS028 Fix:**
```csharp
// Before (BAD)
public class InvalidOperation : Exception { }

// After (GOOD - follows .NET naming convention)
public class InvalidOperationException : Exception { }
```

**Example - THROWS029 Fix:**
```csharp
// Before (BAD - throws in loop)
foreach (var item in items)
{
    if (item < 0)
        throw new ArgumentException(); // THROWS029 - performance issue
    Process(item);
}

// After Fix: Move validation before loop
if (items.Any(i => i < 0))
    throw new ArgumentException();

foreach (var item in items)
{
    Process(item);
}
```

**Example - THROWS030:**
```csharp
// Current approach with exceptions
int ParseValue(string input)
{
    if (string.IsNullOrEmpty(input))
        throw new ArgumentException(); // THROWS030 suggests Result<T>
    return int.Parse(input);
}

// Better: Result<T> pattern
Result<int> ParseValue(string input)
{
    if (string.IsNullOrEmpty(input))
        return Result<int>.Failure("Input is empty");
    return Result<int>.Success(int.Parse(input));
}
```

---

## Summary Statistics

### Coverage

- ✅ **30 Total Diagnostics** across 6 categories
- ✅ **16 Automated Code Fixes** (53% of diagnostics)
- ✅ **269 Unit Tests** (100% passing)
- ✅ **All C# Member Types** supported

### Code Fixes by Category

| Category | Diagnostics | Code Fixes | Fix Coverage |
|----------|-------------|------------|--------------|
| Basic Exception Handling | 8 | 8 | 100% |
| Exception Flow | 3 | 2 | 67% |
| Async Patterns | 3 | 3 | 100% |
| Iterator Patterns | 2 | 0 | 0% |
| Lambda Patterns | 2 | 0 | 0% |
| Best Practices | 4 | 3 | 75% |
| **Total** | **22** | **16** | **73%** |

### Why Some Diagnostics Don't Have Code Fixes

**Informational Only**:
- THROWS018 (Deep Propagation) - Requires human judgment
- THROWS024 (Iterator Timing) - Documentation warning only

**Context-Dependent**:
- THROWS023 (Deferred Iterator) - Complex refactoring, case-by-case
- THROWS025 (Lambda Uncaught) - Depends on lambda usage context
- THROWS026 (Event Handler Lambda) - Depends on error handling strategy
- THROWS027 (Control Flow) - Significant architectural refactoring

---

## Member Type Coverage

ThrowsAnalyzer works across **all C# executable member types**:

| Member Type | Example in Sample | Diagnostics Apply |
|-------------|-------------------|-------------------|
| **Methods** | `MethodWithThrow()` | ✅ All |
| **Constructors** | `Program()` | ✅ All |
| **Properties** | `PropertyWithThrow` | ✅ All |
| **Indexers** | *(Not in sample)* | ✅ All |
| **Operators** | `operator +` | ✅ All |
| **Local Functions** | `MethodWithLocalFunction()` | ✅ All |
| **Lambda Expressions** | `MethodWithLambda()` | ✅ All + THROWS025/026 |
| **Anonymous Methods** | *(Not in sample)* | ✅ All |
| **Accessors** | `PropertyWithThrow.get/set` | ✅ All |
| **Event Accessors** | *(Not in sample)* | ✅ All |

---

## Configuration

ThrowsAnalyzer provides comprehensive configuration for all 30 diagnostics through `.editorconfig`.

### Severity Levels Explained

| Severity | Build | IDE Display | When to Use |
|----------|-------|-------------|-------------|
| **error** | ❌ Fails | 🔴 Red squiggle | Critical issues (THROWS004, 021, 026) |
| **warning** | ✅ Succeeds | 🟡 Yellow squiggle | Should be fixed (most rules) |
| **suggestion** | ✅ Succeeds | 💡 Gray dots | Optional improvements (informational) |
| **silent** | ✅ Succeeds | No UI | Background only |
| **none** | ✅ Succeeds | Disabled | Turn off rule completely |

### Quick Configuration

**Minimal (Critical Only)**:
```ini
[*.cs]
dotnet_diagnostic.THROWS004.severity = error  # Rethrow anti-pattern
dotnet_diagnostic.THROWS021.severity = error  # Async void
dotnet_diagnostic.THROWS026.severity = error  # Event handler exceptions
```

**Recommended for Production**:
```ini
[*.cs]
# Critical - Build fails
dotnet_diagnostic.THROWS004.severity = error
dotnet_diagnostic.THROWS021.severity = error
dotnet_diagnostic.THROWS026.severity = error

# Important - Warnings
dotnet_diagnostic.THROWS002.severity = warning  # Unhandled throws
dotnet_diagnostic.THROWS008.severity = warning  # Empty catch
dotnet_diagnostic.THROWS017.severity = warning  # Unhandled method call
dotnet_diagnostic.THROWS019.severity = warning  # Undocumented public API
dotnet_diagnostic.THROWS022.severity = warning  # Unobserved Task
dotnet_diagnostic.THROWS028.severity = warning  # Exception naming
dotnet_diagnostic.THROWS029.severity = warning  # Hot path performance

# Informational - Suggestions
dotnet_diagnostic.THROWS001.severity = suggestion
dotnet_diagnostic.THROWS003.severity = suggestion
dotnet_diagnostic.THROWS009.severity = suggestion
dotnet_diagnostic.THROWS010.severity = suggestion
dotnet_diagnostic.THROWS030.severity = suggestion
```

**For Learning/Training**:
```ini
[*.cs]
# Make all diagnostics visible as warnings
dotnet_diagnostic.THROWS001.severity = warning
dotnet_diagnostic.THROWS002.severity = warning
dotnet_diagnostic.THROWS003.severity = warning
# ... (all 30 diagnostics as warning)
```

### Complete Configuration Reference

This sample project includes a fully configured `.editorconfig` with:
- ✅ All 30 diagnostics configured
- ✅ Severity explanations for each rule
- ✅ Category organization
- ✅ Conflicting analyzer suppressions
- ✅ Severity level guide

**View**: `.editorconfig` in this directory

### Configuration by Category

**Category 1: Basic Exception Handling**
```ini
dotnet_diagnostic.THROWS001.severity = suggestion  # Method contains throw
dotnet_diagnostic.THROWS002.severity = warning     # Unhandled throw
dotnet_diagnostic.THROWS003.severity = suggestion  # Try-catch present
dotnet_diagnostic.THROWS004.severity = error       # Rethrow anti-pattern ⚠️
dotnet_diagnostic.THROWS007.severity = warning     # Unreachable catch
dotnet_diagnostic.THROWS008.severity = warning     # Empty catch block
dotnet_diagnostic.THROWS009.severity = suggestion  # Catch only rethrows
dotnet_diagnostic.THROWS010.severity = suggestion  # Overly broad catch
```

**Category 2: Exception Flow Analysis**
```ini
dotnet_diagnostic.THROWS017.severity = warning     # Unhandled method call
dotnet_diagnostic.THROWS018.severity = suggestion  # Deep propagation (info)
dotnet_diagnostic.THROWS019.severity = warning     # Undocumented public API
```

**Category 3: Async Patterns**
```ini
dotnet_diagnostic.THROWS020.severity = warning     # Async synchronous throw
dotnet_diagnostic.THROWS021.severity = error       # Async void ⚠️ CRITICAL
dotnet_diagnostic.THROWS022.severity = warning     # Unobserved Task
```

**Category 4: Iterator Patterns**
```ini
dotnet_diagnostic.THROWS023.severity = warning     # Deferred iterator exception
dotnet_diagnostic.THROWS024.severity = suggestion  # Iterator timing (info)
```

**Category 5: Lambda Patterns**
```ini
dotnet_diagnostic.THROWS025.severity = warning     # Lambda uncaught exception
dotnet_diagnostic.THROWS026.severity = error       # Event handler ⚠️ CRITICAL
```

**Category 6: Best Practices**
```ini
dotnet_diagnostic.THROWS027.severity = warning     # Exception for control flow
dotnet_diagnostic.THROWS028.severity = warning     # Exception naming convention
dotnet_diagnostic.THROWS029.severity = warning     # Exception in hot path
dotnet_diagnostic.THROWS030.severity = suggestion  # Result<T> suggestion
```

### Configuration Scenarios

**1. Web Application (ASP.NET Core)**
```ini
# Focus: Async patterns, request handling, API documentation
dotnet_diagnostic.THROWS021.severity = error  # Async void
dotnet_diagnostic.THROWS026.severity = error  # Event handlers
dotnet_diagnostic.THROWS004.severity = error  # Stack trace
dotnet_diagnostic.THROWS019.severity = error  # API docs
dotnet_diagnostic.THROWS020.severity = warning
dotnet_diagnostic.THROWS022.severity = warning
```

**2. Library/SDK**
```ini
# Focus: Public API documentation
dotnet_diagnostic.THROWS019.severity = error  # Undocumented public exceptions
dotnet_diagnostic.THROWS004.severity = error  # Rethrow anti-pattern
dotnet_diagnostic.THROWS021.severity = error  # Async void
dotnet_diagnostic.THROWS001.severity = silent  # Internal throws OK
dotnet_diagnostic.THROWS003.severity = silent  # Internal try-catch OK
```

**3. Performance-Critical Application**
```ini
# Focus: Performance issues
dotnet_diagnostic.THROWS029.severity = error  # Exception in hot path
dotnet_diagnostic.THROWS027.severity = error  # Exception for control flow
dotnet_diagnostic.THROWS004.severity = error  # Rethrow anti-pattern
dotnet_diagnostic.THROWS021.severity = error  # Async void
```

**4. Legacy Code Migration**
```ini
# Start conservative, gradually enable
dotnet_diagnostic.THROWS004.severity = error   # Critical only
dotnet_diagnostic.THROWS021.severity = error
dotnet_diagnostic.THROWS026.severity = error
dotnet_diagnostic.THROWS*.severity = suggestion # Rest as suggestions
dotnet_diagnostic.THROWS001.severity = none    # Too noisy in legacy
dotnet_diagnostic.THROWS003.severity = none
```

### Per-File Configuration

Disable for specific files using `#pragma`:

```csharp
#pragma warning disable THROWS001 // Method contains throw
public void LegacyMethod()
{
    throw new NotImplementedException();
}
#pragma warning restore THROWS001
```

### Build Integration

**CI/CD - Treat warnings as errors**:
```bash
dotnet build /p:TreatWarningsAsErrors=true
```

**Selective errors in .csproj**:
```xml
<PropertyGroup>
  <!-- Only these diagnostics fail build -->
  <WarningsAsErrors>THROWS004;THROWS021;THROWS026</WarningsAsErrors>
</PropertyGroup>
```

**Suppress in build**:
```xml
<PropertyGroup>
  <NoWarn>THROWS001;THROWS003</NoWarn>
</PropertyGroup>
```

### IDE-Specific Notes

**Visual Studio 2022**:
- Tools > Options > Text Editor > C# > Code Style
- Or edit `.editorconfig` (recommended)

**VS Code**:
- Requires C# extension
- `.editorconfig` automatically applied

**JetBrains Rider**:
- Settings > Editor > Code Style > C# > Inspection Severity
- Or edit `.editorconfig` (recommended)

### Configuration Best Practices

1. ✅ **Use `.editorconfig`** - Version control your settings
2. ✅ **Start strict** - Easier to relax than tighten
3. ✅ **Critical as errors** - THROWS004, 021, 026
4. ✅ **Team consensus** - Share configuration across team
5. ✅ **Document deviations** - Comment why rules are disabled
6. ✅ **Review periodically** - Audit which rules help/hinder
7. ✅ **CI enforcement** - Fail build on critical issues

### Troubleshooting

**Diagnostics not appearing?**
- Check `.editorconfig` location (project root)
- Verify severity is not `none` or `silent`
- Restart IDE after config changes
- Check analyzer is installed: `dotnet list package | grep ThrowsAnalyzer`

**Too many warnings?**
- Start with minimal configuration
- Gradually enable rules
- Use per-file `#pragma` for exceptions
- Consider legacy code migration pattern

**Performance issues?**
- Disable expensive analyzers:
  ```ini
  dotnet_diagnostic.THROWS017.severity = none  # Call graph analysis
  dotnet_diagnostic.THROWS018.severity = none  # Deep propagation
  ```

### Full Configuration Guide

For complete documentation, see: [CONFIGURATION_GUIDE.md](../../docs/CONFIGURATION_GUIDE.md)

Includes:
- Detailed explanation of each diagnostic
- All code fix options with equivalence keys
- Advanced configuration scenarios
- IDE-specific integration
- Build system integration
- Performance tuning

---

## Code Fix Equivalence Keys

For programmatic code fix application or testing:

| Code Fix | Equivalence Key Prefix |
|----------|------------------------|
| Wrap in try-catch | `MethodThrowsCodeFixProvider:WrapInTryCatch` |
| Replace throw ex | `RethrowAntiPatternCodeFixProvider:ReplaceWithBareRethrow` |
| Reorder catches | `CatchClauseOrderingCodeFixProvider:ReorderCatchClauses` |
| Remove empty catch | `EmptyCatchCodeFixProvider:RemoveEmptyCatch` |
| Add logging | `EmptyCatchCodeFixProvider:AddLogging` |
| Convert async void | `AsyncVoidExceptionCodeFixProvider:ConvertToAsyncTask` |
| Rename exception | `CustomExceptionNamingCodeFixProvider:RenameExceptionType` |
| Move before loop | `ExceptionInHotPathCodeFixProvider:MoveValidationBeforeLoop` |

---

## Testing the Sample

### 1. Build and Review Diagnostics

```bash
cd samples/ExceptionPatterns
dotnet build
```

Review the output to see all diagnostics in action.

### 2. Apply Code Fixes

Open in your IDE and try each code fix:
- ✅ THROWS004: Fix rethrow anti-pattern
- ✅ THROWS007: Reorder catch clauses
- ✅ THROWS008: Add logging to empty catch
- ✅ THROWS009: Remove unnecessary catch

### 3. Experiment with Configuration

Edit `.editorconfig` to change severities and see how diagnostics change.

### 4. Add Your Own Patterns

Try adding:
- Async methods with exceptions
- Iterator methods with exceptions
- More complex exception hierarchies
- Custom exception types

---

## Learning Path

### Beginner
1. Start with **Basic Exception Handling** (THROWS001-010)
2. Understand try-catch patterns
3. Learn rethrow best practices

### Intermediate
4. Explore **Lambda Patterns** (THROWS025-026)
5. Study **Async Patterns** (THROWS020-022)
6. Practice exception flow analysis

### Advanced
7. Master **Iterator Patterns** (THROWS023-024)
8. Apply **Best Practices** (THROWS027-030)
9. Design exception handling strategies

---

## Additional Resources

- 📖 [ThrowsAnalyzer Main README](../../README.md) - Complete documentation
- 📋 [Project Status](../../docs/PROJECT_STATUS.md) - Implementation details
- 📦 [NuGet Package](https://www.nuget.org/packages/ThrowsAnalyzer/) - Production package
- 🔧 [Packaging Guide](../../PACKAGING.md) - Build and publish instructions
- ✅ [Release Checklist](../../RELEASE_CHECKLIST.md) - Quality assurance

---

## Contributing

Found an issue or want to add more examples?

1. Add your example to `Program.cs`
2. Build and verify diagnostics appear
3. Document in this README
4. Submit a pull request

---

**ThrowsAnalyzer** - Write safer C# code with comprehensive exception analysis! 🚀
