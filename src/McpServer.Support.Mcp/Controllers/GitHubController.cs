using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// TR-PLANNED-013, TR-GH-013-006: GitHub metadata via gh CLI (issues and PRs).
/// FR-SUPPORT-010, FR-SUPPORT-013: List, create, comment, update, close, reopen, sync endpoints.
/// </summary>
[ApiController]
[Route("mcpserver/gh")]
public sealed class GitHubController : ControllerBase
{
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
    private readonly IGitHubCliService _gh;
    private readonly IIssueTodoSyncService? _syncService;
    private readonly IChangeEventBus? _eventBus;
    private readonly ILogger<GitHubController> _logger;
    private readonly IGitHubWorkspaceTokenStore _tokenStore;
    private readonly IOptionsMonitor<GitHubIntegrationOptions> _gitHubOptions;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    public GitHubController(
        IGitHubCliService gh,
        IGitHubWorkspaceTokenStore tokenStore,
        IOptionsMonitor<GitHubIntegrationOptions> gitHubOptions,
        IIssueTodoSyncService? syncService = null,
        IChangeEventBus? eventBus = null,
        ILogger<GitHubController>? logger = null)
    {
        _gh = gh;
        _tokenStore = tokenStore;
        _gitHubOptions = gitHubOptions;
        _syncService = syncService;
        _eventBus = eventBus;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GitHubController>.Instance;
    }

    /// <summary>TR-PLANNED-013: List issues (gh.issues.list).</summary>
    /// <param name="state">Optional filter: open, closed, all.</param>
    /// <param name="limit">Max issues to return (1–100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("issues")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> ListIssuesAsync([FromQuery] string? state, [FromQuery] int limit = 30, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeIssueState(state, out var normalizedState, out var errorMessage))
            return BadRequest(new { error = errorMessage });

        var result = await _gh.ListIssuesAsync(normalizedState, limit, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return Ok(new { issues = Array.Empty<object>(), error = result.Error });
        return Ok(new { issues = result.Issues.Select(i => new { i.Number, i.Title, i.Url, i.State }).ToList() });
    }

    /// <summary>TR-GH-013-006: Get single issue with full detail.</summary>
    /// <param name="number">Issue number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("issues/{number:int}")]
    [ProducesResponseType(typeof(GitHubIssueDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetIssueAsync([FromRoute] int number, CancellationToken cancellationToken = default)
    {
        var result = await _gh.GetIssueAsync(number, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return NotFound(new { error = result.ErrorMessage ?? "Issue not found" });
        return Ok(result.Issue);
    }

    /// <summary>TR-PLANNED-013: Create issue (gh.issues.create).</summary>
    /// <param name="request">Title and optional body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("issues")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CreateIssueAsync([FromBody] GitHubIssueRequest? request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "title is required" });
        var result = await _gh.CreateIssueAsync(request.Title, request.Body, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(new { error = result.Error ?? "failed to create issue" });
        await PublishGitHubChangeSafeAsync(ChangeEventActions.Created, result.Number?.ToString() ?? "issue", cancellationToken).ConfigureAwait(false);
        return Ok(new { number = result.Number, url = result.Url });
    }

    /// <summary>TR-GH-013-006: Update issue metadata.</summary>
    /// <param name="number">Issue number.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("issues/{number:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> UpdateIssueAsync([FromRoute] int number, [FromBody] GitHubIssueUpdateRequest? request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return BadRequest(new { error = "request body is required" });
        var result = await _gh.UpdateIssueAsync(number, request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage ?? "failed to update issue" });
        await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, number.ToString(), cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true, url = result.Url });
    }

    /// <summary>TR-GH-013-006: Close an issue.</summary>
    /// <param name="number">Issue number.</param>
    /// <param name="reason">Optional close reason (completed or not_planned).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("issues/{number:int}/close")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CloseIssueAsync([FromRoute] int number, [FromQuery] string? reason = null, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeCloseReason(reason, out var normalizedReason, out var errorMessage))
            return BadRequest(new { error = errorMessage });

        var result = await _gh.CloseIssueAsync(number, normalizedReason, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage ?? "failed to close issue" });
        await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, number.ToString(), cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true, url = result.Url });
    }

    /// <summary>TR-GH-013-006: Reopen an issue.</summary>
    /// <param name="number">Issue number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("issues/{number:int}/reopen")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> ReopenIssueAsync([FromRoute] int number, CancellationToken cancellationToken = default)
    {
        var result = await _gh.ReopenIssueAsync(number, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage ?? "failed to reopen issue" });
        await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, number.ToString(), cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true, url = result.Url });
    }

    /// <summary>TR-PLANNED-013: Comment on issue (gh.issues.comment).</summary>
    /// <param name="id">Issue number.</param>
    /// <param name="body">Comment request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("issues/{id}/comments")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CommentOnIssueAsync([FromRoute] string id, [FromBody] GitHubCommentRequest? body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id) || body == null || string.IsNullOrWhiteSpace(body.Body))
            return BadRequest(new { error = "id and body are required" });
        var result = await _gh.CommentOnIssueAsync(id, body.Body, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(new { error = result.Error ?? "failed to add comment" });
        await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, id, cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true });
    }

    /// <summary>TR-GH-013-006: List available repository labels.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("labels")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> ListLabelsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _gh.ListIssueLabelsAsync(cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return Ok(new { labels = Array.Empty<object>(), error = result.ErrorMessage });
        return Ok(new { labels = result.Labels });
    }

    /// <summary>TR-MCP-GH-002: Get GitHub auth status for the resolved workspace.</summary>
    [HttpGet("auth/status")]
    [ProducesResponseType(typeof(GitHubAuthStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GitHubAuthStatusResponse>> GetAuthStatusAsync(CancellationToken cancellationToken = default)
    {
        var workspacePath = ResolveWorkspacePath();
        if (workspacePath is null)
            return BadRequest(new { error = "Workspace context is required for GitHub auth status." });

        var storedToken = await _tokenStore.GetAsync(workspacePath, cancellationToken).ConfigureAwait(false);
        var options = _gitHubOptions.CurrentValue;
        var oauthConfigured = IsOAuthConfigured(options.OAuth);
        var mode = storedToken is not null
            ? "stored_token"
            : options.AllowCliFallback ? "cli_fallback" : "none";

        return Ok(new GitHubAuthStatusResponse
        {
            WorkspacePath = workspacePath,
            AuthMode = mode,
            HasStoredToken = storedToken is not null,
            TokenUpdatedAtUtc = storedToken?.UpdatedAtUtc,
            TokenExpiresAtUtc = storedToken?.ExpiresAtUtc,
            CliFallbackAllowed = options.AllowCliFallback,
            OAuthConfigured = oauthConfigured,
        });
    }

    /// <summary>TR-MCP-GH-002: Set or replace the GitHub token for the resolved workspace.</summary>
    [HttpPut("auth/token")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> SetAuthTokenAsync([FromBody] GitHubAuthTokenUpsertRequest? request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.AccessToken))
            return BadRequest(new { error = "accessToken is required." });

        var workspacePath = ResolveWorkspacePath();
        if (workspacePath is null)
            return BadRequest(new { error = "Workspace context is required for GitHub auth updates." });

        try
        {
            await _tokenStore.UpsertAsync(workspacePath, request.AccessToken, request.ExpiresAtUtc, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }

        await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, "auth-token", cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true });
    }

    /// <summary>TR-MCP-GH-002: Remove the stored GitHub token for the resolved workspace.</summary>
    [HttpDelete("auth/token")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> DeleteAuthTokenAsync(CancellationToken cancellationToken = default)
    {
        var workspacePath = ResolveWorkspacePath();
        if (workspacePath is null)
            return BadRequest(new { error = "Workspace context is required for GitHub auth updates." });

        bool removed;
        try
        {
            removed = await _tokenStore.DeleteAsync(workspacePath, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }

        if (removed)
            await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, "auth-token", cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true, removed });
    }

    /// <summary>TR-MCP-GH-001: Returns configured GitHub OAuth app settings for client bootstrap.</summary>
    [HttpGet("oauth/config")]
    [ProducesResponseType(typeof(GitHubOAuthConfigResponse), StatusCodes.Status200OK)]
    public ActionResult<GitHubOAuthConfigResponse> GetOAuthConfig()
    {
        var oauth = _gitHubOptions.CurrentValue.OAuth;
        return Ok(new GitHubOAuthConfigResponse
        {
            ClientId = oauth.ClientId,
            RedirectUri = oauth.RedirectUri,
            Scopes = oauth.Scopes,
            AuthorizeEndpoint = oauth.AuthorizeEndpoint,
            IsConfigured = IsOAuthConfigured(oauth),
        });
    }

    /// <summary>TR-MCP-GH-001: Builds a GitHub authorize URL from configured OAuth app values.</summary>
    [HttpGet("oauth/authorize-url")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<object> GetAuthorizeUrl([FromQuery] string? state = null)
    {
        var oauth = _gitHubOptions.CurrentValue.OAuth;
        if (!IsOAuthConfigured(oauth))
            return BadRequest(new { error = "GitHub OAuth is not fully configured. Set Mcp:GitHub:OAuth:ClientId and RedirectUri." });

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = oauth.ClientId,
            ["redirect_uri"] = oauth.RedirectUri,
            ["scope"] = oauth.Scopes,
            ["state"] = state,
        };
        var authorizeUrl = QueryHelpers.AddQueryString(oauth.AuthorizeEndpoint, query);
        return Ok(new { authorizeUrl });
    }

    /// <summary>TR-PLANNED-013: List PRs (gh.prs.list).</summary>
    /// <param name="state">Optional filter: open, closed, all.</param>
    /// <param name="limit">Max PRs to return (1–100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("pulls")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> ListPullsAsync([FromQuery] string? state, [FromQuery] int limit = 30, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeIssueState(state, out var normalizedState, out var errorMessage))
            return BadRequest(new { error = errorMessage });

        var result = await _gh.ListPullsAsync(normalizedState, limit, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return Ok(new { pulls = Array.Empty<object>(), error = result.Error });
        return Ok(new { pulls = result.Pulls.Select(p => new { p.Number, p.Title, p.Url, p.State }).ToList() });
    }

    /// <summary>TR-MCP-GH-004: List workflow runs.</summary>
    [HttpGet("actions/runs")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> ListWorkflowRunsAsync(
        [FromQuery] string? branch = null,
        [FromQuery] string? status = null,
        [FromQuery(Name = "event")] string? eventName = null,
        [FromQuery] string? workflow = null,
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var query = new GitHubWorkflowRunQuery
        {
            Branch = branch,
            Status = status,
            Event = eventName,
            Workflow = workflow,
            Limit = limit,
        };

        var result = await _gh.ListWorkflowRunsAsync(query, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return Ok(new { runs = Array.Empty<object>(), error = result.ErrorMessage ?? "failed to list workflow runs" });
        return Ok(new { runs = result.Runs, error = (string?)null });
    }

    /// <summary>TR-MCP-GH-004: Get workflow run details.</summary>
    [HttpGet("actions/runs/{runId:long}")]
    [ProducesResponseType(typeof(GitHubWorkflowRunDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GitHubWorkflowRunDetail>> GetWorkflowRunAsync([FromRoute] long runId, CancellationToken cancellationToken = default)
    {
        var result = await _gh.GetWorkflowRunAsync(runId, cancellationToken).ConfigureAwait(false);
        if (!result.Success || result.Run is null)
        {
            if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.ErrorMessage });
            return BadRequest(new { error = result.ErrorMessage ?? "failed to fetch workflow run" });
        }

        return Ok(result.Run);
    }

    /// <summary>TR-MCP-GH-004: Re-run a workflow run.</summary>
    [HttpPost("actions/runs/{runId:long}/rerun")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> RerunWorkflowRunAsync([FromRoute] long runId, CancellationToken cancellationToken = default)
    {
        var result = await _gh.RerunWorkflowRunAsync(runId, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage ?? "failed to rerun workflow" });
        await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, $"workflow-run-{runId}", cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true });
    }

    /// <summary>TR-MCP-GH-004: Cancel an in-progress workflow run.</summary>
    [HttpPost("actions/runs/{runId:long}/cancel")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CancelWorkflowRunAsync([FromRoute] long runId, CancellationToken cancellationToken = default)
    {
        var result = await _gh.CancelWorkflowRunAsync(runId, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage ?? "failed to cancel workflow" });
        await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, $"workflow-run-{runId}", cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true });
    }

    /// <summary>TR-PLANNED-013: Comment on PR (gh.prs.comment).</summary>
    /// <param name="id">PR number.</param>
    /// <param name="body">Comment request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("pulls/{id}/comments")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CommentOnPullAsync([FromRoute] string id, [FromBody] GitHubCommentRequest? body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id) || body == null || string.IsNullOrWhiteSpace(body.Body))
            return BadRequest(new { error = "id and body are required" });
        var result = await _gh.CommentOnPullAsync(id, body.Body, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(new { error = result.Error ?? "failed to add comment" });
        await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, id, cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true });
    }

    /// <summary>TR-GH-013-006: Pull GitHub issues into TODOs.</summary>
    /// <param name="state">Issue state filter (open, closed, all). Default: open.</param>
    /// <param name="limit">Max issues to sync. Default: 30.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("issues/sync/from-github")]
    [ProducesResponseType(typeof(IssueSyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> SyncFromGitHubAsync([FromQuery] string? state = "open", [FromQuery] int limit = 30, CancellationToken cancellationToken = default)
    {
        if (_syncService is null)
            return BadRequest(new { error = "Issue sync service not configured" });

        if (!TryNormalizeIssueState(state, out var normalizedState, out var errorMessage))
            return BadRequest(new { error = errorMessage });

        var result = await _syncService.SyncAllIssuesToTodosAsync(normalizedState, limit, cancellationToken).ConfigureAwait(false);
        await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, "sync-from-github", cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>TR-GH-013-006: Push TODO changes to GitHub issues.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("issues/sync/to-github")]
    [ProducesResponseType(typeof(IssueSyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> SyncToGitHubAsync(CancellationToken cancellationToken = default)
    {
        if (_syncService is null)
            return BadRequest(new { error = "Issue sync service not configured" });
        var result = await _syncService.SyncAllTodosToIssuesAsync(cancellationToken).ConfigureAwait(false);
        await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, "sync-to-github", cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>TR-GH-013-006: Sync a single issue bidirectionally.</summary>
    /// <param name="number">Issue number.</param>
    /// <param name="direction">Sync direction: from-github (default) or to-github.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("issues/{number:int}/sync")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> SyncSingleIssueAsync([FromRoute] int number, [FromQuery] string? direction = "from-github", CancellationToken cancellationToken = default)
    {
        if (_syncService is null)
            return BadRequest(new { error = "Issue sync service not configured" });

        if (string.Equals(direction, "to-github", StringComparison.OrdinalIgnoreCase))
        {
            var todoId = $"ISSUE-{number}";
            var result = await _syncService.SyncTodoToIssueAsync(todoId, cancellationToken).ConfigureAwait(false);
            if (!result.Success)
                return BadRequest(new { error = result.ErrorMessage });
            await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, number.ToString(), cancellationToken).ConfigureAwait(false);
            return Ok(new { success = true, url = result.Url });
        }
        else
        {
            var issueResult = await _gh.GetIssueAsync(number, cancellationToken).ConfigureAwait(false);
            if (!issueResult.Success || issueResult.Issue is null)
                return NotFound(new { error = issueResult.ErrorMessage ?? "Issue not found" });
            var result = await _syncService.SyncIssueToTodoAsync(issueResult.Issue, cancellationToken).ConfigureAwait(false);
            if (!result.Success)
                return BadRequest(new { error = result.Error });
            await PublishGitHubChangeSafeAsync(ChangeEventActions.Updated, number.ToString(), cancellationToken).ConfigureAwait(false);
            return Ok(new { success = true, todoId = result.Item?.Id });
        }
    }

    private async Task PublishGitHubChangeSafeAsync(string action, string entityId, CancellationToken cancellationToken)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.GitHub,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = $"mcp://workspace/gh/issues/{entityId}",
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing GitHub change event for {EntityId}", entityId);
        }
    }

    private string? ResolveWorkspacePath()
    {
        return HttpContext.RequestServices.GetService<WorkspaceContext>()?.WorkspacePath;
    }

    private static bool IsOAuthConfigured(GitHubOAuthOptions oauth)
    {
        return !string.IsNullOrWhiteSpace(oauth.ClientId)
               && !string.IsNullOrWhiteSpace(oauth.RedirectUri)
               && !string.IsNullOrWhiteSpace(oauth.AuthorizeEndpoint);
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
}

/// <summary>Request to create GitHub issue. TR-PLANNED-013.</summary>
public sealed class GitHubIssueRequest
{
    /// <summary>Issue title.</summary>
    public string? Title { get; set; }

    /// <summary>Issue body.</summary>
    public string? Body { get; set; }
}

/// <summary>Request to add comment. TR-PLANNED-013.</summary>
public sealed class GitHubCommentRequest
{
    /// <summary>Comment body.</summary>
    public string? Body { get; set; }
}

/// <summary>TR-MCP-GH-002: Request body for setting a workspace GitHub token.</summary>
public sealed class GitHubAuthTokenUpsertRequest
{
    /// <summary>OAuth access token or personal access token.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Optional token expiration timestamp in UTC.</summary>
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}

/// <summary>TR-MCP-GH-002: Workspace GitHub auth status response payload.</summary>
public sealed class GitHubAuthStatusResponse
{
    /// <summary>Resolved workspace path.</summary>
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Current auth mode (stored_token, cli_fallback, or none).</summary>
    public string AuthMode { get; set; } = "none";

    /// <summary>Whether a workspace token is stored.</summary>
    public bool HasStoredToken { get; set; }

    /// <summary>When the stored token was last updated.</summary>
    public DateTimeOffset? TokenUpdatedAtUtc { get; set; }

    /// <summary>When the stored token expires, if known.</summary>
    public DateTimeOffset? TokenExpiresAtUtc { get; set; }

    /// <summary>Whether ambient CLI auth fallback is allowed.</summary>
    public bool CliFallbackAllowed { get; set; }

    /// <summary>Whether OAuth app bootstrap settings are configured.</summary>
    public bool OAuthConfigured { get; set; }
}

/// <summary>TR-MCP-GH-001: OAuth app bootstrap configuration payload.</summary>
public sealed class GitHubOAuthConfigResponse
{
    /// <summary>GitHub OAuth app client identifier.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth redirect URI configured for the app.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Space-separated OAuth scopes.</summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>GitHub authorize endpoint URI.</summary>
    public string AuthorizeEndpoint { get; set; } = string.Empty;

    /// <summary>Whether OAuth values are complete enough to build an authorize URL.</summary>
    public bool IsConfigured { get; set; }
}
