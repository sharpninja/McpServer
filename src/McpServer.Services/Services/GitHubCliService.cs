using System.Globalization;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-CORE-013, TR-MCP-GH-003, TR-MCP-GH-004: Runs gh CLI for issues, PRs, and workflow runs.
/// </summary>
public sealed class GitHubCliService : IGitHubCliService
{
    private const string GhExe = "gh";
    private static readonly HashSet<string> AllowedIssueStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "open",
        "closed",
        "all"
    };
    private static readonly HashSet<string> AllowedCloseReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "completed",
        "not_planned"
    };
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<GitHubCliService> _logger;
    private readonly IGitHubWorkspaceTokenStore? _tokenStore;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IOptionsMonitor<GitHubIntegrationOptions>? _githubOptions;
    private readonly WorkspaceServiceAccessor? _workspaceAccessor;

    /// <summary>TR-PLANNED-CORE-013: Constructor with IProcessRunner for testability.</summary>
    public GitHubCliService(
        IProcessRunner processRunner,
        ILogger<GitHubCliService> logger,
        IGitHubWorkspaceTokenStore? tokenStore = null,
        IHttpContextAccessor? httpContextAccessor = null,
        IOptionsMonitor<GitHubIntegrationOptions>? githubOptions = null,
        WorkspaceServiceAccessor? workspaceAccessor = null)
    {
        _logger = logger;
        _processRunner = processRunner;
        _tokenStore = tokenStore;
        _httpContextAccessor = httpContextAccessor;
        _githubOptions = githubOptions;
        _workspaceAccessor = workspaceAccessor;
    }

    /// <inheritdoc />
    public async Task<GitHubIssueListResult> ListIssuesAsync(string? state, int limit, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeIssueState(state, out var normalizedState, out var errorMessage))
            return new GitHubIssueListResult(false, errorMessage, Array.Empty<GitHubIssueItem>());

        var args = new List<string>
        {
            "issue",
            "list",
            "--limit",
            Math.Clamp(limit, 1, 100).ToString(CultureInfo.InvariantCulture),
            "--json",
            "number,title,url,state"
        };

        if (normalizedState is not null)
        {
            args.Add("--state");
            args.Add(normalizedState);
        }

        var result = await RunGhAsync(args, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            return new GitHubIssueListResult(false, result.Stderr ?? "gh failed", Array.Empty<GitHubIssueItem>());
        }
        var issues = ParseIssueList(result.Stdout);
        return new GitHubIssueListResult(true, null, issues);
    }

    /// <inheritdoc />
    public async Task<GitHubPullListResult> ListPullsAsync(string? state, int limit, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeIssueState(state, out var normalizedState, out var errorMessage))
            return new GitHubPullListResult(false, errorMessage, Array.Empty<GitHubPullItem>());

        var args = new List<string>
        {
            "pr",
            "list",
            "--limit",
            Math.Clamp(limit, 1, 100).ToString(CultureInfo.InvariantCulture),
            "--json",
            "number,title,url,state"
        };

        if (normalizedState is not null)
        {
            args.Add("--state");
            args.Add(normalizedState);
        }

        var result = await RunGhAsync(args, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            return new GitHubPullListResult(false, result.Stderr ?? "gh failed", Array.Empty<GitHubPullItem>());
        }
        var pulls = ParsePullList(result.Stdout);
        return new GitHubPullListResult(true, null, pulls);
    }

    /// <inheritdoc />
    public async Task<GitHubCreateIssueResult> CreateIssueAsync(string title, string? body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(title);
        var args = new List<string> { "issue", "create", "--title", title };
        if (!string.IsNullOrWhiteSpace(body))
        {
            args.Add("--body");
            args.Add(body);
        }

        var result = await RunGhAsync(args, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            return new GitHubCreateIssueResult(false, null, null, result.Stderr ?? "gh failed");
        }
        var url = result.Stdout?.Trim();
        var number = ParseIssueNumberFromUrl(url);
        return new GitHubCreateIssueResult(true, number, url, null);
    }

    /// <inheritdoc />
    public async Task<GitHubCommentResult> CommentOnIssueAsync(string issueId, string body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(issueId);
        ArgumentNullException.ThrowIfNull(body);
        var normalizedIssueId = NormalizeTargetIdentifier(issueId, nameof(issueId));
        var args = new List<string> { "issue", "comment", "--body", body, "--", normalizedIssueId };
        var result = await RunGhAsync(args, cancellationToken).ConfigureAwait(false);
        return new GitHubCommentResult(result.ExitCode == 0, result.ExitCode != 0 ? result.Stderr : null);
    }

    /// <inheritdoc />
    public async Task<GitHubCommentResult> CommentOnPullAsync(string prId, string body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prId);
        ArgumentNullException.ThrowIfNull(body);
        var normalizedPullId = NormalizeTargetIdentifier(prId, nameof(prId));
        var args = new List<string> { "pr", "comment", "--body", body, "--", normalizedPullId };
        var result = await RunGhAsync(args, cancellationToken).ConfigureAwait(false);
        return new GitHubCommentResult(result.ExitCode == 0, result.ExitCode != 0 ? result.Stderr : null);
    }

    /// <inheritdoc />
    public async Task<GitHubIssueDetailResult> GetIssueAsync(int issueNumber, CancellationToken ct = default)
    {
        var args = new List<string>
        {
            "issue",
            "view",
            issueNumber.ToString(CultureInfo.InvariantCulture),
            "--json",
            "number,title,body,state,url,labels,assignees,milestone,createdAt,updatedAt,closedAt,author,comments"
        };
        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubIssueDetailResult(false, null, result.Stderr ?? "gh failed");
        var issue = ParseIssueDetail(result.Stdout);
        return issue is not null
            ? new GitHubIssueDetailResult(true, issue, null)
            : new GitHubIssueDetailResult(false, null, "Failed to parse issue detail");
    }

    /// <inheritdoc />
    public async Task<GitHubMutationResult> UpdateIssueAsync(int issueNumber, GitHubIssueUpdateRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var args = new List<string> { "issue", "edit", issueNumber.ToString(CultureInfo.InvariantCulture) };
        if (request.Title is not null)
        {
            args.Add("--title");
            args.Add(request.Title);
        }

        if (request.Body is not null)
        {
            args.Add("--body");
            args.Add(request.Body);
        }

        if (request.AddLabels is { Count: > 0 })
        {
            foreach (var label in request.AddLabels)
            {
                args.Add("--add-label");
                args.Add(label);
            }
        }

        if (request.RemoveLabels is { Count: > 0 })
        {
            foreach (var label in request.RemoveLabels)
            {
                args.Add("--remove-label");
                args.Add(label);
            }
        }

        if (request.AddAssignees is { Count: > 0 })
        {
            foreach (var assignee in request.AddAssignees)
            {
                args.Add("--add-assignee");
                args.Add(assignee);
            }
        }

        if (request.RemoveAssignees is { Count: > 0 })
        {
            foreach (var assignee in request.RemoveAssignees)
            {
                args.Add("--remove-assignee");
                args.Add(assignee);
            }
        }

        if (request.Milestone is not null)
        {
            args.Add("--milestone");
            args.Add(request.Milestone);
        }

        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubMutationResult(false, null, result.Stderr ?? "gh failed");
        return new GitHubMutationResult(true, result.Stdout?.Trim(), null);
    }

    /// <inheritdoc />
    public async Task<GitHubMutationResult> CloseIssueAsync(int issueNumber, string? reason = null, CancellationToken ct = default)
    {
        if (!TryNormalizeCloseReason(reason, out var normalizedReason, out var errorMessage))
            return new GitHubMutationResult(false, null, errorMessage);

        var args = new List<string> { "issue", "close", issueNumber.ToString(CultureInfo.InvariantCulture) };
        if (normalizedReason is not null)
        {
            args.Add("--reason");
            args.Add(normalizedReason);
        }

        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubMutationResult(false, null, result.Stderr ?? "gh failed");
        return new GitHubMutationResult(true, result.Stdout?.Trim(), null);
    }

    /// <inheritdoc />
    public async Task<GitHubMutationResult> ReopenIssueAsync(int issueNumber, CancellationToken ct = default)
    {
        var args = new List<string> { "issue", "reopen", issueNumber.ToString(CultureInfo.InvariantCulture) };
        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubMutationResult(false, null, result.Stderr ?? "gh failed");
        return new GitHubMutationResult(true, result.Stdout?.Trim(), null);
    }

    /// <inheritdoc />
    public async Task<GitHubLabelsResult> ListIssueLabelsAsync(CancellationToken ct = default)
    {
        var args = new List<string> { "label", "list", "--json", "name,color,description", "--limit", "100" };
        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubLabelsResult(false, null, result.Stderr ?? "gh failed");
        var labels = ParseLabels(result.Stdout);
        return new GitHubLabelsResult(true, labels, null);
    }

    /// <inheritdoc />
    public async Task<GitHubWorkflowRunListResult> ListWorkflowRunsAsync(GitHubWorkflowRunQuery query, CancellationToken ct = default)
    {
        query ??= new GitHubWorkflowRunQuery();
        var args = new List<string>
        {
            "run",
            "list",
            "--limit",
            Math.Clamp(query.Limit, 1, 100).ToString(CultureInfo.InvariantCulture),
            "--json",
            "databaseId,workflowName,displayTitle,headBranch,status,conclusion,event,url,createdAt,updatedAt"
        };
        AddOption(args, "--branch", query.Branch);
        AddOption(args, "--status", query.Status);
        AddOption(args, "--event", query.Event);
        AddOption(args, "--workflow", query.Workflow);

        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubWorkflowRunListResult(false, Array.Empty<GitHubWorkflowRunItem>(), result.Stderr ?? "gh failed");

        return new GitHubWorkflowRunListResult(true, ParseWorkflowRunList(result.Stdout), null);
    }

    /// <inheritdoc />
    public async Task<GitHubWorkflowRunDetailResult> GetWorkflowRunAsync(long runId, CancellationToken ct = default)
    {
        var args = new List<string>
        {
            "run",
            "view",
            runId.ToString(CultureInfo.InvariantCulture),
            "--json",
            "databaseId,workflowName,displayTitle,headBranch,headSha,status,conclusion,event,url,attempt,createdAt,updatedAt,jobs"
        };
        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubWorkflowRunDetailResult(false, null, result.Stderr ?? "gh failed");

        var run = ParseWorkflowRunDetail(result.Stdout);
        return run is null
            ? new GitHubWorkflowRunDetailResult(false, null, "Failed to parse workflow run detail")
            : new GitHubWorkflowRunDetailResult(true, run, null);
    }

    /// <inheritdoc />
    public async Task<GitHubMutationResult> RerunWorkflowRunAsync(long runId, CancellationToken ct = default)
    {
        var args = new List<string> { "run", "rerun", runId.ToString(CultureInfo.InvariantCulture) };
        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubMutationResult(false, null, result.Stderr ?? "gh failed");
        return new GitHubMutationResult(true, result.Stdout?.Trim(), null);
    }

    /// <inheritdoc />
    public async Task<GitHubMutationResult> CancelWorkflowRunAsync(long runId, CancellationToken ct = default)
    {
        var args = new List<string> { "run", "cancel", runId.ToString(CultureInfo.InvariantCulture) };
        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubMutationResult(false, null, result.Stderr ?? "gh failed");
        return new GitHubMutationResult(true, result.Stdout?.Trim(), null);
    }

    private IReadOnlyList<GitHubIssueItem> ParseIssueList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<GitHubIssueItem>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var list = new List<GitHubIssueItem>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var number = el.TryGetProperty("number", out var n) ? n.GetInt32() : 0;
                var title = el.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                var url = el.TryGetProperty("url", out var u) ? u.GetString() : null;
                var state = el.TryGetProperty("state", out var s) ? s.GetString() : null;
                list.Add(new GitHubIssueItem(number, title, url, state));
            }
            return list;
        }
        catch (JsonException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Array.Empty<GitHubIssueItem>();
        }
    }

    private IReadOnlyList<GitHubPullItem> ParsePullList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<GitHubPullItem>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var list = new List<GitHubPullItem>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var number = el.TryGetProperty("number", out var n) ? n.GetInt32() : 0;
                var title = el.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                var url = el.TryGetProperty("url", out var u) ? u.GetString() : null;
                var state = el.TryGetProperty("state", out var s) ? s.GetString() : null;
                list.Add(new GitHubPullItem(number, title, url, state));
            }
            return list;
        }
        catch (JsonException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Array.Empty<GitHubPullItem>();
        }
    }

    private static int? ParseIssueNumberFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var last = url.TrimEnd('/').AsSpan();
        var i = last.LastIndexOf('/');
        if (i >= 0 && i < last.Length - 1 && int.TryParse(last[(i + 1)..], out var num))
            return num;
        return null;
    }

    private static bool TryNormalizeIssueState(string? state, out string? normalizedState, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            normalizedState = null;
            errorMessage = null;
            return true;
        }

        normalizedState = state.Trim().ToLowerInvariant();
        if (AllowedIssueStates.Contains(normalizedState))
        {
            errorMessage = null;
            return true;
        }

        normalizedState = null;
        errorMessage = "Invalid state. Allowed values: open, closed, all.";
        return false;
    }

    private static bool TryNormalizeCloseReason(string? reason, out string? normalizedReason, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            normalizedReason = null;
            errorMessage = null;
            return true;
        }

        normalizedReason = reason.Trim().ToLowerInvariant();
        if (AllowedCloseReasons.Contains(normalizedReason))
        {
            errorMessage = null;
            return true;
        }

        normalizedReason = null;
        errorMessage = "Invalid close reason. Allowed values: completed, not_planned.";
        return false;
    }

    private static string NormalizeTargetIdentifier(string value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value.Trim();
    }

    private static void AddOption(List<string> args, string option, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        args.Add(option);
        args.Add(value);
    }

    private static string BuildArguments(IEnumerable<string> arguments)
    {
        return string.Join(" ", arguments.Select(QuoteArgument));
    }

    private static string BuildArguments(params string[] arguments)
    {
        return BuildArguments((IEnumerable<string>)arguments);
    }

    private static string QuoteArgument(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        if (argument.Length == 0)
            return "\"\"";

        var requiresQuoting = false;
        for (var i = 0; i < argument.Length; i++)
        {
            if (char.IsWhiteSpace(argument[i]) || argument[i] == '"')
            {
                requiresQuoting = true;
                break;
            }
        }

        if (!requiresQuoting)
            return argument;

        var builder = new StringBuilder(argument.Length + 2);
        builder.Append('"');
        var backslashCount = 0;
        foreach (var character in argument)
        {
            if (character == '\\')
            {
                backslashCount++;
                continue;
            }

            if (character == '"')
            {
                builder.Append('\\', backslashCount * 2 + 1);
                builder.Append('"');
                backslashCount = 0;
                continue;
            }

            if (backslashCount > 0)
            {
                builder.Append('\\', backslashCount);
                backslashCount = 0;
            }

            builder.Append(character);
        }

        if (backslashCount > 0)
            builder.Append('\\', backslashCount * 2);

        builder.Append('"');
        return builder.ToString();
    }

    private async Task<ProcessRunResult> RunGhAsync(IReadOnlyList<string> arguments, CancellationToken ct)
    {
        var preferStoredToken = _githubOptions?.CurrentValue.PreferStoredToken ?? true;
        var allowFallback = _githubOptions?.CurrentValue.AllowCliFallback ?? true;
        var workingDirectory = ResolveWorkingDirectory();
        var repository = ResolveRepositoryArgument(workingDirectory);
        var args = BuildArguments(AddRepositoryOption(arguments, repository));
        var processWorkingDirectory = string.IsNullOrWhiteSpace(repository) ? workingDirectory : null;
        var environmentVariables = string.IsNullOrWhiteSpace(repository)
            ? BuildGitSafeDirectoryEnvironment(workingDirectory)
            : null;

        if (preferStoredToken)
        {
            var token = await TryResolveWorkspaceTokenAsync(ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(token))
            {
                _logger.LogDebug("GitHub CLI auth mode: stored workspace token.");
                return await _processRunner.RunAsync(
                    new ProcessRunRequest(GhExe, args, token, processWorkingDirectory, environmentVariables),
                    ct).ConfigureAwait(false);
            }
        }

        if (!allowFallback)
        {
            _logger.LogWarning("GitHub CLI auth mode: fallback disabled and no stored token is available.");
            return new ProcessRunResult(-1, null, "No stored GitHub token and CLI fallback is disabled.");
        }

        _logger.LogDebug("GitHub CLI auth mode: CLI fallback.");
        if (string.IsNullOrWhiteSpace(processWorkingDirectory) && environmentVariables is null)
            return await _processRunner.RunAsync(GhExe, args, ct).ConfigureAwait(false);

        return await _processRunner.RunAsync(
            new ProcessRunRequest(GhExe, args, WorkingDirectory: processWorkingDirectory, EnvironmentVariables: environmentVariables),
            ct).ConfigureAwait(false);
    }

    private string? ResolveRepositoryArgument(string? workingDirectory)
    {
        var configured = _githubOptions?.CurrentValue.Repository;
        var configuredRepository = TryNormalizeRepositoryArgument(configured);
        if (!string.IsNullOrWhiteSpace(configuredRepository))
            return configuredRepository;

        if (string.IsNullOrWhiteSpace(workingDirectory))
            return null;

        return TryResolveGitHubRepositoryFromGitConfig(workingDirectory);
    }

    private static IReadOnlyDictionary<string, string?>? BuildGitSafeDirectoryEnvironment(string? workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
            return null;

        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["GIT_CONFIG_COUNT"] = "1",
            ["GIT_CONFIG_KEY_0"] = "safe.directory",
            ["GIT_CONFIG_VALUE_0"] = Path.GetFullPath(workingDirectory)
        };
    }

    private static IReadOnlyList<string> AddRepositoryOption(IReadOnlyList<string> arguments, string? repository)
    {
        if (string.IsNullOrWhiteSpace(repository))
            return arguments;

        var result = new List<string>(arguments.Count + 2);
        var inserted = false;
        foreach (var argument in arguments)
        {
            if (!inserted && string.Equals(argument, "--", StringComparison.Ordinal))
            {
                result.Add("--repo");
                result.Add(repository);
                inserted = true;
            }

            result.Add(argument);
        }

        if (!inserted)
        {
            result.Add("--repo");
            result.Add(repository);
        }

        return result;
    }

    private static string? TryResolveGitHubRepositoryFromGitConfig(string workingDirectory)
    {
        var configPath = TryResolveGitConfigPath(workingDirectory);
        if (configPath is null || !File.Exists(configPath))
            return null;

        try
        {
            foreach (var rawLine in File.ReadLines(configPath))
            {
                var line = rawLine.Trim();
                if (!line.StartsWith("url", StringComparison.OrdinalIgnoreCase))
                    continue;

                var equalsIndex = line.IndexOf('=', StringComparison.Ordinal);
                if (equalsIndex < 0)
                    continue;

                var repository = TryNormalizeRepositoryArgument(line[(equalsIndex + 1)..]);
                if (!string.IsNullOrWhiteSpace(repository))
                    return repository;
            }
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    private static string? TryResolveGitConfigPath(string workingDirectory)
    {
        try
        {
            var dotGitPath = Path.Combine(Path.GetFullPath(workingDirectory), ".git");
            if (Directory.Exists(dotGitPath))
                return Path.Combine(dotGitPath, "config");

            if (!File.Exists(dotGitPath))
                return null;

            var gitDirLine = File.ReadLines(dotGitPath).FirstOrDefault();
            const string gitDirPrefix = "gitdir:";
            if (gitDirLine is null || !gitDirLine.StartsWith(gitDirPrefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var gitDir = gitDirLine[gitDirPrefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(gitDir))
                return null;

            var resolvedGitDir = Path.IsPathRooted(gitDir)
                ? gitDir
                : Path.GetFullPath(Path.Combine(workingDirectory, gitDir));
            return Path.Combine(resolvedGitDir, "config");
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? TryNormalizeRepositoryArgument(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim().TrimEnd('/');
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            if (!IsGitHubHost(uri.Host))
                return null;

            return BuildRepositoryArgument(uri.Host, uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries));
        }

        var scpSeparator = trimmed.IndexOf(':', StringComparison.Ordinal);
        var atIndex = trimmed.IndexOf('@', StringComparison.Ordinal);
        if (atIndex >= 0 && scpSeparator > atIndex)
        {
            var host = trimmed[(atIndex + 1)..scpSeparator];
            if (!IsGitHubHost(host))
                return null;

            return BuildRepositoryArgument(host, trimmed[(scpSeparator + 1)..].Split('/', StringSplitOptions.RemoveEmptyEntries));
        }

        var parts = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
            return $"{parts[0]}/{TrimGitSuffix(parts[1])}";

        if (parts.Length == 3)
            return string.Equals(parts[0], "github.com", StringComparison.OrdinalIgnoreCase)
                ? $"{parts[1]}/{TrimGitSuffix(parts[2])}"
                : $"{parts[0]}/{parts[1]}/{TrimGitSuffix(parts[2])}";

        return null;
    }

    private static bool IsGitHubHost(string host)
        => string.Equals(host, "github.com", StringComparison.OrdinalIgnoreCase)
           || string.Equals(host, "www.github.com", StringComparison.OrdinalIgnoreCase)
           || host.EndsWith(".github.com", StringComparison.OrdinalIgnoreCase)
           || host.EndsWith(".ghe.com", StringComparison.OrdinalIgnoreCase);

    private static string? BuildRepositoryArgument(string host, string[] pathParts)
    {
        if (pathParts.Length < 2)
            return null;

        var owner = pathParts[0];
        var repo = TrimGitSuffix(pathParts[1]);
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            return null;

        return string.Equals(host, "github.com", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "www.github.com", StringComparison.OrdinalIgnoreCase)
            ? $"{owner}/{repo}"
            : $"{host}/{owner}/{repo}";
    }

    private static string TrimGitSuffix(string value)
        => value.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? value[..^4]
            : value;

    private async Task<string?> TryResolveWorkspaceTokenAsync(CancellationToken ct)
    {
        if (_tokenStore is null || _httpContextAccessor is null)
            return null;

        var requestServices = _httpContextAccessor.HttpContext?.RequestServices;
        if (requestServices is null)
            return null;

        var workspacePath = requestServices.GetService<WorkspaceContext>()?.WorkspacePath;
        if (string.IsNullOrWhiteSpace(workspacePath))
            return null;

        var record = await _tokenStore.GetAsync(workspacePath, ct).ConfigureAwait(false);
        if (record is null)
            return null;

        if (record.ExpiresAtUtc is { } expiresAt && expiresAt <= DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Stored GitHub token is expired for workspace {WorkspacePath}.", workspacePath);
            return null;
        }

        return record.AccessToken;
    }

    private string? ResolveWorkingDirectory()
    {
        var workingDirectory = _workspaceAccessor?.GetWorkspacePath();
        return string.IsNullOrWhiteSpace(workingDirectory)
            ? null
            : Path.GetFullPath(workingDirectory);
    }

    private GitHubIssueDetail? ParseIssueDetail(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var el = doc.RootElement;
            var number = el.TryGetProperty("number", out var n) ? n.GetInt32() : 0;
            var title = el.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            var body = el.TryGetProperty("body", out var b) ? b.GetString() : null;
            var state = el.TryGetProperty("state", out var s) ? s.GetString() : null;
            var url = el.TryGetProperty("url", out var u) ? u.GetString() : null;
            var milestone = el.TryGetProperty("milestone", out var ms) && ms.ValueKind == JsonValueKind.Object
                ? (ms.TryGetProperty("title", out var mt) ? mt.GetString() : null)
                : null;
            var createdAt = el.TryGetProperty("createdAt", out var ca) ? ca.GetString() : null;
            var updatedAt = el.TryGetProperty("updatedAt", out var ua) ? ua.GetString() : null;
            var closedAt = el.TryGetProperty("closedAt", out var cla) ? cla.GetString() : null;
            var author = el.TryGetProperty("author", out var au) && au.ValueKind == JsonValueKind.Object
                ? (au.TryGetProperty("login", out var al) ? al.GetString() : null)
                : null;

            var labels = new List<GitHubLabel>();
            if (el.TryGetProperty("labels", out var lbs) && lbs.ValueKind == JsonValueKind.Array)
            {
                foreach (var lb in lbs.EnumerateArray())
                {
                    var ln = lb.TryGetProperty("name", out var lnp) ? lnp.GetString() ?? "" : "";
                    var lc = lb.TryGetProperty("color", out var lcp) ? lcp.GetString() : null;
                    var ld = lb.TryGetProperty("description", out var ldp) ? ldp.GetString() : null;
                    labels.Add(new GitHubLabel(ln, lc, ld));
                }
            }

            var assignees = new List<string>();
            if (el.TryGetProperty("assignees", out var asgn) && asgn.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in asgn.EnumerateArray())
                {
                    var login = a.ValueKind == JsonValueKind.Object
                        ? (a.TryGetProperty("login", out var alg) ? alg.GetString() : null)
                        : a.GetString();
                    if (login is not null) assignees.Add(login);
                }
            }

            var comments = new List<GitHubIssueComment>();
            if (el.TryGetProperty("comments", out var cmts) && cmts.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in cmts.EnumerateArray())
                {
                    var cAuthor = c.TryGetProperty("author", out var cap) && cap.ValueKind == JsonValueKind.Object
                        ? (cap.TryGetProperty("login", out var cl) ? cl.GetString() : null)
                        : null;
                    var cBody = c.TryGetProperty("body", out var cbp) ? cbp.GetString() : null;
                    var cCreated = c.TryGetProperty("createdAt", out var ccp) ? ccp.GetString() : null;
                    comments.Add(new GitHubIssueComment(cAuthor, cBody, cCreated));
                }
            }

            return new GitHubIssueDetail(number, title, body, state, url, labels, assignees, milestone, createdAt, updatedAt, closedAt, author, comments);
        }
        catch (JsonException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    private IReadOnlyList<GitHubLabel> ParseLabels(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<GitHubLabel>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var list = new List<GitHubLabel>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var name = el.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var color = el.TryGetProperty("color", out var c) ? c.GetString() : null;
                var desc = el.TryGetProperty("description", out var d) ? d.GetString() : null;
                list.Add(new GitHubLabel(name, color, desc));
            }
            return list;
        }
        catch (JsonException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Array.Empty<GitHubLabel>();
        }
    }

    private IReadOnlyList<GitHubWorkflowRunItem> ParseWorkflowRunList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<GitHubWorkflowRunItem>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var list = new List<GitHubWorkflowRunItem>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                list.Add(new GitHubWorkflowRunItem(
                    RunId: ReadLong(el, "databaseId"),
                    WorkflowName: ReadString(el, "workflowName"),
                    DisplayTitle: ReadString(el, "displayTitle"),
                    HeadBranch: ReadString(el, "headBranch"),
                    Status: ReadString(el, "status"),
                    Conclusion: ReadString(el, "conclusion"),
                    Event: ReadString(el, "event"),
                    Url: ReadString(el, "url"),
                    CreatedAt: ReadString(el, "createdAt"),
                    UpdatedAt: ReadString(el, "updatedAt")));
            }

            return list;
        }
        catch (JsonException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Array.Empty<GitHubWorkflowRunItem>();
        }
    }

    private GitHubWorkflowRunDetail? ParseWorkflowRunDetail(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var jobs = new List<GitHubWorkflowRunJob>();
            if (root.TryGetProperty("jobs", out var jobsElement) && jobsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var jobElement in jobsElement.EnumerateArray())
                {
                    var steps = new List<GitHubWorkflowRunJobStep>();
                    if (jobElement.TryGetProperty("steps", out var stepsElement) && stepsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var stepElement in stepsElement.EnumerateArray())
                        {
                            steps.Add(new GitHubWorkflowRunJobStep(
                                Name: ReadString(stepElement, "name"),
                                Status: ReadString(stepElement, "status"),
                                Conclusion: ReadString(stepElement, "conclusion"),
                                Number: ReadNullableInt(stepElement, "number")));
                        }
                    }

                    jobs.Add(new GitHubWorkflowRunJob(
                        Name: ReadString(jobElement, "name"),
                        Status: ReadString(jobElement, "status"),
                        Conclusion: ReadString(jobElement, "conclusion"),
                        StartedAt: ReadString(jobElement, "startedAt"),
                        CompletedAt: ReadString(jobElement, "completedAt"),
                        Url: ReadString(jobElement, "url"),
                        Steps: steps));
                }
            }

            return new GitHubWorkflowRunDetail(
                RunId: ReadLong(root, "databaseId"),
                WorkflowName: ReadString(root, "workflowName"),
                DisplayTitle: ReadString(root, "displayTitle"),
                HeadBranch: ReadString(root, "headBranch"),
                HeadSha: ReadString(root, "headSha"),
                Status: ReadString(root, "status"),
                Conclusion: ReadString(root, "conclusion"),
                Event: ReadString(root, "event"),
                Url: ReadString(root, "url"),
                Attempt: ReadNullableInt(root, "attempt"),
                CreatedAt: ReadString(root, "createdAt"),
                UpdatedAt: ReadString(root, "updatedAt"),
                Jobs: jobs);
        }
        catch (JsonException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    private static string? ReadString(JsonElement parent, string propertyName)
    {
        return parent.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static long ReadLong(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var property))
            return 0;
        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var numeric))
            return numeric;
        if (property.ValueKind == JsonValueKind.String && long.TryParse(property.GetString(), out var parsed))
            return parsed;
        return 0;
    }

    private static int? ReadNullableInt(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var property))
            return null;
        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numeric))
            return numeric;
        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out var parsed))
            return parsed;
        return null;
    }
}
