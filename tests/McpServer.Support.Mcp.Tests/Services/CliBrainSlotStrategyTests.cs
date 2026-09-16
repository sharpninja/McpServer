using System.Diagnostics;
using System.Text;
using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-QBCLI-001: QuadBrain CLI provider kind, persistent Grok/Codex session args, and slot session reuse.
/// </summary>
public sealed class CliBrainSlotStrategyTests
{
    private static readonly SemaphoreSlim TempEnvLock = new(1, 1);

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
            workingDirectory: @"F:\GitHub\McpServer",
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
            workingDirectory: @"F:\GitHub\McpServer",
            promptFilePath: @"C:\temp\grok-prompt.txt",
            model: "grok-4.6",
            sessionId: sessionId,
            resume: true);

        Assert.Contains("--resume", args);
        Assert.Contains(sessionId, args);
        Assert.DoesNotContain("--session-id", args);
    }

    [Fact]
    public async Task CompleteAsync_Grok_UsesIsolatedCwdNotConfiguredRepoRoot()
    {
        await WithIsolatedQuadbrainTemp(async _ =>
        {
            var spawner = new RecordingSpawner(stdout: "ok");
            var client = new CliBrainSlotChatClient(
                spawner,
                new NoopProcessEnvironmentService(),
                new CliBrainSlotSessionStore(),
                new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions
                {
                    CliWorkingDirectory = @"F:\GitHub\McpServer",
                }),
                NullLogger.Instance);

            await client.CompleteAsync(GrokSlot(), "ping", temperature: null, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            var cwd = spawner.StartInfos[0].WorkingDirectory;
            Assert.False(string.IsNullOrWhiteSpace(cwd));
            Assert.Contains("grok-work", cwd, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(@"F:\GitHub\McpServer", cwd, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(cwd, ArgumentAfter(spawner.StartInfos[0], "--cwd"));
            Assert.Equal("0", spawner.StartInfos[0].Environment["GROK_AGENT_DASHBOARD"]);
            Assert.Equal("0", spawner.StartInfos[0].Environment["GROK_MEMORY"]);
            Assert.Equal("0", spawner.StartInfos[0].Environment["GROK_SUBAGENTS"]);
            Assert.False(spawner.StartInfos[0].Environment.ContainsKey("GROK_PLUGIN_ROOT"));
            Assert.False(spawner.StartInfos[0].Environment.ContainsKey("GROK_HOME"));
        }).ConfigureAwait(true);
    }

    [Fact]
    public async Task CompleteAsync_Grok_MaxTurnsReached_ReturnsStdoutInsteadOfThrowing()
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
    }

    [Fact]
    public async Task CompleteAsync_Grok_MaxTurnsReached_EmptyStdout_DoesNotThrow()
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
    }

    [Fact]
    public async Task CompleteAsync_Grok_NonZeroExit_StillThrows()
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
    }

    [Fact]
    public async Task CompleteAsync_Grok_ResumeSessionMissing_RetriesWithoutResume()
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
    }

    [Fact]
    public void BuildPersistentCodexArgumentList_FirstTurn_UsesExecStdinAndXhigh()
    {
        var args = CodexCliAgentExecutionStrategy.BuildPersistentCodexArgumentList(
            workingDirectory: @"F:\GitHub\McpServer",
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
            workingDirectory: @"F:\GitHub\McpServer",
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

    [Fact]
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
                    CliWorkingDirectory = @"F:\GitHub\McpServer",
                    CliRunAs = "kingd",
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

    [Fact]
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
                    CliWorkingDirectory = @"F:\GitHub\McpServer",
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

    [Fact]
    public void DefaultSharedTempDirectory_Windows_IsUnderCommonApplicationData()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var directory = Path.GetFullPath(CliBrainSlotChatClient.DefaultSharedTempDirectory());
        var common = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        Assert.StartsWith(common, directory, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("quadbrain-cli", directory, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
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

    [Fact]
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
                    CliWorkingDirectory = @"F:\GitHub\McpServer",
                    CliRunAs = "kingd",
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

    [Fact]
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
                    CliWorkingDirectory = @"F:\GitHub\McpServer",
                    CliRunAs = "kingd",
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

    [Fact]
    public async Task CompleteAsync_GrokSecondCall_ResumesSameSession()
    {
        var spawner = new RecordingSpawner(stdout: "ok");
        var store = new CliBrainSlotSessionStore();
        var client = new CliBrainSlotChatClient(
            spawner,
            new NoopProcessEnvironmentService(),
            store,
            new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions
            {
                CliWorkingDirectory = @"F:\GitHub\McpServer",
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
    }

    [Fact]
    public async Task CompleteAsync_CodexSecondCall_ResumesExtractedThread()
    {
        var jsonl = """{"type":"thread.started","thread_id":"thr_logic"}""" + Environment.NewLine;
        var spawner = new RecordingSpawner(stdout: jsonl, outputBody: "logic-out");
        var client = new CliBrainSlotChatClient(
            spawner,
            new NoopProcessEnvironmentService(),
            new CliBrainSlotSessionStore(),
            new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions
            {
                CliWorkingDirectory = @"F:\GitHub\McpServer",
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
    }

    [Fact]
    public async Task Factory_Create_CliSlot_ReturnsCliClient()
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

    private static async Task WithIsolatedQuadbrainTemp(Func<string, Task> body)
    {
        await TempEnvLock.WaitAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var previousQuad = Environment.GetEnvironmentVariable("MCPSERVER_QUADBRAIN_CLI_TEMP");
        var previousOneShot = Environment.GetEnvironmentVariable("MCPSERVER_ONESHOT_TEMP");
        var temp = Path.Combine(Path.GetTempPath(), "qbt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            Environment.SetEnvironmentVariable("MCPSERVER_QUADBRAIN_CLI_TEMP", temp);
            Environment.SetEnvironmentVariable("MCPSERVER_ONESHOT_TEMP", null);
            await body(temp).ConfigureAwait(true);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCPSERVER_QUADBRAIN_CLI_TEMP", previousQuad);
            Environment.SetEnvironmentVariable("MCPSERVER_ONESHOT_TEMP", previousOneShot);
            try
            {
                Directory.Delete(temp, recursive: true);
            }
            catch (IOException)
            {
            }

            TempEnvLock.Release();
        }
    }

    private static string? ArgumentAfter(ProcessStartInfo startInfo, string name)
    {
        var index = startInfo.ArgumentList.IndexOf(name);
        return index >= 0 && index + 1 < startInfo.ArgumentList.Count
            ? startInfo.ArgumentList[index + 1]
            : null;
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
        where T : class
    {
        public T CurrentValue { get; } = value;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
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
