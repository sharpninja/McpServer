using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>
/// TR-MCP-CLIENT-001: Request body for <c>POST /mcpserver/sessionlog/{agent}/{sessionId}/open</c>.
/// Replaces the compiler-generated anonymous body that the source-generated
/// <c>McpClientJsonContext</c> resolver could not produce <c>JsonTypeInfo</c> for.
/// </summary>
public sealed record SessionLifecycleOpenRequest
{
    /// <summary>Optional session title applied when the open call creates the session.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>Optional model identifier recorded on a newly created session.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; init; }
}

/// <summary>
/// TR-MCP-CLIENT-001: Request body for
/// <c>POST /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/begin</c>.
/// Replaces the compiler-generated anonymous body that the source-generated
/// <c>McpClientJsonContext</c> resolver could not produce <c>JsonTypeInfo</c> for.
/// </summary>
public sealed record SessionLifecycleBeginRequest
{
    /// <summary>Optional short title for the turn being started.</summary>
    [JsonPropertyName("queryTitle")]
    public string? QueryTitle { get; init; }

    /// <summary>Optional verbatim prompt text for the turn being started.</summary>
    [JsonPropertyName("queryText")]
    public string? QueryText { get; init; }

    /// <summary>Optional ISO-8601 turn timestamp; the server stamps <c>UtcNow</c> when omitted.</summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }

    /// <summary>Optional model identifier recorded on the turn.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; init; }
}

/// <summary>
/// TR-MCP-CLIENT-001: Request body for the dedicated retitle endpoints
/// <c>POST /mcpserver/sessionlog/{agent}/{sessionId}/title</c> and
/// <c>POST /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/title</c>.
/// Replaces the compiler-generated anonymous body that the source-generated
/// <c>McpClientJsonContext</c> resolver could not produce <c>JsonTypeInfo</c> for.
/// </summary>
public sealed record SessionTitleRequest
{
    /// <summary>New title to apply to the target session or turn.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }
}
