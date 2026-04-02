using McpServer.Client;
using McpServer.McpAgent.PowerShellSessions;
using McpServer.McpAgent.SessionLog;
using McpServer.McpAgent.Todo;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using Microsoft.Extensions.AI;
using IAgentSessionLogWorkflow = McpServer.McpAgent.SessionLog.ISessionLogWorkflow;
using IAgentTodoWorkflow = McpServer.McpAgent.Todo.ITodoWorkflow;
using IReplSessionLogWorkflow = McpServer.Repl.Core.ISessionLogWorkflow;

namespace McpServer.McpAgent.Hosting;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Adapts hosted-agent tool definitions to the existing
/// session-log, TODO, repository, desktop-launch, local PowerShell-session contracts,
/// and REPL-based requirements, session history, and generic client passthrough operations.
/// </summary>
internal sealed class McpHostedAgentToolAdapter
{
    private readonly McpServerClient _client;
    private readonly IHostedPowerShellSessionManager _powerShellSessions;
    private readonly IAgentSessionLogWorkflow _sessionLog;
    private readonly IAgentTodoWorkflow _todo;
    private readonly IRequirementsWorkflow _requirements;
    private readonly IGenericClientPassthrough _clientPassthrough;
    private readonly IReplSessionLogWorkflow _replSessionLog;

    public McpHostedAgentToolAdapter(
        McpServerClient client,
        IAgentSessionLogWorkflow sessionLog,
        IAgentTodoWorkflow todo,
        IHostedPowerShellSessionManager powerShellSessions,
        IRequirementsWorkflow requirements,
        IGenericClientPassthrough clientPassthrough,
        IReplSessionLogWorkflow replSessionLog)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _sessionLog = sessionLog ?? throw new ArgumentNullException(nameof(sessionLog));
        _todo = todo ?? throw new ArgumentNullException(nameof(todo));
        _powerShellSessions = powerShellSessions ?? throw new ArgumentNullException(nameof(powerShellSessions));
        _requirements = requirements ?? throw new ArgumentNullException(nameof(requirements));
        _clientPassthrough = clientPassthrough ?? throw new ArgumentNullException(nameof(clientPassthrough));
        _replSessionLog = replSessionLog ?? throw new ArgumentNullException(nameof(replSessionLog));
    }

    public IReadOnlyList<AIFunction> CreateFunctions() =>
    [
        // ── Session log tools ──────────────────────────────────────────
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
            (Func<string?, int, int, CancellationToken, Task<IReadOnlyList<ISessionLogSummary>>>)QuerySessionHistoryAsync,
            "mcp_session_query_history",
            "Query session log history with optional agent filter, limit, and offset for pagination."),

        // ── TODO tools ─────────────────────────────────────────────────
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
            (Func<TodoCreateRequest, CancellationToken, Task<TodoMutationResult>>)CreateTodoAsync,
            "mcp_todo_create",
            "Create a new MCP TODO item with id, title, section, priority, and optional estimate/note/description fields."),
        CreateTool(
            (Func<string, CancellationToken, Task<TodoMutationResult>>)DeleteTodoAsync,
            "mcp_todo_delete",
            "Delete an MCP TODO item by its identifier."),
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

        // ── Repository tools ───────────────────────────────────────────
        CreateTool(
            (Func<string, CancellationToken, Task<RepoFileReadResult>>)ReadRepoFileAsync,
            "mcp_repo_read",
            "Read repository file content by relative path from the workspace root."),
        CreateTool(
            (Func<string?, CancellationToken, Task<RepoListResult>>)ListRepoAsync,
            "mcp_repo_list",
            "List repository files and directories under an optional relative path."),
        CreateTool(
            (Func<string, string, CancellationToken, Task<RepoWriteResult>>)WriteRepoFileAsync,
            "mcp_repo_write",
            "Write repository file content by relative path from the workspace root."),

        // ── Desktop tools ──────────────────────────────────────────────
        CreateTool(
            (Func<string, string?, string?, Dictionary<string, string>?, bool, string, bool, int?, CancellationToken, Task<DesktopLaunchResult>>)LaunchDesktopProcessAsync,
            "mcp_desktop_launch",
            "Launch a local desktop process through the MCP server for the current workspace."),

        // ── PowerShell session tools ───────────────────────────────────
        CreateTool(
            (Func<string?, CancellationToken, Task<PowerShellSessionCreateResult>>)CreatePowerShellSessionAsync,
            "mcp_powershell_session_create",
            "Create a persistent PowerShell session hosted directly inside the current .NET agent process."),
        CreateTool(
            (Func<string, string, CancellationToken, Task<PowerShellSessionCommandResult>>)ExecutePowerShellSessionCommandAsync,
            "mcp_powershell_session_command",
            "Run a command inside a previously created in-process PowerShell session and return its output."),
        CreateTool(
            (Func<string, CancellationToken, Task<PowerShellSessionCloseResult>>)ClosePowerShellSessionAsync,
            "mcp_powershell_session_close",
            "Close a previously created in-process PowerShell session and release its resources."),

        // ── Requirements tools (REPL-backed) ───────────────────────────
        CreateTool(
            (Func<string?, string?, CancellationToken, Task<IFrQueryResult>>)ListFunctionalRequirementsAsync,
            "mcp_requirements_list_fr",
            "List functional requirements with optional area and status filters."),
        CreateTool(
            (Func<string?, string?, string?, CancellationToken, Task<ITrQueryResult>>)ListTechnicalRequirementsAsync,
            "mcp_requirements_list_tr",
            "List technical requirements with optional area, subarea, and status filters."),
        CreateTool(
            (Func<string?, string?, CancellationToken, Task<ITestQueryResult>>)ListTestRequirementsAsync,
            "mcp_requirements_list_test",
            "List test requirements with optional area and status filters."),
        CreateTool(
            (Func<string, CancellationToken, Task<IFrItem>>)GetFunctionalRequirementAsync,
            "mcp_requirements_get_fr",
            "Get a specific functional requirement by its canonical identifier (e.g. FR-MCP-001)."),
        CreateTool(
            (Func<string, CancellationToken, Task<ITrItem>>)GetTechnicalRequirementAsync,
            "mcp_requirements_get_tr",
            "Get a specific technical requirement by its canonical identifier (e.g. TR-MCP-ARCH-001)."),
        CreateTool(
            (Func<string, CancellationToken, Task<ITestItem>>)GetTestRequirementAsync,
            "mcp_requirements_get_test",
            "Get a specific test requirement by its canonical identifier (e.g. TEST-MCP-001)."),

        // ── Generic client passthrough (REPL-backed) ───────────────────
        CreateTool(
            (Func<string, string, Dictionary<string, object?>, CancellationToken, Task<object?>>)InvokeClientAsync,
            "mcp_client_invoke",
            "Dynamically invoke any MCP Server sub-client method by specifying clientName (e.g. 'context', 'github', 'workspace'), methodName (e.g. 'SearchAsync'), and a dictionary of arguments."),
    ];

    private static AIFunction CreateTool(Delegate implementation, string name, string description) =>
        AIFunctionFactory.Create(
            implementation,
            new AIFunctionFactoryOptions
            {
                Description = description,
                Name = name,
            });

    // ── Session log implementations ────────────────────────────────────

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

    private Task<IReadOnlyList<ISessionLogSummary>> QuerySessionHistoryAsync(
        string? agent,
        int limit = 10,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        _replSessionLog.QueryHistoryAsync(agent, limit, offset, cancellationToken);

    // ── TODO implementations ───────────────────────────────────────────

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

    private Task<TodoMutationResult> CreateTodoAsync(
        TodoCreateRequest request,
        CancellationToken cancellationToken) =>
        _client.Todo.CreateAsync(
            request ?? throw new ArgumentNullException(nameof(request)),
            cancellationToken);

    private Task<TodoMutationResult> DeleteTodoAsync(
        string id,
        CancellationToken cancellationToken) =>
        _client.Todo.DeleteAsync(id, cancellationToken);

    private Task<string> GetTodoPlanAsync(string id, CancellationToken cancellationToken) =>
        _todo.GetPlanAsync(id, cancellationToken);

    private Task<string> GetTodoStatusAsync(string id, CancellationToken cancellationToken) =>
        _todo.GetStatusReportAsync(id, cancellationToken);

    private Task<string> GetTodoImplementationGuideAsync(string id, CancellationToken cancellationToken) =>
        _todo.GetImplementationGuideAsync(id, cancellationToken);

    // ── Repository implementations ─────────────────────────────────────

    private Task<RepoFileReadResult> ReadRepoFileAsync(string path, CancellationToken cancellationToken) =>
        _client.Repo.ReadFileAsync(path, cancellationToken);

    private Task<RepoListResult> ListRepoAsync(string? path, CancellationToken cancellationToken) =>
        _client.Repo.ListAsync(path, cancellationToken);

    private Task<RepoWriteResult> WriteRepoFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken) =>
        _client.Repo.WriteFileAsync(path, content, cancellationToken);

    // ── Desktop implementations ────────────────────────────────────────

    private Task<DesktopLaunchResult> LaunchDesktopProcessAsync(
        string executablePath,
        string? arguments = null,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null,
        bool createNoWindow = false,
        string windowStyle = "Normal",
        bool waitForExit = false,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default) =>
        _client.Desktop.LaunchAsync(
            new DesktopLaunchRequest
            {
                ExecutablePath = executablePath,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                EnvironmentVariables = environmentVariables,
                CreateNoWindow = createNoWindow,
                WindowStyle = string.IsNullOrWhiteSpace(windowStyle) ? "Normal" : windowStyle,
                WaitForExit = waitForExit,
                TimeoutMs = timeoutMs
            },
            cancellationToken);

    // ── PowerShell session implementations ─────────────────────────────

    private Task<PowerShellSessionCreateResult> CreatePowerShellSessionAsync(
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_powerShellSessions.CreateSession(_client.WorkspacePath, workingDirectory));
    }

    private Task<PowerShellSessionCommandResult> ExecutePowerShellSessionCommandAsync(
        string sessionId,
        string command,
        CancellationToken cancellationToken = default) =>
        _powerShellSessions.ExecuteCommandAsync(sessionId, command, cancellationToken);

    private Task<PowerShellSessionCloseResult> ClosePowerShellSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_powerShellSessions.CloseSession(sessionId));
    }

    // ── Requirements implementations (REPL-backed) ─────────────────────

    private Task<IFrQueryResult> ListFunctionalRequirementsAsync(
        string? area,
        string? status,
        CancellationToken cancellationToken) =>
        _requirements.ListFrAsync(area, status, cancellationToken);

    private Task<ITrQueryResult> ListTechnicalRequirementsAsync(
        string? area,
        string? subarea,
        string? status,
        CancellationToken cancellationToken) =>
        _requirements.ListTrAsync(area, subarea, status, cancellationToken);

    private Task<ITestQueryResult> ListTestRequirementsAsync(
        string? area,
        string? status,
        CancellationToken cancellationToken) =>
        _requirements.ListTestAsync(area, status, cancellationToken);

    private Task<IFrItem> GetFunctionalRequirementAsync(
        string id,
        CancellationToken cancellationToken) =>
        _requirements.GetFrAsync(id, cancellationToken);

    private Task<ITrItem> GetTechnicalRequirementAsync(
        string id,
        CancellationToken cancellationToken) =>
        _requirements.GetTrAsync(id, cancellationToken);

    private Task<ITestItem> GetTestRequirementAsync(
        string id,
        CancellationToken cancellationToken) =>
        _requirements.GetTestAsync(id, cancellationToken);

    // ── Generic client passthrough (REPL-backed) ───────────────────────

    private Task<object?> InvokeClientAsync(
        string clientName,
        string methodName,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken) =>
        _clientPassthrough.InvokeAsync(clientName, methodName, arguments, cancellationToken);
}
