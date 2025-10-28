using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DisposalPatterns.Examples;

/// <summary>
/// Demonstrates disposal in special contexts (DISP014-017)
/// Lambda expressions, iterators, return values, and parameter passing
/// </summary>
public class SpecialContexts
{
    // DISP014: Disposable in lambda expression
    public void DisposableInLambda_Bad()
    {
        var files = new[] { "file1.txt", "file2.txt" };

        var streams = files.Select(f =>
        {
            var stream = new FileStream(f, FileMode.Create); // ⚠️ DISP014 - disposable in lambda
            return stream.Length;
        }).ToList();
    }

    // ✓ Fixed: Proper disposal in lambda
    public void DisposableInLambda_Good()
    {
        var files = new[] { "file1.txt", "file2.txt" };

        var lengths = files.Select(f =>
        {
            using var stream = new FileStream(f, FileMode.Create); // ✓ Good
            return stream.Length;
        }).ToList();
    }

    // DISP015: Disposable in iterator (yield)
    public IEnumerable<long> DisposableInIterator_Bad() // ⚠️ DISP015
    {
        var files = new[] { "file1.txt", "file2.txt" };

        foreach (var file in files)
        {
            using var stream = new FileStream(file, FileMode.Create); // Problematic in iterator
            yield return stream.Length;
            // Stream disposed before consumer can use the value
        }
    }

    // ✓ Fixed: Extract non-iterator helper
    public IEnumerable<long> DisposableInIterator_Good()
    {
        var files = new[] { "file1.txt", "file2.txt" };

        foreach (var file in files)
        {
            yield return GetFileLength(file);
        }
    }

    private long GetFileLength(string file)
    {
        using var stream = new FileStream(file, FileMode.Create); // ✓ Good
        return stream.Length;
    }

    // DISP016: Disposable returned without documentation
    public FileStream CreateStream_Suggestion() // ℹ️ DISP016 - should document ownership
    {
        return new FileStream("test.txt", FileMode.Create);
    }

    // ✓ Fixed: Documented ownership transfer
    /// <summary>
    /// Creates a new file stream.
    /// </summary>
    /// <returns>A FileStream that must be disposed by the caller.</returns>
    /// <remarks>The caller is responsible for disposing the returned stream.</remarks>
    public FileStream CreateStreamDocumented_Good() // ✓ Good
    {
        return new FileStream("test.txt", FileMode.Create);
    }

    // ✓ Alternative: Clear naming convention
    public FileStream CreateOwnedStream_Good() // ✓ Good - naming indicates ownership transfer
    {
        return new FileStream("test.txt", FileMode.Create);
    }

    // DISP017: Disposal responsibility unclear when passing as argument
    public void PassDisposableAsArgument_Suggestion()
    {
        var stream = new FileStream("test.txt", FileMode.Create);
        ProcessStream(stream); // ℹ️ DISP017 - unclear who disposes
        // Should we dispose here or does ProcessStream take ownership?
    }

    private void ProcessStream(Stream stream)
    {
        stream.WriteByte(42);
        // Does not dispose - caller retains responsibility
    }

    // ✓ Fixed: Clear ownership through naming
    public void PassDisposableToMethod_Good()
    {
        using var stream = new FileStream("test.txt", FileMode.Create);
        ProcessStreamNoOwnership(stream); // ✓ Good - clear caller retains ownership
    }

    private void ProcessStreamNoOwnership(Stream stream)
    {
        stream.WriteByte(42);
        // Does not take ownership
    }

    // ✓ Alternative: Transfer ownership explicitly
    public void PassDisposableTransferOwnership_Good()
    {
        var stream = new FileStream("test.txt", FileMode.Create);
        TakeOwnershipOfStream(stream); // ✓ Good - method takes ownership
        // Don't dispose here - ownership transferred
    }

    private void TakeOwnershipOfStream(Stream ownedStream)
    {
        using (ownedStream) // Takes ownership and disposes
        {
            ownedStream.WriteByte(42);
        }
    }
}

// DISP018: Disposable in constructor without exception safety
public class ConstructorException_Bad : IDisposable // ⚠️ DISP018
{
    private FileStream _stream1;
    private FileStream _stream2;

    public ConstructorException_Bad()
    {
        _stream1 = new FileStream("file1.txt", FileMode.Create);
        _stream2 = new FileStream("file2.txt", FileMode.Create); // If this throws, _stream1 leaks
    }

    public void Dispose()
    {
        _stream1?.Dispose();
        _stream2?.Dispose();
    }
}

// ✓ Fixed: Exception safety in constructor
public class ConstructorExceptionSafe_Good : IDisposable
{
    private FileStream? _stream1;
    private FileStream? _stream2;

    public ConstructorExceptionSafe_Good()
    {
        try
        {
            _stream1 = new FileStream("file1.txt", FileMode.Create);
            _stream2 = new FileStream("file2.txt", FileMode.Create);
        }
        catch
        {
            // Clean up on failure
            _stream1?.Dispose();
            _stream1 = null;
            throw;
        }
    }

    public void Dispose()
    {
        _stream1?.Dispose();
        _stream2?.Dispose();
    }
}
