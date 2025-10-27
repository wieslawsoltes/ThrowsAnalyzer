# ThrowsAnalyzer Configuration Guide

Complete guide to configuring all 30 ThrowsAnalyzer diagnostics and 16 code fixes.

## Table of Contents

- [Quick Start](#quick-start)
- [Severity Levels](#severity-levels)
- [All Diagnostic Rules](#all-diagnostic-rules)
- [Configuration Scenarios](#configuration-scenarios)
- [Code Fix Options](#code-fix-options)
- [IDE-Specific Configuration](#ide-specific-configuration)
- [Build Integration](#build-integration)

---

## Quick Start

### Minimal Configuration

Add to your project's `.editorconfig`:

```ini
[*.cs]

# Enable ThrowsAnalyzer with recommended settings
dotnet_diagnostic.THROWS002.severity = warning  # Unhandled throws
dotnet_diagnostic.THROWS004.severity = error    # Rethrow anti-pattern
dotnet_diagnostic.THROWS008.severity = warning  # Empty catch blocks
dotnet_diagnostic.THROWS021.severity = error    # Async void
dotnet_diagnostic.THROWS026.severity = error    # Event handler exceptions
```

### Full Configuration

For complete control, see [All Diagnostic Rules](#all-diagnostic-rules) below.

---

## Severity Levels

| Severity | Build | IDE | Description | When to Use |
|----------|-------|-----|-------------|-------------|
| **error** | ❌ Fails | 🔴 Red squiggle | Must fix | Critical issues that can crash or corrupt |
| **warning** | ✅ Succeeds | 🟡 Yellow squiggle | Should fix | Issues that should be addressed |
| **suggestion** | ✅ Succeeds | 💡 Gray dots | Optional | Informational, style preferences |
| **silent** | ✅ Succeeds | No indication | Background only | Metrics, code fix availability |
| **none** | ✅ Succeeds | No indication | Disabled | Turn off specific rules |

### Recommended Severities by Diagnostic

```ini
[*.cs]

# CRITICAL (error) - Can crash application or corrupt data
dotnet_diagnostic.THROWS004.severity = error   # Rethrow anti-pattern
dotnet_diagnostic.THROWS021.severity = error   # Async void exception
dotnet_diagnostic.THROWS026.severity = error   # Event handler lambda exception

# IMPORTANT (warning) - Should be fixed
dotnet_diagnostic.THROWS002.severity = warning # Unhandled throws
dotnet_diagnostic.THROWS007.severity = warning # Unreachable catch
dotnet_diagnostic.THROWS008.severity = warning # Empty catch block
dotnet_diagnostic.THROWS017.severity = warning # Unhandled method call
dotnet_diagnostic.THROWS019.severity = warning # Undocumented public exception
dotnet_diagnostic.THROWS020.severity = warning # Async synchronous throw
dotnet_diagnostic.THROWS022.severity = warning # Unobserved Task
dotnet_diagnostic.THROWS023.severity = warning # Deferred iterator exception
dotnet_diagnostic.THROWS025.severity = warning # Lambda uncaught exception
dotnet_diagnostic.THROWS027.severity = warning # Exception for control flow
dotnet_diagnostic.THROWS028.severity = warning # Custom exception naming
dotnet_diagnostic.THROWS029.severity = warning # Exception in hot path

# INFORMATIONAL (suggestion) - Optional improvements
dotnet_diagnostic.THROWS001.severity = suggestion # Method contains throw
dotnet_diagnostic.THROWS003.severity = suggestion # Try-catch present
dotnet_diagnostic.THROWS009.severity = suggestion # Catch only rethrows
dotnet_diagnostic.THROWS010.severity = suggestion # Overly broad catch
dotnet_diagnostic.THROWS018.severity = suggestion # Deep propagation
dotnet_diagnostic.THROWS024.severity = suggestion # Iterator timing
dotnet_diagnostic.THROWS030.severity = suggestion # Result pattern suggestion
```

---

## All Diagnostic Rules

### Category 1: Basic Exception Handling (THROWS001-010)

```ini
# THROWS001: Method contains throw statement
# Default: suggestion | Code Fix: ✅ Wrap in try-catch
# Use: Track which methods throw exceptions (informational)
dotnet_diagnostic.THROWS001.severity = suggestion

# THROWS002: Method contains unhandled throw statement
# Default: warning | Code Fix: ✅ Wrap in try-catch
# Use: Detect throws not enclosed in try-catch
dotnet_diagnostic.THROWS002.severity = warning

# THROWS003: Method contains try/catch block
# Default: suggestion | Code Fix: ✅ Remove try-catch, Add logging
# Use: Track exception handling locations (informational)
dotnet_diagnostic.THROWS003.severity = suggestion

# THROWS004: Rethrow anti-pattern (throw ex; instead of throw;)
# Default: error | Code Fix: ✅ Replace with 'throw;'
# Use: Prevent stack trace loss - CRITICAL BUG
dotnet_diagnostic.THROWS004.severity = error

# THROWS007: Unreachable catch clause due to ordering
# Default: warning | Code Fix: ✅ Reorder catch clauses
# Use: Fix catch clause ordering (base types must come after derived)
dotnet_diagnostic.THROWS007.severity = warning

# THROWS008: Empty catch block swallows exceptions
# Default: warning | Code Fix: ✅ Remove catch, Add logging
# Use: Prevent silent exception swallowing
dotnet_diagnostic.THROWS008.severity = warning

# THROWS009: Catch block only rethrows exception
# Default: suggestion | Code Fix: ✅ Remove unnecessary catch
# Use: Remove useless catch-rethrow patterns
dotnet_diagnostic.THROWS009.severity = suggestion

# THROWS010: Overly broad exception catch
# Default: suggestion | Code Fix: ✅ Add 'when' filter
# Use: Encourage catching specific exceptions
dotnet_diagnostic.THROWS010.severity = suggestion
```

### Category 2: Exception Flow Analysis (THROWS017-019)

```ini
# THROWS017: Unhandled method call exception
# Default: warning | Code Fix: ✅ Wrap in try-catch, Add XML doc
# Use: Detect when called method's exception is not caught
dotnet_diagnostic.THROWS017.severity = warning

# THROWS018: Deep exception propagation
# Default: suggestion | Code Fix: ❌ None (informational)
# Use: Track exceptions propagating through many methods
dotnet_diagnostic.THROWS018.severity = suggestion

# THROWS019: Undocumented public exception
# Default: warning | Code Fix: ✅ Add XML exception documentation
# Use: Enforce XML documentation for public API exceptions
dotnet_diagnostic.THROWS019.severity = warning
```

### Category 3: Async Exception Patterns (THROWS020-022)

```ini
# THROWS020: Async synchronous throw
# Default: warning | Code Fix: ✅ Add Task.Yield(), Extract wrapper
# Use: Detect synchronous throws in async methods
dotnet_diagnostic.THROWS020.severity = warning

# THROWS021: Async void exception
# Default: error | Code Fix: ✅ Convert to async Task, Wrap in try-catch
# Use: Prevent app crashes from unobserved async void exceptions - CRITICAL
dotnet_diagnostic.THROWS021.severity = error

# THROWS022: Unobserved Task exception
# Default: warning | Code Fix: ✅ Add await, Assign to variable, Add ContinueWith
# Use: Ensure Task exceptions are observed
dotnet_diagnostic.THROWS022.severity = warning
```

### Category 4: Iterator Exception Patterns (THROWS023-024)

```ini
# THROWS023: Deferred iterator exception
# Default: warning | Code Fix: ❌ None (complex refactoring)
# Use: Warn about deferred exception execution in yield methods
dotnet_diagnostic.THROWS023.severity = warning

# THROWS024: Iterator try-finally timing
# Default: suggestion | Code Fix: ❌ None (informational)
# Use: Inform about deferred finally execution in iterators
dotnet_diagnostic.THROWS024.severity = suggestion
```

### Category 5: Lambda Exception Patterns (THROWS025-026)

```ini
# THROWS025: Lambda uncaught exception
# Default: warning | Code Fix: ❌ None (context-dependent)
# Use: Detect uncaught exceptions in LINQ, Task.Run, callbacks
dotnet_diagnostic.THROWS025.severity = warning

# THROWS026: Event handler lambda exception
# Default: error | Code Fix: ❌ None (context-dependent)
# Use: Prevent app crashes from event handler exceptions - CRITICAL
dotnet_diagnostic.THROWS026.severity = error
```

### Category 6: Best Practices (THROWS027-030)

```ini
# THROWS027: Exception used for control flow
# Default: warning | Code Fix: ❌ None (architectural refactoring)
# Use: Discourage using exceptions for normal program flow
dotnet_diagnostic.THROWS027.severity = warning

# THROWS028: Custom exception naming convention
# Default: warning | Code Fix: ✅ Rename to include "Exception" suffix
# Use: Enforce .NET naming conventions for exception types
dotnet_diagnostic.THROWS028.severity = warning

# THROWS029: Exception in hot path (loop)
# Default: warning | Code Fix: ✅ Move validation before loop, Convert to return
# Use: Prevent performance issues from exceptions in loops
dotnet_diagnostic.THROWS029.severity = warning

# THROWS030: Result pattern suggestion
# Default: suggestion | Code Fix: ✅ Add comment, Convert to bool return
# Use: Suggest Result<T> pattern for expected validation errors
dotnet_diagnostic.THROWS030.severity = suggestion
```

---

## Configuration Scenarios

### 1. Production Application (Strict)

Maximum safety, all critical issues as errors:

```ini
[*.cs]

# Critical - Build fails
dotnet_diagnostic.THROWS004.severity = error  # Rethrow anti-pattern
dotnet_diagnostic.THROWS008.severity = error  # Empty catch blocks
dotnet_diagnostic.THROWS021.severity = error  # Async void
dotnet_diagnostic.THROWS026.severity = error  # Event handler exceptions

# Important - Warnings
dotnet_diagnostic.THROWS002.severity = warning
dotnet_diagnostic.THROWS007.severity = warning
dotnet_diagnostic.THROWS017.severity = warning
dotnet_diagnostic.THROWS019.severity = warning
dotnet_diagnostic.THROWS020.severity = warning
dotnet_diagnostic.THROWS022.severity = warning
dotnet_diagnostic.THROWS023.severity = warning
dotnet_diagnostic.THROWS025.severity = warning
dotnet_diagnostic.THROWS027.severity = warning
dotnet_diagnostic.THROWS028.severity = warning
dotnet_diagnostic.THROWS029.severity = warning

# Informational - Suggestions
dotnet_diagnostic.THROWS001.severity = suggestion
dotnet_diagnostic.THROWS003.severity = suggestion
dotnet_diagnostic.THROWS009.severity = suggestion
dotnet_diagnostic.THROWS010.severity = suggestion
dotnet_diagnostic.THROWS018.severity = suggestion
dotnet_diagnostic.THROWS024.severity = suggestion
dotnet_diagnostic.THROWS030.severity = suggestion
```

### 2. Library/SDK (Documentation Focused)

Emphasize public API documentation:

```ini
[*.cs]

# Critical
dotnet_diagnostic.THROWS004.severity = error
dotnet_diagnostic.THROWS021.severity = error

# Public API Documentation - Error
dotnet_diagnostic.THROWS019.severity = error  # Undocumented public exceptions

# Important
dotnet_diagnostic.THROWS002.severity = warning
dotnet_diagnostic.THROWS008.severity = warning
dotnet_diagnostic.THROWS017.severity = warning

# Relaxed for internal code
dotnet_diagnostic.THROWS001.severity = silent
dotnet_diagnostic.THROWS003.severity = silent
dotnet_diagnostic.THROWS009.severity = suggestion
```

### 3. Learning/Training Environment

Show all diagnostics for educational purposes:

```ini
[*.cs]

# Make all diagnostics visible as warnings
dotnet_diagnostic.THROWS001.severity = warning
dotnet_diagnostic.THROWS002.severity = warning
dotnet_diagnostic.THROWS003.severity = warning
dotnet_diagnostic.THROWS004.severity = warning
dotnet_diagnostic.THROWS007.severity = warning
dotnet_diagnostic.THROWS008.severity = warning
dotnet_diagnostic.THROWS009.severity = warning
dotnet_diagnostic.THROWS010.severity = warning
dotnet_diagnostic.THROWS017.severity = warning
dotnet_diagnostic.THROWS018.severity = warning
dotnet_diagnostic.THROWS019.severity = warning
dotnet_diagnostic.THROWS020.severity = warning
dotnet_diagnostic.THROWS021.severity = warning
dotnet_diagnostic.THROWS022.severity = warning
dotnet_diagnostic.THROWS023.severity = warning
dotnet_diagnostic.THROWS024.severity = warning
dotnet_diagnostic.THROWS025.severity = warning
dotnet_diagnostic.THROWS026.severity = warning
dotnet_diagnostic.THROWS027.severity = warning
dotnet_diagnostic.THROWS028.severity = warning
dotnet_diagnostic.THROWS029.severity = warning
dotnet_diagnostic.THROWS030.severity = warning
```

### 4. Performance-Critical Application

Focus on performance issues:

```ini
[*.cs]

# Performance Issues - Error
dotnet_diagnostic.THROWS029.severity = error  # Exception in hot path
dotnet_diagnostic.THROWS027.severity = error  # Exception for control flow

# Critical Safety
dotnet_diagnostic.THROWS004.severity = error
dotnet_diagnostic.THROWS021.severity = error
dotnet_diagnostic.THROWS026.severity = error

# Other warnings
dotnet_diagnostic.THROWS002.severity = warning
dotnet_diagnostic.THROWS008.severity = warning
```

### 5. Legacy Code Migration

Gradual adoption with minimal disruption:

```ini
[*.cs]

# Start with only critical errors
dotnet_diagnostic.THROWS004.severity = error
dotnet_diagnostic.THROWS021.severity = error
dotnet_diagnostic.THROWS026.severity = error

# Everything else as suggestions (visible in IDE only)
dotnet_diagnostic.THROWS*.severity = suggestion

# Disable specific rules causing too much noise
dotnet_diagnostic.THROWS001.severity = none  # Too many throws in legacy code
dotnet_diagnostic.THROWS003.severity = none  # Too many try-catches
```

### 6. Web Application (ASP.NET Core)

Focus on request handling and async patterns:

```ini
[*.cs]

# Critical for web apps
dotnet_diagnostic.THROWS021.severity = error  # Async void (fire-and-forget)
dotnet_diagnostic.THROWS026.severity = error  # Event handlers
dotnet_diagnostic.THROWS004.severity = error  # Stack trace preservation

# Async patterns - Important
dotnet_diagnostic.THROWS020.severity = warning
dotnet_diagnostic.THROWS022.severity = warning

# Request handling
dotnet_diagnostic.THROWS002.severity = warning
dotnet_diagnostic.THROWS008.severity = warning
dotnet_diagnostic.THROWS019.severity = warning  # API documentation
```

---

## Code Fix Options

ThrowsAnalyzer provides 16 automated code fixes. Configure which fixes are available:

### Code Fix Behavior

Code fixes are automatically available when diagnostics are enabled. No additional configuration needed.

### Available Code Fixes

| Diagnostic | Code Fix Actions | Equivalence Key |
|------------|------------------|-----------------|
| THROWS001 | Wrap in try-catch | `MethodThrowsCodeFixProvider:WrapInTryCatch` |
| THROWS002 | Wrap in try-catch | `UnhandledThrowsCodeFixProvider:WrapInTryCatch` |
| THROWS003 | Remove try-catch<br>Add logging | `TryCatchCodeFixProvider:*` |
| THROWS004 | Replace `throw ex;` with `throw;` | `RethrowAntiPatternCodeFixProvider:ReplaceWithBareRethrow` |
| THROWS007 | Reorder catch clauses | `CatchClauseOrderingCodeFixProvider:ReorderCatchClauses` |
| THROWS008 | Remove empty catch<br>Add logging | `EmptyCatchCodeFixProvider:*` |
| THROWS009 | Remove unnecessary catch | `RethrowOnlyCatchCodeFixProvider:RemoveCatch` |
| THROWS010 | Add `when` filter | `OverlyBroadCatchCodeFixProvider:AddWhenFilter` |
| THROWS017 | Wrap in try-catch<br>Add XML doc | `UnhandledMethodCallCodeFixProvider:*` |
| THROWS019 | Add XML exception doc | `UndocumentedPublicExceptionCodeFixProvider:AddDocumentation` |
| THROWS020 | Add Task.Yield()<br>Extract wrapper | `AsyncSynchronousThrowCodeFixProvider:*` |
| THROWS021 | Convert to async Task<br>Wrap in try-catch | `AsyncVoidExceptionCodeFixProvider:*` |
| THROWS022 | Add await<br>Assign to variable<br>Add ContinueWith | `UnobservedTaskExceptionCodeFixProvider:*` |
| THROWS028 | Rename exception type | `CustomExceptionNamingCodeFixProvider:RenameExceptionType` |
| THROWS029 | Move before loop<br>Convert to return | `ExceptionInHotPathCodeFixProvider:*` |
| THROWS030 | Add comment<br>Convert to bool | `ResultPatternCodeFixProvider:*` |

### Batch Fixing

All code fixes support "Fix All" operations:
- **Fix All in Document** - Ctrl+.
- **Fix All in Project**
- **Fix All in Solution**

---

## IDE-Specific Configuration

### Visual Studio 2022

1. **Tools** > **Options** > **Text Editor** > **C#** > **Code Style** > **Formatting**
2. Navigate to **Analyzer Settings**
3. Configure individual ThrowsAnalyzer rules

Or edit `.editorconfig` directly (recommended).

### Visual Studio Code

1. Install C# extension
2. Create/edit `.editorconfig` in project root
3. Rules automatically apply

### JetBrains Rider

1. **File** > **Settings** > **Editor** > **Code Style** > **C#**
2. Navigate to **Inspection Severity**
3. Search for "THROWS" to find all rules

Or edit `.editorconfig` (recommended).

---

## Build Integration

### CI/CD Pipeline

Treat warnings as errors in CI:

```bash
# .NET CLI
dotnet build /p:TreatWarningsAsErrors=true

# MSBuild
msbuild /p:TreatWarningsAsErrors=true
```

### Selective Warning as Error

Only specific diagnostics as errors:

```xml
<!-- .csproj -->
<PropertyGroup>
  <WarningsAsErrors>THROWS004;THROWS021;THROWS026</WarningsAsErrors>
</PropertyGroup>
```

### Suppress Specific Diagnostics in Build

```xml
<PropertyGroup>
  <NoWarn>THROWS001;THROWS003</NoWarn>
</PropertyGroup>
```

### Per-File Configuration

Disable for specific files:

```csharp
#pragma warning disable THROWS001 // Method contains throw
public void LegacyMethod()
{
    throw new NotImplementedException();
}
#pragma warning restore THROWS001
```

---

## Advanced Configuration

### Conditional Configuration by Project Type

```ini
# Different settings for tests
[*Tests/**/*.cs]
dotnet_diagnostic.THROWS001.severity = none
dotnet_diagnostic.THROWS002.severity = suggestion

# Strict for public API
[src/PublicApi/**/*.cs]
dotnet_diagnostic.THROWS019.severity = error
```

### Custom Severity by Namespace

Use `#pragma` directives or `.editorconfig` wildcards:

```ini
# Relax rules in generated code
[*Generated/**/*.cs]
dotnet_diagnostic.THROWS*.severity = none

# Strict in core domain
[src/Domain/**/*.cs]
dotnet_diagnostic.THROWS002.severity = error
dotnet_diagnostic.THROWS004.severity = error
dotnet_diagnostic.THROWS008.severity = error
```

---

## Configuration Validation

### Verify Configuration

Build and check for diagnostics:

```bash
dotnet build /v:detailed 2>&1 | grep THROWS
```

### List Active Diagnostics

```bash
dotnet build /p:ReportAnalyzer=true
```

---

## Troubleshooting

### Diagnostics Not Appearing

1. **Check .editorconfig location** - Must be in project root or solution root
2. **Verify severity** - `none` or `silent` won't show in IDE
3. **Restart IDE** - Sometimes required after config changes
4. **Check analyzer installation**:
   ```bash
   dotnet list package --include-transitive | grep ThrowsAnalyzer
   ```

### Code Fixes Not Working

1. **Verify diagnostic is enabled** - Code fixes only appear for active diagnostics
2. **Check equivalence key** - Ensure using correct provider
3. **Restart IDE** - May be needed after NuGet updates

### Performance Issues

If analyzer slows down IDE:

```ini
# Disable expensive analyzers
dotnet_diagnostic.THROWS017.severity = none  # Call graph analysis
dotnet_diagnostic.THROWS018.severity = none  # Deep propagation tracking
```

---

## Best Practices

1. **Start strict, relax as needed** - Easier than the reverse
2. **Document exceptions** - Use THROWS019 to enforce
3. **Critical as errors** - THROWS004, THROWS021, THROWS026
4. **Use .editorconfig** - Version control your configuration
5. **Review regularly** - Audit which rules are helping/hindering
6. **Team consistency** - Share .editorconfig across team
7. **CI enforcement** - Use build-time warnings-as-errors

---

## Examples Repository

See `samples/ExceptionPatterns` for a complete working example with all diagnostics configured.

---

## Related Documentation

- [Main README](../README.md) - Overview and installation
- [Project Status](PROJECT_STATUS.md) - Implementation details
- [Sample README](../samples/ExceptionPatterns/README.md) - Working examples

---

**ThrowsAnalyzer** - Configure comprehensive exception analysis for your needs! 🔧
