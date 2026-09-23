// Summary metrics and results for a completed load test round.

namespace LoadTester.Metrics;

public sealed class RoundResult
{
    public string RoundName { get; init; } = string.Empty;
    public int Concurrency { get; init; }
    public string TargetUrl { get; init; } = string.Empty;
    public DateTime StartTimeUtc { get; init; }
    public DateTime EndTimeUtc { get; init; }
    public double DurationSeconds { get; init; }

    public long TotalRequests { get; init; }
    public long SuccessCount { get; init; }
    public long FailedCount { get; init; }
    public double SuccessRatePercent { get; init; }
    public double RequestsPerSecond { get; init; }

    public Dictionary<int, long> StatusCodeCounts { get; init; } = new();
    public long TimeoutErrors { get; init; }
    public long ConnectionErrors { get; init; }

    public LatencyStats Latency { get; init; } = new();
    public HardwareTelemetrySummary Hardware { get; init; } = new();
}

