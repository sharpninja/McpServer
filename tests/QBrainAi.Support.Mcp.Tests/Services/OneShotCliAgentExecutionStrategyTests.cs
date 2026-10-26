using System.Diagnostics;
using System.Text;

using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.TestSupport;

using Microsoft.Extensions.Logging.Abstractions;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Tests the one-shot CLI execution strategy's command shapes for Codex, Claude, Grok and Cline, and the strategy
/// name. The class runs in <see cref="ProcessEnvironmentCollection"/> because every test that starts a session writes
/// the process environment: the strategy's temp-folder lookup reads the CLI temp variables, so those tests set and
/// restore them through <see cref="CliTempVariables"/> and are integration tests (TR-MCP-USECASE-018 AC-006,
/// TR-MCP-USECASE-019 AC-008; hostile validation finding R6-F1).
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class OneShotCliAgentExecutionStrategyTests
{
    /// <summary>
    /// The Codex one-shot command runs <c>codex exec</c> with the effort, working folder, output file and stdin
    /// prompt. The test sets and restores every CLI temp variable through <see cref="CliTempVariables"/>, so it is an
    /// integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReadInitialResponseAsync_WithCodexAgent_UsesCodexOneShotCommandShape()
    {
        await CliTempVariables.RunWithTestFolderAsync(async _ =>
        {
            CapturingProcessSpawner spawner = new(outputBody: "codex final");
            OneShotCliAgentExecutionStrategy strategy = CreateStrategy(spawner);

            await using IAgentExecutionSession session = await strategy.CreateSessionAsync(CreateRequest("codex"), cancellationToken: TestContext.Current.CancellationToken);
            AgentCliResult result = await session.ReadInitialResponseAsync(CancellationToken.None);

            Assert.Equal(AgentCliResultState.Success, result.State);
            Assert.Equal("codex final", result.Body);
            Assert.NotNull(spawner.StartInfo);
            Assert.Equal("codex", spawner.StartInfo!.FileName);
            Assert.Contains("exec", spawner.StartInfo.ArgumentList);
            Assert.Equal("model_reasoning_effort=\"xhigh\"", GetArgumentAfter(spawner.StartInfo, "-c"));
            Assert.Equal("Q:\\__mcp_unit_test__\\QBrainAi", GetArgumentAfter(spawner.StartInfo, "-C"));
            Assert.NotNull(GetArgumentAfter(spawner.StartInfo, "-o"));
            Assert.Contains("-", spawner.StartInfo.ArgumentList);
            Assert.DoesNotContain("--output-schema", spawner.StartInfo.ArgumentList);
            Assert.Equal("rendered prompt", spawner.Process!.StandardInputText);
        });
    }

    /// <summary>
    /// The Claude one-shot command runs <c>claude -p</c> in plan mode with the model, effort and working folder and a
    /// stdin prompt. The test sets and restores every CLI temp variable through <see cref="CliTempVariables"/>, so it
    /// is an integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReadInitialResponseAsync_WithClaudeAgent_UsesClaudePlanModeCommandShape()
    {
        await CliTempVariables.RunWithTestFolderAsync(async _ =>
        {
            CapturingProcessSpawner spawner = new(stdout: "claude final");
            OneShotCliAgentExecutionStrategy strategy = CreateStrategy(spawner);

            await using IAgentExecutionSession session = await strategy.CreateSessionAsync(CreateRequest("claude"), cancellationToken: TestContext.Current.CancellationToken);
            AgentCliResult result = await session.ReadInitialResponseAsync(CancellationToken.None);

            Assert.Equal(AgentCliResultState.Success, result.State);
            Assert.Equal("claude final", result.Body);
            Assert.NotNull(spawner.StartInfo);
            Assert.Equal("claude", spawner.StartInfo!.FileName);
            Assert.Contains("-p", spawner.StartInfo.ArgumentList);
            Assert.Equal("plan", GetArgumentAfter(spawner.StartInfo, "--permission-mode"));
            Assert.Equal("Q:\\__mcp_unit_test__\\QBrainAi", GetArgumentAfter(spawner.StartInfo, "--add-dir"));
            Assert.Equal("opus", GetArgumentAfter(spawner.StartInfo, "--model"));
            Assert.Equal("max", GetArgumentAfter(spawner.StartInfo, "--effort"));
            Assert.Equal("rendered prompt", spawner.Process!.StandardInputText);
        });
    }

    /// <summary>
    /// The Grok one-shot command runs <c>grok</c> in plan mode with a prompt file, plain output and max effort. The
    /// test sets and restores every CLI temp variable through <see cref="CliTempVariables"/>, so it is an integration
    /// test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReadInitialResponseAsync_WithGrokAgent_UsesGrokPromptFileCommandShape()
    {
        await CliTempVariables.RunWithTestFolderAsync(async _ =>
        {
            CapturingProcessSpawner spawner = new(stdout: "grok final");
            OneShotCliAgentExecutionStrategy strategy = CreateStrategy(spawner);

            await using IAgentExecutionSession session = await strategy.CreateSessionAsync(CreateRequest("grok"), cancellationToken: TestContext.Current.CancellationToken);
            AgentCliResult result = await session.ReadInitialResponseAsync(CancellationToken.None);

            Assert.Equal(AgentCliResultState.Success, result.State);
            Assert.Equal("grok final", result.Body);
            Assert.NotNull(spawner.StartInfo);
            Assert.Equal("grok", spawner.StartInfo!.FileName);
            Assert.Equal("Q:\\__mcp_unit_test__\\QBrainAi", GetArgumentAfter(spawner.StartInfo, "--cwd"));
            Assert.Equal("plan", GetArgumentAfter(spawner.StartInfo, "--permission-mode"));
            Assert.Equal("plain", GetArgumentAfter(spawner.StartInfo, "--output-format"));
            Assert.Equal("max", GetArgumentAfter(spawner.StartInfo, "--effort"));
            Assert.Equal("max", GetArgumentAfter(spawner.StartInfo, "--reasoning-effort"));
            Assert.NotNull(GetArgumentAfter(spawner.StartInfo, "--prompt-file"));
            Assert.Equal("rendered prompt", spawner.PromptFileText);
            Assert.Equal(string.Empty, spawner.Process!.StandardInputText);
        });
    }

    /// <summary>
    /// The Cline one-shot command runs <c>cline -p</c> with the prompt as an argument, the working folder and xhigh
    /// thinking. The test sets and restores every CLI temp variable through <see cref="CliTempVariables"/>, so it is
    /// an integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReadInitialResponseAsync_WithClineAgent_UsesClineParameterizedPromptCommandShape()
    {
        await CliTempVariables.RunWithTestFolderAsync(async _ =>
        {
            CapturingProcessSpawner spawner = new(stdout: "cline final");
            OneShotCliAgentExecutionStrategy strategy = CreateStrategy(spawner);

            await using IAgentExecutionSession session = await strategy.CreateSessionAsync(CreateRequest("cline"), cancellationToken: TestContext.Current.CancellationToken);
            AgentCliResult result = await session.ReadInitialResponseAsync(CancellationToken.None);

            Assert.Equal(AgentCliResultState.Success, result.State);
            Assert.Equal("cline final", result.Body);
            Assert.NotNull(spawner.StartInfo);
            Assert.Equal("cline", spawner.StartInfo!.FileName);
            Assert.Contains("-p", spawner.StartInfo.ArgumentList);
            Assert.Equal("Q:\\__mcp_unit_test__\\QBrainAi", GetArgumentAfter(spawner.StartInfo, "-c"));
            Assert.Equal("xhigh", GetArgumentAfter(spawner.StartInfo, "--thinking"));
            Assert.Contains("rendered prompt", spawner.StartInfo.ArgumentList);
            Assert.Equal(string.Empty, spawner.Process!.StandardInputText);
        });
    }

    /// <summary>The one-shot strategy name is supported and is the default instead of Copilot.</summary>
    [Fact]
    public void AgentExecutionStrategyNames_SupportsOneShotCliAndDefaultsAwayFromCopilot()
    {
        Assert.Contains(AgentExecutionStrategyNames.OneShotCli, AgentExecutionStrategyNames.SupportedNames);
        Assert.True(AgentExecutionStrategyNames.IsSupported(AgentExecutionStrategyNames.OneShotCli));
        Assert.Equal(AgentExecutionStrategyNames.OneShotCli, AgentExecutionStrategyNames.NormalizeOrDefault(null));
    }

    private static OneShotCliAgentExecutionStrategy CreateStrategy(CapturingProcessSpawner spawner) =>
        new(
            new CapturingProcessEnvironmentService(),
            spawner,
            NullLogger<OneShotCliAgentExecutionStrategy>.Instance);

    private static AgentExecutionSessionRequest CreateRequest(string agentPath) =>
        new(
            "rendered prompt",
            "Q:\\__mcp_unit_test__\\QBrainAi",
            "triage",
            "one-shot-cli",
            new AgentCliClientOptions
            {
                AgentPath = agentPath,
                Model = "auto",
                WorkingDirectory = "Q:\\__mcp_unit_test__\\QBrainAi",
            });

    private static string? GetArgumentAfter(ProcessStartInfo startInfo, string argument)
    {
        int index = startInfo.ArgumentList.IndexOf(argument);
        return index >= 0 && index + 1 < startInfo.ArgumentList.Count
            ? startInfo.ArgumentList[index + 1]
            : null;
    }

    private sealed class CapturingProcessEnvironmentService : IProcessEnvironmentService
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

    private sealed class CapturingProcessSpawner(string stdout = "", string stderr = "", string outputBody = "") : IProcessSpawner
    {
        public ProcessStartInfo? StartInfo { get; private set; }

        public CapturingSpawnedProcess? Process { get; private set; }

        public string? PromptFileText { get; private set; }

        public ISpawnedProcess Spawn(ProcessStartInfo startInfo)
        {
            StartInfo = startInfo;
            string? outputPath = GetArgumentAfter(startInfo, "-o");
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                File.WriteAllText(outputPath, outputBody);
            }

            string? promptFilePath = GetArgumentAfter(startInfo, "--prompt-file");
            if (!string.IsNullOrWhiteSpace(promptFilePath))
            {
                PromptFileText = File.ReadAllText(promptFilePath);
            }

            Process = new CapturingSpawnedProcess(stdout, stderr);
            return Process;
        }
    }

    private sealed class CapturingSpawnedProcess : ISpawnedProcess
    {
        private readonly MemoryStream _standardInput = new();
        private bool _killed;

        public CapturingSpawnedProcess(string stdout, string stderr)
        {
            StandardInput = new StreamWriter(_standardInput, Encoding.UTF8, leaveOpen: true);
            StandardOutput = CreateReader(stdout);
            StandardError = CreateReader(stderr);
        }

        public StreamReader StandardOutput { get; }

        public StreamReader StandardError { get; }

        public StreamWriter? StandardInput { get; }

        public int Id => 1234;

        public bool HasExited => !_killed;

        public int ExitCode { get; } = 0;

        public string StandardInputText => Encoding.UTF8.GetString(_standardInput.ToArray()).TrimStart('\uFEFF');

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Kill()
        {
            _killed = true;
        }

        public void Dispose()
        {
            StandardInput?.Dispose();
            StandardOutput.Dispose();
            StandardError.Dispose();
            _standardInput.Dispose();
        }

        private static StreamReader CreateReader(string value) =>
            new(new MemoryStream(Encoding.UTF8.GetBytes(value)), Encoding.UTF8);
    }
}
