using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-003: Verifies the Codex CLI execution strategy used by
/// background triage invokes <c>codex exec</c> non-interactively.
/// </summary>
public sealed class CodexCliAgentExecutionStrategyTests
{
    /// <summary>
    /// TEST-MCP-TRIAGE-003: Codex uses <c>exec</c> plus an output file and never
    /// uses the Copilot interactive <c>-i</c>, <c>-p</c>, stream, or yolo flags.
    /// </summary>
    [Fact]
    public async Task ReadInitialResponseAsync_UsesCodexExecWithoutCopilotInteractiveFlags()
    {
        var spawner = new CapturingProcessSpawner(exitCode: 0, outputBody: """{"title":"triage result"}""");
        var strategy = new CodexCliAgentExecutionStrategy(
            new CapturingProcessEnvironmentService(),
            spawner,
            NullLogger<CodexCliAgentExecutionStrategy>.Instance);

        await using var session = await strategy.CreateSessionAsync(CreateRequest()).ConfigureAwait(true);

        var result = await session.ReadInitialResponseAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(CopilotResultState.Success, result.State);
        Assert.Equal("""{"title":"triage result"}""", result.Body);
        Assert.NotNull(spawner.StartInfo);
        if (OperatingSystem.IsWindows())
        {
            Assert.EndsWith("cmd.exe", spawner.StartInfo.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/c", spawner.StartInfo.ArgumentList);
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains("codex.cmd", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains("exec", StringComparison.Ordinal));
        }
        else
        {
            Assert.Equal("codex.cmd", spawner.StartInfo.FileName);
        }

        Assert.Equal("F:\\GitHub\\McpServer", spawner.StartInfo.WorkingDirectory);
        var serializedArguments = string.Join(" ", spawner.StartInfo.ArgumentList);
        Assert.Contains("exec", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("--output-schema", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("--output-last-message", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("--model", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("model-triage", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("--sandbox", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("read-only", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("--skip-git-repo-check", serializedArguments, StringComparison.Ordinal);
        Assert.DoesNotContain("rendered prompt", serializedArguments, StringComparison.Ordinal);
        Assert.True(spawner.StartInfo.RedirectStandardInput);
        Assert.DoesNotContain("-i", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("-p", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--stream", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--yolo", spawner.StartInfo.ArgumentList);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: A failed Codex CLI run preserves stderr as the
    /// inspectable research-run failure reason.
    /// </summary>
    [Fact]
    public async Task ReadInitialResponseAsync_WhenCodexFails_ReturnsErrorResult()
    {
        var spawner = new CapturingProcessSpawner(exitCode: 2, stderr: "not authenticated");
        var strategy = new CodexCliAgentExecutionStrategy(
            new CapturingProcessEnvironmentService(),
            spawner,
            NullLogger<CodexCliAgentExecutionStrategy>.Instance);

        await using var session = await strategy.CreateSessionAsync(CreateRequest()).ConfigureAwait(true);

        var result = await session.ReadInitialResponseAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(CopilotResultState.Error, result.State);
        Assert.Contains("not authenticated", result.Stderr, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: Timed-out Codex CLI triage runs preserve partial
    /// stdout and stderr instead of returning only the timeout message.
    /// </summary>
    [Fact]
    public async Task ReadInitialResponseAsync_WhenCodexTimesOut_PreservesPartialStreams()
    {
        var spawner = new CapturingProcessSpawner(
            exitCode: 0,
            stdout: "analysis started",
            stderr: "loading context",
            waitForCancellation: true);
        var strategy = new CodexCliAgentExecutionStrategy(
            new CapturingProcessEnvironmentService(),
            spawner,
            NullLogger<CodexCliAgentExecutionStrategy>.Instance);

        await using var session = await strategy.CreateSessionAsync(CreateRequest()).ConfigureAwait(true);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));

        var result = await session.ReadInitialResponseAsync(timeout.Token).ConfigureAwait(true);

        Assert.Equal(CopilotResultState.Error, result.State);
        Assert.Contains("analysis started", result.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("loading context", result.Stderr, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cancelled or timed out", result.Stderr, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: Codex CLI triage runs stream stdout and stderr
    /// chunks to the configured output callback while the process is running.
    /// </summary>
    [Fact]
    public async Task ReadInitialResponseAsync_WithOutputCallback_StreamsAgentOutput()
    {
        var streamed = new List<string>();
        var spawner = new CapturingProcessSpawner(
            exitCode: 0,
            outputBody: """{"title":"triage result"}""",
            stdout: "analysis started" + Environment.NewLine + "analysis done",
            stderr: "loading context");
        var strategy = new CodexCliAgentExecutionStrategy(
            new CapturingProcessEnvironmentService(),
            spawner,
            NullLogger<CodexCliAgentExecutionStrategy>.Instance);
        var request = CreateRequest(outputReceivedAsync: (streamName, text) =>
        {
            streamed.Add($"{streamName}:{text}");
            return Task.CompletedTask;
        });

        await using var session = await strategy.CreateSessionAsync(request).ConfigureAwait(true);

        var result = await session.ReadInitialResponseAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(CopilotResultState.Success, result.State);
        Assert.Contains("stdout:analysis started", streamed);
        Assert.Contains("stdout:analysis done", streamed);
        Assert.Contains("stderr:loading context", streamed);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-003: Windows npm command shims are wrapped through
    /// <c>cmd.exe</c> so desktop spawning does not execute <c>.cmd</c> directly.
    /// </summary>
    [Fact]
    public async Task ReadInitialResponseAsync_WithWindowsNpmShim_UsesCmdWrapperAndStdinPrompt()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var shimDirectory = Path.Combine(Path.GetTempPath(), $"codex-shim-{Guid.NewGuid():N}");
        Directory.CreateDirectory(shimDirectory);
        var cmdPath = Path.Combine(shimDirectory, "codex.cmd");
        File.WriteAllText(cmdPath, "@echo off", Encoding.UTF8);
        try
        {
            var spawner = new CapturingProcessSpawner(
                exitCode: 0,
                outputBody: """{"title":"triage result"}""",
                stdout: "codex stdout",
                stderr: "codex stderr");
            var strategy = new CodexCliAgentExecutionStrategy(
                new CapturingProcessEnvironmentService(),
                spawner,
                NullLogger<CodexCliAgentExecutionStrategy>.Instance);

            await using var session = await strategy.CreateSessionAsync(CreateRequest(cmdPath)).ConfigureAwait(true);

            var result = await session.ReadInitialResponseAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(CopilotResultState.Success, result.State);
            Assert.Equal("codex stdout", result.Stdout);
            Assert.Equal("codex stderr", result.Stderr);
            Assert.Equal(0, result.ExitCode);
            Assert.NotNull(spawner.StartInfo);
            Assert.EndsWith("cmd.exe", spawner.StartInfo.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/c", spawner.StartInfo.ArgumentList);
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains(cmdPath, StringComparison.OrdinalIgnoreCase));
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains("exec", StringComparison.Ordinal));
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains("--output-last-message", StringComparison.Ordinal));
            Assert.True(spawner.StartInfo.RedirectStandardInput);
            Assert.DoesNotContain(spawner.StartInfo.ArgumentList, argument => argument.Contains("rendered prompt", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(shimDirectory, recursive: true);
        }
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-003: The Codex CLI strategy writes schema and output
    /// files under the configured shared temp root so a service-spawned
    /// interactive-user codex process can read and write the files.
    /// </summary>
    [Fact]
    public async Task ReadInitialResponseAsync_WithConfiguredSharedTempRoot_UsesSharedSchemaAndOutputPaths()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"codex-triage-shared-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var previous = Environment.GetEnvironmentVariable("MCPSERVER_CODEX_TRIAGE_TEMP");
        Environment.SetEnvironmentVariable("MCPSERVER_CODEX_TRIAGE_TEMP", tempRoot);
        try
        {
            var spawner = new CapturingProcessSpawner(exitCode: 0, outputBody: """{"title":"triage result"}""");
            var strategy = new CodexCliAgentExecutionStrategy(
                new CapturingProcessEnvironmentService(),
                spawner,
                NullLogger<CodexCliAgentExecutionStrategy>.Instance);

            await using var session = await strategy.CreateSessionAsync(CreateRequest()).ConfigureAwait(true);

            var result = await session.ReadInitialResponseAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(CopilotResultState.Success, result.State);
            Assert.NotNull(spawner.OutputSchemaPath);
            Assert.NotNull(spawner.OutputLastMessagePath);
            Assert.StartsWith(tempRoot, spawner.OutputSchemaPath, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(tempRoot, spawner.OutputLastMessagePath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"required\"", spawner.OutputSchemaJson, StringComparison.Ordinal);
            Assert.Contains("acceptanceCriteria", spawner.OutputSchemaJson, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCPSERVER_CODEX_TRIAGE_TEMP", previous);
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-003: The strategy resolver recognizes the configured
    /// <c>codex-cli</c> strategy name.
    /// </summary>
    [Fact]
    public void AgentExecutionStrategyNames_SupportsCodexCli()
    {
        Assert.True(AgentExecutionStrategyNames.IsSupported("codex-cli"));
        Assert.Contains("codex-cli", AgentExecutionStrategyNames.SupportedNames);
    }

    private static AgentExecutionSessionRequest CreateRequest(
        string agentPath = "codex.cmd",
        Func<string, string, Task>? outputReceivedAsync = null) =>
        new(
            "rendered prompt",
            "F:\\GitHub\\McpServer",
            "triage",
            "codex-cli",
            new CopilotClientOptions
            {
                AgentPath = agentPath,
                Model = "model-triage",
                WorkingDirectory = "F:\\GitHub\\McpServer",
                Timeout = TimeSpan.FromSeconds(30),
                AgentOutputReceivedAsync = outputReceivedAsync,
            });

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

    private sealed class CapturingProcessSpawner(
        int exitCode,
        string outputBody = "",
        string stdout = "",
        string stderr = "",
        bool waitForCancellation = false) : IProcessSpawner
    {
        public ProcessStartInfo? StartInfo { get; private set; }

        public string? OutputLastMessagePath { get; private set; }

        public string? OutputSchemaPath { get; private set; }

        public string? OutputSchemaJson { get; private set; }

        public ISpawnedProcess Spawn(ProcessStartInfo startInfo)
        {
            StartInfo = startInfo;
            OutputLastMessagePath = GetArgumentValue(startInfo, "--output-last-message");
            OutputSchemaPath = GetArgumentValue(startInfo, "--output-schema");
            if (!string.IsNullOrWhiteSpace(OutputSchemaPath) && File.Exists(OutputSchemaPath))
            {
                OutputSchemaJson = File.ReadAllText(OutputSchemaPath, Encoding.UTF8);
            }

            if (!string.IsNullOrWhiteSpace(OutputLastMessagePath) && outputBody.Length > 0)
            {
                File.WriteAllText(OutputLastMessagePath, outputBody, Encoding.UTF8);
            }

            return new FakeSpawnedProcess(exitCode, stdout, stderr, waitForCancellation);
        }

        private static string? GetArgumentValue(ProcessStartInfo startInfo, string name)
        {
            for (var index = 0; index < startInfo.ArgumentList.Count - 1; index++)
            {
                if (string.Equals(startInfo.ArgumentList[index], name, StringComparison.Ordinal))
                    return startInfo.ArgumentList[index + 1];
            }

            var pattern = Regex.Escape(name) + "\\s+(?:\"(?<quoted>[^\"]+)\"|(?<plain>\\S+))";
            foreach (var argument in startInfo.ArgumentList)
            {
                var match = Regex.Match(argument, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
                if (match.Success)
                    return match.Groups["quoted"].Success ? match.Groups["quoted"].Value : match.Groups["plain"].Value;
            }

            return null;
        }
    }

    private sealed class FakeSpawnedProcess(int exitCode, string stdout, string stderr, bool waitForCancellation) : ISpawnedProcess
    {
        private bool _killed;

        public StreamReader StandardOutput { get; } = CreateReader(stdout);

        public StreamReader StandardError { get; } = CreateReader(stderr);

        public StreamWriter? StandardInput => null;

        public int Id => 1234;

        public bool HasExited => !waitForCancellation || _killed;

        public int ExitCode { get; } = exitCode;

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
            waitForCancellation ? Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken) : Task.CompletedTask;

        public void Kill()
        {
            _killed = true;
        }

        public void Dispose()
        {
            StandardOutput.Dispose();
            StandardError.Dispose();
        }

        private static StreamReader CreateReader(string value) =>
            new(new MemoryStream(Encoding.UTF8.GetBytes(value)), Encoding.UTF8);
    }
}
