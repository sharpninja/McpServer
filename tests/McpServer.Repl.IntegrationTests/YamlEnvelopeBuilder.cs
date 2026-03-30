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

    public static object CreateSessionLogBootstrapRequest(string requestId)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.sessionlog.bootstrap",
            new { });
    }

    public static object CreateSessionLogOpenSessionRequest(
        string requestId,
        string agent,
        string sessionId,
        string title,
        string model)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.sessionlog.openSession",
            new
            {
                agent,
                sessionId,
                title,
                model
            });
    }

    public static object CreateSessionLogCurrentSessionRequest(string requestId)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.sessionlog.currentSession",
            new { });
    }

    public static object CreateSessionLogBeginTurnRequest(
        string requestId,
        string turnRequestId,
        string queryTitle,
        string queryText)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.sessionlog.beginTurn",
            new
            {
                requestId = turnRequestId,
                queryTitle,
                queryText
            });
    }

    public static object CreateSessionLogUpdateTurnRequest(
        string requestId,
        string? response = null,
        string? interpretation = null,
        int? tokenCount = null,
        string[]? tags = null,
        string[]? contextList = null)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.sessionlog.updateTurn",
            new
            {
                response,
                interpretation,
                tokenCount,
                tags,
                contextList
            });
    }

    public static object CreateSessionLogCompleteTurnRequest(
        string requestId,
        string response)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.sessionlog.completeTurn",
            new { response });
    }

    public static object CreateSessionLogFailTurnRequest(
        string requestId,
        string errorMessage,
        string? errorCode = null)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.sessionlog.failTurn",
            new
            {
                errorMessage,
                errorCode
            });
    }

    public static object CreateSessionLogAppendDialogRequest(
        string requestId,
        object[] dialogItems)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.sessionlog.appendDialog",
            new { dialogItems });
    }

    public static object CreateSessionLogAppendActionsRequest(
        string requestId,
        object[] actions)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.sessionlog.appendActions",
            new { actions });
    }

    public static object CreateSessionLogQueryHistoryRequest(
        string requestId,
        string? agent = null,
        int limit = 10,
        int offset = 0)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.sessionlog.queryHistory",
            new
            {
                agent,
                limit,
                offset
            });
    }

    public static object CreateDialogItem(
        DateTimeOffset timestamp,
        string role,
        string content,
        string category)
    {
        return new
        {
            timestamp = timestamp.ToString("o"),
            role,
            content,
            category
        };
    }

    public static object CreateAction(
        int order,
        string description,
        string type,
        string status,
        string filePath)
    {
        return new
        {
            order,
            description,
            type,
            status,
            filePath
        };
    }
}
