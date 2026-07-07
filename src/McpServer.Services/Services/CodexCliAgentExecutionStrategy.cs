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
/// TR-MCP-TRIAGE-003: Runs Codex CLI through reusable one-shot <c>codex exec</c>
/// invocations for non-interactive direct-agent prompts such as background triage.
/// </summary>
internal sealed class CodexCliAgentExecutionStrategy(
    IProcessEnvironmentService processEnvironment,
    IProcessSpawner processSpawner,
    ILogger<CodexCliAgentExecutionStrategy> logger) : IAgentExecutionStrategy
{
    private const string HighestReasoningEffortConfig = "model_reasoning_effort=\"xhigh\"";

    /// <inheritdoc />
    public string Name => AgentExecutionStrategyNames.CodexCli;

    /// <inheritdoc />
    public ValueTask<IAgentExecutionSession> CreateSessionAsync(
        AgentExecutionSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IAgentExecutionSession>(
            new CodexCliAgentExecutionSession(request, processEnvironment, processSpawner, logger));
    }

    private sealed class CodexCliAgentExecutionSession(
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
            var outputPath = Path.Combine(tempDirectory, $"codex-output-{Guid.NewGuid():N}.txt");

            var psi = BuildStartInfo(outputPath);
            logger.LogInformation(
                "Launching Codex CLI one-shot command: {CommandLine}",
                BuildDisplayCommandLine(psi));
            try
            {
                _process = processSpawner.Spawn(psi);
                await WritePromptAsync(_process, prompt, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                logger.LogError(ex, "Failed to spawn Codex CLI: {Agent}", psi.FileName);
                return new AgentCliResult
                {
                    State = AgentCliResultState.Error,
                    Stderr = $"error: Failed to spawn Codex CLI - {ex.Message}",
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
                    CancellationToken.None);
                stderrTask = CaptureStreamAsync(
                    _process.StandardError,
                    "stderr",
                    stderrBuilder,
                    request.Options.AgentOutputReceivedAsync,
                    CancellationToken.None);
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
                    "error: Codex CLI triage run was cancelled or timed out.");
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
            }
        }

        private ProcessStartInfo BuildStartInfo(string outputPath)
        {
            var options = request.Options;
            var workingDirectory = !string.IsNullOrWhiteSpace(options.WorkingDirectory)
                ? options.WorkingDirectory
                : request.WorkspacePath;

            var psi = new ProcessStartInfo
            {
                FileName = options.AgentPath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            psi.ArgumentList.Add("exec");
            if (!string.Equals(options.Model, "auto", StringComparison.OrdinalIgnoreCase))
            {
                psi.ArgumentList.Add("--model");
                psi.ArgumentList.Add(options.Model);
            }

            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(HighestReasoningEffortConfig);
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

            processEnvironment.ApplyAll(psi, options.RunAs, options.GitHubToken);
            psi.FileName = processEnvironment.ResolveExecutable(psi, options.AgentPath);
            WrapWindowsCommandShim(psi);

            foreach (var (key, value) in options.EnvironmentVariables)
            {
                psi.Environment[key] = value;
            }

            return psi;
        }

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
            var configured = Environment.GetEnvironmentVariable("MCPSERVER_CODEX_ONESHOT_TEMP")
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
                var fallbackDirectory = Path.Combine(Path.GetTempPath(), "mcpserver-codex-oneshot");
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
                    "codex-oneshot");
            }

            return Path.Combine(Path.GetTempPath(), "mcpserver-codex-oneshot");
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
    }
}
