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

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// Overlay G5 hygiene: named tests against shipped
/// <see cref="WorkspaceValidationService"/>. TEST-MCP-HYGIENE-001 through 005.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-05</term><description>TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): CreateService hands every service the class's settable clock instead of TimeProvider.System, the two stale-turn cases seed from it, and the D3 binding was added (hostile validation round 12 finding R12-F2, S3 and S4); the assertions are unchanged.</description></item>
/// <item><term>2026-11-06</term><description>FR-MCP-HYGIENE-001, FR-MCP-HYGIENE-002, FR-MCP-HYGIENE-003, FR-MCP-HYGIENE-004 and FR-MCP-HYGIENE-005 (every criterion of each; they carry no criterion ids): cites the criteria whose evidence names this class, so the evidence no longer names the test (operator: "Must be fixed.").</description></item>
/// <item><term>2026-11-07</term><description>TEST-MCP-HYGIENE-001; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/WorkspaceHygieneG5OverlayTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases and the helpers only they use moved unchanged; the state and helpers both halves use come from the abstract base class WorkspaceHygieneG5OverlayTestsBase in Services/WorkspaceHygieneG5OverlayTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project with AgeBoundaryContractTestsBase (DRY); only the namespace changed.</description></item>
/// </list>
/// </remarks>
public sealed class WorkspaceHygieneG5OverlayTests : WorkspaceHygieneG5OverlayTestsBase, IDisposable
{
    /// <summary>TEST-MCP-HYGIENE-005: REST, REPL, Director, plugin expose the same validation.</summary>
    /// <remarks>Integration: reads the live API, Director, REPL or service source to verify implementation parity (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task Parity_RestDirectorReplPlugin_SameRuleCodesAndCounts()
    {
        Assert.Contains("ValidateAsync", typeof(WorkspaceValidationController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Select(method => method.Name));
        Assert.Equal("workspace.validate", WorkspaceValidationCommandShapes.ValidateMethod);
        Assert.Equal("validate-workspace", WorkspaceValidationDirectorCommands.Validate);
        var toolNames = typeof(FwhMcpTools).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        Assert.Contains("workspace_validate", toolNames);
        var skill = File.ReadAllText(Path.Combine(FindRepoRoot(), "plugins", "core", "skills", "workspace-validation", "SKILL.md"));
        Assert.Contains("workspace.validate", skill, StringComparison.Ordinal);
        Assert.DoesNotContain("workflow.workspace.repair", skill, StringComparison.Ordinal);

        SeedTodo("TODO-HY-PARITY", done: true, doneSummary: null, remaining: null);
        var service = CreateService();
        var request = new WorkspaceValidationRequest();
        var expected = await service.ValidateAsync(request, TestContext.Current.CancellationToken);

        var rest = await new WorkspaceValidationController(service).ValidateAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(expected.TotalFindings, rest.TotalFindings);
        Assert.Equal(expected.ErrorCount, rest.ErrorCount);
        Assert.Equal(expected.WarningCount, rest.WarningCount);
        AssertParity(expected, rest);

        var dispatcher = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            workspaceValidationWorkflow: CreateProductionWorkflow(service));
        var envelope = await dispatcher.DispatchAsync(new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-g5-hygiene",
                Method = WorkspaceValidationCommandShapes.ValidateMethod,
                Params = new Dictionary<string, object?>(),
            },
        }, TestContext.Current.CancellationToken);
        Assert.Equal("result", envelope.Type);
        var repl = Assert.IsType<QBrainAi.Client.Models.WorkspaceValidationResult>((envelope.Payload as ResultPayload)!.Result);
        AssertParity(expected, repl);

        var command = new WorkspaceValidationDirectorCommand(new WorkspaceValidationDirectorExecutor(service));
        await command.PrimaryCommand.ExecuteAsync(TestContext.Current.CancellationToken);
        var director = Assert.IsType<WorkspaceValidationResult>(command.Result);
        Assert.Equal(expected.TotalFindings, director.TotalFindings);
        Assert.Equal(expected.ErrorCount, director.ErrorCount);
        Assert.Equal(expected.WarningCount, director.WarningCount);
        AssertParity(expected, director);

        var pluginJson = await InvokePluginValidateAsync(service, TestContext.Current.CancellationToken);
        var plugin = System.Text.Json.JsonSerializer.Deserialize<WorkspaceValidationResult>(pluginJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(plugin);
        Assert.Equal(expected.TotalFindings, plugin!.TotalFindings);
        Assert.Equal(expected.ErrorCount, plugin.ErrorCount);
        Assert.Equal(expected.WarningCount, plugin.WarningCount);
        AssertParity(expected, plugin);

        var recorder = new RecordingValidationHandler();
        using var http = new HttpClient(recorder);
        var shippedWorkflow = new WorkspaceValidationWorkflow(new WorkspaceValidationClient(http, new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
        }));
        await shippedWorkflow.ValidateAsync(new Dictionary<string, object?>
        {
            ["ruleCodes"] = new[] { "todo_done_missing_summary" },
            ["offset"] = 0,
            ["limit"] = 1,
            ["staleTurnThresholdHours"] = 24d,
        }, TestContext.Current.CancellationToken);
        Assert.Contains("/qbrainai/workspace-validation/validate", recorder.Path, StringComparison.Ordinal);
        Assert.Contains("todo_done_missing_summary", recorder.Body, StringComparison.Ordinal);
        Assert.Contains("\"limit\":1", recorder.Body, StringComparison.Ordinal);
        Assert.Contains("staleTurnThresholdHours", recorder.Body, StringComparison.Ordinal);
    }

    /// <summary>TEST-MCP-HYGIENE-003: triage statuses come from the live triage domain, queries are batched.</summary>
    /// <remarks>Integration: reads the live API, Director, REPL or service source to verify implementation parity (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void ValidationService_UsesLiveTriageDomainAndBatchedQueries()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "QBrainAi.Services", "Services", "WorkspaceValidationService.cs"));
        Assert.Contains("TriageService.IsNonTerminalStatus", source, StringComparison.Ordinal);
        Assert.Contains("TriageService.IsFailedStatus", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NonTerminalTriageStatuses", source, StringComparison.Ordinal);
        Assert.Contains("QueryBatchSize", source, StringComparison.Ordinal);
        Assert.Contains("Take(QueryBatchSize)", source, StringComparison.Ordinal);
    }

    private static void AssertParity(WorkspaceValidationResult expected, WorkspaceValidationResult actual)
    {
        Assert.Equal(expected.StaleTurnThresholdHours, actual.StaleTurnThresholdHours);
        Assert.Equal(expected.TotalFindings, actual.TotalFindings);
        Assert.Equal(expected.ErrorCount, actual.ErrorCount);
        Assert.Equal(expected.WarningCount, actual.WarningCount);
        Assert.Equal(
            expected.Findings.Select(item => (item.RuleCode, item.RecordId, item.Severity)).OrderBy(item => item.RuleCode).ThenBy(item => item.RecordId),
            actual.Findings.Select(item => (item.RuleCode, item.RecordId, item.Severity)).OrderBy(item => item.RuleCode).ThenBy(item => item.RecordId));
    }

    private static void AssertParity(WorkspaceValidationResult expected, QBrainAi.Client.Models.WorkspaceValidationResult actual)
    {
        Assert.Equal(expected.StaleTurnThresholdHours, actual.StaleTurnThresholdHours);
        Assert.Equal(expected.TotalFindings, actual.TotalFindings);
        Assert.Equal(expected.ErrorCount, actual.ErrorCount);
        Assert.Equal(expected.WarningCount, actual.WarningCount);
        Assert.Equal(
            expected.Findings.Select(item => (item.RuleCode, item.RecordId, item.Severity)).OrderBy(item => item.RuleCode).ThenBy(item => item.RecordId),
            actual.Findings.Select(item => (item.RuleCode, item.RecordId, item.Severity)).OrderBy(item => item.RuleCode).ThenBy(item => item.RecordId));
    }

    private static WorkspaceValidationWorkflow CreateProductionWorkflow(IWorkspaceValidationService service)
    {
        var http = new HttpClient(new ServiceForwardingValidationHandler(service))
        {
            BaseAddress = new Uri("http://localhost:7147/"),
        };
        return new WorkspaceValidationWorkflow(new WorkspaceValidationClient(http, new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
        }));
    }

    private static string FindRepoRoot() => QBrainAi.Tests.Shared.TestRepositoryRoot.Resolve();

    private async Task<string> InvokePluginValidateAsync(IWorkspaceValidationService service, CancellationToken cancellationToken)
    {
        var tools = (FwhMcpTools)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(FwhMcpTools));
        var ingestion = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _workspace });
        var resolver = new TodoServiceResolver(Substitute.For<ITodoService>(), ingestion, Substitute.For<ITodoServiceFactory>());
        var accessor = new WorkspaceServiceAccessor(resolver, new Microsoft.AspNetCore.Http.HttpContextAccessor(), ingestion);
        typeof(FwhMcpTools).GetField("_workspaceValidationService", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tools, service);
        typeof(FwhMcpTools).GetField("_workspaceContext", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tools, new WorkspaceContext { WorkspacePath = _workspace });
        typeof(FwhMcpTools).GetField("_httpContextAccessor", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tools, new Microsoft.AspNetCore.Http.HttpContextAccessor());
        typeof(FwhMcpTools).GetField("_workspaceAccessor", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tools, accessor);
        typeof(FwhMcpTools).GetField("_db", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tools, CreateDb());
        return await tools.WorkspaceValidate(_workspace, cancellationToken: cancellationToken);
    }

    /// <summary>Records the shipped client HTTP body for workflow contract proof.</summary>
    private sealed class RecordingValidationHandler : HttpMessageHandler
    {
        public string Path { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath ?? string.Empty;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"success":true,"httpStatus":200,"findings":[],"totalFindings":0,"exitCode":0,"errorCount":0,"warningCount":0}""", Encoding.UTF8, "application/json"),
            };
        }
    }

    /// <summary>Production client HTTP path that forwards to the shipped validation service.</summary>
    private sealed class ServiceForwardingValidationHandler(IWorkspaceValidationService service) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = request.Content is null
                ? "{}"
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var incoming = System.Text.Json.JsonSerializer.Deserialize<QBrainAi.Client.Models.WorkspaceValidationRequest>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new QBrainAi.Client.Models.WorkspaceValidationRequest();
            var result = await service.ValidateAsync(
                    new WorkspaceValidationRequest
                    {
                        Authenticated = incoming.Authenticated,
                        StaleTurnThresholdHours = incoming.StaleTurnThresholdHours,
                        RuleCodes = incoming.RuleCodes,
                        Offset = incoming.Offset,
                        Limit = incoming.Limit,
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            var payload = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
        }
    }
}
