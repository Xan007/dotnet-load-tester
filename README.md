# Web API Load & Resilience Benchmark Tool (.NET 8)

A high-performance, asynchronous HTTP load testing and resilience analysis console application built with .NET 8 for academic and enterprise resilience benchmarking.

Designed to conduct controlled load testing rounds against a target Web API, monitor latency percentiles, observe response stability, capture host CPU/RAM consumption, detect API degradation points, and export structured JSON data for performance analysis and graph generation.

---

## Architecture Overview

The application follows SOLID principles with decoupled, single-responsibility components:

```
src/LoadTester/
├── Configuration/          # Strongly-typed configuration models
│   ├── TargetEndpoint.cs   # URL, HTTP method, headers, payload, and timeouts
│   ├── RoundScenario.cs    # Round name, concurrency, request volume, and pacing
│   └── LoadTestOptions.cs  # Global test options and scenario registry
├── Core/                   # Load generation engine
│   ├── IHttpRequestDispatcher.cs # Dispatcher contract
│   ├── HttpRequestDispatcher.cs  # SocketsHttpHandler connection pooling
│   ├── ILoadTestEngine.cs  # Engine contract
│   ├── LoadTestEngine.cs   # High-throughput persistent worker pool
│   ├── ILoadTestRunner.cs  # Test orchestration contract
│   └── LoadTestRunner.cs   # Progressive test orchestrator
├── Metrics/                # Thread-safe metrics collection & system telemetry
│   ├── ISystemResourceSampler.cs # Sampler abstraction
│   ├── WindowsResourceSampler.cs # High-precision Windows Performance Counters
│   ├── ProcessResourceSampler.cs # Cross-platform fallback sampler
│   ├── SystemMonitor.cs    # Background periodic CPU and RAM sampling
│   ├── LatencyTracker.cs   # Nanosecond-precision percentiles (P50, P90, P95, P99)
│   ├── HardwareTelemetry.cs# Telemetry data contracts & time-series samples
│   ├── MetricsCollector.cs # Lock-free atomic metric aggregators
│   └── RoundResult.cs      # Encapsulated round metrics
├── Analysis/               # Automated resilience and degradation detection
│   ├── IResilienceAnalyzer.cs
│   └── ResilienceAnalyzer.cs # Saturation & degradation threshold detection
├── Reporting/              # Console output formatting
│   ├── IReportPresenter.cs # Presenter contract
│   ├── ConsoleReportPresenter.cs # Clean plain text tabular reporter
│   └── SpectreReportPresenter.cs # Rich terminal tables and charts
├── Exporters/              # Data export layer
│   ├── IResultExporter.cs  # Exporter contract
│   └── JsonResultExporter.cs # Formatted JSON report serializer
├── Cli/                    # User interface and argument parsing
│   ├── ICommandLineParser.cs
│   ├── CommandLineParser.cs
│   ├── IInteractiveMenu.cs
│   └── SpectreInteractiveMenu.cs # Interactive terminal menu with cancellation
├── appsettings.json        # Configurable target endpoint & progressive test scenarios
└── Program.cs              # Application composition root
```

---

## Key Features & Design Decisions

- **Zero Socket Exhaustion**: Uses a tuned `SocketsHttpHandler` with `PooledConnectionLifetime` to avoid `TIME_WAIT` socket starvation.
- **Persistent Worker Pool**: Dedicated long-lived tasks loop without per-request thread allocation, eliminating GC pressure and maximizing throughput.
- **Fast Connect Timeout**: Prevents operating system default 21s TCP SYN retries when testing offline or non-existent endpoints.
- **Accurate Timing**: Leverages `Stopwatch.GetTimestamp()` and `Stopwatch.GetElapsedTime()` for nanosecond-accuracy, zero-allocation latency recording.
- **Hardware Telemetry**: Periodically samples real-time host CPU utilization (%) and physical RAM consumption (MB) throughout each round.
- **Progressive Test Scenarios**: Includes 5 pre-configured rounds scaling from baseline to stress/saturation to pinpoint the API's degradation point.
- **Statistical Breakdown**: Calculates Min, Max, Mean, P50, P90, P95, and P99 latency percentiles alongside Requests Per Second (RPS) and HTTP status code distributions.
- **Structured JSON Export**: Dumps complete test results and time-series hardware samples into `results/` for graphing and analysis.

---

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher.
- Windows 10/11 (or modern Linux/macOS with process-level telemetry fallback).
- Python 3 with `matplotlib` (optional, for automated chart generation).

---

## Getting Started

### 1. Build the Application
```bash
dotnet build LoadTester.sln -c Release
```

### 2. Configure the Target Endpoint
Edit `src/LoadTester/appsettings.json` or pass arguments via CLI:
```json
{
  "LoadTest": {
    "Target": {
      "Url": "http://localhost:5000/api/identity/users",
      "Method": "GET",
      "TimeoutSeconds": 10
    }
  }
}
```

### 3. Run Interactively
Launch without arguments to use the interactive terminal menu:
```bash
dotnet run --project src/LoadTester
```

---

## CLI Usage

The tool can also be run in headless or automated modes via command-line arguments:

```bash
# Run the full progressive suite configured in appsettings.json
dotnet run --project src/LoadTester -- --suite

# Run a custom load test against a specific URL
dotnet run --project src/LoadTester -- --url http://localhost:5000/api/test --concurrency 50 --requests 2000

# Specify duration instead of fixed requests
dotnet run --project src/LoadTester -- --url http://localhost:5000/api/test --concurrency 100 --duration 30

# Specify pacing delay between requests per worker (e.g. 5ms)
dotnet run --project src/LoadTester -- --url http://localhost:5000/api/test --concurrency 25 --requests 1000 --delay 5
```

### Available CLI Flags:
| Option | Description | Default |
| :--- | :--- | :--- |
| `--suite` | Executes all 5 progressive resilience rounds defined in `appsettings.json` | Disabled |
| `--url <url>` | Target HTTP endpoint URL | `http://localhost:5000/api/identity/users` |
| `--method <method>` | HTTP verb (`GET`, `POST`, `PUT`, `DELETE`) | `GET` |
| `--concurrency <num>` | Number of concurrent workers | `10` |
| `--requests <num>` | Total number of requests to execute | `500` |
| `--duration <sec>` | Test duration in seconds (overrides `--requests`) | None |
| `--delay <ms>` | Pacing delay between requests per worker | `0` |
| `--timeout <sec>` | Request timeout in seconds | `10` |
| `--output <dir>` | Directory to write exported JSON files | `results` |
| `--help`, `-h` | Prints available command-line options | - |

---

## Mandatory Lab Charts Generation

The lab guide requires 5 specific graphs for the technical report:
1. **Request Load vs. Response Time**: Concurrency / RPS vs. latency (Mean, P95, Max).
2. **Request Load vs. Success Rate**: Concurrency vs. success rate percentage.
3. **Request Load vs. Failure Count**: Concurrency vs. failed request count.
4. **Test Round vs. CPU Usage**: Test round vs. average and peak CPU usage.
5. **Test Round vs. Memory Usage**: Test round vs. average and peak RAM memory.

To automatically generate all 5 high-resolution PNG charts from the latest test run, execute:

```bash
python scripts/generate_charts.py
```

The resulting charts are saved in `results/charts/` ready to be embedded into the final report.
