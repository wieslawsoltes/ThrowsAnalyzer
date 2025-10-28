using System;
using System.IO;
using System.Text;

namespace ResourceManagement.Examples;

/// <summary>
/// Demonstrates file handling and stream management patterns
/// Shows proper disposal with file I/O operations
/// </summary>
public class FileOperations
{
    /// <summary>
    /// Basic file reading with proper disposal
    /// </summary>
    public string ReadFileContent(string path)
    {
        // ✓ Good: Using declaration
        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Basic file writing with proper disposal
    /// </summary>
    public void WriteFileContent(string path, string content)
    {
        // ✓ Good: Using declaration
        using var stream = File.OpenWrite(path);
        using var writer = new StreamWriter(stream);
        writer.Write(content);
    }

    /// <summary>
    /// File copying with multiple streams
    /// </summary>
    public void CopyFile(string sourcePath, string destinationPath)
    {
        // ✓ Good: Both streams properly disposed
        using var source = File.OpenRead(sourcePath);
        using var destination = File.OpenWrite(destinationPath);
        source.CopyTo(destination);
    }

    /// <summary>
    /// Processing large files in chunks
    /// </summary>
    public void ProcessLargeFile(string path, Action<byte[]> processChunk)
    {
        using var stream = File.OpenRead(path);
        var buffer = new byte[4096];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            var chunk = new byte[bytesRead];
            Array.Copy(buffer, chunk, bytesRead);
            processChunk(chunk);
        }
    }

    /// <summary>
    /// Multiple file processing with proper cleanup
    /// </summary>
    public void ProcessMultipleFiles(string[] paths)
    {
        var streams = new List<FileStream>();

        try
        {
            // Open all files
            foreach (var path in paths)
            {
                streams.Add(File.OpenRead(path));
            }

            // Process files
            foreach (var stream in streams)
            {
                ProcessStream(stream);
            }
        }
        finally
        {
            // ✓ Good: Ensure all streams are disposed even if exception occurs
            foreach (var stream in streams)
            {
                stream?.Dispose();
            }
            streams.Clear();
        }
    }

    private void ProcessStream(FileStream stream)
    {
        // Process the stream
        Console.WriteLine($"Processing file: {stream.Name}");
    }
}

/// <summary>
/// File manager with caching and proper disposal
/// </summary>
public class FileManager : IDisposable
{
    private readonly Dictionary<string, FileStream> _openFiles = new();
    private bool _disposed = false;

    public FileStream GetOrOpenFile(string path)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileManager));

        if (_openFiles.TryGetValue(path, out var existingStream))
        {
            return existingStream;
        }

        var newStream = File.OpenRead(path);
        _openFiles[path] = newStream;
        return newStream;
    }

    public void CloseFile(string path)
    {
        if (_openFiles.TryGetValue(path, out var stream))
        {
            stream?.Dispose();
            _openFiles.Remove(path);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // ✓ Good: Dispose all cached streams
        foreach (var kvp in _openFiles)
        {
            kvp.Value?.Dispose();
        }
        _openFiles.Clear();

        _disposed = true;
    }
}

/// <summary>
/// Log file writer with buffering and automatic flushing
/// </summary>
public class LogFileWriter : IDisposable
{
    private readonly FileStream _fileStream;
    private readonly StreamWriter _writer;
    private readonly Timer _flushTimer;

    public LogFileWriter(string logFilePath)
    {
        _fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(_fileStream, Encoding.UTF8) { AutoFlush = false };

        // Auto-flush every 5 seconds
        _flushTimer = new Timer(_ => Flush(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public void WriteLine(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        _writer.WriteLine($"[{timestamp}] {message}");
    }

    public void Flush()
    {
        _writer?.Flush();
        _fileStream?.Flush();
    }

    public void Dispose()
    {
        // ✓ Good: Proper disposal order - most dependent first
        _flushTimer?.Dispose();
        Flush(); // Ensure all data is written
        _writer?.Dispose();
        _fileStream?.Dispose();
    }
}

/// <summary>
/// Temporary file manager with automatic cleanup
/// </summary>
public class TempFileManager : IDisposable
{
    private readonly List<string> _tempFiles = new();
    private readonly string _tempDirectory;

    public TempFileManager()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public string CreateTempFile(string prefix = "temp")
    {
        var fileName = $"{prefix}_{Guid.NewGuid()}.tmp";
        var fullPath = Path.Combine(_tempDirectory, fileName);
        _tempFiles.Add(fullPath);
        return fullPath;
    }

    public FileStream OpenTempFile(string prefix = "temp")
    {
        var path = CreateTempFile(prefix);
        return File.Create(path);
    }

    public void Dispose()
    {
        // ✓ Good: Clean up all temp files
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Log but don't throw in Dispose
            }
        }

        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Log but don't throw in Dispose
        }
    }
}

/// <summary>
/// Compressed file operations
/// </summary>
public class CompressionOperations
{
    public void CompressFile(string sourcePath, string compressedPath)
    {
        using var sourceStream = File.OpenRead(sourcePath);
        using var compressedStream = File.Create(compressedPath);
        using var compressionStream = new System.IO.Compression.GZipStream(
            compressedStream,
            System.IO.Compression.CompressionMode.Compress);

        // ✓ Good: All three streams properly disposed in correct order
        sourceStream.CopyTo(compressionStream);
    }

    public void DecompressFile(string compressedPath, string destinationPath)
    {
        using var compressedStream = File.OpenRead(compressedPath);
        using var decompressionStream = new System.IO.Compression.GZipStream(
            compressedStream,
            System.IO.Compression.CompressionMode.Decompress);
        using var destinationStream = File.Create(destinationPath);

        // ✓ Good: All three streams properly disposed
        decompressionStream.CopyTo(destinationStream);
    }
}

/// <summary>
/// File watcher with proper disposal
/// </summary>
public class FileWatcherService : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly List<string> _changes = new();

    public FileWatcherService(string path)
    {
        _watcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        lock (_changes)
        {
            _changes.Add($"{e.ChangeType}: {e.FullPath}");
        }
    }

    public List<string> GetChanges()
    {
        lock (_changes)
        {
            var result = new List<string>(_changes);
            _changes.Clear();
            return result;
        }
    }

    public void Dispose()
    {
        // ✓ Good: Unsubscribe from events before disposing
        _watcher.Changed -= OnFileChanged;
        _watcher.Created -= OnFileChanged;
        _watcher.Deleted -= OnFileChanged;
        _watcher?.Dispose();
    }
}
