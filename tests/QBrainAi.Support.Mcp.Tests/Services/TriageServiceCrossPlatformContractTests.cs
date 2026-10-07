using QBrainAi.Client;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Tests.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-FED-PATH-001 / FR-MCP-FED-PATH-001-AC1..AC3: <see cref="TriageService"/> stores and
/// finds reports by the submitting workspace's own path syntax. A simulated Linux hub
/// (cwd /opt/mcpserver/app) must not prefix Windows paths; a Windows host must not re-root POSIX
/// paths. BDP v4 (plan section 2a): MockData fixture (scripted normalizer data) then Real fixture
/// (production normalizer over the simulated host).
/// </summary>
public abstract class TriageServiceCrossPlatformContractTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _dbOptions;

    /// <summary>Creates an isolated relational database for each test.</summary>
    protected TriageServiceCrossPlatformContractTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(_connection).Options;
        using var db = CreateDb("seed");
        db.Database.EnsureCreated();
    }

    /// <summary>Normalizer for a simulated host of the given platform (MockData or Real).</summary>
    protected abstract IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform);

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC1: on a Linux hub a report for a Windows workspace keeps the Windows
    /// path as original and effective workspace; the hub working directory is never prepended.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_LinuxHost_WindowsWorkspace_IsNotPrefixedWithHostWorkingDirectory()
    {
        const string windowsWorkspace = @"C:\Users\kingd";
        var sut = CreateService(windowsWorkspace, Normalizer(WorkspacePathPlatform.CaseSensitive));

        var result = await sut.SubmitReportAsync(Report("linux-hub-windows-path", windowsWorkspace), TestContext.Current.CancellationToken);

        Assert.Equal(@"C:\Users\kingd", result.WorkspacePath);
        using var db = CreateDb(@"C:\Users\kingd");
        var report = await db.TriageReports.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(@"C:\Users\kingd", report.OriginalWorkspacePath);
        Assert.Equal(@"C:\Users\kingd", report.WorkspaceId);
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC2: on a Windows host a report for a POSIX workspace keeps the POSIX
    /// path; it is never re-rooted onto a Windows drive.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_WindowsHost_PosixWorkspace_IsNotReRootedOnDrive()
    {
        const string posixWorkspace = "/home/sharpninja/github/RideAudit";
        var sut = CreateService(posixWorkspace, Normalizer(WorkspacePathPlatform.Windows));

        var result = await sut.SubmitReportAsync(Report("windows-hub-posix-path", posixWorkspace), TestContext.Current.CancellationToken);

        Assert.Equal("/home/sharpninja/github/RideAudit", result.WorkspacePath);
        using var db = CreateDb(posixWorkspace);
        var report = await db.TriageReports.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("/home/sharpninja/github/RideAudit", report.OriginalWorkspacePath);
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001: a relative submitting path belongs to the host, so a Linux hub anchors
    /// it at its own working directory.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_LinuxHost_RelativeWorkspace_AnchorsAtHostWorkingDirectory()
    {
        var sut = CreateService("/opt/mcpserver/app/workspaces/rel", Normalizer(WorkspacePathPlatform.CaseSensitive));

        var result = await sut.SubmitReportAsync(Report("linux-hub-relative-path", "workspaces/rel"), TestContext.Current.CancellationToken);

        Assert.Equal("/opt/mcpserver/app/workspaces/rel", result.WorkspacePath);
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC3: after a Linux hub accepts a report for a Windows workspace, the
    /// report and its group are found again by the submitter's path in either separator spelling.
    /// </summary>
    [Fact]
    public async Task SubmitThenLookup_LinuxHost_WindowsWorkspace_RoundTripsByOriginalPath()
    {
        var linux = Normalizer(WorkspacePathPlatform.CaseSensitive);
        var submitted = await CreateService(@"C:\Users\kingd", linux)
            .SubmitReportAsync(Report("linux-hub-roundtrip", @"C:\Users\kingd"), TestContext.Current.CancellationToken);

        var report = await CreateService(@"C:\Users\kingd", linux).GetReportAsync(submitted.ReportId, TestContext.Current.CancellationToken);
        var groups = await CreateService(@"C:\Users\kingd", linux).QueryGroupsAsync(
            workspacePath: "C:/Users/kingd",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(submitted.ReportId, report.ReportId);
        Assert.Equal(submitted.GroupId, Assert.Single(groups.Items).GroupId);
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001: on a Linux hub a QBrain.AI-related report from a Windows proxy
    /// workspace routes to the hub's POSIX QBrainAi workspace and keeps the Windows original.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_LinuxHost_RoutedReport_PreservesWindowsOriginal()
    {
        const string hubProduct = "/home/sharpninja/github/QBrainAi";
        var sut = CreateService(
            @"C:\Users\kingd",
            Normalizer(WorkspacePathPlatform.CaseSensitive),
            [Workspace(@"C:\Users\kingd", "kingd"), Workspace(hubProduct, "QBrainAi")]);

        var result = await sut.SubmitReportAsync(new TriageReportRequest
        {
            Title = "mcpserver-codex-plugin masks method_not_found",
            Summary = "The QBrain.AI Codex plugin reports success after a workflow.triage call fails.",
            Component = "mcpserver-codex-plugin",
            WorkspacePath = @"C:\Users\kingd",
        }, TestContext.Current.CancellationToken);

        Assert.Equal("/home/sharpninja/github/QBrainAi", result.WorkspacePath);
        using var db = CreateDb(hubProduct);
        var report = await db.TriageReports.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(@"C:\Users\kingd", report.OriginalWorkspacePath);
        Assert.Equal("/home/sharpninja/github/QBrainAi", report.EffectiveWorkspacePath);
    }

    private McpDbContext CreateDb(string workspacePath) =>
        new(_dbOptions, new WorkspaceContext { WorkspacePath = workspacePath });

    private TriageService CreateService(
        string contextWorkspace,
        IWorkspacePathNormalizer normalizer,
        IReadOnlyList<WorkspaceDto>? workspaces = null)
    {
        var workspaceService = Substitute.For<IWorkspaceService>();
        var list = workspaces ?? [Workspace(contextWorkspace, "ws")];
        workspaceService.ListAsync(Arg.Any<CancellationToken>()).Returns(new WorkspaceListResult(list, list.Count));
        var todo = Substitute.For<ITodoService>();
        var todoCreator = Substitute.For<ITriageTodoCreator>();

        return new TriageService(
            CreateDb(contextWorkspace),
            new WorkspaceContext { WorkspacePath = contextWorkspace },
            workspaceService,
            Substitute.For<ITriageResearchRunner>(),
            todo,
            todoCreator,
            Substitute.For<IPromptTemplateService>(),
            Microsoft.Extensions.Options.Options.Create(new TriageOptions { QuietPeriod = TimeSpan.FromMinutes(15), MaxRunTime = TimeSpan.FromMinutes(30) }),
            TimeProvider.System,
            NullLogger<TriageService>.Instance,
            normalizer);
    }

    private static TriageReportRequest Report(string dedupeKey, string workspacePath) => new()
    {
        Title = "REPL triage wrapper failure",
        Summary = "workflow.triage.report returns the wrong envelope.",
        DedupeKey = dedupeKey,
        WorkspacePath = workspacePath,
    };

    private static WorkspaceDto Workspace(string path, string name) => new()
    {
        WorkspacePath = path,
        Name = name,
        TodoPath = "docs/Project/TODO.yaml",
        StatusPrompt = string.Empty,
        ImplementPrompt = string.Empty,
        PlanPrompt = string.Empty,
    };
}

/// <summary>BDP v4 step 2: triage path contract against scripted normalizer data.</summary>
public sealed class TriageServiceCrossPlatformMockDataTests : TriageServiceCrossPlatformContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Scripted(hostPlatform);
}

/// <summary>BDP v4 step 3: triage path contract against the production normalizer.</summary>
public sealed class TriageServiceCrossPlatformRealTests : TriageServiceCrossPlatformContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Real(hostPlatform);
}
