using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using QBrainAi.Client;
using QBrainAi.Cqrs.Mvvm;
using QBrainAi.Repl.Core;
using QBrainAi.Support.Mcp.Controllers;
using QBrainAi.Support.Mcp.McpStdio;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-HOSTILEREVIEW-001; FR-MCP-HOSTILEREVIEW-001; FR-MCP-HOSTILEREVIEW-002; FR-MCP-HOSTILEREVIEW-003; FR-MCP-HOSTILEREVIEW-004; FR-MCP-HOSTILEREVIEW-005; FR-MCP-HOSTILEREVIEW-006: the per-test state and helpers that both halves of HostileReviewG4OverlayTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-HOSTILEREVIEW-001; FR-MCP-HOSTILEREVIEW-001; FR-MCP-HOSTILEREVIEW-002; FR-MCP-HOSTILEREVIEW-003; FR-MCP-HOSTILEREVIEW-004; FR-MCP-HOSTILEREVIEW-005; FR-MCP-HOSTILEREVIEW-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from HostileReviewG4OverlayTests (_workspace, _connection, _options, .ctor, Dispose, CreateService, CreateDb, SeedTodo and Request) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class HostileReviewG4OverlayTestsBase : IDisposable
{
    internal readonly string _workspace;
    internal readonly SqliteConnection _connection;
    internal readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Isolated in-memory SQLite workspace with a seeded TODO row.</summary>
    protected HostileReviewG4OverlayTestsBase()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "hr-g4", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(_connection).Options;
        using var db = CreateDb();
        db.Database.EnsureCreated();
        SeedTodo(db, "MCP-HOSTILEREVIEW-001");
        db.SaveChanges();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_workspace))
            Directory.Delete(_workspace, recursive: true);
    }

    internal HostileReviewService CreateService()
        => new(CreateDb(), new HostileReviewWorkerOptions());

    internal McpDbContext CreateDb()
        => new(_options, new WorkspaceContext { WorkspacePath = _workspace });

    internal void SeedTodo(McpDbContext db, string id, string? title = null, string? workspaceId = null)
    {
        db.TodoItems.Add(new TodoItemEntity
        {
            Id = id,
            Title = title ?? id,
            Section = "overlay",
            Priority = "high",
            WorkspaceId = workspaceId ?? _workspace,
        });
    }

    internal static IYamlEnvelope Request(string method, Dictionary<string, object?> args)
        => new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-g4-hostile-review-parity",
                Method = method,
                Params = args,
            },
        };
}
