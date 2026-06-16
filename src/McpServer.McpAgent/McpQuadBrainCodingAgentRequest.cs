using System.Text.Json.Serialization;

namespace McpServer.McpAgent;

/// <summary>
/// FR-MCP-137/TR-MCP-AGENT-016: Request accepted by the hosted MCP coding agent when routing
/// a coding task through MCP Server Quad Brain orchestration.
/// </summary>
public sealed class McpQuadBrainCodingAgentRequest
{
    /// <summary>
    /// Gets or sets the coding prompt to evaluate through the Quad Brain roles.
    /// </summary>
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional coding task kind, such as <c>implementation</c>,
    /// <c>review</c>, <c>debugging</c>, or <c>test-design</c>.
    /// </summary>
    [JsonPropertyName("taskKind")]
    public string? TaskKind { get; set; }

    /// <summary>
    /// Gets or sets the session-log turn identifier that owns this coding-agent execution.
    /// </summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>
    /// Gets or sets whether committed CuriosityEngine output should be eligible for GraphRAG admission.
    /// </summary>
    [JsonPropertyName("admitCuriosityToGraphRag")]
    public bool AdmitCuriosityToGraphRag { get; set; }

    /// <summary>
    /// Gets or sets caller metadata to preserve in the Quad Brain transaction evidence.
    /// </summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}
