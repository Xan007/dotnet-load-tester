// Global load test configuration options.

namespace LoadTester.Configuration;

public sealed class LoadTestOptions
{
    public TargetEndpoint Target { get; set; } = new();
    public List<RoundScenario> Rounds { get; set; } = new();
    public string ExportDirectory { get; set; } = "results";
    public int WarmupRequests { get; set; } = 15;
}

