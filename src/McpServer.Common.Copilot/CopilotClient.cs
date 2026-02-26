using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Common.Copilot;

/// <summary>TR-CLI-001: Invokes the Copilot CLI agent, captures output, and returns structured results.</summary>
public sealed class CopilotClient(
    IOptionsMonitor<CopilotClientOptions> defaultOptions,
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
    public async IAsyncEnumerable<string> InvokeStreamingAsync(
        string prompt,
        CopilotClientOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        var opts = options ?? defaultOptions.CurrentValue;

        var psi = BuildProcessStartInfo(opts, prompt);

        logger.LogDebug("Streaming: {Agent} in {Cwd}", opts.AgentPath, psi.WorkingDirectory);

        Process? process;
        string? spawnError = null;
        try
        {
            process = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            logger.LogError(ex, "Failed to spawn streaming process: {Agent}", opts.AgentPath);
            spawnError = $"error: Failed to spawn Copilot CLI — {ex.Message}";
            process = null;
        }

        if (spawnError is not null)
        {
            yield return spawnError;
            yield break;
        }

        // process is guaranteed non-null when spawnError is null.
        var proc = process!;

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (opts.Timeout > TimeSpan.Zero && opts.Timeout != System.Threading.Timeout.InfiniteTimeSpan)
                timeoutCts.CancelAfter(opts.Timeout);

            // Drain stderr in background to prevent deadlocks and capture error output.
            var stderrTask = proc.StandardError.ReadToEndAsync(timeoutCts.Token);

            var reader = proc.StandardOutput;
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

                yield return line;
            }

            if (!proc.HasExited)
                TryKillProcess(proc);

            await proc.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

            // Log stderr if present (best-effort, don't block on timeout).
            var stderr = await ReadPartialAsync(stderrTask).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(stderr))
                logger.LogWarning("Copilot CLI stderr: {Stderr}", stderr.Trim());
        }
        finally
        {
            proc.Dispose();
        }
    }

    private async Task<CopilotResult> RunProcessAsync(
        string prompt,
        CopilotClientOptions opts,
        CancellationToken cancellationToken)
    {
        var psi = BuildProcessStartInfo(opts, prompt);

        logger.LogDebug("Spawning: {Agent} {Args} in {Cwd}", opts.AgentPath, psi.Arguments, psi.WorkingDirectory);

        Process process;
        try
        {
            process = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null");
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

    /// <summary>
    /// Builds a <see cref="ProcessStartInfo"/> that invokes the agent binary directly
    /// (no shell wrapper), using <see cref="ProcessStartInfo.ArgumentList"/> for safe escaping.
    /// This avoids PowerShell/sh buffering so stdout streams in real time.
    /// </summary>
    private ProcessStartInfo BuildProcessStartInfo(CopilotClientOptions opts, string prompt)
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
        };

        psi.ArgumentList.Add("-p");
        psi.ArgumentList.Add(prompt);

        if (!string.Equals(opts.Model, "auto", StringComparison.OrdinalIgnoreCase))
        {
            psi.ArgumentList.Add("--model");
            psi.ArgumentList.Add(opts.Model);
        }

        if (opts.Silent)
            psi.ArgumentList.Add("--silent");

        // Force streaming even when stdout is a pipe (not a TTY).
        psi.ArgumentList.Add("--stream");
        psi.ArgumentList.Add("on");

        ApplyRunAsEnvironment(psi, opts.RunAs);
        ApplyGitHubToken(psi, opts.GitHubToken);

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

    private void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
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

    /// <summary>
    /// Sets <c>GH_TOKEN</c> on the process if a GitHub token is configured.
    /// Falls back to the current process's <c>GH_TOKEN</c> environment variable.
    /// This is required when the service account cannot access the user's keyring.
    /// </summary>
    private static void ApplyGitHubToken(ProcessStartInfo psi, string? token)
    {
        var effective = !string.IsNullOrWhiteSpace(token)
            ? token
            : Environment.GetEnvironmentVariable("GH_TOKEN");

        if (!string.IsNullOrWhiteSpace(effective))
            psi.Environment["GH_TOKEN"] = effective;
    }

    /// <summary>
    /// When <paramref name="runAsUser"/> is specified (Windows only), loads the user's
    /// profile environment into <paramref name="psi"/> so the spawned process can find
    /// CLIs on the user's PATH and access cached auth tokens in their profile.
    /// </summary>
    private void ApplyRunAsEnvironment(ProcessStartInfo psi, string? runAsUser)
    {
        if (string.IsNullOrWhiteSpace(runAsUser) || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var userProfile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Contains(runAsUser, StringComparison.OrdinalIgnoreCase)
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : Path.Combine(GetUsersRoot(), runAsUser));

        if (!Directory.Exists(userProfile))
        {
            logger.LogWarning("RunAs user profile not found: {UserProfile}", userProfile);
            return;
        }

        var appData = Path.Combine(userProfile, "AppData", "Roaming");
        var localAppData = Path.Combine(userProfile, "AppData", "Local");

        psi.Environment["USERPROFILE"] = userProfile;
        psi.Environment["HOME"] = userProfile;
        psi.Environment["APPDATA"] = appData;
        psi.Environment["LOCALAPPDATA"] = localAppData;

        // Merge the user's PATH: read from registry HKEY_USERS\{username} or standard locations.
        var userPath = ResolveUserPath(runAsUser, localAppData);
        if (!string.IsNullOrWhiteSpace(userPath))
        {
            var currentPath = psi.Environment.TryGetValue("PATH", out var existing) ? existing : Environment.GetEnvironmentVariable("PATH");
            psi.Environment["PATH"] = $"{userPath};{currentPath}";
        }

        logger.LogDebug("Applied RunAs environment for user {User}: USERPROFILE={Profile}", runAsUser, userProfile);
    }

    /// <summary>
    /// Resolves the user-specific PATH entries by reading from the registry
    /// (<c>HKEY_USERS\{SID}\Environment\Path</c>) and appending common WinGet/Scoop directories.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private string ResolveUserPath(string username, string localAppData)
    {
        var parts = new List<string>();

        // Try reading the user's PATH from the registry via their SID.
        try
        {
            using var usersKey = Microsoft.Win32.Registry.Users;
            foreach (var sid in usersKey.GetSubKeyNames())
            {
                using var envKey = usersKey.OpenSubKey($@"{sid}\Environment");
                if (envKey is null) continue;

                var regPath = envKey.GetValue("Path") as string;
                if (string.IsNullOrWhiteSpace(regPath)) continue;

                // Heuristic: the correct SID's PATH will reference the username's profile.
                if (regPath.Contains(username, StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add(regPath);
                    break;
                }
            }
        }
        catch (System.Security.SecurityException ex)
        {
            logger.LogWarning("{ExceptionDetail}", ex.ToString());
            // LocalSystem may not be able to read all registry hives.
        }

        // Always include common tool directories that are known to host CLIs.
        var wingetLinks = Path.Combine(localAppData, "Microsoft", "WinGet", "Links");
        if (Directory.Exists(wingetLinks) && !parts.Any(p => p.Contains(wingetLinks, StringComparison.OrdinalIgnoreCase)))
            parts.Add(wingetLinks);

        return string.Join(";", parts);
    }

    private static string GetUsersRoot()
    {
        // "C:\Users" on typical Windows installs.
        var profileRoot = Environment.GetEnvironmentVariable("PUBLIC");
        return profileRoot is not null
            ? Path.GetDirectoryName(profileRoot) ?? @"C:\Users"
            : @"C:\Users";
    }
}
