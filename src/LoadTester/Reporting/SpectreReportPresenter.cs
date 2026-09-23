// Rich terminal presentation for load test summaries using Spectre.Console.

using Spectre.Console;
using LoadTester.Analysis;
using LoadTester.Metrics;

namespace LoadTester.Reporting;

public sealed class SpectreReportPresenter : IReportPresenter
{
    public void ShowProgress(long total, long failed, double rps)
    {
        string failColor = failed > 0 ? "red" : "grey";
        AnsiConsole.Markup($"\r [cyan][[PROGRESS]][/] Sent: [bold white]{total,-6:N0}[/] | Failures: [{failColor}]{failed,-4:N0}[/] | Throughput: [bold green]{rps,6:F1}[/] req/s  ");
    }

    public void ShowRoundSummary(RoundResult result)
    {
        AnsiConsole.WriteLine();
        var table = new Table().Border(TableBorder.Rounded).Title($"[bold cyan]Summary: {result.RoundName}[/]");
        table.AddColumn(new TableColumn("[bold]Metric[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Value[/]").RightAligned());

        string successColor = result.SuccessRatePercent >= 99.0 ? "green" : (result.SuccessRatePercent >= 90.0 ? "yellow" : "red");

        table.AddRow("Concurrency Workers", $"[bold white]{result.Concurrency}[/]");
        table.AddRow("Round Duration", $"{result.DurationSeconds:F2} s");
        table.AddRow("Total Requests", $"{result.TotalRequests:N0}");
        table.AddRow("Success Rate", $"[{successColor}]{result.SuccessRatePercent:F2}%[/] ({result.SuccessCount:N0} passed)");
        table.AddRow("Failures", result.FailedCount > 0 ? $"[bold red]{result.FailedCount:N0}[/] (Timeouts: {result.TimeoutErrors}, ConnErrors: {result.ConnectionErrors})" : "[green]0[/]");
        table.AddRow("Throughput (RPS)", $"[bold green]{result.RequestsPerSecond:F2}[/] req/s");
        table.AddRow("Latency (Min / Mean)", $"{result.Latency.MinMs:F2} ms / [bold]{result.Latency.MeanMs:F2}[/] ms");
        table.AddRow("Latency (P50 / P95)", $"{result.Latency.P50Ms:F2} ms / [bold yellow]{result.Latency.P95Ms:F2}[/] ms");
        table.AddRow("Latency (P99 / Max)", $"{result.Latency.P99Ms:F2} ms / {result.Latency.MaxMs:F2} ms");
        table.AddRow("Host CPU Usage", $"Avg [bold]{result.Hardware.AvgCpuPercent:F1}%[/] | Peak {result.Hardware.PeakCpuPercent:F1}%");
        table.AddRow("Host Memory (RAM)", $"Avg [bold]{result.Hardware.AvgMemoryUsedMb:F0}[/] MB | Peak {result.Hardware.PeakMemoryUsedMb:F0} MB");

        if (result.StatusCodeCounts.Count > 0)
        {
            var statusBreakdown = string.Join(", ", result.StatusCodeCounts.Select(kv => $"HTTP {kv.Key}: [bold]{kv.Value:N0}[/]"));
            table.AddRow("Status Codes", statusBreakdown);
        }

        AnsiConsole.Write(table);
    }

    public void ShowSuiteComparison(IReadOnlyList<RoundResult> rounds, DegradationReport degradationReport)
    {
        AnsiConsole.WriteLine();
        var table = new Table().Border(TableBorder.Rounded).Title("[bold yellow]PROGRESSIVE RESILIENCE SUITE SUMMARY[/]");
        table.AddColumn(new TableColumn("[bold]Round Name[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Conc[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Requests[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Success %[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]RPS[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Mean (ms)[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]P95 (ms)[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Avg CPU %[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]RAM (MB)[/]").RightAligned());

        foreach (var r in rounds)
        {
            string successColor = r.SuccessRatePercent >= 99.0 ? "green" : (r.SuccessRatePercent >= 90.0 ? "yellow" : "red");
            table.AddRow(
                r.RoundName,
                r.Concurrency.ToString(),
                $"{r.TotalRequests:N0}",
                $"[{successColor}]{r.SuccessRatePercent:F1}%[/]",
                $"{r.RequestsPerSecond:F1}",
                $"{r.Latency.MeanMs:F1}",
                $"{r.Latency.P95Ms:F1}",
                $"{r.Hardware.AvgCpuPercent:F1}%",
                $"{r.Hardware.AvgMemoryUsedMb:F0}");
        }

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        if (degradationReport.DegradationDetected)
        {
            var content = string.Join("\n", degradationReport.DegradationReasons.Select(r => $"• {r}"));
            var panel = new Panel($"[bold red]{degradationReport.SummaryMessage}[/]\n\n{content}")
            {
                Header = new PanelHeader("[bold red] Resilience Degradation Detected [/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Red)
            };
            AnsiConsole.Write(panel);
        }
        else
        {
            var panel = new Panel($"[bold green]{degradationReport.SummaryMessage}[/]")
            {
                Header = new PanelHeader("[bold green] Resilience Assessment [/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green)
            };
            AnsiConsole.Write(panel);
        }
    }

    public void ShowHeader(string title)
    {
        AnsiConsole.Write(new Rule($"[bold cyan]{title}[/]").RuleStyle("grey"));
    }

    public void ShowSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[bold green][[OK]][/] {Markup.Escape(message)}");
    }

    public void ShowWarning(string message)
    {
        AnsiConsole.MarkupLine($"[bold yellow][[WARN]][/] {Markup.Escape(message)}");
    }

    public void ShowError(string message)
    {
        AnsiConsole.MarkupLine($"[bold red][[ERR]][/] {Markup.Escape(message)}");
    }

    public void ShowInfo(string message)
    {
        AnsiConsole.WriteLine(message);
    }
}
