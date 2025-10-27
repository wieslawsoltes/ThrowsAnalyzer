; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
THROWS004 | Usage | Warning | Rethrow modifies stack trace
THROWS007 | Usage | Warning | Unreachable catch clause
THROWS008 | Usage | Warning | Empty catch block swallows exceptions
THROWS009 | Usage | Info | Catch block only rethrows exception
THROWS010 | Usage | Info | Overly broad exception catch
