using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-QBEXEC-002 / TR-MCP-QBEXEC-002: Concrete <see cref="IQuadBrainInternalToolExecutor"/> that executes
/// MCP-internal tools server-side through existing application services (the same CQRS-backed services
/// controllers dispatch), never through HTTP endpoints. Mutations go through the transaction-gated TODO,
/// repo, and requirements services. Reads go through <see cref="ITodoService"/> and <see cref="IRepoFileService"/>.
/// Unknown mcp_ tools return <see cref="InternalToolExecutionOutcome.Unhandled"/>.
/// </summary>
public sealed class QuadBrainInternalToolExecutor : IQuadBrainInternalToolExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ITransactionGatedTodoMutationService _todo;
    private readonly ITodoService _todoQueries;
    private readonly ITodoPromptService _todoPrompts;
    private readonly IRepoFileService _repo;
    private readonly IRequirementsDocumentService _requirements;
    private readonly ISessionLogService _sessionLog;
    private readonly IGraphRagService _graphRag;
    private readonly IDesktopLaunchService _desktop;
    private readonly IProcessRunner _processRunner;
    private readonly WorkspaceContext _workspace;
    private readonly IQuadBrainPowerShellSessions _powerShell;

    /// <summary>Initializes a new instance of the <see cref="QuadBrainInternalToolExecutor"/> class.</summary>
    public QuadBrainInternalToolExecutor(
        ITransactionGatedTodoMutationService todo,
        ITodoService todoQueries,
        ITodoPromptService todoPrompts,
        IRepoFileService repo,
        IRequirementsDocumentService requirements,
        ISessionLogService sessionLog,
        IGraphRagService graphRag,
        IDesktopLaunchService desktop,
        IProcessRunner processRunner,
        WorkspaceContext workspace,
        IQuadBrainPowerShellSessions? powerShell = null)
    {
        _todo = todo ?? throw new ArgumentNullException(nameof(todo));
        _todoQueries = todoQueries ?? throw new ArgumentNullException(nameof(todoQueries));
        _todoPrompts = todoPrompts ?? throw new ArgumentNullException(nameof(todoPrompts));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _requirements = requirements ?? throw new ArgumentNullException(nameof(requirements));
        _sessionLog = sessionLog ?? throw new ArgumentNullException(nameof(sessionLog));
        _graphRag = graphRag ?? throw new ArgumentNullException(nameof(graphRag));
        _desktop = desktop ?? throw new ArgumentNullException(nameof(desktop));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _powerShell = powerShell ?? new InMemoryQuadBrainPowerShellSessions(_processRunner);
    }

    /// <inheritdoc />
    public async Task<InternalToolExecutionOutcome> TryExecuteAsync(
        OpenAiToolCall toolCall,
        string? turnId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        _ = turnId;
        var name = toolCall.Function.Name?.Trim() ?? string.Empty;
        var arguments = string.IsNullOrWhiteSpace(toolCall.Function.Arguments) ? "{}" : toolCall.Function.Arguments;

        try
        {
            return name switch
            {
                "mcp_todo_query" => await QueryTodosAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_todo_get" => await GetTodoAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_todo_create" => await CreateTodoAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_todo_update" => await UpdateTodoAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_todo_delete" => await DeleteTodoAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_todo_plan" => await StreamTodoPromptAsync(arguments, "plan", cancellationToken).ConfigureAwait(false),
                "mcp_todo_status" => await StreamTodoPromptAsync(arguments, "status", cancellationToken).ConfigureAwait(false),
                "mcp_todo_implementation" => await StreamTodoPromptAsync(arguments, "implementation", cancellationToken).ConfigureAwait(false),
                "mcp_repo_read" => await ReadRepoAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_repo_list" => await ListRepoAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_repo_write" => await WriteRepoAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_repo_edit" => await EditRepoAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_list_fr" => await ListFrAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_list_tr" => await ListTrAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_list_test" => await ListTestAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_get_fr" => await GetFrAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_get_tr" => await GetTrAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_get_test" => await GetTestAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_create_fr" => await CreateFrAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_update_fr" => await UpdateFrAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_create_tr" => await CreateTrAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_update_tr" => await UpdateTrAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_create_test" => await CreateTestAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_update_test" => await UpdateTestAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_session_bootstrap" => await SessionBootstrapAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_session_update" => await SessionUpdateAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_session_turn_begin" => await SessionTurnBeginAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_session_turn_update" => await SessionTurnUpdateAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_session_turn_complete" => await SessionTurnCompleteAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_session_query_history" => await SessionQueryHistoryAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_desktop_launch" => await DesktopLaunchAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_powershell_session_create" => PowerShellCreate(arguments),
                "mcp_powershell_session_command" => await PowerShellCommandAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_powershell_session_close" => PowerShellClose(arguments),
                "mcp_graphrag_ingest_text" => await GraphIngestAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_list_documents" => await GraphListDocumentsAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_get_document_chunks" => await GraphGetChunksAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_delete_document" => await GraphDeleteDocumentAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_create_entity" => await GraphCreateEntityAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_list_entities" => await GraphListEntitiesAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_get_entity" => await GraphGetEntityAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_update_entity" => await GraphUpdateEntityAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_delete_entity" => await GraphDeleteEntityAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_create_relationship" => await GraphCreateRelationshipAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_list_relationships" => await GraphListRelationshipsAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_get_relationship" => await GraphGetRelationshipAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_update_relationship" => await GraphUpdateRelationshipAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_graphrag_delete_relationship" => await GraphDeleteRelationshipAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_client_invoke" => await ClientInvokeAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_git" => await GitAsync(arguments, cancellationToken).ConfigureAwait(false),
                _ => InternalToolExecutionOutcome.Unhandled,
            };
        }
        catch (JsonException ex)
        {
            return InternalToolExecutionOutcome.Fail($"invalid arguments for '{name}': {ex.Message}");
        }
    }

    private async Task<InternalToolExecutionOutcome> QueryTodosAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var request = new TodoQueryRequest
        {
            Keyword = GetString(root, "keyword"),
            Priority = GetString(root, "priority"),
            Section = GetString(root, "section"),
            Id = GetString(root, "id"),
            Done = GetBool(root, "done"),
        };
        var result = await _todoQueries.QueryAsync(request, cancellationToken).ConfigureAwait(false);
        return InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(result, JsonOptions));
    }

    private async Task<InternalToolExecutionOutcome> GetTodoAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_todo_get requires an 'id'.");

        var item = await _todoQueries.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return item is null
            ? InternalToolExecutionOutcome.Fail($"Item with id '{id}' not found.")
            : InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(item, JsonOptions));
    }

    private async Task<InternalToolExecutionOutcome> ReadRepoAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var path = GetString(document.RootElement, "path") ?? GetString(document.RootElement, "relativePath");
        if (string.IsNullOrWhiteSpace(path))
            return InternalToolExecutionOutcome.Fail("mcp_repo_read requires a 'path'.");

        var result = await _repo.ReadAsync(path, cancellationToken).ConfigureAwait(false);
        return result is null
            ? InternalToolExecutionOutcome.Fail($"File '{path}' was not found or is not allowed.")
            : InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(result, JsonOptions));
    }

    private async Task<InternalToolExecutionOutcome> ListRepoAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var path = GetString(document.RootElement, "path") ?? GetString(document.RootElement, "relativePath");
        var result = await _repo.ListAsync(path, cancellationToken).ConfigureAwait(false);
        return InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(result, JsonOptions));
    }

    private async Task<InternalToolExecutionOutcome> StreamTodoPromptAsync(string arguments, string kind, CancellationToken cancellationToken)
    {
        var id = ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail($"mcp_todo_{kind} requires an 'id'.");

        using var document = JsonDocument.Parse(arguments);
        var extra = GetString(document.RootElement, "additionalPrompt");
        var stream = kind switch
        {
            "plan" => _todoPrompts.StreamPlanAsync(id, extra, cancellationToken),
            "status" => _todoPrompts.StreamStatusAsync(id, cancellationToken),
            _ => _todoPrompts.StreamImplementAsync(id, cancellationToken),
        };
        var text = await CollectLinesAsync(stream).ConfigureAwait(false);
        return InternalToolExecutionOutcome.Ok(text);
    }

    private async Task<InternalToolExecutionOutcome> ListFrAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var items = await _requirements.QueryFrAsync(GetString(document.RootElement, "area"), GetString(document.RootElement, "status"), cancellationToken).ConfigureAwait(false);
        return OkJson(items);
    }

    private async Task<InternalToolExecutionOutcome> ListTrAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var items = await _requirements.QueryTrAsync(GetString(root, "area"), GetString(root, "subarea"), GetString(root, "status"), cancellationToken).ConfigureAwait(false);
        return OkJson(items);
    }

    private async Task<InternalToolExecutionOutcome> ListTestAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var items = await _requirements.QueryTestAsync(GetString(document.RootElement, "area"), GetString(document.RootElement, "status"), cancellationToken).ConfigureAwait(false);
        return OkJson(items);
    }

    private async Task<InternalToolExecutionOutcome> GetFrAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_requirements_get_fr requires an 'id'.");
        var item = await _requirements.GetFrAsync(id, cancellationToken).ConfigureAwait(false);
        return item is null ? InternalToolExecutionOutcome.Fail($"FR '{id}' was not found.") : OkJson(item);
    }

    private async Task<InternalToolExecutionOutcome> GetTrAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_requirements_get_tr requires an 'id'.");
        var item = await _requirements.GetTrAsync(id, cancellationToken).ConfigureAwait(false);
        return item is null ? InternalToolExecutionOutcome.Fail($"TR '{id}' was not found.") : OkJson(item);
    }

    private async Task<InternalToolExecutionOutcome> GetTestAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_requirements_get_test requires an 'id'.");
        var item = await _requirements.GetTestAsync(id, cancellationToken).ConfigureAwait(false);
        return item is null ? InternalToolExecutionOutcome.Fail($"TEST '{id}' was not found.") : OkJson(item);
    }

    private async Task<InternalToolExecutionOutcome> SessionBootstrapAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var agent = GetString(root, "agent") ?? GetString(root, "sourceType");
        var sessionId = GetString(root, "sessionId");
        if (string.IsNullOrWhiteSpace(agent) || string.IsNullOrWhiteSpace(sessionId))
            return InternalToolExecutionOutcome.Fail("mcp_session_bootstrap requires agent and sessionId.");
        var created = await _sessionLog.OpenSessionAsync(agent, sessionId, GetString(root, "title"), GetString(root, "model"), cancellationToken).ConfigureAwait(false);
        return OkJson(new { success = true, agent, sessionId, created });
    }

    private async Task<InternalToolExecutionOutcome> SessionUpdateAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var agent = GetString(root, "agent") ?? GetString(root, "sourceType");
        var sessionId = GetString(root, "sessionId");
        var title = GetString(root, "title");
        if (string.IsNullOrWhiteSpace(agent) || string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(title))
            return InternalToolExecutionOutcome.Fail("mcp_session_update requires agent, sessionId, and title.");
        var id = await _sessionLog.SetSessionTitleAsync(agent, sessionId, title, cancellationToken).ConfigureAwait(false);
        return OkJson(new { success = true, id, agent, sessionId, title });
    }

    private async Task<InternalToolExecutionOutcome> SessionTurnBeginAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var agent = GetString(root, "agent") ?? GetString(root, "sourceType");
        var sessionId = GetString(root, "sessionId");
        var requestId = GetString(root, "requestId");
        if (string.IsNullOrWhiteSpace(agent) || string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(requestId))
            return InternalToolExecutionOutcome.Fail("mcp_session_turn_begin requires agent, sessionId, and requestId.");
        var turn = new UnifiedRequestEntryDto
        {
            RequestId = requestId,
            Status = "in_progress",
            QueryTitle = GetString(root, "queryTitle"),
            QueryText = GetString(root, "queryText"),
            PlanFile = GetString(root, "planFile"),
            TodoId = GetString(root, "todoId"),
        };
        var turnId = await _sessionLog.UpsertTurnAsync(agent, sessionId, turn, cancellationToken).ConfigureAwait(false);
        return OkJson(new { success = true, turnId, agent, sessionId, requestId, status = "in_progress" });
    }

    private async Task<InternalToolExecutionOutcome> SessionTurnUpdateAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var agent = GetString(root, "agent") ?? GetString(root, "sourceType");
        var sessionId = GetString(root, "sessionId");
        var requestId = GetString(root, "requestId");
        if (string.IsNullOrWhiteSpace(agent) || string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(requestId))
            return InternalToolExecutionOutcome.Fail("mcp_session_turn_update requires agent, sessionId, and requestId.");
        var turn = JsonSerializer.Deserialize<UnifiedRequestEntryDto>(arguments, JsonOptions) ?? new UnifiedRequestEntryDto();
        turn.RequestId = requestId;
        var turnId = await _sessionLog.UpsertTurnAsync(agent, sessionId, turn, cancellationToken).ConfigureAwait(false);
        return OkJson(new { success = true, turnId, agent, sessionId, requestId });
    }

    private async Task<InternalToolExecutionOutcome> SessionTurnCompleteAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var agent = GetString(root, "agent") ?? GetString(root, "sourceType");
        var sessionId = GetString(root, "sessionId");
        var requestId = GetString(root, "requestId");
        if (string.IsNullOrWhiteSpace(agent) || string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(requestId))
            return InternalToolExecutionOutcome.Fail("mcp_session_turn_complete requires agent, sessionId, and requestId.");
        var turn = JsonSerializer.Deserialize<UnifiedRequestEntryDto>(arguments, JsonOptions) ?? new UnifiedRequestEntryDto();
        turn.RequestId = requestId;
        turn.Status = "completed";
        var turnId = await _sessionLog.UpsertTurnAsync(agent, sessionId, turn, cancellationToken).ConfigureAwait(false);
        return OkJson(new { success = true, turnId, agent, sessionId, requestId, status = "completed" });
    }

    private async Task<InternalToolExecutionOutcome> SessionQueryHistoryAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var request = new SessionLogQueryRequest
        {
            Agent = GetString(root, "agent") ?? GetString(root, "sourceType"),
            Limit = GetInt(root, "limit") ?? 5,
            Offset = GetInt(root, "offset") ?? 0,
        };
        var result = await _sessionLog.QueryAsync(request, cancellationToken).ConfigureAwait(false);
        return OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> DesktopLaunchAsync(string arguments, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<DesktopLaunchRequest>(arguments, JsonOptions) ?? new DesktopLaunchRequest();
        var workspace = _workspace.WorkspacePath ?? GetString(JsonDocument.Parse(arguments).RootElement, "workspacePath");
        if (string.IsNullOrWhiteSpace(workspace))
            return InternalToolExecutionOutcome.Fail("mcp_desktop_launch requires a workspace path.");
        var result = await _desktop.LaunchAsync(workspace, request, cancellationToken).ConfigureAwait(false);
        return OkJson(result);
    }

    private InternalToolExecutionOutcome PowerShellCreate(string arguments)
    {
        using var document = JsonDocument.Parse(arguments);
        var cwd = GetString(document.RootElement, "workingDirectory") ?? _workspace.WorkspacePath ?? Environment.CurrentDirectory;
        var id = _powerShell.Create(cwd);
        return OkJson(new { sessionId = id, workingDirectory = cwd });
    }

    private async Task<InternalToolExecutionOutcome> PowerShellCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var sessionId = GetString(document.RootElement, "sessionId");
        var command = GetString(document.RootElement, "command");
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(command))
            return InternalToolExecutionOutcome.Fail("mcp_powershell_session_command requires sessionId and command.");
        try
        {
            var result = await _powerShell.ExecuteAsync(sessionId, command, cancellationToken).ConfigureAwait(false);
            return OkJson(result);
        }
        catch (InvalidOperationException ex)
        {
            return InternalToolExecutionOutcome.Fail(ex.Message);
        }
    }

    private InternalToolExecutionOutcome PowerShellClose(string arguments)
    {
        using var document = JsonDocument.Parse(arguments);
        var sessionId = GetString(document.RootElement, "sessionId");
        if (string.IsNullOrWhiteSpace(sessionId))
            return InternalToolExecutionOutcome.Fail("mcp_powershell_session_close requires sessionId.");
        _powerShell.Close(sessionId);
        return OkJson(new { success = true, sessionId });
    }

    private async Task<InternalToolExecutionOutcome> GraphIngestAsync(string arguments, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<GraphRagIngestTextRequest>(arguments, JsonOptions);
        if (request is null || string.IsNullOrWhiteSpace(request.Content))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_ingest_text requires content.");
        var result = await _graphRag.IngestTextAsync(request, cancellationToken).ConfigureAwait(false);
        return OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> GraphListDocumentsAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var result = await _graphRag.ListDocumentsAsync(GetInt(document.RootElement, "skip") ?? 0, GetInt(document.RootElement, "take") ?? 50, GetString(document.RootElement, "sourceType"), cancellationToken).ConfigureAwait(false);
        return OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> GraphGetChunksAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = GetString(JsonDocument.Parse(arguments).RootElement, "documentId") ?? ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_get_document_chunks requires documentId.");
        var result = await _graphRag.GetDocumentChunksAsync(id, cancellationToken).ConfigureAwait(false);
        return result is null ? InternalToolExecutionOutcome.Fail($"Document '{id}' was not found.") : OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> GraphDeleteDocumentAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = GetString(JsonDocument.Parse(arguments).RootElement, "documentId") ?? ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_delete_document requires documentId.");
        var result = await _graphRag.DeleteDocumentAsync(id, cancellationToken).ConfigureAwait(false);
        return OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> GraphCreateEntityAsync(string arguments, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<GraphEntityRequest>(arguments, JsonOptions);
        if (request is null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.EntityType))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_create_entity requires name and entityType.");
        var result = await _graphRag.CreateEntityAsync(request, cancellationToken).ConfigureAwait(false);
        return OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> GraphListEntitiesAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var result = await _graphRag.ListEntitiesAsync(GetInt(document.RootElement, "skip") ?? 0, GetInt(document.RootElement, "take") ?? 50, GetString(document.RootElement, "entityType"), cancellationToken).ConfigureAwait(false);
        return OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> GraphGetEntityAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = GetString(JsonDocument.Parse(arguments).RootElement, "entityId") ?? ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_get_entity requires entityId.");
        var result = await _graphRag.GetEntityAsync(id, cancellationToken).ConfigureAwait(false);
        return result is null ? InternalToolExecutionOutcome.Fail($"Entity '{id}' was not found.") : OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> GraphUpdateEntityAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var id = GetString(document.RootElement, "entityId") ?? ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_update_entity requires entityId.");
        var request = JsonSerializer.Deserialize<GraphEntityRequest>(arguments, JsonOptions);
        if (request is null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.EntityType))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_update_entity requires name and entityType.");
        var result = await _graphRag.UpdateEntityAsync(id, request, cancellationToken).ConfigureAwait(false);
        return result is null ? InternalToolExecutionOutcome.Fail($"Entity '{id}' was not found.") : OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> GraphDeleteEntityAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = GetString(JsonDocument.Parse(arguments).RootElement, "entityId") ?? ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_delete_entity requires entityId.");
        var deleted = await _graphRag.DeleteEntityAsync(id, cancellationToken).ConfigureAwait(false);
        return OkJson(new { deleted, entityId = id });
    }

    private async Task<InternalToolExecutionOutcome> GraphCreateRelationshipAsync(string arguments, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<GraphRelationshipRequest>(arguments, JsonOptions);
        if (request is null
            || string.IsNullOrWhiteSpace(request.SourceEntityId)
            || string.IsNullOrWhiteSpace(request.TargetEntityId)
            || string.IsNullOrWhiteSpace(request.RelationshipType))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_create_relationship requires sourceEntityId, targetEntityId, and relationshipType.");
        var result = await _graphRag.CreateRelationshipAsync(request, cancellationToken).ConfigureAwait(false);
        return OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> GraphListRelationshipsAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var result = await _graphRag.ListRelationshipsAsync(GetInt(document.RootElement, "skip") ?? 0, GetInt(document.RootElement, "take") ?? 50, GetString(document.RootElement, "entityId"), GetString(document.RootElement, "type") ?? GetString(document.RootElement, "relationshipType"), cancellationToken).ConfigureAwait(false);
        return OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> GraphGetRelationshipAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = GetString(JsonDocument.Parse(arguments).RootElement, "relationshipId") ?? ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_get_relationship requires relationshipId.");
        var result = await _graphRag.GetRelationshipAsync(id, cancellationToken).ConfigureAwait(false);
        return result is null ? InternalToolExecutionOutcome.Fail($"Relationship '{id}' was not found.") : OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> GraphUpdateRelationshipAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var id = GetString(document.RootElement, "relationshipId") ?? ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_update_relationship requires relationshipId.");
        var request = JsonSerializer.Deserialize<GraphRelationshipRequest>(arguments, JsonOptions);
        if (request is null
            || string.IsNullOrWhiteSpace(request.SourceEntityId)
            || string.IsNullOrWhiteSpace(request.TargetEntityId)
            || string.IsNullOrWhiteSpace(request.RelationshipType))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_update_relationship requires sourceEntityId, targetEntityId, and relationshipType.");
        var result = await _graphRag.UpdateRelationshipAsync(id, request, cancellationToken).ConfigureAwait(false);
        return result is null ? InternalToolExecutionOutcome.Fail($"Relationship '{id}' was not found.") : OkJson(result);
    }

    private async Task<InternalToolExecutionOutcome> GraphDeleteRelationshipAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = GetString(JsonDocument.Parse(arguments).RootElement, "relationshipId") ?? ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_graphrag_delete_relationship requires relationshipId.");
        var deleted = await _graphRag.DeleteRelationshipAsync(id, cancellationToken).ConfigureAwait(false);
        return OkJson(new { deleted, relationshipId = id });
    }

    private async Task<InternalToolExecutionOutcome> ClientInvokeAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var clientName = GetString(document.RootElement, "clientName");
        var methodName = GetString(document.RootElement, "methodName");
        if (string.IsNullOrWhiteSpace(clientName) || string.IsNullOrWhiteSpace(methodName))
            return InternalToolExecutionOutcome.Fail("mcp_client_invoke requires clientName and methodName.");

        var key = clientName.Trim() + "." + methodName.Trim();
        return key.ToLowerInvariant() switch
        {
            "todo.queryasync" or "todo.query" => await QueryTodosAsync(arguments, cancellationToken).ConfigureAwait(false),
            "todo.getasync" or "todo.get" => await GetTodoAsync(arguments, cancellationToken).ConfigureAwait(false),
            "todo.createasync" or "todo.create" => await CreateTodoAsync(arguments, cancellationToken).ConfigureAwait(false),
            "todo.updateasync" or "todo.update" => await UpdateTodoAsync(arguments, cancellationToken).ConfigureAwait(false),
            "repo.readasync" or "repo.read" => await ReadRepoAsync(arguments, cancellationToken).ConfigureAwait(false),
            "repo.listasync" or "repo.list" => await ListRepoAsync(arguments, cancellationToken).ConfigureAwait(false),
            "repo.writeasync" or "repo.write" => await WriteRepoAsync(arguments, cancellationToken).ConfigureAwait(false),
            "repo.editasync" or "repo.edit" => await EditRepoAsync(arguments, cancellationToken).ConfigureAwait(false),
            "session.queryasync" or "session.query" or "sessionlog.queryasync" or "sessionlog.query"
                => await SessionQueryHistoryAsync(arguments, cancellationToken).ConfigureAwait(false),
            "session.openasync" or "session.open" or "session.bootstrap"
                => await SessionBootstrapAsync(arguments, cancellationToken).ConfigureAwait(false),
            "requirements.listfrasync" or "requirements.list_fr" or "requirements.queryfrasync"
                => await ListFrAsync(arguments, cancellationToken).ConfigureAwait(false),
            "desktop.launchasync" or "desktop.launch" => await DesktopLaunchAsync(arguments, cancellationToken).ConfigureAwait(false),
            "git.runasync" or "git.run" or "git.status" => await GitAsync(arguments, cancellationToken).ConfigureAwait(false),
            _ => InternalToolExecutionOutcome.Fail($"mcp_client_invoke cannot dispatch '{clientName}.{methodName}' internally; use a named mcp_* tool."),
        };
    }

    private async Task<InternalToolExecutionOutcome> GitAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var command = GetString(document.RootElement, "command") ?? "status";
        var cwd = _workspace.WorkspacePath;
        if (string.IsNullOrWhiteSpace(cwd))
            return InternalToolExecutionOutcome.Fail("mcp_git requires a resolved workspace path.");
        if (string.Equals(command, "push", StringComparison.OrdinalIgnoreCase))
        {
            var result = await _processRunner.RunAsync(new ProcessRunRequest("git", "push origin", WorkingDirectory: cwd), cancellationToken).ConfigureAwait(false);
            return result.ExitCode == 0 ? OkJson(result) : InternalToolExecutionOutcome.Fail(result.Stderr ?? "git push failed");
        }

        var run = await _processRunner.RunAsync(new ProcessRunRequest("git", command, WorkingDirectory: cwd), cancellationToken).ConfigureAwait(false);
        return OkJson(run);
    }

    private async Task<InternalToolExecutionOutcome> CreateTodoAsync(string arguments, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<TodoCreateRequest>(arguments, JsonOptions);
        if (request is null)
            return InternalToolExecutionOutcome.Fail("mcp_todo_create requires id, title, section, and priority.");

        var result = await _todo.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return ToOutcome(result.Success, result.Error, result);
    }

    private async Task<InternalToolExecutionOutcome> UpdateTodoAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_todo_update requires an 'id'.");

        var request = JsonSerializer.Deserialize<TodoUpdateRequest>(arguments, JsonOptions) ?? new TodoUpdateRequest();
        var result = await _todo.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ToOutcome(result.Success, result.Error, result);
    }

    private async Task<InternalToolExecutionOutcome> DeleteTodoAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_todo_delete requires an 'id'.");

        var result = await _todo.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return ToOutcome(result.Success, result.Error, result);
    }

    private async Task<InternalToolExecutionOutcome> WriteRepoAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var path = GetString(root, "path");
        if (string.IsNullOrWhiteSpace(path))
            return InternalToolExecutionOutcome.Fail("mcp_repo_write requires a 'path'.");

        var content = GetString(root, "content") ?? string.Empty;
        var result = await _repo.WriteAsync(path, content, cancellationToken).ConfigureAwait(false);
        return result.Written
            ? InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(new { path, written = true }, JsonOptions))
            : InternalToolExecutionOutcome.Fail(result.Error ?? "repo write failed");
    }

    private async Task<InternalToolExecutionOutcome> EditRepoAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var path = GetString(root, "path");
        var oldString = GetString(root, "oldString");
        var newString = GetString(root, "newString");
        if (string.IsNullOrWhiteSpace(path) || oldString is null || newString is null)
            return InternalToolExecutionOutcome.Fail("mcp_repo_edit requires 'path', 'oldString', and 'newString'.");

        var replaceAll = root.TryGetProperty("replaceAll", out var ra) && ra.ValueKind == JsonValueKind.True;
        int? expected = root.TryGetProperty("expectedOccurrences", out var eo) && eo.ValueKind == JsonValueKind.Number
            ? eo.GetInt32()
            : null;

        var result = await _repo.EditAsync(path, oldString, newString, replaceAll, expected, cancellationToken).ConfigureAwait(false);
        return result.Written
            ? InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(new { path, written = true, replacements = result.Replacements }, JsonOptions))
            : InternalToolExecutionOutcome.Fail(result.Error ?? "repo edit failed");
    }

    // FR-MCP-QBEXEC-001 (AC-4) / FR-MCP-QBEXEC-002: requirements list/get/create/update all route through
    // IRequirementsDocumentService. Mutations stay transaction-gated; list/get are in-process reads.

    private Task<InternalToolExecutionOutcome> CreateFrAsync(string arguments, CancellationToken cancellationToken)
        => MutateRequirementAsync(arguments, "create_fr", (entry, ct) => _requirements.AddFrAsync(ReadFrEntry(entry), ct), cancellationToken);

    private Task<InternalToolExecutionOutcome> UpdateFrAsync(string arguments, CancellationToken cancellationToken)
        => MutateRequirementAsync(arguments, "update_fr", (entry, ct) => _requirements.UpdateFrAsync(ReadFrEntry(entry), ct), cancellationToken);

    private Task<InternalToolExecutionOutcome> CreateTrAsync(string arguments, CancellationToken cancellationToken)
        => MutateRequirementAsync(arguments, "create_tr", (entry, ct) => _requirements.AddTrAsync(ReadTrEntry(entry), ct), cancellationToken);

    private Task<InternalToolExecutionOutcome> UpdateTrAsync(string arguments, CancellationToken cancellationToken)
        => MutateRequirementAsync(arguments, "update_tr", (entry, ct) => _requirements.UpdateTrAsync(ReadTrEntry(entry), ct), cancellationToken);

    private Task<InternalToolExecutionOutcome> CreateTestAsync(string arguments, CancellationToken cancellationToken)
        => MutateRequirementAsync(arguments, "create_test", (entry, ct) => _requirements.AddTestAsync(ReadTestEntry(entry), ct), cancellationToken);

    private Task<InternalToolExecutionOutcome> UpdateTestAsync(string arguments, CancellationToken cancellationToken)
        => MutateRequirementAsync(arguments, "update_test", (entry, ct) => _requirements.UpdateTestAsync(ReadTestEntry(entry), ct), cancellationToken);

    private static async Task<InternalToolExecutionOutcome> MutateRequirementAsync(
        string arguments,
        string operation,
        Func<JsonElement, CancellationToken, Task> mutation,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var id = GetString(root, "id");
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail($"mcp_requirements_{operation} requires an 'id'.");

        try
        {
            await mutation(root, cancellationToken).ConfigureAwait(false);
            return InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(new { id, operation, applied = true }, JsonOptions));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return InternalToolExecutionOutcome.Fail($"mcp_requirements_{operation} failed: {ex.Message}");
        }
    }

    private static FrEntry ReadFrEntry(JsonElement root)
        => new(
            Id: GetString(root, "id") ?? string.Empty,
            Title: GetString(root, "title") ?? string.Empty,
            Body: GetString(root, "body") ?? string.Empty,
            Priority: GetString(root, "priority") ?? "medium",
            Status: GetString(root, "status") ?? "pending",
            Notes: GetString(root, "notes"));

    private static TrEntry ReadTrEntry(JsonElement root)
        => new(
            Id: GetString(root, "id") ?? string.Empty,
            Title: GetString(root, "title") ?? string.Empty,
            Body: GetString(root, "body") ?? string.Empty,
            Priority: GetString(root, "priority") ?? "medium",
            Status: GetString(root, "status") ?? "pending",
            Notes: GetString(root, "notes"));

    private static TestEntry ReadTestEntry(JsonElement root)
        => new(
            Id: GetString(root, "id") ?? string.Empty,
            Condition: GetString(root, "condition") ?? string.Empty,
            Title: GetString(root, "title") ?? string.Empty,
            Priority: GetString(root, "priority") ?? "medium",
            Status: GetString(root, "status") ?? "pending",
            Notes: GetString(root, "notes"));

    private static InternalToolExecutionOutcome ToOutcome(bool success, string? error, object result)
        => success
            ? InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(result, JsonOptions))
            : InternalToolExecutionOutcome.Fail(error ?? "mutation failed");

    private static string? ReadId(string arguments)
    {
        using var document = JsonDocument.Parse(arguments);
        return GetString(document.RootElement, "id");
    }

    private static string? GetString(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object
           && element.TryGetProperty(property, out var value)
           && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool? GetBool(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
            return null;
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static int? GetInt(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
            return null;
        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n) ? n : null;
    }

    private static InternalToolExecutionOutcome OkJson<T>(T value)
        => InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(value, JsonOptions));

    private static async Task<string> CollectLinesAsync(IAsyncEnumerable<string> lines)
    {
        var builder = new StringBuilder();
        await foreach (var line in lines.ConfigureAwait(false))
        {
            if (builder.Length > 0)
                builder.AppendLine();
            builder.Append(line);
        }

        return builder.ToString();
    }
}
