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
    public const string BaseUrl = "http://localhost:7147";
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
        var apiKey = Environment.GetEnvironmentVariable("MCPSERVER_APIKEY");
        if (!string.IsNullOrEmpty(apiKey))
            Client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
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
