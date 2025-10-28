using System;
using ResourceManagement.Examples;

namespace ResourceManagement;

/// <summary>
/// ResourceManagement Sample Project
///
/// This project demonstrates real-world resource management scenarios:
/// - Database connections and connection pooling
/// - File operations and stream management
/// - HttpClient patterns and best practices
/// - Threading and concurrency primitives
/// - Proper disposal in complex scenarios
///
/// Build the project to see DisposableAnalyzer in action:
///   dotnet build
///
/// Each example class shows:
/// - Proper disposal patterns for specific resource types
/// - Common mistakes and their fixes
/// - Production-ready code examples
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("ResourceManagement Sample Project");
        Console.WriteLine("==================================");
        Console.WriteLine();
        Console.WriteLine("Real-world resource management patterns:");
        Console.WriteLine();

        Console.WriteLine("1. Database Patterns");
        Console.WriteLine("   - Connection management");
        Console.WriteLine("   - Repository pattern");
        Console.WriteLine("   - Unit of Work");
        Console.WriteLine("   - Connection pooling");
        Console.WriteLine();

        Console.WriteLine("2. File Operations");
        Console.WriteLine("   - Stream management");
        Console.WriteLine("   - Large file processing");
        Console.WriteLine("   - Temporary file cleanup");
        Console.WriteLine("   - File watching");
        Console.WriteLine();

        Console.WriteLine("3. HTTP Client Patterns");
        Console.WriteLine("   - Singleton vs per-request");
        Console.WriteLine("   - Response disposal");
        Console.WriteLine("   - Download manager");
        Console.WriteLine("   - WebSocket connections");
        Console.WriteLine();

        Console.WriteLine("4. Concurrency Patterns");
        Console.WriteLine("   - Thread-safe resources");
        Console.WriteLine("   - Resource pooling");
        Console.WriteLine("   - Background tasks");
        Console.WriteLine("   - Rate limiting");
        Console.WriteLine();

        Console.WriteLine("Build this project to see analyzer warnings:");
        Console.WriteLine("  dotnet build");
        Console.WriteLine();
        Console.WriteLine("Open in your IDE to apply code fixes:");
        Console.WriteLine("  - Visual Studio: View → Error List");
        Console.WriteLine("  - VS Code: View → Problems");
        Console.WriteLine("  - Rider: View → Problems");
        Console.WriteLine();

        // Run some examples
        Console.WriteLine("Running examples...");
        Console.WriteLine();

        RunDatabaseExamples();
        await RunFileExamplesAsync();
        await RunHttpExamplesAsync();
        await RunConcurrencyExamplesAsync();

        Console.WriteLine();
        Console.WriteLine("Examples completed!");
    }

    private static void RunDatabaseExamples()
    {
        Console.WriteLine(">>> Database Examples");

        var examples = new DatabaseExamples();

        Console.WriteLine("  - Simple query with using statement");
        examples.SimpleQuery();

        Console.WriteLine("  - Repository pattern");
        examples.RepositoryPattern();

        Console.WriteLine("  - Transaction with commit");
        examples.TransactionPattern();

        Console.WriteLine();
    }

    private static async Task RunFileExamplesAsync()
    {
        Console.WriteLine(">>> File Operation Examples");

        var fileOps = new FileOperations();
        var tempFile = Path.GetTempFileName();

        try
        {
            Console.WriteLine("  - Writing file content");
            fileOps.WriteFileContent(tempFile, "Hello, DisposableAnalyzer!");

            Console.WriteLine("  - Reading file content");
            var content = fileOps.ReadFileContent(tempFile);
            Console.WriteLine($"    Read: {content}");

            Console.WriteLine("  - Using temporary file manager");
            using var tempManager = new TempFileManager();
            var tempPath = tempManager.CreateTempFile("example");
            Console.WriteLine($"    Created: {tempPath}");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }

        Console.WriteLine();
    }

    private static async Task RunHttpExamplesAsync()
    {
        Console.WriteLine(">>> HTTP Client Examples");
        Console.WriteLine("  - HttpClient patterns demonstrated in code");
        Console.WriteLine("  - See HttpClientPatterns.cs for examples");
        Console.WriteLine();
    }

    private static async Task RunConcurrencyExamplesAsync()
    {
        Console.WriteLine(">>> Concurrency Examples");

        Console.WriteLine("  - Periodic task executor");
        var taskCount = 0;
        using var executor = new PeriodicTaskExecutor(
            async () =>
            {
                taskCount++;
                Console.WriteLine($"    Task executed {taskCount} times");
                await Task.Delay(10);
            },
            TimeSpan.FromMilliseconds(100));

        await Task.Delay(350);

        Console.WriteLine("  - Rate limiter");
        using var rateLimiter = new RateLimiter(maxRequests: 3, TimeSpan.FromSeconds(1));

        for (int i = 0; i < 5; i++)
        {
            using (await rateLimiter.AcquireAsync())
            {
                Console.WriteLine($"    Request {i + 1} allowed");
            }
        }

        Console.WriteLine();
    }
}
