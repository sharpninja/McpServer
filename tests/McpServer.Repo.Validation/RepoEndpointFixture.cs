using Xunit;

namespace McpServer.Repo.Validation;

/// <summary>
/// xUnit collection wiring for shared validation fixtures in <c>RepoEndpointCollection</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
/// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
/// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
/// </remarks>
[CollectionDefinition("RepoEndpoint")]
public sealed class RepoEndpointCollection : ICollectionFixture<RepoEndpointFixture> { }

/// <summary>
/// Shared validation fixture for <c>RepoEndpointFixture</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
/// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
/// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
/// </remarks>
public sealed class RepoEndpointFixture : IDisposable
{
    /// <summary>
    /// Defines <c>BaseUrl</c> constant used by validation tests.
    /// </summary>
    public const string BaseUrl = "http://localhost:7147";
    /// <summary>
    /// Defines <c>RepoRoute</c> constant used by validation tests.
    /// </summary>
    public const string RepoRoute = "/mcpserver/repo";
    /// <summary>
    /// Gets or sets <c>Client</c> for validation payload/state handling.
    /// </summary>
    public HttpClient Client { get; }

    /// <summary>
    /// Initializes a new instance of RepoEndpointFixture.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
    /// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
    /// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
    /// </remarks>
    public RepoEndpointFixture()
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
    /// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
    /// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
    /// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
    /// </remarks>
    public void Dispose() => Client.Dispose();
}
