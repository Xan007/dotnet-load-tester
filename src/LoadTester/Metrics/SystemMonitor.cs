// Coordinates background system resource telemetry sampling during load test rounds.

using System.Diagnostics;

namespace LoadTester.Metrics;

public sealed class SystemMonitor : IDisposable
{
    private readonly List<SystemSample> _samples = new();
    private readonly object _sync = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly ISystemResourceSampler _sampler;
    private Task? _monitorTask;

    public SystemMonitor()
    {
        double totalMemoryMb = Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024.0), 2);
        _sampler = CreateSampler(totalMemoryMb);
    }

    private static ISystemResourceSampler CreateSampler(double totalMemoryMb)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                return new WindowsResourceSampler(totalMemoryMb);
            }
            catch
            {
                // Fallback on permission/counter failure
            }
        }
        return new ProcessResourceSampler(totalMemoryMb);
    }

    public void Start()
    {
        _stopwatch.Restart();
        RecordSample();
        _monitorTask = Task.Run(MonitorLoopAsync);
    }

    public async Task<HardwareTelemetrySummary> StopAsync()
    {
        _cts.Cancel();
        if (_monitorTask != null)
        {
            try { await _monitorTask; } catch (OperationCanceledException) { }
        }
        _stopwatch.Stop();
        RecordSample();

        lock (_sync)
        {
            if (_samples.Count == 0)
            {
                return new HardwareTelemetrySummary { TotalSystemMemoryMb = _sampler.TotalSystemMemoryMb };
            }

            return new HardwareTelemetrySummary
            {
                TotalSystemMemoryMb = _sampler.TotalSystemMemoryMb,
                AvgCpuPercent = Math.Round(_samples.Average(s => s.CpuPercent), 2),
                PeakCpuPercent = Math.Round(_samples.Max(s => s.CpuPercent), 2),
                AvgMemoryUsedMb = Math.Round(_samples.Average(s => s.MemoryUsedMb), 2),
                PeakMemoryUsedMb = Math.Round(_samples.Max(s => s.MemoryUsedMb), 2),
                Samples = new List<SystemSample>(_samples)
            };
        }
    }

    private void RecordSample()
    {
        var (cpu, memUsed) = _sampler.Sample();
        lock (_sync)
        {
            _samples.Add(new SystemSample
            {
                SecondsElapsed = Math.Round(_stopwatch.Elapsed.TotalSeconds, 2),
                CpuPercent = cpu,
                MemoryUsedMb = memUsed
            });
        }
    }

    private async Task MonitorLoopAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(500, _cts.Token);
                RecordSample();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Resilient loop
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _sampler.Dispose();
    }
}
