// Contract for command-line arguments parsing.

using LoadTester.Configuration;

namespace LoadTester.Cli;

public sealed class ParsedExecutionCommand
{
    public bool ShowHelp { get; init; }
    public bool IsSuite { get; init; }
    public bool IsCustomSingleRound { get; init; }
    public RoundScenario? Scenario { get; init; }
}

public interface ICommandLineParser
{
    ParsedExecutionCommand Parse(LoadTestOptions options, string[] args);
    void PrintHelp();
}

