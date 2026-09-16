using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using McpServer.Client;
using McpServer.Cqrs.Mvvm;
using McpServer.Repl.Core;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Overlay G4 HOSTILEREVIEW: named tests against shipped
/// <see cref="HostileReviewService"/>, <see cref="AgentPoolService"/>,
/// <see cref="HostileReviewWorkflow"/>, and public submit/status/get/query surfaces.
/// TEST-MCP-HOSTILEREVIEW-001 through 006.
/// </summary>
public sealed class HostileReviewG4OverlayTests : IDisposable
{
    private readonly string _workspace;
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Isolated in-memory SQLite workspace with a seeded TODO row.</summary>
    public HostileReviewG4OverlayTests()
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

    /// <summary>TEST-MCP-HOSTILEREVIEW-006: REST, REPL, Director, and plugin expose submit/status/get/query only.</summary>
    [Fact]
    public async Task HostileReview_SurfaceParity_RestReplDirectorPlugin()
    {
        var rest = typeof(HostileReviewController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToArray();
        Assert.Contains("SubmitAsync", rest);
        Assert.Contains("StatusAsync", rest);
        Assert.Contains("GetAsync", rest);
        Assert.Contains("QueryAsync", rest);
        Assert.DoesNotContain(rest, name => name.Contains("Cancel", StringComparison.OrdinalIgnoreCase) || name.Contains("Repair", StringComparison.OrdinalIgnoreCase) || name.Contains("Apply", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(IHostileReviewService).GetMethods(), method => method.Name == "AcceptReviewerOutputAsync");
        Assert.Contains(typeof(IHostileReviewWorker).GetMethods(), method => method.Name == "AcceptReviewerOutputAsync");

        Assert.Equal("workflow.hostileReview.submit", HostileReviewCommandShapes.SubmitMethod);
        Assert.Equal("workflow.hostileReview.status", HostileReviewCommandShapes.StatusMethod);
        Assert.Equal("workflow.hostileReview.get", HostileReviewCommandShapes.GetMethod);
        Assert.Equal("workflow.hostileReview.query", HostileReviewCommandShapes.QueryMethod);

        Assert.Equal("hostile-review-submit", HostileReviewDirectorCommands.Submit);
        Assert.Equal("hostile-review-status", HostileReviewDirectorCommands.Status);
        Assert.Equal("hostile-review-get", HostileReviewDirectorCommands.Get);
        Assert.Equal("hostile-review-query", HostileReviewDirectorCommands.Query);

        var toolNames = typeof(FwhMcpTools).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        Assert.Contains("hostile_review_submit", toolNames);
        Assert.Contains("hostile_review_status", toolNames);
        Assert.Contains("hostile_review_get", toolNames);
        Assert.Contains("hostile_review_query", toolNames);
        Assert.DoesNotContain(toolNames, name => name is not null && name.StartsWith("hostile_review_", StringComparison.Ordinal) && name is not "hostile_review_submit" and not "hostile_review_status" and not "hostile_review_get" and not "hostile_review_query");

        var skill = File.ReadAllText(Path.Combine(FindRepoRoot(), "plugins", "core", "skills", "hostile-review", "SKILL.md"));
        Assert.Contains("workflow.hostileReview.submit", skill, StringComparison.Ordinal);
        Assert.Contains("workflow.hostileReview.status", skill, StringComparison.Ordinal);
        Assert.Contains("workflow.hostileReview.get", skill, StringComparison.Ordinal);
        Assert.Contains("workflow.hostileReview.query", skill, StringComparison.Ordinal);
        Assert.Contains("Queue-and-record", skill, StringComparison.Ordinal);
        Assert.DoesNotContain("workflow.hostileReview.repair", skill, StringComparison.Ordinal);
        Assert.DoesNotContain("workflow.hostileReview.apply", skill, StringComparison.Ordinal);

        var handler = new RecordingHostileReviewHandler();
        using var http = new HttpClient(handler);
        var workflow = new HostileReviewWorkflow(new HostileReviewClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
        }));
        var dispatcher = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            hostileReviewWorkflow: workflow);

        var submitEnvelope = await dispatcher.DispatchAsync(Request(HostileReviewCommandShapes.SubmitMethod, new Dictionary<string, object?>
        {
            ["targetType"] = "code",
            ["mode"] = "adversarial",
            ["scopeStatement"] = "parity",
            ["requestingAgent"] = "repl",
            ["workspacePath"] = _workspace,
        }), TestContext.Current.CancellationToken);
        Assert.Equal("result", submitEnvelope.Type);

        var statusEnvelope = await dispatcher.DispatchAsync(Request(HostileReviewCommandShapes.StatusMethod, new Dictionary<string, object?>
        {
            ["requestId"] = "hr-parity",
        }), TestContext.Current.CancellationToken);
        Assert.Equal("result", statusEnvelope.Type);

        var getEnvelope = await dispatcher.DispatchAsync(Request(HostileReviewCommandShapes.GetMethod, new Dictionary<string, object?>
        {
            ["requestId"] = "hr-parity",
        }), TestContext.Current.CancellationToken);
        Assert.Equal("result", getEnvelope.Type);

        var queryEnvelope = await dispatcher.DispatchAsync(Request(HostileReviewCommandShapes.QueryMethod, new Dictionary<string, object?>
        {
            ["model"] = "gpt-6-astra",
            ["effort"] = "xhigh",
        }), TestContext.Current.CancellationToken);
        Assert.Equal("result", queryEnvelope.Type);

        Assert.Contains(handler.Paths, path => path.Contains("/mcpserver/hostile-review/submit", StringComparison.Ordinal));
        Assert.Contains(handler.Paths, path => path.Contains("/mcpserver/hostile-review/hr-parity/status", StringComparison.Ordinal));
        Assert.Contains(handler.Paths, path => path.Equals("/mcpserver/hostile-review/hr-parity", StringComparison.Ordinal));
        Assert.Contains(handler.Paths, path => path.Contains("/mcpserver/hostile-review/query", StringComparison.Ordinal));
        Assert.DoesNotContain(handler.Paths, path => path.Contains("repair", StringComparison.OrdinalIgnoreCase) || path.Contains("apply", StringComparison.OrdinalIgnoreCase));

        var executor = new HostileReviewDirectorExecutor(CreateService());
        var submitCommand = new HostileReviewSubmitDirectorCommand(executor)
        {
            TargetType = "code",
            Mode = "adversarial",
            ScopeStatement = "director parity",
            RequestingAgent = "director",
            WorkspacePath = _workspace,
        };
        await submitCommand.PrimaryCommand.ExecuteAsync(TestContext.Current.CancellationToken);
        var directorSubmit = Assert.IsType<HostileReviewResult>(submitCommand.Result);
        Assert.True(directorSubmit.Success, directorSubmit.Error);
        Assert.Equal("Queued", directorSubmit.Status);

        var statusCommand = new HostileReviewStatusDirectorCommand(executor) { RequestId = directorSubmit.RequestId };
        await statusCommand.PrimaryCommand.ExecuteAsync(TestContext.Current.CancellationToken);
        var directorStatus = Assert.IsType<HostileReviewResult>(statusCommand.Result);
        Assert.Equal(directorSubmit.RequestId, directorStatus.RequestId);

        var getCommand = new HostileReviewGetDirectorCommand(executor) { RequestId = directorSubmit.RequestId };
        await getCommand.PrimaryCommand.ExecuteAsync(TestContext.Current.CancellationToken);
        Assert.Equal(directorSubmit.RequestId, Assert.IsType<HostileReviewResult>(getCommand.Result).RequestId);

        var queryCommand = new HostileReviewQueryDirectorCommand(executor)
        {
            RequestingAgent = "director",
            TargetType = "code",
        };
        await queryCommand.PrimaryCommand.ExecuteAsync(TestContext.Current.CancellationToken);
        var directorQuery = Assert.IsAssignableFrom<IReadOnlyList<HostileReviewResult>>(queryCommand.Result);
        Assert.Contains(directorQuery, item => item.RequestId == directorSubmit.RequestId);
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

    /// <summary>TR-MCP-HOSTILEREVIEW-003: invalid worker options fail host startup validation.</summary>
    [Fact]
    public async Task HostileReviewWorker_InvalidOptions_FailStartup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mcp:HostileReviewWorker:LeaseDuration"] = "00:01:00",
            ["Mcp:HostileReviewWorker:RenewalInterval"] = "00:01:00",
            ["Mcp:HostileReviewWorker:AgentName"] = "Astra",
            ["Mcp:HostileReviewWorker:RequiredModel"] = "gpt-6-astra",
            ["Mcp:HostileReviewWorker:RequiredEffort"] = "xhigh",
        });
        builder.Services.AddHostileReviewServices();
        using var host = builder.Build();
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => host.StartAsync(TestContext.Current.CancellationToken));
        Assert.Contains("HostileReviewWorker", ex.ToString(), StringComparison.Ordinal);
    }

    private HostileReviewService CreateService()
        => new(CreateDb(), new HostileReviewWorkerOptions());

    private McpDbContext CreateDb()
        => new(_options, new WorkspaceContext { WorkspacePath = _workspace });

    private void SeedTodo(McpDbContext db, string id, string? title = null, string? workspaceId = null)
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

    private static IYamlEnvelope Request(string method, Dictionary<string, object?> args)
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

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "docs", "Project")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }

    /// <summary>Records hostile-review HTTP paths for shipped workflow proof.</summary>
    private sealed class RecordingHostileReviewHandler : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            Paths.Add(path);
            var body = path.Contains("/query", StringComparison.Ordinal)
                ? """[{"success":true,"httpStatus":200,"requestId":"hr-parity","status":"Queued"}]"""
                : """{"success":true,"httpStatus":200,"requestId":"hr-parity","status":"Queued"}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
