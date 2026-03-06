using Xunit;

namespace McpServer.GitHub.Validation;

/// <summary>
/// xUnit collection wiring for shared validation fixtures in <c>GitHubEndpointCollection</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
/// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
/// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
/// </remarks>
[CollectionDefinition("GitHubEndpoint")]
public sealed class GitHubEndpointCollection : ICollectionFixture<GitHubEndpointFixture> { }

/// <summary>
/// Shared validation fixture for <c>GitHubEndpointFixture</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
/// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
/// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
/// </remarks>
public sealed class GitHubEndpointFixture : IDisposable
{
    /// <summary>
    /// Defines <c>BaseUrl</c> constant used by validation tests.
    /// </summary>
    public const string BaseUrl = "http://localhost:7147";
    /// <summary>
    /// Defines <c>GhRoute</c> constant used by validation tests.
    /// </summary>
    public const string GhRoute = "/mcpserver/gh";
    /// <summary>
    /// Gets or sets <c>Client</c> for validation payload/state handling.
    /// </summary>
    public HttpClient Client { get; }

    /// <summary>
    /// Initializes a new instance of GitHubEndpointFixture.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    public GitHubEndpointFixture()
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
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    public void Dispose() => Client.Dispose();
}
