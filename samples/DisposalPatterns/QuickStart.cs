using System;
using System.IO;

namespace DisposalPatterns.Examples;

/// <summary>
/// Quick start examples - common disposal patterns you'll use every day
/// </summary>
public class QuickStart
{
    // ❌ WRONG: Local disposable not disposed
    public void Wrong_NotDisposed()
    {
        var stream = new FileStream("test.txt", FileMode.Create); // ⚠️ DISP001
        stream.WriteByte(42);
        // stream is never disposed - MEMORY LEAK!
    }

    // ✅ RIGHT: Using declaration (C# 8+)
    public void Right_UsingDeclaration()
    {
        using var stream = new FileStream("test.txt", FileMode.Create); // ✓ Disposed automatically
        stream.WriteByte(42);
    } // stream.Dispose() called here

    // ✅ RIGHT: Traditional using statement
    public void Right_UsingStatement()
    {
        using (var stream = new FileStream("test.txt", FileMode.Create))
        {
            stream.WriteByte(42);
        } // stream.Dispose() called here
    }

    // ❌ WRONG: Field not disposed
    public class WrongFieldDisposal : IDisposable // ⚠️ DISP002
    {
        private FileStream _stream = new FileStream("test.txt", FileMode.Create);

        public void Dispose()
        {
            // Forgot to dispose _stream!
        }
    }

    // ✅ RIGHT: Field properly disposed
    public class RightFieldDisposal : IDisposable
    {
        private FileStream? _stream = new FileStream("test.txt", FileMode.Create);

        public void Dispose()
        {
            _stream?.Dispose(); // ✓ Properly disposed
            _stream = null;
        }
    }

    // ❌ WRONG: Async resource with sync using
    public async Task Wrong_AsyncResourceSyncUsing()
    {
        using (var stream = File.OpenRead("test.txt")) // ⚠️ Should be 'await using'
        {
            byte[] buffer = new byte[1024];
            await stream.ReadAsync(buffer, 0, buffer.Length);
        }
    }

    // ✅ RIGHT: Async resource with await using
    public async Task Right_AwaitUsing()
    {
        await using (var stream = File.OpenRead("test.txt")) // ✓ Async disposal
        {
            byte[] buffer = new byte[1024];
            await stream.ReadAsync(buffer, 0, buffer.Length);
        }
    }

    // ❌ WRONG: Not disposed on all paths
    public void Wrong_NotDisposedOnAllPaths(bool condition)
    {
        var stream = new FileStream("test.txt", FileMode.Create); // ⚠️ DISP025

        if (condition)
        {
            stream.WriteByte(42);
            return; // Exits without disposing!
        }

        stream.Dispose(); // Only disposed if condition is false
    }

    // ✅ RIGHT: Disposed on all paths with using
    public void Right_DisposedOnAllPaths(bool condition)
    {
        using var stream = new FileStream("test.txt", FileMode.Create); // ✓ Always disposed

        if (condition)
        {
            stream.WriteByte(42);
            return; // stream will be disposed here
        }

        stream.WriteByte(99);
    } // and disposed here

    // ✅ ALTERNATIVE: Disposed on all paths with try-finally
    public void Right_TryFinally(bool condition)
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
            stream?.Dispose(); // ✓ Disposed on all paths
        }
    }
}

/// <summary>
/// Summary: Quick Reference
///
/// Use 'using' statements for local disposables:
///   using var obj = new DisposableType();
///
/// Implement IDisposable for types with disposable fields:
///   public void Dispose() => _field?.Dispose();
///
/// Use 'await using' for IAsyncDisposable:
///   await using var obj = new AsyncDisposableType();
///
/// Ensure disposal on all code paths:
///   - Use using statements (preferred)
///   - Or try-finally blocks
///
/// See other example files for advanced scenarios!
/// </summary>
public class Summary { }
