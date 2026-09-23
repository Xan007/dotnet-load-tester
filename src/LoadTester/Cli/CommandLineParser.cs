// Parses command-line flags and parameters for load test executions.

using LoadTester.Configuration;

namespace LoadTester.Cli;

public sealed class CommandLineParser : ICommandLineParser
{
    public ParsedExecutionCommand Parse(LoadTestOptions options, string[] args)
    {
        if (args.Length == 0)
        {
            return new ParsedExecutionCommand();
        }

        if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
        {
            return new ParsedExecutionCommand { ShowHelp = true };
        }

        bool isSuite = false;
        var scenario = new RoundScenario
        {
            Name = "CLI Execution",
            Concurrency = 10,
            TotalRequests = 500
        };

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg.Equals("--suite", StringComparison.OrdinalIgnoreCase))
            {
                isSuite = true;
            }
            else if (arg.Equals("--url", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.Target.Url = args[++i];
            }
            else if (arg.Equals("--method", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.Target.Method = args[++i].ToUpperInvariant();
            }
            else if (arg.Equals("--concurrency", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out int c)) scenario.Concurrency = c;
            }
            else if (arg.Equals("--requests", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out int r)) scenario.TotalRequests = r;
            }
            else if (arg.Equals("--duration", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out int d)) scenario.DurationSeconds = d;
            }
            else if (arg.Equals("--delay", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out int del)) scenario.PacingDelayMs = del;
            }
            else if (arg.Equals("--timeout", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out int t)) options.Target.TimeoutSeconds = t;
            }
            else if (arg.Equals("--output", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.ExportDirectory = args[++i];
            }
        }

        return new ParsedExecutionCommand
        {
            IsSuite = isSuite,
            IsCustomSingleRound = !isSuite,
            Scenario = scenario
        };
    }

    public void PrintHelp()
    {
        Console.WriteLine(@"
HTTP Load & Resilience Testing Tool (.NET 8)
Usage:
  dotnet run [options]

Options:
  --suite                  Execute the full progressive resilience suite (configured in appsettings)
  --url <url>              Target endpoint URL
  --method <method>        HTTP Method (GET, POST, PUT, DELETE)
  --concurrency <num>      Number of concurrent workers (default: 10)
  --requests <num>         Total number of requests to execute (default: 500)
  --duration <sec>         Duration in seconds (overrides requests count)
  --delay <ms>             Pacing delay between requests per worker (ms)
  --timeout <sec>          HTTP request timeout in seconds (default: 10)
  --output <dir>           Output directory for results JSON (default: results)
  --help, -h               Show this help message
");
    }
}

