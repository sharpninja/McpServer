using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.UI.Core.Models;

public sealed class McpSessionLogQueryResult
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("items")]
    public List<McpUnifiedSessionLogDto> Items { get; set; } = new();
}

public sealed class McpUnifiedSessionLogDto
{
    [JsonPropertyName("sourceType")]
    public string? SourceType { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("started")]
    public string? Started { get; set; }

    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("entryCount")]
    public int EntryCount { get; set; }

    [JsonPropertyName("totalTokens")]
    public int? TotalTokens { get; set; }

    [JsonPropertyName("cursorSessionLabel")]
    public string? CursorSessionLabel { get; set; }

    [JsonPropertyName("workspace")]
    public McpWorkspaceInfoDto? Workspace { get; set; }

    [JsonPropertyName("copilotStatistics")]
    public McpCopilotStatisticsDto? CopilotStatistics { get; set; }

    [JsonPropertyName("entries")]
    public List<McpUnifiedRequestEntryDto> Entries { get; set; } = new();
}

public sealed class McpWorkspaceInfoDto
{
    [JsonPropertyName("project")]
    public string? Project { get; set; }

    [JsonPropertyName("targetFramework")]
    public string? TargetFramework { get; set; }

    [JsonPropertyName("repository")]
    public string? Repository { get; set; }

    [JsonPropertyName("branch")]
    public string? Branch { get; set; }
}

public sealed class McpCopilotStatisticsDto
{
    [JsonPropertyName("averageSuccessScore")]
    public double? AverageSuccessScore { get; set; }

    [JsonPropertyName("totalNetTokens")]
    public int? TotalNetTokens { get; set; }

    [JsonPropertyName("totalNetPremiumRequests")]
    public int? TotalNetPremiumRequests { get; set; }

    [JsonPropertyName("completedCount")]
    public int? CompletedCount { get; set; }

    [JsonPropertyName("inProgressCount")]
    public int? InProgressCount { get; set; }
}

public sealed class McpUnifiedRequestEntryDto
{
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("queryText")]
    public string? QueryText { get; set; }

    [JsonPropertyName("queryTitle")]
    public string? QueryTitle { get; set; }

    [JsonPropertyName("response")]
    public string? Response { get; set; }

    [JsonPropertyName("interpretation")]
    public string? Interpretation { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("modelProvider")]
    public string? ModelProvider { get; set; }

    [JsonPropertyName("tokenCount")]
    public int? TokenCount { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("contextList")]
    public List<string>? ContextList { get; set; }

    [JsonPropertyName("failureNote")]
    public string? FailureNote { get; set; }

    [JsonPropertyName("score")]
    public double? Score { get; set; }

    [JsonPropertyName("isPremium")]
    public bool? IsPremium { get; set; }

    [JsonPropertyName("rawContext")]
    public object? RawContext { get; set; }

    [JsonPropertyName("originalEntry")]
    public object? OriginalEntry { get; set; }

    [JsonPropertyName("actions")]
    public List<McpUnifiedActionDto>? Actions { get; set; }

    [JsonPropertyName("processingDialog")]
    public List<McpProcessingDialogItemDto>? ProcessingDialog { get; set; }
}

public sealed class McpUnifiedActionDto
{
    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("filePath")]
    public string? FilePath { get; set; }
}

public sealed class McpProcessingDialogItemDto
{
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

