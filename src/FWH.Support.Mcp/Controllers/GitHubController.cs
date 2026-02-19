using FWH.Support.Mcp.Models;
using FWH.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace FWH.Support.Mcp.Controllers;

/// <summary>
/// TR-PLANNED-013, TR-GH-013-006: GitHub metadata via gh CLI (issues and PRs).
/// FR-SUPPORT-010, FR-SUPPORT-013: List, create, comment, update, close, reopen, sync endpoints.
/// </summary>
[ApiController]
[Route("mcp/gh")]
public sealed class GitHubController : ControllerBase
{
    private readonly IGitHubCliService _gh;
    private readonly IIssueTodoSyncService? _syncService;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    public GitHubController(IGitHubCliService gh, IIssueTodoSyncService? syncService = null)
    {
        _gh = gh;
        _syncService = syncService;
    }

    /// <summary>TR-PLANNED-013: List issues (gh.issues.list).</summary>
    /// <param name="state">Optional filter: open, closed, all.</param>
    /// <param name="limit">Max issues to return (1–100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("issues")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> ListIssuesAsync([FromQuery] string? state, [FromQuery] int limit = 30, CancellationToken cancellationToken = default)
    {
        var result = await _gh.ListIssuesAsync(state, limit, cancellationToken).ConfigureAwait(false);
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
        var result = await _gh.CloseIssueAsync(number, reason, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage ?? "failed to close issue" });
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

    /// <summary>TR-PLANNED-013: List PRs (gh.prs.list).</summary>
    /// <param name="state">Optional filter: open, closed, all.</param>
    /// <param name="limit">Max PRs to return (1–100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("pulls")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> ListPullsAsync([FromQuery] string? state, [FromQuery] int limit = 30, CancellationToken cancellationToken = default)
    {
        var result = await _gh.ListPullsAsync(state, limit, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return Ok(new { pulls = Array.Empty<object>(), error = result.Error });
        return Ok(new { pulls = result.Pulls.Select(p => new { p.Number, p.Title, p.Url, p.State }).ToList() });
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
        var result = await _syncService.SyncAllIssuesToTodosAsync(state, limit, cancellationToken).ConfigureAwait(false);
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
            return Ok(new { success = true, todoId = result.Item?.Id });
        }
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
