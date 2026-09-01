// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Protocol infrastructure
// TR-MCP-REPL-001: YAML Envelope Protocol - YAML serialization and framing
// TEST-MCP-REPL-001: YAML envelopes serialize/deserialize correctly

namespace McpServer.Repl.Core;

/// <summary>
/// Represents a typed YAML envelope for REPL protocol messages.
/// Each envelope wraps a single message with a discriminator field indicating its shape.
/// </summary>
public interface IYamlEnvelope
{
    /// <summary>
    /// Gets the envelope type discriminator.
    /// Valid values: "hello", "request", "event", "result", "error".
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Gets the message payload as an object.
    /// The runtime type depends on the <see cref="Type"/> discriminator:
    /// <list type="bullet">
    /// <item><term>hello</term><description>Connection handshake with protocol version and capabilities.</description></item>
    /// <item><term>request</term><description>Command invocation with method name, parameters, and request ID.</description></item>
    /// <item><term>event</term><description>Server-initiated notification with event name and payload.</description></item>
    /// <item><term>result</term><description>Successful command response with result data and matching request ID.</description></item>
    /// <item><term>error</term><description>Command failure with error code, message, and matching request ID.</description></item>
    /// </list>
    /// </summary>
    object? Payload { get; }
}

/// <summary>
/// Represents a "hello" envelope payload sent at connection establishment.
/// </summary>
public interface IHelloPayload
{
    /// <summary>
    /// Gets the REPL protocol version supported by the sender.
    /// Format: "major.minor" (e.g., "1.0").
    /// </summary>
    string ProtocolVersion { get; }

    /// <summary>
    /// Gets optional capability flags declared by the sender.
    /// Examples: "auth", "workspace-multi", "streaming".
    /// </summary>
    IReadOnlyList<string>? Capabilities { get; }

    /// <summary>
    /// Gets optional client identification metadata.
    /// </summary>
    IReadOnlyDictionary<string, string>? Metadata { get; }
}

/// <summary>
/// Represents a "request" envelope payload for command invocation.
/// </summary>
public interface IRequestPayload
{
    /// <summary>
    /// Gets the unique request identifier for correlation with responses.
    /// Must be unique within the session.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the command method name to invoke.
    /// Examples: "workspace.select", "tool.list", "context.search".
    /// </summary>
    string Method { get; }

    /// <summary>
    /// Gets the command parameters as a dictionary.
    /// Structure depends on the <see cref="Method"/>.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Params { get; }
}

/// <summary>
/// Represents an "event" envelope payload for server-initiated notifications.
/// </summary>
public interface IEventPayload
{
    /// <summary>
    /// Gets the event name.
    /// Examples: "workspace.changed", "auth.rotated", "connection.lost".
    /// </summary>
    string Event { get; }

    /// <summary>
    /// Gets the event-specific data payload.
    /// </summary>
    object? Data { get; }

    /// <summary>
    /// Gets the optional timestamp when the event occurred.
    /// </summary>
    DateTimeOffset? Timestamp { get; }
}

/// <summary>
/// Represents a "result" envelope payload for successful command responses.
/// </summary>
public interface IResultPayload
{
    /// <summary>
    /// Gets the request ID that this result corresponds to.
    /// Must match a previously sent <see cref="IRequestPayload.RequestId"/>.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the command result data.
    /// Structure depends on the command method.
    /// </summary>
    object? Result { get; }
}

/// <summary>
/// Represents an "error" envelope payload for failed command responses.
/// </summary>
public interface IErrorPayload
{
    /// <summary>
    /// Gets the request ID that this error corresponds to.
    /// Must match a previously sent <see cref="IRequestPayload.RequestId"/>.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the error code indicating the failure category.
    /// Examples: "invalid_workspace", "auth_failed", "method_not_found", "internal_error".
    /// </summary>
    string Code { get; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// FR-MCP-TRIAGEERR-001: whether the caller should retry the command.
    /// </summary>
    bool Retryable { get; }

    /// <summary>
    /// Gets optional additional error details or context.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Details { get; }
}
