using System.Net;
using System.Text.Json;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Regression coverage for the production REPL memory workflow.
/// </summary>
public sealed class MemoryWorkflowTests
{
    private static readonly McpServerClientOptions Options = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
    };

    /// <summary>Verifies that list filters are forwarded to the typed memory client.</summary>
    [Fact]
    public async Task ListAsync_ForwardsFiltersToMemoryClient()
    {
        var handler = new JsonHandler("""{"items":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var sut = new MemoryWorkflow(new MemoryClient(http, Options));

        var result = await sut.ListAsync(MemoryScope.Global, "AGENT", "PowerShell", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(0, result.TotalCount);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.EndsWith("/mcpserver/memory?scope=Global&category=AGENT&keyword=PowerShell", handler.LastRequest.RequestUri!.OriginalString, StringComparison.Ordinal);
    }

    /// <summary>Verifies that add requests are posted through the typed memory client.</summary>
    [Fact]
    public async Task AddAsync_PostsMemoryRequest()
    {
        var handler = new JsonHandler("""
            {"success":true,"memory":{"id":"MEMORY-AGENT-001","category":"AGENT","scope":"Workspace","workspacePath":"F:\\GitHub\\McpServer","text":"Use wrappers.","version":1,"createdAtUtc":"2026-06-08T07:00:00Z","updatedAtUtc":"2026-06-08T07:00:00Z","updatedBy":"Codex"},"failureKind":"None"}
            """);
        using var http = new HttpClient(handler);
        var sut = new MemoryWorkflow(new MemoryClient(http, Options));

        var result = await sut.AddAsync(new MemoryAddRequest
        {
            Id = "MEMORY-AGENT-001",
            Category = "agent",
            Scope = MemoryScope.Workspace,
            Text = "Use wrappers.",
            UpdatedBy = "Codex",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/mcpserver/memory", handler.LastRequest.RequestUri!.OriginalString, StringComparison.Ordinal);
        Assert.NotNull(handler.LastBody);
        using var document = JsonDocument.Parse(handler.LastBody!);
        Assert.Equal("MEMORY-AGENT-001", document.RootElement.GetProperty("id").GetString());
        Assert.Equal("Workspace", document.RootElement.GetProperty("scope").GetString());
    }

    /// <summary>
    /// TEST-MCP-MEMORY-004 / TEST-MCP-MEMORY-005 / triage-report-a6fb8ae08ce348799d0db61ae2e0734a:
    /// workflow.memory.recall must keep live-API <c>items</c> as plugin-facing <c>hits</c>.
    /// </summary>
    [Fact]
    public async Task RecallAsync_LiveItemsJson_PreservesHitsInPluginYaml()
    {
        var handler = new JsonHandler(
            """
            {"statusCode":200,"items":[{"id":"MEMORY-FACT-003","score":0.87,"title":"Legion fact","content":"Operator fact from Legion recall proof.","type":"fact","scope":"Workspace","matchKind":"hybrid"}],"failureKind":"None","rerankApplied":false,"rankingMode":"hybrid"}
            """);
        using var http = new HttpClient(handler);
        var workflow = new MemoryWorkflow(new MemoryClient(http, Options));
        var dispatcher = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            memoryWorkflow: workflow);
        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-memory-recall-hits",
                Method = MemoryCommandShapes.RecallMethod,
                Params = new Dictionary<string, object?>
                {
                    ["query"] = "MEMORY-FACT-003",
                },
            },
        };

        var response = await dispatcher.DispatchAsync(envelope, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal("result", response.Type);
        var payload = Assert.IsAssignableFrom<IResultPayload>(response.Payload);
        var recall = Assert.IsType<MemoryRecallResult>(payload.Result);
        Assert.Equal("MEMORY-FACT-003", Assert.Single(recall.Hits!).Id);
        Assert.Equal("MEMORY-FACT-003", Assert.Single(recall.Items!).Id);

        var yaml = new YamlSerializer().Serialize(response);
        Assert.Contains("hits:", yaml, StringComparison.Ordinal);
        Assert.Contains("MEMORY-FACT-003", yaml, StringComparison.Ordinal);
        Assert.Contains("Operator fact from Legion recall proof.", yaml, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-MEMORY-004 / TEST-MCP-MEMORY-005:
    /// workflow.memory.explore must keep live-API neighbor <c>items</c> as plugin-facing <c>hits</c>.
    /// </summary>
    [Fact]
    public async Task ExploreAsync_LiveItemsJson_PreservesHitsInPluginYaml()
    {
        var handler = new JsonHandler(
            """
            {"statusCode":200,"items":[{"id":"MEMORY-FACT-004","weight":0.8,"edgeType":"related","depth":1}],"failureKind":"None","seedId":"MEMORY-FACT-003","hebbianApplied":false}
            """);
        using var http = new HttpClient(handler);
        var workflow = new MemoryWorkflow(new MemoryClient(http, Options));
        var dispatcher = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            memoryWorkflow: workflow);
        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-memory-explore-hits",
                Method = MemoryCommandShapes.ExploreMethod,
                Params = new Dictionary<string, object?>
                {
                    ["seedId"] = "MEMORY-FACT-003",
                },
            },
        };

        var response = await dispatcher.DispatchAsync(envelope, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal("result", response.Type);
        var payload = Assert.IsAssignableFrom<IResultPayload>(response.Payload);
        var explore = Assert.IsType<MemoryExploreResult>(payload.Result);
        Assert.Equal("MEMORY-FACT-004", Assert.Single(explore.Hits!).Id);
        Assert.Equal("MEMORY-FACT-004", Assert.Single(explore.Items!).Id);

        var yaml = new YamlSerializer().Serialize(response);
        Assert.Contains("hits:", yaml, StringComparison.Ordinal);
        Assert.Contains("MEMORY-FACT-004", yaml, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-MEMORY-004 / TEST-MCP-MEMORY-005:
    /// workflow.memory.consolidate must keep live-API <c>plan</c> as plugin-facing <c>items</c>/<c>hits</c>.
    /// </summary>
    [Fact]
    public async Task ConsolidateAsync_LivePlanJson_PreservesHitsInPluginYaml()
    {
        var handler = new JsonHandler(
            """
            {"statusCode":200,"plan":[{"candidateIds":["MEMORY-FACT-003","MEMORY-FACT-004"],"similarityScore":0.91,"survivorId":"MEMORY-FACT-003"}],"mergedAwayIds":[],"dryRunApplied":true,"failureKind":"None"}
            """);
        using var http = new HttpClient(handler);
        var workflow = new MemoryWorkflow(new MemoryClient(http, Options));
        var dispatcher = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            memoryWorkflow: workflow);
        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-memory-consolidate-hits",
                Method = MemoryCommandShapes.ConsolidateMethod,
                Params = new Dictionary<string, object?>
                {
                    ["dryRun"] = true,
                },
            },
        };

        var response = await dispatcher.DispatchAsync(envelope, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal("result", response.Type);
        var payload = Assert.IsAssignableFrom<IResultPayload>(response.Payload);
        var consolidate = Assert.IsType<MemoryConsolidateResult>(payload.Result);
        Assert.Equal("MEMORY-FACT-003", Assert.Single(consolidate.Hits!).SurvivorId);
        Assert.Equal("MEMORY-FACT-003", Assert.Single(consolidate.Items!).SurvivorId);

        var yaml = new YamlSerializer().Serialize(response);
        Assert.Contains("hits:", yaml, StringComparison.Ordinal);
        Assert.Contains("MEMORY-FACT-003", yaml, StringComparison.Ordinal);
    }

    private sealed class JsonHandler : HttpMessageHandler
    {
        private readonly string _json;

        public JsonHandler(string json)
        {
            _json = json;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json),
            };
        }
    }
}
