// Interactive console terminal navigation for load testing workflows.

using LoadTester.Configuration;
using LoadTester.Core;
using LoadTester.Reporting;

namespace LoadTester.Cli;

public sealed class InteractiveMenu : IInteractiveMenu
{
    private readonly ILoadTestRunner _runner;
    private readonly IReportPresenter _presenter;

    public InteractiveMenu(ILoadTestRunner runner, IReportPresenter presenter)
    {
        _runner = runner;
        _presenter = presenter;
    }

    public async Task RunLoopAsync(LoadTestOptions options)
    {
        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================================================");
            Console.WriteLine("          .NET 8 WEB API LOAD TESTING & RESILIENCE BENCHMARK            ");
            Console.WriteLine("========================================================================");
            Console.ResetColor();
            Console.WriteLine($" Target URL : {options.Target.Method} {options.Target.Url}");
            Console.WriteLine($" Timeout    : {options.Target.TimeoutSeconds}s");
            Console.WriteLine($" Rounds     : {options.Rounds.Count} progressive scenarios configured");
            Console.WriteLine("------------------------------------------------------------------------");
            Console.WriteLine(" 1. Run Full Progressive Resilience Suite (5 Rounds - Academic Report)");
            Console.WriteLine(" 2. Run Custom Single Round");
            Console.WriteLine(" 3. Run Endpoint Connectivity Health Check");
            Console.WriteLine(" 4. Change Target URL & Method");
            Console.WriteLine(" 5. View Scenarios Configuration");
            Console.WriteLine(" 6. Exit");
            Console.WriteLine("------------------------------------------------------------------------");
            Console.Write("Select an option [1-6]: ");

            string? choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await _runner.RunFullSuiteAsync(options);
                    WaitForKey();
                    break;
                case "2":
                    await RunCustomRoundAsync(options);
                    WaitForKey();
                    break;
                case "3":
                    await _runner.RunHealthCheckAsync(options);
                    WaitForKey();
                    break;
                case "4":
                    ConfigureTarget(options);
                    break;
                case "5":
                    PrintScenarios(options);
                    WaitForKey();
                    break;
                case "6":
                    _presenter.ShowInfo("Exiting. Good luck with your laboratory analysis!");
                    return;
                default:
                    _presenter.ShowWarning("Invalid selection. Press any key to try again...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private async Task RunCustomRoundAsync(LoadTestOptions options)
    {
        Console.Write("Enter Concurrency [default: 20]: ");
        int concurrency = int.TryParse(Console.ReadLine(), out int c) && c > 0 ? c : 20;

        Console.Write("Enter Total Requests [default: 1000]: ");
        int requests = int.TryParse(Console.ReadLine(), out int r) && r > 0 ? r : 1000;

        var scenario = new RoundScenario
        {
            Name = $"Custom Run (C={concurrency}, N={requests})",
            Concurrency = concurrency,
            TotalRequests = requests
        };

        await _runner.RunSingleRoundAsync(options, scenario);
    }

    private static void ConfigureTarget(LoadTestOptions options)
    {
        Console.Write($"Enter Target URL [Current: {options.Target.Url}]: ");
        string? url = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(url))
        {
            options.Target.Url = url.Trim();
        }

        Console.Write($"Enter HTTP Method (GET, POST, etc.) [Current: {options.Target.Method}]: ");
        string? method = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(method))
        {
            options.Target.Method = method.Trim().ToUpperInvariant();
        }
    }

    private static void PrintScenarios(LoadTestOptions options)
    {
        Console.WriteLine("Configured Progressive Rounds:");
        Console.WriteLine("------------------------------------------------------------------------");
        foreach (var r in options.Rounds)
        {
            Console.WriteLine($" - {r.Name,-28} | Concurrency: {r.Concurrency,4} | Total Requests: {r.TotalRequests,5}");
        }
    }

    private static void WaitForKey()
    {
        Console.WriteLine("\nPress any key to return to main menu...");
        Console.ReadKey();
    }
}

