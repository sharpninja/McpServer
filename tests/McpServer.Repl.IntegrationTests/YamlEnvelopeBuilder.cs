namespace McpServer.Repl.IntegrationTests;

/// <summary>
/// Builder class for constructing YAML envelope objects for testing.
/// </summary>
public static class YamlEnvelopeBuilder
{
    public static object CreateHelloEnvelope(
        string protocolVersion = "1.0",
        string[]? capabilities = null,
        Dictionary<string, string>? metadata = null)
    {
        return new
        {
            type = "hello",
            payload = new
            {
                protocolVersion,
                capabilities = capabilities ?? Array.Empty<string>(),
                metadata = metadata ?? new Dictionary<string, string>()
            }
        };
    }

    public static object CreateRequestEnvelope(
        string requestId,
        string method,
        object? parameters = null)
    {
        return new
        {
            type = "request",
            payload = new
            {
                requestId,
                method,
                @params = parameters
            }
        };
    }

    public static object CreateTrustBootstrapRequest(
        string requestId,
        string workspacePath,
        string? nonce = null,
        string? signature = null)
    {
        return CreateRequestEnvelope(
            requestId,
            "trust.bootstrap",
            new
            {
                workspacePath,
                nonce,
                signature
            });
    }

    public static object CreateWorkspaceSelectRequest(
        string requestId,
        string workspacePath)
    {
        return CreateRequestEnvelope(
            requestId,
            "workspace.select",
            new { workspacePath });
    }

    public static object CreateNonceRequest(
        string requestId,
        string workspacePath)
    {
        return CreateRequestEnvelope(
            requestId,
            "trust.getNonce",
            new { workspacePath });
    }

    public static object CreateResultEnvelope(
        string requestId,
        object? result = null)
    {
        return new
        {
            type = "result",
            payload = new
            {
                requestId,
                result
            }
        };
    }

    public static object CreateErrorEnvelope(
        string requestId,
        string code,
        string message,
        object? details = null)
    {
        return new
        {
            type = "error",
            payload = new
            {
                requestId,
                code,
                message,
                details
            }
        };
    }

    public static object CreateEventEnvelope(
        string eventName,
        object? data = null,
        DateTimeOffset? timestamp = null)
    {
        return new
        {
            type = "event",
            payload = new
            {
                @event = eventName,
                data,
                timestamp = timestamp ?? DateTimeOffset.UtcNow
            }
        };
    }
}
