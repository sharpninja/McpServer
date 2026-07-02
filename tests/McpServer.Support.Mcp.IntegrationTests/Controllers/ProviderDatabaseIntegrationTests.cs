using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-101, TEST-MCP-102: Verifies that the provider factory can apply migrations and persist data on
/// clean SQLite and SQL Server LocalDB databases.
/// These tests exercise the actual runtime DbContext wiring without editing production code.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ProviderDatabaseIntegrationTests
{
    /// <summary>
    /// Verifies that the SQLite provider can migrate and persist data against a clean on-disk database file.
    /// </summary>
    [Fact]
    public async Task Sqlite_CleanDatabase_AppliesMigrationsAndPersistsEntity()
    {
        await using var workspace = ProviderIntegrationTestSupport.CreateWorkspace();
        var databasePath = workspace.GetDatabasePath("sqlite-provider-clean.db");
        var factory = ProviderIntegrationTestSupport.CreateFactory(
            workspace,
            new Dictionary<string, string?>
            {
                ["Mcp:DatabaseProvider"] = "sqlite",
                ["Mcp:DataSource"] = databasePath,
            });

        try
        {
            using var client = factory.CreateClient();
            _ = client;

            await ProviderIntegrationTestSupport.AssertDatabaseRoundTripAsync(factory, "Sqlite", string.Empty).ConfigureAwait(true);

            Assert.True(File.Exists(databasePath), "The SQLite provider should materialize a clean on-disk database file.");
        }
        finally
        {
            factory.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the SQL Server provider can create, migrate, persist, and tear down a dedicated LocalDB
    /// instance created by the test harness.
    /// </summary>
    [Fact]
    public async Task SqlServer_LocalDb_CleanDatabase_AppliesMigrationsAndPersistsEntity()
    {
        await using var workspace = ProviderIntegrationTestSupport.CreateWorkspace();
        await using var localDb = await SqlLocalDbSandbox.CreateAsync().ConfigureAwait(true);
        var databaseName = $"mcp_provider_{Guid.NewGuid():N}";
        var factory = ProviderIntegrationTestSupport.CreateFactory(
            workspace,
            new Dictionary<string, string?>
            {
                ["Mcp:DatabaseProvider"] = "sqlserver",
                ["Mcp:SqlServerConnectionString"] = $"{localDb.ConnectionString}Database={databaseName};",
            });

        try
        {
            using var client = factory.CreateClient();
            _ = client;

            await ProviderIntegrationTestSupport.AssertDatabaseRoundTripAsync(factory, "SqlServer", string.Empty).ConfigureAwait(true);
        }
        finally
        {
            factory.Dispose();
        }
    }

}
