using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Common.Copilot;

/// <summary>TR-CLI-001: Invokes the Copilot CLI agent, captures output, and returns structured results.</summary>
public sealed class CopilotClient(
    IOptions<CopilotClientOptions> defaultOptions,
    ILogger<CopilotClient> logger) : ICopilotClient
{

    /// <inheritdoc />
    public async Task<CopilotResult> InvokeAsync(
        string prompt,
        CopilotClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        var opts = options ?? defaultOptions.Value;
        return await RunProcessAsync(prompt, opts, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CopilotResult<T>> InvokeAsync<T>(
        string prompt,
        CopilotClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        var opts = options ?? defaultOptions.Value;
        var result = await RunProcessAsync(prompt, opts, cancellationToken).ConfigureAwait(false);

        // Attempt typed deserialization
        var (contentType, parsed) = ContentParser.DetectAndParse<T>(result.Body);

        return new CopilotResult<T>
        {
            State = result.State,
            Body = result.Body,
            Stderr = result.Stderr,
            ExitCode = result.ExitCode,
            Parsed = parsed,
            ContentType = contentType,
        };
    }

    private async Task<CopilotResult> RunProcessAsync(
        string prompt,
        CopilotClientOptions opts,
        CancellationToken cancellationToken)
    {
        var agentPath = opts.AgentPath;
        var model = opts.Model;
        var outputFormat = opts.OutputFormat;
        var cwd = opts.WorkingDirectory ?? Environment.CurrentDirectory;
        var isWindows = OperatingSystem.IsWindows();

        // Write prompt to temp file to avoid shell escaping issues
        var tmpFile = Path.Combine(Path.GetTempPath(), $"fwh-copilot-{Environment.TickCount64}-{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(tmpFile, prompt, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Failed to write prompt to temp file {TmpFile}", tmpFile);
            return new CopilotResult
            {
                State = CopilotResultState.SpawnError,
                Stderr = $"Failed to write prompt to temp file: {ex.Message}",
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Failed to write prompt to temp file {TmpFile}", tmpFile);
            return new CopilotResult
            {
                State = CopilotResultState.SpawnError,
                Stderr = $"Failed to write prompt to temp file: {ex.Message}",
            };
        }

        try
        {
            return await SpawnAgentAsync(agentPath, model, outputFormat, tmpFile, cwd, opts, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            TryDeleteFile(tmpFile);
        }
    }

    private async Task<CopilotResult> SpawnAgentAsync(
        string agentPath,
        string model,
        string outputFormat,
        string tmpFile,
        string cwd,
        CopilotClientOptions opts,
        CancellationToken cancellationToken)
    {
        var isWindows = OperatingSystem.IsWindows();
        var readCmd = isWindows
            ? $"Get-Content -Raw '{tmpFile.Replace("'", "''", StringComparison.Ordinal)}'"
            : $"cat '{tmpFile.Replace("'", "'\\''", StringComparison.Ordinal)}'";
        var modelArg = string.Equals(model, "auto", StringComparison.OrdinalIgnoreCase) ? "" : $" --model {model}";
        var agentCmd = $"{agentPath} -p \"$({readCmd})\"{modelArg} --output-format {outputFormat} 2>&1";

        var shell = isWindows ? "pwsh" : "sh";
        var shellArgs = isWindows ? $"-NoProfile -Command {agentCmd}" : $"-c {agentCmd}";

        logger.LogDebug("Spawning: {Shell} {Args} in {Cwd}", shell, shellArgs, cwd);

        var psi = new ProcessStartInfo
        {
            FileName = shell,
            Arguments = shellArgs,
            WorkingDirectory = cwd,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (opts.EnvironmentVariables is { Count: > 0 } envVars)
        {
            foreach (var (key, value) in envVars)
                psi.Environment[key] = value;
        }

        Process process;
        try
        {
            process = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to spawn process: {Shell}", shell);
            return new CopilotResult
            {
                State = CopilotResultState.SpawnError,
                Stderr = $"Failed to spawn process: {ex.Message}",
            };
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            logger.LogError(ex, "Failed to spawn process: {Shell}", shell);
            return new CopilotResult
            {
                State = CopilotResultState.SpawnError,
                Stderr = $"Failed to spawn process: {ex.Message}",
            };
        }

        try
        {
            // Read stdout and stderr concurrently to avoid deadlocks
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            var timeout = opts.Timeout;
            var hasTimeout = timeout > TimeSpan.Zero && timeout != System.Threading.Timeout.InfiniteTimeSpan;

            if (hasTimeout)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);

                try
                {
                    await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Timeout — kill process
                    logger.LogWarning("Copilot CLI timed out after {Timeout}", timeout);
                    TryKillProcess(process);
                    var partialStdout = await ReadPartialAsync(stdoutTask).ConfigureAwait(false);
                    var partialStderr = await ReadPartialAsync(stderrTask).ConfigureAwait(false);
                    return new CopilotResult
                    {
                        State = CopilotResultState.Timeout,
                        Body = partialStdout.Trim(),
                        Stderr = partialStderr.Trim(),
                    };
                }
            }
            else
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }

            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            var body = stdout.Trim();
            var (contentType, parsed) = ContentParser.DetectAndParse(body);

            logger.LogDebug("Copilot CLI exited with code {ExitCode}, content type: {ContentType}", process.ExitCode, contentType);

            return new CopilotResult
            {
                State = process.ExitCode == 0 ? CopilotResultState.Success : CopilotResultState.Error,
                Body = body,
                Stderr = stderr.Trim(),
                ExitCode = process.ExitCode,
                Parsed = parsed,
                ContentType = contentType,
            };
        }
        finally
        {
            process.Dispose();
        }
    }

    private static async Task<string> ReadPartialAsync(Task<string> readTask)
    {
        try
        {
            return await readTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return string.Empty;
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Access denied or other OS error
        }
    }

    private static void TryDeleteFile(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { /* Best-effort cleanup */ }
        catch (UnauthorizedAccessException) { /* Best-effort cleanup */ }
    }
}
