using McpServer.Support.Mcp.Web;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-017 / TR-MCP-MEMORY-UI-002:
/// Deep-link helper fail-closes unknown assets and accepts MEMORY-* ids.
/// </summary>
public sealed class MemoryUiEndpointTests
{
    /// <summary>Hashed bundles and the unknown-asset probe are not deep links.</summary>
    [Fact]
    public void UnknownAsset_NotServedAsIndex()
    {
        Assert.True(MemoryUiEndpoints.IsUnknownAsset("unknown-asset"));
        Assert.True(MemoryUiEndpoints.IsUnknownAsset("app.abc123.js"));
        Assert.True(MemoryUiEndpoints.IsUnknownAsset("missing.css"));
        Assert.False(MemoryUiEndpoints.IsUnknownAsset("MEMORY-FACT-001"));
    }

    /// <summary>Only MEMORY-* path segments are treated as deep-link ids.</summary>
    [Fact]
    public void DeepLink_AcceptsMemoryIdsOnly()
    {
        Assert.True(MemoryUiEndpoints.IsMemoryId("MEMORY-FACT-001"));
        Assert.False(MemoryUiEndpoints.IsMemoryId("unknown-asset"));
        Assert.False(MemoryUiEndpoints.IsMemoryId("app.js"));
    }
}
