using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Models;

/// <summary>
/// FR-SUPPORT-010: DTO matching docs/schemas/UnifiedModel.schema.json for session log normalization.
/// </summary>
public sealed class UnifiedSessionLogDto
{
    /// <summary>Agent source type (e.g. Cursor, Copilot).</summary>
    [JsonPropertyName("sourceType")]
    public string? SourceType { get; set; }

    /// <summary>Unique session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    /// <summary>Human-readable session title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>AI model used for the session.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>ISO 8601 timestamp when the session started.</summary>
    [JsonPropertyName("started")]
    public string? Started { get; set; }

    /// <summary>ISO 8601 timestamp of the last update.</summary>
    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; set; }

    /// <summary>Session status (e.g. in_progress, completed).</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Number of request/response entries in the session.</summary>
    [JsonPropertyName("entryCount")]
    public int EntryCount { get; set; }

    /// <summary>Workspace metadata.</summary>
    [JsonPropertyName("workspace")]
    public WorkspaceInfoDto? Workspace { get; set; }

    /// <summary>Ordered request/response entries.</summary>
    [JsonPropertyName("entries")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "DTO for JSON schema compatibility")]
    public List<UnifiedRequestEntryDto>? Entries { get; set; }

    /// <summary>TR-PLANNED-013: Total token count across all entries.</summary>
    [JsonPropertyName("totalTokens")]
    public int? TotalTokens { get; set; }

    /// <summary>TR-PLANNED-013: Cursor-specific session label.</summary>
    [JsonPropertyName("cursorSessionLabel")]
    public string? CursorSessionLabel { get; set; }

    /// <summary>TR-PLANNED-013: Copilot-specific aggregate statistics.</summary>
    [JsonPropertyName("copilotStatistics")]
    public CopilotStatisticsDto? CopilotStatistics { get; set; }
}

/// <summary>TR-PLANNED-013: Copilot aggregate statistics for a session.</summary>
public sealed class CopilotStatisticsDto
{
    /// <summary>Average success score across entries.</summary>
    [JsonPropertyName("averageSuccessScore")]
    public double? AverageSuccessScore { get; set; }

    /// <summary>Total net tokens used.</summary>
    [JsonPropertyName("totalNetTokens")]
    public int? TotalNetTokens { get; set; }

    /// <summary>Total net premium requests.</summary>
    [JsonPropertyName("totalNetPremiumRequests")]
    public int? TotalNetPremiumRequests { get; set; }

    /// <summary>Number of completed entries.</summary>
    [JsonPropertyName("completedCount")]
    public int? CompletedCount { get; set; }

    /// <summary>Number of in-progress entries.</summary>
    [JsonPropertyName("inProgressCount")]
    public int? InProgressCount { get; set; }
}

/// <summary>FR-SUPPORT-010: Workspace section of UnifiedModel.</summary>
public sealed class WorkspaceInfoDto
{
    /// <summary>Project name.</summary>
    [JsonPropertyName("project")]
    public string? Project { get; set; }

    /// <summary>Target framework (e.g. .NET 9).</summary>
    [JsonPropertyName("targetFramework")]
    public string? TargetFramework { get; set; }

    /// <summary>Repository URL or name.</summary>
    [JsonPropertyName("repository")]
    public string? Repository { get; set; }

    /// <summary>Git branch name.</summary>
    [JsonPropertyName("branch")]
    public string? Branch { get; set; }
}

/// <summary>FR-SUPPORT-010: Single request entry in a session log.</summary>
public sealed class UnifiedRequestEntryDto
{
    /// <summary>Unique request identifier.</summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    /// <summary>ISO 8601 timestamp of the request.</summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>Full user query text.</summary>
    [JsonPropertyName("queryText")]
    public string? QueryText { get; set; }

    /// <summary>Short title summarizing the query.</summary>
    [JsonPropertyName("queryTitle")]
    public string? QueryTitle { get; set; }

    /// <summary>Agent response text.</summary>
    [JsonPropertyName("response")]
    public string? Response { get; set; }

    /// <summary>Agent interpretation of the request.</summary>
    [JsonPropertyName("interpretation")]
    public string? Interpretation { get; set; }

    /// <summary>Entry status (e.g. completed, in_progress).</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Actions taken by the agent for this request.</summary>
    [JsonPropertyName("actions")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "DTO for JSON schema compatibility")]
    public List<UnifiedActionDto>? Actions { get; set; }

    /// <summary>TR-PLANNED-013: AI model used for this specific entry.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>TR-PLANNED-013: Model provider (e.g. OpenAI, Anthropic).</summary>
    [JsonPropertyName("modelProvider")]
    public string? ModelProvider { get; set; }

    /// <summary>TR-PLANNED-013: Token count for this entry.</summary>
    [JsonPropertyName("tokenCount")]
    public int? TokenCount { get; set; }

    /// <summary>TR-PLANNED-013: Tags associated with this entry.</summary>
    [JsonPropertyName("tags")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "DTO for JSON schema compatibility")]
    public List<string>? Tags { get; set; }

    /// <summary>TR-PLANNED-013: Context items referenced by this entry.</summary>
    [JsonPropertyName("contextList")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "DTO for JSON schema compatibility")]
    public List<string>? ContextList { get; set; }

    /// <summary>TR-PLANNED-013: Failure note if the entry failed.</summary>
    [JsonPropertyName("failureNote")]
    public string? FailureNote { get; set; }

    /// <summary>TR-PLANNED-013: Success score for this entry.</summary>
    [JsonPropertyName("score")]
    public double? Score { get; set; }

    /// <summary>TR-PLANNED-013: Whether this was a premium request.</summary>
    [JsonPropertyName("isPremium")]
    public bool? IsPremium { get; set; }

    /// <summary>TR-PLANNED-013: Raw context data (stored as JSON).</summary>
    [JsonPropertyName("rawContext")]
    public object? RawContext { get; set; }

    /// <summary>TR-PLANNED-013: Original entry before normalization (stored as JSON).</summary>
    [JsonPropertyName("originalEntry")]
    public object? OriginalEntry { get; set; }

    /// <summary>TR-PLANNED-013: Processing dialog — model reasoning, tool calls, and execution trace appended during request execution.</summary>
    [JsonPropertyName("processingDialog")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "DTO for JSON schema compatibility")]
    public List<ProcessingDialogItemDto>? ProcessingDialog { get; set; }
}

/// <summary>TR-PLANNED-013: Single processing dialog entry recording model reasoning during request execution.</summary>
public sealed class ProcessingDialogItemDto
{
    /// <summary>ISO 8601 timestamp when this dialog item was recorded.</summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>Role of the speaker (e.g. model, tool, system, user).</summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>Content of the processing dialog entry.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>Optional category (e.g. reasoning, tool_call, tool_result, observation, decision).</summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

/// <summary>FR-SUPPORT-010: Action in an entry.</summary>
public sealed class UnifiedActionDto
{
    /// <summary>Execution order within the request.</summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <summary>Human-readable description of the action.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Action type (e.g. edit, create, delete).</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Action status (e.g. completed, failed).</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>File path affected by this action.</summary>
    [JsonPropertyName("filePath")]
    public string? FilePath { get; set; }
}
