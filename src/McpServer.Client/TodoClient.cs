using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for TODO management endpoints (<c>/mcpserver/todo</c>). Provides full CRUD operations
/// on TODO items and a Copilot-powered requirements analysis endpoint.
///
/// <para>All methods read <see cref="McpClientBase.ApiKey"/> and <see cref="McpClientBase.Port"/>
/// at call time, allowing runtime re-targeting without recreating the client.</para>
/// </summary>
/// <seealso cref="McpServerClient.Todo"/>
public sealed class TodoClient : McpClientBase
{
    /// <inheritdoc />
    public TodoClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal TodoClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Query TODO items with optional filters.</summary>
    public async Task<TodoQueryResult> QueryAsync(
        string? keyword = null, string? priority = null, string? section = null,
        string? id = null, bool? done = null, CancellationToken cancellationToken = default)
    {
        var qs = BuildQueryString(keyword, priority, section, id, done);
        return await GetAsync<TodoQueryResult>($"mcpserver/todo{qs}", cancellationToken);
    }

    /// <summary>Get a single TODO item by ID.</summary>
    public async Task<TodoFlatItem> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TodoFlatItem>($"mcpserver/todo/{Encode(id)}", cancellationToken);
    }

    /// <summary>Get append-only audit history for a TODO item.</summary>
    public async Task<TodoAuditQueryResult> GetAuditAsync(
        string id,
        int? limit = null,
        int? offset = null,
        CancellationToken cancellationToken = default)
    {
        var parts = new List<string>();
        if (limit.HasValue) parts.Add($"limit={limit.Value}");
        if (offset.HasValue) parts.Add($"offset={offset.Value}");
        var suffix = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
        return await GetAsync<TodoAuditQueryResult>($"mcpserver/todo/{Encode(id)}/audit{suffix}", cancellationToken);
    }

    /// <summary>Get projection status for database-authoritative TODO storage.</summary>
    public async Task<TodoProjectionStatusResult> GetProjectionStatusAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<TodoProjectionStatusResult>("mcpserver/todo/projection/status", cancellationToken);
    }

    /// <summary>Repair TODO.yaml projection from database-authoritative TODO storage.</summary>
    public async Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default)
    {
        return await PostAsync<TodoProjectionRepairResult>("mcpserver/todo/projection/repair", null, cancellationToken);
    }

    /// <summary>Create a new TODO item.</summary>
    public async Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TodoMutationResult>("mcpserver/todo", request, cancellationToken);
    }

    /// <summary>Update an existing TODO item.</summary>
    public async Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<TodoMutationResult>($"mcpserver/todo/{Encode(id)}", request, cancellationToken);
    }

    /// <summary>Delete a TODO item.</summary>
    public async Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<TodoMutationResult>($"mcpserver/todo/{Encode(id)}", cancellationToken);
    }

    /// <summary>Move a TODO item to another registered workspace.</summary>
    public async Task<TodoMutationResult> MoveAsync(string id, TodoMoveRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TodoMutationResult>($"mcpserver/todo/{Encode(id)}/move", request, cancellationToken);
    }

    /// <summary>Analyze requirements for a TODO item via Copilot.</summary>
    public async Task<RequirementsAnalysisResult> AnalyzeRequirementsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementsAnalysisResult>($"mcpserver/todo/{Encode(id)}/requirements", null, cancellationToken);
    }

    /// <summary>Create a bounded Byrd iteration phase.</summary>
    public async Task<CreateIterationPhaseResult> CreateIterationPhaseAsync(
        CreateIterationPhaseRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreateIterationPhaseResult>("mcpserver/todo-execution/phases", request, cancellationToken);
    }

    /// <summary>Create execution TODOs from an approved plan.</summary>
    public async Task<CreateTodosFromPlanResult> CreateTodosFromPlanAsync(
        string phaseId,
        CreateTodosFromPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreateTodosFromPlanResult>($"mcpserver/todo-execution/phases/{Encode(phaseId)}/todos", request, cancellationToken);
    }

    /// <summary>Return the active Byrd execution TODO.</summary>
    public async Task<ActiveTodoResult> GetActiveTodoAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<ActiveTodoResult>("mcpserver/todo-execution/active", cancellationToken);
    }

    /// <summary>Return the next ready Byrd execution TODO.</summary>
    public async Task<ActiveTodoResult> GetNextReadyTodoAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<ActiveTodoResult>("mcpserver/todo-execution/next-ready", cancellationToken);
    }

    /// <summary>Hydrate the bounded execution context for a Byrd TODO.</summary>
    public async Task<ActiveTodoContext> GetExecutionContextAsync(
        string todoId,
        int requirementSnippetLimit = 5,
        int sessionTurnSummaryLimit = 5,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<ActiveTodoContext>(
            $"mcpserver/todo-execution/todos/{Encode(todoId)}?requirementSnippetLimit={requirementSnippetLimit}&sessionTurnSummaryLimit={sessionTurnSummaryLimit}",
            cancellationToken);
    }

    /// <summary>Return the execution delta for a Byrd TODO since a checkpoint.</summary>
    public async Task<TodoDeltaContext> GetDeltaContextAsync(
        string todoId,
        string? sinceCheckpointId = null,
        CancellationToken cancellationToken = default)
    {
        var suffix = string.IsNullOrWhiteSpace(sinceCheckpointId)
            ? string.Empty
            : $"?sinceCheckpointId={Encode(sinceCheckpointId)}";
        return await GetAsync<TodoDeltaContext>($"mcpserver/todo-execution/todos/{Encode(todoId)}/delta{suffix}", cancellationToken);
    }

    /// <summary>Store the test plan for a Byrd TODO.</summary>
    public async Task<SetTodoTestPlanResult> SetTestPlanAsync(
        string todoId,
        SetTodoTestPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutAsync<SetTodoTestPlanResult>($"mcpserver/todo-execution/todos/{Encode(todoId)}/test-plan", request, cancellationToken);
    }

    /// <summary>Move a Byrd TODO through its execution states.</summary>
    public async Task<UpdateTodoStatusResult> UpdateExecutionStatusAsync(
        string todoId,
        UpdateTodoStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<UpdateTodoStatusResult>($"mcpserver/todo-execution/todos/{Encode(todoId)}/status", request, cancellationToken);
    }

    /// <summary>Append a checkpoint to a Byrd TODO.</summary>
    public async Task<AppendTodoCheckpointResult> AppendCheckpointAsync(
        string todoId,
        AppendTodoCheckpointRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<AppendTodoCheckpointResult>($"mcpserver/todo-execution/todos/{Encode(todoId)}/checkpoints", request, cancellationToken);
    }

    /// <summary>Record a validation result for a Byrd TODO.</summary>
    public async Task<RecordTodoValidationResultResult> RecordValidationResultAsync(
        string todoId,
        RecordTodoValidationResultRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<RecordTodoValidationResultResult>($"mcpserver/todo-execution/todos/{Encode(todoId)}/validation", request, cancellationToken);
    }

    /// <summary>Link historical session turns to a Byrd TODO.</summary>
    public async Task<LinkTodoToSessionTurnsResult> LinkSessionTurnsAsync(
        string todoId,
        LinkTodoToSessionTurnsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<LinkTodoToSessionTurnsResult>($"mcpserver/todo-execution/todos/{Encode(todoId)}/session-turns", request, cancellationToken);
    }

    /// <summary>Perform a safe Android ADB step.</summary>
    public async Task<AdbStepResult> AdbStepAsync(
        AdbStepRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<AdbStepResult>("mcpserver/todo-execution/adb/step", request, cancellationToken);
    }

    /// <summary>
    /// Streams a Copilot-generated status report for the specified TODO item via SSE.
    /// Each yielded string is one line of the report, delivered in real-time as the
    /// server generates it.
    /// </summary>
    /// <param name="id">TODO item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of status-report lines.</returns>
    /// <example>
    /// <code>
    /// await foreach (var line in client.Todo.StreamStatusAsync("MVP-APP-001"))
    ///     Console.WriteLine(line);
    /// </code>
    /// </example>
    /// <seealso cref="StreamImplementAsync"/>
    /// <seealso cref="StreamPlanAsync"/>
    public IAsyncEnumerable<string> StreamStatusAsync(string id, CancellationToken cancellationToken = default)
        => StreamSseAsync($"mcpserver/todo/{Encode(id)}/prompt/status", cancellationToken);

    /// <summary>
    /// Streams a Copilot-generated implementation guide for the specified TODO item via SSE.
    /// Each yielded string is one line of the guide, delivered in real-time as the
    /// server generates it.
    /// </summary>
    /// <param name="id">TODO item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of implementation-guide lines.</returns>
    /// <example>
    /// <code>
    /// await foreach (var line in client.Todo.StreamImplementAsync("MVP-APP-001"))
    ///     Console.WriteLine(line);
    /// </code>
    /// </example>
    /// <seealso cref="StreamStatusAsync"/>
    /// <seealso cref="StreamPlanAsync"/>
    public IAsyncEnumerable<string> StreamImplementAsync(string id, CancellationToken cancellationToken = default)
        => StreamSseAsync($"mcpserver/todo/{Encode(id)}/prompt/implement", cancellationToken);

    /// <summary>
    /// Streams a Copilot-generated plan for the specified TODO item via SSE.
    /// Each yielded string is one line of the plan, delivered in real-time as the
    /// server generates it.
    /// </summary>
    /// <param name="id">TODO item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of plan lines.</returns>
    /// <example>
    /// <code>
    /// await foreach (var line in client.Todo.StreamPlanAsync("MVP-APP-001"))
    ///     Console.WriteLine(line);
    /// </code>
    /// </example>
    /// <seealso cref="StreamStatusAsync"/>
    /// <seealso cref="StreamImplementAsync"/>
    public IAsyncEnumerable<string> StreamPlanAsync(string id, CancellationToken cancellationToken = default)
        => StreamSseAsync($"mcpserver/todo/{Encode(id)}/prompt/plan", cancellationToken);

    /// <summary>
    /// Enqueues a TODO status prompt through the agent-pool one-shot queue.
    /// </summary>
    /// <param name="id">TODO item ID.</param>
    /// <param name="request">Optional one-shot queue overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Queue job metadata.</returns>
    public async Task<AgentPoolEnqueueResult> QueueStatusPromptAsync(
        string id,
        AgentPoolOneShotRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolEnqueueResult>(
            $"mcpserver/todo/{Encode(id)}/prompt/status/queue",
            request,
            cancellationToken);
    }

    /// <summary>
    /// Enqueues a TODO implementation prompt through the agent-pool one-shot queue.
    /// </summary>
    /// <param name="id">TODO item ID.</param>
    /// <param name="request">Optional one-shot queue overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Queue job metadata.</returns>
    public async Task<AgentPoolEnqueueResult> QueueImplementPromptAsync(
        string id,
        AgentPoolOneShotRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolEnqueueResult>(
            $"mcpserver/todo/{Encode(id)}/prompt/implement/queue",
            request,
            cancellationToken);
    }

    /// <summary>
    /// Enqueues a TODO planning prompt through the agent-pool one-shot queue.
    /// </summary>
    /// <param name="id">TODO item ID.</param>
    /// <param name="request">Optional one-shot queue overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Queue job metadata.</returns>
    public async Task<AgentPoolEnqueueResult> QueuePlanPromptAsync(
        string id,
        AgentPoolOneShotRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentPoolEnqueueResult>(
            $"mcpserver/todo/{Encode(id)}/prompt/plan/queue",
            request,
            cancellationToken);
    }

    private static string Encode(string value) => System.Uri.EscapeDataString(value);

    private static string BuildQueryString(string? keyword, string? priority, string? section, string? id, bool? done)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (keyword is not null) parts.Add($"keyword={Encode(keyword)}");
        if (priority is not null) parts.Add($"priority={Encode(priority)}");
        if (section is not null) parts.Add($"section={Encode(section)}");
        if (id is not null) parts.Add($"id={Encode(id)}");
        if (done.HasValue) parts.Add($"done={done.Value.ToString().ToLowerInvariant()}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }
}
