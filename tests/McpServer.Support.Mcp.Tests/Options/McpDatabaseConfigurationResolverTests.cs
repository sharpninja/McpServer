using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Storage.Database;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Options;

/// <summary>
/// Unit tests for <see cref="McpDatabaseConfigurationResolver"/>.
/// </summary>
public sealed class McpDatabaseConfigurationResolverTests
{
    /// <summary>
    /// Verifies PostgreSQL URI configuration is converted without using the obsolete resolver.
    /// </summary>
    [Fact]
    public void ResolveProviderOptions_PostgreSqlUri_ConvertsToNpgsqlConnectionString()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Mcp:Database:Provider"] = "postgresql",
            ["Mcp:Database:PostgreSql:ConnectionString"] = "postgresql://user:pass@db.example:5433/appdb?sslmode=disable",
        });

        var options = McpDatabaseConfigurationResolver.ResolveProviderOptions(configuration, instanceName: null);

        Assert.Equal(McpDatabaseProviderKind.PostgreSql, options.ProviderKind);
        Assert.Contains("Host=db.example", options.ConnectionString, StringComparison.Ordinal);
        Assert.Contains("Port=5433", options.ConnectionString, StringComparison.Ordinal);
        Assert.Contains("Database=appdb", options.ConnectionString, StringComparison.Ordinal);
        Assert.Contains("Username=user", options.ConnectionString, StringComparison.Ordinal);
        Assert.Contains("Password=pass", options.ConnectionString, StringComparison.Ordinal);
        Assert.Contains("SSL Mode=Disable", options.ConnectionString, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies existing Npgsql key value connection strings pass through unchanged.
    /// </summary>
    [Fact]
    public void ResolveProviderOptions_PostgreSqlKeyValueConnectionString_PreservesConfiguredValue()
    {
        const string connectionString = "Host=db.example;Port=5432;Database=mcp;Username=user;Password=pass";
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Mcp:Database:Provider"] = "postgresql",
            ["Mcp:Database:PostgreSql:ConnectionString"] = connectionString,
        });

        var options = McpDatabaseConfigurationResolver.ResolveProviderOptions(configuration, instanceName: null);

        Assert.Equal(connectionString, options.ConnectionString);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
