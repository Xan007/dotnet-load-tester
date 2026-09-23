// Thread-safe accumulator for real-time load test metrics.

using System.Collections.Concurrent;

namespace LoadTester.Metrics;

public sealed class MetricsCollector
{
    private long _totalRequests;
    private long _successCount;
    private long _failedCount;
    private long _timeoutErrors;
    private long _connectionErrors;

    private readonly ConcurrentDictionary<int, long> _statusCodes = new();
    private readonly LatencyTracker _latencyTracker = new();

    public long TotalRequests => Interlocked.Read(ref _totalRequests);
    public long SuccessCount => Interlocked.Read(ref _successCount);
    public long FailedCount => Interlocked.Read(ref _failedCount);

    public void RecordSuccess(int statusCode, double latencyMs)
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Increment(ref _successCount);
        _statusCodes.AddOrUpdate(statusCode, 1, (_, count) => count + 1);
        _latencyTracker.Record(latencyMs);
    }

    public void RecordFailure(int? statusCode, double latencyMs, bool isTimeout, bool isConnectionError)
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Increment(ref _failedCount);

        if (statusCode.HasValue)
        {
            _statusCodes.AddOrUpdate(statusCode.Value, 1, (_, count) => count + 1);
        }

        if (isTimeout)
        {
            Interlocked.Increment(ref _timeoutErrors);
        }
        else if (isConnectionError)
        {
            Interlocked.Increment(ref _connectionErrors);
        }

        _latencyTracker.Record(latencyMs);
    }

    public RoundResult BuildResult(
        string roundName,
        int concurrency,
        string targetUrl,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        HardwareTelemetrySummary hardware)
    {
        double durationSeconds = Math.Max(0.001, (endTimeUtc - startTimeUtc).TotalSeconds);
        long total = TotalRequests;
        long success = SuccessCount;
        long failed = FailedCount;

        double successRate = total > 0 ? Math.Round((double)success / total * 100.0, 2) : 0.0;
        double rps = total > 0 ? Math.Round(total / durationSeconds, 2) : 0.0;

        return new RoundResult
        {
            RoundName = roundName,
            Concurrency = concurrency,
            TargetUrl = targetUrl,
            StartTimeUtc = startTimeUtc,
            EndTimeUtc = endTimeUtc,
            DurationSeconds = Math.Round(durationSeconds, 2),
            TotalRequests = total,
            SuccessCount = success,
            FailedCount = failed,
            SuccessRatePercent = successRate,
            RequestsPerSecond = rps,
            StatusCodeCounts = new Dictionary<int, long>(_statusCodes),
            TimeoutErrors = Interlocked.Read(ref _timeoutErrors),
            ConnectionErrors = Interlocked.Read(ref _connectionErrors),
            Latency = _latencyTracker.ComputeStats(),
            Hardware = hardware
        };
    }
}

