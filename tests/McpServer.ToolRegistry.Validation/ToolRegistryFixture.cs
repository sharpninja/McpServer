using Xunit;

namespace McpServer.ToolRegistry.Validation;

/// <summary>Shared fixture providing an HttpClient for the live MCP Server.</summary>
public sealed class ToolRegistryFixture : IDisposable
{
    /// <summary>
    /// Defines <c>BaseUrl</c> constant used by validation tests.
    /// </summary>
    public const string BaseUrl = "http://localhost:7147";
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
