using System;
using System.IO;
using System.Threading.Tasks;

namespace DisposalPatterns.Examples;

/// <summary>
/// Demonstrates async disposal patterns (DISP011-013)
/// Shows proper IAsyncDisposable implementation and await using
/// </summary>

// Mock async disposable for demonstration
public class AsyncStream : IAsyncDisposable, IDisposable
{
    private readonly Stream _stream;

    public AsyncStream(string path)
    {
        _stream = File.OpenWrite(path);
    }

    public async Task WriteAsync(byte[] data)
    {
        await _stream.WriteAsync(data, 0, data.Length);
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}

public class AsyncDisposalIssues
{
    // DISP011: Should use await using for IAsyncDisposable
    public async Task SyncUsingOnAsync_Bad()
    {
        using var stream = new AsyncStream("test.txt"); // ⚠️ DISP011 - should use 'await using'
        await stream.WriteAsync(new byte[] { 42 });
    }

    // ✓ Fixed: Await using
    public async Task AwaitUsing_Good()
    {
        await using var stream = new AsyncStream("test.txt"); // ✓ Good
        await stream.WriteAsync(new byte[] { 42 });
    }
}

// DISP012: IAsyncDisposable not implemented
public class AsyncOperationsInDispose_Bad : IDisposable // ⚠️ DISP012
{
    private readonly HttpClient _client = new HttpClient();

    public void Dispose()
    {
        // Problem: HttpClient should be disposed asynchronously
        _client.Dispose(); // Synchronous disposal of async resource
    }
}

// ✓ Fixed: Implement IAsyncDisposable
public class AsyncDisposableImplemented_Good : IAsyncDisposable
{
    private readonly HttpClient _client = new HttpClient();

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await Task.CompletedTask; // ✓ Good - async disposal
    }
}

// DISP013: DisposeAsync pattern violations (for non-sealed classes)
public class DisposeAsyncNoCore_Bad : IAsyncDisposable // ⚠️ DISP013
{
    private AsyncStream? _stream = new AsyncStream("test.txt");

    // Missing DisposeAsyncCore for inheritance support
    public async ValueTask DisposeAsync()
    {
        if (_stream != null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }
    }
}

// ✓ Fixed: Proper DisposeAsync pattern for inheritance
public class DisposeAsyncPattern_Good : IAsyncDisposable
{
    private AsyncStream? _stream = new AsyncStream("test.txt");
    private bool _disposed = false;

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
            return;

        if (_stream != null)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
            _stream = null;
        }

        _disposed = true;
    }
}

// ✓ Both sync and async disposal supported
public class DualDisposal_Good : IDisposable, IAsyncDisposable
{
    private FileStream? _stream = new FileStream("test.txt", FileMode.Create);
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false); // Dispose unmanaged resources
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _stream?.Dispose();
            _stream = null;
        }

        _disposed = true;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
            return;

        if (_stream != null)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
            _stream = null;
        }

        _disposed = true;
    }
}
