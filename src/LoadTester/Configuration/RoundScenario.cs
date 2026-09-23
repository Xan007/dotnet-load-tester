// Parameters for a specific load testing round.

namespace LoadTester.Configuration;

public sealed class RoundScenario
{
    public string Name { get; set; } = "Round 1";
    public int Concurrency { get; set; } = 10;
    public int TotalRequests { get; set; } = 500;
    public int? DurationSeconds { get; set; }
    public int PacingDelayMs { get; set; } = 0;
}

