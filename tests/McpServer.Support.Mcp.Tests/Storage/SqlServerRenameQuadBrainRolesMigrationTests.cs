using McpServer.Support.Mcp.Storage;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// SQL Server twin of <see cref="RenameQuadBrainRolesMigrationTests"/> running against an ad-hoc
/// LocalDB database: exercises both directions of
/// <c>20260720170000_RenameQuadBrainRolesToCreativityLogic</c> through the real SqlServer provider
/// migration assembly, proving the role value, the trusted-party key, and the role-derived slot id
/// are rewritten on Up and reconstructed on Down while CuriosityEngine/ArbiterOfTruth rows are left
/// alone. Uses <c>(localdb)\MSSQLLocalDB</c> (override with <c>MCP_TEST_SQLSERVER_CONNECTION</c>);
/// LocalDB is provisioned by the <c>InstallTestDependencies</c> Nuke target, and an unreachable
/// server fails the gate directly rather than skipping. Validates FR-MCP-129 and FR-MCP-134.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SqlServerRenameQuadBrainRolesMigrationTests : IDisposable
{
    private const string PrecedingMigration = "20260702193940_Decompose4nfAgentModelLists";
    private readonly string _serverConnectionString;
    private readonly string _databaseName = $"mcp_quadbrain_{Guid.NewGuid():N}";
    private DbContextOptions<McpDbContext>? _options;

    /// <summary>Resolves the server connection (LocalDB by default).</summary>
    public SqlServerRenameQuadBrainRolesMigrationTests()
    {
        _serverConnectionString = Environment.GetEnvironmentVariable("MCP_TEST_SQLSERVER_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True";
    }

    /// <summary>
    /// FR-MCP-129 and FR-MCP-134: seeds legacy LeftHemisphere/RightHemisphere brain-slot rows at the
    /// migration immediately preceding the rename, migrates to head, and asserts each row landed on
    /// Creativity/Logic with the new party id and slot id, that CuriosityEngine/ArbiterOfTruth are
    /// unchanged, and that no legacy role value survives.
    /// </summary>
    [Fact]
    public void Migrate_RenameQuadBrainRoles_RenamesLegacyHemisphereRows()
    {
        EnsureScratchDatabase();
        var now = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PrecedingMigration);
            SeedSlot(db, "brain-slot-left-hemisphere-claude-code-opus-4-8", "LeftHemisphere", "brain-slot:left-hemisphere", now);
            SeedSlot(db, "brain-slot-right-hemisphere-codex-cli-gpt-5-5", "RightHemisphere", "brain-slot:right-hemisphere", now);
            SeedSlot(db, "brain-slot-curiosity-engine-claude-code-opus-4-8", "CuriosityEngine", "brain-slot:curiosity-engine", now);
            SeedSlot(db, "brain-slot-arbiter-of-truth-grok-build", "ArbiterOfTruth", "brain-slot:arbiter-of-truth", now);
        }

        using (var db = CreateContext())
        {
            db.Database.Migrate();
            AssertRenamedState(db);
        }
    }

    /// <summary>
    /// FR-MCP-129 and FR-MCP-134: seeds renamed Creativity/Logic brain-slot rows at head, migrates
    /// back down to the preceding migration, and asserts the legacy LeftHemisphere/RightHemisphere
    /// role values, party ids, and slot ids are reconstructed while CuriosityEngine/ArbiterOfTruth
    /// are unchanged and no Creativity/Logic row survives.
    /// </summary>
    [Fact]
    public void MigrateDown_RenameQuadBrainRoles_RestoresLegacyHemisphereRows()
    {
        EnsureScratchDatabase();
        var now = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

        using (var db = CreateContext())
        {
            db.Database.Migrate();
            SeedSlot(db, "brain-slot-creativity-claude-code-opus-4-8", "Creativity", "brain-slot:creativity", now);
            SeedSlot(db, "brain-slot-logic-codex-cli-gpt-5-5", "Logic", "brain-slot:logic", now);
            SeedSlot(db, "brain-slot-curiosity-engine-claude-code-opus-4-8", "CuriosityEngine", "brain-slot:curiosity-engine", now);
            SeedSlot(db, "brain-slot-arbiter-of-truth-grok-build", "ArbiterOfTruth", "brain-slot:arbiter-of-truth", now);
        }

        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PrecedingMigration);
        }

        using (var db = CreateContext())
        {
            AssertLegacyState(db);
        }
    }

    /// <summary>Drops the scratch database.</summary>
    public void Dispose()
    {
        if (_options is null)
            return;
        try
        {
            using var admin = new SqlConnection(_serverConnectionString);
            admin.Open();
            using var drop = admin.CreateCommand();
            drop.CommandText =
                $"IF DB_ID('{_databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]; END";
            drop.ExecuteNonQuery();
        }
        catch (SqlException)
        {
            // Best-effort cleanup; scratch databases are uniquely named.
        }
    }

    private void EnsureScratchDatabase()
    {
        try
        {
            using var admin = new SqlConnection(_serverConnectionString);
            admin.Open();
            using var create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE [{_databaseName}];";
            create.ExecuteNonQuery();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "SQL Server is not reachable for the migration gate. Run the 'InstallTestDependencies' " +
                "Nuke target to provision LocalDB, or set MCP_TEST_SQLSERVER_CONNECTION. " +
                $"({ex.Message})",
                ex);
        }

        var builder = new SqlConnectionStringBuilder(_serverConnectionString) { InitialCatalog = _databaseName };
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlServer(builder.ToString(), sql => sql.MigrationsAssembly("McpServer.Storage.SqlServerMigrations"))
            .Options;
    }

    private McpDbContext CreateContext() => new(_options!);

    private static void AssertRenamedState(McpDbContext db)
    {
        var creativity = db.BrainSlotDefinitions.IgnoreQueryFilters()
            .Single(s => s.SlotId == "brain-slot-creativity-claude-code-opus-4-8");
        Assert.Equal("Creativity", creativity.Role);
        Assert.Equal("brain-slot:creativity", creativity.PartyId);

        var logic = db.BrainSlotDefinitions.IgnoreQueryFilters()
            .Single(s => s.SlotId == "brain-slot-logic-codex-cli-gpt-5-5");
        Assert.Equal("Logic", logic.Role);
        Assert.Equal("brain-slot:logic", logic.PartyId);

        AssertUntouchedRoles(db);
        Assert.Empty(db.BrainSlotDefinitions.IgnoreQueryFilters()
            .Where(s => s.Role == "LeftHemisphere" || s.Role == "RightHemisphere"));
    }

    private static void AssertLegacyState(McpDbContext db)
    {
        var left = db.BrainSlotDefinitions.IgnoreQueryFilters()
            .Single(s => s.SlotId == "brain-slot-left-hemisphere-claude-code-opus-4-8");
        Assert.Equal("LeftHemisphere", left.Role);
        Assert.Equal("brain-slot:left-hemisphere", left.PartyId);

        var right = db.BrainSlotDefinitions.IgnoreQueryFilters()
            .Single(s => s.SlotId == "brain-slot-right-hemisphere-codex-cli-gpt-5-5");
        Assert.Equal("RightHemisphere", right.Role);
        Assert.Equal("brain-slot:right-hemisphere", right.PartyId);

        AssertUntouchedRoles(db);
        Assert.Empty(db.BrainSlotDefinitions.IgnoreQueryFilters()
            .Where(s => s.Role == "Creativity" || s.Role == "Logic"));
    }

    private static void AssertUntouchedRoles(McpDbContext db)
    {
        var curiosity = db.BrainSlotDefinitions.IgnoreQueryFilters()
            .Single(s => s.SlotId == "brain-slot-curiosity-engine-claude-code-opus-4-8");
        Assert.Equal("CuriosityEngine", curiosity.Role);
        Assert.Equal("brain-slot:curiosity-engine", curiosity.PartyId);

        var arbiter = db.BrainSlotDefinitions.IgnoreQueryFilters()
            .Single(s => s.SlotId == "brain-slot-arbiter-of-truth-grok-build");
        Assert.Equal("ArbiterOfTruth", arbiter.Role);
        Assert.Equal("brain-slot:arbiter-of-truth", arbiter.PartyId);
    }

    private static void SeedSlot(McpDbContext db, string slotId, string role, string partyId, DateTime now)
    {
        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO [BrainSlotDefinitions] (
                [WorkspaceId], [SlotId], [Role], [ProviderKind], [ModelId], [CredentialReference], [PartyId],
                [Enabled], [TimeoutSeconds], [MaxOutputTokens], [OrchestrationWeight], [WeightVersion],
                [CreatedAtUtc], [UpdatedAtUtc], [IsDeleted]
            )
            VALUES ('', {0}, {1}, 'OpenAICompatible', 'test-model', 'env:TEST_API_KEY', {2}, 1, 180, 4096, 1.0, 0, {3}, {3}, 0);
            """,
            slotId,
            role,
            partyId,
            now);
    }
}
