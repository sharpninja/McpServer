using System.Text.Json.Serialization;

namespace McpServer.SessionLog.Validation.Models;

/// <summary>
/// Validation contract type <c>SubmitResult</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
public sealed class SubmitResult
{
    /// <summary>
    /// Gets or sets <c>Id</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("id")] public int Id { get; set; }
    /// <summary>
    /// Gets or sets <c>SourceType</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("sourceType")] public string? SourceType { get; set; }
    /// <summary>
    /// Gets or sets <c>SessionId</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("sessionId")] public string? SessionId { get; set; }
}

/// <summary>
/// Validation contract type <c>ErrorResult</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
public sealed class ErrorResult
{
    /// <summary>
    /// Gets or sets <c>Error</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("error")] public string? Error { get; set; }
}

/// <summary>
/// Validation contract type <c>QueryResult</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
public sealed class QueryResult
{
    /// <summary>
    /// Gets or sets <c>TotalCount</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
    /// <summary>
    /// Gets or sets <c>Limit</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("limit")] public int Limit { get; set; }
    /// <summary>
    /// Gets or sets <c>Offset</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("offset")] public int Offset { get; set; }
    /// <summary>
    /// Gets or sets <c>Items</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("items")] public List<SessionSummary>? Items { get; set; }
}

/// <summary>
/// Validation contract type <c>SessionSummary</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
public sealed class SessionSummary
{
    /// <summary>
    /// Gets or sets <c>Id</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("id")] public int Id { get; set; }
    /// <summary>
    /// Gets or sets <c>SourceType</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("sourceType")] public string? SourceType { get; set; }
    /// <summary>
    /// Gets or sets <c>SessionId</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("sessionId")] public string? SessionId { get; set; }
    /// <summary>
    /// Gets or sets <c>Title</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("title")] public string? Title { get; set; }
    /// <summary>
    /// Gets or sets <c>Model</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("model")] public string? Model { get; set; }
    /// <summary>
    /// Gets or sets <c>Status</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("status")] public string? Status { get; set; }
    /// <summary>
    /// Gets or sets <c>EntryCount</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("entryCount")] public int EntryCount { get; set; }
    /// <summary>
    /// Gets or sets <c>Started</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("started")] public string? Started { get; set; }
    /// <summary>
    /// Gets or sets <c>LastUpdated</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("lastUpdated")] public string? LastUpdated { get; set; }
    /// <summary>
    /// Gets or sets <c>Entries</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("entries")] public List<EntryDto>? Entries { get; set; }
}

/// <summary>
/// Validation contract type <c>EntryDto</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
public sealed class EntryDto
{
    /// <summary>
    /// Gets or sets <c>RequestId</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("requestId")] public string? RequestId { get; set; }
    /// <summary>
    /// Gets or sets <c>QueryText</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("queryText")] public string? QueryText { get; set; }
    /// <summary>
    /// Gets or sets <c>QueryTitle</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("queryTitle")] public string? QueryTitle { get; set; }
    /// <summary>
    /// Gets or sets <c>Response</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("response")] public string? Response { get; set; }
    /// <summary>
    /// Gets or sets <c>Status</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("status")] public string? Status { get; set; }
    /// <summary>
    /// Gets or sets <c>Tags</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    /// <summary>
    /// Gets or sets <c>Actions</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("actions")] public List<ActionDto>? Actions { get; set; }
    /// <summary>
    /// Gets or sets <c>ProcessingDialog</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("processingDialog")] public List<DialogItemDto>? ProcessingDialog { get; set; }
}

/// <summary>
/// Validation contract type <c>ActionDto</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
public sealed class ActionDto
{
    /// <summary>
    /// Gets or sets <c>Order</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("order")] public int Order { get; set; }
    /// <summary>
    /// Gets or sets <c>Description</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("description")] public string? Description { get; set; }
    /// <summary>
    /// Gets or sets <c>Type</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("type")] public string? Type { get; set; }
    /// <summary>
    /// Gets or sets <c>Status</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("status")] public string? Status { get; set; }
    /// <summary>
    /// Gets or sets <c>FilePath</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("filePath")] public string? FilePath { get; set; }
}

/// <summary>
/// Validation contract type <c>DialogItemDto</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
public sealed class DialogItemDto
{
    /// <summary>
    /// Gets or sets <c>Timestamp</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("timestamp")] public string? Timestamp { get; set; }
    /// <summary>
    /// Gets or sets <c>Role</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("role")] public string? Role { get; set; }
    /// <summary>
    /// Gets or sets <c>Content</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("content")] public string? Content { get; set; }
    /// <summary>
    /// Gets or sets <c>Category</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("category")] public string? Category { get; set; }
}

/// <summary>
/// Validation contract type <c>DialogAppendResult</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
public sealed class DialogAppendResult
{
    /// <summary>
    /// Gets or sets <c>Agent</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("agent")] public string? Agent { get; set; }
    /// <summary>
    /// Gets or sets <c>SessionId</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("sessionId")] public string? SessionId { get; set; }
    /// <summary>
    /// Gets or sets <c>RequestId</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("requestId")] public string? RequestId { get; set; }
    /// <summary>
    /// Gets or sets <c>TotalDialogCount</c> for validation payload/state handling.
    /// </summary>
    [JsonPropertyName("totalDialogCount")] public int TotalDialogCount { get; set; }
}
