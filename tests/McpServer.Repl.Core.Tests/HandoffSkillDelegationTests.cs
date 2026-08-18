using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-HANDOFF-006: plugin skill methods must dispatch through the supported REPL workflow
/// and HandoffClient HTTP surface that the server maps onto IHandoffIngestionService.
/// </summary>
public sealed class HandoffSkillDelegationTests
{
    /// <summary>
    /// TEST-HANDOFF-006: parse workflow.handoff methods from the shared skill and invoke each
    /// through ReplCommandDispatcher plus a real HandoffWorkflow. The mock HTTP handler must
    /// receive ingest, get, and approve calls on /mcpserver/handoff/*.
    /// </summary>
    [Fact]
    public async Task SkillDocumentedMethods_DispatchThroughWorkflowToHandoffClient()
    {
        var skillPath = Path.Combine(FindRepoRoot(), "plugins", "core", "skills", "handoff", "SKILL.md");
        Assert.True(File.Exists(skillPath), skillPath);
        var skillText = await File.ReadAllTextAsync(skillPath, TestContext.Current.CancellationToken);
        var documented = Regex.Matches(skillText, @"method:\s*(workflow\.handoff\.[A-Za-z]+)")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.Contains(HandoffCommandShapes.IngestMethod, documented);
        Assert.Contains(HandoffCommandShapes.GetMethod, documented);
        Assert.Contains(HandoffCommandShapes.ApproveMethod, documented);

        var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var client = new HandoffClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
        });
        var dispatcher = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            handoffWorkflow: new HandoffWorkflow(client));

        foreach (var method in documented)
        {
            var envelope = await dispatcher.DispatchAsync(Request(method, ArgsFor(method)), TestContext.Current.CancellationToken);
            Assert.Equal("result", envelope.Type);
        }

        Assert.Contains(handler.Requests, item => item.Method == HttpMethod.Post && item.Path.Contains("/mcpserver/handoff/ingest", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, item => item.Method == HttpMethod.Get && item.Path.Contains("/mcpserver/handoff/runs/handoff-run-001", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, item => item.Method == HttpMethod.Post && item.Path.Contains("/mcpserver/handoff/runs/handoff-run-001/approve", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, item => item.Body is not null && item.Body.Contains("\"sourceKind\":\"Path\"", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, item => item.Body is not null && item.Body.Contains("\"approved\":true", StringComparison.Ordinal));
    }

    /// <summary>
    /// TEST-HANDOFF-006: invoke the real HandoffWorkflow methods that the plugin skill documents,
    /// not just parse the skill inventory.
    /// </summary>
    [Fact]
    public async Task PluginSkillWorkflow_InvokesTypedClientHandoffEndpoints()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var workflow = new HandoffWorkflow(new HandoffClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
        }));

        var ingested = await workflow.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "plugin-invoke",
            Mode = HandoffIngestionMode.DraftOnly,
        }, TestContext.Current.CancellationToken);
        var loaded = await workflow.GetAsync("handoff-run-001", TestContext.Current.CancellationToken);
        var approved = await workflow.ApproveAsync("handoff-run-001", new HandoffApprovalRequest { Approved = true }, TestContext.Current.CancellationToken);

        Assert.True(ingested.Success);
        Assert.True(loaded.Success);
        Assert.True(approved.Success);
        Assert.Contains(handler.Requests, item => item.Method == HttpMethod.Post && item.Path.Contains("/mcpserver/handoff/ingest", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, item => item.Method == HttpMethod.Get && item.Path.Contains("/mcpserver/handoff/runs/handoff-run-001", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, item => item.Method == HttpMethod.Post && item.Path.Contains("/mcpserver/handoff/runs/handoff-run-001/approve", StringComparison.Ordinal));
    }

    private static Dictionary<string, object?> ArgsFor(string method)
        => method switch
        {
            HandoffCommandShapes.IngestMethod => new Dictionary<string, object?>
            {
                ["sourceKind"] = "Path",
                ["path"] = "docs/handoffs/example.md",
                ["mode"] = "DraftOnly",
            },
            HandoffCommandShapes.GetMethod => new Dictionary<string, object?>
            {
                ["runId"] = "handoff-run-001",
            },
            HandoffCommandShapes.ApproveMethod => new Dictionary<string, object?>
            {
                ["runId"] = "handoff-run-001",
                ["approved"] = true,
                ["reviewer"] = "operator",
            },
            _ => throw new InvalidOperationException($"Skill documented an unrouted method '{method}'."),
        };

    private static IYamlEnvelope Request(string method, Dictionary<string, object?> args)
        => new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-20260816T222400Z-handoff-skill",
                Method = method,
                Params = args,
            },
        };

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing McpServer.sln.");
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path, string? Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Requests.Add((request.Method, request.RequestUri!.AbsolutePath, body));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"success":true,"created":false,"replayed":false,"requiresReview":false,"diagnostics":[]}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }
}
