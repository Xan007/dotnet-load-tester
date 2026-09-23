// Contract for hardware CPU and memory sampling strategies.

namespace LoadTester.Metrics;

public interface ISystemResourceSampler : IDisposable
{
    (double CpuPercent, double MemoryUsedMb) Sample();
    double TotalSystemMemoryMb { get; }
}

