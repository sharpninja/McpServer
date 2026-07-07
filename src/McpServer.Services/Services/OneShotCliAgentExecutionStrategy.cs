using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

using McpServer.Common.AgentCli;

using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TRIAGE-003: Runs a configured CLI agent in one-shot mode for
/// non-interactive prompts where the selected agent is part of the request.
/// </summary>
internal sealed class OneShotCliAgentExecutionStrategy(
    IProcessEnvironmentService processEnvironment,
    IProcessSpawner processSpawner,
    ILogger<OneShotCliAgentExecutionStrategy> logger) : IAgentExecutionStrategy
{
    private const string CodexHighestReasoningEffortConfig = "model_reasoning_effort=\"xhigh\"";
    private const string ClaudeHighestEffort = "max";
    private const string ClineHighestThinkingLevel = "xhigh";
    private const string GrokHighestEffort = "max";

    /// <inheritdoc />
    public string Name => AgentExecutionStrategyNames.OneShotCli;

    /// <inheritdoc />
    public ValueTask<IAgentExecutionSession> CreateSessionAsync(
        AgentExecutionSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IAgentExecutionSession>(
            new OneShotCliAgentExecutionSession(request, processEnvironment, processSpawner, logger));
    }

    private sealed class OneShotCliAgentExecutionSession(
        AgentExecutionSessionRequest request,
        IProcessEnvironmentService processEnvironment,
        IProcessSpawner processSpawner,
        ILogger logger) : IAgentExecutionSession
    {
        private ISpawnedProcess? _process;

        public bool IsAlive => _process is { HasExited: false };

        public int? ProcessId => _process?.Id;

        public async Task<AgentCliResult> ReadInitialResponseAsync(CancellationToken cancellationToken = default) =>
            await RunPromptAsync(request.InitialPrompt, cancellationToken).ConfigureAwait(false);

        public async IAsyncEnumerable<string> ReadInitialResponseStreamingAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var result = await ReadInitialResponseAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(result.Body))
                yield return result.Body;
        }

        public async Task<AgentCliResult> SendAsync(string prompt, CancellationToken cancellationToken = default) =>
            await RunPromptAsync(prompt, cancellationToken).ConfigureAwait(false);

        public async IAsyncEnumerable<string> SendStreamingAsync(
            string prompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var result = await SendAsync(prompt, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(result.Body))
                yield return result.Body;
        }

        public Task SendEscapeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EndAsync(TimeSpan timeout)
        {
            if (_process is { HasExited: false })
            {
                _process.Kill();
            }

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (_process is { HasExited: false })
            {
                _process.Kill();
            }

            _process?.Dispose();
            return ValueTask.CompletedTask;
        }

        private async Task<AgentCliResult> RunPromptAsync(string prompt, CancellationToken cancellationToken)
        {
            var tempDirectory = ResolveSharedTempDirectory();
            var outputPath = Path.Combine(tempDirectory, $"oneshot-output-{Guid.NewGuid():N}.txt");
            OneShotLaunch? launch = null;

            try
            {
                launch = await BuildLaunchAsync(
                    prompt,
                    tempDirectory,
                    outputPath,
                    cancellationToken).ConfigureAwait(false);
                logger.LogInformation(
                    "Launching one-shot CLI agent command: {CommandLine}",
                    BuildDisplayCommandLine(launch.StartInfo));

                _process = processSpawner.Spawn(launch.StartInfo);
                if (launch.WritePromptToStandardInput)
                    await WritePromptAsync(_process, prompt, cancellationToken).ConfigureAwait(false);
                else
                    _process.StandardInput?.Close();
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
            {
                logger.LogError(ex, "Failed to spawn one-shot CLI agent: {Agent}", request.Options.AgentPath);
                return new AgentCliResult
                {
                    State = AgentCliResultState.Error,
                    Stderr = $"error: Failed to spawn one-shot CLI agent - {ex.Message}",
                };
            }

            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();
            var stdoutTask = Task.CompletedTask;
            var stderrTask = Task.CompletedTask;
            try
            {
                stdoutTask = CaptureStreamAsync(
                    _process.StandardOutput,
                    "stdout",
                    stdoutBuilder,
                    request.Options.AgentOutputReceivedAsync,
                    cancellationToken);
                stderrTask = CaptureStreamAsync(
                    _process.StandardError,
                    "stderr",
                    stderrBuilder,
                    request.Options.AgentOutputReceivedAsync,
                    cancellationToken);
                await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                await WaitForCaptureAsync(stdoutTask).ConfigureAwait(false);
                await WaitForCaptureAsync(stderrTask).ConfigureAwait(false);

                var stdout = stdoutBuilder.ToString();
                var stderr = stderrBuilder.ToString();
                var body = File.Exists(outputPath)
                    ? await File.ReadAllTextAsync(outputPath, cancellationToken).ConfigureAwait(false)
                    : stdout;

                if (_process.ExitCode == 0)
                {
                    return new AgentCliResult
                    {
                        State = AgentCliResultState.Success,
                        Body = string.IsNullOrWhiteSpace(body) ? stdout : body.Trim(),
                        Stdout = stdout,
                        Stderr = stderr,
                        ExitCode = _process.ExitCode,
                    };
                }

                return new AgentCliResult
                {
                    State = AgentCliResultState.Error,
                    Body = string.IsNullOrWhiteSpace(body) ? stdout : body.Trim(),
                    Stdout = stdout,
                    Stderr = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr,
                    ExitCode = _process.ExitCode,
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (_process is { HasExited: false })
                    _process.Kill();

                await WaitForCaptureAsync(stdoutTask).ConfigureAwait(false);
                await WaitForCaptureAsync(stderrTask).ConfigureAwait(false);
                var stdout = stdoutBuilder.ToString();
                var stderr = AppendProcessError(
                    stderrBuilder.ToString(),
                    "error: One-shot CLI agent run was cancelled or timed out.");
                return new AgentCliResult
                {
                    State = AgentCliResultState.Error,
                    Body = stdout,
                    Stdout = stdout,
                    Stderr = stderr,
                };
            }
            finally
            {
                TryDelete(outputPath);
                if (launch?.PromptFilePath is not null)
                    TryDelete(launch.PromptFilePath);
            }
        }

        private async Task<OneShotLaunch> BuildLaunchAsync(
            string prompt,
            string tempDirectory,
            string outputPath,
            CancellationToken cancellationToken)
        {
            var options = request.Options;
            var agentPath = string.IsNullOrWhiteSpace(options.AgentPath)
                ? "cline"
                : options.AgentPath;
            var agent = NormalizeAgentName(agentPath);
            var workingDirectory = !string.IsNullOrWhiteSpace(options.WorkingDirectory)
                ? options.WorkingDirectory
                : request.WorkspacePath;

            var psi = new ProcessStartInfo
            {
                FileName = agentPath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            string? promptFilePath = null;
            var writePromptToStandardInput = true;

            switch (agent)
            {
                case "codex":
                    AddCodexArguments(psi, options, workingDirectory, outputPath);
                    break;

                case "claude":
                    AddClaudeArguments(psi, options, workingDirectory);
                    break;

                case "grok":
                    promptFilePath = Path.Combine(tempDirectory, $"oneshot-prompt-{Guid.NewGuid():N}.txt");
                    await File.WriteAllTextAsync(promptFilePath, prompt, cancellationToken).ConfigureAwait(false);
                    AddGrokArguments(psi, workingDirectory, promptFilePath);
                    writePromptToStandardInput = false;
                    break;

                case "cline":
                    AddClineArguments(psi, workingDirectory, prompt);
                    writePromptToStandardInput = false;
                    break;

                default:
                    throw new NotSupportedException($"Unsupported one-shot CLI agent '{agent}'.");
            }

            processEnvironment.ApplyAll(psi, options.RunAs, options.GitHubToken);
            psi.FileName = processEnvironment.ResolveExecutable(psi, agentPath);
            WrapWindowsCommandShim(psi);

            foreach (var (key, value) in options.EnvironmentVariables)
            {
                psi.Environment[key] = value;
            }

            return new OneShotLaunch(psi, writePromptToStandardInput, promptFilePath);
        }

        private static void AddCodexArguments(
            ProcessStartInfo psi,
            AgentCliClientOptions options,
            string workingDirectory,
            string outputPath)
        {
            psi.ArgumentList.Add("exec");
            if (!IsAutoModel(options.Model))
            {
                psi.ArgumentList.Add("--model");
                psi.ArgumentList.Add(options.Model);
            }

            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(CodexHighestReasoningEffortConfig);
            psi.ArgumentList.Add("--sandbox");
            psi.ArgumentList.Add("read-only");
            psi.ArgumentList.Add("--color");
            psi.ArgumentList.Add("never");
            psi.ArgumentList.Add("--skip-git-repo-check");
            psi.ArgumentList.Add("-C");
            psi.ArgumentList.Add(workingDirectory);
            psi.ArgumentList.Add("-o");
            psi.ArgumentList.Add(outputPath);
            psi.ArgumentList.Add("-");
        }

        private static void AddClaudeArguments(
            ProcessStartInfo psi,
            AgentCliClientOptions options,
            string workingDirectory)
        {
            psi.ArgumentList.Add("-p");
            psi.ArgumentList.Add("--permission-mode");
            psi.ArgumentList.Add("plan");
            psi.ArgumentList.Add("--add-dir");
            psi.ArgumentList.Add(workingDirectory);
            psi.ArgumentList.Add("--model");
            psi.ArgumentList.Add(IsAutoModel(options.Model) ? "opus" : options.Model);
            psi.ArgumentList.Add("--effort");
            psi.ArgumentList.Add(ClaudeHighestEffort);
        }

        private static void AddGrokArguments(
            ProcessStartInfo psi,
            string workingDirectory,
            string promptFilePath)
        {
            psi.ArgumentList.Add("--prompt-file");
            psi.ArgumentList.Add(promptFilePath);
            psi.ArgumentList.Add("--cwd");
            psi.ArgumentList.Add(workingDirectory);
            psi.ArgumentList.Add("--permission-mode");
            psi.ArgumentList.Add("plan");
            psi.ArgumentList.Add("--output-format");
            psi.ArgumentList.Add("plain");
            psi.ArgumentList.Add("--effort");
            psi.ArgumentList.Add(GrokHighestEffort);
            psi.ArgumentList.Add("--reasoning-effort");
            psi.ArgumentList.Add(GrokHighestEffort);
        }

        private static void AddClineArguments(
            ProcessStartInfo psi,
            string workingDirectory,
            string prompt)
        {
            psi.ArgumentList.Add("-p");
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(workingDirectory);
            psi.ArgumentList.Add("--thinking");
            psi.ArgumentList.Add(ClineHighestThinkingLevel);
            psi.ArgumentList.Add(prompt);
        }

        private static string NormalizeAgentName(string agentPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(agentPath);
            return string.IsNullOrWhiteSpace(fileName)
                ? agentPath.Trim().ToLowerInvariant()
                : fileName.Trim().ToLowerInvariant();
        }

        private static bool IsAutoModel(string? model) =>
            string.IsNullOrWhiteSpace(model) ||
            string.Equals(model, "auto", StringComparison.OrdinalIgnoreCase);

        private static string BuildDisplayCommandLine(ProcessStartInfo psi) =>
            BuildCmdCommandLine(psi.FileName, psi.ArgumentList);

        private static void WrapWindowsCommandShim(ProcessStartInfo psi)
        {
            if (!OperatingSystem.IsWindows())
                return;

            var extension = Path.GetExtension(psi.FileName);
            if (!string.Equals(extension, ".cmd", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".bat", StringComparison.OrdinalIgnoreCase))
                return;

            var command = BuildCmdCommandLine(psi.FileName, psi.ArgumentList);
            psi.ArgumentList.Clear();
            psi.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
            psi.ArgumentList.Add("/d");
            psi.ArgumentList.Add("/s");
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(command);
        }

        private static string BuildCmdCommandLine(string fileName, IEnumerable<string> arguments)
            => string.Join(' ', new[] { QuoteCmdArgument(fileName) }.Concat(arguments.Select(QuoteCmdArgument)));

        private static string QuoteCmdArgument(string value)
        {
            if (value.Length == 0)
                return "\"\"";

            var escaped = value
                .Replace("^", "^^", StringComparison.Ordinal)
                .Replace("&", "^&", StringComparison.Ordinal)
                .Replace("|", "^|", StringComparison.Ordinal)
                .Replace("<", "^<", StringComparison.Ordinal)
                .Replace(">", "^>", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);

            return escaped.Any(char.IsWhiteSpace) || escaped.Contains('"', StringComparison.Ordinal)
                ? $"\"{escaped}\""
                : escaped;
        }

        private static async Task WritePromptAsync(
            ISpawnedProcess process,
            string prompt,
            CancellationToken cancellationToken)
        {
            if (process.StandardInput is null)
                return;

            await process.StandardInput.WriteAsync(prompt.AsMemory(), cancellationToken).ConfigureAwait(false);
            await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
            process.StandardInput.Close();
        }

        private static async Task CaptureStreamAsync(
            StreamReader reader,
            string streamName,
            StringBuilder builder,
            Func<string, string, Task>? outputReceivedAsync,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (line is null)
                    return;

                if (builder.Length > 0)
                    builder.AppendLine();
                builder.Append(line);

                if (outputReceivedAsync is not null)
                    await outputReceivedAsync(streamName, line).ConfigureAwait(false);
            }
        }

        private static async Task WaitForCaptureAsync(Task captureTask)
        {
            try
            {
                await captureTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected when the agent run times out.
            }
            catch (ObjectDisposedException)
            {
                // The process streams can close while cancellation is killing the process.
            }
            catch (IOException)
            {
                // The process streams can close while cancellation is killing the process.
            }
        }

        private static string AppendProcessError(string? current, string error)
        {
            if (string.IsNullOrWhiteSpace(current))
                return error;
            if (current.EndsWith(Environment.NewLine, StringComparison.Ordinal))
                return string.Concat(current, error);
            return string.Concat(current, Environment.NewLine, error);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }

        private static string ResolveSharedTempDirectory()
        {
            var configured = Environment.GetEnvironmentVariable("MCPSERVER_ONESHOT_TEMP")
                             ?? Environment.GetEnvironmentVariable("MCPSERVER_CODEX_ONESHOT_TEMP")
                             ?? Environment.GetEnvironmentVariable("MCPSERVER_CODEX_TRIAGE_TEMP");
            var hasConfiguredDirectory = !string.IsNullOrWhiteSpace(configured);
            var preferredDirectory = hasConfiguredDirectory
                ? configured!
                : ResolveDefaultSharedTempDirectory();

            try
            {
                EnsureSharedTempDirectory(preferredDirectory);
                return preferredDirectory;
            }
            catch (Exception ex) when (!hasConfiguredDirectory && OperatingSystem.IsWindows() &&
                                       (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException))
            {
                var fallbackDirectory = Path.Combine(Path.GetTempPath(), "mcpserver-oneshot");
                Directory.CreateDirectory(fallbackDirectory);
                return fallbackDirectory;
            }
        }

        private static string ResolveDefaultSharedTempDirectory()
        {
            if (OperatingSystem.IsWindows())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "McpServer",
                    "Temp",
                    "oneshot");
            }

            return Path.Combine(Path.GetTempPath(), "mcpserver-oneshot");
        }

        private static void EnsureSharedTempDirectory(string path)
        {
            Directory.CreateDirectory(path);
            if (OperatingSystem.IsWindows())
                EnsureWindowsUsersCanModify(path);
        }

        [SupportedOSPlatform("windows")]
        private static void EnsureWindowsUsersCanModify(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            var security = directoryInfo.GetAccessControl();
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                FileSystemRights.Modify | FileSystemRights.Synchronize,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));
            directoryInfo.SetAccessControl(security);
        }

        private sealed record OneShotLaunch(
            ProcessStartInfo StartInfo,
            bool WritePromptToStandardInput,
            string? PromptFilePath);
    }
}
