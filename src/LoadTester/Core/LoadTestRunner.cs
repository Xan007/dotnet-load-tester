// Orchestrates load testing suites, health checks, resilience analysis, and report exports with cancellation support.

using System.Diagnostics;
using Spectre.Console;
using LoadTester.Analysis;
using LoadTester.Configuration;
using LoadTester.Exporters;
using LoadTester.Metrics;
using LoadTester.Reporting;

namespace LoadTester.Core;

public sealed class LoadTestRunner : ILoadTestRunner
{
    private readonly ILoadTestEngine _engine;
    private readonly IResultExporter _exporter;
    private readonly IReportPresenter _presenter;
    private readonly IResilienceAnalyzer _analyzer;

    public LoadTestRunner(
        ILoadTestEngine engine,
        IResultExporter exporter,
        IReportPresenter presenter,
        IResilienceAnalyzer analyzer)
    {
        _engine = engine;
        _exporter = exporter;
        _presenter = presenter;
        _analyzer = analyzer;
    }

    public async Task RunFullSuiteAsync(LoadTestOptions options, CancellationToken cancellationToken = default)
    {
        _presenter.ShowSuccess($"[START] Starting Full Progressive Resilience Suite ({options.Rounds.Count} rounds)");
        _presenter.ShowInfo($"Endpoint: {options.Target.Method} {options.Target.Url}");
        _presenter.ShowInfo("[grey]Tip: Press Ctrl+C at any moment to cancel gracefully.[/]\n");

        var completedRounds = new List<RoundResult>();

        if (options.WarmupRequests > 0 && !cancellationToken.IsCancellationRequested)
        {
            Console.Write($"[WARMUP] Performing {options.WarmupRequests} initial warm-up requests... ");
            await _engine.RunWarmupAsync(options.Target, options.WarmupRequests, cancellationToken);
            _presenter.ShowSuccess("Done.");
        }

        for (int i = 0; i < options.Rounds.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var round = options.Rounds[i];
            Console.WriteLine("\n========================================================================");
            _presenter.ShowWarning($"[ROUND {i + 1}/{options.Rounds.Count}] {round.Name}");
            _presenter.ShowInfo($"Concurrency: {round.Concurrency} workers | Total: {round.TotalRequests} requests");
            Console.WriteLine("------------------------------------------------------------------------");

            try
            {
                var result = await _engine.ExecuteRoundAsync(options.Target, round, _presenter.ShowProgress, cancellationToken);
                Console.WriteLine();
                completedRounds.Add(result);
                _presenter.ShowRoundSummary(result);
            }
            catch (OperationCanceledException)
            {
                _presenter.ShowWarning("\n[CANCELLED] Round execution stopped early.");
                break;
            }

            if (i < options.Rounds.Count - 1 && !cancellationToken.IsCancellationRequested)
            {
                _presenter.ShowInfo("\n[COOLDOWN] Pausing 3 seconds before next round to allow socket & memory stabilization...");
                try
                {
                    await Task.Delay(3000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            _presenter.ShowWarning("\n[ABORTED] Suite was stopped early by user request.");
        }

        if (completedRounds.Count > 0)
        {
            var degradationReport = _analyzer.Analyze(completedRounds);
            _presenter.ShowSuiteComparison(completedRounds, degradationReport);

            var report = new TestSessionReport
            {
                Target = options.Target,
                Rounds = completedRounds
            };

            string exportPath = await _exporter.ExportAsync(report, options.ExportDirectory, CancellationToken.None);
            _presenter.ShowHeader("EXPORT");
            _presenter.ShowSuccess($"Benchmark data successfully exported to:\n{exportPath}");
        }
        else
        {
            _presenter.ShowInfo("No rounds were completed. Skipping report export.");
        }
    }

    public async Task RunSingleRoundAsync(LoadTestOptions options, RoundScenario scenario, CancellationToken cancellationToken = default)
    {
        _presenter.ShowInfo($"[INFO] Target: {options.Target.Method} {options.Target.Url}");
        _presenter.ShowInfo($"[INFO] Concurrency: {scenario.Concurrency} | Target Requests: {scenario.TotalRequests}");
        _presenter.ShowInfo("[grey]Tip: Press Ctrl+C at any moment to cancel gracefully.[/]\n");

        try
        {
            var result = await _engine.ExecuteRoundAsync(options.Target, scenario, _presenter.ShowProgress, cancellationToken);
            Console.WriteLine();
            _presenter.ShowRoundSummary(result);

            var report = new TestSessionReport
            {
                Target = options.Target,
                Rounds = new List<RoundResult> { result }
            };

            string exportPath = await _exporter.ExportAsync(report, options.ExportDirectory, CancellationToken.None);
            _presenter.ShowSuccess($"[SUCCESS] Result exported to: {exportPath}");
        }
        catch (OperationCanceledException)
        {
            _presenter.ShowWarning("\n[ABORTED] Single round cancelled by user.");
        }
    }

    public async Task RunHealthCheckAsync(LoadTestOptions options, CancellationToken cancellationToken = default)
    {
        _presenter.ShowHeader("HEALTH CHECK");

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Pinging {options.Target.Method} {options.Target.Url}...", async _ =>
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(options.Target.TimeoutSeconds) };
                try
                {
                    var sw = Stopwatch.StartNew();
                    using var response = await client.GetAsync(options.Target.Url, cancellationToken);
                    sw.Stop();

                    string status = $"Response: {(int)response.StatusCode} {response.StatusCode} in {sw.ElapsedMilliseconds} ms";
                    if (response.IsSuccessStatusCode)
                    {
                        _presenter.ShowSuccess(status);
                    }
                    else
                    {
                        _presenter.ShowError(status);
                    }
                }
                catch (OperationCanceledException)
                {
                    _presenter.ShowWarning("Health check cancelled.");
                }
                catch (Exception ex)
                {
                    _presenter.ShowError($"Failed to connect: {ex.Message}");
                }
            });
    }
}
