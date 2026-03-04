using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Common.Copilot;

/// <summary>TR-CLI-001: Invokes the Copilot CLI agent, captures output, and returns structured results.</summary>
public sealed class CopilotClient(
    IOptionsMonitor<CopilotClientOptions> defaultOptions,
    IProcessEnvironmentService processEnvironment,
    IProcessSpawner processSpawner,
    ILogger<CopilotClient> logger) : ICopilotClient
{

    /// <inheritdoc />
    public async Task<CopilotResult> InvokeAsync(
        string prompt,
        CopilotClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        var opts = options ?? defaultOptions.CurrentValue;
        return await RunProcessAsync(prompt, opts, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CopilotResult<T>> InvokeAsync<T>(
        string prompt,
        CopilotClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        var opts = options ?? defaultOptions.CurrentValue;
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

    /// <inheritdoc />
    public CopilotInteractiveSession CreateInteractiveSession(
        string initialPrompt,
        CopilotClientOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(initialPrompt);
        var opts = options ?? defaultOptions.CurrentValue;
        var psi = BuildProcessStartInfo(opts, initialPrompt, interactive: true);

        logger.LogDebug("Launching interactive session: {Agent} in {Cwd}", opts.AgentPath, psi.WorkingDirectory);

        var process = processSpawner.Spawn(psi);
        return new CopilotInteractiveSession(process, logger);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> InvokeStreamingAsync(
        string prompt,
        CopilotClientOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        var opts = options ?? defaultOptions.CurrentValue;

        var psi = BuildProcessStartInfo(opts, prompt);

        logger.LogDebug("Streaming: {Agent} in {Cwd}", opts.AgentPath, psi.WorkingDirectory);

        ISpawnedProcess? proc;
        string? spawnError = null;
        try
        {
            proc = processSpawner.Spawn(psi);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            logger.LogError(ex, "Failed to spawn streaming process: {Agent}", opts.AgentPath);
            spawnError = $"error: Failed to spawn Copilot CLI — {ex.Message}";
            proc = null;
        }

        if (spawnError is not null)
        {
            yield return spawnError;
            yield break;
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (opts.Timeout > TimeSpan.Zero && opts.Timeout != System.Threading.Timeout.InfiniteTimeSpan)
                timeoutCts.CancelAfter(opts.Timeout);

            // Drain stderr in background to prevent deadlocks and capture error output.
            var stderrTask = proc!.StandardError.ReadToEndAsync(timeoutCts.Token);

            var reader = proc.StandardOutput;
            var lineCount = 0;
            while (!timeoutCts.Token.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync(timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex)
                {
                    logger.LogWarning("{ExceptionDetail}", ex.ToString());
                    break;
                }

                if (line is null)
                    break;

                lineCount++;
                yield return LineSanitizer.Sanitize(line);
            }

            var timedOut = timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
            if (timedOut)
            {
                logger.LogWarning("Copilot CLI streaming timed out after {Timeout}", opts.Timeout);
                yield return $"error: Copilot CLI timed out after {opts.Timeout}.";
            }

            if (!proc.HasExited)
                TryKill(proc);

            try
            {
                await proc.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning(ex, "Copilot CLI process did not exit after kill within grace period.");
            }

            // Log stderr if present (best-effort, don't block on timeout).
            var stderr = await ReadPartialAsync(stderrTask).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(stderr))
                logger.LogWarning("Copilot CLI stderr: {Stderr}", stderr.Trim());

            logger.LogInformation("Copilot CLI streaming finished: {LineCount} lines, exit code {ExitCode}", lineCount, proc.ExitCode);
        }
        finally
        {
            proc!.Dispose();
        }
    }

    private async Task<CopilotResult> RunProcessAsync(
        string prompt,
        CopilotClientOptions opts,
        CancellationToken cancellationToken)
    {
        var psi = BuildProcessStartInfo(opts, prompt);

        logger.LogDebug("Spawning: {Agent} in {Cwd}", opts.AgentPath, psi.WorkingDirectory);

        ISpawnedProcess proc;
        try
        {
            proc = processSpawner.Spawn(psi);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to spawn process: {Agent}", opts.AgentPath);
            return new CopilotResult
            {
                State = CopilotResultState.SpawnError,
                Stderr = $"Failed to spawn process: {ex.Message}",
            };
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            logger.LogError(ex, "Failed to spawn process: {Agent}", opts.AgentPath);
            return new CopilotResult
            {
                State = CopilotResultState.SpawnError,
                Stderr = $"Failed to spawn process: {ex.Message}",
            };
        }

        try
        {
            // Read stdout and stderr concurrently to avoid deadlocks
            var stdoutTask = proc.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = proc.StandardError.ReadToEndAsync(cancellationToken);

            var timeout = opts.Timeout;
            var hasTimeout = timeout > TimeSpan.Zero && timeout != System.Threading.Timeout.InfiniteTimeSpan;

            if (hasTimeout)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);

                try
                {
                    await proc.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Timeout — kill process
                    logger.LogWarning("Copilot CLI timed out after {Timeout}", timeout);
                    TryKill(proc);
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
                await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }

            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            var body = stdout.Trim();
            var (contentType, parsed) = ContentParser.DetectAndParse(body);

            logger.LogDebug("Copilot CLI exited with code {ExitCode}, content type: {ContentType}", proc.ExitCode, contentType);

            return new CopilotResult
            {
                State = proc.ExitCode == 0 ? CopilotResultState.Success : CopilotResultState.Error,
                Body = body,
                Stderr = stderr.Trim(),
                ExitCode = proc.ExitCode,
                Parsed = parsed,
                ContentType = contentType,
            };
        }
        finally
        {
            proc.Dispose();
        }
    }

    /// <summary>
    /// Builds a <see cref="ProcessStartInfo"/> that invokes the agent binary directly
    /// (no shell wrapper), using <see cref="ProcessStartInfo.ArgumentList"/> for safe escaping.
    /// This avoids PowerShell/sh buffering so stdout streams in real time.
    /// </summary>
    private ProcessStartInfo BuildProcessStartInfo(CopilotClientOptions opts, string prompt, bool interactive = false)
    {
        var cwd = opts.WorkingDirectory ?? Environment.CurrentDirectory;

        var psi = new ProcessStartInfo
        {
            FileName = opts.AgentPath,
            WorkingDirectory = cwd,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = interactive,
        };

        psi.ArgumentList.Add(interactive ? "-i" : "-p");
        psi.ArgumentList.Add(prompt);

        if (!string.Equals(opts.Model, "auto", StringComparison.OrdinalIgnoreCase))
        {
            psi.ArgumentList.Add("--model");
            psi.ArgumentList.Add(opts.Model);
        }

        // Don't suppress interactive prompts — the sentinel is needed for turn detection.
        if (opts.Silent && !interactive)
            psi.ArgumentList.Add("--silent");

        // Force streaming even when stdout is a pipe (not a TTY).
        psi.ArgumentList.Add("--stream");
        psi.ArgumentList.Add("on");

        // Auto-confirm tool invocations without user prompts.
        psi.ArgumentList.Add("--yolo");

        processEnvironment.ApplyAll(psi, opts.RunAs, opts.GitHubToken);
        psi.FileName = processEnvironment.ResolveExecutable(psi, opts.AgentPath);

        if (opts.EnvironmentVariables is { Count: > 0 } envVars)
        {
            foreach (var (key, value) in envVars)
                psi.Environment[key] = value;
        }

        return psi;
    }

    private async Task<string> ReadPartialAsync(Task<string> readTask)
    {
        try
        {
            return await readTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return string.Empty;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return string.Empty;
        }
    }

    private void TryKill(ISpawnedProcess proc)
    {
        try
        {
            if (!proc.HasExited)
                proc.Kill();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("{ExceptionDetail}", ex.ToString());
            // Process already exited
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            logger.LogWarning("{ExceptionDetail}", ex.ToString());
            // Access denied or other OS error
        }
    }
}
