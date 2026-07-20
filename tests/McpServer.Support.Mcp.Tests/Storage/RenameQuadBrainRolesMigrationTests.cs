using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// FR-MCP-129 and FR-MCP-134: the QuadBrain role rename migration converts existing
/// LeftHemisphere/RightHemisphere brain-slot rows to Creativity/Logic, updating the role
/// value, the trusted-party id, and the role-derived slot id, and leaves the other two
/// roles untouched.
/// </summary>
public sealed class RenameQuadBrainRolesMigrationTests : IDisposable
{
    private const string PreviousMigration = "20260628194717_RepairTriageCreatedTodoWorkspace";
    private const string PrecedingMigration = "20260702193911_Decompose4nfAgentModelLists";
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Creates an isolated SQLite database using the real provider migration assembly.</summary>
    public RenameQuadBrainRolesMigrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly("McpServer.Storage.SqliteMigrations"))
            .Options;
    }

    /// <summary>
    /// FR-MCP-129 and FR-MCP-134: applying the rename migration over legacy rows renames
    /// LeftHemisphere -> Creativity and RightHemisphere -> Logic (role, party id, slot id),
    /// leaves CuriosityEngine/ArbiterOfTruth unchanged, and leaves no legacy role values.
    /// </summary>
    [Fact]
    public void Migrate_RenameQuadBrainRoles_RenamesLegacyHemisphereRows()
    {
        var now = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PreviousMigration);
            SeedSlot(db, "brain-slot-left-hemisphere-claude-code-opus-4-8", "LeftHemisphere", "brain-slot:left-hemisphere", now);
            SeedSlot(db, "brain-slot-right-hemisphere-codex-cli-gpt-5-5", "RightHemisphere", "brain-slot:right-hemisphere", now);
            SeedSlot(db, "brain-slot-curiosity-engine-claude-code-opus-4-8", "CuriosityEngine", "brain-slot:curiosity-engine", now);
            SeedSlot(db, "brain-slot-arbiter-of-truth-grok-build", "ArbiterOfTruth", "brain-slot:arbiter-of-truth", now);
        }

        using (var db = CreateContext())
        {
            db.Database.Migrate();

            var creativity = db.BrainSlotDefinitions.IgnoreQueryFilters()
                .Single(s => s.SlotId == "brain-slot-creativity-claude-code-opus-4-8");
            Assert.Equal("Creativity", creativity.Role);
            Assert.Equal("brain-slot:creativity", creativity.PartyId);

            var logic = db.BrainSlotDefinitions.IgnoreQueryFilters()
                .Single(s => s.SlotId == "brain-slot-logic-codex-cli-gpt-5-5");
            Assert.Equal("Logic", logic.Role);
            Assert.Equal("brain-slot:logic", logic.PartyId);

            var curiosity = db.BrainSlotDefinitions.IgnoreQueryFilters()
                .Single(s => s.SlotId == "brain-slot-curiosity-engine-claude-code-opus-4-8");
            Assert.Equal("CuriosityEngine", curiosity.Role);
            Assert.Equal("brain-slot:curiosity-engine", curiosity.PartyId);

            var arbiter = db.BrainSlotDefinitions.IgnoreQueryFilters()
                .Single(s => s.SlotId == "brain-slot-arbiter-of-truth-grok-build");
            Assert.Equal("ArbiterOfTruth", arbiter.Role);
            Assert.Equal("brain-slot:arbiter-of-truth", arbiter.PartyId);

            Assert.Empty(db.BrainSlotDefinitions.IgnoreQueryFilters()
                .Where(s => s.Role == "LeftHemisphere" || s.Role == "RightHemisphere"));
        }
    }

    /// <summary>
    /// FR-MCP-129 and FR-MCP-134: migrating back down from head to
    /// <c>20260702193911_Decompose4nfAgentModelLists</c> reverses the rename, restoring
    /// LeftHemisphere/RightHemisphere role values, the legacy <c>brain-slot:left-hemisphere</c> and
    /// <c>brain-slot:right-hemisphere</c> party ids, and the role-derived slot ids, while leaving
    /// CuriosityEngine/ArbiterOfTruth rows untouched and no Creativity/Logic rows behind.
    /// </summary>
    [Fact]
    public void MigrateDown_RenameQuadBrainRoles_RestoresLegacyHemisphereRows()
    {
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
            var left = db.BrainSlotDefinitions.IgnoreQueryFilters()
                .Single(s => s.SlotId == "brain-slot-left-hemisphere-claude-code-opus-4-8");
            Assert.Equal("LeftHemisphere", left.Role);
            Assert.Equal("brain-slot:left-hemisphere", left.PartyId);

            var right = db.BrainSlotDefinitions.IgnoreQueryFilters()
                .Single(s => s.SlotId == "brain-slot-right-hemisphere-codex-cli-gpt-5-5");
            Assert.Equal("RightHemisphere", right.Role);
            Assert.Equal("brain-slot:right-hemisphere", right.PartyId);

            var curiosity = db.BrainSlotDefinitions.IgnoreQueryFilters()
                .Single(s => s.SlotId == "brain-slot-curiosity-engine-claude-code-opus-4-8");
            Assert.Equal("CuriosityEngine", curiosity.Role);
            Assert.Equal("brain-slot:curiosity-engine", curiosity.PartyId);

            var arbiter = db.BrainSlotDefinitions.IgnoreQueryFilters()
                .Single(s => s.SlotId == "brain-slot-arbiter-of-truth-grok-build");
            Assert.Equal("ArbiterOfTruth", arbiter.Role);
            Assert.Equal("brain-slot:arbiter-of-truth", arbiter.PartyId);

            Assert.Empty(db.BrainSlotDefinitions.IgnoreQueryFilters()
                .Where(s => s.Role == "Creativity" || s.Role == "Logic"));
        }
    }

    /// <summary>Releases the in-memory database connection.</summary>
    public void Dispose() => _connection.Dispose();

    private McpDbContext CreateContext() => new(_options);

    // The pre-StoreDateTimeOffsetAsUtcDateTime schema types the audit columns as datetimeoffset,
    // the head schema types them as datetime2, so each seed site supplies the matching CLR type.
    private static void SeedSlot(McpDbContext db, string slotId, string role, string partyId, DateTimeOffset now)
        => SeedSlotCore(db, slotId, role, partyId, now);

    private static void SeedSlot(McpDbContext db, string slotId, string role, string partyId, DateTime now)
        => SeedSlotCore(db, slotId, role, partyId, now);

    private static void SeedSlotCore(McpDbContext db, string slotId, string role, string partyId, object now)
    {
        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "BrainSlotDefinitions" (
                "WorkspaceId", "SlotId", "Role", "ProviderKind", "ModelId", "CredentialReference", "PartyId",
                "Enabled", "TimeoutSeconds", "MaxOutputTokens", "OrchestrationWeight", "WeightVersion",
                "CreatedAtUtc", "UpdatedAtUtc"
            )
            VALUES ('', {0}, {1}, 'OpenAICompatible', 'test-model', 'env:TEST_API_KEY', {2}, 1, 180, 4096, 1.0, 0, {3}, {3});
            """,
            slotId,
            role,
            partyId,
            now);
    }
}
