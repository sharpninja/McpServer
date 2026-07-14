using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-AIUNIT-002: Verifies EF navigation collections remain materializable after CA2227 remediation.
/// </summary>
public sealed class EfNavigationCollectionContractTests : IDisposable
{
    private const string WorkspacePath = @"F:\tests\mcpserver-w15";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Creates an isolated relational database for navigation materialization checks.</summary>
    public EfNavigationCollectionContractTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: Ensures EF can populate each remediated navigation collection.
    /// </summary>
    [Fact]
    public async Task NavigationCollections_ArePopulatedAfterRelationalMaterialization()
    {
        using (var seed = CreateContext())
        {
            var document = new ContextDocumentEntity
            {
                Id = "doc-1",
                WorkspaceId = WorkspacePath,
                SourceKey = "doc.md",
                SourceType = "repo",
                ContentHash = "doc-hash",
            };
            document.Chunks.Add(new ContextChunkEntity
            {
                Id = "chunk-1",
                WorkspaceId = WorkspacePath,
                DocumentId = document.Id,
                Content = "chunk text",
                TokenCount = 2,
                ChunkIndex = 0,
            });

            var source = new GraphEntityEntity
            {
                Id = "entity-source",
                WorkspaceId = WorkspacePath,
                Name = "Source",
                EntityType = "concept",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            var target = new GraphEntityEntity
            {
                Id = "entity-target",
                WorkspaceId = WorkspacePath,
                Name = "Target",
                EntityType = "concept",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            var relationship = new GraphRelationshipEntity
            {
                Id = "rel-1",
                WorkspaceId = WorkspacePath,
                SourceEntityId = source.Id,
                TargetEntityId = target.Id,
                RelationshipType = "references",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            var session = new SessionLogEntity
            {
                WorkspaceId = WorkspacePath,
                SourceType = "Codex",
                SessionId = "session-1",
            };
            var turn = new SessionLogTurnEntity
            {
                WorkspaceId = WorkspacePath,
                RequestId = "req-1",
                QueryText = "prove navigations",
                Status = "completed",
            };
            turn.Actions.Add(new SessionLogActionEntity
            {
                WorkspaceId = WorkspacePath,
                Order = 1,
                Type = "test",
                Status = "completed",
            });
            turn.Tags.Add(new SessionLogTurnTagEntity { WorkspaceId = WorkspacePath, Tag = "w15" });
            turn.ContextItems.Add(new SessionLogTurnContextEntity
            {
                WorkspaceId = WorkspacePath,
                Ordinal = 0,
                ContextItem = "src/file.cs",
            });
            turn.ProcessingDialog.Add(new SessionLogProcessingDialogEntity
            {
                WorkspaceId = WorkspacePath,
                Ordinal = 0,
                Timestamp = DateTimeOffset.UtcNow,
                Role = "model",
                Content = "reasoning",
            });
            turn.Commits.Add(new SessionLogCommitEntity
            {
                WorkspaceId = WorkspacePath,
                Ordinal = 0,
                Sha = "abc123",
                Branch = "main",
                Message = "test commit",
            });
            turn.StringListItems.Add(new SessionLogTurnStringListEntity
            {
                WorkspaceId = WorkspacePath,
                ListType = "DesignDecision",
                Ordinal = 0,
                Value = "Use getter-only EF navigation collections.",
            });
            session.Turns.Add(turn);

            var tool = new ToolDefinitionEntity
            {
                WorkspaceId = WorkspacePath,
                Name = "sample_tool",
                Description = "Sample tool",
                DateTimeCreated = DateTimeOffset.UtcNow,
                DateTimeModified = DateTimeOffset.UtcNow,
            };
            tool.Tags.Add(new ToolDefinitionTagEntity { WorkspaceId = WorkspacePath, Tag = "sample" });

            seed.Documents.Add(document);
            seed.GraphEntities.AddRange(source, target);
            seed.GraphRelationships.Add(relationship);
            seed.SessionLogs.Add(session);
            seed.ToolDefinitions.Add(tool);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        using var read = CreateContext();
        var loadedDocument = await read.Documents
            .Include(entity => entity.Chunks)
            .SingleAsync(entity => entity.Id == "doc-1", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var loadedSource = await read.GraphEntities
            .Include(entity => entity.SourceRelationships)
            .SingleAsync(entity => entity.Id == "entity-source", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var loadedTarget = await read.GraphEntities
            .Include(entity => entity.TargetRelationships)
            .SingleAsync(entity => entity.Id == "entity-target", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var loadedSession = await read.SessionLogs
            .Include(entity => entity.Turns).ThenInclude(turn => turn.Actions)
            .Include(entity => entity.Turns).ThenInclude(turn => turn.Tags)
            .Include(entity => entity.Turns).ThenInclude(turn => turn.ContextItems)
            .Include(entity => entity.Turns).ThenInclude(turn => turn.ProcessingDialog)
            .Include(entity => entity.Turns).ThenInclude(turn => turn.Commits)
            .Include(entity => entity.Turns).ThenInclude(turn => turn.StringListItems)
            .SingleAsync(entity => entity.SessionId == "session-1", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var loadedTool = await read.ToolDefinitions
            .Include(entity => entity.Tags)
            .SingleAsync(entity => entity.Name == "sample_tool", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Single(loadedDocument.Chunks);
        Assert.Single(loadedSource.SourceRelationships);
        Assert.Single(loadedTarget.TargetRelationships);
        var loadedTurn = Assert.Single(loadedSession.Turns);
        Assert.Single(loadedTurn.Actions);
        Assert.Single(loadedTurn.Tags);
        Assert.Single(loadedTurn.ContextItems);
        Assert.Single(loadedTurn.ProcessingDialog);
        Assert.Single(loadedTurn.Commits);
        Assert.Single(loadedTurn.StringListItems);
        Assert.Single(loadedTool.Tags);
    }

    /// <summary>Disposes the in-memory SQLite connection.</summary>
    public void Dispose() => _connection.Dispose();

    private McpDbContext CreateContext()
    {
        var workspace = new WorkspaceContext { WorkspacePath = WorkspacePath };
        return new McpDbContext(_options, workspace);
    }
}
