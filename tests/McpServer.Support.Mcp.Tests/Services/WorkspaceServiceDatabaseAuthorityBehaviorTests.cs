using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-138: Behavioral DB-FK-001 tests for database-authoritative workspace
/// CRUD and appsettings projection timing.
/// </summary>
public sealed class WorkspaceServiceDatabaseAuthorityBehaviorTests
{
    /// <summary>
    /// TEST-MCP-138: Workspace creation commits the canonical row before the
    /// appsettings projection writer is invoked.
    /// </summary>
    [Fact]
    public async Task WorkspaceService_Create_CommitsWorkspaceBeforeProjection()
    {
        var databaseName = $"workspace-authority-{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);
        await using var db = new McpDbContext(options);
        var projectionWriter = new CommitObservingProjectionWriter(options);
        var sut = CreateSut(db, projectionWriter);
        var workspacePath = Path.Combine(Path.GetTempPath(), $"dbfk-create-{Guid.NewGuid():N}");

        var result = await sut.CreateAsync(new WorkspaceCreateRequest
        {
            WorkspacePath = workspacePath,
            Name = "dbfk-create",
        }).ConfigureAwait(true);

        Assert.True(result.Success, result.Error);
        Assert.True(projectionWriter.SawCommittedWorkspace);
        Assert.Single(projectionWriter.LastProjection);
        Assert.Equal(workspacePath, projectionWriter.LastProjection[0].WorkspacePath);
    }

    /// <summary>
    /// TEST-MCP-138 / TEST-MCP-139: Workspace deletion is a soft delete in the
    /// database and deleted rows are omitted from normal reads and projection.
    /// </summary>
    [Fact]
    public async Task WorkspaceService_Delete_SoftDeletesWorkspaceAndProjectionOmitsDeleted()
    {
        var options = CreateOptions($"workspace-delete-{Guid.NewGuid():N}");
        await using var db = new McpDbContext(options);
        var projectionWriter = new CommitObservingProjectionWriter(options);
        var sut = CreateSut(db, projectionWriter);
        var workspacePath = Path.Combine(Path.GetTempPath(), $"dbfk-delete-{Guid.NewGuid():N}");
        var created = await sut.CreateAsync(new WorkspaceCreateRequest
        {
            WorkspacePath = workspacePath,
            Name = "dbfk-delete",
        }).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        var deleted = await sut.DeleteAsync(workspacePath).ConfigureAwait(true);

        Assert.True(deleted.Success, deleted.Error);
        var list = await sut.ListAsync().ConfigureAwait(true);
        Assert.DoesNotContain(list.Items, item => item.WorkspacePath == workspacePath);
        Assert.DoesNotContain(projectionWriter.LastProjection, item => item.WorkspacePath == workspacePath);

        var stored = await db.Workspaces
            .IgnoreQueryFilters()
            .SingleAsync(row => row.WorkspacePath == workspacePath)
            .ConfigureAwait(true);
        Assert.True(stored.IsDeleted);
        Assert.NotNull(stored.DeletedAtUtc);
        Assert.Equal(nameof(WorkspaceService), stored.DeletedBy);
    }

    /// <summary>
    /// Strict 4NF (WorkspaceBannedItemEntity): banned policy lists persist as child rows and
    /// round-trip per category in order; a partial update that supplies only one category replaces
    /// that category's rows (orphan deletion) and leaves the other categories intact.
    /// </summary>
    [Fact]
    public async Task WorkspaceService_BannedPolicyLists_RoundTripAndPartialUpdateReplacesOnlyThatCategory()
    {
        var options = CreateOptions($"workspace-banned-{Guid.NewGuid():N}");
        await using var db = new McpDbContext(options);
        var projectionWriter = new CommitObservingProjectionWriter(options);
        var sut = CreateSut(db, projectionWriter);
        var workspacePath = Path.Combine(Path.GetTempPath(), $"dbfk-banned-{Guid.NewGuid():N}");

        var created = await sut.CreateAsync(new WorkspaceCreateRequest
        {
            WorkspacePath = workspacePath,
            Name = "dbfk-banned",
            BannedLicenses = ["GPL-3.0", "AGPL-3.0"],
            BannedCountriesOfOrigin = ["CN", "RU"],
            BannedOrganizations = ["EvilCorp"],
            BannedIndividuals = ["mallory"],
        }).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        var afterCreate = (await sut.ListAsync().ConfigureAwait(true)).Items
            .Single(i => i.WorkspacePath == workspacePath);
        Assert.Equal(["GPL-3.0", "AGPL-3.0"], afterCreate.BannedLicenses);
        Assert.Equal(["CN", "RU"], afterCreate.BannedCountriesOfOrigin);
        Assert.Equal(["EvilCorp"], afterCreate.BannedOrganizations);
        Assert.Equal(["mallory"], afterCreate.BannedIndividuals);

        // Partial update: only BannedLicenses supplied -> replaces that category (the previous
        // License rows are orphaned and removed), leaving the other three categories intact.
        var updated = await sut.UpdateAsync(
            workspacePath,
            new WorkspaceUpdateRequest { BannedLicenses = ["MIT"] }).ConfigureAwait(true);
        Assert.True(updated.Success, updated.Error);

        var afterUpdate = (await sut.ListAsync().ConfigureAwait(true)).Items
            .Single(i => i.WorkspacePath == workspacePath);
        Assert.Equal(["MIT"], afterUpdate.BannedLicenses);
        Assert.Equal(["CN", "RU"], afterUpdate.BannedCountriesOfOrigin);
        Assert.Equal(["EvilCorp"], afterUpdate.BannedOrganizations);
        Assert.Equal(["mallory"], afterUpdate.BannedIndividuals);
    }

    private static DbContextOptions<McpDbContext> CreateOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    private static WorkspaceService CreateSut(McpDbContext db, IWorkspaceProjectionWriter projectionWriter)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:Workspaces"] = "[]",
            })
            .Build();
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(Path.GetTempPath());
        var processRunner = Substitute.For<IProcessRunner>();

        return new WorkspaceService(
            configuration,
            environment,
            processRunner,
            db,
            projectionWriter,
            NullLogger<WorkspaceService>.Instance);
    }

    private sealed class CommitObservingProjectionWriter : IWorkspaceProjectionWriter
    {
        private readonly DbContextOptions<McpDbContext> _options;

        public CommitObservingProjectionWriter(DbContextOptions<McpDbContext> options)
        {
            _options = options;
        }

        public bool SawCommittedWorkspace { get; private set; }

        public IReadOnlyList<WorkspaceConfigEntry> LastProjection { get; private set; } = [];

        public async Task WriteProjectionAsync(IReadOnlyList<WorkspaceConfigEntry> workspaces, CancellationToken ct)
        {
            LastProjection = workspaces.ToArray();
            await using var probe = new McpDbContext(_options);
            foreach (var workspace in workspaces)
            {
                SawCommittedWorkspace |= await probe.Workspaces
                    .AsNoTracking()
                    .AnyAsync(row => row.WorkspacePath == workspace.WorkspacePath, ct)
                    .ConfigureAwait(false);
            }
        }
    }
}
