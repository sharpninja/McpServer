using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using QBrainAi.Client;
using QBrainAi.Cqrs.Mvvm;
using QBrainAi.Repl.Core;
using QBrainAi.Support.Mcp.Controllers;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.McpStdio;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-HYGIENE-001: the per-test state and helpers that both halves of WorkspaceHygieneG5OverlayTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-HYGIENE-001; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted from WorkspaceHygieneG5OverlayTests (_workspace, _connection, _options, _clock, .ctor, Dispose, CreateService, CreateDb and SeedTodo) when that class split into its unit half and its integration half (slice S8, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); the accessibility changed from private to internal and the constructor takes the base class name and protected accessibility; the clock now reads the reference time from AgeBoundaryContractTestsBase, which both projects compile (see the entry on _clock); xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class WorkspaceHygieneG5OverlayTestsBase : IDisposable
{
    internal readonly string _workspace;
    internal readonly SqliteConnection _connection;
    internal readonly DbContextOptions<McpDbContext> _options;

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 (appended 2026-11-05): the clock every service of this class reads, so the stale-turn
    /// rule is decided by the test's instant and the seeds placed from it (hostile validation round 12 finding R12-F2).
    /// </summary>
    /// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: reads the same reference time through AgeBoundaryContractTestsBase.References, where AgeBoundaryContractTests.References now lives, because this base is compiled into the integration-owned project too, which cannot reach the unit test class (slice S8, hostile validation finding R14-F4); the instant is unchanged.</remarks>
    internal readonly QBrainAi.Support.Mcp.Tests.TestSupport.SettableTimeProvider _clock =
        new(QBrainAi.Support.Mcp.Tests.TestSupport.AgeBoundaryContractTestsBase.References[0]);

    /// <summary>Isolated in-memory SQLite workspace.</summary>
    protected WorkspaceHygieneG5OverlayTestsBase()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "hy-g5", Guid.NewGuid().ToString("N"));
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
        if (Directory.Exists(_workspace))
            Directory.Delete(_workspace, recursive: true);
    }

    /// <summary>TR-MCP-USECASE-018 AC-006: a workspace validation service over a new context of the class's database, reading the class's settable clock.</summary>
    /// <returns>The service under test.</returns>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the helper, changed in slice Z4 to hand the service the class's settable clock instead of TimeProvider.System (hostile validation round 12 finding R12-F2, S3 and S4), because the tenth holistic read before hostile validation round 13 (finding d1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    internal WorkspaceValidationService CreateService()
        => new(CreateDb(), _clock);

    internal McpDbContext CreateDb()
        => new(_options, new WorkspaceContext { WorkspacePath = _workspace });

    internal void SeedTodo(string id, bool done, string? doneSummary, string? remaining)
    {
        using var db = CreateDb();
        db.TodoItems.Add(new TodoItemEntity
        {
            Id = id,
            Title = id,
            Section = "overlay",
            Priority = "high",
            WorkspaceId = _workspace,
            Done = done,
            DoneSummary = doneSummary,
            Remaining = remaining,
        });
        db.SaveChanges();
    }
}
