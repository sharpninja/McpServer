// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Server-side command dispatcher
// FR-MCP-REPL-003: Command Namespace Parity - Request routing to client passthrough
// TR-MCP-REPL-004: Command Registry and Dispatcher - Envelope-to-handler routing
// TEST-MCP-REPL-001: REPL host processes well-formed YAML command envelopes

namespace McpServer.Repl.Core;

/// <summary>
/// Dispatches parsed YAML envelopes to the appropriate handler and returns the response
/// envelope. Responsible for routing <c>hello</c> handshakes and <c>request</c> envelopes
/// by method namespace (currently <c>client.*.*</c> via <see cref="IGenericClientPassthrough"/>).
/// Unknown namespaces produce a <c>method_not_found</c> error envelope so the agent loop
/// can respond and continue instead of crashing.
/// </summary>
public interface IReplCommandDispatcher
{
    /// <summary>
    /// Dispatches a parsed YAML envelope and returns the response envelope (result or error).
    /// Never throws for recoverable dispatch failures — unexpected exceptions are caught and
    /// wrapped in an error envelope so the caller's read/write loop can remain alive.
    /// </summary>
    /// <param name="envelope">The inbound envelope to dispatch. Must have a non-null payload.</param>
    /// <param name="cancellationToken">Cancellation token propagated to handlers.</param>
    /// <returns>The response envelope to emit back to the caller.</returns>
    Task<IYamlEnvelope> DispatchAsync(IYamlEnvelope envelope, CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IReplCommandDispatcher"/> implementation. Routes <c>hello</c> envelopes
/// to a handshake response and <c>request</c> envelopes with the <c>client.&lt;clientName&gt;.&lt;methodName&gt;</c>
/// method shape to <see cref="IGenericClientPassthrough.InvokeAsync"/>. All other method
/// namespaces produce a <c>method_not_found</c> error envelope.
/// </summary>
public sealed class ReplCommandDispatcher : IReplCommandDispatcher
{
    private const string ServerProtocolVersion = "1.0";
    private readonly IGenericClientPassthrough _passthrough;

    /// <summary>
    /// Initializes a new <see cref="ReplCommandDispatcher"/>.
    /// </summary>
    /// <param name="passthrough">The generic client passthrough used to invoke <c>client.*.*</c> methods.</param>
    public ReplCommandDispatcher(IGenericClientPassthrough passthrough)
    {
        _passthrough = passthrough ?? throw new ArgumentNullException(nameof(passthrough));
    }

    /// <inheritdoc />
    public async Task<IYamlEnvelope> DispatchAsync(IYamlEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        switch (envelope.Type)
        {
            case "hello":
                return BuildHelloResponse(envelope.Payload as IHelloPayload);

            case "request":
                if (envelope.Payload is not IRequestPayload request)
                {
                    return BuildError(
                        requestId: "unknown",
                        code: "invalid_envelope",
                        message: "Request envelope is missing a request payload.");
                }
                return await DispatchRequestAsync(request, cancellationToken).ConfigureAwait(false);

            default:
                return BuildError(
                    requestId: "unknown",
                    code: "invalid_envelope",
                    message: $"Unsupported envelope type: {envelope.Type}");
        }
    }

    private async Task<IYamlEnvelope> DispatchRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        var method = request.Method ?? "";

        if (method.StartsWith("client.", StringComparison.Ordinal))
        {
            return await DispatchClientRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        return BuildError(
            requestId: request.RequestId,
            code: "method_not_found",
            message: $"Method '{method}' is not routed by this dispatcher. " +
                     "Supported namespaces: client.<clientName>.<methodName>.");
    }

    private async Task<IYamlEnvelope> DispatchClientRequestAsync(IRequestPayload request, CancellationToken cancellationToken)
    {
        // method shape: client.<clientName>.<methodName>
        var parts = request.Method.Split('.', 3);
        if (parts.Length != 3 || parts[0] != "client" ||
            string.IsNullOrEmpty(parts[1]) || string.IsNullOrEmpty(parts[2]))
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_not_found",
                message: $"Method '{request.Method}' does not match the expected 'client.<clientName>.<methodName>' shape.");
        }

        var clientName = parts[1];
        var methodName = parts[2];
        var args = request.Params is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(request.Params, StringComparer.OrdinalIgnoreCase);

        try
        {
            var result = await _passthrough.InvokeAsync(clientName, methodName, args, cancellationToken).ConfigureAwait(false);
            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload
                {
                    RequestId = request.RequestId,
                    Result = result,
                },
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BuildError(
                requestId: request.RequestId,
                code: "method_invocation_error",
                message: ex.Message,
                details: new Dictionary<string, object?>
                {
                    ["clientName"] = clientName,
                    ["methodName"] = methodName,
                    ["exceptionType"] = ex.GetType().FullName,
                });
        }
    }

    private static IYamlEnvelope BuildHelloResponse(IHelloPayload? request)
    {
        var capabilities = new List<string> { "client-passthrough" };
        if (request?.Capabilities is not null)
        {
            capabilities.AddRange(request.Capabilities);
        }

        return new YamlEnvelope
        {
            Type = "hello",
            Payload = new HelloPayload
            {
                ProtocolVersion = ServerProtocolVersion,
                Capabilities = capabilities,
            },
        };
    }

    private static IYamlEnvelope BuildError(
        string requestId,
        string code,
        string message,
        IReadOnlyDictionary<string, object?>? details = null)
    {
        return new YamlEnvelope
        {
            Type = "error",
            Payload = new ErrorPayload
            {
                RequestId = requestId,
                Code = code,
                Message = message,
                Details = details,
            },
        };
    }
}
