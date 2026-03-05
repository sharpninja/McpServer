using Xunit;

namespace McpServer.ToolRegistry.Validation;

/// <summary>Shared fixture providing an HttpClient for the live MCP Server.</summary>
public sealed class ToolRegistryFixture : IDisposable
{
    public const string BaseUrl = "http://localhost:7147";
    public const string ToolRoute = "/mcpserver/tools";
    public const string BucketRoute = "/mcpserver/tools/buckets";

    public HttpClient Client { get; }

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

    public void Dispose() => Client.Dispose();
}

[CollectionDefinition("ToolRegistry")]
public sealed class ToolRegistryCollection : ICollectionFixture<ToolRegistryFixture>;
