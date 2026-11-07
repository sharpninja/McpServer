using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QBrainAi.Support.Mcp.Controllers;
using QBrainAi.Support.Mcp.McpStdio;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Requirements.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-WIKIEXPORT-003; FR-MCP-WIKIEXPORT-003; FR-MCP-WIKIEXPORT-004; FR-MCP-WIKIEXPORT-005: the per-test state and helpers that both halves of WikiDumpG6OverlayTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-WIKIEXPORT-003; FR-MCP-WIKIEXPORT-003; FR-MCP-WIKIEXPORT-004; FR-MCP-WIKIEXPORT-005; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from WikiDumpG6OverlayTests (_workspace, _connection, _options, .ctor, Dispose, CreateDumpService, CreateDb and Camel) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class WikiDumpG6OverlayTestsBase : IDisposable
{
    internal readonly string _workspace;
    internal readonly SqliteConnection _connection;
    internal readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Isolated in-memory SQLite workspace.</summary>
    protected WikiDumpG6OverlayTestsBase()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "wiki-g6", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(_connection).Options;
        using var db = CreateDb();
        db.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        try
        {
            if (Directory.Exists(_workspace))
                Directory.Delete(_workspace, recursive: true);
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }

    internal WikiDumpService CreateDumpService()
        => new(CreateDb(), TimeProvider.System);

    internal McpDbContext CreateDb()
        => new(_options, new WorkspaceContext { WorkspacePath = _workspace });

    internal static JsonSerializerOptions Camel()
        => new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
