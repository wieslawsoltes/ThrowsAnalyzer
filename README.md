# ThrowsAnalyzer

[![CI](https://github.com/wieslawsoltes/ThrowsAnalyzer/actions/workflows/ci.yml/badge.svg)](https://github.com/wieslawsoltes/ThrowsAnalyzer/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/ThrowsAnalyzer.svg)](https://www.nuget.org/packages/ThrowsAnalyzer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ThrowsAnalyzer.svg)](https://www.nuget.org/packages/ThrowsAnalyzer)
[![License](https://img.shields.io/github/license/wieslawsoltes/ThrowsAnalyzer.svg)](LICENSE)

A Roslyn-based C# analyzer that detects exception handling patterns in your code. ThrowsAnalyzer helps identify throw statements, unhandled exceptions, and try-catch blocks across all executable member types.

## Features

ThrowsAnalyzer provides **30 diagnostic rules** organized into 6 categories, with **16 automated code fixes** for quick issue resolution.

### Diagnostic Rules Summary

| Category | Diagnostics | Description |
|----------|------------|-------------|
| **Basic Exception Handling** | THROWS001-003, 004, 007-010 | Fundamental exception patterns and anti-patterns |
| **Exception Flow Analysis** | THROWS017-019 | Method call exception propagation and documentation |
| **Async Exception Patterns** | THROWS020-022 | Async/await exception handling issues |
| **Iterator Exception Patterns** | THROWS023-024 | Exception handling in yield-based iterators |
| **Lambda Exception Patterns** | THROWS025-026 | Exception handling in lambda expressions |
| **Best Practices** | THROWS027-030 | Design patterns and performance recommendations |

### Code Fixes Summary

| Code Fix | Diagnostics | Actions |
|----------|-------------|---------|
| **Wrap in try-catch** | THROWS001, THROWS002 | Adds try-catch around throwing code |
| **Fix rethrow** | THROWS004 | Converts `throw ex;` to `throw;` |
| **Reorder catches** | THROWS007 | Reorders catch clauses from specific to general |
| **Add/Remove logging** | THROWS008, THROWS003 | Adds logging or removes empty catch |
| **Remove rethrow-only catch** | THROWS009 | Removes unnecessary catch blocks |
| **Add exception filter** | THROWS010 | Adds `when` clause to specific catches |
| **Convert async void** | THROWS021 | Converts async void to async Task |
| **Add Task observation** | THROWS022 | Adds await or continuation |
| **Wrap iterator validation** | THROWS023 | Moves validation outside iterator |
| **Add try-finally** | THROWS024 | Adds try-finally for cleanup |
| **Wrap lambda in try-catch** | THROWS025, THROWS026 | Adds exception handling to lambdas |
| **Refactor control flow** | THROWS027 | Suggests return value instead of exceptions |
| **Rename exception** | THROWS028 | Renames to follow convention |
| **Move to cold path** | THROWS029 | Suggests refactoring for performance |
| **Add XML docs** | THROWS019 | Documents thrown exceptions |
| **Suggest Result pattern** | THROWS030 | Suggests Result<T> for error handling |

## Supported Member Types

ThrowsAnalyzer analyzes exception handling patterns in:

- Methods
- Constructors and Destructors
- Properties (including expression-bodied properties)
- Property Accessors (get, set, init, add, remove)
- Operators (binary, unary, conversion)
- Local Functions
- Lambda Expressions (simple and parenthesized)
- Anonymous Methods

## Installation

### Analyzer Library

Add the analyzer to your project via NuGet:

```bash
dotnet add package ThrowsAnalyzer
```

Once installed, the analyzer runs automatically during compilation. Diagnostics will appear in your IDE and build output.

### CLI Tool

Install the command-line tool globally to analyze projects and generate reports:

```bash
dotnet tool install --global ThrowsAnalyzer.Cli
```

#### CLI Quick Start

```bash
# Analyze a project and generate reports
throws-analyzer analyze MyProject.csproj

# Analyze a solution
throws-analyzer analyze MySolution.sln

# Generate HTML and Markdown reports
throws-analyzer analyze MyProject.csproj --verbose --open
```

The CLI tool generates comprehensive reports showing:
- Summary statistics by diagnostic ID, project, severity, and file
- Interactive HTML reports with sortable tables
- Markdown reports for documentation
- Detailed diagnostics with code snippets

See [CLI Tool Documentation](docs/CLI_TOOL.md) for complete usage guide and CI/CD integration examples.

## Configuration

ThrowsAnalyzer provides granular configuration options through `.editorconfig` files. You can control analyzer enablement, severity, and which member types to analyze.

### Enabling/Disabling Individual Analyzers

Control whether each analyzer is completely enabled or disabled:

```ini
[*.cs]

# Enable/disable throw statement analyzer (THROWS001)
throws_analyzer_enable_throw_statement = true

# Enable/disable unhandled throw analyzer (THROWS002)
throws_analyzer_enable_unhandled_throw = true

# Enable/disable try-catch block analyzer (THROWS003)
throws_analyzer_enable_try_catch = true
```

All analyzers are enabled by default. Setting an option to `false` completely disables that analyzer, regardless of severity settings.

### Configuring Analyzer Severity

Control the severity of each diagnostic rule:

```ini
[*.cs]

# Basic analyzers
# THROWS001: Detects throw statements in members
dotnet_diagnostic.THROWS001.severity = suggestion

# THROWS002: Detects unhandled throw statements (not wrapped in try-catch)
dotnet_diagnostic.THROWS002.severity = warning

# THROWS003: Detects try-catch blocks in members
dotnet_diagnostic.THROWS003.severity = suggestion

# Advanced type-aware analyzers
# THROWS004: Rethrow anti-pattern (throw ex; instead of throw;)
dotnet_diagnostic.THROWS004.severity = warning

# THROWS007: Unreachable catch clause due to ordering
dotnet_diagnostic.THROWS007.severity = warning

# THROWS008: Empty catch block swallows exceptions
dotnet_diagnostic.THROWS008.severity = warning

# THROWS009: Catch block only rethrows exception
dotnet_diagnostic.THROWS009.severity = suggestion

# THROWS010: Overly broad exception catch
dotnet_diagnostic.THROWS010.severity = suggestion
```

**Severity options:** `none`, `silent`, `suggestion`, `warning`, `error`

### Configuring Member Type Analysis

Selectively enable or disable analysis for specific member types:

```ini
[*.cs]

# Analyze regular methods
throws_analyzer_analyze_methods = true

# Analyze constructors
throws_analyzer_analyze_constructors = true

# Analyze destructors/finalizers
throws_analyzer_analyze_destructors = true

# Analyze operator overloads
throws_analyzer_analyze_operators = true

# Analyze conversion operators (implicit/explicit)
throws_analyzer_analyze_conversion_operators = true

# Analyze properties (expression-bodied properties)
throws_analyzer_analyze_properties = true

# Analyze property accessors (get, set, init, add, remove)
throws_analyzer_analyze_accessors = true

# Analyze local functions
throws_analyzer_analyze_local_functions = true

# Analyze lambda expressions
throws_analyzer_analyze_lambdas = true

# Analyze anonymous methods (delegate { } syntax)
throws_analyzer_analyze_anonymous_methods = true
```

All member types are analyzed by default. Set any option to `false` to disable analysis for that member type.

### Example Configurations

#### Minimal Configuration (Methods and Constructors Only)

```ini
[*.cs]
throws_analyzer_analyze_methods = true
throws_analyzer_analyze_constructors = true
throws_analyzer_analyze_destructors = false
throws_analyzer_analyze_operators = false
throws_analyzer_analyze_conversion_operators = false
throws_analyzer_analyze_properties = false
throws_analyzer_analyze_accessors = false
throws_analyzer_analyze_local_functions = false
throws_analyzer_analyze_lambdas = false
throws_analyzer_analyze_anonymous_methods = false
```

#### Focus on Unhandled Exceptions Only

```ini
[*.cs]
throws_analyzer_enable_throw_statement = false
throws_analyzer_enable_unhandled_throw = true
throws_analyzer_enable_try_catch = false
dotnet_diagnostic.THROWS002.severity = error
```

#### Disable Analysis for Lambdas and Local Functions

```ini
[*.cs]
throws_analyzer_analyze_local_functions = false
throws_analyzer_analyze_lambdas = false
throws_analyzer_analyze_anonymous_methods = false
```

See [.editorconfig.example](.editorconfig.example) for a complete configuration template.

## Complete Diagnostic Reference

### Category 1: Basic Exception Handling (8 rules)

#### THROWS001: Method contains throw statement
**Severity:** Info | **Code Fix:** Wrap in try-catch

Detects any method or member that contains throw statements.

```csharp
// Before
void ProcessData(string data)
{
    if (string.IsNullOrEmpty(data))
        throw new ArgumentException("Data cannot be empty");
}

// After (Code Fix Applied)
void ProcessData(string data)
{
    try
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentException("Data cannot be empty");
    }
    catch (ArgumentException ex)
    {
        // Handle exception
        throw;
    }
}
```

#### THROWS002: Unhandled throw statement
**Severity:** Warning | **Code Fix:** Wrap in try-catch

Detects throw statements not wrapped in try-catch blocks.

```csharp
// Before
void SaveFile(string path, string content)
{
    File.WriteAllText(path, content); // Throws IOException
}

// After (Code Fix Applied)
void SaveFile(string path, string content)
{
    try
    {
        File.WriteAllText(path, content);
    }
    catch (IOException ex)
    {
        // Handle exception
        throw;
    }
}
```

#### THROWS003: Method contains try-catch block
**Severity:** Info | **Code Fix:** Remove try-catch or add logging

Flags methods containing try-catch blocks for tracking exception handling.

#### THROWS004: Rethrow anti-pattern
**Severity:** Warning | **Code Fix:** Fix rethrow

Detects `throw ex;` which resets the stack trace. Should use `throw;` instead.

```csharp
// Before - WRONG (resets stack trace)
try
{
    DoSomething();
}
catch (Exception ex)
{
    throw ex; // ❌ Resets stack trace
}

// After (Code Fix Applied) - CORRECT
try
{
    DoSomething();
}
catch (Exception ex)
{
    throw; // ✓ Preserves stack trace
}
```

#### THROWS007: Unreachable catch clause
**Severity:** Warning | **Code Fix:** Reorder catches

Detects catch clauses that can never be reached due to ordering.

```csharp
// Before - WRONG (InvalidOperationException is unreachable)
try
{
    DoSomething();
}
catch (Exception ex) // ❌ Catches everything
{
    Log(ex);
}
catch (InvalidOperationException ex) // Never reached
{
    LogSpecific(ex);
}

// After (Code Fix Applied) - CORRECT
try
{
    DoSomething();
}
catch (InvalidOperationException ex) // ✓ Specific first
{
    LogSpecific(ex);
}
catch (Exception ex)
{
    Log(ex);
}
```

#### THROWS008: Empty catch block
**Severity:** Warning | **Code Fix:** Add logging or remove

Detects empty catch blocks that silently swallow exceptions.

```csharp
// Before - WRONG
try
{
    LoadConfiguration();
}
catch (Exception)
{
    // ❌ Empty catch swallows exceptions
}

// After (Code Fix: Add Logging)
try
{
    LoadConfiguration();
}
catch (Exception ex)
{
    Logger.LogError(ex, "Failed to load configuration"); // ✓ Logs error
    throw;
}
```

#### THROWS009: Catch block only rethrows
**Severity:** Info | **Code Fix:** Remove unnecessary catch

Detects catch blocks that only rethrow without doing any work.

```csharp
// Before - Unnecessary
try
{
    ProcessData();
}
catch (Exception ex)
{
    throw; // No work done, catch is unnecessary
}

// After (Code Fix Applied)
ProcessData(); // ✓ Simplified
```

#### THROWS010: Overly broad exception catch
**Severity:** Info | **Code Fix:** Add exception filter

Detects catching `System.Exception` or `System.SystemException`.

```csharp
// Before - Too broad
try
{
    ParseUserInput(input);
}
catch (Exception ex) // ❌ Catches everything
{
    LogError(ex);
}

// After (Code Fix: Add Filter)
try
{
    ParseUserInput(input);
}
catch (Exception ex) when (ex is FormatException || ex is ArgumentException) // ✓ Specific
{
    LogError(ex);
}
```

### Category 2: Exception Flow Analysis (3 rules)

#### THROWS017: Unhandled method call
**Severity:** Info

Detects method calls that may throw exceptions without try-catch handling.

```csharp
// Detected
void ProcessFile(string path)
{
    var content = File.ReadAllText(path); // May throw IOException
    Process(content);
}

// Recommended
void ProcessFile(string path)
{
    try
    {
        var content = File.ReadAllText(path);
        Process(content);
    }
    catch (IOException ex)
    {
        Logger.LogError(ex, "Failed to read file: {Path}", path);
        throw;
    }
}
```

#### THROWS018: Deep exception propagation
**Severity:** Info

Detects exceptions propagating through many call stack levels.

#### THROWS019: Undocumented public API exception
**Severity:** Warning | **Code Fix:** Add XML documentation

Detects public methods that throw exceptions without XML documentation.

```csharp
// Before - Missing documentation
public void ValidateUser(string username)
{
    if (string.IsNullOrEmpty(username))
        throw new ArgumentException("Username required");
}

// After (Code Fix Applied)
/// <summary>
/// Validates the specified username.
/// </summary>
/// <param name="username">The username to validate.</param>
/// <exception cref="ArgumentException">
/// Thrown when <paramref name="username"/> is null or empty.
/// </exception>
public void ValidateUser(string username)
{
    if (string.IsNullOrEmpty(username))
        throw new ArgumentException("Username required");
}
```

### Category 3: Async Exception Patterns (3 rules)

#### THROWS020: Async method throws synchronously
**Severity:** Warning

Detects async methods that throw exceptions before the first await.

```csharp
// Before - WRONG (throws before async)
async Task<string> LoadDataAsync(string id)
{
    if (string.IsNullOrEmpty(id))
        throw new ArgumentException(); // ❌ Synchronous throw

    return await LoadFromDatabaseAsync(id);
}

// After - CORRECT
async Task<string> LoadDataAsync(string id)
{
    if (string.IsNullOrEmpty(id))
        return Task.FromException<string>(
            new ArgumentException()); // ✓ Returns faulted task

    return await LoadFromDatabaseAsync(id);
}
```

#### THROWS021: Async void exception
**Severity:** Error | **Code Fix:** Convert to async Task

Detects async void methods that can crash the application if they throw.

```csharp
// Before - WRONG (can crash app)
async void LoadDataButton_Click(object sender, EventArgs e)
{
    await LoadDataAsync(); // ❌ Exception crashes app
}

// After (Code Fix Applied) - CORRECT
async Task LoadDataButton_Click(object sender, EventArgs e)
{
    try
    {
        await LoadDataAsync(); // ✓ Exception can be handled
    }
    catch (Exception ex)
    {
        ShowError(ex.Message);
    }
}
```

#### THROWS022: Unobserved Task exception
**Severity:** Warning | **Code Fix:** Add await or continuation

Detects Task-returning methods called without await or exception handling.

```csharp
// Before - WRONG (exception unobserved)
void ProcessData()
{
    LoadDataAsync(); // ❌ Exception lost
}

// After (Code Fix Applied) - CORRECT
async Task ProcessData()
{
    await LoadDataAsync(); // ✓ Exception propagates
}
```

### Category 4: Iterator Exception Patterns (2 rules)

#### THROWS023: Iterator deferred exception
**Severity:** Info | **Code Fix:** Move validation outside iterator

Detects exceptions in yield-based iterators that are deferred until enumeration.

```csharp
// Before - WRONG (exception deferred)
IEnumerable<int> GetNumbers(int count)
{
    if (count < 0)
        throw new ArgumentException(); // ❌ Thrown during enumeration

    for (int i = 0; i < count; i++)
        yield return i;
}

// After (Code Fix Applied) - CORRECT
IEnumerable<int> GetNumbers(int count)
{
    if (count < 0)
        throw new ArgumentException(); // ✓ Thrown immediately

    return GetNumbersIterator(count);
}

IEnumerable<int> GetNumbersIterator(int count)
{
    for (int i = 0; i < count; i++)
        yield return i;
}
```

#### THROWS024: Iterator try-finally issue
**Severity:** Warning | **Code Fix:** Add proper cleanup

Detects try-finally issues in iterators where finally may not execute.

### Category 5: Lambda Exception Patterns (2 rules)

#### THROWS025: Lambda uncaught exception
**Severity:** Warning | **Code Fix:** Wrap in try-catch

Detects lambdas that throw exceptions without proper handling.

```csharp
// Before - WRONG (exception propagates to LINQ)
var results = items.Select(x => {
    if (x == null)
        throw new ArgumentNullException(); // ❌ Crashes enumeration
    return x.Value;
});

// After (Code Fix Applied) - CORRECT
var results = items.Select(x => {
    try
    {
        if (x == null)
            throw new ArgumentNullException();
        return x.Value;
    }
    catch (ArgumentNullException ex)
    {
        Logger.LogError(ex, "Null item in collection");
        return default;
    }
});
```

#### THROWS026: Event handler lambda exception
**Severity:** Error | **Code Fix:** Wrap in try-catch

Detects event handler lambdas that throw unhandled exceptions.

```csharp
// Before - WRONG (can crash app)
button.Click += (sender, e) => {
    throw new InvalidOperationException(); // ❌ Crashes app
};

// After (Code Fix Applied) - CORRECT
button.Click += (sender, e) => {
    try
    {
        ProcessClick();
    }
    catch (InvalidOperationException ex)
    {
        MessageBox.Show(ex.Message); // ✓ Handled gracefully
    }
};
```

### Category 6: Best Practices (4 rules)

#### THROWS027: Exception used for control flow
**Severity:** Info | **Code Fix:** Refactor to use return values

Detects exceptions used for normal control flow instead of return values.

```csharp
// Before - WRONG (exception for control flow)
try
{
    var user = FindUser(id);
    if (user == null)
        throw new UserNotFoundException(); // ❌ Expected condition
}
catch (UserNotFoundException)
{
    CreateDefaultUser(id);
}

// After (Code Fix Applied) - CORRECT
var user = FindUser(id);
if (user == null) // ✓ Return value check
{
    CreateDefaultUser(id);
}
```

#### THROWS028: Custom exception naming violation
**Severity:** Info | **Code Fix:** Rename exception

Detects custom exception types not ending with "Exception".

```csharp
// Before - WRONG
public class UserNotFound : Exception { } // ❌ Missing "Exception"

// After (Code Fix Applied) - CORRECT
public class UserNotFoundException : Exception { } // ✓ Follows convention
```

#### THROWS029: Exception in hot path
**Severity:** Warning | **Code Fix:** Suggest refactoring

Detects exceptions thrown inside loops (performance issue).

```csharp
// Before - WRONG (exception in loop)
for (int i = 0; i < items.Count; i++)
{
    if (items[i] == null)
        throw new ArgumentNullException(); // ❌ Performance issue
    Process(items[i]);
}

// After (Code Fix Applied) - CORRECT
// Validate before loop
for (int i = 0; i < items.Count; i++)
{
    if (items[i] == null)
        continue; // ✓ Or validate before loop starts
    Process(items[i]);
}
```

#### THROWS030: Consider Result pattern
**Severity:** Info | **Code Fix:** Suggest Result<T>

Suggests using Result<T> pattern for expected error conditions.

```csharp
// Before - Using exceptions for expected failures
public User ParseUser(string data)
{
    if (string.IsNullOrEmpty(data))
        throw new FormatException(); // Expected condition
    return JsonSerializer.Deserialize<User>(data);
}

// After - Using Result<T> pattern (suggested)
public Result<User> ParseUser(string data)
{
    if (string.IsNullOrEmpty(data))
        return Result<User>.Failure("Data cannot be empty");

    try
    {
        return Result<User>.Success(
            JsonSerializer.Deserialize<User>(data));
    }
    catch (JsonException ex)
    {
        return Result<User>.Failure(ex.Message);
    }
}
```

## Examples and Samples

For comprehensive examples demonstrating all diagnostics and code fixes, see:
- [ExceptionPatterns Sample](samples/ExceptionPatterns/) - Demonstrates all 30 diagnostics
- [LibraryManagement Sample](samples/LibraryManagement/) - Real-world library management system

## Building from Source

```bash
# Build the analyzer
dotnet build src/ThrowsAnalyzer/ThrowsAnalyzer.csproj

# Run tests
dotnet test tests/ThrowsAnalyzer.Tests/ThrowsAnalyzer.Tests.csproj
```

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
