using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>Unified session log DTO for submit and query.</summary>
public sealed class UnifiedSessionLogDto
{
    /// <summary>Agent source type (e.g. Copilot, Cursor, Cline).</summary>
    [JsonPropertyName("sourceType")]
    public string? SourceType { get; set; }

    /// <summary>Stable session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    /// <summary>Session title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>AI model used.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>Session start time (ISO 8601).</summary>
    [JsonPropertyName("started")]
    public string? Started { get; set; }

    /// <summary>Last activity time (ISO 8601).</summary>
    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; set; }

    /// <summary>Session status (in_progress, completed).</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Number of entries.</summary>
    [JsonPropertyName("entryCount")]
    public int EntryCount { get; set; }

    /// <summary>Workspace metadata.</summary>
    [JsonPropertyName("workspace")]
    public WorkspaceInfoDto? Workspace { get; set; }

    /// <summary>Request entries.</summary>
    [JsonPropertyName("entries")]
    public List<UnifiedRequestEntryDto>? Entries { get; set; }

    /// <summary>Total tokens across all entries.</summary>
    [JsonPropertyName("totalTokens")]
    public int? TotalTokens { get; set; }

    /// <summary>Optional Cursor session label.</summary>
    [JsonPropertyName("cursorSessionLabel")]
    public string? CursorSessionLabel { get; set; }

    /// <summary>Optional Copilot statistics.</summary>
    [JsonPropertyName("copilotStatistics")]
    public CopilotStatisticsDto? CopilotStatistics { get; set; }
}

/// <summary>Workspace metadata within a session log.</summary>
public sealed class WorkspaceInfoDto
{
    /// <summary>Project name.</summary>
    [JsonPropertyName("project")]
    public string? Project { get; set; }

    /// <summary>Target framework.</summary>
    [JsonPropertyName("targetFramework")]
    public string? TargetFramework { get; set; }

    /// <summary>Repository URL.</summary>
    [JsonPropertyName("repository")]
    public string? Repository { get; set; }

    /// <summary>Git branch.</summary>
    [JsonPropertyName("branch")]
    public string? Branch { get; set; }
}

/// <summary>A single request entry within a session log.</summary>
public sealed class UnifiedRequestEntryDto
{
    /// <summary>Unique request identifier within the session.</summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    /// <summary>Timestamp (ISO 8601).</summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>Full user query text.</summary>
    [JsonPropertyName("queryText")]
    public string? QueryText { get; set; }

    /// <summary>Short query title.</summary>
    [JsonPropertyName("queryTitle")]
    public string? QueryTitle { get; set; }

    /// <summary>Agent response text.</summary>
    [JsonPropertyName("response")]
    public string? Response { get; set; }

    /// <summary>Agent interpretation of the query.</summary>
    [JsonPropertyName("interpretation")]
    public string? Interpretation { get; set; }

    /// <summary>Entry status (completed, in_progress).</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Actions taken.</summary>
    [JsonPropertyName("actions")]
    public List<UnifiedActionDto>? Actions { get; set; }

    /// <summary>Model used for this entry.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>Approximate token count.</summary>
    [JsonPropertyName("tokenCount")]
    public int? TokenCount { get; set; }

    /// <summary>Relevant tags.</summary>
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    /// <summary>Referenced files or resources.</summary>
    [JsonPropertyName("contextList")]
    public List<string>? ContextList { get; set; }

    /// <summary>Provider-specific raw context payload.</summary>
    [JsonPropertyName("rawContext")]
    public object? RawContext { get; set; }

    /// <summary>Model provider identifier.</summary>
    [JsonPropertyName("modelProvider")]
    public string? ModelProvider { get; set; }

    /// <summary>Failure note (when present).</summary>
    [JsonPropertyName("failureNote")]
    public string? FailureNote { get; set; }

    /// <summary>Optional score.</summary>
    [JsonPropertyName("score")]
    public double? Score { get; set; }

    /// <summary>Whether the request used premium capacity.</summary>
    [JsonPropertyName("isPremium")]
    public bool? IsPremium { get; set; }

    /// <summary>Original provider-specific entry payload.</summary>
    [JsonPropertyName("originalEntry")]
    public object? OriginalEntry { get; set; }

    /// <summary>Processing dialog items.</summary>
    [JsonPropertyName("processingDialog")]
    public List<ProcessingDialogItemDto>? ProcessingDialog { get; set; }

    /// <summary>Git commits made during this request entry.</summary>
    [JsonPropertyName("commits")]
    public List<SessionLogCommitDto>? Commits { get; set; }

    /// <summary>Design decisions made during this interaction.</summary>
    [JsonPropertyName("designDecisions")]
    public List<string>? DesignDecisions { get; set; }

    /// <summary>Requirement IDs discovered or created.</summary>
    [JsonPropertyName("requirementsDiscovered")]
    public List<string>? RequirementsDiscovered { get; set; }

    /// <summary>File paths modified during this interaction.</summary>
    [JsonPropertyName("filesModified")]
    public List<string>? FilesModified { get; set; }

    /// <summary>Blockers or issues preventing progress.</summary>
    [JsonPropertyName("blockers")]
    public List<string>? Blockers { get; set; }
}

/// <summary>Copilot statistics summary when present on a session log item.</summary>
public sealed class CopilotStatisticsDto
{
    /// <summary>Average success score across requests.</summary>
    [JsonPropertyName("averageSuccessScore")]
    public double? AverageSuccessScore { get; set; }

    /// <summary>Total net tokens consumed.</summary>
    [JsonPropertyName("totalNetTokens")]
    public int? TotalNetTokens { get; set; }

    /// <summary>Total net premium requests consumed.</summary>
    [JsonPropertyName("totalNetPremiumRequests")]
    public int? TotalNetPremiumRequests { get; set; }

    /// <summary>Completed request count.</summary>
    [JsonPropertyName("completedCount")]
    public int? CompletedCount { get; set; }

    /// <summary>In-progress request count.</summary>
    [JsonPropertyName("inProgressCount")]
    public int? InProgressCount { get; set; }
}

/// <summary>An action taken within a request entry.</summary>
public sealed class UnifiedActionDto
{
    /// <summary>Execution order.</summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <summary>Action description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Action type.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Action status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>File path affected.</summary>
    [JsonPropertyName("filePath")]
    public string? FilePath { get; set; }
}

/// <summary>A processing dialog item for streaming reasoning.</summary>
public sealed class ProcessingDialogItemDto
{
    /// <summary>Timestamp (ISO 8601).</summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>Role: model, tool, system, or user.</summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>Content text.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>Category: reasoning, tool_call, tool_result, observation, decision.</summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

/// <summary>Git commit recorded during a session log request entry.</summary>
public sealed class SessionLogCommitDto
{
    /// <summary>Git commit SHA hash.</summary>
    [JsonPropertyName("sha")]
    public string? Sha { get; set; }

    /// <summary>Git branch name.</summary>
    [JsonPropertyName("branch")]
    public string? Branch { get; set; }

    /// <summary>Commit message text.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>Commit author name or email.</summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>Commit timestamp (ISO 8601).</summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>Files changed in this commit.</summary>
    [JsonPropertyName("filesChanged")]
    public List<string>? FilesChanged { get; set; }
}

/// <summary>Result of a session log query.</summary>
public sealed class SessionLogQueryResult
{
    /// <summary>Total matching session logs.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>Page size limit.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    /// <summary>Page offset.</summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>Session log items.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<UnifiedSessionLogDto> Items { get; set; } = [];
}

/// <summary>Result of submitting a session log.</summary>
public sealed class SessionLogSubmitResult
{
    /// <summary>Database row ID.</summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>Agent source type.</summary>
    [JsonPropertyName("sourceType")]
    public string? SourceType { get; set; }

    /// <summary>Session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}

/// <summary>Result of appending dialog items.</summary>
public sealed class DialogAppendResult
{
    /// <summary>Agent identifier.</summary>
    [JsonPropertyName("agent")]
    public string? Agent { get; set; }

    /// <summary>Session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    /// <summary>Request identifier.</summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    /// <summary>Total dialog items after append.</summary>
    [JsonPropertyName("totalDialogCount")]
    public int TotalDialogCount { get; set; }
}
