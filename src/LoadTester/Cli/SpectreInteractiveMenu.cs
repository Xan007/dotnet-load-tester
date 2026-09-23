// Minimalist interactive terminal navigation for load testing workflows.

using Spectre.Console;
using LoadTester.Configuration;
using LoadTester.Core;
using LoadTester.Reporting;

namespace LoadTester.Cli;

public sealed class SpectreInteractiveMenu : IInteractiveMenu
{
    private readonly ILoadTestRunner _runner;
    private readonly IReportPresenter _presenter;

    public SpectreInteractiveMenu(ILoadTestRunner runner, IReportPresenter presenter)
    {
        _runner = runner;
        _presenter = presenter;
    }

    public async Task RunLoopAsync(LoadTestOptions options)
    {
        while (true)
        {
            AnsiConsole.Clear();
            RenderHeader(options);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Menu:[/]")
                    .PageSize(10)
                    .UseConverter(Markup.Escape)
                    .AddChoices(
                        "1. Run All Rounds",
                        "2. Run Single Round",
                        "3. Custom Test",
                        "4. Ping Endpoint",
                        "5. Configure Target",
                        "6. Environment Specs",
                        "7. Exit"
                    ));

            AnsiConsole.WriteLine();

            if (choice.StartsWith("1."))
            {
                await ExecuteWithCancellationAsync(ct => _runner.RunFullSuiteAsync(options, ct));
                WaitForKey();
            }
            else if (choice.StartsWith("2."))
            {
                await RunSpecificRoundWorkflowAsync(options);
            }
            else if (choice.StartsWith("3."))
            {
                await RunCustomRoundWorkflowAsync(options);
            }
            else if (choice.StartsWith("4."))
            {
                await ExecuteWithCancellationAsync(ct => _runner.RunHealthCheckAsync(options, ct));
                WaitForKey();
            }
            else if (choice.StartsWith("5."))
            {
                ConfigureTargetMenu(options);
            }
            else if (choice.StartsWith("6."))
            {
                DisplayEnvironmentAndHardware(options);
                WaitForKey();
            }
            else if (choice.StartsWith("7."))
            {
                _presenter.ShowInfo("[bold cyan]Exiting.[/]");
                return;
            }
        }
    }

    private static async Task ExecuteWithCancellationAsync(Func<CancellationToken, Task> action)
    {
        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            AnsiConsole.MarkupLine("\n[bold yellow][[ABORT]][/] Stopping workers...");
        };

        Console.CancelKeyPress += cancelHandler;
        try
        {
            await action(cts.Token);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow][[CANCELLED]][/] Test cancelled.");
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static void RenderHeader(LoadTestOptions options)
    {
        var panel = new Panel(new FigletText("LOAD TESTER").Centered().Color(Color.Cyan1))
            .Border(BoxBorder.Double)
            .BorderStyle(new Style(Color.Cyan1));

        AnsiConsole.Write(panel);

        var infoTable = new Table().Border(TableBorder.Minimal);
        infoTable.AddColumn("Endpoint");
        infoTable.AddColumn("Timeout");
        infoTable.AddColumn("Rounds");
        infoTable.AddRow(
            $"[bold white]{options.Target.Method} {options.Target.Url}[/]",
            $"{options.Target.TimeoutSeconds}s",
            $"{options.Rounds.Count} configured");

        AnsiConsole.Write(infoTable);
        AnsiConsole.WriteLine();
    }

    private async Task RunSpecificRoundWorkflowAsync(LoadTestOptions options)
    {
        var choices = options.Rounds
            .Select((r, i) => $"{i + 1}. {r.Name} ({r.Concurrency} workers, {r.TotalRequests:N0} req)")
            .Concat(new[] { "<- Back" })
            .ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold yellow]Select round:[/]")
                .UseConverter(Markup.Escape)
                .AddChoices(choices));

        if (selected.StartsWith("<-"))
        {
            return;
        }

        int roundIndex = int.Parse(selected.Substring(0, selected.IndexOf('.'))) - 1;
        var round = options.Rounds[roundIndex];

        AnsiConsole.MarkupLine($"\n[bold cyan]Starting:[/] {round.Name}\n");
        await ExecuteWithCancellationAsync(ct => _runner.RunSingleRoundAsync(options, round, ct));
        WaitForKey();
    }

    private async Task RunCustomRoundWorkflowAsync(LoadTestOptions options)
    {
        while (true)
        {
            AnsiConsole.Clear();
            _presenter.ShowHeader("CUSTOM LOAD TEST");
            AnsiConsole.MarkupLine($"Target: [bold cyan]{options.Target.Method} {options.Target.Url}[/]\n");

            var preAction = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Action:[/]")
                    .UseConverter(Markup.Escape)
                    .AddChoices(
                        "1. Configure & Run",
                        "2. <- Back"
                    ));

            if (preAction.StartsWith("2."))
            {
                return;
            }

            int concurrency = AnsiConsole.Prompt(
                new TextPrompt<int>("[bold cyan]Concurrency workers (0 to cancel):[/]")
                    .DefaultValue(20)
                    .Validate(c => c >= 0 ? ValidationResult.Success() : ValidationResult.Error("[red]Cannot be negative[/]")));

            if (concurrency == 0)
            {
                return;
            }

            int requests = AnsiConsole.Prompt(
                new TextPrompt<int>("[bold cyan]Total requests (0 to cancel):[/]")
                    .DefaultValue(1000)
                    .Validate(r => r >= 0 ? ValidationResult.Success() : ValidationResult.Error("[red]Cannot be negative[/]")));

            if (requests == 0)
            {
                return;
            }

            var confirmTable = new Table().Border(TableBorder.Rounded);
            confirmTable.AddColumn("Parameter");
            confirmTable.AddColumn("Value");
            confirmTable.AddRow("Target URL", $"{options.Target.Method} {options.Target.Url}");
            confirmTable.AddRow("Concurrency", $"{concurrency} workers");
            confirmTable.AddRow("Total Requests", $"{requests:N0}");
            AnsiConsole.Write(confirmTable);

            var confirm = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Ready to run?[/]")
                    .UseConverter(Markup.Escape)
                    .AddChoices(
                        "1. Start",
                        "2. Reconfigure",
                        "3. <- Cancel"
                    ));

            if (confirm.StartsWith("3."))
            {
                return;
            }

            if (confirm.StartsWith("2."))
            {
                continue;
            }

            var scenario = new RoundScenario
            {
                Name = $"Custom (C={concurrency}, N={requests})",
                Concurrency = concurrency,
                TotalRequests = requests
            };

            await ExecuteWithCancellationAsync(ct => _runner.RunSingleRoundAsync(options, scenario, ct));
            WaitForKey();
            return;
        }
    }

    private static void ConfigureTargetMenu(LoadTestOptions options)
    {
        while (true)
        {
            AnsiConsole.Clear();
            var table = new Table().Border(TableBorder.Rounded).Title("[bold cyan]Target Configuration[/]");
            table.AddColumn("Setting");
            table.AddColumn("Value");
            table.AddRow("Target URL", $"[bold white]{options.Target.Url}[/]");
            table.AddRow("HTTP Method", $"[bold yellow]{options.Target.Method}[/]");
            table.AddRow("Timeout", $"{options.Target.TimeoutSeconds}s");
            table.AddRow("JSON Body", string.IsNullOrWhiteSpace(options.Target.Body) ? "[grey]None[/]" : $"[green]{options.Target.Body.Length} chars[/]");
            AnsiConsole.Write(table);

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Edit setting:[/]")
                    .UseConverter(Markup.Escape)
                    .AddChoices(
                        "1. Target URL",
                        "2. HTTP Method",
                        "3. Timeout",
                        "4. JSON Body",
                        "5. <- Back"
                    ));

            if (action.StartsWith("5."))
            {
                return;
            }

            if (action.StartsWith("1."))
            {
                string newUrl = AnsiConsole.Prompt(
                    new TextPrompt<string>("[bold cyan]New URL (empty to keep):[/]")
                        .AllowEmpty()
                        .DefaultValue(options.Target.Url));

                if (!string.IsNullOrWhiteSpace(newUrl))
                {
                    options.Target.Url = newUrl.Trim();
                    AnsiConsole.MarkupLine("[bold green][[OK]][/] URL updated.");
                    Thread.Sleep(600);
                }
            }
            else if (action.StartsWith("2."))
            {
                string newMethod = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold cyan]Select Method:[/]")
                        .UseConverter(Markup.Escape)
                        .AddChoices("GET", "POST", "PUT", "DELETE", "PATCH", "Cancel"));

                if (newMethod != "Cancel")
                {
                    options.Target.Method = newMethod;
                    AnsiConsole.MarkupLine($"[bold green][[OK]][/] Method set to {newMethod}.");
                    Thread.Sleep(600);
                }
            }
            else if (action.StartsWith("3."))
            {
                int newTimeout = AnsiConsole.Prompt(
                    new TextPrompt<int>("[bold cyan]Timeout seconds (1-120):[/]")
                        .DefaultValue(options.Target.TimeoutSeconds)
                        .Validate(t => t is > 0 and <= 120 ? ValidationResult.Success() : ValidationResult.Error("[red]Must be 1-120s[/]")));

                options.Target.TimeoutSeconds = newTimeout;
                AnsiConsole.MarkupLine("[bold green][[OK]][/] Timeout updated.");
                Thread.Sleep(600);
            }
            else if (action.StartsWith("4."))
            {
                string body = AnsiConsole.Prompt(
                    new TextPrompt<string>("[bold cyan]JSON payload ('none' to clear):[/]")
                        .AllowEmpty());

                if (body.Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    options.Target.Body = null;
                    AnsiConsole.MarkupLine("[bold green][[OK]][/] Body cleared.");
                }
                else if (!string.IsNullOrWhiteSpace(body))
                {
                    options.Target.Body = body.Trim();
                    AnsiConsole.MarkupLine("[bold green][[OK]][/] Body saved.");
                }
                Thread.Sleep(600);
            }
        }
    }

    private static void DisplayEnvironmentAndHardware(LoadTestOptions options)
    {
        AnsiConsole.Clear();
        var hardwareTable = new Table().Border(TableBorder.Rounded).Title("[bold cyan]Environment & Hardware Specs[/]");
        hardwareTable.AddColumn(new TableColumn("[bold]Parameter[/]").LeftAligned());
        hardwareTable.AddColumn(new TableColumn("[bold]Value[/]").LeftAligned());

        double totalRamGb = Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024.0 * 1024.0), 2);

        hardwareTable.AddRow("Machine Name", Environment.MachineName);
        hardwareTable.AddRow("Operating System", Environment.OSVersion.VersionString);
        hardwareTable.AddRow("Processor Cores", $"{Environment.ProcessorCount} logical cores");
        hardwareTable.AddRow("Physical RAM", $"{totalRamGb:F2} GB");
        hardwareTable.AddRow(".NET Version", Environment.Version.ToString());
        hardwareTable.AddRow("Current Target", $"{options.Target.Method} {options.Target.Url}");

        AnsiConsole.Write(hardwareTable);

        AnsiConsole.WriteLine();
        var roundsTable = new Table().Border(TableBorder.Rounded).Title("[bold yellow]Configured Progressive Rounds[/]");
        roundsTable.AddColumn("Round");
        roundsTable.AddColumn("Name");
        roundsTable.AddColumn(new TableColumn("Concurrency").RightAligned());
        roundsTable.AddColumn(new TableColumn("Total Requests").RightAligned());

        for (int i = 0; i < options.Rounds.Count; i++)
        {
            var r = options.Rounds[i];
            roundsTable.AddRow($"Round {i + 1}", r.Name, $"{r.Concurrency} workers", $"{r.TotalRequests:N0} req");
        }

        AnsiConsole.Write(roundsTable);
    }

    private static void WaitForKey()
    {
        AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
