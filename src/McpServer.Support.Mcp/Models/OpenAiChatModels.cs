using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Models;

/// <summary>FR-MCP-QBOPENAI-001: One message in an OpenAI-compatible chat-completion request.</summary>
public sealed class OpenAiChatMessage
{
    /// <summary>Message role (system, user, assistant, tool).</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    /// <summary>Message content.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

/// <summary>FR-MCP-QBOPENAI-001: OpenAI-compatible chat-completion request accepted by the QuadBrain endpoint.</summary>
public sealed class OpenAiChatCompletionRequest
{
    /// <summary>Requested model id (informational; QuadBrain orchestration backs every model).</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>Ordered chat messages.</summary>
    [JsonPropertyName("messages")]
    public List<OpenAiChatMessage> Messages { get; set; } = [];

    /// <summary>Tool/function definitions the model may call.</summary>
    [JsonPropertyName("tools")]
    public List<OpenAiToolDefinition>? Tools { get; set; }

    /// <summary>Tool-choice directive (<c>auto</c>, <c>none</c>, <c>required</c>, or a specific tool object).</summary>
    [JsonPropertyName("tool_choice")]
    public JsonElement? ToolChoice { get; set; }

    /// <summary>Whether a streamed Server-Sent Events response was requested.</summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

/// <summary>FR-MCP-QBOPENAI-001: A tool the model may call (OpenAI function-tool shape).</summary>
public sealed class OpenAiToolDefinition
{
    /// <summary>Tool type, always <c>function</c>.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>Function definition.</summary>
    [JsonPropertyName("function")]
    public OpenAiFunctionDefinition Function { get; set; } = new();
}

/// <summary>FR-MCP-QBOPENAI-001: An OpenAI function definition.</summary>
public sealed class OpenAiFunctionDefinition
{
    /// <summary>Function name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Function description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>JSON-schema parameters object.</summary>
    [JsonPropertyName("parameters")]
    public JsonElement? Parameters { get; set; }
}

/// <summary>FR-MCP-QBOPENAI-001: Assistant message in an OpenAI-compatible chat-completion response.</summary>
public sealed class OpenAiChatResponseMessage
{
    /// <summary>Message role (always <c>assistant</c> for the completion).</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "assistant";

    /// <summary>Assistant content (the QuadBrain Arbiter decision); null when the assistant emits tool calls.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>Tool calls the assistant elected to make, when QuadBrain decided to invoke tools.</summary>
    [JsonPropertyName("tool_calls")]
    public List<OpenAiToolCall>? ToolCalls { get; set; }
}

/// <summary>FR-MCP-QBOPENAI-001: An assistant tool call (OpenAI function-call shape).</summary>
public sealed class OpenAiToolCall
{
    /// <summary>Tool-call id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Call type, always <c>function</c>.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>The function invocation.</summary>
    [JsonPropertyName("function")]
    public OpenAiFunctionCall Function { get; set; } = new();
}

/// <summary>FR-MCP-QBOPENAI-001: A function invocation within a tool call.</summary>
public sealed class OpenAiFunctionCall
{
    /// <summary>Function name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>JSON-encoded arguments string.</summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "{}";
}

/// <summary>FR-MCP-QBOPENAI-001: One choice in an OpenAI-compatible chat-completion response.</summary>
public sealed class OpenAiChatChoice
{
    /// <summary>Choice index.</summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>Assistant message.</summary>
    [JsonPropertyName("message")]
    public OpenAiChatResponseMessage Message { get; set; } = new();

    /// <summary>Finish reason (<c>stop</c> or <c>tool_calls</c> in a later slice).</summary>
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = "stop";
}

/// <summary>FR-MCP-QBOPENAI-001: Token usage block (best-effort; QuadBrain does not surface token counts).</summary>
public sealed class OpenAiUsage
{
    /// <summary>Prompt tokens (0 when unknown).</summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>Completion tokens (0 when unknown).</summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    /// <summary>Total tokens (0 when unknown).</summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

/// <summary>FR-MCP-QBOPENAI-001: OpenAI-compatible chat-completion response produced from QuadBrain orchestration.</summary>
public sealed class OpenAiChatCompletionResponse
{
    /// <summary>Completion id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Object type, always <c>chat.completion</c>.</summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion";

    /// <summary>Creation time as a Unix timestamp (seconds).</summary>
    [JsonPropertyName("created")]
    public long Created { get; set; }

    /// <summary>Model id echoed from the request (or <c>quadbrain</c>).</summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = "quadbrain";

    /// <summary>Response choices.</summary>
    [JsonPropertyName("choices")]
    public List<OpenAiChatChoice> Choices { get; set; } = [];

    /// <summary>Token usage.</summary>
    [JsonPropertyName("usage")]
    public OpenAiUsage Usage { get; set; } = new();
}
