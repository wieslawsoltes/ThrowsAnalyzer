# Phase 4.6 & 4.7: Performance Optimization and Enhanced Configuration

## Overview

Phases 4.6 and 4.7 extend ThrowsAnalyzer beyond core functionality to provide:
- Performance optimization and diagnostic reporting
- Enhanced configuration options
- Suppression attribute support
- Benchmark suite for validation

## Phase 4.6: Performance Optimization & Telemetry

### Objectives

1. **Performance Benchmarking**: Create benchmark suite to measure analyzer performance
2. **Cache Optimization**: Implement caching for expensive semantic model queries
3. **Diagnostic Reporting**: Add performance telemetry option
4. **Optimization**: Reduce allocations and improve analysis speed

### Scope

#### 4.6.1 Benchmark Suite

Create comprehensive benchmarks using BenchmarkDotNet:

**Benchmarks to implement:**
- Analyzer initialization time
- Per-file analysis time (small, medium, large files)
- Code fix application time
- Semantic model query performance
- Memory allocation profiling

**Expected metrics:**
- Baseline performance measurements
- Performance regression detection
- Comparison with other popular analyzers

#### 4.6.2 Performance Optimizations

**Caching layer:**
- Cache `ITypeSymbol` lookups for common exception types
- Cache exception hierarchy calculations
- Reuse semantic model queries within same analysis

**Allocation reduction:**
- Use object pooling for frequently created objects
- Reduce LINQ allocations in hot paths
- Use `stackalloc` where appropriate

**Algorithm improvements:**
- Early exit conditions in detectors
- Avoid redundant syntax tree traversals
- Batch semantic model queries

#### 4.6.3 Telemetry (Optional)

**Performance logging:**
- Optional diagnostic performance logging
- Configurable via `.editorconfig`
- Output to build log or file

**Metrics tracked:**
- Analysis time per diagnostic
- Code fix application time
- Cache hit rates
- Memory usage

### Deliverables

1. BenchmarkDotNet project: `benchmarks/ThrowsAnalyzer.Benchmarks`
2. Performance optimization implementations
3. Benchmark results documentation
4. Performance guidelines for users

### Success Criteria

- [ ] Benchmark suite with 10+ scenarios
- [ ] Analysis time < 50ms per file (1000 LOC)
- [ ] Code fix application < 20ms
- [ ] Memory allocations reduced by 30%
- [ ] No performance regressions vs baseline

## Phase 4.7: Enhanced Configuration & Suppressions

### Objectives

1. **Suppression Attributes**: Support custom attributes to suppress diagnostics
2. **Enhanced Configuration**: More granular `.editorconfig` options
3. **Rule Sets**: Predefined configuration profiles
4. **Documentation**: Comprehensive configuration guide

### Scope

#### 4.7.1 Suppression Attribute Support

**Custom attribute:**
```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | ...)]
public class SuppressThrowsAnalysisAttribute : Attribute
{
    public string[] Rules { get; set; }
    public string Justification { get; set; }
}
```

**Usage:**
```csharp
[SuppressThrowsAnalysis(Rules = new[] { "THROWS001", "THROWS002" },
                        Justification = "Exception handling is intentional")]
public void MethodWithIntentionalThrow()
{
    throw new InvalidOperationException();
}
```

**Implementation:**
- Check for attribute in analyzers
- Suppress specified diagnostics
- Support wildcard: `THROWS*` to suppress all

#### 4.7.2 Enhanced Configuration Options

**Code fix preferences:**
```ini
[*.cs]

# Code fix behavior
throws_analyzer_codefix_prefer_logging = true
throws_analyzer_codefix_logging_framework = Console.WriteLine|ILogger|Debug
throws_analyzer_codefix_exception_variable_name = ex|exception|e
throws_analyzer_codefix_add_todo_comments = true
throws_analyzer_codefix_todo_format = TODO:|FIXME:|NOTE:

# Try-catch wrapping preferences
throws_analyzer_codefix_catch_exception_type = specific|general
throws_analyzer_codefix_wrap_entire_method = true|false
throws_analyzer_codefix_add_exception_message = true

# Catch clause preferences
throws_analyzer_codefix_remove_empty_catches = true
throws_analyzer_codefix_remove_rethrow_only = true
```

**Diagnostic filtering:**
```ini
# Exclude specific patterns
throws_analyzer_exclude_test_methods = true
throws_analyzer_exclude_async_methods = false
throws_analyzer_exclude_finalizers = true

# Exception type filtering
throws_analyzer_ignore_exception_types = NotImplementedException,NotSupportedException
```

#### 4.7.3 Configuration Profiles

**Predefined rule sets:**

**Strict profile:**
```ini
# .editorconfig.strict
[*.cs]
dotnet_diagnostic.THROWS001.severity = warning
dotnet_diagnostic.THROWS002.severity = error
dotnet_diagnostic.THROWS003.severity = none
dotnet_diagnostic.THROWS004.severity = error
dotnet_diagnostic.THROWS007.severity = error
dotnet_diagnostic.THROWS008.severity = error
dotnet_diagnostic.THROWS009.severity = warning
dotnet_diagnostic.THROWS010.severity = warning
```

**Minimal profile:**
```ini
# .editorconfig.minimal
[*.cs]
dotnet_diagnostic.THROWS001.severity = none
dotnet_diagnostic.THROWS002.severity = warning
dotnet_diagnostic.THROWS003.severity = none
dotnet_diagnostic.THROWS004.severity = error
dotnet_diagnostic.THROWS007.severity = error
dotnet_diagnostic.THROWS008.severity = error
dotnet_diagnostic.THROWS009.severity = none
dotnet_diagnostic.THROWS010.severity = none
```

**Recommended profile:**
```ini
# .editorconfig.recommended (default)
[*.cs]
dotnet_diagnostic.THROWS001.severity = suggestion
dotnet_diagnostic.THROWS002.severity = warning
dotnet_diagnostic.THROWS003.severity = suggestion
dotnet_diagnostic.THROWS004.severity = warning
dotnet_diagnostic.THROWS007.severity = warning
dotnet_diagnostic.THROWS008.severity = warning
dotnet_diagnostic.THROWS009.severity = suggestion
dotnet_diagnostic.THROWS010.severity = suggestion
```

#### 4.7.4 Configuration Documentation

**Create comprehensive guide:**
- Configuration option reference
- Examples for common scenarios
- Migration guide from other analyzers
- Best practices

### Deliverables

1. `SuppressThrowsAnalysisAttribute` implementation
2. Enhanced configuration option parsing
3. Three predefined configuration profiles
4. Configuration guide: `docs/CONFIGURATION_GUIDE.md`
5. Migration examples

### Success Criteria

- [ ] Suppression attribute works across all analyzers
- [ ] All configuration options functional
- [ ] Three profiles tested and documented
- [ ] Configuration guide complete (20+ examples)
- [ ] Backward compatible with existing configurations

## Implementation Timeline

### Phase 4.6 (1-2 days)
1. **Day 1**: Create benchmark project, implement base benchmarks
2. **Day 2**: Add performance optimizations, document results

### Phase 4.7 (1-2 days)
1. **Day 1**: Implement suppression attribute, enhance configuration parsing
2. **Day 2**: Create configuration profiles, write documentation

**Total: 2-4 days**

## Technical Design

### Phase 4.6: Caching Architecture

```csharp
public class ExceptionTypeCache
{
    private static readonly ConcurrentDictionary<string, ITypeSymbol> _typeCache = new();

    public static ITypeSymbol? GetExceptionType(
        SemanticModel semanticModel,
        string typeName)
    {
        string key = $"{semanticModel.Compilation.AssemblyName}::{typeName}";
        return _typeCache.GetOrAdd(key, _ =>
            semanticModel.Compilation.GetTypeByMetadataName(typeName));
    }
}
```

### Phase 4.7: Suppression Attribute

```csharp
public static class SuppressionAnalyzer
{
    public static bool IsSuppressed(
        SyntaxNodeAnalysisContext context,
        string diagnosticId,
        SyntaxNode node)
    {
        // Check for [SuppressThrowsAnalysis] attribute
        var memberSymbol = GetMemberSymbol(context, node);
        if (memberSymbol == null) return false;

        var attributes = memberSymbol.GetAttributes();
        foreach (var attr in attributes)
        {
            if (attr.AttributeClass?.Name == "SuppressThrowsAnalysisAttribute")
            {
                var rules = GetRulesFromAttribute(attr);
                if (rules.Contains(diagnosticId) || rules.Contains("THROWS*"))
                    return true;
            }
        }

        return false;
    }
}
```

### Phase 4.7: Enhanced Configuration

```csharp
public class ThrowsAnalyzerConfig
{
    public bool PreferLogging { get; set; } = true;
    public string LoggingFramework { get; set; } = "Console.WriteLine";
    public string ExceptionVariableName { get; set; } = "ex";
    public bool AddTodoComments { get; set; } = true;
    public string TodoFormat { get; set; } = "TODO:";

    public static ThrowsAnalyzerConfig LoadFromOptions(AnalyzerConfigOptions options)
    {
        var config = new ThrowsAnalyzerConfig();

        if (options.TryGetValue("throws_analyzer_codefix_prefer_logging", out var val))
            config.PreferLogging = bool.Parse(val);

        // ... load other options

        return config;
    }
}
```

## Testing Strategy

### Phase 4.6 Tests

1. **Benchmark validation**: Ensure benchmarks run without errors
2. **Performance regression tests**: Automated tests to catch slowdowns
3. **Cache correctness**: Verify cached results match uncached

### Phase 4.7 Tests

1. **Suppression tests**: Verify attribute suppresses diagnostics
2. **Configuration parsing tests**: Test all config option combinations
3. **Profile validation**: Ensure profiles load correctly
4. **Backward compatibility**: Existing configs still work

**New tests:** ~30-40 tests

## Documentation

### Phase 4.6

- `docs/PERFORMANCE.md` - Benchmark results and optimization guide
- `benchmarks/README.md` - How to run benchmarks

### Phase 4.7

- `docs/CONFIGURATION_GUIDE.md` - Comprehensive configuration reference
- `docs/SUPPRESSION_GUIDE.md` - How to use suppression attributes
- `.editorconfig.strict` - Strict profile
- `.editorconfig.minimal` - Minimal profile
- `.editorconfig.recommended` - Recommended profile

## Risk Assessment

### Phase 4.6 Risks

**Low risk:**
- Benchmark creation is isolated
- Performance optimizations are measurable

**Medium risk:**
- Caching could introduce stale data bugs
- Mitigation: Extensive testing, cache invalidation

### Phase 4.7 Risks

**Low risk:**
- Suppression attribute is additive
- Configuration is backward compatible

**Medium risk:**
- Complex configuration could confuse users
- Mitigation: Clear documentation, sensible defaults

## Success Metrics

### Phase 4.6
- Analyzer performance within top 25% of popular analyzers
- Zero performance regressions
- Documentation helps users optimize large codebases

### Phase 4.7
- Users can suppress diagnostics without `#pragma`
- Configuration covers 95% of common scenarios
- Configuration guide reduces support questions by 50%

## Future Extensions (Beyond 4.7)

- **Phase 5**: Advanced analysis (async, iterators, expression trees)
- **Phase 6**: IDE extensions (custom visualizers, quick info)
- **Phase 7**: Integration with other tools (SonarQube, code metrics)
