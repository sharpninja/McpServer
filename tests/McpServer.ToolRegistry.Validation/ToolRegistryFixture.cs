using System.Text.Json;
using Xunit;

namespace McpServer.ToolRegistry.Validation;

/// <summary>Shared fixture providing an HttpClient for the live MCP Server.</summary>
public sealed class ToolRegistryFixture : IDisposable
{
    /// <summary>
    /// Defines <c>BaseUrl</c> constant used by validation tests.
    /// </summary>
    public static string BaseUrl { get; } = Environment.GetEnvironmentVariable("MCPSERVER_BASEURL") ?? "http://localhost:7147";
    /// <summary>
    /// Defines <c>ToolRoute</c> constant used by validation tests.
    /// </summary>
    public const string ToolRoute = "/mcpserver/tools";
    /// <summary>
    /// Defines <c>BucketRoute</c> constant used by validation tests.
    /// </summary>
    public const string BucketRoute = "/mcpserver/tools/buckets";

    /// <summary>
    /// Gets or sets <c>Client</c> for validation payload/state handling.
    /// </summary>
    public HttpClient Client { get; }

    /// <summary>
    /// Initializes a new instance of ToolRegistryFixture.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    public ToolRegistryFixture()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var apiKey = ResolvePreferredApiKeyAsync(Client).GetAwaiter().GetResult();
        if (!string.IsNullOrWhiteSpace(apiKey))
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

    /// <summary>Generate a unique tool name for tests.</summary>
    public static string GenerateToolName() =>
        $"audit-tool-{Guid.NewGuid().ToString("N")[..8]}";

    /// <summary>Generate a unique bucket name for tests.</summary>
    public static string GenerateBucketName() =>
        $"audit-bucket-{Guid.NewGuid().ToString("N")[..8]}";

    /// <summary>
    /// Releases resources used by validation tests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    public void Dispose() => Client.Dispose();
}

/// <summary>
/// xUnit collection wiring for shared validation fixtures in <c>ToolRegistryCollection</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
/// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
/// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
/// </remarks>
[CollectionDefinition("ToolRegistry")]
public sealed class ToolRegistryCollection : ICollectionFixture<ToolRegistryFixture>;
