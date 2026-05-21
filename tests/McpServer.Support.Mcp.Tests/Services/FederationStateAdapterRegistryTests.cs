using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.FederationAdapters;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for federation state adapter registration and snapshots.</summary>
public sealed class FederationStateAdapterRegistryTests
{
    private const string WorkspacePath = @"F:\GitHub\McpServer";

    /// <summary>All required domains are covered and explicit exemptions are local-only.</summary>
    [Fact]
    public void AddFederationStateAdapters_CoversRequiredDomains()
    {
        using var provider = CreateProvider();
        var registry = provider.GetRequiredService<FederationStateAdapterRegistry>();

        var coverage = registry.GetCoverage();
        Assert.All(FederationStateAdapterRegistry.RequiredDomains, domain =>
            Assert.Contains(coverage, row => row.Domain == domain && row.Covered));

        var localOnlyDomains = coverage
            .Where(row => row.LocalOnly)
            .Select(row => row.Domain)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["context_metadata", "github_metadata", "marker_state", "repo_file_changes"],
            localOnlyDomains);
    }

    /// <summary>Workspace adapter snapshots registration metadata and versions from modification time.</summary>
    [Fact]
    public async Task WorkspaceAdapter_SnapshotsWorkspaceMetadata()
    {
        using var provider = CreateProvider();
        var adapter = ResolveAdapter(provider, "workspace");

        var snapshot = await adapter.SnapshotAsync(WorkspacePath, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("workspace", snapshot.Domain);
        Assert.Equal("1700000000000", snapshot.Version);
        Assert.Contains("McpServer", snapshot.PayloadJson, StringComparison.Ordinal);
    }

    /// <summary>TODO adapter uses the latest audit version and includes item state.</summary>
    [Fact]
    public async Task TodoAdapter_UsesAuditVersionAndSnapshotsItem()
    {
        using var provider = CreateProvider();
        await SeedAsync(provider, db =>
        {
            db.TodoItems.Add(new TodoItemEntity
            {
                Id = "PLAN-FEDERATION-001",
                Title = "Federation plan",
                Section = "Backlog",
                Priority = "high",
            });
            db.TodoAuditHistory.Add(new TodoAuditHistoryEntity
            {
                TodoId = "PLAN-FEDERATION-001",
                Version = 3,
                Action = "updated",
                RecordedAtUtc = "2026-05-21T19:00:00Z",
                Source = "test",
            });
        }).ConfigureAwait(true);

        var adapter = ResolveAdapter(provider, "todo");
        var snapshot = await adapter.SnapshotAsync("PLAN-FEDERATION-001", CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("3", snapshot.Version);
        Assert.Contains("Federation plan", snapshot.PayloadJson, StringComparison.Ordinal);
    }

    /// <summary>Requirements adapter snapshots requirement rows and their cross-links.</summary>
    [Fact]
    public async Task RequirementsAdapter_SnapshotsRequirementAndTraceabilityLinks()
    {
        using var provider = CreateProvider();
        await SeedAsync(provider, db =>
        {
            db.Requirements.Add(new RequirementEntity
            {
                Kind = "fr",
                Id = "FR-MCP-103",
                Title = "Hub-and-spoke federation",
                Body = "Federation support",
                CreatedAtUtc = "2026-05-21T19:00:00Z",
                UpdatedAtUtc = "2026-05-21T19:00:00Z",
            });
            db.RequirementTraceabilityLinks.Add(new RequirementTraceabilityLinkEntity
            {
                FrId = "FR-MCP-103",
                TargetKind = "tr",
                TargetId = "TR-MCP-FED-001",
                CreatedAtUtc = "2026-05-21T19:00:00Z",
            });
        }).ConfigureAwait(true);

        var adapter = ResolveAdapter(provider, "requirements");
        var snapshot = await adapter.SnapshotAsync("fr/FR-MCP-103", CancellationToken.None).ConfigureAwait(true);

        Assert.NotNull(snapshot.Version);
        Assert.Contains("TR-MCP-FED-001", snapshot.PayloadJson, StringComparison.Ordinal);
    }

    /// <summary>Tool and agent adapters snapshot persisted configuration without runtime process state.</summary>
    [Fact]
    public async Task ToolsAndAgentsAdapters_SnapshotPersistedConfiguration()
    {
        using var provider = CreateProvider();
        await SeedAsync(provider, db =>
        {
            var tool = new ToolDefinitionEntity
            {
                Name = "federation-status",
                Description = "Show federation status",
                BucketName = "official",
                DateTimeCreated = DateTimeOffset.Parse("2026-05-21T19:00:00Z"),
                DateTimeModified = DateTimeOffset.Parse("2026-05-21T19:01:00Z"),
            };
            tool.Tags.Add(new ToolDefinitionTagEntity { Tag = "federation" });
            db.ToolBuckets.Add(new ToolBucketEntity
            {
                Name = "official",
                Owner = "sharpninja",
                Repo = "mcp-tools",
                DateTimeCreated = DateTimeOffset.Parse("2026-05-21T19:00:00Z"),
            });
            db.ToolDefinitions.Add(tool);
            db.AgentDefinitions.Add(new AgentDefinitionEntity
            {
                Id = "codex",
                DisplayName = "Codex",
                DefaultLaunchCommand = "codex",
                DefaultInstructionFile = "AGENTS.md",
                CreatedAt = DateTime.Parse("2026-05-21T19:00:00Z"),
                ModifiedAt = DateTime.Parse("2026-05-21T19:02:00Z"),
            });
        }).ConfigureAwait(true);

        var toolsSnapshot = await ResolveAdapter(provider, "tools_buckets")
            .SnapshotAsync("official", CancellationToken.None)
            .ConfigureAwait(true);
        var agentsSnapshot = await ResolveAdapter(provider, "agents")
            .SnapshotAsync("codex", CancellationToken.None)
            .ConfigureAwait(true);

        Assert.NotNull(toolsSnapshot.Version);
        Assert.Contains("federation-status", toolsSnapshot.PayloadJson, StringComparison.Ordinal);
        Assert.NotNull(agentsSnapshot.Version);
        Assert.Contains("Codex", agentsSnapshot.PayloadJson, StringComparison.Ordinal);
    }

    /// <summary>Local-only adapters reject apply attempts and explain the exemption.</summary>
    [Fact]
    public async Task LocalOnlyAdapter_RejectsApply()
    {
        using var provider = CreateProvider();
        var adapter = ResolveAdapter(provider, "marker_state");

        var result = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-1",
            Domain = "marker_state",
            ResourceId = "AGENTS-README-FIRST.yaml",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Applied);
        Assert.True(result.Conflict);
        Assert.Contains("local-only", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IFederationStateAdapter ResolveAdapter(ServiceProvider provider, string domain)
        => provider.GetServices<IFederationStateAdapter>().Single(adapter => adapter.Domain == domain);

    private static async Task SeedAsync(ServiceProvider provider, Action<McpDbContext> seed)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        seed(db);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<IWorkspaceService>(new StaticWorkspaceService());
        services.AddScoped(_ => new WorkspaceContext { WorkspacePath = WorkspacePath });
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = $"fed-adapters-{Guid.NewGuid():N}";
        services.AddDbContext<McpDbContext>(options => options.UseInMemoryDatabase(databaseName, databaseRoot));
        services.AddFederationStateAdapters();
        services.AddSingleton<FederationStateAdapterRegistry>();
        return services.BuildServiceProvider();
    }

    private sealed class StaticWorkspaceService : IWorkspaceService
    {
        private readonly WorkspaceDto _workspace = new()
        {
            WorkspacePath = WorkspacePath,
            Name = "McpServer",
            TodoPath = "docs/Project/TODO.yaml",
            IsPrimary = true,
            DateTimeCreated = DateTimeOffset.FromUnixTimeMilliseconds(1699999999000),
            DateTimeModified = DateTimeOffset.FromUnixTimeMilliseconds(1700000000000),
            StatusPrompt = "status",
            ImplementPrompt = "implement",
            PlanPrompt = "plan",
        };

        public Task<WorkspaceListResult> ListAsync(CancellationToken ct = default)
            => Task.FromResult(new WorkspaceListResult([_workspace], 1));

        public Task<WorkspaceDto?> GetAsync(string workspacePath, CancellationToken ct = default)
            => Task.FromResult(string.Equals(workspacePath, _workspace.WorkspacePath, StringComparison.OrdinalIgnoreCase)
                ? _workspace
                : null);

        public Task<WorkspaceMutationResult> CreateAsync(WorkspaceCreateRequest request, CancellationToken ct = default)
            => Task.FromResult(new WorkspaceMutationResult(false, "Not used by adapter tests."));

        public Task<WorkspaceMutationResult> UpdateAsync(string workspacePath, WorkspaceUpdateRequest request, CancellationToken ct = default)
            => Task.FromResult(new WorkspaceMutationResult(false, "Not used by adapter tests."));

        public Task<WorkspaceMutationResult> DeleteAsync(string workspacePath, CancellationToken ct = default)
            => Task.FromResult(new WorkspaceMutationResult(false, "Not used by adapter tests."));

        public Task<WorkspaceInitResult> InitAsync(string workspacePath, CancellationToken ct = default)
            => Task.FromResult(new WorkspaceInitResult(false, "Not used by adapter tests."));
    }
}
