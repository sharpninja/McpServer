using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// Triage-report-0009bcac98de435dbae803806f846c11 coverage: the SQL Server provider strategy must
/// configure SqlClient connection resiliency (ConnectRetryCount/ConnectRetryInterval) so transient
/// connection failures (provider error 19 followed by error 40 while SQL Server stays up) recover
/// instead of failing writes for minutes. Uses a DbContextOptionsBuilder and inspects the
/// configured relational connection string.
/// </summary>
public sealed class SqlServerProviderResiliencyTests
{
    /// <summary>Connect retry settings are added when the operator connection string omits them.</summary>
    [Fact]
    public void Configure_DefaultConnectionString_AddsConnectRetrySettings()
    {
        var connectionString = ConfigureAndGetConnectionString(
            "Server=localhost;Database=mcp;Integrated Security=true;TrustServerCertificate=true");

        var parsed = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        Assert.Equal(6, parsed.ConnectRetryCount);
        Assert.Equal(10, parsed.ConnectRetryInterval);
    }

    /// <summary>Operator-specified retry settings are preserved.</summary>
    [Fact]
    public void Configure_ExplicitRetrySettings_ArePreserved()
    {
        var connectionString = ConfigureAndGetConnectionString(
            "Server=localhost;Database=mcp;Integrated Security=true;ConnectRetryCount=2;ConnectRetryInterval=3");

        var parsed = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        Assert.Equal(2, parsed.ConnectRetryCount);
        Assert.Equal(3, parsed.ConnectRetryInterval);
    }

    private static string ConfigureAndGetConnectionString(string input)
    {
        var strategy = new SqlServerMcpDatabaseProviderStrategy();
        var optionsBuilder = new DbContextOptionsBuilder<McpDbContext>();
        strategy.Configure(optionsBuilder, new McpDatabaseProviderOptions(
            strategy.Kind,
            strategy.CanonicalName,
            input,
            strategy.DefaultMigrationsAssembly));

        var relational = optionsBuilder.Options.Extensions.OfType<RelationalOptionsExtension>().Single();
        Assert.NotNull(relational.ConnectionString);
        return relational.ConnectionString!;
    }
}
