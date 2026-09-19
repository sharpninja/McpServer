using System.Reflection;
using McpServer.Cqrs;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 / TEST-MCP-MEMORY-011 / TEST-MCP-MEMORY-012 / TEST-MCP-MEMORY-014 / TEST-MCP-MEMORY-016:
/// Shared SQLite fixture and production-surface probes for MCP-MEMORY-002 S1/S2/S3 tests.
/// </summary>
public sealed partial class MemoryS1Harness : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Workspace A path used as the calling workspace.</summary>
    public string WorkspaceA { get; } = Path.Combine(Path.GetTempPath(), "mcp-s1-a-" + Guid.NewGuid().ToString("N"));

    /// <summary>Workspace B path used as a foreign workspace.</summary>
    public string WorkspaceB { get; } = Path.Combine(Path.GetTempPath(), "mcp-s1-b-" + Guid.NewGuid().ToString("N"));

    /// <summary>Creates an isolated in-memory SQLite schema.</summary>
    public MemoryS1Harness()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = CreateContext(WorkspaceA);
        ctx.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    /// <summary>Creates a context bound to <paramref name="workspacePath"/>.</summary>
    public McpDbContext CreateContext(string? workspacePath)
        => new(_options, new WorkspaceContext { WorkspacePath = workspacePath });

    /// <summary>Creates the current production <see cref="MemoryService"/>.</summary>
    public static MemoryService CreateService(McpDbContext db)
        => new(db, NullLogger<MemoryService>.Instance);

    /// <summary>Creates the current production <see cref="MemoryController"/>.</summary>
    public static MemoryController CreateController(IMemoryService service)
        => new(service);

    /// <summary>
    /// Dispatches <see cref="RememberMemoryCommand"/> through a production-assembly CQRS scan.
    /// Missing handlers surface as HTTP 501 so assertions fail for missing remember behavior.
    /// </summary>
    public async Task<MemoryRememberResult> RememberAsync(
        MemoryRememberRequest? request,
        bool readOnlyCaller = false,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.SendAsync(
            new RememberMemoryCommand(WorkspaceA, request, readOnlyCaller),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryRememberResult(
            StatusCode: 501,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "memory_remember handler is not registered");
    }

    /// <summary>Lists versions through the S1 CQRS query port.</summary>
    public async Task<MemoryVersionListResult> ListVersionsAsync(
        string memoryId,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.QueryAsync(
            new ListMemoryVersionsQuery(workspacePath ?? WorkspaceA, memoryId),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryVersionListResult(
            StatusCode: 501,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "list versions handler is not registered");
    }

    /// <summary>Reverts through the S1 CQRS command port.</summary>
    public async Task<MemoryRevertResult> RevertAsync(
        string memoryId,
        int versionNumber,
        bool readOnlyCaller = false,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.SendAsync(
            new RevertMemoryCommand(WorkspaceA, memoryId, versionNumber, readOnlyCaller),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryRevertResult(
            StatusCode: 501,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "revert handler is not registered");
    }

    /// <summary>Recalls through the S1/S2 CQRS query port (used by AfterRevert and S2 matrix).</summary>
    public Task<MemoryRecallResult> RecallAsync(
        string? query,
        CancellationToken cancellationToken = default)
        => RecallAsync(new MemoryRecallRequest { Query = query }, cancellationToken: cancellationToken);

    /// <summary>Recalls through the S2 CQRS query port with filters and ranking options.</summary>
    public async Task<MemoryRecallResult> RecallAsync(
        MemoryRecallRequest request,
        string? workspacePath = null,
        bool readOnlyCaller = false,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.QueryAsync(
            new RecallMemoryQuery(
                workspacePath ?? WorkspaceA,
                request.Query,
                request.MinScore,
                request.TopN,
                request.Tags,
                request.Type,
                request.Scope,
                readOnlyCaller,
                request.FusionBm25Weight,
                request.FusionVectorWeight,
                request.RerankEnabled),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryRecallResult(
            StatusCode: 501,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "memory_recall handler is not registered");
    }
}
