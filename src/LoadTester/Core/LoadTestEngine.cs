// High-performance asynchronous HTTP load testing engine using persistent worker pools.

using LoadTester.Configuration;
using LoadTester.Metrics;

namespace LoadTester.Core;

public sealed class LoadTestEngine : ILoadTestEngine, IDisposable
{
    private readonly IHttpRequestDispatcher _dispatcher;

    public LoadTestEngine(IHttpRequestDispatcher? dispatcher = null)
    {
        _dispatcher = dispatcher ?? new HttpRequestDispatcher();
    }

    public async Task RunWarmupAsync(TargetEndpoint target, int warmupRequests, CancellationToken cancellationToken = default)
    {
        if (warmupRequests <= 0) return;

        for (int i = 0; i < warmupRequests; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                await _dispatcher.ExecutePingAsync(target, cancellationToken);
            }
            catch
            {
                // Discard warmup transient exceptions
            }
        }
    }

    public async Task<RoundResult> ExecuteRoundAsync(
        TargetEndpoint target,
        RoundScenario scenario,
        Action<long, long, double>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var collector = new MetricsCollector();
        using var systemMonitor = new SystemMonitor();
        using var roundCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (scenario.DurationSeconds.HasValue && scenario.DurationSeconds.Value > 0)
        {
            roundCts.CancelAfter(TimeSpan.FromSeconds(scenario.DurationSeconds.Value));
        }

        DateTime startTimeUtc = DateTime.UtcNow;
        systemMonitor.Start();

        long requestIndex = 0;
        int targetRequests = scenario.TotalRequests;
        bool isDurationBased = scenario.DurationSeconds.HasValue && scenario.DurationSeconds.Value > 0;

        using var progressTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
        var progressTask = Task.Run(async () =>
        {
            while (!roundCts.Token.IsCancellationRequested)
            {
                try
                {
                    if (await progressTimer.WaitForNextTickAsync(roundCts.Token))
                    {
                        double elapsed = Math.Max(0.05, (DateTime.UtcNow - startTimeUtc).TotalSeconds);
                        long currentTotal = collector.TotalRequests;
                        double currentRps = Math.Round(currentTotal / elapsed, 1);
                        onProgress?.Invoke(currentTotal, collector.FailedCount, currentRps);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, CancellationToken.None);

        try
        {
            int concurrency = Math.Max(1, scenario.Concurrency);
            var workers = new Task[concurrency];

            for (int i = 0; i < concurrency; i++)
            {
                workers[i] = Task.Run(async () =>
                {
                    while (!roundCts.Token.IsCancellationRequested)
                    {
                        if (!isDurationBased && Interlocked.Increment(ref requestIndex) > targetRequests)
                        {
                            break;
                        }

                        await _dispatcher.ExecuteAsync(target, collector, roundCts.Token);

                        if (scenario.PacingDelayMs > 0)
                        {
                            await Task.Delay(scenario.PacingDelayMs, roundCts.Token);
                        }
                    }
                }, CancellationToken.None);
            }

            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException)
        {
            // Handled when test is cancelled
        }
        finally
        {
            roundCts.Cancel();
            await progressTask;
        }

        DateTime endTimeUtc = DateTime.UtcNow;
        var hardwareTelemetry = await systemMonitor.StopAsync();

        return collector.BuildResult(
            scenario.Name,
            scenario.Concurrency,
            target.Url,
            startTimeUtc,
            endTimeUtc,
            hardwareTelemetry);
    }

    public void Dispose()
    {
        _dispatcher.Dispose();
    }
}
