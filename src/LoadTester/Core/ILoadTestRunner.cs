// Contract for executing end-to-end load test workflows.

using LoadTester.Configuration;

namespace LoadTester.Core;

public interface ILoadTestRunner
{
    Task RunFullSuiteAsync(LoadTestOptions options, CancellationToken cancellationToken = default);
    Task RunSingleRoundAsync(LoadTestOptions options, RoundScenario scenario, CancellationToken cancellationToken = default);
    Task RunHealthCheckAsync(LoadTestOptions options, CancellationToken cancellationToken = default);
}

