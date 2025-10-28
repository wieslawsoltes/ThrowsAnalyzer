# ResourceManagement Sample Project

This sample project demonstrates real-world resource management patterns using DisposableAnalyzer. Unlike the DisposalPatterns project which shows all diagnostic rules, this project focuses on production-ready code for common scenarios.

## Purpose

Learn how to properly manage resources in real applications:
- **Database connections** - Connection pooling, repositories, transactions
- **File operations** - Streams, compression, temp files, file watching
- **HTTP clients** - Proper HttpClient usage, downloads, WebSockets
- **Concurrency** - Thread safety, resource pools, background tasks, rate limiting

## Project Structure

### DatabaseConnection.cs
Real-world database patterns:
- **DatabaseConnection** - Basic connection with proper disposal
- **UserRepository** - Repository pattern managing its own connection
- **UnitOfWork** - Transaction management with automatic rollback
- **DatabaseConnectionFactory** - Factory pattern for connection creation
- **ConnectionPool** - Simplified connection pooling implementation

**Key Concepts:**
- Connection lifecycle management
- Transaction boundaries
- Repository ownership of connections
- Factory methods indicating ownership transfer

### FileOperations.cs
File handling patterns:
- **FileOperations** - Basic file reading, writing, copying
- **FileManager** - Caching open files with proper cleanup
- **LogFileWriter** - Buffered log writing with auto-flush
- **TempFileManager** - Temporary file creation and automatic cleanup
- **CompressionOperations** - Working with compressed files
- **FileWatcherService** - File system monitoring with event cleanup

**Key Concepts:**
- Stream disposal order (dependent streams first)
- Multiple file handling with try-finally
- Temporary resource cleanup
- Event unsubscription before disposal

### HttpClientPatterns.cs
HTTP client best practices:
- **HttpClientPatterns** - Wrong vs right HttpClient usage
- **HttpClientService** - Service managing an HttpClient instance
- **HttpClientFactory** - Multiple named clients
- **ApiClient** - Retry logic with proper disposal
- **HttpResponsePatterns** - Response disposal patterns
- **DownloadManager** - File downloads with progress and cancellation
- **WebSocketClient** - WebSocket with async disposal

**Key Concepts:**
- HttpClient should be reused, not disposed per request
- HttpResponseMessage must be disposed
- Async disposal for async resources
- Cancellation token cleanup

### ConcurrencyPatterns.cs
Threading and synchronization:
- **ThreadSafeResourceManager** - ReaderWriterLockSlim with resource dictionary
- **ResourcePool<T>** - Generic resource pooling with SemaphoreSlim
- **BackgroundTaskProcessor** - Background task queue with cancellation
- **PeriodicTaskExecutor** - Timer-based periodic execution
- **TimeoutOperation** - Timeout and cancellation coordination
- **RateLimiter** - Request rate limiting with semaphore

**Key Concepts:**
- Lock disposal after unlocking
- Semaphore cleanup
- Cancellation token source disposal
- Timer disposal and final execution wait

## Building and Running

### Build the Project
```bash
cd samples/ResourceManagement
dotnet build
```

The build will succeed and show DisposableAnalyzer warnings from commented examples.

### Run the Examples
```bash
dotnet run
```

This runs real demonstrations of each pattern.

### Add to Solution
```bash
dotnet sln add samples/ResourceManagement/ResourceManagement.csproj
```

## Usage Examples

### Database Pattern
```csharp
// Repository pattern - repository manages connection
using var repository = new UserRepository("Server=localhost");
repository.GetUser(1);
repository.UpdateUser(1, "John");
// Connection disposed automatically when repository is disposed

// Transaction pattern - auto-rollback if not committed
using var unitOfWork = new UnitOfWork("Server=localhost");
unitOfWork.RegisterUser("Alice");
unitOfWork.Commit(); // Or rollback on exception
```

### File Pattern
```csharp
// Simple file operations
var fileOps = new FileOperations();
var content = fileOps.ReadFileContent("data.txt");
fileOps.WriteFileContent("output.txt", content);

// Temporary files with automatic cleanup
using var tempManager = new TempFileManager();
var tempFile = tempManager.CreateTempFile("prefix");
File.WriteAllText(tempFile, "data");
// All temp files deleted when tempManager is disposed
```

### HTTP Client Pattern
```csharp
// WRONG: Don't do this - creates socket exhaustion
using var client = new HttpClient(); // BAD!
var result = await client.GetStringAsync(url);

// RIGHT: Reuse HttpClient as singleton or long-lived instance
private static readonly HttpClient _client = new HttpClient();
var result = await _client.GetStringAsync(url);

// OR: Use HttpClientService for managed lifecycle
using var service = new HttpClientService("https://api.example.com");
var data = await service.GetAsync("/users/1");
```

### Concurrency Pattern
```csharp
// Resource pooling with semaphore
using var pool = new ResourcePool<DatabaseConnection>(
    () => new DatabaseConnection("Server=localhost"),
    maxSize: 10);

using (var pooled = await pool.AcquireAsync())
{
    pooled.Resource.ExecuteQuery("SELECT * FROM Users");
}
// Resource returned to pool automatically

// Rate limiting
using var rateLimiter = new RateLimiter(
    maxRequests: 10,
    TimeSpan.FromSeconds(1));

using (await rateLimiter.AcquireAsync())
{
    // Make rate-limited request
    await ProcessRequestAsync();
}
```

## Common Patterns

### Pattern 1: Repository with Own Connection
```csharp
public class Repository : IDisposable
{
    private readonly DbConnection _connection;

    public Repository(string connString)
    {
        _connection = new DbConnection(connString);
    }

    public void Dispose() => _connection?.Dispose();
}
```

### Pattern 2: Factory Returning Owned Resource
```csharp
public class Factory
{
    // "Create" name indicates caller owns returned resource
    public Stream CreateStream() => File.OpenRead("data.txt");
}

// Caller must dispose
using var stream = factory.CreateStream();
```

### Pattern 3: Resource Pool
```csharp
public class Pool<T> : IDisposable where T : IDisposable
{
    private Queue<T> _available = new();

    public T Acquire() { /* ... */ }
    public void Return(T item) { /* ... */ }

    public void Dispose()
    {
        // Dispose all pooled items
        foreach (var item in _available)
            item?.Dispose();
    }
}
```

### Pattern 4: Async Disposal
```csharp
public class AsyncResource : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        // Async cleanup
        await FlushAsync();
        await CloseAsync();
    }
}

// Use with await using
await using var resource = new AsyncResource();
```

## Best Practices Demonstrated

1. **HttpClient Reuse** - Don't create/dispose per request
2. **Connection Pooling** - Reuse expensive resources
3. **Transaction Management** - Auto-rollback on exception
4. **Temporary File Cleanup** - Automatic deletion on disposal
5. **Thread Safety** - Proper lock disposal after unlock
6. **Cancellation** - CancellationTokenSource disposal
7. **Event Cleanup** - Unsubscribe before disposal
8. **Async Disposal** - Use IAsyncDisposable for async resources

## Integration with CI/CD

Use DisposableAnalyzer in your build pipeline:

```xml
<!-- .csproj -->
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

Or in `.editorconfig`:
```ini
[*.cs]
dotnet_diagnostic.DISP001.severity = error
dotnet_diagnostic.DISP002.severity = error
```

## Learning Path

1. **Start with DatabaseConnection.cs** - Learn connection management
2. **Study FileOperations.cs** - Understand stream disposal
3. **Review HttpClientPatterns.cs** - Learn HTTP best practices
4. **Explore ConcurrencyPatterns.cs** - Advanced threading patterns

## Related Resources

- [DisposalPatterns](../DisposalPatterns/) - All 30 diagnostic rules with examples
- [DisposableAnalyzer NuGet](https://www.nuget.org/packages/DisposableAnalyzer)
- [Documentation](../../docs/DISPOSABLE_ANALYZER_PLAN.md)

## Troubleshooting

**No warnings shown?**
- Ensure DisposableAnalyzer is referenced in .csproj
- Clean and rebuild: `dotnet clean && dotnet build`

**Too many warnings?**
- This project demonstrates proper patterns - minimal warnings expected
- Use `.editorconfig` to adjust severity

**Need more examples?**
- See [DisposalPatterns](../DisposalPatterns/) for all diagnostic rules
- Review individual files for specific scenarios

## Contributing

Found a better pattern? Have a real-world scenario to add?
- Open an issue: https://github.com/wieslawsoltes/ThrowsAnalyzer/issues
- Submit a PR with your example!
