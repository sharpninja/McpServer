using System.Text.Json.Serialization;

namespace McpServer.SessionLog.Validation.Models;

public sealed class SubmitResult
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("sourceType")] public string? SourceType { get; set; }
    [JsonPropertyName("sessionId")] public string? SessionId { get; set; }
}

public sealed class ErrorResult
{
    [JsonPropertyName("error")] public string? Error { get; set; }
}

public sealed class QueryResult
{
    [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
    [JsonPropertyName("limit")] public int Limit { get; set; }
    [JsonPropertyName("offset")] public int Offset { get; set; }
    [JsonPropertyName("items")] public List<SessionSummary>? Items { get; set; }
}

public sealed class SessionSummary
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("sourceType")] public string? SourceType { get; set; }
    [JsonPropertyName("sessionId")] public string? SessionId { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("model")] public string? Model { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("entryCount")] public int EntryCount { get; set; }
    [JsonPropertyName("started")] public string? Started { get; set; }
    [JsonPropertyName("lastUpdated")] public string? LastUpdated { get; set; }
    [JsonPropertyName("entries")] public List<EntryDto>? Entries { get; set; }
}

public sealed class EntryDto
{
    [JsonPropertyName("requestId")] public string? RequestId { get; set; }
    [JsonPropertyName("queryText")] public string? QueryText { get; set; }
    [JsonPropertyName("queryTitle")] public string? QueryTitle { get; set; }
    [JsonPropertyName("response")] public string? Response { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    [JsonPropertyName("actions")] public List<ActionDto>? Actions { get; set; }
    [JsonPropertyName("processingDialog")] public List<DialogItemDto>? ProcessingDialog { get; set; }
}

public sealed class ActionDto
{
    [JsonPropertyName("order")] public int Order { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("filePath")] public string? FilePath { get; set; }
}

public sealed class DialogItemDto
{
    [JsonPropertyName("timestamp")] public string? Timestamp { get; set; }
    [JsonPropertyName("role")] public string? Role { get; set; }
    [JsonPropertyName("content")] public string? Content { get; set; }
    [JsonPropertyName("category")] public string? Category { get; set; }
}

public sealed class DialogAppendResult
{
    [JsonPropertyName("agent")] public string? Agent { get; set; }
    [JsonPropertyName("sessionId")] public string? SessionId { get; set; }
    [JsonPropertyName("requestId")] public string? RequestId { get; set; }
    [JsonPropertyName("totalDialogCount")] public int TotalDialogCount { get; set; }
}
