using McpServer.Support.Mcp.Identity;
using Microsoft.Extensions.Configuration;

namespace McpServer.Support.Mcp.Tests.Identity;

public sealed class IdentityServerExtensionsTests
{
    [Fact]
    public void ResolveIdentityConnectionString_UsesExplicitIdentityConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:Database:Provider"] = "sqlserver",
                ["Mcp:Database:SqlServer:ConnectionString"] = "Server=mcp;Database=McpServer;",
            })
            .Build();
        var options = new IdentityServerOptions
        {
            ConnectionString = "Server=identity;Database=McpIdentity;",
        };

        var resolved = IdentityServerExtensions.ResolveIdentityConnectionString(configuration, options);

        Assert.Equal("Server=identity;Database=McpIdentity;", resolved);
    }

    [Fact]
    public void ResolveIdentityConnectionString_UsesMcpSqlServerConnectionWhenIdentityIsBlank()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:Database:Provider"] = "sqlserver",
                ["Mcp:Database:SqlServer:ConnectionString"] = "Server=mcp;Database=McpServer;",
            })
            .Build();
        var options = new IdentityServerOptions
        {
            ConnectionString = "",
            DatabaseName = "McpIdentityLive",
        };

        var resolved = IdentityServerExtensions.ResolveIdentityConnectionString(configuration, options);

        Assert.Equal("Server=mcp;Database=McpServer;", resolved);
    }

    [Fact]
    public void ResolveIdentityConnectionString_FallsBackToLocalDbWhenMcpSqlServerIsUnavailable()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:Database:Provider"] = "sqlite",
            })
            .Build();
        var options = new IdentityServerOptions
        {
            ConnectionString = "",
            DatabaseName = "McpIdentityFallback",
        };

        var resolved = IdentityServerExtensions.ResolveIdentityConnectionString(configuration, options);

        Assert.Contains(@"Server=(localdb)\MSSQLLocalDB", resolved, StringComparison.Ordinal);
        Assert.Contains("Database=McpIdentityFallback", resolved, StringComparison.Ordinal);
    }
}
