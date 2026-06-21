using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace McpServer.Repl.IntegrationTests;

/// <summary>
/// Helper class to launch and manage mcpserver-repl child processes for integration testing.
/// </summary>
public sealed class ReplChildProcessHelper : IDisposable
{
    private const string LiveWorkspaceDiscoveryEnvVar = "MCPSERVER_REPL_TEST_USE_LIVE_WORKSPACE_DISCOVERY";
    private static readonly TimeSpan StartupProbeTimeout = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(2);

    private Process? _process;
    private readonly List<string> _stdoutLines = new();
    private readonly List<string> _stderrLines = new();
    private readonly List<string> _stdoutDocumentLines = new();
    private readonly object _lock = new();
    private int? _stdoutBlockScalarIndent;
    private bool _disposed;

    /// <summary>
    /// Gets all stdout lines received from the child process.
    /// </summary>
    public IReadOnlyList<string> StdoutLines
    {
        get
        {
            lock (_lock)
            {
                return _stdoutLines.ToList();
            }
        }
    }

    /// <summary>
    /// Gets all stderr lines received from the child process.
    /// </summary>
    public IReadOnlyList<string> StderrLines
    {
        get
        {
            lock (_lock)
            {
                return _stderrLines.ToList();
            }
        }
    }

    /// <summary>
    /// Gets captured process diagnostics for assertion failure messages.
    /// </summary>
    public string Diagnostics => BuildDiagnostics();

    /// <summary>
    /// Gets a value indicating whether the child process is running.
    /// </summary>
    public bool IsRunning => _process != null && !_process.HasExited;

    /// <summary>
    /// Launches the mcpserver-repl --agent-stdio child process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the process is started.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ReplChildProcessHelper));
        }

        if (_process != null)
        {
            throw new InvalidOperationException("Process already started");
        }

        var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
        var workspaceRoot = await ResolveWorkspaceRootAsync(repoRoot).ConfigureAwait(false);
        var markerPath = Path.Combine(workspaceRoot, "AGENTS-README-FIRST.yaml");
        var hostAssemblyPath = ResolveHostAssemblyPath(repoRoot);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workspaceRoot,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardInputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add(hostAssemblyPath);
        startInfo.ArgumentList.Add("--agent-stdio");
        startInfo.ArgumentList.Add("--workspace-path");
        startInfo.ArgumentList.Add(workspaceRoot);
        if (File.Exists(markerPath))
        {
            startInfo.ArgumentList.Add("--marker-file");
            startInfo.ArgumentList.Add(markerPath);
        }

        startInfo.Environment["MCP_WORKSPACE_PATH"] = workspaceRoot;
        startInfo.Environment["MCPSERVER_WORKSPACE_PATH"] = workspaceRoot;
        startInfo.Environment["MCPSERVER_REPL_COMMAND_TIMEOUT_SECONDS"] = "10";
        startInfo.Environment["MCPSERVER_REPL_STREAM_COMMAND_TIMEOUT_SECONDS"] = "8";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";

        _process = new Process { StartInfo = startInfo };
        _process.OutputDataReceived += OnStdoutDataReceived;
        _process.ErrorDataReceived += OnStderrDataReceived;

        if (!_process.Start())
        {
            _process = null;
            throw new InvalidOperationException("Failed to start mcpserver-repl host process.");
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await Task.Delay(StartupProbeTimeout, cancellationToken).ConfigureAwait(false);
        if (_process.HasExited)
        {
            throw new InvalidOperationException(
                $"mcpserver-repl host exited during startup with code {_process.ExitCode}.{Environment.NewLine}{BuildDiagnostics()}");
        }
    }

    private static string FindRepoRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln"))
                || File.Exists(Path.Combine(directory.FullName, "McpServer.slnx"))
                || File.Exists(Path.Combine(directory.FullName, ".git"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not resolve repository root from '{startPath}'.");
    }

    private static async Task<string> ResolveWorkspaceRootAsync(string repoRoot)
    {
        if (IsLiveWorkspaceDiscoveryEnabled())
        {
            var registeredWorkspaceRoot = await TryResolveRegisteredWorkspaceRootAsync(repoRoot).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(registeredWorkspaceRoot))
            {
                return registeredWorkspaceRoot;
            }
        }

        return TryResolveSubmoduleWorkspaceRoot(repoRoot)
            ?? TryFindMarkerRoot(repoRoot)
            ?? repoRoot;
    }

    private static bool IsLiveWorkspaceDiscoveryEnabled()
        => string.Equals(
            Environment.GetEnvironmentVariable(LiveWorkspaceDiscoveryEnvVar),
            "true",
            StringComparison.OrdinalIgnoreCase);

    private static string ResolveHostAssemblyPath(string repoRoot)
    {
        var outputDirectory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar));
        var targetFramework = outputDirectory.Name;
        var configuration = outputDirectory.Parent?.Name ?? "Debug";
        var candidate = Path.Combine(
            repoRoot,
            "src",
            "McpServer.Repl.Host",
            "bin",
            configuration,
            targetFramework,
            "McpServer.Repl.Host.dll");
        if (File.Exists(candidate))
        {
            return candidate;
        }

        var copiedCandidate = Path.Combine(AppContext.BaseDirectory, "McpServer.Repl.Host.dll");
        if (File.Exists(copiedCandidate))
        {
            return copiedCandidate;
        }

        throw new FileNotFoundException(
            $"Could not find built McpServer.Repl.Host assembly. Build the test project before running REPL integration tests. Checked '{candidate}' and '{copiedCandidate}'.",
            candidate);
    }

    private static async Task<string?> TryResolveRegisteredWorkspaceRootAsync(string repoRoot)
    {
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri("http://localhost:7147") };
            using var keyResponse = await client.GetAsync("/api-key").ConfigureAwait(false);
            if (!keyResponse.IsSuccessStatusCode)
            {
                return null;
            }

            await using var keyStream = await keyResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var keyDocument = await JsonDocument.ParseAsync(keyStream).ConfigureAwait(false);
            if (!keyDocument.RootElement.TryGetProperty("apiKey", out var keyElement))
            {
                return null;
            }

            var apiKey = keyElement.GetString();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return null;
            }

            using var workspaceRequest = new HttpRequestMessage(HttpMethod.Get, "/mcpserver/workspace");
            workspaceRequest.Headers.Add("X-Api-Key", apiKey);
            using var workspaceResponse = await client.SendAsync(workspaceRequest).ConfigureAwait(false);
            if (!workspaceResponse.IsSuccessStatusCode)
            {
                return null;
            }

            await using var workspaceStream = await workspaceResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var workspaceDocument = await JsonDocument.ParseAsync(workspaceStream).ConfigureAwait(false);
            if (!workspaceDocument.RootElement.TryGetProperty("items", out var items)
                || items.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var workspaces = items.EnumerateArray()
                .Select(TryGetWorkspaceCandidate)
                .Where(candidate => candidate is not null)
                .Select(candidate => candidate!)
                .ToList();

            var remoteMatched = TryFindRemoteMatchedWorkspace(repoRoot, workspaces);
            if (!string.IsNullOrWhiteSpace(remoteMatched))
            {
                return remoteMatched;
            }

            return workspaces
                .Select(candidate => Path.GetFullPath(candidate.WorkspacePath))
                .Where(path => IsSameOrAncestor(path, repoRoot))
                .OrderByDescending(path => path.Length)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static WorkspaceCandidate? TryGetWorkspaceCandidate(JsonElement element)
    {
        if (!element.TryGetProperty("workspacePath", out var workspacePathElement))
        {
            return null;
        }

        var workspacePath = workspacePathElement.GetString();
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return null;
        }

        var gitRemoteUrl = element.TryGetProperty("gitRemoteUrl", out var gitRemoteUrlElement)
            ? gitRemoteUrlElement.GetString()
            : null;
        return new WorkspaceCandidate(workspacePath, gitRemoteUrl);
    }

    private static string? TryFindRemoteMatchedWorkspace(string repoRoot, IReadOnlyList<WorkspaceCandidate> workspaces)
    {
        var sourceOriginUrls = GetRemoteUrls(repoRoot, "origin").ToList();
        var sourceUrls = sourceOriginUrls.Count > 0
            ? sourceOriginUrls
            : GetRemoteUrls(repoRoot).ToList();
        var normalizedSourceUrls = sourceUrls
            .Select(NormalizeRemoteUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (normalizedSourceUrls.Count == 0)
        {
            return null;
        }

        foreach (var workspace in workspaces)
        {
            var workspacePath = Path.GetFullPath(workspace.WorkspacePath);
            var workspaceUrls = new List<string>();
            if (!string.IsNullOrWhiteSpace(workspace.GitRemoteUrl))
            {
                workspaceUrls.Add(workspace.GitRemoteUrl);
            }

            workspaceUrls.AddRange(GetRemoteUrls(workspacePath, "origin"));
            workspaceUrls.AddRange(GetRemoteUrls(workspacePath));

            if (workspaceUrls
                .Select(NormalizeRemoteUrl)
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Any(url => normalizedSourceUrls.Contains(url!)))
            {
                return workspacePath;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetRemoteUrls(string repoPath, string? remoteName = null)
    {
        if (!Directory.Exists(repoPath))
        {
            yield break;
        }

        var output = RunGit(repoPath, remoteName is null ? "remote -v" : $"remote get-url --all {remoteName}");
        if (string.IsNullOrWhiteSpace(output))
        {
            yield break;
        }

        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var remoteUrl = line.Trim();
            if (remoteName is null && remoteUrl.Contains('\t', StringComparison.Ordinal))
            {
                remoteUrl = remoteUrl.Split('\t', 2)[1].Split(' ', 2)[0];
            }

            if (!string.IsNullOrWhiteSpace(remoteUrl))
            {
                yield return remoteUrl;
            }
        }
    }

    private static string? RunGit(string workingDirectory, string arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(5000))
            {
                process.Kill(entireProcessTree: true);
                return null;
            }

            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeRemoteUrl(string? remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl))
        {
            return null;
        }

        var value = remoteUrl.Trim().Replace('\\', '/');
        if (value.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
        {
            var separator = value.IndexOf(':', StringComparison.Ordinal);
            if (separator > 0)
            {
                value = "https://" + value[4..separator] + "/" + value[(separator + 1)..];
            }
        }

        if (value.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^4];
        }

        return value.TrimEnd('/');
    }

    private static string? TryResolveSubmoduleWorkspaceRoot(string repoRoot)
    {
        var gitFile = Path.Combine(repoRoot, ".git");
        if (!File.Exists(gitFile))
        {
            return null;
        }

        var line = File.ReadLines(gitFile)
            .FirstOrDefault(value => value.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase));
        if (line is null)
        {
            return null;
        }

        var gitDir = line["gitdir:".Length..].Trim();
        var absoluteGitDir = Path.IsPathFullyQualified(gitDir)
            ? Path.GetFullPath(gitDir)
            : Path.GetFullPath(Path.Combine(repoRoot, gitDir));

        var current = new DirectoryInfo(absoluteGitDir);
        while (current is not null)
        {
            if (string.Equals(current.Name, ".git", StringComparison.OrdinalIgnoreCase)
                && current.Parent is not null
                && File.Exists(Path.Combine(current.Parent.FullName, "AGENTS-README-FIRST.yaml")))
            {
                return current.Parent.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string? TryFindMarkerRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS-README-FIRST.yaml")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool IsSameOrAncestor(string candidateAncestor, string path)
    {
        var ancestor = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidateAncestor));
        var descendant = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        return string.Equals(ancestor, descendant, StringComparison.OrdinalIgnoreCase)
            || descendant.StartsWith(ancestor + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || descendant.StartsWith(ancestor + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Writes a YAML envelope to the child process stdin.
    /// </summary>
    /// <param name="yamlContent">The YAML content to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public async Task WriteLineAsync(string yamlContent, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ReplChildProcessHelper));
        }

        if (_process == null || _process.HasExited)
        {
            throw new InvalidOperationException($"Process is not running.{Environment.NewLine}{BuildDiagnostics()}");
        }

        await _process.StandardInput.WriteLineAsync(yamlContent.AsMemory(), cancellationToken);
        await _process.StandardInput.WriteLineAsync(string.Empty.AsMemory(), cancellationToken);
        await _process.StandardInput.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Waits for a specific number of stdout lines to be received.
    /// </summary>
    /// <param name="count">The number of lines to wait for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the expected line count was reached; otherwise, false.</returns>
    public async Task<bool> WaitForStdoutLineCountAsync(
        int count,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            lock (_lock)
            {
                if (_stdoutLines.Count >= count)
                {
                    return true;
                }
            }

            if (_process is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"Process exited before stdout reached {count} document(s).{Environment.NewLine}{BuildDiagnostics()}");
            }

            await Task.Delay(50, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Waits until stdout contains a final result or error envelope for the specified request ID.
    /// </summary>
    /// <param name="requestId">Request ID to wait for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a final response envelope was received; otherwise, false.</returns>
    public async Task<bool> WaitForStdoutResponseAsync(
        string requestId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            lock (_lock)
            {
                if (_stdoutLines.Any(document => IsFinalResponseForRequest(document, requestId)))
                {
                    return true;
                }
            }

            if (_process is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"Process exited before stdout contained a final response for request '{requestId}'.{Environment.NewLine}{BuildDiagnostics()}");
            }

            await Task.Delay(50, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Waits for stdout to contain specific text.
    /// </summary>
    /// <param name="expectedText">The text to search for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the text was found; otherwise, false.</returns>
    public async Task<bool> WaitForStdoutContainsAsync(
        string expectedText,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            lock (_lock)
            {
                if (_stdoutLines.Any(line => line.Contains(expectedText, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            if (_process is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"Process exited before stdout contained '{expectedText}'.{Environment.NewLine}{BuildDiagnostics()}");
            }

            await Task.Delay(50, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Clears all captured stdout lines.
    /// Useful for test isolation when testing multiple commands in sequence.
    /// </summary>
    public void ClearStdout()
    {
        lock (_lock)
        {
            _stdoutLines.Clear();
            _stdoutDocumentLines.Clear();
            _stdoutBlockScalarIndent = null;
        }
    }

    /// <summary>
    /// Waits for a specific pattern to appear in any stdout line.
    /// </summary>
    /// <param name="pattern">The pattern to search for (case-insensitive).</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the pattern was found; otherwise, false.</returns>
    public async Task<bool> WaitForStdoutPatternAsync(
        string pattern,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            lock (_lock)
            {
                if (_stdoutLines.Any(line => line.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            if (_process is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"Process exited before stdout matched '{pattern}'.{Environment.NewLine}{BuildDiagnostics()}");
            }

            await Task.Delay(50, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Clears all captured stderr lines.
    /// Useful for test isolation when testing multiple commands in sequence.
    /// </summary>
    public void ClearStderr()
    {
        lock (_lock)
        {
            _stderrLines.Clear();
        }
    }

    /// <summary>
    /// Stops the child process gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous stop operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_process == null || _process.HasExited)
        {
            return;
        }

        try
        {
            _process.StandardInput.Close();

            if (!_process.WaitForExit((int)ShutdownTimeout.TotalMilliseconds))
            {
                _process.Kill(entireProcessTree: true);
            }

            await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await Task.CompletedTask;
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private string BuildDiagnostics()
    {
        lock (_lock)
        {
            var pendingStdout = _stdoutDocumentLines.Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine, _stdoutDocumentLines);
            var stdout = _stdoutLines.Count == 0
                ? "<none>"
                : string.Join($"{Environment.NewLine}--- stdout document ---{Environment.NewLine}", _stdoutLines);
            if (!string.IsNullOrWhiteSpace(pendingStdout))
            {
                stdout += $"{Environment.NewLine}--- pending stdout document ---{Environment.NewLine}{pendingStdout}";
            }

            var stderr = _stderrLines.Count == 0
                ? "<none>"
                : string.Join(Environment.NewLine, _stderrLines);
            return $"STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}";
        }
    }

    private void OnStdoutDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
        {
            return;
        }

        lock (_lock)
        {
            var line = e.Data.TrimStart('\uFEFF');
            if (IsTopLevelDocumentStart(line) && _stdoutDocumentLines.Count > 0)
            {
                FlushStdoutDocument();
            }

            if (string.IsNullOrWhiteSpace(e.Data))
            {
                if (_stdoutBlockScalarIndent is null)
                {
                    FlushStdoutDocument();
                }
                else
                {
                    _stdoutDocumentLines.Add(line);
                }
                return;
            }

            if (_stdoutBlockScalarIndent is int blockIndent
                && CountLeadingSpaces(line) <= blockIndent)
            {
                _stdoutBlockScalarIndent = null;
            }

            _stdoutDocumentLines.Add(line);
            if (StartsBlockScalar(line))
            {
                _stdoutBlockScalarIndent = CountLeadingSpaces(line);
            }
        }
    }

    private void OnStderrDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            lock (_lock)
            {
                _stderrLines.Add(e.Data);
            }
        }
    }

    private void FlushStdoutDocument()
    {
        if (_stdoutDocumentLines.Count == 0)
        {
            return;
        }

        _stdoutLines.Add(string.Join(Environment.NewLine, _stdoutDocumentLines));
        _stdoutDocumentLines.Clear();
        _stdoutBlockScalarIndent = null;
    }

    private static bool IsTopLevelDocumentStart(string line) =>
        line.StartsWith("type:", StringComparison.Ordinal);

    private static bool IsFinalResponseForRequest(string document, string requestId)
    {
        return (document.Contains("type: result", StringComparison.Ordinal)
                || document.Contains("type: error", StringComparison.Ordinal))
               && (document.Contains($"requestId: {requestId}", StringComparison.Ordinal)
                   || document.Contains($"requestId: \"{requestId}\"", StringComparison.Ordinal)
                   || document.Contains($"requestId: '{requestId}'", StringComparison.Ordinal));
    }

    private static int CountLeadingSpaces(string line)
    {
        var count = 0;
        while (count < line.Length && line[count] == ' ')
        {
            count++;
        }

        return count;
    }

    private static bool StartsBlockScalar(string line)
    {
        var trimmed = line.TrimEnd();
        var colonIndex = trimmed.LastIndexOf(':');
        if (colonIndex < 0 || colonIndex == trimmed.Length - 1)
        {
            return false;
        }

        var value = trimmed[(colonIndex + 1)..].TrimStart();
        return value.StartsWith('|') || value.StartsWith('>');
    }

    private sealed record WorkspaceCandidate(string WorkspacePath, string? GitRemoteUrl);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }

                _process.Dispose();
            }
            catch
            {
                // Best effort cleanup
            }
        }

        _disposed = true;
    }
}
