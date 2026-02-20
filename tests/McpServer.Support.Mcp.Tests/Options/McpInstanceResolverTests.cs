using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Options;

public sealed class McpInstanceResolverTests
{
    [Fact]
    public void GetRequestedInstanceName_ReadsFromArgs_EqualsSyntax()
    {
        var value = McpInstanceResolver.GetRequestedInstanceName(["--instance=alpha"]);
        Assert.Equal("alpha", value);
    }

    [Fact]
    public void GetRequestedInstanceName_ReadsFromArgs_SeparatedSyntax()
    {
        var value = McpInstanceResolver.GetRequestedInstanceName(["--instance", "beta"]);
        Assert.Equal("beta", value);
    }

    [Fact]
    public void GetEffectiveMcpValue_PrefersInstanceOverride()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:RepoRoot"] = ".",
            ["Mcp:Instances:alt:RepoRoot"] = "temp_test",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        var value = McpInstanceResolver.GetEffectiveMcpValue(configuration, "alt", "RepoRoot");
        Assert.Equal("temp_test", value);
    }

    [Fact]
    public void ValidateInstances_ThrowsOnDuplicatePorts()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:Instances:a:RepoRoot"] = ".",
            ["Mcp:Instances:a:Port"] = "7147",
            ["Mcp:Instances:b:RepoRoot"] = ".",
            ["Mcp:Instances:b:Port"] = "7147",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        Assert.Throws<InvalidOperationException>(() => McpInstanceResolver.ValidateInstances(configuration));
    }

    [Fact]
    public void ValidateInstances_ThrowsWhenRepoRootMissingOnDisk()
    {
        var missingRoot = Path.Combine(Path.GetTempPath(), $"mcp-missing-{Guid.NewGuid():N}");
        var data = new Dictionary<string, string?>
        {
            ["Mcp:Instances:a:RepoRoot"] = missingRoot,
            ["Mcp:Instances:a:Port"] = "7147",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => McpInstanceResolver.ValidateInstances(configuration));
        Assert.Contains("does not exist", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateTodoStorage_ThrowsOnUnsupportedProvider()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:TodoStorage:Provider"] = "memory",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        Assert.Throws<InvalidOperationException>(() => McpInstanceResolver.ValidateTodoStorage(configuration, null));
    }

    [Fact]
    public void ValidateTodoStorage_ThrowsWhenSqliteDataSourceMissing()
    {
        var data = new Dictionary<string, string?>
        {
            ["Mcp:TodoStorage:Provider"] = "sqlite",
            ["Mcp:TodoStorage:SqliteDataSource"] = "",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        Assert.Throws<InvalidOperationException>(() => McpInstanceResolver.ValidateTodoStorage(configuration, null));
    }
}
