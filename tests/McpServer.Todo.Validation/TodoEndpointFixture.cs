using Xunit;
using System.Text.Json;
using System.Threading;

namespace McpServer.Todo.Validation;

/// <summary>
/// Shared fixture that provides an HttpClient configured to hit the live MCP Server
/// on port 7147, plus helper methods for generating unique test TODO IDs.
/// </summary>
public sealed class TodoEndpointFixture : IDisposable
{
    private static int s_idCounter = Random.Shared.Next(0, 1000);

    /// <summary>Base URL of the running MCP Server.</summary>
    public static string BaseUrl { get; } = Environment.GetEnvironmentVariable("MCPSERVER_BASEURL") ?? "http://localhost:7147";

    /// <summary>Route prefix for TODO endpoints.</summary>
    public const string TodoRoute = "/mcpserver/todo";

    /// <summary>Pre-configured HTTP client targeting the live service.</summary>
    public HttpClient Client { get; }

    /// <summary>
    /// Initializes a new instance of TodoEndpointFixture.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    public TodoEndpointFixture()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var apiKey = GetDefaultApiKeyAsync(Client).GetAwaiter().GetResult();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            Client.DefaultRequestHeaders.Remove("X-Api-Key");
            Client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }
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

    /// <summary>Generate a unique test TODO item ID that won't collide with real data.</summary>
    public static string GenerateTestId()
    {
        var area = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var sequence = Interlocked.Increment(ref s_idCounter) % 1000;
        return $"AUDIT-{area}-{sequence:000}";
    }

    /// <summary>Generate a valid TODO ID that is expected not to exist.</summary>
    public static string GenerateMissingId()
    {
        var area = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"MISSING-{area}-{Random.Shared.Next(0, 1000):000}";
    }

    /// <summary>
    /// Releases resources used by validation tests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    public void Dispose() => Client.Dispose();
}

/// <summary>xUnit collection definition so all todo tests share the same fixture.</summary>
[CollectionDefinition("TodoEndpoint")]
public sealed class TodoEndpointCollection : ICollectionFixture<TodoEndpointFixture>;
