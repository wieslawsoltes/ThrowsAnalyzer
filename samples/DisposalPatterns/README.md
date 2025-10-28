# DisposalPatterns Sample Project

This sample project demonstrates all 30 diagnostic rules from DisposableAnalyzer with both problematic and corrected code examples.

## Purpose

This project serves as:
- **Learning resource** - See examples of all disposal patterns and anti-patterns
- **Testing playground** - Manually verify analyzer behavior
- **Documentation** - Real code examples for each diagnostic rule

## Structure

The project is organized by diagnostic category:

### 01_BasicDisposalIssues.cs
Basic disposal issues with local variables:
- **DISP001**: Local disposable not disposed
- **DISP003**: Potential double disposal
- **DISP004**: Should use 'using' statement
- **DISP005**: Using statement scope too broad
- **DISP006**: Use using declaration (C# 8+)

### 02_FieldDisposal.cs
IDisposable implementation patterns:
- **DISP002**: Disposable field not disposed in type
- **DISP007**: Type has disposable field but doesn't implement IDisposable
- **DISP008**: Dispose(bool) pattern violations
- **DISP009**: Missing base.Dispose() call
- **DISP010**: Access to disposed field

### 03_AsyncDisposal.cs
Async disposal with IAsyncDisposable:
- **DISP011**: Should use await using for IAsyncDisposable
- **DISP012**: IAsyncDisposable not implemented
- **DISP013**: DisposeAsync pattern violations

### 04_SpecialContexts.cs
Disposal in special contexts:
- **DISP014**: Disposable resource in lambda
- **DISP015**: Disposable in iterator method
- **DISP016**: Disposable returned without transfer documentation
- **DISP017**: Disposal responsibility unclear when passing as argument
- **DISP018**: Disposable in constructor without exception safety

### 05_AntiPatterns.cs
Common disposal anti-patterns:
- **DISP019**: Finalizer without proper Dispose pattern
- **DISP020**: Collection of disposables not disposed
- **DISP030**: GC.SuppressFinalize usage issues

### 06_CrossMethodAnalysis.cs
Cross-method disposal tracking:
- **DISP021**: Disposal not propagated across methods
- **DISP022**: Disposable created but not returned
- **DISP023**: Resource leak across method boundaries
- **DISP024**: Conditional ownership creates unclear disposal
- **DISP025**: Disposal not on all code paths

### 07_BestPractices.cs
Design patterns and recommendations:
- **DISP026**: CompositeDisposable recommended
- **DISP027**: Factory method disposal responsibility unclear
- **DISP028**: Wrapper class disposal patterns
- **DISP029**: IDisposable struct patterns

## Usage

### Building the Project

```bash
cd samples/DisposalPatterns
dotnet build
```

You'll see warnings for all the intentional disposal issues.

### Viewing Diagnostics

**Visual Studio:**
1. Open the solution in Visual Studio
2. Build the project
3. View warnings in the Error List (View → Error List)
4. Click on a warning to jump to the code
5. Click the light bulb icon or press `Ctrl+.` to see code fixes

**VS Code:**
1. Open the folder in VS Code with C# extension installed
2. Build with `Ctrl+Shift+B`
3. View problems in the Problems panel (View → Problems)
4. Click on warnings to see quick fixes

**Rider:**
1. Open the solution in Rider
2. Build the project
3. View warnings in the Problems tool window
4. Click warnings and use `Alt+Enter` to see fixes

### Testing Code Fixes

Each file contains pairs of "Bad" and "Good" examples:
- **_Bad** classes/methods: Contain intentional issues that trigger warnings
- **_Good** classes/methods: Show the corrected versions

Try applying the analyzer's code fixes to the "Bad" examples and compare with the "Good" versions.

## Code Fix Examples

### Example 1: DISP001 - Local Not Disposed

**Before:**
```csharp
public void LocalNotDisposed_Bad()
{
    var stream = new FileStream("test.txt", FileMode.Create); // ⚠️ DISP001
    stream.WriteByte(42);
}
```

**After applying "Wrap in using" fix:**
```csharp
public void LocalNotDisposed_Fixed()
{
    using var stream = new FileStream("test.txt", FileMode.Create); // ✓ Fixed
    stream.WriteByte(42);
}
```

### Example 2: DISP002 - Field Not Disposed

**Before:**
```csharp
public class IDisposableNotDisposing_Bad : IDisposable
{
    private FileStream _stream = new FileStream("test.txt", FileMode.Create); // ⚠️ DISP002

    public void Dispose()
    {
        // _stream is never disposed
    }
}
```

**After applying "Add disposal" fix:**
```csharp
public class IDisposableNotDisposing_Fixed : IDisposable
{
    private FileStream? _stream = new FileStream("test.txt", FileMode.Create);

    public void Dispose()
    {
        _stream?.Dispose(); // ✓ Fixed
        _stream = null;
    }
}
```

### Example 3: DISP025 - Not Disposed On All Paths

**Before:**
```csharp
public void NotDisposedOnAllPaths_Bad(bool condition)
{
    var stream = new FileStream("test.txt", FileMode.Create); // ⚠️ DISP025

    if (condition)
    {
        stream.WriteByte(42);
        return; // Stream not disposed on this path!
    }

    stream.Dispose();
}
```

**After applying "Move to finally block" fix:**
```csharp
public void NotDisposedOnAllPaths_Fixed(bool condition)
{
    var stream = new FileStream("test.txt", FileMode.Create);
    try
    {
        if (condition)
        {
            stream.WriteByte(42);
            return;
        }

        stream.WriteByte(99);
    }
    finally
    {
        stream?.Dispose(); // ✓ Fixed - disposed on all paths
    }
}
```

## Configuration

You can customize analyzer behavior by creating an `.editorconfig` file:

```ini
# Disable specific rules
dotnet_diagnostic.DISP001.severity = none

# Set severity levels
dotnet_diagnostic.DISP002.severity = error
dotnet_diagnostic.DISP006.severity = suggestion
dotnet_diagnostic.DISP024.severity = warning
```

## Notes

- Some examples require additional packages (like System.Reactive for CompositeDisposable)
- The `_Bad` examples are intentionally incorrect to demonstrate warnings
- The `_Good` examples show best practices
- Comments indicate which diagnostic each example triggers
- All examples use realistic scenarios you might encounter in production code

## Learning Path

Recommended order for learning:

1. **Start with basics** (01_BasicDisposalIssues.cs)
   - Understand using statements and local disposal

2. **Learn IDisposable** (02_FieldDisposal.cs)
   - Implement IDisposable correctly
   - Understand Dispose(bool) pattern

3. **Async disposal** (03_AsyncDisposal.cs)
   - Learn IAsyncDisposable
   - Understand await using

4. **Special contexts** (04_SpecialContexts.cs)
   - Handle lambdas, iterators, parameters

5. **Avoid anti-patterns** (05_AntiPatterns.cs)
   - Finalizers, collections, common mistakes

6. **Advanced tracking** (06_CrossMethodAnalysis.cs)
   - Understand cross-method disposal
   - Control flow analysis

7. **Design patterns** (07_BestPractices.cs)
   - Factory patterns, wrappers
   - Professional recommendations

## Troubleshooting

**Analyzer not showing warnings?**
- Ensure DisposableAnalyzer is referenced in the .csproj
- Clean and rebuild: `dotnet clean && dotnet build`
- Check IDE analyzer settings

**Want to suppress a specific warning?**
```csharp
#pragma warning disable DISP001
var stream = GetStream();
#pragma warning restore DISP001
```

Or use SuppressMessage attribute:
```csharp
[SuppressMessage("DisposableAnalyzer", "DISP001", Justification = "Ownership transferred")]
```

## Contributing

Found issues or have suggestions? Please open an issue at:
https://github.com/wieslawsoltes/ThrowsAnalyzer/issues
