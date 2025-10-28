# DisposableAnalyzer Samples

This directory contains sample projects demonstrating the DisposableAnalyzer in action.

## Available Samples

### 1. [DisposalPatterns](./DisposalPatterns/) - Diagnostic Rule Demonstrations

Comprehensive demonstration of all 30 diagnostic rules (DISP001-030) with:
- **QuickStart.cs** - Common disposal patterns you'll use every day
- **01_BasicDisposalIssues.cs** - Local variables and using statements (DISP001-006)
- **02_FieldDisposal.cs** - IDisposable implementation (DISP002, DISP007-010)
- **03_AsyncDisposal.cs** - Async disposal patterns (DISP011-013)
- **04_SpecialContexts.cs** - Lambdas, iterators, parameters (DISP014-018)
- **05_AntiPatterns.cs** - Finalizers and collections (DISP019-020, DISP030)
- **06_CrossMethodAnalysis.cs** - Call graph tracking (DISP021-025)
- **07_BestPractices.cs** - Design patterns (DISP026-029)

**Statistics:**
- 8 example files
- 336+ analyzer warnings (intentional)
- All 30 diagnostic rules demonstrated
- Both "Bad" and "Good" examples for each pattern

**Best For:** Learning all diagnostic rules, understanding analyzer behavior, testing code fixes

---

### 2. [ResourceManagement](./ResourceManagement/) - Real-World Patterns

Production-ready code demonstrating proper resource management:
- **DatabaseConnection.cs** - Connection pooling, repositories, transactions
- **FileOperations.cs** - Stream management, temp files, compression
- **HttpClientPatterns.cs** - Proper HttpClient usage, downloads, WebSockets
- **ConcurrencyPatterns.cs** - Thread safety, resource pools, rate limiting

**Statistics:**
- 4 example files
- 160+ code examples
- Production-ready patterns
- Real-world scenarios

**Best For:** Implementing proper patterns in your code, understanding best practices, production reference

## Getting Started

### Choose Your Sample

**Learning the analyzer?** → Start with [DisposalPatterns](./DisposalPatterns/)
```bash
cd samples/DisposalPatterns
dotnet build  # See 336+ warnings demonstrating all rules
```

**Need production patterns?** → Start with [ResourceManagement](./ResourceManagement/)
```bash
cd samples/ResourceManagement
dotnet build  # See real-world implementations
dotnet run    # Run live examples
```

### Exploring in Your IDE

1. **Open the sample**:
   ```bash
   cd samples/DisposalPatterns  # or ResourceManagement
   code .  # or open in Visual Studio/Rider
   ```

2. **View diagnostics**:
   - Visual Studio: View → Error List
   - VS Code: View → Problems
   - Rider: View → Problems

3. **Apply code fixes**:
   - Click on warnings to see available fixes
   - Use light bulb icons or quick fix shortcuts
   - Compare with "_Good" examples (DisposalPatterns)

## Learning Paths

### Path 1: Understanding Diagnostics (DisposalPatterns)
1. **QuickStart.cs** - Common patterns you'll use daily
2. **01_BasicDisposalIssues.cs** - Local variables and using
3. **02_FieldDisposal.cs** - IDisposable implementation
4. **03_AsyncDisposal.cs** - Async disposal patterns
5. **04-07** - Special contexts, anti-patterns, advanced analysis

### Path 2: Applying Patterns (ResourceManagement)
1. **DatabaseConnection.cs** - Learn connection management first
2. **FileOperations.cs** - Then file and stream handling
3. **HttpClientPatterns.cs** - HTTP client best practices
4. **ConcurrencyPatterns.cs** - Advanced threading patterns

## Example Output

Building the project shows warnings like:

```
warning DISP001: Local disposable 'stream' is not disposed
warning DISP002: Disposable field '_stream' is not disposed in type 'BadExample'
warning DISP011: Should use 'await using' for IAsyncDisposable type 'AsyncStream'
warning DISP025: Disposable 'stream' is not disposed on all execution paths
```

Each warning includes:
- Clear description of the issue
- Location in code
- Available code fixes (in IDE)

## Running the Sample

```bash
cd samples/DisposalPatterns
dotnet run
```

The program prints information about the sample and its structure.

## Next Steps

- Review the warnings in your IDE
- Try applying code fixes
- Compare "Bad" vs "Good" examples
- Adapt patterns to your own code
- Use `.editorconfig` to customize severity levels

## Documentation

See [DisposalPatterns/README.md](./DisposalPatterns/README.md) for detailed information about each diagnostic rule.
