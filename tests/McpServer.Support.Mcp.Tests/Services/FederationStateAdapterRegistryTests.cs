using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.FederationAdapters;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.Support.Mcp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Text.Json;

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
            ["context_metadata", "github_metadata", "marker_state", "mcp_transport", "repo_file_changes"],
            localOnlyDomains);

        Assert.Contains(coverage, row => row.Domain == "todo" && row.ApplySupported);
        Assert.Contains(coverage, row => row.Domain == "memory" && row.Covered && row.ApplySupported && !row.LocalOnly);
        Assert.Contains(coverage, row => row.Domain == "session_log" && row.Covered && row.ApplySupported);
        Assert.Contains(coverage, row => row.Domain == "mcp_transport" && row.LocalOnly && !row.ApplySupported);
        Assert.Collection(
            coverage.OrderBy(row => row.Domain, StringComparer.Ordinal).Select(row => (row.Domain, row.LocalOnly, row.ApplySupported)),
            row => Assert.Equal(("agents", false, true), row),
            row => Assert.Equal(("context_metadata", true, false), row),
            row => Assert.Equal(("github_metadata", true, false), row),
            row => Assert.Equal(("marker_state", true, false), row),
            row => Assert.Equal(("mcp_transport", true, false), row),
            row => Assert.Equal(("memory", false, true), row),
            row => Assert.Equal(("repo_file_changes", true, false), row),
            row => Assert.Equal(("requirements", false, true), row),
            row => Assert.Equal(("session_log", false, true), row),
            row => Assert.Equal(("todo", false, true), row),
            row => Assert.Equal(("tools_buckets", false, true), row),
            row => Assert.Equal(("workspace", false, true), row));
        Assert.True(registry.CanApply("todo"));
        Assert.True(registry.CanApply("memory"));
        Assert.True(registry.CanApply("session_log"));
        Assert.False(registry.CanApply("mcp_transport"));
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

    /// <summary>Workspace adapter applies signed state payloads through the canonical workspace service.</summary>
    [Fact]
    public async Task WorkspaceAdapter_AppliesWorkspacePayloadThroughWorkspaceService()
    {
        var workspaceService = Substitute.For<IWorkspaceService>();
        workspaceService.GetAsync(WorkspacePath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkspaceDto?>(null));
        workspaceService.CreateAsync(Arg.Any<WorkspaceCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkspaceMutationResult(true, Workspace: new WorkspaceDto
            {
                WorkspacePath = WorkspacePath,
                Name = "McpServer",
                TodoPath = "docs/Project/TODO.yaml",
                StatusPrompt = "status",
                ImplementPrompt = "implement",
                PlanPrompt = "plan",
                DateTimeModified = DateTimeOffset.FromUnixTimeMilliseconds(1700000000000),
            })));
        using var provider = CreateProvider(services => services.AddSingleton(workspaceService));
        var adapter = ResolveAdapter(provider, "workspace");

        var result = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-workspace",
            Domain = "workspace",
            ResourceId = WorkspacePath,
            HttpMethod = "PUT",
            PayloadJson = JsonSerializer.Serialize(new WorkspaceDto
            {
                WorkspacePath = WorkspacePath,
                Name = "McpServer",
                TodoPath = "docs/Project/TODO.yaml",
                IsPrimary = true,
                IsEnabled = true,
                StatusPrompt = "status",
                ImplementPrompt = "implement",
                PlanPrompt = "plan",
                DateTimeModified = DateTimeOffset.FromUnixTimeMilliseconds(1700000000000),
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        await workspaceService.Received(1)
            .CreateAsync(
                Arg.Is<WorkspaceCreateRequest>(request => request != null && request.WorkspacePath == WorkspacePath),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
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

    /// <summary>TODO adapter applies signed HTTP create operations through the TODO service.</summary>
    [Fact]
    public async Task TodoAdapter_AppliesHttpCreateThroughTodoService()
    {
        var todoService = Substitute.For<ITodoService>();
        todoService.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TodoMutationResult(
                true,
                Item: new TodoFlatItem
                {
                    Id = "PLAN-FEDERATION-002",
                    Title = "Create through federation",
                    Section = "federation",
                    Priority = "high",
                    Done = false,
                })));
        using var provider = CreateProvider(services => services.AddSingleton(todoService));
        var adapter = ResolveAdapter(provider, "todo");

        var result = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-create",
            Domain = "todo",
            HttpMethod = "POST",
            PayloadJson = JsonSerializer.Serialize(new TodoCreateRequest
            {
                Id = "PLAN-FEDERATION-002",
                Title = "Create through federation",
                Section = "federation",
                Priority = "high",
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        await todoService.Received(1)
            .CreateAsync(
                Arg.Is<TodoCreateRequest>(request => request != null && request.Id == "PLAN-FEDERATION-002"),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>TODO adapter resolves path ids for signed HTTP update operations.</summary>
    [Fact]
    public async Task TodoAdapter_AppliesHttpUpdateUsingPathId()
    {
        var todoService = Substitute.For<ITodoService>();
        todoService.UpdateAsync("PLAN-FEDERATION-003", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TodoMutationResult(
                true,
                Item: new TodoFlatItem
                {
                    Id = "PLAN-FEDERATION-003",
                    Title = "Updated through federation",
                    Section = "federation",
                    Priority = "high",
                    Done = true,
                })));
        using var provider = CreateProvider(services => services.AddSingleton(todoService));
        var adapter = ResolveAdapter(provider, "todo");

        var result = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-update",
            Domain = "todo",
            ResourceId = "/mcpserver/todo/PLAN-FEDERATION-003",
            HttpMethod = "PUT",
            Path = "/mcpserver/todo/PLAN-FEDERATION-003",
            PayloadJson = JsonSerializer.Serialize(new TodoUpdateRequest
            {
                Done = true,
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        await todoService.Received(1)
            .UpdateAsync(
                "PLAN-FEDERATION-003",
                Arg.Is<TodoUpdateRequest>(request => request != null && request.Done == true),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>TODO adapter resolves path ids for signed HTTP delete operations.</summary>
    [Fact]
    public async Task TodoAdapter_AppliesHttpDeleteUsingPathId()
    {
        var todoService = Substitute.For<ITodoService>();
        todoService.DeleteAsync("PLAN-FEDERATION-004", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TodoMutationResult(
                true,
                Item: new TodoFlatItem
                {
                    Id = "PLAN-FEDERATION-004",
                    Title = "Deleted through federation",
                    Section = "federation",
                    Priority = "high",
                    Done = true,
                })));
        using var provider = CreateProvider(services => services.AddSingleton(todoService));
        var adapter = ResolveAdapter(provider, "todo");

        var result = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-delete",
            Domain = "todo",
            ResourceId = "/mcpserver/todo/PLAN-FEDERATION-004",
            HttpMethod = "DELETE",
            Path = "/mcpserver/todo/PLAN-FEDERATION-004",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        await todoService.Received(1)
            .DeleteAsync("PLAN-FEDERATION-004", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
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

    /// <summary>Requirements adapter applies requirement rows and cross-links from signed state payloads.</summary>
    [Fact]
    public async Task RequirementsAdapter_AppliesRequirementAndTraceabilityPayload()
    {
        using var provider = CreateProvider();
        var adapter = ResolveAdapter(provider, "requirements");
        var payload = new
        {
            requirements = new[]
            {
                new
                {
                    kind = "fr",
                    id = "FR-MCP-999",
                    title = "Federated requirement",
                    body = "Replicated through federation",
                    createdAtUtc = "2026-05-22T00:00:00Z",
                    updatedAtUtc = "2026-05-22T00:00:00Z",
                },
                new
                {
                    kind = "tr",
                    id = "TR-MCP-FED-999",
                    title = "Federated technical requirement",
                    body = "Replicated through federation",
                    createdAtUtc = "2026-05-22T00:00:00Z",
                    updatedAtUtc = "2026-05-22T00:00:00Z",
                },
            },
            links = new[]
            {
                new
                {
                    frId = "FR-MCP-999",
                    targetKind = "tr",
                    targetId = "TR-MCP-FED-999",
                    createdAtUtc = "2026-05-22T00:00:00Z",
                },
            },
        };

        var result = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-req",
            Domain = "requirements",
            ResourceId = "fr/FR-MCP-999",
            PayloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        Assert.NotNull(await db.Requirements.FirstOrDefaultAsync(r => r.Id == "FR-MCP-999", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.NotNull(await db.RequirementTraceabilityLinks.FirstOrDefaultAsync(l => l.FrId == "FR-MCP-999", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
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

    /// <summary>Tools/buckets adapter applies bucket, tool definition, and tag rows from signed state payloads.</summary>
    [Fact]
    public async Task ToolsBucketsAdapter_AppliesBucketToolAndTagPayload()
    {
        using var provider = CreateProvider();
        var adapter = ResolveAdapter(provider, "tools_buckets");
        var payload = new
        {
            buckets = new[]
            {
                new
                {
                    name = "official",
                    owner = "sharpninja",
                    repo = "mcp-tools",
                    branch = "main",
                    manifestPath = "/",
                    dateTimeCreated = DateTimeOffset.Parse("2026-05-22T00:00:00Z"),
                },
            },
            tools = new[]
            {
                new
                {
                    name = "federation-status",
                    description = "Show federation status",
                    bucketName = "official",
                    dateTimeCreated = DateTimeOffset.Parse("2026-05-22T00:00:00Z"),
                    dateTimeModified = DateTimeOffset.Parse("2026-05-22T00:01:00Z"),
                    tags = new[] { "federation", "status" },
                },
            },
        };

        var result = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-tools",
            Domain = "tools_buckets",
            ResourceId = "official",
            PayloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var tool = await db.ToolDefinitions.Include(t => t.Tags).FirstOrDefaultAsync(t => t.Name == "federation-status", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(tool);
        Assert.Contains(tool.Tags, tag => tag.Tag == "federation");
    }

    /// <summary>Agents adapter applies agent definitions and workspace configuration from signed state payloads.</summary>
    [Fact]
    public async Task AgentsAdapter_AppliesDefinitionAndWorkspacePayload()
    {
        using var provider = CreateProvider();
        var adapter = ResolveAdapter(provider, "agents");
        var payload = new
        {
            definitions = new[]
            {
                new
                {
                    id = "codex",
                    displayName = "Codex",
                    defaultLaunchCommand = "codex",
                    defaultInstructionFile = "AGENTS.md",
                    defaultModelsJson = "[]",
                    defaultBranchStrategy = "feature/{agent}/{task}",
                    defaultSeedPrompt = "",
                    isBuiltIn = true,
                    createdAt = DateTime.Parse("2026-05-22T00:00:00Z"),
                    modifiedAt = DateTime.Parse("2026-05-22T00:01:00Z"),
                },
            },
            workspaceConfigs = new[]
            {
                new
                {
                    agentDefinitionId = "codex",
                    workspacePath = WorkspacePath,
                    enabled = true,
                    banned = false,
                    agentIsolation = "worktree",
                    markerAdditions = "",
                    restartPolicy = "never",
                    addedAt = DateTime.Parse("2026-05-22T00:00:00Z"),
                },
            },
        };

        var result = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-agents",
            Domain = "agents",
            ResourceId = "codex",
            PayloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        Assert.NotNull(await db.AgentDefinitions.FirstOrDefaultAsync(a => a.Id == "codex", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.NotNull(await db.AgentWorkspaces.FirstOrDefaultAsync(a => a.AgentDefinitionId == "codex", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>Session log adapter applies unified session log payloads through the canonical session log service.</summary>
    [Fact]
    public async Task SessionLogAdapter_AppliesPayloadThroughSessionLogService()
    {
        var sessionLogService = Substitute.For<ISessionLogService>();
        sessionLogService.SubmitAsync(Arg.Any<UnifiedSessionLogDto>(), null, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(42L));
        using var provider = CreateProvider(services => services.AddSingleton(sessionLogService));
        var adapter = ResolveAdapter(provider, "session_log");

        var result = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-session",
            Domain = "session_log",
            ResourceId = "Codex/Codex-20260522T000000Z-fed",
            PayloadJson = JsonSerializer.Serialize(new UnifiedSessionLogDto
            {
                SourceType = "Codex",
                SessionId = "Codex-20260522T000000Z-fed",
                Status = "completed",
                Turns = [],
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        await sessionLogService.Received(1)
            .SubmitAsync(
                Arg.Is<UnifiedSessionLogDto>(payload => payload != null && payload.SessionId == "Codex-20260522T000000Z-fed"),
                null,
                "op-session",
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>Session log DELETE replay soft-deletes the retained session graph instead of physically removing rows.</summary>
    [Fact]
    public async Task SessionLogAdapter_DeleteReplaySoftDeletesRetainedSessionGraph()
    {
        const string sessionId = "Codex-20260522T000000Z-delete-replay";
        using var provider = CreateProvider();
        await SeedAsync(provider, db =>
        {
            db.SessionLogs.Add(new SessionLogEntity
            {
                WorkspaceId = WorkspacePath,
                SourceType = "Codex",
                SessionId = sessionId,
                Status = "completed",
                Started = DateTimeOffset.Parse("2026-05-22T00:00:00Z"),
                LastUpdated = DateTimeOffset.Parse("2026-05-22T00:01:00Z"),
                Turns =
                {
                    new SessionLogTurnEntity
                    {
                        WorkspaceId = WorkspacePath,
                        RequestId = "req-delete-replay",
                        Status = "completed",
                        Actions =
                        {
                            new SessionLogActionEntity
                            {
                                WorkspaceId = WorkspacePath,
                                Order = 0,
                                Description = "delete replay action",
                                Type = "test",
                                Status = "completed",
                            },
                        },
                        Tags =
                        {
                            new SessionLogTurnTagEntity
                            {
                                WorkspaceId = WorkspacePath,
                                Tag = "delete-replay",
                            },
                        },
                    },
                },
            });
        }).ConfigureAwait(true);
        var adapter = ResolveAdapter(provider, "session_log");

        var result = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-session-delete",
            Domain = "session_log",
            ResourceId = $"Codex/{sessionId}",
            HttpMethod = "DELETE",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        Assert.Empty(await db.SessionLogs.Where(session => session.SessionId == sessionId).ToListAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        var retained = await db.SessionLogs
            .IgnoreQueryFilters()
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.Actions)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.Tags)
            .SingleAsync(session => session.SessionId == sessionId, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        AssertSoftDeleted(db, retained);
        Assert.All(retained.Turns, turn => AssertSoftDeleted(db, turn));
        Assert.All(retained.Turns.SelectMany(turn => turn.Actions), action => AssertSoftDeleted(db, action));
        Assert.All(retained.Turns.SelectMany(turn => turn.Tags), tag => AssertSoftDeleted(db, tag));
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

    private static void AssertSoftDeleted(McpDbContext db, object entity)
        => Assert.True(db.Entry(entity).Property("IsDeleted").CurrentValue is true);

    private static ServiceProvider CreateProvider(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<IWorkspaceService>(new StaticWorkspaceService());
        services.AddScoped(_ => new WorkspaceContext { WorkspacePath = WorkspacePath });
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = $"fed-adapters-{Guid.NewGuid():N}";
        services.AddDbContext<McpDbContext>(options => options.UseInMemoryDatabase(databaseName, databaseRoot));
        configureServices?.Invoke(services);
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
