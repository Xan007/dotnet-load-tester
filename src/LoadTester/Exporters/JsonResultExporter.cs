// Exports load test session results to indented JSON.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoadTester.Exporters;

public sealed class JsonResultExporter : IResultExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public async Task<string> ExportAsync(TestSessionReport report, string outputDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);
        string filename = $"loadtest-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        string filePath = Path.Combine(outputDirectory, filename);

        using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, report, JsonOptions, cancellationToken);

        return Path.GetFullPath(filePath);
    }
}

