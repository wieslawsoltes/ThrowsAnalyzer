# ThrowsAnalyzer

**Comprehensive Exception Analysis for C# with Automated Code Fixes**

[![NuGet](https://img.shields.io/nuget/v/ThrowsAnalyzer.svg)](https://www.nuget.org/packages/ThrowsAnalyzer/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## What is ThrowsAnalyzer?

ThrowsAnalyzer is a production-ready Roslyn analyzer that provides comprehensive exception handling analysis for C# codebases. It detects exception anti-patterns, enforces best practices, and provides automated code fixes—all integrated directly into your IDE.

## Key Features

✅ **30 Diagnostic Rules** covering all exception patterns
✅ **16 Automated Code Fixes** for one-click issue resolution
✅ **Exception Flow Analysis** across method calls
✅ **Async/Await Pattern Detection** for safe async code
✅ **Iterator Exception Analysis** for yield-based methods
✅ **Lambda & Event Handler** exception detection
✅ **Performance Analysis** for exceptions in hot paths
✅ **Best Practices Enforcement** (naming, patterns, design)

## Quick Start

### Installation

```bash
dotnet add package ThrowsAnalyzer
```

Or via NuGet Package Manager:
```
Install-Package ThrowsAnalyzer
```

### Usage

Once installed, ThrowsAnalyzer automatically analyzes your code and provides:
- **Squiggly underlines** for detected issues
- **Light bulb suggestions** for automated fixes
- **Batch fixing** (Fix All in Document/Project/Solution)

## What It Detects

### Exception Handling Issues
- ❌ Unhandled exceptions
- ❌ Missing try-catch blocks
- ❌ Empty catch blocks (exception swallowing)
- ❌ Unreachable catch clauses
- ❌ Rethrow anti-patterns (`throw ex;` vs `throw;`)

### Async/Await Problems
- ❌ `async void` methods that throw
- ❌ Unobserved `Task` exceptions
- ❌ Synchronous throws in async methods

### Performance Issues
- ❌ Exceptions in loops (hot path)
- ❌ Exception-based control flow

### Best Practices
- ❌ Custom exceptions without "Exception" suffix
- ❌ Undocumented public API exceptions
- ❌ Methods that should use Result<T> pattern

## Example Code Fixes

### Fix Rethrow Anti-Pattern
```csharp
// Before
catch (Exception ex)
{
    throw ex; // ❌ Resets stack trace
}

// After (one-click fix)
catch (Exception ex)
{
    throw; // ✅ Preserves stack trace
}
```

### Convert Async Void to Async Task
```csharp
// Before
async void ProcessData() // ❌ May crash app
{
    await DoWorkAsync();
}

// After (one-click fix)
async Task ProcessData() // ✅ Exceptions can be observed
{
    await DoWorkAsync();
}
```

### Move Exception Outside Loop
```csharp
// Before
foreach (var item in items)
{
    if (item < 0)
        throw new ArgumentException(); // ❌ Performance issue
}

// After (one-click fix)
if (items.Any(i => i < 0))
    throw new ArgumentException(); // ✅ Validate before loop

foreach (var item in items)
{
    Process(item);
}
```

## All Diagnostic Rules (THROWS001-030)

| ID | Category | Description |
|----|----------|-------------|
| THROWS001-003 | Basic | Method throws, unhandled throws, try-catch blocks |
| THROWS004 | Patterns | Rethrow anti-pattern detection |
| THROWS007-010 | Catch Clauses | Ordering, empty, rethrow-only, overly broad |
| THROWS017-019 | Flow Analysis | Unhandled calls, deep propagation, undocumented |
| THROWS020-022 | Async | Sync throw, async void, unobserved tasks |
| THROWS023-024 | Iterators | Deferred exceptions, try-finally timing |
| THROWS025-026 | Lambdas | Uncaught exceptions, event handlers |
| THROWS027-030 | Best Practices | Control flow, naming, hot path, Result<T> |

## Configuration

Configure via `.editorconfig`:

```ini
# Set severity levels
dotnet_diagnostic.THROWS001.severity = suggestion
dotnet_diagnostic.THROWS004.severity = warning
dotnet_diagnostic.THROWS021.severity = error

# Disable specific rules
dotnet_diagnostic.THROWS030.severity = none
```

## IDE Support

- ✅ **Visual Studio 2022** (and newer)
- ✅ **Visual Studio Code** with C# extension
- ✅ **JetBrains Rider**
- ✅ **Command Line** via `dotnet build`

## Why ThrowsAnalyzer?

### For Teams
- Enforce consistent exception handling patterns
- Catch issues during code review automatically
- Reduce production bugs related to exceptions

### For CI/CD
- Integrate into build pipelines
- Fail builds on critical exception issues
- Generate reports on exception handling quality

### For Learning
- Learn exception best practices
- Understand async/await pitfalls
- See suggested improvements with explanations

## Documentation

Full documentation available at:
- **GitHub**: https://github.com/wieslawsoltes/ThrowsAnalyzer
- **Diagnostic Reference**: See docs folder for detailed rule descriptions
- **Examples**: Sample projects demonstrating all features

## Statistics

- **30** diagnostic rules
- **16** automated code fixes
- **269** unit tests (100% passing)
- **10+** supported member types (methods, properties, lambdas, etc.)

## Contributing

Contributions welcome! Please see CONTRIBUTING.md for guidelines.

## License

MIT License - see LICENSE file for details.

## Support

- **Issues**: https://github.com/wieslawsoltes/ThrowsAnalyzer/issues
- **Discussions**: https://github.com/wieslawsoltes/ThrowsAnalyzer/discussions

---

**ThrowsAnalyzer** - Write safer C# code with comprehensive exception analysis.
