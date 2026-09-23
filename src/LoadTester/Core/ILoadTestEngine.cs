// Contract for load testing engine execution.

using LoadTester.Configuration;
using LoadTester.Metrics;

namespace LoadTester.Core;

public interface ILoadTestEngine : IDisposable
{
    Task<RoundResult> ExecuteRoundAsync(
        TargetEndpoint target,
        RoundScenario scenario,
        Action<long, long, double>? onProgress = null,
        CancellationToken cancellationToken = default);

    Task RunWarmupAsync(TargetEndpoint target, int warmupRequests, CancellationToken cancellationToken = default);
}

