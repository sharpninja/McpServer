using System.Text;
using System.Text.Json;
using Xunit;

namespace McpServer.Workspace.Validation;

/// <summary>
/// Shared fixture that provides an HttpClient configured to hit the live MCP Server
/// on port 7147, plus helper methods for Base64URL key encoding.
/// </summary>
public sealed class WorkspaceEndpointFixture : IDisposable
{
    /// <summary>Base URL of the running MCP Server.</summary>
    public static string BaseUrl { get; } = Environment.GetEnvironmentVariable("MCPSERVER_BASEURL") ?? "http://localhost:7147";

    /// <summary>Route prefix for workspace endpoints.</summary>
    public const string WorkspaceRoute = "/mcpserver/workspace";

    /// <summary>Pre-configured HTTP client targeting the live service.</summary>
    public HttpClient Client { get; }

    /// <summary>Optional API key. Set via MCPSERVER_APIKEY environment variable.</summary>
    public string? ApiKey { get; }

    /// <summary>Initializes a new instance.</summary>
    public WorkspaceEndpointFixture()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        ApiKey = ResolvePreferredApiKeyAsync(Client).GetAwaiter().GetResult();
        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            Client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        }
    }

    private static async Task<string?> ResolvePreferredApiKeyAsync(HttpClient client)
    {
        var explicitKey = Environment.GetEnvironmentVariable("MCPSERVER_APIKEY");
        if (!string.IsNullOrWhiteSpace(explicitKey))
            return explicitKey;

        var fullKey = TryReadApiKeyFromSessionState() ?? TryReadApiKeyFromMarkerFile();
        if (!string.IsNullOrWhiteSpace(fullKey))
            return fullKey;

        return await GetDefaultApiKeyAsync(client).ConfigureAwait(false);
    }

    private static string? TryReadApiKeyFromSessionState()
    {
        var sessionPath = FindFileUpwards(".mcpServer", "session.yaml");
        if (sessionPath is null)
            return null;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(sessionPath));
            return document.RootElement.TryGetProperty("apiKey", out var apiKeyElement)
                ? apiKeyElement.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadApiKeyFromMarkerFile()
    {
        var markerPath = FindFileUpwards("AGENTS-README-FIRST.yaml");
        if (markerPath is null)
            return null;

        foreach (var line in File.ReadLines(markerPath))
        {
            if (line.StartsWith("apiKey:", StringComparison.OrdinalIgnoreCase))
                return line["apiKey:".Length..].Trim();
        }

        return null;
    }

    private static string? FindFileUpwards(params string[] pathSegments)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine([current.FullName, .. pathSegments]);
            if (File.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        return null;
    }

    private static async Task<string?> GetDefaultApiKeyAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api-key").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(contentStream).ConfigureAwait(false);
        return document.RootElement.TryGetProperty("apiKey", out var apiKeyElement)
            ? apiKeyElement.GetString()
            : null;
    }

    /// <summary>Encode a workspace path to a Base64URL key for use in route segments.</summary>
    public static string EncodeKey(string path)
    {
        var bytes = Encoding.UTF8.GetBytes(path.Trim());
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Generate a unique test workspace path that won't collide with real data.</summary>
    public static string GenerateTestWorkspacePath()
    {
        return $@"C:\Temp\McpAuditTest_{Guid.NewGuid():N}";
    }

    /// <summary>Disposes resources.</summary>
    public void Dispose() => Client.Dispose();
}

/// <summary>xUnit collection definition so all workspace tests share the same fixture.</summary>
[CollectionDefinition("WorkspaceEndpoint")]
public sealed class WorkspaceEndpointCollection : ICollectionFixture<WorkspaceEndpointFixture>;
