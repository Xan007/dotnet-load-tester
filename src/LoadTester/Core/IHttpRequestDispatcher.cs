// Contract for executing individual HTTP benchmark requests.

using LoadTester.Configuration;
using LoadTester.Metrics;

namespace LoadTester.Core;

public interface IHttpRequestDispatcher : IDisposable
{
    Task ExecuteAsync(TargetEndpoint target, MetricsCollector collector, CancellationToken cancellationToken);
    Task ExecutePingAsync(TargetEndpoint target, CancellationToken cancellationToken);
}

