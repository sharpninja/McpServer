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
    /// <summary>
    /// No requirement id applies: BrainSlotValidation.NormalizeProviderKind returns the canonical provider kind Cli for
    /// the inputs Cli and cli, a behavior no record in the requirements store describes. Data: the inline values Cli
    /// and cli.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for provider kind, brain slot and cli:// (uncited-matches.txt, test 7 of 23); no criterion names a Cli provider kind, and TR-MCP-LLMSTRATEGY-001 and TR-MCP-QUAD-001 have no criteria. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs both rows when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Theory]
    [Trait("Quarantine", "NoMatchingCriterion")]
    [InlineData("Cli")]
    [InlineData("cli")]
    public void NormalizeProviderKind_AcceptsCli(string kind)
    {
        Assert.Equal("Cli", BrainSlotValidation.NormalizeProviderKind(kind));
    }

    /// <summary>
    /// No requirement id applies: CliBrainSlotEndpoint.TryParse accepts cli://grok-cli and cli://grok-build as the
    /// grok-cli strategy and cli://codex-cli as the codex-cli strategy, a behavior no record in the requirements store
    /// describes. Data: three inline endpoint and expected-strategy pairs.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for cli://, grok, codex and brain slot (uncited-matches.txt, test 8 of 23); FR-MCP-HELP-011 (the Agent Help grok-cli strategy) has no criteria and is not the brain-slot endpoint parser. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs all three rows when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Theory]
    [Trait("Quarantine", "NoMatchingCriterion")]
    [InlineData("cli://grok-cli", "grok-cli")]
    [InlineData("cli://grok-build", "grok-cli")]
    [InlineData("cli://codex-cli", "codex-cli")]
    public void TryParseCliEndpoint_KnownStrategies_Succeed(string endpoint, string expected)
    {
        Assert.True(CliBrainSlotEndpoint.TryParse(endpoint, out var strategy));
        Assert.Equal(expected, strategy);
    }

    /// <summary>
    /// No requirement id applies: CliBrainSlotEndpoint.TryParse rejects an HTTP endpoint (http://127.0.0.1:8311/v1), an
    /// unknown CLI strategy (cli://unknown) and an empty string, a behavior no record in the requirements store
    /// describes. Data: the three inline endpoints.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for cli://, grok, codex and brain slot (uncited-matches.txt, test 9 of 23); no criterion states which endpoints the brain-slot CLI parser refuses. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs all three rows when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Theory]
    [Trait("Quarantine", "NoMatchingCriterion")]
    [InlineData("http://127.0.0.1:8311/v1")]
    [InlineData("cli://unknown")]
    [InlineData("")]
    public void TryParseCliEndpoint_Unknown_Fails(string endpoint)
    {
        Assert.False(CliBrainSlotEndpoint.TryParse(endpoint, out _));
    }

    /// <summary>
    /// No requirement id applies: BrainSlotValidation.ValidateEndpoint accepts the Cli endpoint cli://grok-cli without
    /// throwing even when loopback endpoints are disallowed and the allowed host list is empty, a behavior no record in
    /// the requirements store describes. Data: a StaticOptionsMonitor (Services/CliBrainSlotStrategyTestsSupport.cs)
    /// over BrainSlotOptions with AllowLoopbackEndpoints false and no AllowedEndpointHosts.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for endpoint policy, allowed hosts, loopback, cli:// and EndpointNotAllowed (uncited-matches.txt, test 10 of 23); the nearest, FR-MCP-129 criterion 6, states only that an invocation is rejected unless endpoint policy passes, not that a CLI endpoint passes without a host allowlist, and TR-MCP-QUAD-002 has no criteria. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
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

    /// <summary>
    /// FR-MCP-129 criterion 6 (by position; the criterion has no id), in part: BrainSlotValidation.ValidateEndpoint
    /// rejects the Cli endpoint cli://copilot-cli with a BrainSlotValidationException whose reason is
    /// BrainSlotReasonCodes.EndpointNotAllowed, which proves the endpoint-policy condition of the criterion (an
    /// invocation is rejected unless, among other conditions, endpoint policy passes): the endpoint check refuses a
    /// disallowed CLI endpoint with a structured reason. It does not prove that an invocation is then rejected, nor the
    /// criterion's other conditions (brain-slot execution enabled, the slot enabled, credentials resolving, required turn
    /// transactions enabled). Data: a StaticOptionsMonitor (Services/CliBrainSlotStrategyTestsSupport.cs) over default
    /// BrainSlotOptions.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, FR-MCP-129 criterion 6: cites the criterion whose endpoint-policy condition this case proves in part, in place of "No requirement id applies" (operator 2026-11-07: "Find the AC matching the test behavior."; uncited-matches.txt, test 11 of 23, the one partial match); the summary states the part it does not prove; the case is not quarantined and the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    public void ValidateEndpoint_CliUnknownStrategy_Throws()
    {
        var options = new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions());

        var ex = Assert.Throws<BrainSlotValidationException>(() =>
            BrainSlotValidation.ValidateEndpoint("Cli", "cli://copilot-cli", options));

        Assert.Equal(BrainSlotReasonCodes.EndpointNotAllowed, ex.Reason);
    }

    /// <summary>
    /// No requirement id applies: for a first turn (resume false)
    /// GrokCliAgentExecutionStrategy.BuildPersistentGrokArgumentList emits --effort xhigh, --reasoning-effort xhigh,
    /// --model grok-4.6, --session-id with the given id, --always-approve, --no-plan, --max-turns 1, --no-subagents and
    /// --verbatim, and emits neither --resume nor a bare plan argument; no record in the requirements store describes
    /// these arguments. Data: the fixed session GUID aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee, the model grok-4.6, and
    /// placeholder Windows-style working-folder and prompt-file path strings.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for grok, xhigh, reasoning effort, --resume, session id near resume and brain slot (uncited-matches.txt, test 12 of 23); the nearest, FR-MCP-HVPIPE-001 AC003, states the hostile validation reviewer launch effort, a different component, and none of the brain-slot Grok session arguments. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
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

    /// <summary>
    /// No requirement id applies: for a resumed turn (resume true)
    /// GrokCliAgentExecutionStrategy.BuildPersistentGrokArgumentList emits --resume and the session id and omits
    /// --session-id; no record in the requirements store describes these arguments. Data: the fixed session GUID
    /// aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee, the model grok-4.6, and placeholder Windows-style working-folder and
    /// prompt-file path strings.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for grok, xhigh, reasoning effort, --resume, session id near resume and brain slot (uncited-matches.txt, test 13 of 23); no criterion states Grok session resume. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
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

    /// <summary>
    /// No requirement id applies: for a first turn (no session id)
    /// CodexCliAgentExecutionStrategy.BuildPersistentCodexArgumentList starts with exec, omits resume, and emits
    /// --model gpt-5.6-sol, model_reasoning_effort="xhigh", --json and the stdin marker -; no record in the
    /// requirements store describes these arguments. Data: the model gpt-5.6-sol, a null session id, and placeholder
    /// Windows-style working-folder and output-file path strings.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for codex, xhigh, reasoning effort, --resume and brain slot (uncited-matches.txt, test 14 of 23); the nearest, FR-MCP-HVPIPE-001 AC003, states the hostile validation reviewer launch effort, a different component and a different effort value. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
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

    /// <summary>
    /// No requirement id applies: for a resumed turn CodexCliAgentExecutionStrategy.BuildPersistentCodexArgumentList
    /// starts with exec, places the exec options (including --json and -o) before the resume subcommand, follows resume
    /// with the thread id, and ends with the stdin marker -; no record in the requirements store describes these
    /// arguments. Data: the model gpt-5.6-sol, the session id thread-123, and placeholder Windows-style working-folder
    /// and output-file path strings.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for codex, xhigh, reasoning effort, --resume and brain slot (uncited-matches.txt, test 15 of 23); no criterion states the Codex resume argument order. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
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

    /// <summary>
    /// No requirement id applies: CliBrainSlotChatClient.ExtractCodexSessionId returns the thread id thr_abc from a
    /// JSONL stream whose thread.started event carries it; no record in the requirements store describes this parsing.
    /// Data: an inline two-line JSONL string (a thread.started event and an item.completed event).
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for thread.started, ExtractCodex, codex and session id near resume (uncited-matches.txt, test 16 of 23); the Codex criteria concern plugin caches, the hostile validation reviewer pool and transcript ingestion, not the brain-slot client. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
    public void ExtractCodexSessionId_FromJsonlThreadStarted_ReturnsId()
    {
        var jsonl = """
            {"type":"thread.started","thread_id":"thr_abc"}
            {"type":"item.completed"}
            """;

        Assert.Equal("thr_abc", CliBrainSlotChatClient.ExtractCodexSessionId(jsonl));
    }

    /// <summary>
    /// No requirement id applies: CliBrainSlotChatClient.ExtractCodexAssistantText returns the text of the
    /// agent_message item from a JSONL stream that also holds a thread.started event and a command_execution item; no
    /// record in the requirements store describes this parsing. Data: an inline three-line JSONL string whose agent
    /// message text is Logic analysis of the voice chat.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for agent_message, assistant text, ExtractCodex and codex (uncited-matches.txt, test 17 of 23); the nearest, FR-MCP-HVPIPE-010 AC006, states how the hostile validation reviewer output contract reads records split across CLI events, a different component, not Codex agent_message extraction. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
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

    /// <summary>
    /// No requirement id applies: CliBrainSlotChatClient.ExtractCodexAssistantText returns the message of an event_msg
    /// event whose payload is an agent_message; no record in the requirements store describes this parsing. Data: an
    /// inline one-line JSONL string whose message is working on it.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for agent_message, assistant text, ExtractCodex and codex (uncited-matches.txt, test 18 of 23); no criterion states Codex event_msg extraction. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
    public void ExtractCodexAssistantText_FromEventMsg_ReturnsMessage()
    {
        var jsonl = """
            {"type":"event_msg","payload":{"type":"agent_message","message":"working on it"}}
            """;

        Assert.Equal("working on it", CliBrainSlotChatClient.ExtractCodexAssistantText(jsonl));
    }

    /// <summary>
    /// No requirement id applies: BrainSlotCredentialResolver.IsSupportedReference accepts the references
    /// cli:interactive and cli:session and rejects vault:future; no record in the requirements store describes the cli:
    /// credential scheme. Data: three inline reference and expected-result rows and a resolver over an empty
    /// configuration.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for cli:interactive, credential near scheme, reference or resolver (uncited-matches.txt, test 19 of 23); the nearest, FR-MCP-129 criterion 4, states that CRUD stores and returns only a credential reference, not which reference schemes the resolver supports, and TEST-MCP-177 has no criteria. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs all three rows when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Theory]
    [Trait("Quarantine", "NoMatchingCriterion")]
    [InlineData("cli:interactive", true)]
    [InlineData("cli:session", true)]
    [InlineData("vault:future", false)]
    public void CredentialResolver_SupportsCliScheme(string reference, bool expected)
    {
        var resolver = new BrainSlotCredentialResolver(new ConfigurationBuilder().Build());
        Assert.Equal(expected, resolver.IsSupportedReference(reference));
    }

    /// <summary>
    /// No requirement id applies: BrainSlotCredentialResolver.ResolveAsync returns the placeholder value cli for the
    /// reference cli:interactive; no record in the requirements store describes the cli: credential scheme. Data: a
    /// resolver over an empty configuration.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for cli:interactive, credential near scheme, reference or resolver (uncited-matches.txt, test 20 of 23); no criterion states the placeholder the cli: scheme resolves to. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
    public async Task CredentialResolver_CliInteractive_ReturnsPlaceholder()
    {
        var resolver = new BrainSlotCredentialResolver(new ConfigurationBuilder().Build());
        var value = await resolver.ResolveAsync("cli:interactive", TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("cli", value);
    }
}
