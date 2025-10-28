using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ResourceManagement.Examples;

/// <summary>
/// Demonstrates proper HttpClient usage patterns
/// HttpClient is meant to be reused and should NOT be disposed per request
/// </summary>
public class HttpClientPatterns
{
    // ❌ WRONG: Creating and disposing HttpClient per request
    public async Task<string> WrongPattern_DisposePerRequest(string url)
    {
        using var client = new HttpClient(); // BAD: Socket exhaustion!
        return await client.GetStringAsync(url);
    }

    // ✓ GOOD: Reuse HttpClient as singleton
    private static readonly HttpClient _sharedClient = new HttpClient();

    public async Task<string> GoodPattern_ReuseClient(string url)
    {
        // ✓ Good: Reuses the same client instance
        return await _sharedClient.GetStringAsync(url);
    }
}

/// <summary>
/// HttpClient service with proper lifecycle management
/// </summary>
public class HttpClientService : IDisposable
{
    private readonly HttpClient _client;

    public HttpClientService(string baseAddress)
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri(baseAddress),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<string> GetAsync(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> PostJsonAsync(string endpoint, string json)
    {
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public void Dispose()
    {
        // ✓ Good: Dispose when service is no longer needed
        _client?.Dispose();
    }
}

/// <summary>
/// HttpClient factory pattern (similar to IHttpClientFactory)
/// </summary>
public class HttpClientFactory : IDisposable
{
    private readonly Dictionary<string, HttpClient> _clients = new();

    public HttpClient GetOrCreateClient(string name, Action<HttpClient>? configure = null)
    {
        if (_clients.TryGetValue(name, out var existingClient))
        {
            return existingClient;
        }

        var newClient = new HttpClient();
        configure?.Invoke(newClient);
        _clients[name] = newClient;
        return newClient;
    }

    public void Dispose()
    {
        // ✓ Good: Dispose all managed clients
        foreach (var client in _clients.Values)
        {
            client?.Dispose();
        }
        _clients.Clear();
    }
}

/// <summary>
/// API client with retry logic and proper disposal
/// </summary>
public class ApiClient : IDisposable
{
    private readonly HttpClient _client;
    private readonly int _maxRetries;

    public ApiClient(string baseUrl, int maxRetries = 3)
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _maxRetries = maxRetries;
    }

    public async Task<T> GetAsync<T>(string endpoint)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                var response = await _client.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<T>(content)!;
            }
            catch (HttpRequestException) when (attempt < _maxRetries)
            {
                attempt++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
            }
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

/// <summary>
/// Demonstrates proper HttpResponseMessage disposal
/// </summary>
public class HttpResponsePatterns
{
    private readonly HttpClient _client = new HttpClient();

    // ❌ WRONG: Not disposing response
    public async Task<string> WrongPattern_NoDispose(string url)
    {
        var response = await _client.GetAsync(url); // Response not disposed
        return await response.Content.ReadAsStringAsync();
    }

    // ✓ GOOD: Using statement disposes response
    public async Task<string> GoodPattern_UsingStatement(string url)
    {
        using var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    // ✓ GOOD: Helper methods that handle disposal
    public async Task<string> GoodPattern_HelperMethod(string url)
    {
        // GetStringAsync handles response disposal internally
        return await _client.GetStringAsync(url);
    }
}

/// <summary>
/// Download manager with progress reporting
/// </summary>
public class DownloadManager : IDisposable
{
    private readonly HttpClient _client;
    private readonly Dictionary<Guid, CancellationTokenSource> _activeDownloads = new();

    public DownloadManager()
    {
        _client = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan // Handle timeout manually
        };
    }

    public async Task<Guid> DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<double>? progress = null)
    {
        var downloadId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        _activeDownloads[downloadId] = cts;

        try
        {
            using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var bytesRead = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            int read;

            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read, cts.Token);
                bytesRead += read;

                if (totalBytes > 0)
                {
                    progress?.Report((double)bytesRead / totalBytes * 100);
                }
            }
        }
        finally
        {
            _activeDownloads.Remove(downloadId);
            cts.Dispose();
        }

        return downloadId;
    }

    public void CancelDownload(Guid downloadId)
    {
        if (_activeDownloads.TryGetValue(downloadId, out var cts))
        {
            cts.Cancel();
        }
    }

    public void Dispose()
    {
        // ✓ Good: Cancel all active downloads
        foreach (var cts in _activeDownloads.Values)
        {
            cts?.Cancel();
            cts?.Dispose();
        }
        _activeDownloads.Clear();

        _client?.Dispose();
    }
}

/// <summary>
/// WebSocket client wrapper with proper disposal
/// </summary>
public class WebSocketClient : IAsyncDisposable, IDisposable
{
    private readonly System.Net.WebSockets.ClientWebSocket _webSocket;
    private readonly CancellationTokenSource _cts;

    public WebSocketClient()
    {
        _webSocket = new System.Net.WebSockets.ClientWebSocket();
        _cts = new CancellationTokenSource();
    }

    public async Task ConnectAsync(string url)
    {
        await _webSocket.ConnectAsync(new Uri(url), _cts.Token);
    }

    public async Task SendAsync(string message)
    {
        var buffer = System.Text.Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(
            new ArraySegment<byte>(buffer),
            System.Net.WebSockets.WebSocketMessageType.Text,
            true,
            _cts.Token);
    }

    public async Task<string> ReceiveAsync()
    {
        var buffer = new byte[1024];
        var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
        return System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();

        if (_webSocket.State == System.Net.WebSockets.WebSocketState.Open)
        {
            await _webSocket.CloseAsync(
                System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                "Closing",
                CancellationToken.None);
        }

        _webSocket?.Dispose();
        _cts?.Dispose();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _webSocket?.Dispose();
        _cts?.Dispose();
    }
}
