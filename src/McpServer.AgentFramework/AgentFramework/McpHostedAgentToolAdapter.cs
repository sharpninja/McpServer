using McpServer.AgentFramework.SessionLog;
using McpServer.AgentFramework.Todo;
using McpServer.Client.Models;
using Microsoft.Extensions.AI;

namespace McpServer.AgentFramework.AgentFramework;

internal sealed class McpHostedAgentToolAdapter
{
    private readonly ISessionLogWorkflow _sessionLog;
    private readonly ITodoWorkflow _todo;

    public McpHostedAgentToolAdapter(
        ISessionLogWorkflow sessionLog,
        ITodoWorkflow todo)
    {
        _sessionLog = sessionLog ?? throw new ArgumentNullException(nameof(sessionLog));
        _todo = todo ?? throw new ArgumentNullException(nameof(todo));
    }

    public IReadOnlyList<AIFunction> CreateFunctions() =>
    [
        CreateTool(
            (Func<SessionLogBootstrapRequest, CancellationToken, Task<SessionLogWorkflowContext>>)BootstrapSessionAsync,
            "mcp_session_bootstrap",
            "Bootstrap the MCP session-log workflow by submitting a SessionLogBootstrapRequest payload."),
        CreateTool(
            (Func<SessionLogSessionUpdateRequest, CancellationToken, Task<SessionLogWorkflowContext>>)UpdateSessionAsync,
            "mcp_session_update",
            "Update session-level MCP session-log metadata by submitting a SessionLogSessionUpdateRequest payload."),
        CreateTool(
            (Func<SessionLogTurnCreateRequest, CancellationToken, Task<SessionLogTurnContext>>)BeginSessionTurnAsync,
            "mcp_session_turn_begin",
            "Create a new MCP session-log turn by submitting a SessionLogTurnCreateRequest payload."),
        CreateTool(
            (Func<SessionLogTurnUpdateRequest, CancellationToken, Task<SessionLogWorkflowContext>>)UpdateSessionTurnAsync,
            "mcp_session_turn_update",
            "Update an existing MCP session-log turn by submitting a SessionLogTurnUpdateRequest payload."),
        CreateTool(
            (Func<SessionLogTurnCompleteRequest, CancellationToken, Task<SessionLogTurnContext>>)CompleteSessionTurnAsync,
            "mcp_session_turn_complete",
            "Complete an MCP session-log turn by submitting a SessionLogTurnCompleteRequest payload."),
        CreateTool(
            (Func<string?, string?, string?, string?, bool?, CancellationToken, Task<TodoQueryResult>>)QueryTodosAsync,
            "mcp_todo_query",
            "Query MCP TODO items using the same keyword, priority, section, id, and done filters exposed by the MCP client."),
        CreateTool(
            (Func<string, CancellationToken, Task<TodoFlatItem>>)GetTodoAsync,
            "mcp_todo_get",
            "Get a single MCP TODO item by identifier."),
        CreateTool(
            (Func<string, TodoUpdateRequest, CancellationToken, Task<TodoMutationResult>>)UpdateTodoAsync,
            "mcp_todo_update",
            "Update an MCP TODO item by identifier using a TodoUpdateRequest payload."),
        CreateTool(
            (Func<string, CancellationToken, Task<string>>)GetTodoPlanAsync,
            "mcp_todo_plan",
            "Get the buffered MCP TODO plan text for a TODO item identifier."),
        CreateTool(
            (Func<string, CancellationToken, Task<string>>)GetTodoStatusAsync,
            "mcp_todo_status",
            "Get the buffered MCP TODO status report text for a TODO item identifier."),
        CreateTool(
            (Func<string, CancellationToken, Task<string>>)GetTodoImplementationGuideAsync,
            "mcp_todo_implementation",
            "Get the buffered MCP TODO implementation guide text for a TODO item identifier."),
    ];

    private static AIFunction CreateTool(Delegate implementation, string name, string description) =>
        AIFunctionFactory.Create(
            implementation,
            new AIFunctionFactoryOptions
            {
                Description = description,
                Name = name,
            });

    private Task<SessionLogWorkflowContext> BootstrapSessionAsync(
        SessionLogBootstrapRequest request,
        CancellationToken cancellationToken) =>
        _sessionLog.BootstrapAsync(request ?? throw new ArgumentNullException(nameof(request)), cancellationToken);

    private Task<SessionLogWorkflowContext> UpdateSessionAsync(
        SessionLogSessionUpdateRequest request,
        CancellationToken cancellationToken) =>
        _sessionLog.UpdateSessionAsync(request ?? throw new ArgumentNullException(nameof(request)), cancellationToken);

    private Task<SessionLogTurnContext> BeginSessionTurnAsync(
        SessionLogTurnCreateRequest request,
        CancellationToken cancellationToken) =>
        _sessionLog.BeginTurnAsync(request ?? throw new ArgumentNullException(nameof(request)), cancellationToken);

    private Task<SessionLogWorkflowContext> UpdateSessionTurnAsync(
        SessionLogTurnUpdateRequest request,
        CancellationToken cancellationToken) =>
        _sessionLog.UpdateTurnAsync(request ?? throw new ArgumentNullException(nameof(request)), cancellationToken);

    private Task<SessionLogTurnContext> CompleteSessionTurnAsync(
        SessionLogTurnCompleteRequest request,
        CancellationToken cancellationToken) =>
        _sessionLog.CompleteTurnAsync(request ?? throw new ArgumentNullException(nameof(request)), cancellationToken);

    private Task<TodoQueryResult> QueryTodosAsync(
        string? keyword,
        string? priority,
        string? section,
        string? id,
        bool? done,
        CancellationToken cancellationToken) =>
        _todo.QueryAsync(keyword, priority, section, id, done, cancellationToken);

    private Task<TodoFlatItem> GetTodoAsync(string id, CancellationToken cancellationToken) =>
        _todo.GetAsync(id, cancellationToken);

    private Task<TodoMutationResult> UpdateTodoAsync(
        string id,
        TodoUpdateRequest request,
        CancellationToken cancellationToken) =>
        _todo.UpdateAsync(
            id,
            request ?? throw new ArgumentNullException(nameof(request)),
            cancellationToken);

    private Task<string> GetTodoPlanAsync(string id, CancellationToken cancellationToken) =>
        _todo.GetPlanAsync(id, cancellationToken);

    private Task<string> GetTodoStatusAsync(string id, CancellationToken cancellationToken) =>
        _todo.GetStatusReportAsync(id, cancellationToken);

    private Task<string> GetTodoImplementationGuideAsync(string id, CancellationToken cancellationToken) =>
        _todo.GetImplementationGuideAsync(id, cancellationToken);
}
