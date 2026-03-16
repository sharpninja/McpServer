using System.Text.Json;
using Xunit;

namespace McpServer.Context.Validation;

/// <summary>
/// xUnit collection wiring for shared validation fixtures in <c>ContextEndpointCollection</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
/// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
/// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
/// </remarks>
[CollectionDefinition("ContextEndpoint")]
public sealed class ContextEndpointCollection : ICollectionFixture<ContextEndpointFixture> { }

/// <summary>
/// Shared validation fixture for <c>ContextEndpointFixture</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
/// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
/// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
/// </remarks>
public sealed class ContextEndpointFixture : IDisposable
{
    /// <summary>
    /// Defines <c>BaseUrl</c> constant used by validation tests.
    /// </summary>
    public static string BaseUrl { get; } = Environment.GetEnvironmentVariable("MCPSERVER_BASEURL") ?? "http://localhost:7147";
    /// <summary>
    /// Defines <c>ContextRoute</c> constant used by validation tests.
    /// </summary>
    public const string ContextRoute = "/mcpserver/context";
    /// <summary>
    /// Gets or sets <c>Client</c> for validation payload/state handling.
    /// </summary>
    public HttpClient Client { get; }

    /// <summary>
    /// Initializes a new instance of ContextEndpointFixture.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    public ContextEndpointFixture()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var apiKey = ResolvePreferredApiKeyAsync(Client).GetAwaiter().GetResult();
        if (!string.IsNullOrEmpty(apiKey))
            Client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
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

    /// <summary>
    /// Releases resources used by validation tests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    public void Dispose() => Client.Dispose();
}
