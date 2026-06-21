using System.Diagnostics;
using System.Text.Json;

namespace McpServer.Validation;

internal static class ValidationAuth
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string MarkerFileName = "AGENTS-README-FIRST.yaml";

    public static void AddPreferredApiKey(HttpClient client)
    {
        var apiKey = ResolvePreferredApiKeyAsync(client).GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }

        client.DefaultRequestHeaders.Remove(ApiKeyHeaderName);
        client.DefaultRequestHeaders.Add(ApiKeyHeaderName, apiKey);
    }

    public static string? ResolvePreferredApiKey(HttpClient client) =>
        ResolvePreferredApiKeyAsync(client).GetAwaiter().GetResult();

    private static async Task<string?> ResolvePreferredApiKeyAsync(HttpClient client)
    {
        var explicitKey = Environment.GetEnvironmentVariable("MCPSERVER_APIKEY");
        if (!string.IsNullOrWhiteSpace(explicitKey))
        {
            return explicitKey;
        }

        var fullKey = IsSubmoduleCheckout()
            ? await TryReadApiKeyFromRegisteredWorkspaceMarkerAsync(client).ConfigureAwait(false)
                ?? TryReadApiKeyFromSubmoduleWorkspaceMarker()
            : null;
        fullKey ??= TryReadApiKeyFromMarkerFile()
            ?? TryReadApiKeyFromEnvironmentWorkspaceMarker()
            ?? TryReadApiKeyFromSessionState();
        if (!string.IsNullOrWhiteSpace(fullKey))
        {
            return fullKey;
        }

        return await GetDefaultApiKeyAsync(client).ConfigureAwait(false);
    }

    private static string? TryReadApiKeyFromSessionState()
    {
        var sessionPath = FindFileUpwards(".mcpServer", "session.yaml");
        if (sessionPath is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(sessionPath));
            return document.RootElement.TryGetProperty("apiKey", out var apiKeyElement)
                ? apiKeyElement.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadApiKeyFromMarkerFile()
    {
        var markerPath = FindFileUpwards(MarkerFileName);
        return markerPath is null ? null : TryReadApiKeyFromMarkerFile(markerPath);
    }

    private static async Task<string?> TryReadApiKeyFromRegisteredWorkspaceMarkerAsync(HttpClient client)
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            return null;
        }

        var workspacePath = await TryResolveRegisteredWorkspacePathAsync(client, repoRoot).ConfigureAwait(false);
        if (workspacePath is null)
        {
            return null;
        }

        return TryReadApiKeyFromMarkerFile(Path.Combine(workspacePath, MarkerFileName));
    }

    private static string? TryReadApiKeyFromSubmoduleWorkspaceMarker()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            return null;
        }

        var workspaceRoot = TryResolveSubmoduleWorkspaceRoot(repoRoot);
        if (workspaceRoot is null)
        {
            return null;
        }

        return TryReadApiKeyFromMarkerFile(Path.Combine(workspaceRoot, MarkerFileName));
    }

    private static string? TryReadApiKeyFromEnvironmentWorkspaceMarker()
    {
        foreach (var variableName in new[] { "MCPSERVER_WORKSPACE_PATH", "MCP_WORKSPACE_PATH" })
        {
            var workspacePath = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(workspacePath))
            {
                continue;
            }

            var markerPath = Path.Combine(workspacePath, MarkerFileName);
            var apiKey = TryReadApiKeyFromMarkerFile(markerPath);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                return apiKey;
            }
        }

        return null;
    }

    private static string? TryReadApiKeyFromMarkerFile(string markerPath)
    {
        if (!File.Exists(markerPath))
        {
            return null;
        }

        foreach (var line in File.ReadLines(markerPath))
        {
            if (line.StartsWith("apiKey:", StringComparison.OrdinalIgnoreCase))
            {
                return line["apiKey:".Length..].Trim();
            }
        }

        return null;
    }

    private static async Task<string?> TryResolveRegisteredWorkspacePathAsync(HttpClient client, string repoRoot)
    {
        var bootstrapKey = await GetDefaultApiKeyAsync(client).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(bootstrapKey))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/mcpserver/workspace");
            request.Headers.Add(ApiKeyHeaderName, bootstrapKey);
            using var response = await client.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(contentStream).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("items", out var items)
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

    private static string? TryFindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "McpServer.sln"))
                || File.Exists(Path.Combine(current.FullName, "McpServer.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool IsSubmoduleCheckout()
    {
        var repoRoot = TryFindRepoRoot();
        return repoRoot is not null && File.Exists(Path.Combine(repoRoot, ".git"));
    }

    private static string? TryResolveSubmoduleWorkspaceRoot(string repoRoot)
    {
        var gitFile = Path.Combine(repoRoot, ".git");
        if (!File.Exists(gitFile))
        {
            return null;
        }

        var gitDir = TryReadGitDir(gitFile);
        if (gitDir is null)
        {
            return null;
        }

        var current = new DirectoryInfo(gitDir);
        while (current is not null)
        {
            if (string.Equals(current.Name, ".git", StringComparison.OrdinalIgnoreCase)
                && current.Parent is not null
                && File.Exists(Path.Combine(current.Parent.FullName, MarkerFileName)))
            {
                return current.Parent.FullName;
            }

            current = current.Parent;
        }

        return TryFindAncestorWorkspaceContainingSubmodule(repoRoot);
    }

    private static string? TryReadGitDir(string gitFile)
    {
        var line = File.ReadLines(gitFile)
            .FirstOrDefault(value => value.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase));
        if (line is null)
        {
            return null;
        }

        var gitDir = line["gitdir:".Length..].Trim();
        if (Path.IsPathFullyQualified(gitDir))
        {
            return Path.GetFullPath(gitDir);
        }

        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(gitFile)!, gitDir));
    }

    private static string? TryFindAncestorWorkspaceContainingSubmodule(string repoRoot)
    {
        var repo = new DirectoryInfo(repoRoot);
        var current = repo.Parent;
        while (current is not null)
        {
            var gitmodulesPath = Path.Combine(current.FullName, ".gitmodules");
            if (File.Exists(gitmodulesPath)
                && File.Exists(Path.Combine(current.FullName, MarkerFileName))
                && GitmodulesContainsSubmodulePath(gitmodulesPath, current.FullName, repoRoot))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool GitmodulesContainsSubmodulePath(string gitmodulesPath, string workspaceRoot, string repoRoot)
    {
        var relativePath = Path.GetRelativePath(workspaceRoot, repoRoot).Replace('\\', '/');
        foreach (var line in File.ReadLines(gitmodulesPath))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("path", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var separator = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (separator < 0)
            {
                continue;
            }

            var configuredPath = trimmed[(separator + 1)..].Trim().Replace('\\', '/');
            if (string.Equals(configuredPath, relativePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameOrAncestor(string candidateAncestor, string path)
    {
        var ancestor = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidateAncestor));
        var descendant = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        return string.Equals(ancestor, descendant, StringComparison.OrdinalIgnoreCase)
            || descendant.StartsWith(ancestor + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || descendant.StartsWith(ancestor + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindFileUpwards(params string[] pathSegments)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine([current.FullName, .. pathSegments]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private static async Task<string?> GetDefaultApiKeyAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api-key").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(contentStream).ConfigureAwait(false);
        return document.RootElement.TryGetProperty("apiKey", out var apiKeyElement)
            ? apiKeyElement.GetString()
            : null;
    }

    private sealed record WorkspaceCandidate(string WorkspacePath, string? GitRemoteUrl);
}
