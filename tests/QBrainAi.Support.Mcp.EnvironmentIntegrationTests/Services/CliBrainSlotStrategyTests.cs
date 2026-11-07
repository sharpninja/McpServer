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

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

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
/// <para>2026-11-07, TEST-MCP-QBCLI-001; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/CliBrainSlotStrategyTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 13 Category=Integration cases and the helpers only they use (AssertGrokRunsIsolatedAsync, GrokSlot, CodexSlot, WithIsolatedQuadbrainTemp, ArgumentAfter, NoopProcessEnvironmentService, RecordingSpawner and FakeProcess) moved unchanged with the class attributes; the helpers both halves use come from Services/CliBrainSlotStrategyTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</para>
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class CliBrainSlotStrategyTests
{
    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): Grok runs in an isolated working folder with its dashboard, memory and subagents off and
    /// without the inherited <c>GROK_PLUGIN_ROOT</c> and <c>GROK_HOME</c>. The test writes the process environment
    /// (the temp variables, and both Grok variables set to scripted values so their removal is observed), restores it,
    /// and is an integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteAsync_Grok_UsesIsolatedCwdNotConfiguredRepoRoot()
    {
        await WithIsolatedQuadbrainTemp(async temp =>
        {
            var previousPluginRoot = Environment.GetEnvironmentVariable("GROK_PLUGIN_ROOT");
            var previousGrokHome = Environment.GetEnvironmentVariable("GROK_HOME");
            Environment.SetEnvironmentVariable("GROK_PLUGIN_ROOT", Path.Combine(temp, "inherited-grok-plugin-root"));
            Environment.SetEnvironmentVariable("GROK_HOME", Path.Combine(temp, "inherited-grok-home"));
            try
            {
                await AssertGrokRunsIsolatedAsync().ConfigureAwait(true);
            }
            finally
            {
                Environment.SetEnvironmentVariable("GROK_PLUGIN_ROOT", previousPluginRoot);
                Environment.SetEnvironmentVariable("GROK_HOME", previousGrokHome);
            }
        }).ConfigureAwait(true);
    }

    private static async Task AssertGrokRunsIsolatedAsync()
    {
        var spawner = new RecordingSpawner(stdout: "ok");
        var client = new CliBrainSlotChatClient(
            spawner,
            new NoopProcessEnvironmentService(),
            new CliBrainSlotSessionStore(),
            new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions
            {
                CliWorkingDirectory = @"Q:\__mcp_unit_test__\QBrainAi",
            }),
            NullLogger.Instance);

        await client.CompleteAsync(GrokSlot(), "ping", temperature: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var cwd = spawner.StartInfos[0].WorkingDirectory;
        Assert.False(string.IsNullOrWhiteSpace(cwd));
        Assert.Contains("grok-work", cwd, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"Q:\__mcp_unit_test__\QBrainAi", cwd, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(cwd, ArgumentAfter(spawner.StartInfos[0], "--cwd"));
        Assert.Equal("0", spawner.StartInfos[0].Environment["GROK_AGENT_DASHBOARD"]);
        Assert.Equal("0", spawner.StartInfos[0].Environment["GROK_MEMORY"]);
        Assert.Equal("0", spawner.StartInfos[0].Environment["GROK_SUBAGENTS"]);
        Assert.False(spawner.StartInfos[0].Environment.ContainsKey("GROK_PLUGIN_ROOT"));
        Assert.False(spawner.StartInfos[0].Environment.ContainsKey("GROK_HOME"));
    }

    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): Grok output that stops at the turn limit is returned instead of thrown. The
    /// client's temp-folder lookup reads the CLI temp variables, so the test sets and restores them through
    /// <c>WithIsolatedQuadbrainTemp</c> and is an integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019
    /// AC-008; hostile validation finding R6-F1).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteAsync_Grok_MaxTurnsReached_ReturnsStdoutInsteadOfThrowing()
    {
        await WithIsolatedQuadbrainTemp(async _ =>
        {
            var spawner = new RecordingSpawner(
                stdout: "Creativity analysis for ArbiterOfTruth",
                stderr: "Max turns reached",
                exitCode: 1);
            var client = new CliBrainSlotChatClient(
                spawner,
                new NoopProcessEnvironmentService(),
                new CliBrainSlotSessionStore(),
                new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions()),
                NullLogger.Instance);

            var output = await client.CompleteAsync(GrokSlot(), "new session", temperature: null, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            Assert.Equal("Creativity analysis for ArbiterOfTruth", output);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): Grok stopping at the turn limit with empty output returns empty text. The
    /// client's temp-folder lookup reads the CLI temp variables, so the test sets and restores them through
    /// <c>WithIsolatedQuadbrainTemp</c> and is an integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019
    /// AC-008; hostile validation finding R6-F1).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteAsync_Grok_MaxTurnsReached_EmptyStdout_DoesNotThrow()
    {
        await WithIsolatedQuadbrainTemp(async _ =>
        {
            var spawner = new RecordingSpawner(stdout: "", stderr: "Error: Max turns reached", exitCode: 1);
            var client = new CliBrainSlotChatClient(
                spawner,
                new NoopProcessEnvironmentService(),
                new CliBrainSlotSessionStore(),
                new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions()),
                NullLogger.Instance);

            var output = await client.CompleteAsync(GrokSlot(), "ping", temperature: null, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            Assert.Equal(string.Empty, output);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): any other Grok failure still throws with its error text. The client's
    /// temp-folder lookup reads the CLI temp variables, so the test sets and restores them through
    /// <c>WithIsolatedQuadbrainTemp</c> and is an integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019
    /// AC-008; hostile validation finding R6-F1).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteAsync_Grok_NonZeroExit_StillThrows()
    {
        await WithIsolatedQuadbrainTemp(async _ =>
        {
            var spawner = new RecordingSpawner(stdout: "", stderr: "Access is denied. (os error 5)", exitCode: 1);
            var client = new CliBrainSlotChatClient(
                spawner,
                new NoopProcessEnvironmentService(),
                new CliBrainSlotSessionStore(),
                new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions()),
                NullLogger.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => client.CompleteAsync(GrokSlot(), "ping", temperature: null, TestContext.Current.CancellationToken))
                .ConfigureAwait(true);

            Assert.Contains("Access is denied", ex.Message, StringComparison.Ordinal);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): a Grok resume whose session is missing retries once without resume. The
    /// client's temp-folder lookup reads the CLI temp variables, so the test sets and restores them through
    /// <c>WithIsolatedQuadbrainTemp</c> and is an integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019
    /// AC-008; hostile validation finding R6-F1).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteAsync_Grok_ResumeSessionMissing_RetriesWithoutResume()
    {
        await WithIsolatedQuadbrainTemp(async _ =>
        {
            var spawner = new RecordingSpawner(stdout: "first-ok")
            {
                Script =
                [
                    (0, "first-ok", ""),
                    (1, "", """Session "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee" not found locally, restoring conversation from remote..."""),
                    (0, "retry-ok", ""),
                ],
            };
            var client = new CliBrainSlotChatClient(
                spawner,
                new NoopProcessEnvironmentService(),
                new CliBrainSlotSessionStore(),
                new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions()),
                NullLogger.Instance);

            var first = await client.CompleteAsync(GrokSlot(), "one", temperature: null, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            var second = await client.CompleteAsync(GrokSlot(), "two", temperature: null, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            Assert.Equal("first-ok", first);
            Assert.Equal("retry-ok", second);
            Assert.Equal(3, spawner.StartInfos.Count);
            Assert.Contains("--session-id", spawner.StartInfos[0].ArgumentList);
            Assert.Contains("--resume", spawner.StartInfos[1].ArgumentList);
            Assert.Contains("--session-id", spawner.StartInfos[2].ArgumentList);
            Assert.DoesNotContain("--resume", spawner.StartInfos[2].ArgumentList);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): Codex returns the assistant text from the JSONL stream, not the raw events. The test writes
    /// the process environment through <c>WithIsolatedQuadbrainTemp</c>, so it is an integration test
    /// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteAsync_Codex_ReturnsAssistantTextFromJsonlNotRawEvents()
    {
        await WithIsolatedQuadbrainTemp(async temp =>
        {
            var jsonl = """
                {"type":"thread.started","thread_id":"thr_logic"}
                {"type":"item.completed","item":{"type":"agent_message","text":"structured decomposition of QuadBrain voice chat"}}
                """ + Environment.NewLine;
            var spawner = new RecordingSpawner(stdout: jsonl, outputBody: "");
            var client = new CliBrainSlotChatClient(
                spawner,
                new NoopProcessEnvironmentService(),
                new CliBrainSlotSessionStore(),
                new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions
                {
                    CliWorkingDirectory = @"Q:\__mcp_unit_test__\QBrainAi",
                    CliRunAs = "fixture-user",
                }),
                NullLogger.Instance);

            var output = await client.CompleteAsync(
                CodexSlot(),
                "first",
                temperature: null,
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal("structured decomposition of QuadBrain voice chat", output);
            Assert.DoesNotContain("thread.started", output, StringComparison.Ordinal);
            await Task.CompletedTask.ConfigureAwait(true);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): Codex prefers the JSONL assistant text over tool-call JSON in the output file. The test
    /// writes the process environment through <c>WithIsolatedQuadbrainTemp</c>, so it is an integration test
    /// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteAsync_Codex_PrefersJsonlAssistantTextOverOutputFileToolCallJson()
    {
        await WithIsolatedQuadbrainTemp(async _ =>
        {
            var jsonl = """{"type":"item.completed","item":{"type":"agent_message","text":"read the marker then plan"}}""" + Environment.NewLine;
            var spawner = new RecordingSpawner(
                stdout: jsonl,
                outputBody: """{"tool_calls":[{"name":"read_file","arguments":{"path":"AGENTS-README-FIRST.yaml"}}]}""");
            var client = new CliBrainSlotChatClient(
                spawner,
                new NoopProcessEnvironmentService(),
                new CliBrainSlotSessionStore(),
                new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions
                {
                    CliWorkingDirectory = @"Q:\__mcp_unit_test__\QBrainAi",
                }),
                NullLogger.Instance);

            var output = await client.CompleteAsync(
                CodexSlot(),
                "first",
                temperature: null,
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal("read the marker then plan", output);
            Assert.DoesNotContain("tool_calls", output, StringComparison.Ordinal);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): the temp folder comes from the configured variable without the Windows service ACL. The
    /// test writes the process environment through <c>WithIsolatedQuadbrainTemp</c>, so it is an integration test
    /// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ResolveTempDirectory_HonorsConfiguredEnvVar_WithoutWindowsServiceAcl()
    {
        await WithIsolatedQuadbrainTemp(async temp =>
        {
            var directory = Path.GetFullPath(CliBrainSlotChatClient.ResolveTempDirectory());
            Assert.Equal(Path.GetFullPath(temp), directory);
            Assert.True(Directory.Exists(directory));
            await Task.CompletedTask.ConfigureAwait(true);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): Grok writes its prompt file under the configured temp folder and points TEMP and TMP at it.
    /// The test writes the process environment through <c>WithIsolatedQuadbrainTemp</c>, so it is an integration test
    /// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteAsync_Grok_WritesPromptFileUnderConfiguredTempAndSetsTempEnv()
    {
        await WithIsolatedQuadbrainTemp(async temp =>
        {
            var spawner = new RecordingSpawner(stdout: "ok");
            var client = new CliBrainSlotChatClient(
                spawner,
                new NoopProcessEnvironmentService(),
                new CliBrainSlotSessionStore(),
                new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions
                {
                    CliWorkingDirectory = @"Q:\__mcp_unit_test__\QBrainAi",
                    CliRunAs = "fixture-user",
                }),
                NullLogger.Instance);

            var output = await client.CompleteAsync(
                GrokSlot(),
                "ping",
                temperature: null,
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal("ok", output);
            var promptPath = ArgumentAfter(spawner.StartInfos[0], "--prompt-file");
            Assert.False(string.IsNullOrWhiteSpace(promptPath));
            var root = Path.GetFullPath(temp);
            Assert.StartsWith(root, Path.GetFullPath(promptPath!), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(root, spawner.StartInfos[0].Environment["TEMP"]);
            Assert.Equal(root, spawner.StartInfos[0].Environment["TMP"]);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): Codex writes its output file under the configured temp folder. The test writes the process
    /// environment through <c>WithIsolatedQuadbrainTemp</c>, so it is an integration test (TR-MCP-USECASE-018 AC-006,
    /// TR-MCP-USECASE-019 AC-008).
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteAsync_Codex_WritesOutputFileUnderConfiguredTemp()
    {
        await WithIsolatedQuadbrainTemp(async temp =>
        {
            var jsonl = """{"type":"thread.started","thread_id":"thr_logic"}""" + Environment.NewLine;
            var spawner = new RecordingSpawner(stdout: jsonl, outputBody: "logic-out");
            var client = new CliBrainSlotChatClient(
                spawner,
                new NoopProcessEnvironmentService(),
                new CliBrainSlotSessionStore(),
                new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions
                {
                    CliWorkingDirectory = @"Q:\__mcp_unit_test__\QBrainAi",
                    CliRunAs = "fixture-user",
                }),
                NullLogger.Instance);

            var output = await client.CompleteAsync(
                CodexSlot(),
                "first",
                temperature: null,
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal("logic-out", output);
            var outputPath = ArgumentAfter(spawner.StartInfos[0], "-o");
            Assert.False(string.IsNullOrWhiteSpace(outputPath));
            Assert.StartsWith(Path.GetFullPath(temp), Path.GetFullPath(outputPath!), StringComparison.OrdinalIgnoreCase);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): a second Grok call on the same slot resumes the first call's session. The
    /// client's temp-folder lookup reads the CLI temp variables, so the test sets and restores them through
    /// <c>WithIsolatedQuadbrainTemp</c> and is an integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019
    /// AC-008; hostile validation finding R6-F1).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteAsync_GrokSecondCall_ResumesSameSession()
    {
        await WithIsolatedQuadbrainTemp(async _ =>
        {
            var spawner = new RecordingSpawner(stdout: "ok");
            var store = new CliBrainSlotSessionStore();
            var client = new CliBrainSlotChatClient(
                spawner,
                new NoopProcessEnvironmentService(),
                store,
                new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions
                {
                    CliWorkingDirectory = @"Q:\__mcp_unit_test__\QBrainAi",
                }),
                NullLogger.Instance);

            var slot = GrokSlot();
            var first = await client.CompleteAsync(slot, "first", temperature: null, TestContext.Current.CancellationToken).ConfigureAwait(true);
            var second = await client.CompleteAsync(slot, "second", temperature: null, TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal("ok", first);
            Assert.Equal("ok", second);
            Assert.Equal(2, spawner.StartInfos.Count);
            Assert.Contains("--session-id", spawner.StartInfos[0].ArgumentList);
            Assert.Contains("--resume", spawner.StartInfos[1].ArgumentList);
            var firstId = ArgumentAfter(spawner.StartInfos[0], "--session-id");
            var resumeId = ArgumentAfter(spawner.StartInfos[1], "--resume");
            Assert.Equal(firstId, resumeId);
            Assert.Contains("grok-4.6", spawner.StartInfos[0].ArgumentList);
            Assert.Equal("xhigh", ArgumentAfter(spawner.StartInfos[0], "--effort"));
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): a second Codex call on the same slot resumes the thread the first call
    /// reported. The client's temp-folder lookup reads the CLI temp variables, so the test sets and restores them
    /// through <c>WithIsolatedQuadbrainTemp</c> and is an integration test (TR-MCP-USECASE-018 AC-006,
    /// TR-MCP-USECASE-019 AC-008; hostile validation finding R6-F1).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteAsync_CodexSecondCall_ResumesExtractedThread()
    {
        await WithIsolatedQuadbrainTemp(async _ =>
        {
            var jsonl = """{"type":"thread.started","thread_id":"thr_logic"}""" + Environment.NewLine;
            var spawner = new RecordingSpawner(stdout: jsonl, outputBody: "logic-out");
            var client = new CliBrainSlotChatClient(
                spawner,
                new NoopProcessEnvironmentService(),
                new CliBrainSlotSessionStore(),
                new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions
                {
                    CliWorkingDirectory = @"Q:\__mcp_unit_test__\QBrainAi",
                }),
                NullLogger.Instance);

            var slot = CodexSlot();
            await client.CompleteAsync(slot, "first", temperature: null, TestContext.Current.CancellationToken).ConfigureAwait(true);
            await client.CompleteAsync(slot, "second", temperature: null, TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(2, spawner.StartInfos.Count);
            Assert.DoesNotContain("resume", spawner.StartInfos[0].ArgumentList);
            Assert.Contains("resume", spawner.StartInfos[1].ArgumentList);
            Assert.Contains("thr_logic", spawner.StartInfos[1].ArgumentList);
            Assert.Contains("gpt-5.6-sol", spawner.StartInfos[0].ArgumentList);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBCLI-001 (historical): the factory creates the CLI client for a CLI slot, and the client completes.
    /// The client's temp-folder lookup reads the CLI temp variables, so the test sets and restores them through
    /// <c>WithIsolatedQuadbrainTemp</c> and is an integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019
    /// AC-008; hostile validation finding R6-F1).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Factory_Create_CliSlot_ReturnsCliClient()
    {
        await WithIsolatedQuadbrainTemp(async _ =>
        {
            var factory = new BrainSlotChatClientFactory(
                new RecordingSpawner(stdout: "ok"),
                new NoopProcessEnvironmentService(),
                new CliBrainSlotSessionStore(),
                new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions()),
                NullLogger<BrainSlotChatClientFactory>.Instance);

            var client = factory.Create(GrokSlot(), "cli");

            Assert.IsType<CliBrainSlotChatClient>(client);
            var output = await client.CompleteAsync(GrokSlot(), "ping", null, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal("ok", output);
        }).ConfigureAwait(true);
    }

    private static BrainSlotDefinitionEntity GrokSlot()
        => new()
        {
            SlotId = "brain-slot-arbiter-of-truth-grok-build",
            Role = BrainSlotRoles.ArbiterOfTruth,
            ProviderKind = "Cli",
            ModelId = "grok-4.6",
            Endpoint = "cli://grok-cli",
            CredentialReference = "cli:interactive",
            PartyId = "brain-slot:arbiter-of-truth",
            Enabled = true,
            TimeoutSeconds = 180,
        };

    private static BrainSlotDefinitionEntity CodexSlot()
        => new()
        {
            SlotId = "brain-slot-logic-codex-cli",
            Role = BrainSlotRoles.Logic,
            ProviderKind = "Cli",
            ModelId = "gpt-5.6-sol",
            Endpoint = "cli://codex-cli",
            CredentialReference = "cli:interactive",
            PartyId = "brain-slot:logic",
            Enabled = true,
            TimeoutSeconds = 180,
        };

    // MCPSERVER_QUADBRAIN_CLI_TEMP, which the client reads first, points at a test-owned folder and every other CLI temp
    // variable at a separate fallback folder while the body runs, then all are restored (the shared CliTempVariables
    // helper; this class runs in the serialized ProcessEnvironmentCollection).
    private static Task WithIsolatedQuadbrainTemp(Func<string, Task> body) =>
        CliTempVariables.RunWithTestFolderAsync("MCPSERVER_QUADBRAIN_CLI_TEMP", body);

    private static string? ArgumentAfter(ProcessStartInfo startInfo, string name)
    {
        var index = startInfo.ArgumentList.IndexOf(name);
        return index >= 0 && index + 1 < startInfo.ArgumentList.Count
            ? startInfo.ArgumentList[index + 1]
            : null;
    }

    private sealed class NoopProcessEnvironmentService : IProcessEnvironmentService
    {
        public void ApplyGitHubToken(ProcessStartInfo psi, string? token)
        {
        }

        public void ApplyRunAsEnvironment(ProcessStartInfo psi, string? runAsUser)
        {
        }

        public void ApplyAll(ProcessStartInfo psi, string? runAsUser, string? gitHubToken)
        {
        }

        public string ResolveExecutable(ProcessStartInfo psi, string fileName) => fileName;
    }

    private sealed class RecordingSpawner(
        string stdout = "",
        string outputBody = "",
        int exitCode = 0,
        string stderr = "") : IProcessSpawner
    {
        public List<ProcessStartInfo> StartInfos { get; } = [];

        public List<(int ExitCode, string Stdout, string Stderr)>? Script { get; init; }

        private int _scriptIndex;

        public ISpawnedProcess Spawn(ProcessStartInfo startInfo)
        {
            StartInfos.Add(Clone(startInfo));
            var outputPath = GetArgumentAfter(startInfo, "-o");
            if (!string.IsNullOrWhiteSpace(outputPath) && outputBody.Length > 0)
                File.WriteAllText(outputPath, outputBody, Encoding.UTF8);

            if (Script is { Count: > 0 } script && _scriptIndex < script.Count)
            {
                var step = script[_scriptIndex++];
                return new FakeProcess(step.Stdout, step.Stderr, step.ExitCode);
            }

            return new FakeProcess(stdout, stderr, exitCode);
        }

        private static string? GetArgumentAfter(ProcessStartInfo startInfo, string name)
        {
            var index = startInfo.ArgumentList.IndexOf(name);
            return index >= 0 && index + 1 < startInfo.ArgumentList.Count
                ? startInfo.ArgumentList[index + 1]
                : null;
        }

        private static ProcessStartInfo Clone(ProcessStartInfo source)
        {
            var copy = new ProcessStartInfo
            {
                FileName = source.FileName,
                WorkingDirectory = source.WorkingDirectory,
                RedirectStandardInput = source.RedirectStandardInput,
            };
            foreach (var argument in source.ArgumentList)
                copy.ArgumentList.Add(argument);
            // A fresh ProcessStartInfo inherits the current process environment.
            // Copying source entries does not drop keys the product removed
            // (GROK_PLUGIN_ROOT / GROK_HOME). Record the source environment only.
            var sourceKeys = new HashSet<string>(source.Environment.Keys, StringComparer.OrdinalIgnoreCase);
            var inherited = new List<string>();
            foreach (var key in copy.Environment.Keys)
            {
                if (!sourceKeys.Contains(key))
                    inherited.Add(key);
            }

            foreach (var key in inherited)
                copy.Environment.Remove(key);
            foreach (var (key, value) in source.Environment)
                copy.Environment[key] = value;
            return copy;
        }
    }

    private sealed class FakeProcess : ISpawnedProcess
    {
        private readonly MemoryStream _stdin = new();

        public FakeProcess(string stdout, string stderr = "", int exitCode = 0)
        {
            StandardOutput = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(stdout)), Encoding.UTF8);
            StandardError = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(stderr)), Encoding.UTF8);
            StandardInput = new StreamWriter(_stdin, Encoding.UTF8, leaveOpen: true);
            ExitCode = exitCode;
        }

        public StreamReader StandardOutput { get; }

        public StreamReader StandardError { get; }

        public StreamWriter? StandardInput { get; }

        public int Id => 42;

        public bool HasExited => true;

        public int ExitCode { get; }

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Kill()
        {
        }

        public void Dispose()
        {
            StandardInput?.Dispose();
            StandardOutput.Dispose();
            StandardError.Dispose();
            _stdin.Dispose();
        }
    }
}

/// <summary>
/// TEST-MCP-QBCLI-001 (historical; no such record in the requirements store): the default shared CLI temp directory on a Windows host lives under the machine's
/// common application data folder. Windows-only behavior, so an integration test on Windows hosts
/// (TR-MCP-USECASE-018 AC-006, TR-MCP-NUKE-019).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-QBCLI-001; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its 1 Windows-only Category=Integration case from QBrainAi.Support.Mcp.Tests (Services/CliBrainSlotStrategyTests.cs), whose unit classes stay there, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); only the namespace changed.</remarks>
[Trait("Category", "Integration")]
[Trait("Platform", "Windows")]
public sealed class CliBrainSlotWindowsTempDirectoryTests
{
    /// <summary>The default shared temp directory is under CommonApplicationData and named for quadbrain-cli.</summary>
    [Fact]
    public void DefaultSharedTempDirectory_Windows_IsUnderCommonApplicationData()
    {
        var directory = Path.GetFullPath(CliBrainSlotChatClient.DefaultSharedTempDirectory());
        var common = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        Assert.StartsWith(common, directory, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("quadbrain-cli", directory, StringComparison.OrdinalIgnoreCase);
    }
}
