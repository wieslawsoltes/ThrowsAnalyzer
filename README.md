# ThrowsAnalyzer

[![CI](https://github.com/wieslawsoltes/ThrowsAnalyzer/actions/workflows/ci.yml/badge.svg)](https://github.com/wieslawsoltes/ThrowsAnalyzer/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/ThrowsAnalyzer.svg)](https://www.nuget.org/packages/ThrowsAnalyzer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ThrowsAnalyzer.svg)](https://www.nuget.org/packages/ThrowsAnalyzer)
[![License](https://img.shields.io/github/license/wieslawsoltes/ThrowsAnalyzer.svg)](LICENSE)

A Roslyn-based C# analyzer that detects exception handling patterns in your code. ThrowsAnalyzer helps identify throw statements, unhandled exceptions, and try-catch blocks across all executable member types.

## Features

### Basic Exception Detection

- **THROWS001**: Detects methods and members containing throw statements
- **THROWS002**: Identifies unhandled throw statements (throws outside try-catch blocks)
- **THROWS003**: Flags methods and members containing try-catch blocks

### Advanced Type-Aware Analysis

ThrowsAnalyzer includes semantic model-based exception type analysis for more precise diagnostics:

- **THROWS004**: Detects rethrow anti-pattern (`throw ex;` instead of `throw;`) which modifies stack trace
- **THROWS007**: Detects unreachable catch clauses due to ordering issues
- **THROWS008**: Detects empty catch blocks that swallow exceptions
- **THROWS009**: Detects catch blocks that only rethrow without doing any work
- **THROWS010**: Detects overly broad exception catches (`System.Exception` or `System.SystemException`)

### Automated Code Fixes

ThrowsAnalyzer provides intelligent code fixes for all diagnostics:

| Diagnostic | Code Fix Options |
|------------|------------------|
| **THROWS001** | Wrap throws in try-catch block |
| **THROWS002** | Wrap unhandled throws in try-catch block |
| **THROWS003** | Remove try-catch block, Add logging to empty catches |
| **THROWS004** | Replace `throw ex;` with `throw;` |
| **THROWS007** | Reorder catch clauses (specific to general) |
| **THROWS008** | Remove empty catch, Add logging to catch |
| **THROWS009** | Remove unnecessary rethrow-only catch |
| **THROWS010** | Add exception filter (`when` clause) |

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

Add the analyzer to your project via NuGet:

```bash
dotnet add package ThrowsAnalyzer
```

## Usage

Once installed, the analyzer runs automatically during compilation. Diagnostics will appear in your IDE and build output.

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

## Building from Source

```bash
# Build the analyzer
dotnet build src/ThrowsAnalyzer/ThrowsAnalyzer.csproj

# Run tests
dotnet test tests/ThrowsAnalyzer.Tests/ThrowsAnalyzer.Tests.csproj
```

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
