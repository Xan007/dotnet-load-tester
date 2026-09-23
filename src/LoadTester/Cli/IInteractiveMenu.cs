// Contract for terminal interactive menu navigation.

using LoadTester.Configuration;

namespace LoadTester.Cli;

public interface IInteractiveMenu
{
    Task RunLoopAsync(LoadTestOptions options);
}

