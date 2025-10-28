using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ResourceManagement.Examples;

/// <summary>
/// Demonstrates proper disposal of threading and concurrency primitives
/// Includes locks, semaphores, timers, and cancellation tokens
/// </summary>

/// <summary>
/// Thread-safe resource manager
/// </summary>
public class ThreadSafeResourceManager : IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    private readonly Dictionary<string, Stream> _resources = new();
    private bool _disposed = false;

    public void AddResource(string key, Stream resource)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ThreadSafeResourceManager));

            _resources[key] = resource;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Stream? GetResource(string key)
    {
        _lock.EnterReadLock();
        try
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ThreadSafeResourceManager));

            return _resources.TryGetValue(key, out var resource) ? resource : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _lock.EnterWriteLock();
        try
        {
            // ✓ Good: Dispose all resources
            foreach (var resource in _resources.Values)
            {
                resource?.Dispose();
            }
            _resources.Clear();

            _disposed = true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        // ✓ Good: Dispose the lock
        _lock?.Dispose();
    }
}

/// <summary>
/// Semaphore-based resource pool
/// </summary>
public class ResourcePool<T> : IDisposable where T : class, IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Queue<T> _pool;
    private readonly Func<T> _factory;
    private readonly int _maxSize;
    private bool _disposed = false;

    public ResourcePool(Func<T> factory, int maxSize = 10)
    {
        _factory = factory;
        _maxSize = maxSize;
        _semaphore = new SemaphoreSlim(maxSize, maxSize);
        _pool = new Queue<T>(maxSize);
    }

    public async Task<PooledResource> AcquireAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ResourcePool<T>));

        await _semaphore.WaitAsync(cancellationToken);

        T resource;
        lock (_pool)
        {
            resource = _pool.Count > 0 ? _pool.Dequeue() : _factory();
        }

        return new PooledResource(this, resource);
    }

    private void Return(T resource)
    {
        if (_disposed)
        {
            resource?.Dispose();
            return;
        }

        lock (_pool)
        {
            if (_pool.Count < _maxSize)
            {
                _pool.Enqueue(resource);
            }
            else
            {
                resource?.Dispose();
            }
        }

        _semaphore.Release();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // ✓ Good: Dispose all pooled resources
        lock (_pool)
        {
            while (_pool.Count > 0)
            {
                var resource = _pool.Dequeue();
                resource?.Dispose();
            }
        }

        _semaphore?.Dispose();
    }

    public class PooledResource : IDisposable
    {
        private readonly ResourcePool<T> _pool;
        private T? _resource;

        internal PooledResource(ResourcePool<T> pool, T resource)
        {
            _pool = pool;
            _resource = resource;
        }

        public T Resource => _resource ?? throw new ObjectDisposedException(nameof(PooledResource));

        public void Dispose()
        {
            if (_resource != null)
            {
                _pool.Return(_resource);
                _resource = null;
            }
        }
    }
}

/// <summary>
/// Background task processor with cancellation support
/// </summary>
public class BackgroundTaskProcessor : IDisposable
{
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly Task _processingTask;
    private readonly Channel<Func<Task>> _taskQueue;

    public BackgroundTaskProcessor()
    {
        _taskQueue = Channel.CreateUnbounded<Func<Task>>();
        _processingTask = Task.Run(ProcessTasksAsync);
    }

    public async Task EnqueueTaskAsync(Func<Task> task)
    {
        await _taskQueue.Writer.WriteAsync(task, _cts.Token);
    }

    private async Task ProcessTasksAsync()
    {
        await foreach (var task in _taskQueue.Reader.ReadAllAsync(_cts.Token))
        {
            try
            {
                await task();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Task failed: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        // ✓ Good: Cancel processing and wait for completion
        _cts.Cancel();
        _taskQueue.Writer.Complete();

        try
        {
            _processingTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Task was cancelled, which is expected
        }

        _cts?.Dispose();
    }
}

/// <summary>
/// Timer-based periodic task executor
/// </summary>
public class PeriodicTaskExecutor : IDisposable
{
    private readonly Timer _timer;
    private readonly Func<Task> _task;
    private readonly SemaphoreSlim _executionLock = new SemaphoreSlim(1, 1);
    private bool _disposed = false;

    public PeriodicTaskExecutor(Func<Task> task, TimeSpan interval)
    {
        _task = task;
        _timer = new Timer(async _ => await ExecuteAsync(), null, interval, interval);
    }

    private async Task ExecuteAsync()
    {
        if (_disposed)
            return;

        // Prevent overlapping executions
        if (!await _executionLock.WaitAsync(0))
            return;

        try
        {
            await _task();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Periodic task failed: {ex.Message}");
        }
        finally
        {
            _executionLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // ✓ Good: Stop timer and wait for current execution
        _timer?.Dispose();
        _executionLock.Wait(TimeSpan.FromSeconds(10));
        _executionLock?.Dispose();
    }
}

/// <summary>
/// Async operation with timeout and cancellation
/// </summary>
public class TimeoutOperation : IDisposable
{
    private readonly CancellationTokenSource _timeoutCts;
    private readonly CancellationTokenSource _linkedCts;

    public TimeoutOperation(TimeSpan timeout, CancellationToken externalToken = default)
    {
        _timeoutCts = new CancellationTokenSource(timeout);
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_timeoutCts.Token, externalToken);
    }

    public CancellationToken Token => _linkedCts.Token;

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation)
    {
        try
        {
            return await operation(_linkedCts.Token);
        }
        catch (OperationCanceledException) when (_timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException("Operation timed out");
        }
    }

    public void Dispose()
    {
        // ✓ Good: Dispose both cancellation token sources
        _linkedCts?.Dispose();
        _timeoutCts?.Dispose();
    }
}

/// <summary>
/// Rate limiter using semaphore
/// </summary>
public class RateLimiter : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxRequests;
    private readonly TimeSpan _timeWindow;
    private readonly Queue<DateTime> _requestTimes = new();
    private readonly object _lock = new object();

    public RateLimiter(int maxRequests, TimeSpan timeWindow)
    {
        _maxRequests = maxRequests;
        _timeWindow = timeWindow;
        _semaphore = new SemaphoreSlim(maxRequests, maxRequests);
    }

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var cutoff = now - _timeWindow;

            // Remove old requests
            while (_requestTimes.Count > 0 && _requestTimes.Peek() < cutoff)
            {
                _requestTimes.Dequeue();
            }

            _requestTimes.Enqueue(now);
        }

        return new ReleaseHandle(this);
    }

    private void Release()
    {
        _semaphore.Release();
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }

    private class ReleaseHandle : IDisposable
    {
        private RateLimiter? _limiter;

        public ReleaseHandle(RateLimiter limiter)
        {
            _limiter = limiter;
        }

        public void Dispose()
        {
            _limiter?.Release();
            _limiter = null;
        }
    }
}
