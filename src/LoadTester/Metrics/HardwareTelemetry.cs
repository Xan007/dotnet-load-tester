// Models for system resource metrics and time-series hardware samples.

namespace LoadTester.Metrics;

public sealed class SystemSample
{
    public double SecondsElapsed { get; init; }
    public double CpuPercent { get; init; }
    public double MemoryUsedMb { get; init; }
}

public sealed class HardwareTelemetrySummary
{
    public double AvgCpuPercent { get; set; }
    public double PeakCpuPercent { get; set; }
    public double AvgMemoryUsedMb { get; set; }
    public double PeakMemoryUsedMb { get; set; }
    public double TotalSystemMemoryMb { get; set; }
    public List<SystemSample> Samples { get; set; } = new();
}

