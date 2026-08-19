// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Concrete envelope and payload POCOs
// TR-MCP-REPL-001: YAML Envelope Protocol - Production envelope data models
// TEST-MCP-REPL-001: YAML envelopes serialize/deserialize correctly

namespace McpServer.Repl.Core;

/// <summary>
/// Concrete <see cref="IYamlEnvelope"/> POCO used by the production serializer and dispatcher.
/// </summary>
public sealed class YamlEnvelope : IYamlEnvelope
{
    /// <inheritdoc />
    public string Type { get; init; } = "";

    /// <inheritdoc />
    public object? Payload { get; init; }
}

/// <summary>
/// Concrete <see cref="IHelloPayload"/> POCO for the connection handshake envelope.
/// </summary>
public sealed class HelloPayload : IHelloPayload
{
    /// <inheritdoc />
    public string ProtocolVersion { get; init; } = "1.0";

    /// <inheritdoc />
    public IReadOnlyList<string>? Capabilities { get; init; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Concrete <see cref="IRequestPayload"/> POCO for command-invocation envelopes.
/// </summary>
public sealed class RequestPayload : IRequestPayload
{
    /// <inheritdoc />
    public string RequestId { get; init; } = "";

    /// <inheritdoc />
    public string Method { get; init; } = "";

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? Params { get; init; }
}

/// <summary>
/// Concrete <see cref="IResultPayload"/> POCO for successful command responses.
/// </summary>
public sealed class ResultPayload : IResultPayload
{
    /// <inheritdoc />
    public string RequestId { get; init; } = "";

    /// <inheritdoc />
    public object? Result { get; init; }

    /// <summary>
    /// FR-MCP-REPL-006: True when the invoked method belongs to a deprecated
    /// namespace (<c>workflow.*</c>). Callers should migrate to the
    /// <c>client.&lt;Client&gt;.&lt;Method&gt;</c> passthrough surface. Omitted
    /// from the wire when null.
    /// </summary>
    public bool? Deprecated { get; set; }
}

/// <summary>
/// Concrete <see cref="IErrorPayload"/> POCO for failed command responses.
/// </summary>
public sealed class ErrorPayload : IErrorPayload
{
    /// <inheritdoc />
    public string RequestId { get; init; } = "";

    /// <inheritdoc />
    public string Code { get; init; } = "";

    /// <inheritdoc />
    public string Message { get; init; } = "";

    /// <inheritdoc />
    public bool Retryable { get; init; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? Details { get; init; }
}

/// <summary>
/// Concrete <see cref="IEventPayload"/> POCO for server-initiated events.
/// </summary>
public sealed class EventPayload : IEventPayload
{
    /// <inheritdoc />
    public string Event { get; init; } = "";

    /// <inheritdoc />
    public object? Data { get; init; }

    /// <inheritdoc />
    public DateTimeOffset? Timestamp { get; init; }
}
