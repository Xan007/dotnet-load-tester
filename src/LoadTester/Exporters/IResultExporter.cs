// Contract for exporting load test results.

using LoadTester.Configuration;
using LoadTester.Metrics;

namespace LoadTester.Exporters;

public sealed class TestSessionReport
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString("N");
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string MachineName { get; init; } = Environment.MachineName;
    public string OperatingSystem { get; init; } = Environment.OSVersion.ToString();
    public int ProcessorCount { get; init; } = Environment.ProcessorCount;
    public TargetEndpoint Target { get; init; } = new();
    public List<RoundResult> Rounds { get; init; } = new();
}

public interface IResultExporter
{
    Task<string> ExportAsync(TestSessionReport report, string outputDirectory, CancellationToken cancellationToken = default);
}

