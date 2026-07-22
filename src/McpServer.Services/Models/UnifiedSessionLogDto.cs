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

    /// <summary>Optional linked agent definition identifier resolved from the source type.</summary>
    [JsonPropertyName("agentDefinitionId")]
    public string? AgentDefinitionId { get; set; }

    /// <summary>Provider-native agent session identifier for the session header.</summary>
    [JsonPropertyName("agentSessionId")]
    public string? AgentSessionId { get; set; }

    /// <summary>Provider-native transcript file path for the session header.</summary>
    [JsonPropertyName("agentSessionTranscriptFile")]
    public string? AgentSessionTranscriptFile { get; set; }

    /// <summary>Agent executable path captured in the session header.</summary>
    [JsonPropertyName("agentExecutablePath")]
    public string? AgentExecutablePath { get; set; }

    /// <summary>Agent executable version captured in the session header.</summary>
    [JsonPropertyName("agentExecutableVersion")]
    public string? AgentExecutableVersion { get; set; }

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

    /// <summary>Number of request/response turns in the session.</summary>
    [JsonPropertyName("turnCount")]
    public int TurnCount { get; set; }

    /// <summary>Workspace metadata.</summary>
    [JsonPropertyName("workspace")]
    public WorkspaceInfoDto? Workspace { get; set; }

    /// <summary>Ordered request/response turns.</summary>
    [JsonPropertyName("turns")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    public ICollection<UnifiedRequestEntryDto>? Turns { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Total token count across all turns.</summary>
    [JsonPropertyName("totalTokens")]
    public int? TotalTokens { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Cursor-specific session label.</summary>
    [JsonPropertyName("cursorSessionLabel")]
    public string? CursorSessionLabel { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Copilot-specific aggregate statistics.</summary>
    [JsonPropertyName("copilotStatistics")]
    public CopilotStatisticsDto? CopilotStatistics { get; set; }
}

/// <summary>TR-PLANNED-CORE-013: Copilot aggregate statistics for a session.</summary>
public sealed class CopilotStatisticsDto
{
    /// <summary>Average success score across turns.</summary>
    [JsonPropertyName("averageSuccessScore")]
    public double? AverageSuccessScore { get; set; }

    /// <summary>Total net tokens used.</summary>
    [JsonPropertyName("totalNetTokens")]
    public int? TotalNetTokens { get; set; }

    /// <summary>Total net premium requests.</summary>
    [JsonPropertyName("totalNetPremiumRequests")]
    public int? TotalNetPremiumRequests { get; set; }

    /// <summary>Number of completed turns.</summary>
    [JsonPropertyName("completedCount")]
    public int? CompletedCount { get; set; }

    /// <summary>Number of in-progress turns.</summary>
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
    public ICollection<UnifiedActionDto>? Actions { get; set; }

    /// <summary>TR-PLANNED-CORE-013: AI model used for this specific entry.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Model provider (e.g. OpenAI, Anthropic).</summary>
    [JsonPropertyName("modelProvider")]
    public string? ModelProvider { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Token count for this entry.</summary>
    [JsonPropertyName("tokenCount")]
    public int? TokenCount { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Tags associated with this entry.</summary>
    [JsonPropertyName("tags")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    public ICollection<string>? Tags { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Context items referenced by this entry.</summary>
    [JsonPropertyName("contextList")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    public ICollection<string>? ContextList { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Failure note if the entry failed.</summary>
    [JsonPropertyName("failureNote")]
    public string? FailureNote { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Success score for this entry.</summary>
    [JsonPropertyName("score")]
    public double? Score { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Whether this was a premium request.</summary>
    [JsonPropertyName("isPremium")]
    public bool? IsPremium { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Raw context data (stored as JSON).</summary>
    [JsonPropertyName("rawContext")]
    public object? RawContext { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Original entry before normalization (stored as JSON).</summary>
    [JsonPropertyName("originalEntry")]
    public object? OriginalEntry { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Processing dialog — model reasoning, tool calls, and execution trace appended during request execution.</summary>
    [JsonPropertyName("processingDialog")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    public ICollection<ProcessingDialogItemDto>? ProcessingDialog { get; set; }

    /// <summary>Git commits made during this request entry.</summary>
    [JsonPropertyName("commits")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    public ICollection<SessionLogCommitDto>? Commits { get; set; }

    /// <summary>Design decisions made during this interaction (decision text, rationale, alternatives).</summary>
    [JsonPropertyName("designDecisions")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    public ICollection<string>? DesignDecisions { get; set; }

    /// <summary>Requirement IDs discovered or created during this interaction (e.g. "TR-MCP-CQRS-001", "FR-MCP-029").</summary>
    [JsonPropertyName("requirementsDiscovered")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    public ICollection<string>? RequirementsDiscovered { get; set; }

    /// <summary>File paths modified during this interaction.</summary>
    [JsonPropertyName("filesModified")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    public ICollection<string>? FilesModified { get; set; }

    /// <summary>Blockers or issues preventing progress during this interaction.</summary>
    [JsonPropertyName("blockers")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    public ICollection<string>? Blockers { get; set; }
}

/// <summary>TR-PLANNED-CORE-013: Single processing dialog entry recording model reasoning during request execution.</summary>
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

/// <summary>FR-SUPPORT-010: Git commit recorded during a session log request entry.</summary>
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
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "JSON deserialization requires setter")]
    public ICollection<string>? FilesChanged { get; set; }
}
