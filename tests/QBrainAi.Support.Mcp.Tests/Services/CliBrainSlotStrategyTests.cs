using System.Diagnostics;
using System.Text;
using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.CliBrainSlotStrategyTestsSupport;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-QBCLI-001 (historical): QuadBrain CLI provider kind, persistent Grok/Codex session args, and slot session
/// reuse. The class runs in <see cref="ProcessEnvironmentCollection"/> because every test that reaches
/// <c>CliBrainSlotChatClient.CompleteAsync</c> or its temp-folder lookup writes the process environment through
/// <c>WithIsolatedQuadbrainTemp</c>; those tests are integration tests (TR-MCP-USECASE-018 AC-006,
/// TR-MCP-USECASE-019 AC-008; plan sections 19.3 and 20).
/// </summary>
/// <remarks>
/// The TEST-MCP-QBCLI-001 citations in this file are historical: no such record is in the requirements store
/// (hostile validation finding R6-F2), so they make no active coverage claim. The active claims are the
/// TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008 classification of the integration tests.
/// <para>2026-11-07, TEST-MCP-QBCLI-001; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 13 Category=Integration cases and the helpers only they use (AssertGrokRunsIsolatedAsync, GrokSlot, CodexSlot, WithIsolatedQuadbrainTemp, ArgumentAfter, NoopProcessEnvironmentService, RecordingSpawner and FakeProcess) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (StaticOptionsMonitor) moved once into Services/CliBrainSlotStrategyTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</para>
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class CliBrainSlotStrategyTests
{
    [Theory]
    [InlineData("Cli")]
    [InlineData("cli")]
    public void NormalizeProviderKind_AcceptsCli(string kind)
    {
        Assert.Equal("Cli", BrainSlotValidation.NormalizeProviderKind(kind));
    }

    [Theory]
    [InlineData("cli://grok-cli", "grok-cli")]
    [InlineData("cli://grok-build", "grok-cli")]
    [InlineData("cli://codex-cli", "codex-cli")]
    public void TryParseCliEndpoint_KnownStrategies_Succeed(string endpoint, string expected)
    {
        Assert.True(CliBrainSlotEndpoint.TryParse(endpoint, out var strategy));
        Assert.Equal(expected, strategy);
    }

    [Theory]
    [InlineData("http://127.0.0.1:8311/v1")]
    [InlineData("cli://unknown")]
    [InlineData("")]
    public void TryParseCliEndpoint_Unknown_Fails(string endpoint)
    {
        Assert.False(CliBrainSlotEndpoint.TryParse(endpoint, out _));
    }

    [Fact]
    public void ValidateEndpoint_CliKnownStrategy_DoesNotRequireHttpHostAllowlist()
    {
        var options = new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions
        {
            AllowLoopbackEndpoints = false,
            AllowedEndpointHosts = [],
        });

        var exception = Record.Exception(() =>
            BrainSlotValidation.ValidateEndpoint("Cli", "cli://grok-cli", options));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateEndpoint_CliUnknownStrategy_Throws()
    {
        var options = new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions());

        var ex = Assert.Throws<BrainSlotValidationException>(() =>
            BrainSlotValidation.ValidateEndpoint("Cli", "cli://copilot-cli", options));

        Assert.Equal(BrainSlotReasonCodes.EndpointNotAllowed, ex.Reason);
    }

    [Fact]
    public void BuildPersistentGrokArgumentList_FirstTurn_UsesSessionIdAndXhigh()
    {
        var sessionId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee").ToString();
        var args = GrokCliAgentExecutionStrategy.BuildPersistentGrokArgumentList(
            workingDirectory: @"Q:\__mcp_unit_test__\QBrainAi",
            promptFilePath: @"C:\temp\grok-prompt.txt",
            model: "grok-4.6",
            sessionId: sessionId,
            resume: false);

        Assert.Contains("--effort", args);
        Assert.Equal("xhigh", args[args.ToList().IndexOf("--effort") + 1]);
        Assert.Contains("--reasoning-effort", args);
        Assert.Equal("xhigh", args[args.ToList().IndexOf("--reasoning-effort") + 1]);
        Assert.Contains("--model", args);
        Assert.Contains("grok-4.6", args);
        Assert.Contains("--session-id", args);
        Assert.Contains(sessionId, args);
        Assert.DoesNotContain("--resume", args);
        Assert.Contains("--always-approve", args);
        Assert.DoesNotContain("plan", args);
        Assert.Contains("--no-plan", args);
        Assert.Contains("--max-turns", args);
        Assert.Equal("1", args[args.ToList().IndexOf("--max-turns") + 1]);
        Assert.Contains("--no-subagents", args);
        Assert.Contains("--verbatim", args);
    }

    [Fact]
    public void BuildPersistentGrokArgumentList_Resume_UsesResumeFlag()
    {
        var sessionId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee").ToString();
        var args = GrokCliAgentExecutionStrategy.BuildPersistentGrokArgumentList(
            workingDirectory: @"Q:\__mcp_unit_test__\QBrainAi",
            promptFilePath: @"C:\temp\grok-prompt.txt",
            model: "grok-4.6",
            sessionId: sessionId,
            resume: true);

        Assert.Contains("--resume", args);
        Assert.Contains(sessionId, args);
        Assert.DoesNotContain("--session-id", args);
    }

    [Fact]
    public void BuildPersistentCodexArgumentList_FirstTurn_UsesExecStdinAndXhigh()
    {
        var args = CodexCliAgentExecutionStrategy.BuildPersistentCodexArgumentList(
            workingDirectory: @"Q:\__mcp_unit_test__\QBrainAi",
            outputPath: @"C:\temp\codex-out.txt",
            model: "gpt-5.6-sol",
            sessionId: null);

        Assert.Equal("exec", args[0]);
        Assert.DoesNotContain("resume", args);
        Assert.Contains("--model", args);
        Assert.Contains("gpt-5.6-sol", args);
        Assert.Contains("model_reasoning_effort=\"xhigh\"", args);
        Assert.Contains("-", args);
        Assert.Contains("--json", args);
    }

    [Fact]
    public void BuildPersistentCodexArgumentList_Resume_PlacesExecOptionsBeforeResume()
    {
        var args = CodexCliAgentExecutionStrategy.BuildPersistentCodexArgumentList(
            workingDirectory: @"Q:\__mcp_unit_test__\QBrainAi",
            outputPath: @"C:\temp\codex-out.txt",
            model: "gpt-5.6-sol",
            sessionId: "thread-123");

        Assert.Equal("exec", args[0]);
        Assert.NotEqual("resume", args[1]);
        var resumeIndex = args.ToList().IndexOf("resume");
        Assert.True(resumeIndex > 1, "codex exec OPTIONS must precede the resume subcommand");
        Assert.Equal("thread-123", args[resumeIndex + 1]);
        Assert.Equal("-", args[^1]);
        Assert.True(args.ToList().IndexOf("--json") < resumeIndex);
        Assert.True(args.ToList().IndexOf("-o") < resumeIndex);
    }

    [Fact]
    public void ExtractCodexSessionId_FromJsonlThreadStarted_ReturnsId()
    {
        var jsonl = """
            {"type":"thread.started","thread_id":"thr_abc"}
            {"type":"item.completed"}
            """;

        Assert.Equal("thr_abc", CliBrainSlotChatClient.ExtractCodexSessionId(jsonl));
    }

    [Fact]
    public void ExtractCodexAssistantText_FromItemCompletedAgentMessage_ReturnsText()
    {
        var jsonl = """
            {"type":"thread.started","thread_id":"thr_logic"}
            {"type":"item.completed","item":{"id":"item_1","type":"command_execution","command":"ls"}}
            {"type":"item.completed","item":{"id":"item_2","type":"agent_message","text":"Logic analysis of the voice chat."}}
            """;

        Assert.Equal(
            "Logic analysis of the voice chat.",
            CliBrainSlotChatClient.ExtractCodexAssistantText(jsonl));
    }

    [Fact]
    public void ExtractCodexAssistantText_FromEventMsg_ReturnsMessage()
    {
        var jsonl = """
            {"type":"event_msg","payload":{"type":"agent_message","message":"working on it"}}
            """;

        Assert.Equal("working on it", CliBrainSlotChatClient.ExtractCodexAssistantText(jsonl));
    }

    [Theory]
    [InlineData("cli:interactive", true)]
    [InlineData("cli:session", true)]
    [InlineData("vault:future", false)]
    public void CredentialResolver_SupportsCliScheme(string reference, bool expected)
    {
        var resolver = new BrainSlotCredentialResolver(new ConfigurationBuilder().Build());
        Assert.Equal(expected, resolver.IsSupportedReference(reference));
    }

    [Fact]
    public async Task CredentialResolver_CliInteractive_ReturnsPlaceholder()
    {
        var resolver = new BrainSlotCredentialResolver(new ConfigurationBuilder().Build());
        var value = await resolver.ResolveAsync("cli:interactive", TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("cli", value);
    }
}
