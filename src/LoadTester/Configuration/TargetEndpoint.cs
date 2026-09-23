// Target HTTP endpoint configuration.

namespace LoadTester.Configuration;

public sealed class TargetEndpoint
{
    public string Url { get; set; } = "http://localhost:5000/api/identity/users";
    public string Method { get; set; } = "GET";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
    public string ContentType { get; set; } = "application/json";
    public int TimeoutSeconds { get; set; } = 10;
}

