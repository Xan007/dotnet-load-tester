// Analyzes multi-round results to detect performance degradation and breaking points.

using LoadTester.Metrics;

namespace LoadTester.Analysis;

public sealed class ResilienceAnalyzer : IResilienceAnalyzer
{
    public DegradationReport Analyze(IReadOnlyList<RoundResult> rounds)
    {
        if (rounds.Count < 2)
        {
            return new DegradationReport
            {
                DegradationDetected = false,
                SummaryMessage = "Insufficient rounds to determine degradation point."
            };
        }

        var baseline = rounds[0];

        for (int i = 1; i < rounds.Count; i++)
        {
            var round = rounds[i];
            var reasons = new List<string>();

            bool latencySpike = baseline.Latency.MeanMs > 0 && round.Latency.MeanMs > (baseline.Latency.MeanMs * 2.5);
            bool failureDetected = round.FailedCount > 0;
            bool throughputDrop = i > 1 && round.RequestsPerSecond < (rounds[i - 1].RequestsPerSecond * 0.85);

            if (failureDetected)
            {
                reasons.Add($"Error threshold breached: {round.FailedCount} failures recorded ({round.SuccessRatePercent:F1}% success rate).");
            }

            if (latencySpike)
            {
                reasons.Add($"Latency degradation: Mean latency increased from {baseline.Latency.MeanMs:F1} ms to {round.Latency.MeanMs:F1} ms (>2.5x baseline).");
            }

            if (throughputDrop)
            {
                reasons.Add($"Throughput saturation: RPS dropped from {rounds[i - 1].RequestsPerSecond:F1} to {round.RequestsPerSecond:F1} req/s.");
            }

            if (reasons.Count > 0)
            {
                return new DegradationReport
                {
                    DegradationDetected = true,
                    DegradationRound = round,
                    SummaryMessage = $"Degradation observed starting at {round.RoundName} (Concurrency: {round.Concurrency}).",
                    DegradationReasons = reasons
                };
            }
        }

        return new DegradationReport
        {
            DegradationDetected = false,
            SummaryMessage = "No critical degradation observed across configured rounds. The service remained stable under maximum tested load."
        };
    }
}

