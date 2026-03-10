using McpServer.Client;
using McpServer.McpAgent.PowerShellSessions;
using McpServer.McpAgent.SessionLog;
using McpServer.McpAgent.Todo;
using McpServer.Client.Models;
using Microsoft.Extensions.AI;

namespace McpServer.McpAgent.Hosting;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Adapts hosted-agent tool definitions to the existing
/// session-log, TODO, repository, desktop-launch, and local PowerShell-session contracts.
/// </summary>
internal sealed class McpHostedAgentToolAdapter
{
    private readonly McpServerClient _client;
    private readonly IHostedPowerShellSessionManager _powerShellSessions;
    private readonly ISessionLogWorkflow _sessionLog;
    private readonly ITodoWorkflow _todo;

    public McpHostedAgentToolAdapter(
        McpServerClient client,
        ISessionLogWorkflow sessionLog,
        ITodoWorkflow todo,
        IHostedPowerShellSessionManager powerShellSessions)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _sessionLog = sessionLog ?? throw new ArgumentNullException(nameof(sessionLog));
        _todo = todo ?? throw new ArgumentNullException(nameof(todo));
        _powerShellSessions = powerShellSessions ?? throw new ArgumentNullException(nameof(powerShellSessions));
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
        CreateTool(
            (Func<string, string?, string?, Dictionary<string, string>?, bool, string, bool, int?, CancellationToken, Task<DesktopLaunchResult>>)LaunchDesktopProcessAsync,
            "mcp_desktop_launch",
            "Launch a local desktop process through the MCP server for the current workspace."),
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

    private Task<RepoFileReadResult> ReadRepoFileAsync(string path, CancellationToken cancellationToken) =>
        _client.Repo.ReadFileAsync(path, cancellationToken);

    private Task<RepoListResult> ListRepoAsync(string? path, CancellationToken cancellationToken) =>
        _client.Repo.ListAsync(path, cancellationToken);

    private Task<RepoWriteResult> WriteRepoFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken) =>
        _client.Repo.WriteFileAsync(path, content, cancellationToken);

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
}
