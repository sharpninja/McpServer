using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013, TR-MCP-GH-003, TR-MCP-GH-004: Runs gh CLI for issues, PRs, and workflow runs.
/// </summary>
public sealed class GitHubCliService : IGitHubCliService
{
    private const string GhExe = "gh";
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<GitHubCliService> _logger;
    private readonly IGitHubWorkspaceTokenStore? _tokenStore;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IOptionsMonitor<GitHubIntegrationOptions>? _githubOptions;

    /// <summary>TR-PLANNED-013: Constructor with IProcessRunner for testability.</summary>
    public GitHubCliService(
        IProcessRunner processRunner,
        ILogger<GitHubCliService> logger,
        IGitHubWorkspaceTokenStore? tokenStore = null,
        IHttpContextAccessor? httpContextAccessor = null,
        IOptionsMonitor<GitHubIntegrationOptions>? githubOptions = null)
    {
        _logger = logger;
        _processRunner = processRunner;
        _tokenStore = tokenStore;
        _httpContextAccessor = httpContextAccessor;
        _githubOptions = githubOptions;
    }

    /// <inheritdoc />
    public async Task<GitHubIssueListResult> ListIssuesAsync(string? state, int limit, CancellationToken cancellationToken = default)
    {
        var args = $"issue list --limit {Math.Clamp(limit, 1, 100)} --json number,title,url,state";
        if (!string.IsNullOrWhiteSpace(state))
        {
            args += " --state " + state.Trim();
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
        var args = $"pr list --limit {Math.Clamp(limit, 1, 100)} --json number,title,url,state";
        if (!string.IsNullOrWhiteSpace(state))
        {
            args += " --state " + state.Trim();
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
        var args = $"issue create --title \"{EscapeArg(title)}\"";
        if (!string.IsNullOrWhiteSpace(body))
        {
            args += " --body \"" + EscapeArg(body) + "\"";
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
        var args = $"issue comment {issueId.Trim()} --body \"{EscapeArg(body)}\"";
        var result = await RunGhAsync(args, cancellationToken).ConfigureAwait(false);
        return new GitHubCommentResult(result.ExitCode == 0, result.ExitCode != 0 ? result.Stderr : null);
    }

    /// <inheritdoc />
    public async Task<GitHubCommentResult> CommentOnPullAsync(string prId, string body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prId);
        ArgumentNullException.ThrowIfNull(body);
        var args = $"pr comment {prId.Trim()} --body \"{EscapeArg(body)}\"";
        var result = await RunGhAsync(args, cancellationToken).ConfigureAwait(false);
        return new GitHubCommentResult(result.ExitCode == 0, result.ExitCode != 0 ? result.Stderr : null);
    }

    /// <inheritdoc />
    public async Task<GitHubIssueDetailResult> GetIssueAsync(int issueNumber, CancellationToken ct = default)
    {
        var args = $"issue view {issueNumber} --json number,title,body,state,url,labels,assignees,milestone,createdAt,updatedAt,closedAt,author,comments";
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
        var args = $"issue edit {issueNumber}";
        if (request.Title is not null) args += $" --title \"{EscapeArg(request.Title)}\"";
        if (request.Body is not null) args += $" --body \"{EscapeArg(request.Body)}\"";
        if (request.AddLabels is { Count: > 0 })
            foreach (var label in request.AddLabels) args += $" --add-label \"{EscapeArg(label)}\"";
        if (request.RemoveLabels is { Count: > 0 })
            foreach (var label in request.RemoveLabels) args += $" --remove-label \"{EscapeArg(label)}\"";
        if (request.AddAssignees is { Count: > 0 })
            foreach (var assignee in request.AddAssignees) args += $" --add-assignee \"{EscapeArg(assignee)}\"";
        if (request.RemoveAssignees is { Count: > 0 })
            foreach (var assignee in request.RemoveAssignees) args += $" --remove-assignee \"{EscapeArg(assignee)}\"";
        if (request.Milestone is not null) args += $" --milestone \"{EscapeArg(request.Milestone)}\"";
        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubMutationResult(false, null, result.Stderr ?? "gh failed");
        return new GitHubMutationResult(true, result.Stdout?.Trim(), null);
    }

    /// <inheritdoc />
    public async Task<GitHubMutationResult> CloseIssueAsync(int issueNumber, string? reason = null, CancellationToken ct = default)
    {
        var args = $"issue close {issueNumber}";
        if (!string.IsNullOrWhiteSpace(reason))
            args += $" --reason {reason.Trim()}";
        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubMutationResult(false, null, result.Stderr ?? "gh failed");
        return new GitHubMutationResult(true, result.Stdout?.Trim(), null);
    }

    /// <inheritdoc />
    public async Task<GitHubMutationResult> ReopenIssueAsync(int issueNumber, CancellationToken ct = default)
    {
        var args = $"issue reopen {issueNumber}";
        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubMutationResult(false, null, result.Stderr ?? "gh failed");
        return new GitHubMutationResult(true, result.Stdout?.Trim(), null);
    }

    /// <inheritdoc />
    public async Task<GitHubLabelsResult> ListIssueLabelsAsync(CancellationToken ct = default)
    {
        var args = "label list --json name,color,description --limit 100";
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
        var args = $"run list --limit {Math.Clamp(query.Limit, 1, 100)} --json databaseId,workflowName,displayTitle,headBranch,status,conclusion,event,url,createdAt,updatedAt";
        if (!string.IsNullOrWhiteSpace(query.Branch)) args += $" --branch \"{EscapeArg(query.Branch)}\"";
        if (!string.IsNullOrWhiteSpace(query.Status)) args += $" --status \"{EscapeArg(query.Status)}\"";
        if (!string.IsNullOrWhiteSpace(query.Event)) args += $" --event \"{EscapeArg(query.Event)}\"";
        if (!string.IsNullOrWhiteSpace(query.Workflow)) args += $" --workflow \"{EscapeArg(query.Workflow)}\"";

        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubWorkflowRunListResult(false, Array.Empty<GitHubWorkflowRunItem>(), result.Stderr ?? "gh failed");

        return new GitHubWorkflowRunListResult(true, ParseWorkflowRunList(result.Stdout), null);
    }

    /// <inheritdoc />
    public async Task<GitHubWorkflowRunDetailResult> GetWorkflowRunAsync(long runId, CancellationToken ct = default)
    {
        var args = $"run view {runId} --json databaseId,workflowName,displayTitle,headBranch,headSha,status,conclusion,event,url,attempt,createdAt,updatedAt,jobs";
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
        var args = $"run rerun {runId}";
        var result = await RunGhAsync(args, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            return new GitHubMutationResult(false, null, result.Stderr ?? "gh failed");
        return new GitHubMutationResult(true, result.Stdout?.Trim(), null);
    }

    /// <inheritdoc />
    public async Task<GitHubMutationResult> CancelWorkflowRunAsync(long runId, CancellationToken ct = default)
    {
        var args = $"run cancel {runId}";
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
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
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
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
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

    private static string EscapeArg(string s)
    {
        return s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private async Task<ProcessRunResult> RunGhAsync(string args, CancellationToken ct)
    {
        var preferStoredToken = _githubOptions?.CurrentValue.PreferStoredToken ?? true;
        var allowFallback = _githubOptions?.CurrentValue.AllowCliFallback ?? true;

        if (preferStoredToken)
        {
            var token = await TryResolveWorkspaceTokenAsync(ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(token))
            {
                _logger.LogDebug("GitHub CLI auth mode: stored workspace token.");
                return await _processRunner.RunAsync(new ProcessRunRequest(GhExe, args, token), ct).ConfigureAwait(false);
            }
        }

        if (!allowFallback)
        {
            _logger.LogWarning("GitHub CLI auth mode: fallback disabled and no stored token is available.");
            return new ProcessRunResult(-1, null, "No stored GitHub token and CLI fallback is disabled.");
        }

        _logger.LogDebug("GitHub CLI auth mode: CLI fallback.");
        return await _processRunner.RunAsync(GhExe, args, ct).ConfigureAwait(false);
    }

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
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
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
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
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
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
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
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
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
