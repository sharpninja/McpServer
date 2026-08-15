using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.Support.Mcp.UseCases;
using McpServer.Support.Mcp.UseCases.Commands;
using McpServer.Support.Mcp.UseCases.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-007 / TR-MCP-USECASE-006 / TR-MCP-DB-004:
/// Proves Use Case CQRS mutations emit append-only DataAuditLog rows via McpDbContext.
/// </summary>
public sealed class UseCaseAuditEmissionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;
    private readonly string _workspace = Path.Combine(Path.GetTempPath(), "mcp-uc-audit-" + Guid.NewGuid().ToString("N"));
    private readonly CallContext _ctx = new();

    /// <summary>Opens an isolated in-memory schema with soft-delete + audit metadata.</summary>
    public UseCaseAuditEmissionTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
        if (!ctx.Workspaces.IgnoreQueryFilters().Any(w => w.WorkspaceId == _workspace))
        {
            ctx.Workspaces.Add(new WorkspaceEntity
            {
                WorkspaceId = _workspace,
                WorkspacePath = _workspace,
                Name = "UseCase Audit",
            });
            ctx.SaveChanges();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// Create, update, soft-delete, and FR-link mutations write create/update/delete audit rows
    /// for UseCaseEntity and UseCaseFrLinkEntity.
    /// Note: identity keys may be 0 when create audit is captured (pre-insert), so create rows are
    /// asserted by EntityKind + WorkspaceId, not post-identity UseCaseId.
    /// </summary>
    [Fact]
    public async Task UseCaseMutations_EmitDataAuditLogRows()
    {
        long useCaseId;
        await using (var db = CreateContext())
        {
            var create = await new CreateUseCaseCommandHandler(db, CreateWorkspaceContext())
                .HandleAsync(
                    new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest
                    {
                        Title = "Audit create",
                        BriefDescription = "initial",
                    }),
                    _ctx)
                .ConfigureAwait(true);
            Assert.True(create.IsSuccess, create.Error);
            useCaseId = create.Value!.UseCaseId;

            var createRows = await db.DataAuditLogs
                .AsNoTracking()
                .Where(r => r.EntityKind == nameof(UseCaseEntity) && r.Action == "create" && r.WorkspaceId == _workspace)
                .ToListAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.NotEmpty(createRows);
            Assert.Contains(createRows, r => r.CurrentSnapshotJson != null && r.CurrentSnapshotJson.Contains("Audit create", StringComparison.Ordinal));

            var update = await new UpdateUseCaseCommandHandler(db, CreateWorkspaceContext())
                .HandleAsync(
                    new UpdateUseCaseCommand(_workspace, useCaseId, new UpdateUseCaseRequest
                    {
                        Title = "Audit update",
                    }),
                    _ctx)
                .ConfigureAwait(true);
            Assert.True(update.IsSuccess, update.Error);

            var updateRows = await db.DataAuditLogs
                .AsNoTracking()
                .Where(r => r.EntityKind == nameof(UseCaseEntity) && r.Action == "update" && r.WorkspaceId == _workspace)
                .ToListAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.NotEmpty(updateRows);
            Assert.Contains(updateRows, r => r.EntityKey.Contains($"UseCaseId={useCaseId}", StringComparison.Ordinal));

            await SeedFrAsync(db, "FR-MCP-USECASE-AUDIT-001", "Audit FR").ConfigureAwait(true);
            var link = await new LinkUseCaseToFrCommandHandler(db, CreateWorkspaceContext())
                .HandleAsync(
                    new LinkUseCaseToFrCommand(
                        _workspace,
                        useCaseId,
                        "FR-MCP-USECASE-AUDIT-001",
                        LinkType: null,
                        LinkOrder: 1,
                        Notes: null),
                    _ctx)
                .ConfigureAwait(true);
            Assert.True(link.IsSuccess, link.Error);

            var linkCreates = await db.DataAuditLogs
                .AsNoTracking()
                .Where(r => r.EntityKind == nameof(UseCaseFrLinkEntity) && r.Action == "create" && r.WorkspaceId == _workspace)
                .ToListAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.NotEmpty(linkCreates);
            Assert.Contains(linkCreates, r => r.CurrentSnapshotJson != null
                && r.CurrentSnapshotJson.Contains("FR-MCP-USECASE-AUDIT-001", StringComparison.Ordinal));
        }

        await using (var db = CreateContext())
        {
            var delete = await new DeleteUseCaseCommandHandler(db, CreateWorkspaceContext())
                .HandleAsync(new DeleteUseCaseCommand(_workspace, useCaseId), _ctx)
                .ConfigureAwait(true);
            Assert.True(delete.IsSuccess, delete.Error);

            var deleteRows = await db.DataAuditLogs
                .AsNoTracking()
                .Where(r => r.EntityKind == nameof(UseCaseEntity) && r.Action == "delete" && r.WorkspaceId == _workspace)
                .ToListAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.NotEmpty(deleteRows);
            Assert.Contains(deleteRows, r => r.EntityKey.Contains($"UseCaseId={useCaseId}", StringComparison.Ordinal));

            var softDeleted = await db.UseCases
                .IgnoreQueryFilters()
                .SingleAsync(u => u.UseCaseId == useCaseId, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.True((bool)db.Entry(softDeleted).Property("IsDeleted").CurrentValue!);
        }
    }

    private McpDbContext CreateContext() => new(_options, CreateWorkspaceContext());

    private WorkspaceContext CreateWorkspaceContext()
        => new()
        {
            WorkspacePath = _workspace,
            WorkspaceName = "uc-audit",
            DataDirectory = _workspace,
            TodoFilePath = Path.Combine(_workspace, "docs", "todo.yaml"),
            SessionsPath = Path.Combine(_workspace, "docs", "sessions"),
            ExternalDocsPath = Path.Combine(_workspace, "docs", "external"),
        };

    private async Task SeedFrAsync(McpDbContext db, string id, string title)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = _workspace,
            Kind = UseCaseConstants.FrKind,
            Id = id,
            Title = title,
            Body = "body for " + title,
            Priority = "medium",
            Status = "pending",
            ScopeStartLayerKey = "layer-1",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
    }
}
