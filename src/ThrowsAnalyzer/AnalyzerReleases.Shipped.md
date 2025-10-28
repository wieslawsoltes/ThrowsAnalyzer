## Release 1.0.0-beta.3

### New Rules

#### Basic Exception Handling (8 rules)

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
THROWS001 | Exception | Info | Method contains throw statement
THROWS002 | Exception | Warning | Method contains unhandled throw statement
THROWS003 | Exception | Info | Method contains try-catch block
THROWS004 | Exception | Warning | Rethrow anti-pattern (throw ex;)
THROWS007 | Exception | Warning | Unreachable catch clause
THROWS008 | Exception | Warning | Empty catch block swallows exceptions
THROWS009 | Exception | Info | Catch block only rethrows exception
THROWS010 | Exception | Info | Overly broad exception catch

#### Exception Flow Analysis (3 rules)

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
THROWS017 | Exception | Info | Unhandled method call
THROWS018 | Exception | Info | Deep exception propagation
THROWS019 | Exception | Warning | Undocumented public API exception

#### Async Exception Patterns (3 rules)

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
THROWS020 | Exception | Warning | Async method throws synchronously before first await
THROWS021 | Exception | Error | Async void method throws exception
THROWS022 | Exception | Warning | Unawaited Task may have unobserved exception

#### Iterator Exception Patterns (2 rules)

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
THROWS023 | Exception | Info | Iterator deferred exception (throws before yield)
THROWS024 | Exception | Warning | Iterator try-finally issue

#### Lambda Exception Patterns (2 rules)

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
THROWS025 | Exception | Warning | Lambda uncaught exception
THROWS026 | Exception | Error | Event handler lambda exception

#### Best Practices (4 rules)

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
THROWS027 | Exception | Info | Exception used for control flow
THROWS028 | Exception | Info | Custom exception naming violation
THROWS029 | Exception | Warning | Exception in hot path (loop)
THROWS030 | Exception | Info | Consider Result<T> pattern

### New Features

- **RoslynAnalyzer.Core** - Extracted reusable infrastructure library for building custom Roslyn analyzers
- **16 Code Fixes** - Automated fixes for common exception handling issues
- **Call Graph Analysis** - Tracks exception propagation through method calls
- **Async/Await Pattern Detection** - Comprehensive async exception analysis
- **Iterator Pattern Detection** - Analyzes yield-based iterators
- **Lambda Expression Analysis** - Detects exceptions in lambdas with context awareness
- **Configuration Infrastructure** - .editorconfig support for granular control
- **CLI Tool** - Command-line tool for project analysis and report generation
- **461 Tests** - Comprehensive test coverage (187 Core + 274 ThrowsAnalyzer)
