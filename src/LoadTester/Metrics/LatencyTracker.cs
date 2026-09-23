// Tracks and calculates latency percentiles and distribution.

namespace LoadTester.Metrics;

public sealed class LatencyStats
{
    public double MinMs { get; init; }
    public double MaxMs { get; init; }
    public double MeanMs { get; init; }
    public double P50Ms { get; init; }
    public double P90Ms { get; init; }
    public double P95Ms { get; init; }
    public double P99Ms { get; init; }
}

public sealed class LatencyTracker
{
    private readonly List<double> _samples = new();
    private readonly object _sync = new();

    public void Record(double latencyMs)
    {
        lock (_sync)
        {
            _samples.Add(latencyMs);
        }
    }

    public LatencyStats ComputeStats()
    {
        List<double> sorted;
        lock (_sync)
        {
            if (_samples.Count == 0)
            {
                return new LatencyStats();
            }
            sorted = new List<double>(_samples);
        }

        sorted.Sort();
        int count = sorted.Count;

        return new LatencyStats
        {
            MinMs = Math.Round(sorted[0], 2),
            MaxMs = Math.Round(sorted[^1], 2),
            MeanMs = Math.Round(sorted.Average(), 2),
            P50Ms = Math.Round(GetPercentile(sorted, 50), 2),
            P90Ms = Math.Round(GetPercentile(sorted, 90), 2),
            P95Ms = Math.Round(GetPercentile(sorted, 95), 2),
            P99Ms = Math.Round(GetPercentile(sorted, 99), 2)
        };
    }

    private static double GetPercentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 1) return sorted[0];
        double rank = (percentile / 100.0) * (sorted.Count - 1);
        int lowIndex = (int)Math.Floor(rank);
        int highIndex = (int)Math.Ceiling(rank);

        if (lowIndex == highIndex)
        {
            return sorted[lowIndex];
        }

        double weight = rank - lowIndex;
        return sorted[lowIndex] + weight * (sorted[highIndex] - sorted[lowIndex]);
    }
}

