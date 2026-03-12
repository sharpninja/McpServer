using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace McpServer.SessionLog.Validation;

/// <summary>
/// Shared fixture providing an HttpClient targeting the live MCP Server on port 7147.
/// </summary>
public sealed class SessionLogEndpointFixture : IDisposable
{
    /// <summary>
    /// Defines <c>BaseUrl</c> constant used by validation tests.
    /// </summary>
    public static string BaseUrl { get; } = Environment.GetEnvironmentVariable("MCPSERVER_BASEURL") ?? "http://localhost:7147";
    /// <summary>
    /// Defines <c>SessionLogRoute</c> constant used by validation tests.
    /// </summary>
    public const string SessionLogRoute = "/mcpserver/sessionlog";

    /// <summary>
    /// Gets or sets <c>Client</c> for validation payload/state handling.
    /// </summary>
    public HttpClient Client { get; }
    /// <summary>
    /// Gets or sets <c>ApiKey</c> for validation payload/state handling.
    /// </summary>
    public string? ApiKey { get; }

    /// <summary>
    /// Initializes a new instance of SessionLogEndpointFixture.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    public SessionLogEndpointFixture()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        ApiKey = ResolvePreferredApiKeyAsync(Client).GetAwaiter().GetResult();
        if (!string.IsNullOrWhiteSpace(ApiKey))
            Client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
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

    /// <summary>Generate a unique canonical session ID for test isolation.</summary>
    public static string GenerateSessionId(string sourceType = "AuditTest", string? suffix = null)
        => $"{sourceType}-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}-{SanitizeSlugToken(suffix ?? Guid.NewGuid().ToString("N"))}";

    /// <summary>Generate a unique canonical request ID for dialog tests.</summary>
    public static string GenerateRequestId(string? slug = null)
        => $"req-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}-{SanitizeSlugToken(slug ?? Guid.NewGuid().ToString("N"))}";

    private static string SanitizeSlugToken(string value)
    {
        var token = Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-");
        token = token.Trim('-');
        return string.IsNullOrWhiteSpace(token) ? "test" : token;
    }

    /// <summary>
    /// Releases resources used by validation tests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    public void Dispose() => Client.Dispose();
}

/// <summary>
/// xUnit collection wiring for shared validation fixtures in <c>SessionLogEndpointCollection</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
[CollectionDefinition("SessionLogEndpoint")]
public sealed class SessionLogEndpointCollection : ICollectionFixture<SessionLogEndpointFixture>;
