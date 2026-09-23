// Application bootstrap and entry point.

using Microsoft.Extensions.Configuration;
using LoadTester.Analysis;
using LoadTester.Cli;
using LoadTester.Configuration;
using LoadTester.Core;
using LoadTester.Exporters;
using LoadTester.Reporting;

namespace LoadTester;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var options = new LoadTestOptions();
        config.GetSection("LoadTest").Bind(options);

        // Composition root wiring dependencies
        ICommandLineParser cliParser = new CommandLineParser();
        IReportPresenter presenter = new SpectreReportPresenter();
        IResilienceAnalyzer analyzer = new ResilienceAnalyzer();
        IResultExporter exporter = new JsonResultExporter();
        using IHttpRequestDispatcher dispatcher = new HttpRequestDispatcher();
        using ILoadTestEngine engine = new LoadTestEngine(dispatcher);
        ILoadTestRunner runner = new LoadTestRunner(engine, exporter, presenter, analyzer);
        IInteractiveMenu menu = new SpectreInteractiveMenu(runner, presenter);

        var command = cliParser.Parse(options, args);

        if (command.ShowHelp)
        {
            cliParser.PrintHelp();
            return;
        }

        if (command.IsSuite)
        {
            await runner.RunFullSuiteAsync(options);
            return;
        }

        if (command.IsCustomSingleRound && command.Scenario != null)
        {
            await runner.RunSingleRoundAsync(options, command.Scenario);
            return;
        }

        await menu.RunLoopAsync(options);
    }
}
