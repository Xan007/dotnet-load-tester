// Formats and displays load test metrics, ASCII tables, and degradation warnings in the console.

using LoadTester.Analysis;
using LoadTester.Metrics;

namespace LoadTester.Reporting;

public sealed class ConsoleReportPresenter : IReportPresenter
{
    public void ShowProgress(long total, long failed, double rps)
    {
        Console.Write($"\r[PROGRESS] Sent: {total,-6} | Failures: {failed,-4} | Current Throughput: {rps,6:F1} req/s ");
    }

    public void ShowRoundSummary(RoundResult result)
    {
        Console.WriteLine("\n------------------------------------------------------------------------");
        Console.WriteLine($" Summary for: {result.RoundName}");
        Console.WriteLine("------------------------------------------------------------------------");
        Console.WriteLine($" Concurrency          : {result.Concurrency} workers");
        Console.WriteLine($" Duration             : {result.DurationSeconds:F2} seconds");
        Console.WriteLine($" Total Requests       : {result.TotalRequests:N0}");
        Console.WriteLine($" Success Rate         : {result.SuccessRatePercent:F2}% ({result.SuccessCount:N0} passed)");
        Console.WriteLine($" Failures             : {result.FailedCount:N0} (Timeouts: {result.TimeoutErrors}, ConnErrors: {result.ConnectionErrors})");
        Console.WriteLine($" Throughput (RPS)     : {result.RequestsPerSecond:F2} req/sec");
        Console.WriteLine($" Latency (Min / Mean) : {result.Latency.MinMs:F2} ms / {result.Latency.MeanMs:F2} ms");
        Console.WriteLine($" Latency (P50 / P95)  : {result.Latency.P50Ms:F2} ms / {result.Latency.P95Ms:F2} ms");
        Console.WriteLine($" Latency (P99 / Max)  : {result.Latency.P99Ms:F2} ms / {result.Latency.MaxMs:F2} ms");
        Console.WriteLine($" Host CPU Usage       : Avg {result.Hardware.AvgCpuPercent:F1}% | Peak {result.Hardware.PeakCpuPercent:F1}%");
        Console.WriteLine($" Host Memory (RAM)    : Avg {result.Hardware.AvgMemoryUsedMb:F0} MB | Peak {result.Hardware.PeakMemoryUsedMb:F0} MB");
        Console.WriteLine(" Status Code Breakdown:");
        foreach (var (code, count) in result.StatusCodeCounts)
        {
            Console.WriteLine($"   - HTTP {code} : {count:N0}");
        }
        Console.WriteLine("------------------------------------------------------------------------");
    }

    public void ShowSuiteComparison(IReadOnlyList<RoundResult> rounds, DegradationReport degradationReport)
    {
        Console.WriteLine("\n========================================================================================================================");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("                                             PROGRESSIVE RESILIENCE SUITE SUMMARY                                       ");
        Console.ResetColor();
        Console.WriteLine("========================================================================================================================");
        Console.WriteLine(string.Format("{0,-25} | {1,5} | {2,8} | {3,9} | {4,9} | {5,9} | {6,9} | {7,9} | {8,8}",
            "Round Name", "Conc", "Requests", "Success %", "RPS", "Mean (ms)", "P95 (ms)", "Avg CPU %", "RAM (MB)"));
        Console.WriteLine(new string('-', 120));

        foreach (var r in rounds)
        {
            Console.WriteLine(string.Format("{0,-25} | {1,5} | {2,8} | {3,8}% | {4,9:F1} | {5,9:F1} | {6,9:F1} | {7,8:F1}% | {8,8:F0}",
                r.RoundName,
                r.Concurrency,
                r.TotalRequests,
                r.SuccessRatePercent,
                r.RequestsPerSecond,
                r.Latency.MeanMs,
                r.Latency.P95Ms,
                r.Hardware.AvgCpuPercent,
                r.Hardware.AvgMemoryUsedMb));
        }
        Console.WriteLine("========================================================================================================================");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[ANALYSIS] Resilience & Degradation Assessment:");
        Console.ResetColor();

        if (degradationReport.DegradationDetected)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" -> {degradationReport.SummaryMessage}");
            foreach (var reason in degradationReport.DegradationReasons)
            {
                Console.WriteLine($"    * {reason}");
            }
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" -> {degradationReport.SummaryMessage}");
            Console.ResetColor();
        }
    }

    public void ShowHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== {title} ===");
        Console.ResetColor();
    }

    public void ShowSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void ShowWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void ShowInfo(string message)
    {
        Console.WriteLine(message);
    }
}

