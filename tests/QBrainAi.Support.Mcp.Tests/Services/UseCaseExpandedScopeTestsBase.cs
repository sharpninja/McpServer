using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.UseCases;
using QBrainAi.Support.Mcp.UseCases.Commands;
using QBrainAi.Support.Mcp.UseCases.Models;
using QBrainAi.Support.Mcp.UseCases.Queries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-USECASE-008; TR-MCP-USECASE-018 AC-006: the per-test state and helpers that both halves of UseCaseExpandedScopeTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-USECASE-008; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from UseCaseExpandedScopeTests (_connection, _provider, _workspace, .ctor and Dispose) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class UseCaseExpandedScopeTestsBase : IDisposable
{
    internal readonly SqliteConnection _connection;
    internal readonly ServiceProvider _provider;
    internal readonly string _workspace = Path.Combine(Path.GetTempPath(), "mcp-uc-expanded-" + Guid.NewGuid().ToString("N"));

    /// <summary>Builds an in-memory EF store with Use Case CQRS registered.</summary>
    protected UseCaseExpandedScopeTestsBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<McpDbContext>(o => o.UseSqlite(_connection));
        services.AddScoped<WorkspaceContext>();
        services.AddCqrsDispatcher();
        services.AddUseCaseCqrs();
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        db.Database.EnsureCreated();
        db.Workspaces.Add(new WorkspaceEntity { WorkspaceId = _workspace, WorkspacePath = _workspace, Name = "expanded" });
        db.SaveChanges();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
