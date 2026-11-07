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
/// Overlay G4 HOSTILEREVIEW: named tests against shipped
/// <see cref="HostileReviewService"/>, <see cref="AgentPoolService"/>,
/// <see cref="HostileReviewWorkflow"/>, and public submit/status/get/query surfaces.
/// TEST-MCP-HOSTILEREVIEW-001 through 006.
/// </summary>
/// <remarks>
/// Requirement coverage named by requirement evidence: FR-MCP-HOSTILEREVIEW-001, FR-MCP-HOSTILEREVIEW-002,
/// FR-MCP-HOSTILEREVIEW-003, FR-MCP-HOSTILEREVIEW-004, FR-MCP-HOSTILEREVIEW-005 and FR-MCP-HOSTILEREVIEW-006 (every
/// criterion of each; they carry no criterion ids).
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>FR-MCP-HOSTILEREVIEW-001 through FR-MCP-HOSTILEREVIEW-006 (criteria without ids): cites the criteria whose evidence names this class, so the evidence no longer names the test (operator: "Must be fixed.").</description></item>
/// <item><term>2026-11-07</term><description>TEST-MCP-HOSTILEREVIEW-001; FR-MCP-HOSTILEREVIEW-001; FR-MCP-HOSTILEREVIEW-002; FR-MCP-HOSTILEREVIEW-003; FR-MCP-HOSTILEREVIEW-004; FR-MCP-HOSTILEREVIEW-005; FR-MCP-HOSTILEREVIEW-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (HostileReview_SurfaceParity_RestReplDirectorPlugin and HostileReviewWorker_InvalidOptions_FailStartup) and the helpers only they use (FindRepoRoot and RecordingHostileReviewHandler) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_workspace, _connection, _options, .ctor, Dispose, CreateService, CreateDb, SeedTodo and Request) moved once into the abstract base class HostileReviewG4OverlayTestsBase in Services/HostileReviewG4OverlayTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</description></item>
/// </list>
/// </remarks>
public sealed class HostileReviewG4OverlayTests : HostileReviewG4OverlayTestsBase, IDisposable
{
    /// <summary>TEST-MCP-HOSTILEREVIEW-001: SQLite round-trip of HostileReviewRequestEntity.</summary>
    [Fact]
    public async Task HostileReviewEntity_RoundTrip_SqlitePgSqlServer()
    {
        var sut = CreateService();
        var created = await sut.SubmitAsync(ValidRequest(), TestContext.Current.CancellationToken);
        Assert.True(created.Success, created.Error);
        using var verify = CreateDb();
        var stored = Assert.Single(verify.HostileReviewRequests);
        Assert.Equal(created.RequestId, stored.RequestId);
        Assert.Equal("Queued", stored.Status);
        Assert.DoesNotContain("RAW-BODY", stored.ScopeStatement, StringComparison.Ordinal);
        Assert.DoesNotContain(created.Diagnostics, item => item.Contains("artifact_missing", StringComparison.Ordinal));
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-001: valid submit creates a queued item.</summary>
    [Fact]
    public async Task HostileReviewSubmit_ValidRequest_CreatesQueueItem()
    {
        var result = await CreateService().SubmitAsync(ValidRequest(), TestContext.Current.CancellationToken);
        Assert.True(result.Success, result.Error);
        Assert.Equal(200, result.HttpStatus);
        Assert.Equal("Queued", result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.RequestId));
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-001: serialized payload over 1 MiB is rejected with no queue row.</summary>
    [Fact]
    public async Task HostileReviewSubmit_OversizedPayload_Rejected()
    {
        var request = ValidRequest();
        request.SerializedPayload = new string('x', 1_048_577);
        var result = await CreateService().SubmitAsync(request, TestContext.Current.CancellationToken);
        Assert.False(result.Success);
        Assert.Equal("payload_too_large", result.ErrorCode);
        using var verify = CreateDb();
        Assert.Empty(verify.HostileReviewRequests);
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-001: foreign workspace is 403 and creates no row.</summary>
    [Fact]
    public async Task HostileReviewSubmit_ForeignWorkspace_403()
    {
        var request = ValidRequest();
        request.WorkspacePath = Path.Combine(Path.GetTempPath(), "other-workspace");
        var result = await CreateService().SubmitAsync(request, TestContext.Current.CancellationToken);
        Assert.False(result.Success);
        Assert.Equal(403, result.HttpStatus);
        using var verify = CreateDb();
        Assert.Empty(verify.HostileReviewRequests);
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-002: missing artifact is a diagnostic, not a silent omit.</summary>
    [Fact]
    public async Task HostileReviewSubmit_MissingArtifact_ReturnsDiagnosticNotSilentOmit()
    {
        var request = ValidRequest();
        request.Links.Add(new HostileReviewArtifactLink { ArtifactType = "todo", ArtifactId = "todo-does-not-exist" });
        var result = await CreateService().SubmitAsync(request, TestContext.Current.CancellationToken);
        Assert.True(result.Success, result.Error);
        Assert.Contains(result.Diagnostics, item => item.Contains("artifact_missing", StringComparison.Ordinal));
        Assert.Contains(result.Links, link => link.ArtifactId == "todo-does-not-exist");
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-002: unauthorized links produce diagnostics from the TODO store.</summary>
    [Fact]
    public async Task HostileReviewSubmit_StaleOrUnauthorizedLink_Diagnostic()
    {
        using (var db = CreateDb())
        {
            SeedTodo(db, "MCP-OTHER-WORKSPACE-001", workspaceId: Path.Combine(Path.GetTempPath(), "other-ws"));
            db.SaveChanges();
        }

        var request = ValidRequest();
        request.Links.Add(new HostileReviewArtifactLink { ArtifactType = "todo", ArtifactId = "MCP-OTHER-WORKSPACE-001" });
        var result = await CreateService().SubmitAsync(request, TestContext.Current.CancellationToken);
        Assert.Contains(result.Diagnostics, item => item.Contains("artifact_unauthorized", StringComparison.Ordinal));
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-002: ambiguous TODO title matches produce diagnostics.</summary>
    [Fact]
    public async Task HostileReviewSubmit_AmbiguousLink_Diagnostic()
    {
        using (var db = CreateDb())
        {
            SeedTodo(db, "G4-AMB-A", title: "G4-AMBIGUOUS-TITLE");
            SeedTodo(db, "G4-AMB-B", title: "G4-AMBIGUOUS-TITLE");
            db.SaveChanges();
        }

        var request = ValidRequest();
        request.Links.Add(new HostileReviewArtifactLink { ArtifactType = "todo", ArtifactId = "G4-AMBIGUOUS-TITLE" });
        var result = await CreateService().SubmitAsync(request, TestContext.Current.CancellationToken);
        Assert.Contains(result.Diagnostics, item => item.Contains("artifact_ambiguous", StringComparison.Ordinal));
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-003: execution records model, effort, agent, template, run id.</summary>
    [Fact]
    public async Task HostileReviewExecution_RecordsModelEffortAgentTemplateRunId()
    {
        var sut = CreateService();
        var submitted = await sut.SubmitAsync(ValidRequest(), TestContext.Current.CancellationToken);
        var accepted = await ((IHostileReviewWorker)sut).AcceptReviewerOutputAsync(submitted.RequestId!, SampleOutput(), TestContext.Current.CancellationToken);
        var execution = Assert.Single(accepted.Executions);
        Assert.Equal("gpt-6-astra", execution.Model);
        Assert.Equal("xhigh", execution.Effort);
        Assert.Equal("Astra", execution.ReviewerAgent);
        Assert.Equal("hostile-review", execution.PromptTemplateId);
        Assert.Equal("hostile-review/v1", execution.PromptVersion);
        Assert.False(string.IsNullOrWhiteSpace(execution.ExecutionId));
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-003: token counts are omitted when the reviewer did not supply them.</summary>
    [Fact]
    public async Task HostileReviewExecution_OmitsFabricatedTokenCounts()
    {
        var sut = CreateService();
        var submitted = await sut.SubmitAsync(ValidRequest(), TestContext.Current.CancellationToken);
        var output = SampleOutput();
        output.InputTokens = null;
        output.OutputTokens = null;
        var accepted = await ((IHostileReviewWorker)sut).AcceptReviewerOutputAsync(submitted.RequestId!, output, TestContext.Current.CancellationToken);
        var execution = Assert.Single(accepted.Executions);
        Assert.Null(execution.InputTokens);
        Assert.Null(execution.OutputTokens);
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-004: findings taxonomy is complete.</summary>
    [Fact]
    public async Task HostileReviewGet_NormalizedFindings_TaxonomyComplete()
    {
        var sut = CreateService();
        var submitted = await sut.SubmitAsync(ValidRequest(), TestContext.Current.CancellationToken);
        var output = SampleOutput();
        output.Findings =
        [
            new() { Category = "defect", Severity = "high", Recommendation = "fix" },
            new() { Category = "risk", Severity = "medium", Recommendation = "mitigate" },
            new() { Category = "missing-tests", Severity = "medium", Recommendation = "add tests" },
            new() { Category = "unclear-requirements", Severity = "low", Recommendation = "clarify" },
            new() { Category = "insufficient-disclosure", Severity = "low", Recommendation = "disclose" },
            new() { Category = "out-of-scope", Severity = "low", Recommendation = "drop" },
            new() { Category = "uncertainty", Severity = "low", Recommendation = "ask" },
        ];
        await ((IHostileReviewWorker)sut).AcceptReviewerOutputAsync(submitted.RequestId!, output, TestContext.Current.CancellationToken);
        var got = await sut.GetAsync(submitted.RequestId!, TestContext.Current.CancellationToken);
        Assert.Equal(7, got.Findings.Count);
        Assert.Contains(got.Findings, item => item.Category == "defect");
        Assert.Contains(got.Findings, item => item.Category == "uncertainty");
        Assert.Equal("DISAGREE", got.Verdict);
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-004: request-quality scores are explicit fields.</summary>
    [Fact]
    public async Task HostileReview_RequestQuality_ScoresDisclosureScopeObjectiveConfidenceContext()
    {
        var sut = CreateService();
        var submitted = await sut.SubmitAsync(ValidRequest(), TestContext.Current.CancellationToken);
        var output = SampleOutput();
        output.RequestQuality = new HostileReviewRequestQuality
        {
            Disclosure = 0.8,
            ScopeClarity = 0.7,
            ObjectiveClarity = 0.6,
            Confidence = 0.9,
            EnoughContext = true,
        };
        var accepted = await ((IHostileReviewWorker)sut).AcceptReviewerOutputAsync(submitted.RequestId!, output, TestContext.Current.CancellationToken);
        Assert.NotNull(accepted.RequestQuality);
        Assert.Equal(0.8, accepted.RequestQuality!.Disclosure);
        Assert.Equal(0.7, accepted.RequestQuality.ScopeClarity);
        Assert.Equal(0.6, accepted.RequestQuality.ObjectiveClarity);
        Assert.Equal(0.9, accepted.RequestQuality.Confidence);
        Assert.True(accepted.RequestQuality.EnoughContext);
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-005: model and effort AND-filter.</summary>
    [Fact]
    public async Task HostileReviewQuery_ByModelAndEffort_ReturnsOnlyMatchingRuns()
    {
        var sut = CreateService();
        var first = await sut.SubmitAsync(ValidRequest("code", "agent-a"), TestContext.Current.CancellationToken);
        await ((IHostileReviewWorker)sut).AcceptReviewerOutputAsync(first.RequestId!, SampleOutput(), TestContext.Current.CancellationToken);
        var second = await sut.SubmitAsync(ValidRequest("docs", "agent-b"), TestContext.Current.CancellationToken);
        var other = SampleOutput();
        other.Model = "gpt-6-astra";
        other.Effort = "xhigh";
        await ((IHostileReviewWorker)sut).AcceptReviewerOutputAsync(second.RequestId!, other, TestContext.Current.CancellationToken);

        var matched = await sut.QueryAsync(new HostileReviewQueryRequest { Model = "gpt-6-astra", Effort = "xhigh" }, TestContext.Current.CancellationToken);
        Assert.Equal(2, matched.Count);
        var empty = await sut.QueryAsync(new HostileReviewQueryRequest { Model = "missing-model" }, TestContext.Current.CancellationToken);
        Assert.Empty(empty);
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-005: requester and target type AND-filter.</summary>
    [Fact]
    public async Task HostileReviewQuery_ByRequesterAndTargetType_AndFilters()
    {
        var sut = CreateService();
        await sut.SubmitAsync(ValidRequest("code", "agent-a"), TestContext.Current.CancellationToken);
        await sut.SubmitAsync(ValidRequest("docs", "agent-a"), TestContext.Current.CancellationToken);
        var matched = await sut.QueryAsync(new HostileReviewQueryRequest { RequestingAgent = "agent-a", TargetType = "docs" }, TestContext.Current.CancellationToken);
        Assert.Single(matched);
        Assert.Equal("docs", matched[0].TargetType);
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-005: no match is an empty list, not an error.</summary>
    [Fact]
    public async Task HostileReviewQuery_NoMatch_EmptyList()
    {
        var matched = await CreateService().QueryAsync(new HostileReviewQueryRequest { RequestingAgent = "nobody" }, TestContext.Current.CancellationToken);
        Assert.Empty(matched);
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-006: completing a review does not mutate product files.</summary>
    [Fact]
    public async Task HostileReview_Default_DoesNotMutateProductFiles()
    {
        var marker = Path.Combine(_workspace, "product.txt");
        await File.WriteAllTextAsync(marker, "unchanged", TestContext.Current.CancellationToken);
        var before = await File.ReadAllTextAsync(marker, TestContext.Current.CancellationToken);
        var sut = CreateService();
        var submitted = await sut.SubmitAsync(ValidRequest(), TestContext.Current.CancellationToken);
        await ((IHostileReviewWorker)sut).AcceptReviewerOutputAsync(submitted.RequestId!, SampleOutput(), TestContext.Current.CancellationToken);
        var after = await File.ReadAllTextAsync(marker, TestContext.Current.CancellationToken);
        Assert.Equal(before, after);
    }

    /// <summary>TR-MCP-HOSTILEREVIEW-002: HostileReview one-shot context is protected through AgentPoolService.</summary>
    [Fact]
    public async Task HostileReviewProtectedContext_NoRawPromptInPublishedSurface()
    {
        using var service = AgentPoolServiceTests.CreateService(out var voiceService);
        const string raw = "RAW-HOSTILE-REVIEW-PROMPT";
        VoiceTurnRequest? captured = null;
        voiceService.SubmitTurnAsync(Arg.Any<string>(), Arg.Any<VoiceTurnRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.Arg<VoiceTurnRequest>();
                return Task.FromResult<VoiceTurnResponse?>(new VoiceTurnResponse
                {
                    SessionId = ci.ArgAt<string>(0),
                    TurnId = "turn-g4",
                    Status = "completed",
                    AssistantDisplayText = "assistant output",
                    AssistantSpeakText = "assistant output",
                    ToolCalls = [],
                    LatencyMs = 12,
                    ModelRequested = "gpt-6-astra",
                    ModelResolved = "gpt-6-astra",
                });
            });

        var enqueue = await service.EnqueueOneShotAsync(new AgentPoolOneShotRequest
        {
            Context = AgentPoolOneShotContext.HostileReview,
            PromptText = raw,
            UseWorkspaceContext = true,
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(enqueue.Success, enqueue.Error);
        Assert.DoesNotContain(raw, enqueue.RenderedPrompt ?? string.Empty, StringComparison.Ordinal);
        Assert.StartsWith(OneShotSensitivePromptPolicy.RedactedPrefix, enqueue.RenderedPrompt, StringComparison.Ordinal);

        var completed = await AgentPoolServiceTests.WaitForJobStatusAsync(service, enqueue.JobId!, "completed");
        Assert.DoesNotContain(raw, completed.RenderedPrompt ?? string.Empty, StringComparison.Ordinal);
        Assert.StartsWith(OneShotSensitivePromptPolicy.RedactedPrefix, completed.RenderedPrompt, StringComparison.Ordinal);
        Assert.NotNull(captured);
        Assert.Equal(raw, captured!.UserTranscriptText);
        Assert.Null(service.PeekExecutionPrompt(enqueue.JobId!));
        var queue = await service.GetQueueItemsAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.All(queue, item => Assert.DoesNotContain(raw, item.RenderedPrompt ?? string.Empty, StringComparison.Ordinal));
    }

    private HostileReviewSubmitRequest ValidRequest(string targetType = "code", string agent = "agent-a")
        => new()
        {
            TargetType = targetType,
            Mode = "adversarial",
            ScopeStatement = "Review the shipped hostile-review queue.",
            RequestingAgent = agent,
            WorkspacePath = _workspace,
            Links = [new HostileReviewArtifactLink { ArtifactType = "todo", ArtifactId = "MCP-HOSTILEREVIEW-001" }],
        };

    private static HostileReviewerOutput SampleOutput()
        => new()
        {
            Verdict = "DISAGREE",
            Findings = [new HostileReviewFinding { Category = "defect", Severity = "high", Recommendation = "add tests" }],
            RequestQuality = new HostileReviewRequestQuality
            {
                Disclosure = 0.5,
                ScopeClarity = 0.5,
                ObjectiveClarity = 0.5,
                Confidence = 0.5,
                EnoughContext = true,
            },
        };
}
