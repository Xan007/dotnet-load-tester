// High-performance HTTP client dispatcher with connection pooling, fast failover, and latency capture.

using System.Diagnostics;
using System.Text;
using LoadTester.Configuration;
using LoadTester.Metrics;

namespace LoadTester.Core;

public sealed class HttpRequestDispatcher : IHttpRequestDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly SocketsHttpHandler _handler;

    public HttpRequestDispatcher()
    {
        _handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 10000,
            EnableMultipleHttp2Connections = true,
            ConnectTimeout = TimeSpan.FromSeconds(2) // Fast failover if target port is closed
        };

        _httpClient = new HttpClient(_handler, disposeHandler: true);
    }

    public async Task ExecuteAsync(TargetEndpoint target, MetricsCollector collector, CancellationToken cancellationToken)
    {
        long timestamp = Stopwatch.GetTimestamp();

        try
        {
            using var request = BuildRequest(target);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            double elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;

            int statusCode = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                collector.RecordSuccess(statusCode, elapsedMs);
            }
            else
            {
                collector.RecordFailure(statusCode, elapsedMs, isTimeout: false, isConnectionError: false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            double elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
            collector.RecordFailure(null, elapsedMs, isTimeout: true, isConnectionError: false);
        }
        catch (HttpRequestException)
        {
            double elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
            collector.RecordFailure(null, elapsedMs, isTimeout: false, isConnectionError: true);
        }
        catch
        {
            double elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
            collector.RecordFailure(null, elapsedMs, isTimeout: false, isConnectionError: true);
        }
    }

    public async Task ExecutePingAsync(TargetEndpoint target, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(target);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private static HttpRequestMessage BuildRequest(TargetEndpoint target)
    {
        var method = new HttpMethod(target.Method.ToUpperInvariant());
        var request = new HttpRequestMessage(method, target.Url);

        foreach (var (header, value) in target.Headers)
        {
            request.Headers.TryAddWithoutValidation(header, value);
        }

        if (!string.IsNullOrWhiteSpace(target.Body) && method != HttpMethod.Get)
        {
            request.Content = new StringContent(target.Body, Encoding.UTF8, target.ContentType);
        }

        return request;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }
}
