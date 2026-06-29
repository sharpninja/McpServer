using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-TRIAGE-001 and TEST-MCP-TRIAGE-002: controller-level coverage for the public
/// triage REST surface.
/// </summary>
public sealed class TriageControllerTests
{
    /// <summary>
    /// TEST-MCP-TRIAGE-001: POST /mcpserver/triage/reports returns accepted queue state for
    /// valid intake and delegates the shared report contract to the service.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_WhenServiceAccepts_ReturnsAcceptedQueueState()
    {
        var service = Substitute.For<ITriageService>();
        var quietDeadline = new DateTimeOffset(2026, 6, 25, 12, 15, 0, TimeSpan.Zero);
        service.SubmitReportAsync(Arg.Any<TriageReportRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageReportSubmitResult
            {
                Success = true,
                ReportId = "triage-report-001",
                GroupId = "triage-group-001",
                Status = "collecting",
                QuietDeadlineUtc = quietDeadline,
                WorkspacePath = "F:\\GitHub\\McpServer",
            });

        var controller = new TriageController(service);
        var request = new TriageReportRequest
        {
            Title = "mcpserver plugin wrapper masks failures",
            Summary = "The plugin returns success after workflow.triage fails.",
            Component = "mcpserver-codex-plugin",
            DedupeKey = "plugin-wrapper-failure",
        };

        var action = await controller.SubmitReportAsync(request, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(action.Result);
        var result = Assert.IsType<TriageReportSubmitResult>(accepted.Value);
        Assert.True(result.Success);
        Assert.Equal("triage-report-001", result.ReportId);
        Assert.Equal("triage-group-001", result.GroupId);
        Assert.Equal("collecting", result.Status);
        Assert.Equal(quietDeadline, result.QuietDeadlineUtc);
        await service.Received(1).SubmitReportAsync(
            Arg.Is<TriageReportRequest>(value =>
                value != null &&
                value.Title == request.Title &&
                value.Summary == request.Summary &&
                value.Component == request.Component &&
                value.DedupeKey == request.DedupeKey),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-001: invalid report intake returns a Bad Request error envelope.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_WhenServiceRejects_ReturnsBadRequestEnvelope()
    {
        var service = Substitute.For<ITriageService>();
        service.SubmitReportAsync(Arg.Any<TriageReportRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageReportSubmitResult { Success = false, Error = "title is required." });

        var controller = new TriageController(service);
        var action = await controller.SubmitReportAsync(
            new TriageReportRequest { Title = "", Summary = "missing title" },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        var result = Assert.IsType<TriageReportSubmitResult>(badRequest.Value);
        Assert.False(result.Success);
        Assert.Equal("title is required.", result.Error);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-002: flush and retry endpoints route group operations through the
    /// shared triage service and return current group state.
    /// </summary>
    [Theory]
    [InlineData("flush")]
    [InlineData("retry")]
    public async Task GroupMutationAsync_WhenGroupExists_ReturnsOkGroupState(string operation)
    {
        var service = Substitute.For<ITriageService>();
        var group = new TriageGroupDetail
        {
            GroupId = "triage-group-001",
            Status = "collecting",
            ReportCount = 2,
            WorkspacePath = "F:\\GitHub\\McpServer",
            Title = "Wrapper bug",
            Summary = "Plugin wrapper bug",
            QuietDeadlineUtc = DateTimeOffset.UtcNow,
        };
        service.FlushGroupAsync("triage-group-001", Arg.Any<CancellationToken>()).Returns(group);
        service.RetryGroupAsync("triage-group-001", false, Arg.Any<CancellationToken>()).Returns(group);

        var controller = new TriageController(service);
        var action = operation == "flush"
            ? await controller.FlushGroupAsync("triage-group-001", CancellationToken.None)
            : await controller.RetryGroupAsync("triage-group-001", force: false, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(group, ok.Value);
    }

    /// <summary>TEST-MCP-TRIAGE-005: retry endpoint forwards the optional force flag.</summary>
    [Fact]
    public async Task RetryGroupAsync_WhenForceTrue_ForwardsForceFlag()
    {
        var service = Substitute.For<ITriageService>();
        var group = new TriageGroupDetail
        {
            GroupId = "triage-group-001",
            Status = "collecting",
            ReportCount = 2,
            WorkspacePath = "F:\\GitHub\\McpServer",
            QuietDeadlineUtc = DateTimeOffset.UtcNow,
        };
        service.RetryGroupAsync("triage-group-001", true, Arg.Any<CancellationToken>()).Returns(group);
        var controller = new TriageController(service);

        var action = await controller.RetryGroupAsync("triage-group-001", force: true, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(group, ok.Value);
        await service.Received(1).RetryGroupAsync("triage-group-001", true, Arg.Any<CancellationToken>());
    }

    /// <summary>TEST-TRIAGE-003: group edit endpoints route selection commands through the service.</summary>
    [Theory]
    [InlineData("new")]
    [InlineData("consolidate")]
    [InlineData("merge")]
    public async Task GroupEditAsync_WhenSelectionIsValid_ReturnsOkEditResult(string operation)
    {
        var service = Substitute.For<ITriageService>();
        var request = new TriageGroupSelectionRequest
        {
            GroupIds = ["triage-group-source"],
            ReportIds = ["triage-report-001"],
        };
        var result = new TriageGroupEditResult
        {
            Group = new TriageGroupDetail
            {
                GroupId = operation == "new" ? "triage-group-new" : "triage-group-target",
                Status = "collecting",
                ReportCount = 2,
                QuietDeadlineUtc = DateTimeOffset.UtcNow,
            },
            MovedReportCount = 2,
        };
        service.CreateGroupFromSelectionAsync(request, Arg.Any<CancellationToken>()).Returns(result);
        service.ConsolidateIntoGroupAsync("triage-group-target", request, Arg.Any<CancellationToken>()).Returns(result);
        service.MergeGroupsAsync("triage-group-target", request, Arg.Any<CancellationToken>()).Returns(result);

        var controller = new TriageController(service);
        var action = operation switch
        {
            "new" => await controller.CreateGroupFromSelectionAsync(request, CancellationToken.None),
            "consolidate" => await controller.ConsolidateIntoGroupAsync("triage-group-target", request, CancellationToken.None),
            _ => await controller.MergeGroupsAsync("triage-group-target", request, CancellationToken.None),
        };

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(result, ok.Value);
    }

    /// <summary>TEST-TRIAGE-003: group edit endpoints return Bad Request for invalid selections.</summary>
    [Fact]
    public async Task CreateGroupFromSelectionAsync_WhenSelectionInvalid_ReturnsBadRequest()
    {
        var service = Substitute.For<ITriageService>();
        service.CreateGroupFromSelectionAsync(Arg.Any<TriageGroupSelectionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TriageGroupEditResult>(new ArgumentException("At least one group or report id is required.")));

        var action = await new TriageController(service).CreateGroupFromSelectionAsync(
            new TriageGroupSelectionRequest(),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        Assert.Contains("At least one group or report id is required.", badRequest.Value!.ToString(), StringComparison.Ordinal);
    }

    /// <summary>TEST-TRIAGE-001: GET /mcpserver/triage/dashboard returns queue and run history state.</summary>
    [Fact]
    public async Task GetDashboardAsync_ReturnsDashboardState()
    {
        var service = Substitute.For<ITriageService>();
        var dashboard = new TriageDashboardResult
        {
            TriageQueue =
            [
                new TriageGroupDetail
                {
                    GroupId = "triage-group-new",
                    Status = "new",
                    ReportCount = 1,
                    QuietDeadlineUtc = DateTimeOffset.UtcNow,
                },
            ],
            RunHistory =
            [
                new TriageResearchRunDetail
                {
                    RunId = "triage-run-001",
                    GroupId = "triage-group-new",
                    Status = "completed",
                    StartedUtc = DateTimeOffset.UtcNow,
                },
            ],
            TotalGroupCount = 1,
            TotalRunCount = 1,
        };
        service.GetDashboardAsync("F:\\GitHub\\McpServer", Arg.Any<CancellationToken>()).Returns(dashboard);

        var action = await new TriageController(service).GetDashboardAsync("F:\\GitHub\\McpServer", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(dashboard, ok.Value);
        await service.Received(1).GetDashboardAsync("F:\\GitHub\\McpServer", Arg.Any<CancellationToken>());
    }

    /// <summary>TEST-TRIAGE-001: run-history query endpoint returns current AI triage run statuses.</summary>
    [Fact]
    public async Task QueryRunsAsync_ReturnsRunHistory()
    {
        var service = Substitute.For<ITriageService>();
        var query = new TriageRunQueryResult
        {
            Items =
            [
                new TriageResearchRunDetail
                {
                    RunId = "triage-run-001",
                    GroupId = "triage-group-001",
                    Status = "failed",
                    Error = "schema validation failed",
                    StartedUtc = DateTimeOffset.UtcNow,
                },
            ],
            TotalCount = 1,
        };
        service.QueryRunsAsync("failed", "triage-group-001", "F:\\GitHub\\McpServer", Arg.Any<CancellationToken>())
            .Returns(query);

        var action = await new TriageController(service).QueryRunsAsync(
            "failed",
            "triage-group-001",
            "F:\\GitHub\\McpServer",
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(query, ok.Value);
    }

    /// <summary>TEST-TRIAGE-002: triage TODO endpoint returns created TODO ids with timestamps.</summary>
    [Fact]
    public async Task QueryCreatedTodosAsync_ReturnsCreatedTodoIndex()
    {
        var service = Substitute.For<ITriageService>();
        var query = new TriageCreatedTodoQueryResult
        {
            Items =
            [
                new TriageCreatedTodoDetail
                {
                    TodoId = "BUG-TRIAGE-001",
                    CreatedAtUtc = new DateTimeOffset(2026, 6, 25, 5, 3, 0, TimeSpan.Zero),
                    WorkspacePath = "F:\\GitHub\\McpServer",
                    GroupId = "triage-group-001",
                    RunId = "triage-run-001",
                    GroupStatus = "completed",
                    RunStatus = "completed",
                },
            ],
            TotalCount = 1,
        };
        service.QueryCreatedTodosAsync("F:\\GitHub\\McpServer", Arg.Any<CancellationToken>()).Returns(query);

        var action = await new TriageController(service).QueryCreatedTodosAsync(
            "F:\\GitHub\\McpServer",
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(query, ok.Value);
        await service.Received(1).QueryCreatedTodosAsync("F:\\GitHub\\McpServer", Arg.Any<CancellationToken>());
    }

    /// <summary>TEST-TRIAGE-001: run detail endpoint returns not-found envelopes for missing runs.</summary>
    [Fact]
    public async Task GetRunAsync_WhenMissing_ReturnsNotFound()
    {
        var service = Substitute.For<ITriageService>();
        service.GetRunAsync("triage-run-missing", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TriageResearchRunDetail>(new KeyNotFoundException("missing run")));

        var action = await new TriageController(service).GetRunAsync("triage-run-missing", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(action.Result);
        Assert.Contains("missing run", notFound.Value!.ToString(), StringComparison.Ordinal);
    }
}
