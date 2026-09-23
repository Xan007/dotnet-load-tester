// Cross-platform resource sampler fallback using process execution metrics.

using System.Diagnostics;

namespace LoadTester.Metrics;

public sealed class ProcessResourceSampler : ISystemResourceSampler
{
    private readonly Process _process;
    private TimeSpan _previousCpuTime;
    private DateTime _previousTime;
    public double TotalSystemMemoryMb { get; }

    public ProcessResourceSampler(double totalMemoryMb)
    {
        TotalSystemMemoryMb = totalMemoryMb;
        _process = Process.GetCurrentProcess();
        _previousCpuTime = _process.TotalProcessorTime;
        _previousTime = DateTime.UtcNow;
    }

    public (double CpuPercent, double MemoryUsedMb) Sample()
    {
        var currentTime = DateTime.UtcNow;
        var currentCpuTime = _process.TotalProcessorTime;

        double timeWindow = (currentTime - _previousTime).TotalMilliseconds;
        double cpu = 0.0;

        if (timeWindow > 0)
        {
            double cpuWindow = (currentCpuTime - _previousCpuTime).TotalMilliseconds;
            cpu = (cpuWindow / (Environment.ProcessorCount * timeWindow)) * 100.0;
        }

        double memoryUsedMb = _process.WorkingSet64 / (1024.0 * 1024.0);
        _previousCpuTime = currentCpuTime;
        _previousTime = currentTime;

        return (Math.Clamp(Math.Round(cpu, 2), 0.0, 100.0), Math.Round(memoryUsedMb, 2));
    }

    public void Dispose()
    {
    }
}

