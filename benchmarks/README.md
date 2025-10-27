# ThrowsAnalyzer Benchmarks

This project contains performance benchmarks for ThrowsAnalyzer using BenchmarkDotNet.

## Running Benchmarks

### Prerequisites

- .NET SDK 9.0 or later
- Release build recommended for accurate measurements

### Run All Benchmarks

```bash
cd benchmarks/ThrowsAnalyzer.Benchmarks
dotnet run --configuration Release
```

### Results

Benchmark results will be saved to `BenchmarkDotNet.Artifacts/results/` including:
- Console output
- HTML report
- CSV/JSON data

## Benchmark Scenarios

### ThrowStatementDetector Benchmarks

Tests the performance of detecting throw statements across different file sizes:
- **SmallFile**: 10 methods (~50 LOC)
- **MediumFile**: 100 methods (~500 LOC)
- **LargeFile**: 1000 methods (~5000 LOC)

### TryCatchDetector Benchmarks

Tests the performance of detecting try-catch blocks across different file sizes.

### ParseAndAnalyze Benchmarks

Tests the combined performance of parsing and analyzing files.

## Performance Targets

Based on our testing:
- **Analysis time**: < 50ms per 1000 LOC file
- **Memory allocations**: Minimal, using caching where appropriate
- **Scalability**: Linear growth with file size

## Optimization Notes

### Current Optimizations

1. **Exception Type Cache**: Caches `ITypeSymbol` lookups for common exception types
2. **Hierarchy Depth Cache**: Caches inheritance depth calculations
3. **Syntax Tree Traversal**: Efficient use of LINQ and early exits

### Future Optimizations

- Object pooling for frequently created objects
- Batch semantic model queries
- Incremental analysis support

## Interpreting Results

Key metrics to watch:
- **Mean**: Average execution time
- **Allocated**: Memory allocated per operation
- **Gen0/Gen1/Gen2**: Garbage collection counts (lower is better)

## Baseline Comparison

ThrowsAnalyzer performance should be comparable to other popular analyzers:
- StyleCop.Analyzers
- Roslynator
- SonarAnalyzer

## Contributing

When adding new features, run benchmarks before and after to detect performance regressions.

```bash
# Run specific benchmark
dotnet run --configuration Release --filter "*ThrowStatementDetector*"

# Compare with baseline
dotnet run --configuration Release --job short --memory
```
