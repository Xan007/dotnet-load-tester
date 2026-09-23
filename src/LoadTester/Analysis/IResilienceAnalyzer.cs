// Resilience degradation analysis contract and results.

using LoadTester.Metrics;

namespace LoadTester.Analysis;

public sealed class DegradationReport
{
    public bool DegradationDetected { get; init; }
    public RoundResult? DegradationRound { get; init; }
    public string SummaryMessage { get; init; } = string.Empty;
    public List<string> DegradationReasons { get; init; } = new();
}

public interface IResilienceAnalyzer
{
    DegradationReport Analyze(IReadOnlyList<RoundResult> rounds);
}

