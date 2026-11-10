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

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-001, TEST-MCP-TRIAGE-002, TEST-MCP-TRIAGE-004,
/// TEST-MCP-TRIAGE-005, TEST-MCP-TRIAGE-006: acceptance tests for durable triage intake,
/// deterministic grouping, asynchronous TODO conversion, failure preservation, and workspace isolation.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGE-001; TEST-MCP-TRIAGE-002; TEST-MCP-TRIAGE-004; TEST-MCP-TRIAGE-005; TEST-MCP-TRIAGE-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/TriageServiceTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class TriageServiceTestsBase in Services/TriageServiceTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class TriageServiceTests : TriageServiceTestsBase, IDisposable
{
    /// <summary>
    /// TEST-MCP-TRIAGESTORE-002 / BUG-TRIAGE-110: unreachable storage fails fast as
    /// storage-unavailable instead of hanging until the REPL timeout.
    /// </summary>
    /// <remarks>Integration: asserts an elapsed wall-clock bound on actual scheduling and timer execution (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task SubmitReportAsync_UnreachableSql_FailsFastWithStorageUnavailable()
    {
        var missingRoot = Path.Combine(Path.GetTempPath(), "triage-missing-owned-" + Guid.NewGuid().ToString("N"));
        Assert.False(Directory.Exists(missingRoot));
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var unreachableOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = Path.Combine(missingRoot, "unreachable.db"), Pooling = false }.ToString())
            .Options;
        using var db = new McpDbContext(unreachableOptions, new WorkspaceContext { WorkspacePath = PrimaryWorkspace });
        var workspaceContext = new WorkspaceContext { WorkspacePath = PrimaryWorkspace, WorkspaceName = "IncidentSource" };
        var workspaceService = Substitute.For<IWorkspaceService>();
        workspaceService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new WorkspaceListResult([Workspace(PrimaryWorkspace, "IncidentSource")], 1));
        var todo = Substitute.For<ITodoService>();
        var sut = new TriageService(
            db,
            workspaceContext,
            workspaceService,
            Substitute.For<ITriageResearchRunner>(),
            todo,
            new ForwardingTriageTodoCreator(todo),
            Substitute.For<IPromptTemplateService>(),
            Microsoft.Extensions.Options.Options.Create(new TriageOptions
            {
                QuietPeriod = TimeSpan.FromMinutes(15),
                MaxRunTime = TimeSpan.FromMinutes(30),
            }),
            _time,
            NullLogger<TriageService>.Instance);

        var thrown = await Assert.ThrowsAnyAsync<Exception>(() =>
            sut.SubmitReportAsync(CreateReport("unreachable-sql"), TestContext.Current.CancellationToken))
            .ConfigureAwait(true);
        clock.Stop();

        var classified = McpErrorClassifier.Classify(thrown);
        Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
        Assert.True(classified.Retryable);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(8), $"Intake hung for {clock.Elapsed}.");
        Assert.False(Directory.Exists(missingRoot), "Unreachable storage must not create its missing parent directory.");
        using var verify = CreateDb(PrimaryWorkspace);
        Assert.Equal(0, await verify.TriageReports.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
