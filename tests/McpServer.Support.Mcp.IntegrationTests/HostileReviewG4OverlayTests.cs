using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>
/// Overlay G4: three-provider round trip for HostileReviewRequestEntity using shipped
/// <see cref="HostileReviewService"/>. TEST-MCP-HOSTILEREVIEW-001.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Overlay", "G4")]
public sealed class HostileReviewG4OverlayTests
{
    /// <summary>Sqlite, SQL Server, and PostgreSQL persist and reload a queued hostile-review request.</summary>
    [Fact]
    public async Task HostileReviewEntity_RoundTrip_SqlitePgSqlServer()
    {
        await RoundTripSqliteAsync().ConfigureAwait(true);
        await RoundTripSqlServerAsync().ConfigureAwait(true);
        await RoundTripPostgresAsync().ConfigureAwait(true);
    }

    private static async Task RoundTripSqliteAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hr-g4-{Guid.NewGuid():N}.db");
        var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}");
        await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        try
        {
            var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options;
            await AssertRoundTripAsync(options, path).ConfigureAwait(true);
        }
        finally
        {
            await connection.DisposeAsync().ConfigureAwait(true);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static async Task RoundTripSqlServerAsync()
    {
        await using var localDb = await SqlLocalDbSandbox.CreateAsync().ConfigureAwait(true);
        var databaseName = $"mcp_hr_g4_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlServer($"{localDb.ConnectionString}Database={databaseName};Command Timeout=180;")
            .Options;
        await AssertRoundTripAsync(options, $"sqlserver:{databaseName}").ConfigureAwait(true);
    }

    private static async Task RoundTripPostgresAsync()
    {
        await using var postgres = new EphemeralPostgresSandbox();
        var databaseName = $"mcp_hr_g4_{Guid.NewGuid():N}";
        postgres.CreateDatabase(databaseName);
        try
        {
            var options = new DbContextOptionsBuilder<McpDbContext>()
                .UseNpgsql(postgres.GetDatabaseConnectionString(databaseName))
                .Options;
            await AssertRoundTripAsync(options, $"postgres:{databaseName}").ConfigureAwait(true);
        }
        finally
        {
            postgres.DropDatabase(databaseName);
        }
    }

    private static async Task AssertRoundTripAsync(DbContextOptions<McpDbContext> options, string workspace)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = workspace });
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(3));
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        db.TodoItems.Add(new TodoItemEntity
        {
            Id = "MCP-HOSTILEREVIEW-001",
            Title = "MCP-HOSTILEREVIEW-001",
            Section = "overlay",
            Priority = "high",
            WorkspaceId = workspace,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var sut = new HostileReviewService(db, new HostileReviewWorkerOptions());
        var created = await sut.SubmitAsync(new HostileReviewSubmitRequest
        {
            TargetType = "code",
            Mode = "adversarial",
            ScopeStatement = "provider round-trip",
            RequestingAgent = "g4",
            WorkspacePath = workspace,
            Links = [new HostileReviewArtifactLink { ArtifactType = "todo", ArtifactId = "MCP-HOSTILEREVIEW-001" }],
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);
        db.ChangeTracker.Clear();
        var loaded = await db.HostileReviewRequests.SingleAsync(item => item.RequestId == created.RequestId, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("Queued", loaded.Status);
        Assert.Equal("g4", loaded.RequestingAgent);
    }
}
