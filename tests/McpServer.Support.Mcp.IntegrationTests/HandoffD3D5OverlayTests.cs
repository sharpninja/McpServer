namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>
/// Overlay G3 D4: shipped provider persist gate for AddHandoffIngestionStorage via
/// <see cref="ProviderIntegrationTestSupport.AssertHandoffIngestionStorageAsync"/>.
/// TEST-HANDOFF-007. Current-plus-prior still runs the three-provider classes.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Overlay", "G3")]
public sealed class HandoffD3D5OverlayTests
{
    /// <summary>
    /// D4: SQLite hosted factory applies AddHandoffIngestionStorage and round-trips
    /// HandoffIngestionRuns / HandoffDiagnostics through the shipped helper.
    /// </summary>
    [Fact]
    public async Task D4_Sqlite_HandoffIngestionStorage_RoundTripViaShippedAssert()
    {
        await using var workspace = ProviderIntegrationTestSupport.CreateWorkspace();
        var databasePath = workspace.GetDatabasePath("g3-handoff-d4-sqlite.db");
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
            await ProviderIntegrationTestSupport.AssertHandoffIngestionStorageAsync(factory).ConfigureAwait(true);
            Assert.True(File.Exists(databasePath), "The SQLite provider should materialize the overlay D4 database file.");
        }
        finally
        {
            factory.Dispose();
        }
    }
}
