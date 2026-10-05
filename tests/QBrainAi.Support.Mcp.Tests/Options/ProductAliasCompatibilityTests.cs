using QBrainAi.Support.Mcp;
using QBrainAi.Support.Mcp.Options;
using Microsoft.Extensions.Configuration;

namespace QBrainAi.Support.Mcp.Tests.Options;

/// <summary>
/// TR-MCP-QBRAIN-004 and TR-MCP-QBRAIN-005: product route and configuration aliases.
/// </summary>
public sealed class ProductAliasCompatibilityTests
{
    /// <summary>
    /// TR-MCP-QBRAIN-004: both HTTP prefixes match the same resource paths.
    /// </summary>
    [Fact]
    public void ProductApiPaths_AcceptCanonicalAndLegacyPrefixes()
    {
        Assert.True(ProductApiPaths.IsProductApi("/qbrainai/todo"));
        Assert.True(ProductApiPaths.IsProductApi("/mcpserver/todo"));
        Assert.False(ProductApiPaths.IsProductApi("/health"));
        Assert.True(ProductApiPaths.EqualsProductPath("/qbrainai/todo/", "todo"));
        Assert.True(ProductApiPaths.EqualsProductPath("/mcpserver/memory/recall", "memory/recall"));
        Assert.True(ProductApiPaths.StartsWithProductSuffix("/mcpserver/todo/ITEM-1", "todo"));
        Assert.False(ProductApiPaths.StartsWithProductSuffix("/mcpserver/todoextra", "todo"));
        Assert.True(ProductApiPaths.TryReadResourceId("/mcpserver/memory/MEMORY-A-001", "memory", out var id));
        Assert.Equal("MEMORY-A-001", id);
        Assert.True(ProductApiPaths.TryTakeResourceTail("/qbrainai/todo/PLAN-1?x=1".Split('?')[0], "todo", out var tail));
        Assert.Equal("PLAN-1", tail);
    }

    /// <summary>
    /// TR-MCP-QBRAIN-005: QBrainAi keys are projected onto Mcp, and Mcp-only keys remain.
    /// </summary>
    [Fact]
    public void ProjectCanonicalSection_QBrainAiWinsAndLegacyOnlyKeysRemain()
    {
        using var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mcp:RepoRoot"] = @"E:\github\McpServer",
            ["Mcp:Port"] = "7000",
            ["Mcp:TodoFilePath"] = "docs/Project/TODO.yaml",
            ["QBrainAi:Port"] = "7147",
            ["QBrainAi:Workspaces:0:WorkspacePath"] = @"E:\github\McpServer",
            ["QBrainAi:Workspaces:0:Name"] = "McpServer",
        });

        McpInstanceResolver.ProjectCanonicalSectionOverLegacy(configuration);

        Assert.Equal("7147", configuration["Mcp:Port"]);
        Assert.Equal(@"E:\github\McpServer", configuration["Mcp:RepoRoot"]);
        Assert.Equal("docs/Project/TODO.yaml", configuration["Mcp:TodoFilePath"]);
        Assert.Equal(@"E:\github\McpServer", configuration["Mcp:Workspaces:0:WorkspacePath"]);
        Assert.Equal("McpServer", configuration["Mcp:Workspaces:0:Name"]);
    }

    /// <summary>
    /// TR-MCP-QBRAIN-003: triage product-workspace detection accepts the checkout name and the new product name.
    /// </summary>
    [Theory]
    [InlineData("QBrainAi", @"E:\other", true)]
    [InlineData("McpServer", @"E:\other", true)]
    [InlineData("notes", @"E:\github\McpServer", true)]
    [InlineData("notes", @"E:\github\QBrainAi", true)]
    [InlineData("notes", @"E:\github\Other", false)]
    public void ProductWorkspace_AcceptsCanonicalAndLegacyNames(string name, string path, bool expected)
    {
        Assert.Equal(expected, ProductWorkspaceIdentity.IsProductWorkspace(name, path));
    }
}
