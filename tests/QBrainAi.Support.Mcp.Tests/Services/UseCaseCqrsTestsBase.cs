using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.UseCases.Models;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.UseCases;
using QBrainAi.Support.Mcp.UseCases.Commands;
using QBrainAi.Support.Mcp.UseCases.Queries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-002; FR-MCP-USECASE-001; TR-MCP-USECASE-002: the per-test state and helpers that both halves of UseCaseCqrsTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-002; FR-MCP-USECASE-001; TR-MCP-USECASE-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from UseCaseCqrsTests (_connection, _options, _workspace, _ctx, .ctor, Dispose, CreateContext, CreateWorkspaceContext and SeedWorkspace) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class UseCaseCqrsTestsBase : IDisposable
{
    internal readonly SqliteConnection _connection;
    internal readonly DbContextOptions<McpDbContext> _options;
    internal readonly string _workspace = Path.Combine(Path.GetTempPath(), "mcp-uc-cqrs-" + Guid.NewGuid().ToString("N"));
    internal readonly CallContext _ctx = new();

    /// <summary>Creates an isolated SQLite schema shared across handler tests.</summary>
    protected UseCaseCqrsTestsBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
        SeedWorkspace(ctx, _workspace, "UseCase CQRS");
        ctx.SaveChanges();
    }

    /// <summary>Releases the shared SQLite connection.</summary>
    public void Dispose() => _connection.Dispose();

    internal McpDbContext CreateContext()
    {
        return new McpDbContext(_options, CreateWorkspaceContext());
    }

    internal WorkspaceContext CreateWorkspaceContext()
        => new()
        {
            WorkspacePath = _workspace,
            WorkspaceName = "uc-cqrs",
            DataDirectory = _workspace,
            TodoFilePath = Path.Combine(_workspace, "docs", "todo.yaml"),
            SessionsPath = Path.Combine(_workspace, "docs", "sessions"),
            ExternalDocsPath = Path.Combine(_workspace, "docs", "external"),
        };

    internal static void SeedWorkspace(McpDbContext ctx, string workspaceId, string name)
    {
        if (ctx.Workspaces.IgnoreQueryFilters().Any(w => w.WorkspaceId == workspaceId))
            return;

        ctx.Workspaces.Add(new WorkspaceEntity
        {
            WorkspaceId = workspaceId,
            WorkspacePath = workspaceId,
            Name = name,
        });
    }
}
