using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// Validates FR-MCP-022 and TR-MCP-TR-003 by applying real EF Core migrations and
/// asserting that the canonical global <c>official</c> tool bucket for
/// <c>sharpninja/McpServerTools</c> is present without creating duplicates.
/// </summary>
public sealed class CanonicalToolBucketMigrationTests : IDisposable
{
    private const string PreviousMigration = "20260303223000_FixPostgresDateTimeTextColumns";
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>
    /// Creates an isolated relational database that executes the actual migration
    /// chain so seeding behavior is validated against the production schema.
    /// </summary>
    public CanonicalToolBucketMigrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    /// <summary>
    /// Verifies that applying the full migration chain from scratch seeds the
    /// canonical global tool bucket with the expected repository metadata and
    /// global workspace scope.
    /// </summary>
    [Fact]
    public void Migrate_FromScratch_SeedsCanonicalOfficialBucket()
    {
        using var db = CreateContext();
        db.Database.Migrate();

        var bucket = db.ToolBuckets.IgnoreQueryFilters().Single(b => b.Name == "official");

        Assert.Equal("sharpninja", bucket.Owner);
        Assert.Equal("McpServerTools", bucket.Repo);
        Assert.Equal("main", bucket.Branch);
        Assert.Equal("/", bucket.ManifestPath);
        Assert.Equal(string.Empty, bucket.WorkspaceId);
    }

    /// <summary>
    /// Verifies that upgrading a database which already contains the canonical
    /// global bucket does not fail and does not create a duplicate row when the
    /// new seed migration is applied.
    /// </summary>
    [Fact]
    public void Migrate_WhenCanonicalOfficialBucketAlreadyExists_RemainsSingleRow()
    {
        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PreviousMigration);
            db.ToolBuckets.Add(new ToolBucketEntity
            {
                Name = "official",
                Owner = "sharpninja",
                Repo = "McpServerTools",
                Branch = "main",
                ManifestPath = "/",
                WorkspaceId = string.Empty,
                DateTimeCreated = DateTimeOffset.UtcNow,
            });
            db.SaveChanges();
        }

        using (var db = CreateContext())
        {
            db.Database.Migrate();
            var buckets = db.ToolBuckets.IgnoreQueryFilters().Where(b => b.Name == "official").ToList();

            Assert.Single(buckets);
            Assert.Equal("sharpninja", buckets[0].Owner);
            Assert.Equal("McpServerTools", buckets[0].Repo);
            Assert.Equal(string.Empty, buckets[0].WorkspaceId);
        }
    }

    /// <summary>
    /// Verifies that upgrading a database containing the obsolete
    /// <c>mcpservertools</c> bucket alias migrates installed tool provenance to
    /// the canonical <c>official</c> bucket and removes the alias row.
    /// </summary>
    [Fact]
    public void Migrate_WhenLegacyAliasBucketExists_NormalizesBucketAndToolProvenance()
    {
        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PreviousMigration);
            db.ToolBuckets.Add(new ToolBucketEntity
            {
                Name = "mcpservertools",
                Owner = "sharpninja",
                Repo = "McpServerTools",
                Branch = "main",
                ManifestPath = "/",
                WorkspaceId = string.Empty,
                DateTimeCreated = DateTimeOffset.UtcNow,
            });
            db.ToolDefinitions.Add(new ToolDefinitionEntity
            {
                Name = "mcp-session-module",
                Description = "Download McpSession",
                BucketName = "mcpservertools",
                WorkspaceId = string.Empty,
                DateTimeCreated = DateTimeOffset.UtcNow,
                DateTimeModified = DateTimeOffset.UtcNow,
            });
            db.SaveChanges();
        }

        using (var db = CreateContext())
        {
            db.Database.Migrate();

            var officialBucket = db.ToolBuckets.IgnoreQueryFilters().Single(b => b.Name == "official");
            Assert.Equal("sharpninja", officialBucket.Owner);
            Assert.Equal("McpServerTools", officialBucket.Repo);
            Assert.Equal(string.Empty, officialBucket.WorkspaceId);

            Assert.Empty(db.ToolBuckets.IgnoreQueryFilters().Where(b => b.Name == "mcpservertools"));

            var tool = db.ToolDefinitions.IgnoreQueryFilters().Single(t => t.Name == "mcp-session-module");
            Assert.Equal("official", tool.BucketName);
        }
    }

    /// <summary>
    /// Verifies that upgrading a database containing tool definitions whose
    /// declared scope is global or workspace-specific but whose persisted
    /// <c>WorkspaceId</c> no longer matches that contract repairs both the tool
    /// rows and their tag rows.
    /// </summary>
    [Fact]
    public void Migrate_WhenToolWorkspaceScopeWasStampedIncorrectly_NormalizesToolsAndTags()
    {
        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PreviousMigration);
            var tool = new ToolDefinitionEntity
            {
                Name = "mcp-session-module",
                Description = "Download McpSession",
                WorkspacePath = null,
                WorkspaceId = @"E:\github\McpServer",
                DateTimeCreated = DateTimeOffset.UtcNow,
                DateTimeModified = DateTimeOffset.UtcNow,
            };
            tool.Tags.Add(new ToolDefinitionTagEntity
            {
                Tag = "session",
                WorkspaceId = @"E:\github\McpServer",
            });

            db.ToolDefinitions.Add(tool);
            db.SaveChanges();
        }

        using (var db = CreateContext())
        {
            db.Database.Migrate();

            var tool = db.ToolDefinitions
                .IgnoreQueryFilters()
                .Include(t => t.Tags)
                .Single(t => t.Name == "mcp-session-module");

            Assert.Equal(string.Empty, tool.WorkspaceId);
            Assert.Null(tool.WorkspacePath);
            Assert.Single(tool.Tags);
            Assert.Equal(string.Empty, tool.Tags.Single().WorkspaceId);
        }
    }

    /// <summary>
    /// Releases the relational test connection after the migration scenarios run
    /// so temporary database resources do not leak across test classes.
    /// </summary>
    public void Dispose()
    {
        _connection.Dispose();
    }

    private McpDbContext CreateContext()
    {
        return new McpDbContext(_options);
    }
}
