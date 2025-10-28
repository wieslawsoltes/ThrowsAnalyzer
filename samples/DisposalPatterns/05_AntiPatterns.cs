using System;
using System.Collections.Generic;
using System.IO;

namespace DisposalPatterns.Examples;

/// <summary>
/// Demonstrates disposal anti-patterns (DISP019-020)
/// Finalizers and collections of disposables
/// </summary>

// DISP019: Finalizer implementation issues
public class FinalizerIssues_Bad : IDisposable
{
    private FileStream? _stream = new FileStream("test.txt", FileMode.Create);

    public void Dispose()
    {
        Dispose(true);
        // ⚠️ DISP030 - Missing: GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream?.Dispose();
            _stream = null;
        }
    }

    // ⚠️ DISP019 - Finalizer exists but Dispose doesn't call SuppressFinalize
    ~FinalizerIssues_Bad()
    {
        Dispose(false);
    }
}

// ✓ Fixed: Proper finalizer with GC.SuppressFinalize
public class FinalizerPattern_Good : IDisposable
{
    private FileStream? _stream = new FileStream("test.txt", FileMode.Create);
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this); // ✓ Good - suppress finalizer
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources
            _stream?.Dispose();
            _stream = null;
        }

        // Free unmanaged resources here

        _disposed = true;
    }

    ~FinalizerPattern_Good()
    {
        Dispose(false);
    }
}

// DISP030: Unnecessary GC.SuppressFinalize when no finalizer
public class UnnecessarySuppressFinalize_Bad : IDisposable
{
    private FileStream? _stream = new FileStream("test.txt", FileMode.Create);

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
        GC.SuppressFinalize(this); // ⚠️ DISP030 - no finalizer exists
    }
}

// ✓ Fixed: No SuppressFinalize when no finalizer
public class NoFinalizerNoSuppress_Good : IDisposable
{
    private FileStream? _stream = new FileStream("test.txt", FileMode.Create);

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
        // No GC.SuppressFinalize needed - no finalizer
    }
}

// DISP020: Collection of disposables not disposed
public class DisposableCollection_Bad : IDisposable // ⚠️ DISP020
{
    private List<FileStream> _streams = new List<FileStream>
    {
        new FileStream("file1.txt", FileMode.Create),
        new FileStream("file2.txt", FileMode.Create),
        new FileStream("file3.txt", FileMode.Create)
    };

    public void Dispose()
    {
        // Collection is not disposed properly
    }
}

// ✓ Fixed: Dispose collection elements
public class DisposableCollection_Good : IDisposable
{
    private List<FileStream> _streams = new List<FileStream>
    {
        new FileStream("file1.txt", FileMode.Create),
        new FileStream("file2.txt", FileMode.Create),
        new FileStream("file3.txt", FileMode.Create)
    };

    public void Dispose()
    {
        if (_streams != null)
        {
            foreach (var stream in _streams)
            {
                stream?.Dispose(); // ✓ Good - dispose each element
            }
            _streams.Clear();
        }
    }
}

// ✓ Alternative: Array of disposables
public class DisposableArray_Good : IDisposable
{
    private FileStream[] _streams = new FileStream[]
    {
        new FileStream("file1.txt", FileMode.Create),
        new FileStream("file2.txt", FileMode.Create),
        new FileStream("file3.txt", FileMode.Create)
    };

    public void Dispose()
    {
        if (_streams != null)
        {
            foreach (var stream in _streams)
            {
                stream?.Dispose(); // ✓ Good
            }
            Array.Clear(_streams, 0, _streams.Length);
        }
    }
}

// ✓ Alternative: Dictionary of disposables
public class DisposableDictionary_Good : IDisposable
{
    private Dictionary<string, FileStream> _streamCache = new Dictionary<string, FileStream>
    {
        ["file1"] = new FileStream("file1.txt", FileMode.Create),
        ["file2"] = new FileStream("file2.txt", FileMode.Create)
    };

    public void Dispose()
    {
        if (_streamCache != null)
        {
            foreach (var kvp in _streamCache)
            {
                kvp.Value?.Dispose(); // ✓ Good
            }
            _streamCache.Clear();
        }
    }
}
