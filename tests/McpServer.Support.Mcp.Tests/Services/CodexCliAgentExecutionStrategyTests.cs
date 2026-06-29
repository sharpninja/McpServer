using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using McpServer.Common.AgentCli;
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
    /// TEST-MCP-TRIAGE-003: Codex uses reusable one-shot <c>exec -C ... -o ... -</c>
    /// invocation and never uses Copilot interactive flags or prompt arguments.
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

        Assert.Equal(AgentCliResultState.Success, result.State);
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
        Assert.Contains("-C", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("F:\\GitHub\\McpServer", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("-o", serializedArguments, StringComparison.Ordinal);
        Assert.True(spawner.HasArgument("-"), "Codex must receive '-' so the prompt is read from stdin.");
        Assert.DoesNotContain("--output-schema", serializedArguments, StringComparison.Ordinal);
        Assert.DoesNotContain("--output-last-message", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("--model", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("model-triage", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("-c", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("model_reasoning_effort=\\\"xhigh\\\"", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("--sandbox", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("read-only", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("--skip-git-repo-check", serializedArguments, StringComparison.Ordinal);
        Assert.DoesNotContain("rendered prompt", serializedArguments, StringComparison.Ordinal);
        Assert.True(spawner.StartInfo.RedirectStandardInput);
        Assert.DoesNotContain("-i", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("-p", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--stream", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--yolo", spawner.StartInfo.ArgumentList);
        Assert.Equal("rendered prompt", spawner.CapturedStandardInput);
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

        Assert.Equal(AgentCliResultState.Error, result.State);
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

        Assert.Equal(AgentCliResultState.Error, result.State);
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

        Assert.Equal(AgentCliResultState.Success, result.State);
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

            Assert.Equal(AgentCliResultState.Success, result.State);
            Assert.Equal("codex stdout", result.Stdout);
            Assert.Equal("codex stderr", result.Stderr);
            Assert.Equal(0, result.ExitCode);
            Assert.NotNull(spawner.StartInfo);
            Assert.EndsWith("cmd.exe", spawner.StartInfo.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/c", spawner.StartInfo.ArgumentList);
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains(cmdPath, StringComparison.OrdinalIgnoreCase));
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains("exec", StringComparison.Ordinal));
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains("-o", StringComparison.Ordinal));
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.EndsWith(" -", StringComparison.Ordinal));
            Assert.True(spawner.StartInfo.RedirectStandardInput);
            Assert.DoesNotContain(spawner.StartInfo.ArgumentList, argument => argument.Contains("rendered prompt", StringComparison.Ordinal));
            Assert.Equal("rendered prompt", spawner.CapturedStandardInput);
        }
        finally
        {
            Directory.Delete(shimDirectory, recursive: true);
        }
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-003: The Codex CLI strategy writes one-shot output
    /// files under the configured shared temp root so a service-spawned
    /// interactive-user codex process can write the final response file.
    /// </summary>
    [Fact]
    public async Task ReadInitialResponseAsync_WithConfiguredSharedTempRoot_UsesSharedOutputPath()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"codex-oneshot-shared-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var previous = Environment.GetEnvironmentVariable("MCPSERVER_CODEX_ONESHOT_TEMP");
        Environment.SetEnvironmentVariable("MCPSERVER_CODEX_ONESHOT_TEMP", tempRoot);
        try
        {
            var spawner = new CapturingProcessSpawner(exitCode: 0, outputBody: """{"title":"triage result"}""");
            var strategy = new CodexCliAgentExecutionStrategy(
                new CapturingProcessEnvironmentService(),
                spawner,
                NullLogger<CodexCliAgentExecutionStrategy>.Instance);

            await using var session = await strategy.CreateSessionAsync(CreateRequest()).ConfigureAwait(true);

            var result = await session.ReadInitialResponseAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(AgentCliResultState.Success, result.State);
            Assert.NotNull(spawner.OutputLastMessagePath);
            Assert.StartsWith(tempRoot, spawner.OutputLastMessagePath, StringComparison.OrdinalIgnoreCase);
            Assert.Null(spawner.OutputSchemaPath);
            Assert.Null(spawner.OutputSchemaJson);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCPSERVER_CODEX_ONESHOT_TEMP", previous);
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
            new AgentCliClientOptions
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

        public string? CapturedStandardInput => LastProcess?.StandardInputText;

        private FakeSpawnedProcess? LastProcess { get; set; }

        public ISpawnedProcess Spawn(ProcessStartInfo startInfo)
        {
            StartInfo = startInfo;
            OutputLastMessagePath = GetArgumentValue(startInfo, "-o");
            OutputSchemaPath = GetArgumentValue(startInfo, "--output-schema");
            if (!string.IsNullOrWhiteSpace(OutputSchemaPath) && File.Exists(OutputSchemaPath))
            {
                OutputSchemaJson = File.ReadAllText(OutputSchemaPath, Encoding.UTF8);
            }

            if (!string.IsNullOrWhiteSpace(OutputLastMessagePath) && outputBody.Length > 0)
            {
                File.WriteAllText(OutputLastMessagePath, outputBody, Encoding.UTF8);
            }

            LastProcess = new FakeSpawnedProcess(exitCode, stdout, stderr, waitForCancellation);
            return LastProcess;
        }

        public bool HasArgument(string name)
        {
            if (StartInfo is null)
                return false;

            return StartInfo.ArgumentList.Any(argument => string.Equals(argument, name, StringComparison.Ordinal))
                || StartInfo.ArgumentList.Any(argument => Regex.IsMatch(
                    argument,
                    "(^|\\s)" + Regex.Escape(name) + "(\\s|$)",
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(100)));
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

    private sealed class FakeSpawnedProcess : ISpawnedProcess
    {
        private readonly MemoryStream _standardInput = new();
        private bool _killed;

        public FakeSpawnedProcess(int exitCode, string stdout, string stderr, bool waitForCancellation)
        {
            ExitCode = exitCode;
            StandardOutput = CreateReader(stdout);
            StandardError = CreateReader(stderr);
            WaitForCancellation = waitForCancellation;
            StandardInput = new StreamWriter(_standardInput, Encoding.UTF8, leaveOpen: true);
        }

        public StreamReader StandardOutput { get; }

        public StreamReader StandardError { get; }

        public StreamWriter? StandardInput { get; }

        public int Id => 1234;

        public bool HasExited => !WaitForCancellation || _killed;

        public int ExitCode { get; }

        public string StandardInputText => Encoding.UTF8.GetString(_standardInput.ToArray()).TrimStart('\uFEFF');

        private bool WaitForCancellation { get; }

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
            WaitForCancellation ? Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken) : Task.CompletedTask;

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
