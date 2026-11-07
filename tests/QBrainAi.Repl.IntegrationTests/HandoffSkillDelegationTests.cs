using System.Net.Http;
using System.Text.RegularExpressions;
using QBrainAi.Client;
using QBrainAi.Repl.Core;
using NSubstitute;
using QBrainAi.Tests.Shared;
using QBrainAi.TestSupport.Handoff;
using Xunit;

namespace QBrainAi.Repl.IntegrationTests;

/// <summary>
/// TEST-HANDOFF-006: plugin skill methods must dispatch through the supported REPL workflow
/// and HandoffClient HTTP surface that the server maps onto IHandoffIngestionService. The skill is read from the
/// repository checkout through the shared <see cref="TestRepositoryRoot"/>, so this is an integration test on both
/// hosts (TR-MCP-USECASE-018 AC-006, hostile validation round 2 finding R2-F1).
/// </summary>
/// <remarks>2026-11-07, TEST-HANDOFF-006; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of HandoffSkillDelegationTests, split from its unit case in QBrainAi.Repl.Core.Tests into the integration-owned QBrainAi.Repl.IntegrationTests so the unit-owned project holds only unit cases (slice S3, hostile validation finding R14-F4; operator 2026-11-06: "Per-owner projects (Recommended)"). The case and the helpers only it uses (ArgsFor, Request, FindRepoRoot) moved unchanged; the recording HTTP handler both halves use is the one linked RecordingHandler (tests/TestSupport.Shared/HandoffRecordingHandler.cs), not a copy; only the namespace changed.</remarks>
public sealed class HandoffSkillDelegationTests
{
    /// <summary>
    /// TEST-HANDOFF-006: parse workflow.handoff methods from the shared skill and invoke each
    /// through ReplCommandDispatcher plus a real HandoffWorkflow. The mock HTTP handler must
    /// receive ingest, get, and approve calls on /qbrainai/handoff/*.
    /// </summary>
    /// <remarks>Integration: reads the skill from the physical repository checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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
        var client = new HandoffClient(http, new QBrainAiClientOptions
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

        Assert.Contains(handler.Requests, item => item.Method == HttpMethod.Post && item.Path.Contains("/qbrainai/handoff/ingest", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, item => item.Method == HttpMethod.Get && item.Path.Contains("/qbrainai/handoff/runs/handoff-run-001", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, item => item.Method == HttpMethod.Post && item.Path.Contains("/qbrainai/handoff/runs/handoff-run-001/approve", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, item => item.Body is not null && item.Body.Contains("\"sourceKind\":\"Path\"", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, item => item.Body is not null && item.Body.Contains("\"approved\":true", StringComparison.Ordinal));
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

    private static string FindRepoRoot() => TestRepositoryRoot.Resolve();
}
