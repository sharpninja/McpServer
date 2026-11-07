using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-001; TEST-MCP-TRIAGE-002; TEST-MCP-TRIAGE-004; TEST-MCP-TRIAGE-005; TEST-MCP-TRIAGE-006: the per-test state and helpers that both halves of TriageServiceTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGE-001; TEST-MCP-TRIAGE-002; TEST-MCP-TRIAGE-004; TEST-MCP-TRIAGE-005; TEST-MCP-TRIAGE-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from TriageServiceTests (PrimaryWorkspace, _connection, _dbOptions, _time, .ctor, Dispose, CreateReport, ForwardingTriageTodoCreator, CreateDb, Workspace and ManualTimeProvider) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class TriageServiceTestsBase : IDisposable
{
    internal static readonly string PrimaryWorkspace = UnitTestPaths.Combine("IncidentSource");

    internal readonly SqliteConnection _connection;
    internal readonly DbContextOptions<McpDbContext> _dbOptions;
    internal readonly ManualTimeProvider _time = new(new DateTimeOffset(2026, 6, 25, 5, 0, 0, TimeSpan.Zero));

    /// <summary>Creates an isolated relational database used by each triage test.</summary>
    protected TriageServiceTestsBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateDb(PrimaryWorkspace);
        db.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    internal static TriageReportRequest CreateReport(string dedupeKey) => new()
    {
        Title = "REPL triage wrapper failure",
        Summary = "workflow.triage.report returns the wrong envelope.",
        Component = "repl",
        ErrorSignature = "method_not_found",
        DedupeKey = dedupeKey,
        AffectedPaths = ["src/QBrainAi.Repl.Core/ReplCommandDispatcher.cs"],
    };

    /// <summary>A triage TODO creator that forwards each create request to the given TODO service.</summary>
    /// <remarks>2026-11-07, TEST-MCP-TRIAGE-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: summary added (slice S8, hostile validation finding R14-F4): this helper type became internal when slice S7 moved it into this shared base class, so it is documented like the other shared members; its code is unchanged.</remarks>
    internal sealed class ForwardingTriageTodoCreator : ITriageTodoCreator
    {
        private readonly ITodoService _todoService;

        public ForwardingTriageTodoCreator(ITodoService todoService)
        {
            _todoService = todoService;
        }

        public Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
            => _todoService.CreateAsync(request, cancellationToken);
    }

    internal McpDbContext CreateDb(string workspacePath)
        => new(_dbOptions, new WorkspaceContext { WorkspacePath = workspacePath });

    internal static WorkspaceDto Workspace(string path, string name) => new()
    {
        WorkspacePath = path,
        Name = name,
        TodoPath = "docs/Project/TODO.yaml",
        StatusPrompt = string.Empty,
        ImplementPrompt = string.Empty,
        PlanPrompt = string.Empty,
    };

    /// <summary>A time provider whose instant starts at the value it was created with and moves only when Advance is called.</summary>
    /// <remarks>2026-11-07, TEST-MCP-TRIAGE-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: summary added (slice S8, hostile validation finding R14-F4): this helper type became internal when slice S7 moved it into this shared base class, so it is documented like the other shared members; its code is unchanged.</remarks>
    internal sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan value) => _utcNow = _utcNow.Add(value);
    }
}
