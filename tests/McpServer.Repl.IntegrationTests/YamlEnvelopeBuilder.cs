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

    public static object CreateTodoQueryRequest(
        string requestId,
        string? keyword = null,
        string? priority = null,
        string? section = null,
        string? id = null,
        bool? done = null)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.query",
            new
            {
                keyword,
                priority,
                section,
                id,
                done
            });
    }

    public static object CreateTodoGetRequest(string requestId, string id)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.get",
            new { id });
    }

    public static object CreateTodoSelectRequest(string requestId, string id)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.select",
            new { id });
    }

    public static object CreateTodoCreateRequest(
        string requestId,
        string id,
        string title,
        string section,
        string priority,
        string? estimate = null,
        string[]? description = null,
        string[]? technicalDetails = null,
        object[]? implementationTasks = null,
        string? note = null,
        string? remaining = null,
        string[]? dependsOn = null,
        string[]? functionalRequirements = null,
        string[]? technicalRequirements = null)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.create",
            new
            {
                id,
                title,
                section,
                priority,
                estimate,
                description,
                technicalDetails,
                implementationTasks,
                note,
                remaining,
                dependsOn,
                functionalRequirements,
                technicalRequirements
            });
    }

    public static object CreateTodoUpdateRequest(
        string requestId,
        string id,
        string? title = null,
        string? priority = null,
        string? section = null,
        bool? done = null,
        string? estimate = null,
        string[]? description = null,
        string? remaining = null)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.update",
            new
            {
                id,
                title,
                priority,
                section,
                done,
                estimate,
                description,
                remaining
            });
    }

    public static object CreateTodoUpdateSelectedRequest(
        string requestId,
        string? title = null,
        string? priority = null,
        bool? done = null,
        string? remaining = null)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.updateSelected",
            new
            {
                title,
                priority,
                done,
                remaining
            });
    }

    public static object CreateTodoDeleteRequest(string requestId, string id)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.delete",
            new { id });
    }

    public static object CreateTodoDeleteSelectedRequest(string requestId)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.deleteSelected",
            new { });
    }

    public static object CreateTodoAnalyzeRequirementsRequest(string requestId, string id)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.analyzeRequirements",
            new { id });
    }

    public static object CreateTodoStreamStatusRequest(string requestId, string id)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.streamStatus",
            new { id });
    }

    public static object CreateTodoStreamPlanRequest(string requestId, string id)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.streamPlan",
            new { id });
    }

    public static object CreateTodoStreamImplementRequest(string requestId, string id)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.streamImplement",
            new { id });
    }

    public static object CreateTodoGetProjectionStatusRequest(string requestId, string id)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.getProjectionStatus",
            new { id });
    }

    public static object CreateTodoRepairProjectionRequest(string requestId, string id)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.repairProjection",
            new { id });
    }

    public static object CreateTodoCurrentSelectionRequest(string requestId)
    {
        return CreateRequestEnvelope(
            requestId,
            "workflow.todo.currentSelection",
            new { });
    }

    public static object CreateCancelCommandRequest(string requestId)
    {
        return CreateRequestEnvelope(
            requestId,
            "cancel",
            new { });
    }

    public static object CreateTodoSubtask(string task, bool done)
    {
        return new
        {
            task,
            done
        };
    }
}
