using McpServer.Client.Models;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-REPL-TRIAGE-001: REPL tests for canonical client.triage passthrough
/// shape and deprecated workflow.triage typed wrapper dispatch.
/// </summary>
public sealed class TriageWorkflowTests
{
    /// <summary>Enumerates every typed workflow triage method exposed to REPL callers.</summary>
    /// <returns>Method names and representative parameters.</returns>
    public static IEnumerable<object[]> TriageRouteCases()
    {
        yield return [TriageCommandShapes.ReportMethod, new Dictionary<string, object?>
        {
            ["title"] = "Incidental MCP bug",
            ["summary"] = "The wrapper hid a server error.",
            ["component"] = "mcpserver-codex-plugin",
            ["affectedPaths"] = new[] { "F:\\GitHub\\mcpserver-codex-plugin\\lib\\repl-invoke.sh" },
        }];
        yield return [TriageCommandShapes.GetReportMethod, new Dictionary<string, object?> { ["reportId"] = "triage-report-001" }];
        yield return [TriageCommandShapes.QueryGroupsMethod, new Dictionary<string, object?> { ["status"] = "failed" }];
        yield return [TriageCommandShapes.GetDashboardMethod, new Dictionary<string, object?> { ["workspacePath"] = "F:\\GitHub\\McpServer" }];
        yield return [TriageCommandShapes.GetGroupMethod, new Dictionary<string, object?> { ["groupId"] = "triage-group-001" }];
        yield return [TriageCommandShapes.QueryRunsMethod, new Dictionary<string, object?> { ["status"] = "failed", ["groupId"] = "triage-group-001" }];
        yield return [TriageCommandShapes.GetRunMethod, new Dictionary<string, object?> { ["runId"] = "triage-run-001" }];
        yield return [TriageCommandShapes.QueryCreatedTodosMethod, new Dictionary<string, object?> { ["workspacePath"] = "F:\\GitHub\\McpServer" }];
        yield return [TriageCommandShapes.FlushGroupMethod, new Dictionary<string, object?> { ["groupId"] = "triage-group-001" }];
        yield return [TriageCommandShapes.RetryGroupMethod, new Dictionary<string, object?> { ["groupId"] = "triage-group-001", ["force"] = true }];
        yield return [TriageCommandShapes.CreateGroupMethod, new Dictionary<string, object?>
        {
            ["reportIds"] = new[] { "triage-report-001" },
            ["title"] = "Manual group",
        }];
        yield return [TriageCommandShapes.ConsolidateIntoGroupMethod, new Dictionary<string, object?>
        {
            ["targetGroupId"] = "triage-group-target",
            ["groupIds"] = new[] { "triage-group-source" },
        }];
        yield return [TriageCommandShapes.MergeGroupsMethod, new Dictionary<string, object?>
        {
            ["targetGroupId"] = "triage-group-target",
            ["groupIds"] = new[] { "triage-group-source" },
        }];
    }

    /// <summary>TEST-MCP-REPL-TRIAGE-001: the route table covers the complete triage wrapper surface.</summary>
    [Fact]
    public void TriageRouteCases_CoverAllCommandShapes()
    {
        Assert.Equal(13, TriageRouteCases().Count());
    }

    /// <summary>
    /// TEST-MCP-REPL-TRIAGE-001: workflow.triage.* routes through ITriageWorkflow and
    /// returns deprecated metadata consistent with the other workflow namespaces.
    /// </summary>
    /// <param name="method">The workflow triage method.</param>
    /// <param name="parameters">Representative YAML-bound parameters.</param>
    [Theory]
    [MemberData(nameof(TriageRouteCases))]
    public async Task Dispatcher_TriageWorkflowMethod_RoutesToRegisteredWorkflow(
        string method,
        Dictionary<string, object?> parameters)
    {
        var workflow = Substitute.For<ITriageWorkflow>();
        ConfigureWorkflow(workflow);
        var sut = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            triageWorkflow: workflow);

        var response = await sut.DispatchAsync(BuildRequest(method, parameters), CancellationToken.None);

        Assert.Equal("result", response.Type);
        var payload = Assert.IsType<ResultPayload>(response.Payload);
        Assert.True(payload.Deprecated);
        await AssertReceivedAsync(workflow, method);
    }

    /// <summary>
    /// TEST-MCP-REPL-TRIAGE-001: YAML validation rejects malformed triage reports before dispatch.
    /// </summary>
    [Fact]
    public async Task Dispatcher_TriageReportMissingSummary_ReturnsSchemaValidationError()
    {
        var workflow = Substitute.For<ITriageWorkflow>();
        var sut = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            triageWorkflow: workflow);

        var response = await sut.DispatchAsync(BuildRequest(
            TriageCommandShapes.ReportMethod,
            new Dictionary<string, object?> { ["title"] = "Missing summary" }),
            CancellationToken.None);

        Assert.Equal("error", response.Type);
        var payload = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("schema_validation_failed", payload.Code);
        await workflow.DidNotReceiveWithAnyArgs().ReportAsync(default!, default);
    }

    /// <summary>
    /// TEST-MCP-REPL-TRIAGE-001: canonical client.triage.* remains available through generic passthrough.
    /// </summary>
    [Fact]
    public async Task Dispatcher_ClientTriageSubmitReport_UsesGenericPassthrough()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        passthrough.InvokeAsync(
                "triage",
                "SubmitReportAsync",
                Arg.Any<Dictionary<string, object?>>(),
                Arg.Any<CancellationToken>())
            .Returns(new TriageReportSubmitResult
            {
                Success = true,
                ReportId = "triage-report-001",
                GroupId = "triage-group-001",
                Status = "collecting",
                QuietDeadlineUtc = DateTimeOffset.Parse("2026-06-25T05:15:00Z"),
            });
        var sut = new ReplCommandDispatcher(passthrough);

        var response = await sut.DispatchAsync(BuildRequest(
            "client.triage.SubmitReportAsync",
            new Dictionary<string, object?>
            {
                ["request"] = new Dictionary<string, object?>
                {
                    ["title"] = "Incidental bug",
                    ["summary"] = "Found while working on another task.",
                },
            }),
            CancellationToken.None);

        Assert.Equal("result", response.Type);
        await passthrough.Received(1).InvokeAsync(
            "triage",
            "SubmitReportAsync",
            Arg.Any<Dictionary<string, object?>>(),
            Arg.Any<CancellationToken>());
    }

    private static YamlEnvelope BuildRequest(string method, Dictionary<string, object?> parameters) => new()
    {
        Type = "request",
        Payload = new RequestPayload
        {
            RequestId = $"req-20260625T050000Z-triage-{Guid.NewGuid().ToString("N")[..8]}",
            Method = method,
            Params = parameters,
        },
    };

    private static void ConfigureWorkflow(ITriageWorkflow workflow)
    {
        workflow.ReportAsync(Arg.Any<TriageReportRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageReportSubmitResult
            {
                Success = true,
                ReportId = "triage-report-001",
                GroupId = "triage-group-001",
                Status = "collecting",
                QuietDeadlineUtc = DateTimeOffset.Parse("2026-06-25T05:15:00Z"),
            });
        workflow.GetReportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TriageReportDetail { ReportId = "triage-report-001", GroupId = "triage-group-001", Status = "collecting" });
        workflow.QueryGroupsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new TriageGroupQueryResult { Items = [], TotalCount = 0 });
        workflow.GetDashboardAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new TriageDashboardResult());
        workflow.GetGroupAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TriageGroupDetail { GroupId = "triage-group-001", Status = "collecting", ReportCount = 1 });
        workflow.QueryRunsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new TriageRunQueryResult { Items = [], TotalCount = 0 });
        workflow.GetRunAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TriageResearchRunDetail { RunId = "triage-run-001", GroupId = "triage-group-001", Status = "failed" });
        workflow.QueryCreatedTodosAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new TriageCreatedTodoQueryResult { Items = [], TotalCount = 0 });
        workflow.FlushGroupAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TriageGroupDetail { GroupId = "triage-group-001", Status = "queued", ReportCount = 1 });
        workflow.RetryGroupAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new TriageGroupDetail { GroupId = "triage-group-001", Status = "collecting", ReportCount = 1 });
        workflow.CreateGroupFromSelectionAsync(Arg.Any<TriageGroupSelectionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageGroupEditResult
            {
                Group = new TriageGroupDetail { GroupId = "triage-group-new", Status = "collecting", ReportCount = 1 },
                MovedReportCount = 1,
            });
        workflow.ConsolidateIntoGroupAsync(Arg.Any<string>(), Arg.Any<TriageGroupSelectionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageGroupEditResult
            {
                Group = new TriageGroupDetail { GroupId = "triage-group-target", Status = "collecting", ReportCount = 2 },
                MovedReportCount = 1,
            });
        workflow.MergeGroupsAsync(Arg.Any<string>(), Arg.Any<TriageGroupSelectionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageGroupEditResult
            {
                Group = new TriageGroupDetail { GroupId = "triage-group-target", Status = "collecting", ReportCount = 2 },
                RemovedGroupIds = ["triage-group-source"],
                MovedReportCount = 1,
            });
    }

    private static async Task AssertReceivedAsync(ITriageWorkflow workflow, string method)
    {
        switch (method)
        {
            case TriageCommandShapes.ReportMethod:
                await workflow.Received(1).ReportAsync(Arg.Any<TriageReportRequest>(), Arg.Any<CancellationToken>());
                break;
            case TriageCommandShapes.GetReportMethod:
                await workflow.Received(1).GetReportAsync("triage-report-001", Arg.Any<CancellationToken>());
                break;
            case TriageCommandShapes.QueryGroupsMethod:
                await workflow.Received(1).QueryGroupsAsync("failed", null, Arg.Any<CancellationToken>());
                break;
            case TriageCommandShapes.GetDashboardMethod:
                await workflow.Received(1).GetDashboardAsync("F:\\GitHub\\McpServer", Arg.Any<CancellationToken>());
                break;
            case TriageCommandShapes.GetGroupMethod:
                await workflow.Received(1).GetGroupAsync("triage-group-001", Arg.Any<CancellationToken>());
                break;
            case TriageCommandShapes.QueryRunsMethod:
                await workflow.Received(1).QueryRunsAsync("failed", "triage-group-001", null, Arg.Any<CancellationToken>());
                break;
            case TriageCommandShapes.GetRunMethod:
                await workflow.Received(1).GetRunAsync("triage-run-001", Arg.Any<CancellationToken>());
                break;
            case TriageCommandShapes.QueryCreatedTodosMethod:
                await workflow.Received(1).QueryCreatedTodosAsync("F:\\GitHub\\McpServer", Arg.Any<CancellationToken>());
                break;
            case TriageCommandShapes.FlushGroupMethod:
                await workflow.Received(1).FlushGroupAsync("triage-group-001", Arg.Any<CancellationToken>());
                break;
            case TriageCommandShapes.RetryGroupMethod:
                await workflow.Received(1).RetryGroupAsync("triage-group-001", true, Arg.Any<CancellationToken>());
                break;
            case TriageCommandShapes.CreateGroupMethod:
                await workflow.Received(1).CreateGroupFromSelectionAsync(
                    Arg.Is<TriageGroupSelectionRequest>(request => HasReportId(request, "triage-report-001")),
                    Arg.Any<CancellationToken>());
                break;
            case TriageCommandShapes.ConsolidateIntoGroupMethod:
                await workflow.Received(1).ConsolidateIntoGroupAsync(
                    "triage-group-target",
                    Arg.Is<TriageGroupSelectionRequest>(request => HasGroupId(request, "triage-group-source")),
                    Arg.Any<CancellationToken>());
                break;
            case TriageCommandShapes.MergeGroupsMethod:
                await workflow.Received(1).MergeGroupsAsync(
                    "triage-group-target",
                    Arg.Is<TriageGroupSelectionRequest>(request => HasGroupId(request, "triage-group-source")),
                    Arg.Any<CancellationToken>());
                break;
        }
    }

    private static bool HasReportId(TriageGroupSelectionRequest? request, string reportId)
        => request?.ReportIds?.Contains(reportId) == true;

    private static bool HasGroupId(TriageGroupSelectionRequest? request, string groupId)
        => request?.GroupIds?.Contains(groupId) == true;
}
