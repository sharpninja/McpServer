namespace McpServer.Support.Mcp.Models;

/// <summary>
/// TR-GH-013-001: Full issue detail including body, labels, assignees, timestamps, and comments.
/// FR-SUPPORT-013: Complete issue metadata for MCP integration.
/// </summary>
/// <param name="Number">Issue number.</param>
/// <param name="Title">Issue title.</param>
/// <param name="Body">Issue body (markdown).</param>
/// <param name="State">Issue state (OPEN, CLOSED).</param>
/// <param name="Url">Issue URL on GitHub.</param>
/// <param name="Labels">List of labels.</param>
/// <param name="Assignees">List of assignee logins.</param>
/// <param name="Milestone">Milestone title, or null.</param>
/// <param name="CreatedAt">When the issue was created.</param>
/// <param name="UpdatedAt">When the issue was last updated.</param>
/// <param name="ClosedAt">When the issue was closed, or null.</param>
/// <param name="Author">Issue author login.</param>
/// <param name="Comments">List of comments.</param>
public sealed record GitHubIssueDetail(
    int Number,
    string Title,
    string? Body,
    string? State,
    string? Url,
    IReadOnlyList<GitHubLabel> Labels,
    IReadOnlyList<string> Assignees,
    string? Milestone,
    string? CreatedAt,
    string? UpdatedAt,
    string? ClosedAt,
    string? Author,
    IReadOnlyList<GitHubIssueComment> Comments);

/// <summary>TR-GH-013-001: Label with name, color, and description.</summary>
/// <param name="Name">Label name.</param>
/// <param name="Color">Hex color code.</param>
/// <param name="Description">Label description.</param>
public sealed record GitHubLabel(string Name, string? Color, string? Description);

/// <summary>TR-GH-013-001: Single issue comment.</summary>
/// <param name="Author">Comment author login.</param>
/// <param name="Body">Comment body text.</param>
/// <param name="CreatedAt">When the comment was created.</param>
public sealed record GitHubIssueComment(string? Author, string? Body, string? CreatedAt);

/// <summary>TR-GH-013-001: Request to update an issue.</summary>
public sealed class GitHubIssueUpdateRequest
{
    /// <summary>Updated title (null = no change).</summary>
    public string? Title { get; set; }

    /// <summary>Updated body (null = no change).</summary>
    public string? Body { get; set; }

    /// <summary>Labels to add.</summary>
    public IReadOnlyList<string>? AddLabels { get; set; }

    /// <summary>Labels to remove.</summary>
    public IReadOnlyList<string>? RemoveLabels { get; set; }

    /// <summary>Assignees to add.</summary>
    public IReadOnlyList<string>? AddAssignees { get; set; }

    /// <summary>Assignees to remove.</summary>
    public IReadOnlyList<string>? RemoveAssignees { get; set; }

    /// <summary>Milestone to set (null = no change).</summary>
    public string? Milestone { get; set; }
}

/// <summary>TR-GH-013-001: Result of getting full issue detail.</summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="Issue">Issue detail, or null on failure.</param>
/// <param name="ErrorMessage">Error message on failure.</param>
public sealed record GitHubIssueDetailResult(bool Success, GitHubIssueDetail? Issue, string? ErrorMessage);

/// <summary>TR-GH-013-001: Result of a mutation (update, close, reopen).</summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="Url">Affected issue URL, or null.</param>
/// <param name="ErrorMessage">Error message on failure.</param>
public sealed record GitHubMutationResult(bool Success, string? Url, string? ErrorMessage);

/// <summary>TR-GH-013-001: Result of listing labels.</summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="Labels">List of labels, or null on failure.</param>
/// <param name="ErrorMessage">Error message on failure.</param>
public sealed record GitHubLabelsResult(bool Success, IReadOnlyList<GitHubLabel>? Labels, string? ErrorMessage);
