using System;
using System.IO;

namespace DisposalPatterns.Examples;

/// <summary>
/// Demonstrates field disposal issues (DISP002, DISP007-010)
/// Shows how to properly implement IDisposable for classes with disposable fields
/// </summary>

// DISP007: Type has disposable field but doesn't implement IDisposable
public class NoIDisposable_Bad // ⚠️ DISP007
{
    private FileStream _stream = new FileStream("test.txt", FileMode.Create); // Disposable field
    // Class should implement IDisposable
}

// DISP002: Disposable field not disposed in type
public class IDisposableNotDisposing_Bad : IDisposable
{
    private FileStream _stream = new FileStream("test.txt", FileMode.Create); // ⚠️ DISP002

    public void Dispose()
    {
        // _stream is never disposed
    }
}

// ✓ Fixed: Proper IDisposable implementation
public class ProperDisposal_Good : IDisposable
{
    private FileStream? _stream = new FileStream("test.txt", FileMode.Create);

    public void Dispose()
    {
        _stream?.Dispose(); // ✓ Good
        _stream = null;
    }
}

// DISP008: Dispose(bool) pattern violations
public class SimpleDispose_Suggestion : IDisposable // ℹ️ DISP008 - suggest Dispose(bool) pattern
{
    private FileStream? _stream = new FileStream("test.txt", FileMode.Create);

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
    }
}

// ✓ Fixed: Proper Dispose(bool) pattern
public class DisposeBoolPattern_Good : IDisposable
{
    private FileStream? _stream = new FileStream("test.txt", FileMode.Create);
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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

        // Free unmanaged resources here (if any)

        _disposed = true;
    }
}

// DISP009: Missing base.Dispose() call
public class DerivedDisposable_Bad : DisposeBoolPattern_Good
{
    private MemoryStream? _memoryStream = new MemoryStream();

    protected override void Dispose(bool disposing) // ⚠️ DISP009
    {
        if (disposing)
        {
            _memoryStream?.Dispose();
            _memoryStream = null;
        }
        // Missing: base.Dispose(disposing);
    }
}

// ✓ Fixed: Call base.Dispose()
public class DerivedDisposable_Good : DisposeBoolPattern_Good
{
    private MemoryStream? _memoryStream = new MemoryStream();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _memoryStream?.Dispose();
            _memoryStream = null;
        }
        base.Dispose(disposing); // ✓ Good
    }
}

// DISP010: Access to disposed field
public class AccessAfterDispose_Bad : IDisposable
{
    private FileStream? _stream = new FileStream("test.txt", FileMode.Create);
    private bool _disposed = false;

    public void Write(byte value)
    {
        _stream?.WriteByte(value); // ⚠️ DISP010 - might access disposed field
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
        _disposed = true;
    }
}

// ✓ Fixed: Check disposed state
public class AccessAfterDisposeFixed_Good : IDisposable
{
    private FileStream? _stream = new FileStream("test.txt", FileMode.Create);
    private bool _disposed = false;

    public void Write(byte value)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AccessAfterDisposeFixed_Good));

        _stream?.WriteByte(value); // ✓ Good - disposed check
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _stream?.Dispose();
        _stream = null;
        _disposed = true;
    }
}
