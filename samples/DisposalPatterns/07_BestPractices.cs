using System;
using System.Collections.Generic;
using System.IO;

namespace DisposalPatterns.Examples;

/// <summary>
/// Demonstrates disposal best practices (DISP026-030)
/// Design patterns and recommendations for resource management
/// </summary>

// DISP026: CompositeDisposable recommended
public class MultipleDisposables_Suggestion : IDisposable // ℹ️ DISP026
{
    private FileStream _stream1 = new FileStream("file1.txt", FileMode.Create);
    private FileStream _stream2 = new FileStream("file2.txt", FileMode.Create);
    private FileStream _stream3 = new FileStream("file3.txt", FileMode.Create);
    private MemoryStream _stream4 = new MemoryStream();

    public void Dispose()
    {
        _stream1?.Dispose();
        _stream2?.Dispose();
        _stream3?.Dispose();
        _stream4?.Dispose();
    }
}

// ✓ Alternative: Using CompositeDisposable (requires System.Reactive NuGet package)
// Uncomment and add package reference to use:
// <PackageReference Include="System.Reactive" Version="6.0.0" />
/*
public class CompositeDisposablePattern_Good : IDisposable
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public CompositeDisposablePattern_Good()
    {
        _disposables.Add(new FileStream("file1.txt", FileMode.Create));
        _disposables.Add(new FileStream("file2.txt", FileMode.Create));
        _disposables.Add(new FileStream("file3.txt", FileMode.Create));
        _disposables.Add(new MemoryStream());
    }

    public void Dispose()
    {
        _disposables?.Dispose(); // ✓ Good - disposes all at once
    }
}
*/

// DISP027: Factory method disposal responsibility unclear
public class StreamFactory_Suggestion
{
    // ℹ️ DISP027 - "Get" suggests caching, unclear who disposes
    public FileStream GetStream(string path)
    {
        return new FileStream(path, FileMode.Create);
    }

    // ℹ️ DISP027 - "Find" suggests lookup, unclear ownership
    public FileStream FindStream(string name)
    {
        return new FileStream($"{name}.txt", FileMode.Create);
    }
}

// ✓ Fixed: Clear factory naming
public class StreamFactoryClear_Good
{
    // "Create" clearly indicates new instance, caller owns
    public FileStream CreateStream(string path)
    {
        return new FileStream(path, FileMode.Create);
    }

    // "CreateOwned" explicitly states ownership transfer
    public FileStream CreateOwnedStream(string path)
    {
        return new FileStream(path, FileMode.Create);
    }

    // "Build" suggests construction, caller owns
    public FileStream BuildStream(string path)
    {
        return new FileStream(path, FileMode.Create);
    }
}

// DISP028: Wrapper class disposal patterns
public class StreamWrapper_Suggestion : IDisposable // ℹ️ DISP028
{
    private readonly FileStream _innerStream;

    public StreamWrapper_Suggestion(string path)
    {
        _innerStream = new FileStream(path, FileMode.Create);
    }

    public void Write(byte value)
    {
        _innerStream.WriteByte(value);
    }

    public void Dispose()
    {
        _innerStream?.Dispose(); // Should document if wrapper takes ownership
    }
}

// ✓ Documented wrapper with ownership
/// <summary>
/// Wraps a FileStream and takes ownership of it.
/// </summary>
public class StreamWrapperOwned_Good : IDisposable
{
    private readonly FileStream _innerStream;

    /// <summary>
    /// Creates a wrapper that takes ownership of the stream.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <remarks>
    /// The wrapper will dispose the inner stream when disposed.
    /// </remarks>
    public StreamWrapperOwned_Good(string path)
    {
        _innerStream = new FileStream(path, FileMode.Create);
    }

    public void Write(byte value)
    {
        _innerStream.WriteByte(value);
    }

    public void Dispose()
    {
        _innerStream?.Dispose(); // ✓ Good - documented ownership
    }
}

// ✓ Alternative: No-ownership wrapper
/// <summary>
/// Wraps a stream without taking ownership.
/// </summary>
public class StreamWrapperNoOwnership_Good
{
    private readonly Stream _innerStream;

    /// <summary>
    /// Creates a wrapper that does NOT take ownership.
    /// </summary>
    /// <param name="stream">The stream to wrap.</param>
    /// <remarks>
    /// Caller retains responsibility for disposing the stream.
    /// </remarks>
    public StreamWrapperNoOwnership_Good(Stream stream)
    {
        _innerStream = stream;
    }

    public void Write(byte value)
    {
        _innerStream.WriteByte(value);
    }

    // No Dispose - caller owns the stream
}

// DISP029: IDisposable struct patterns
public struct DisposableStruct_Warning : IDisposable // ⚠️ DISP029
{
    private MemoryStream? _stream;

    public DisposableStruct_Warning()
    {
        _stream = new MemoryStream();
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
    }

    // Problems:
    // 1. Structs are copied by value - disposal state not shared
    // 2. Boxing creates copies
    // 3. Default constructor creates uninitialized instances
}

// ✓ Better: Use class instead of struct for IDisposable
public class DisposableClass_Good : IDisposable
{
    private MemoryStream? _stream;

    public DisposableClass_Good()
    {
        _stream = new MemoryStream();
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
    }
}

// ✓ Alternative: Ref struct (C# 7.2+) prevents boxing
public ref struct RefStructDisposable_Good
{
    private MemoryStream? _stream;

    public RefStructDisposable_Good()
    {
        _stream = new MemoryStream();
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
    }

    // Ref structs can't box, reducing some issues
}

// Already demonstrated in 05_AntiPatterns.cs:
// DISP030: GC.SuppressFinalize usage (see FinalizerIssues_Bad and FinalizerPattern_Good)
