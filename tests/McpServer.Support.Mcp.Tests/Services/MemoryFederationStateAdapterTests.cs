using System.Text.Json;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.FederationAdapters;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-MEMORY-FED-001: tests adapter-backed Memory federation under
/// FR-MCP-MEMORY-008 and TR-MCP-FED-MEMORY-001 using isolated EF stores and
/// signed federation operation envelopes.
/// </summary>
public sealed class MemoryFederationStateAdapterTests
{
    private const string WorkspaceA = @"F:\GitHub\McpServer";
    private const string WorkspaceB = @"F:\GitHub\OtherWorkspace";
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Memory create operations preserve explicit ids, categories, raw text, scopes, workspace ownership, and initial versions.</summary>
    [Fact]
    public async Task MemoryFederationAdapter_CreatePreservesGlobalAndWorkspaceState()
    {
        using var provider = CreateProvider();
        var adapter = ResolveMemoryAdapter(provider);

        var workspaceResult = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-memory-create-workspace",
            Domain = "memory",
            ResourceId = "MEMORY-OPERATOR-001",
            GlobalWorkspaceId = WorkspaceA,
            HttpMethod = "POST",
            Path = "/mcpserver/memory",
            PayloadJson = JsonSerializer.Serialize(new MemoryAddRequest
            {
                Id = "MEMORY-OPERATOR-001",
                Category = "operator",
                Scope = MemoryScope.Workspace,
                Text = "Keep this raw text exactly.\nSecond line.",
                UpdatedBy = "Codex",
            }, s_jsonOptions),
        }, CancellationToken.None).ConfigureAwait(true);
        var globalResult = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-memory-create-global",
            Domain = "memory",
            ResourceId = "MEMORY-GLOBAL-001",
            GlobalWorkspaceId = WorkspaceA,
            HttpMethod = "POST",
            Path = "/mcpserver/memory",
            PayloadJson = JsonSerializer.Serialize(new MemoryAddRequest
            {
                Id = "MEMORY-GLOBAL-001",
                Category = "global",
                Scope = MemoryScope.Global,
                Text = "Global raw text.",
                UpdatedBy = "Codex",
            }, s_jsonOptions),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(workspaceResult.Applied);
        Assert.Equal("1", workspaceResult.Version);
        Assert.True(globalResult.Applied);
        Assert.Equal("1", globalResult.Version);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var workspace = await db.Memories.IgnoreQueryFilters().SingleAsync(m => m.Id == "MEMORY-OPERATOR-001").ConfigureAwait(true);
        Assert.Equal("OPERATOR", workspace.Category);
        Assert.Equal(MemoryEntity.WorkspaceScope, workspace.Scope);
        Assert.Equal(WorkspaceA, workspace.WorkspaceId);
        Assert.Equal("Keep this raw text exactly.\nSecond line.", workspace.Text);
        Assert.Equal(1, workspace.Version);
        Assert.Equal("Codex", workspace.UpdatedBy);
        Assert.NotEqual(default, workspace.CreatedAtUtc);
        Assert.NotEqual(default, workspace.UpdatedAtUtc);

        var global = await db.Memories.IgnoreQueryFilters().SingleAsync(m => m.Id == "MEMORY-GLOBAL-001").ConfigureAwait(true);
        Assert.Equal("GLOBAL", global.Category);
        Assert.Equal(MemoryEntity.GlobalScope, global.Scope);
        Assert.Null(global.WorkspaceId);
        Assert.Equal("Global raw text.", global.Text);
    }

    /// <summary>Memory update operations increment versions and preserve workspace ownership when scope is unchanged.</summary>
    [Fact]
    public async Task MemoryFederationAdapter_UpdateIncrementsVersionAndPreservesWorkspaceOwnership()
    {
        using var provider = CreateProvider();
        await SeedMemoryAsync(provider, "MEMORY-OPERATOR-002", WorkspaceA, text: "Before", version: 1).ConfigureAwait(true);
        var adapter = ResolveMemoryAdapter(provider);

        var result = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-memory-update",
            Domain = "memory",
            ResourceId = "MEMORY-OPERATOR-002",
            GlobalWorkspaceId = WorkspaceA,
            HttpMethod = "PUT",
            Path = "/mcpserver/memory/MEMORY-OPERATOR-002",
            PayloadJson = JsonSerializer.Serialize(new MemoryUpdateRequest
            {
                Text = "After",
                UpdatedBy = "Codex",
            }, s_jsonOptions),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        Assert.Equal("2", result.Version);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var row = await db.Memories.IgnoreQueryFilters().SingleAsync(m => m.Id == "MEMORY-OPERATOR-002").ConfigureAwait(true);
        Assert.Equal("After", row.Text);
        Assert.Equal(2, row.Version);
        Assert.Equal(WorkspaceA, row.WorkspaceId);
        Assert.Equal(MemoryEntity.WorkspaceScope, row.Scope);
    }

    /// <summary>Memory delete operations soft-delete rows and are idempotent when replayed.</summary>
    [Fact]
    public async Task MemoryFederationAdapter_DeleteSoftDeletesAndIsIdempotent()
    {
        using var provider = CreateProvider();
        await SeedMemoryAsync(provider, "MEMORY-OPERATOR-003", WorkspaceA, text: "Delete me", version: 4).ConfigureAwait(true);
        var adapter = ResolveMemoryAdapter(provider);

        var first = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-memory-delete",
            Domain = "memory",
            ResourceId = "MEMORY-OPERATOR-003",
            GlobalWorkspaceId = WorkspaceA,
            HttpMethod = "DELETE",
            Path = "/mcpserver/memory/MEMORY-OPERATOR-003",
        }, CancellationToken.None).ConfigureAwait(true);
        var second = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-memory-delete-replay",
            Domain = "memory",
            ResourceId = "MEMORY-OPERATOR-003",
            GlobalWorkspaceId = WorkspaceA,
            HttpMethod = "DELETE",
            Path = "/mcpserver/memory/MEMORY-OPERATOR-003",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(first.Applied);
        Assert.True(second.Applied);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var row = await db.Memories.IgnoreQueryFilters().SingleAsync(m => m.Id == "MEMORY-OPERATOR-003").ConfigureAwait(true);
        Assert.True(db.Entry(row).Property<bool>("IsDeleted").CurrentValue);
    }

    /// <summary>Workspace-scoped memory updates from a different global workspace are conflicts and do not mutate hub state.</summary>
    [Fact]
    public async Task MemoryFederationAdapter_CrossWorkspaceUpdateConflicts()
    {
        using var provider = CreateProvider();
        await SeedMemoryAsync(provider, "MEMORY-OPERATOR-004", WorkspaceA, text: "Original", version: 7).ConfigureAwait(true);
        var adapter = ResolveMemoryAdapter(provider);

        var result = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-memory-cross-workspace",
            Domain = "memory",
            ResourceId = "MEMORY-OPERATOR-004",
            GlobalWorkspaceId = WorkspaceB,
            HttpMethod = "PATCH",
            Path = "/mcpserver/memory/MEMORY-OPERATOR-004",
            PayloadJson = JsonSerializer.Serialize(new MemoryUpdateRequest { Text = "Wrong workspace" }, s_jsonOptions),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Applied);
        Assert.True(result.Conflict);
        Assert.Equal("7", result.Version);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var row = await db.Memories.IgnoreQueryFilters().SingleAsync(m => m.Id == "MEMORY-OPERATOR-004").ConfigureAwait(true);
        Assert.Equal("Original", row.Text);
        Assert.Equal(7, row.Version);
    }

    /// <summary>Invalid memory payloads and ids return conflicts instead of being applied.</summary>
    [Fact]
    public async Task MemoryFederationAdapter_InvalidPayloadsAndIdsConflict()
    {
        using var provider = CreateProvider();
        var adapter = ResolveMemoryAdapter(provider);

        var invalidJson = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-memory-invalid-json",
            Domain = "memory",
            ResourceId = "MEMORY-OPERATOR-005",
            GlobalWorkspaceId = WorkspaceA,
            HttpMethod = "POST",
            Path = "/mcpserver/memory",
            PayloadJson = "{not-json",
        }, CancellationToken.None).ConfigureAwait(true);
        var missingId = await adapter.ApplyAsync(new FederationStateOperation
        {
            OperationId = "op-memory-missing-id",
            Domain = "memory",
            GlobalWorkspaceId = WorkspaceA,
            HttpMethod = "POST",
            Path = "/mcpserver/memory",
            PayloadJson = JsonSerializer.Serialize(new MemoryAddRequest
            {
                Category = "operator",
                Scope = MemoryScope.Workspace,
                Text = "No explicit id",
            }, s_jsonOptions),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(invalidJson.Conflict);
        Assert.True(missingId.Conflict);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        Assert.Empty(await db.Memories.IgnoreQueryFilters().ToListAsync().ConfigureAwait(true));
    }

    /// <summary>Signed memory envelopes apply through the federation controller and create a hub memory row.</summary>
    [Fact]
    public async Task MemoryFederation_SignedEnvelopeAppliesMemoryOperation()
    {
        using var provider = CreateProvider();
        var topology = provider.GetRequiredService<IFederationTopologyService>();
        var signer = provider.GetRequiredService<IFederationEnvelopeSigner>();
        var apply = provider.GetRequiredService<IFederationOperationApplyService>();
        var controller = CreateController(provider, topology, signer, apply);
        var operation = CreateMemoryCreateOperation("op-memory-signed", "MEMORY-OPERATOR-006", "Signed apply", WorkspaceA);
        var envelope = signer.Sign(operation, "PAYTON-LEGION2");

        var result = await controller.RecordEnvelope(envelope, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FederationOperationResponse>(ok.Value);
        Assert.Equal("applied", response.Status);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var row = await db.Memories.IgnoreQueryFilters().SingleAsync(m => m.Id == "MEMORY-OPERATOR-006").ConfigureAwait(true);
        Assert.Equal("Signed apply", row.Text);
    }

    /// <summary>Stale memory base versions create federation conflicts and do not overwrite hub state.</summary>
    [Fact]
    public async Task MemoryFederation_StaleBaseVersionCreatesConflictAndDoesNotOverwrite()
    {
        using var provider = CreateProvider();
        await SeedMemoryAsync(provider, "MEMORY-OPERATOR-007", WorkspaceA, text: "Hub wins", version: 2).ConfigureAwait(true);
        var topology = provider.GetRequiredService<IFederationTopologyService>();
        var signer = provider.GetRequiredService<IFederationEnvelopeSigner>();
        var apply = provider.GetRequiredService<IFederationOperationApplyService>();
        var controller = CreateController(provider, topology, signer, apply);
        var operation = new FederationOperationRequest
        {
            OperationId = "op-memory-stale",
            ProxyId = "PAYTON-LEGION2",
            Domain = "memory",
            ResourceId = "MEMORY-OPERATOR-007",
            GlobalWorkspaceId = WorkspaceA,
            HttpMethod = "PUT",
            Path = "/mcpserver/memory/MEMORY-OPERATOR-007",
            BodyBase64 = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new MemoryUpdateRequest { Text = "Proxy stale" }, s_jsonOptions)),
            BaseVersion = "1",
        };
        var envelope = signer.Sign(operation, "PAYTON-LEGION2");

        var result = await controller.RecordEnvelope(envelope, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FederationOperationResponse>(ok.Value);
        Assert.Equal("conflict", response.Status);

        var conflicts = await topology.ListConflictsAsync("PAYTON-LEGION2", openOnly: true, CancellationToken.None).ConfigureAwait(true);
        var conflict = Assert.Single(conflicts);
        Assert.Equal("memory", conflict.Domain);
        Assert.Equal("MEMORY-OPERATOR-007", conflict.ResourceId);
        Assert.Equal("1", conflict.ProxyVersion);
        Assert.Equal("2", conflict.HubVersion);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var row = await db.Memories.IgnoreQueryFilters().SingleAsync(m => m.Id == "MEMORY-OPERATOR-007").ConfigureAwait(true);
        Assert.Equal("Hub wins", row.Text);
        Assert.Equal(2, row.Version);
    }

    /// <summary>Memory operations create hub fanout rows and recipient apply recreates the memory state.</summary>
    [Fact]
    public async Task MemoryFederation_OperationCreatesFanoutAndRecipientApplyWorks()
    {
        using var hub = CreateProvider();
        var hubTopology = hub.GetRequiredService<IFederationTopologyService>();
        await hubTopology.EnrollAsync(new FederationEnrollmentRequest { ProxyId = "PAYTON-LEGION2" }, CancellationToken.None).ConfigureAwait(true);
        await hubTopology.EnrollAsync(new FederationEnrollmentRequest { ProxyId = "PAYTON-DESKTOP" }, CancellationToken.None).ConfigureAwait(true);
        var hubSigner = hub.GetRequiredService<IFederationEnvelopeSigner>();
        var hubApply = hub.GetRequiredService<IFederationOperationApplyService>();
        var hubController = CreateController(hub, hubTopology, hubSigner, hubApply);
        var operation = CreateMemoryCreateOperation("op-memory-fanout", "MEMORY-OPERATOR-008", "Fanout apply", WorkspaceA);

        var applyResult = await hubController.RecordEnvelope(hubSigner.Sign(operation, "PAYTON-LEGION2"), CancellationToken.None).ConfigureAwait(true);

        var applyOk = Assert.IsType<OkObjectResult>(applyResult.Result);
        var applyResponse = Assert.IsType<FederationOperationResponse>(applyOk.Value);
        Assert.Equal("applied", applyResponse.Status);

        var syncResult = await hubController.Sync("PAYTON-DESKTOP", 0, CancellationToken.None).ConfigureAwait(true);
        var syncOk = Assert.IsType<OkObjectResult>(syncResult.Result);
        var syncItems = Assert.IsAssignableFrom<IReadOnlyList<FederationSyncItem>>(syncOk.Value);
        var syncItem = Assert.Single(syncItems);
        Assert.Equal("memory", syncItem.Domain);
        Assert.NotNull(syncItem.Envelope);

        using var recipient = CreateProvider();
        var recipientApply = recipient.GetRequiredService<IFederationOperationApplyService>();
        var recipientResult = await recipientApply.ApplyAsync(syncItem.Envelope!.Operation, CancellationToken.None).ConfigureAwait(true);

        Assert.True(recipientResult.Applied);
        await using var scope = recipient.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var row = await db.Memories.IgnoreQueryFilters().SingleAsync(m => m.Id == "MEMORY-OPERATOR-008").ConfigureAwait(true);
        Assert.Equal("Fanout apply", row.Text);
        Assert.Equal(WorkspaceA, row.WorkspaceId);
    }

    private static IFederationStateAdapter ResolveMemoryAdapter(ServiceProvider provider)
        => provider.GetServices<IFederationStateAdapter>().Single(adapter => adapter.Domain == "memory");

    private static FederationController CreateController(
        ServiceProvider provider,
        IFederationTopologyService topology,
        IFederationEnvelopeSigner signer,
        IFederationOperationApplyService apply)
        => new(
            new FederationRegistry(Microsoft.Extensions.Options.Options.Create(new FederationOptions())),
            CreateEmptyTunnelRegistry(),
            topologyService: topology,
            adapterRegistry: provider.GetRequiredService<FederationStateAdapterRegistry>(),
            envelopeSigner: signer,
            operationApplyService: apply);

    private static TunnelRegistry CreateEmptyTunnelRegistry()
        => new([], Microsoft.Extensions.Options.Options.Create(new TunnelOptions()), Microsoft.Extensions.Logging.Abstractions.NullLogger<TunnelRegistry>.Instance);

    private static FederationOperationRequest CreateMemoryCreateOperation(string operationId, string memoryId, string text, string workspaceId)
        => new()
        {
            OperationId = operationId,
            ProxyId = "PAYTON-LEGION2",
            Domain = "memory",
            ResourceId = memoryId,
            GlobalWorkspaceId = workspaceId,
            HttpMethod = "POST",
            Path = "/mcpserver/memory",
            BodyBase64 = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new MemoryAddRequest
            {
                Id = memoryId,
                Category = "operator",
                Scope = MemoryScope.Workspace,
                Text = text,
                UpdatedBy = "Codex",
            }, s_jsonOptions)),
        };

    private static async Task SeedMemoryAsync(
        ServiceProvider provider,
        string id,
        string workspaceId,
        string text,
        int version)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        db.Memories.Add(new MemoryEntity
        {
            Id = id,
            Category = "OPERATOR",
            Scope = MemoryEntity.WorkspaceScope,
            WorkspaceId = workspaceId,
            Text = text,
            Version = version,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            UpdatedBy = "Seed",
        });
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        var monitor = Substitute.For<IOptionsMonitor<FederationOptions>>();
        monitor.CurrentValue.Returns(new FederationOptions
        {
            EnrollmentToken = "test-secret",
            Signing = new FederationSigningOptions
            {
                Enabled = true,
                EnvelopeTtlSeconds = 300,
            },
            Sync = new FederationSyncOptions
            {
                HeartbeatSeconds = 5,
            },
        });
        services.AddSingleton(monitor);
        services.AddScoped(_ => new WorkspaceContext { WorkspacePath = WorkspaceA });
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = $"memory-fed-{Guid.NewGuid():N}";
        services.AddDbContext<McpDbContext>(options => options.UseInMemoryDatabase(databaseName, databaseRoot));
        services.AddFederationStateAdapters();
        services.AddSingleton<FederationStateAdapterRegistry>();
        services.AddSingleton<IFederationTopologyService, FederationTopologyService>();
        services.AddSingleton<IFederationOperationApplyService, FederationOperationApplyService>();
        services.AddSingleton<IFederationEnvelopeSigner, FederationEnvelopeSigner>();
        return services.BuildServiceProvider();
    }
}
