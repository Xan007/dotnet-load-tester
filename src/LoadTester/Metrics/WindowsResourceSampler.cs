// Native Windows hardware telemetry sampler using performance counters.

using System.Diagnostics;
using System.Runtime.Versioning;

namespace LoadTester.Metrics;

[SupportedOSPlatform("windows")]
public sealed class WindowsResourceSampler : ISystemResourceSampler
{
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memCounter;
    public double TotalSystemMemoryMb { get; }

    public WindowsResourceSampler(double totalMemoryMb)
    {
        TotalSystemMemoryMb = totalMemoryMb;
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
        _memCounter = new PerformanceCounter("Memory", "Available MBytes", readOnly: true);
        _cpuCounter.NextValue();
    }

    public (double CpuPercent, double MemoryUsedMb) Sample()
    {
        float cpu = _cpuCounter.NextValue();
        float availMb = _memCounter.NextValue();
        double usedMb = Math.Max(0, TotalSystemMemoryMb - availMb);

        return (Math.Clamp(Math.Round(cpu, 2), 0.0, 100.0), Math.Round(usedMb, 2));
    }

    public void Dispose()
    {
        _cpuCounter.Dispose();
        _memCounter.Dispose();
    }
}

