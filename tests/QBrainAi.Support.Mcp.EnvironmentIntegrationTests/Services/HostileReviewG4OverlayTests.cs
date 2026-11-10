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

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

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
/// <item><term>2026-11-07</term><description>TEST-MCP-HOSTILEREVIEW-001; FR-MCP-HOSTILEREVIEW-001; FR-MCP-HOSTILEREVIEW-002; FR-MCP-HOSTILEREVIEW-003; FR-MCP-HOSTILEREVIEW-004; FR-MCP-HOSTILEREVIEW-005; FR-MCP-HOSTILEREVIEW-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/HostileReviewG4OverlayTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases and the helpers only they use (FindRepoRoot and RecordingHostileReviewHandler) moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class HostileReviewG4OverlayTestsBase in Services/HostileReviewG4OverlayTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</description></item>
/// </list>
/// </remarks>
public sealed class HostileReviewG4OverlayTests : HostileReviewG4OverlayTestsBase, IDisposable
{
    /// <summary>TEST-MCP-HOSTILEREVIEW-006: REST, REPL, Director, and plugin expose submit/status/get/query only.</summary>
    /// <remarks>Integration: reads the live plugin skill to check public surface parity (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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
        var workflow = new HostileReviewWorkflow(new HostileReviewClient(http, new QBrainAiClientOptions
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

        Assert.Contains(handler.Paths, path => path.Contains("/qbrainai/hostile-review/submit", StringComparison.Ordinal));
        Assert.Contains(handler.Paths, path => path.Contains("/qbrainai/hostile-review/hr-parity/status", StringComparison.Ordinal));
        Assert.Contains(handler.Paths, path => path.Equals("/qbrainai/hostile-review/hr-parity", StringComparison.Ordinal));
        Assert.Contains(handler.Paths, path => path.Contains("/qbrainai/hostile-review/query", StringComparison.Ordinal));
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

    /// <summary>
    /// TR-MCP-HOSTILEREVIEW-003: invalid worker options fail host startup validation. The failure
    /// message carries the whole exception text so a different startup failure is diagnosable.
    /// The default logging providers are cleared: on Windows the host adds the Event Log provider,
    /// and an account without Event Log write access (TEST-NODE-A, the service account of a test node) failed startup with
    /// "Cannot open log for source '.NET Runtime'" before options validation could report.
    /// </summary>
    /// <remarks>
    /// The default host builder imports the whole process environment, so this is an integration test
    /// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008; the sweep of plan section 19.3). Its assertion depends on
    /// no environment variable: the in-memory source, added after the environment, sets the five worker keys, and the
    /// renewal interval equal to the lease duration fails validation whatever else the environment holds. It writes no
    /// variable, so it stays outside <c>ProcessEnvironmentCollection</c>.
    /// 2026-11-05, TR-MCP-HOSTILEREVIEW-003: the summary describes the account as a test node's service account instead
    /// of naming a lab account, because the twelfth holistic read before hostile validation round 13 (finding g1) found
    /// the lab name in this comment, against the plan 27.7 scrub of this pull request; the case is unchanged.
    /// </remarks>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task HostileReviewWorker_InvalidOptions_FailStartup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
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
        var text = ex.ToString();
        if (!text.Contains("HostileReviewWorker", StringComparison.Ordinal))
            Assert.Fail("Startup failed without naming HostileReviewWorker: " + text);
    }

    private static string FindRepoRoot() => QBrainAi.Tests.Shared.TestRepositoryRoot.Resolve();

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
