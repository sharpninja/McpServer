using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Byrd execution workflow service for TODO-centered development.
/// </summary>
public interface ITodoExecutionService
{
    /// <summary>Create a bounded iteration phase.</summary>
    Task<CreateIterationPhaseResult> CreateIterationPhaseAsync(string workspacePath, CreateIterationPhaseRequest request, CancellationToken cancellationToken = default);

    /// <summary>Create execution TODOs from an approved plan.</summary>
    Task<CreateTodosFromPlanResult> CreateTodosFromPlanAsync(string workspacePath, CreateTodosFromPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>Return the active TODO the agent should work on.</summary>
    Task<ActiveTodoResult?> GetActiveTodoAsync(string workspacePath, CancellationToken cancellationToken = default);

    /// <summary>FR-MCP-REQAC-002: return the raw execution TODO record so callers can copy structured fields (e.g. acceptance criteria) verbatim.</summary>
    Task<TodoExecutionRecord?> GetTodoAsync(string workspacePath, string todoId, CancellationToken cancellationToken = default);

    /// <summary>Return the next ready TODO the agent should work on.</summary>
    Task<ActiveTodoResult?> GetNextReadyTodoAsync(string workspacePath, CancellationToken cancellationToken = default);

    /// <summary>Hydrate a bounded execution context for a TODO.</summary>
    Task<ActiveTodoContext?> GetExecutionContextAsync(
        string workspacePath,
        string todoId,
        int requirementSnippetLimit = 5,
        int sessionTurnSummaryLimit = 5,
        CancellationToken cancellationToken = default);

    /// <summary>Return the delta context for a TODO since a checkpoint.</summary>
    Task<TodoDeltaContext?> GetDeltaContextAsync(
        string workspacePath,
        string todoId,
        string? sinceCheckpointId,
        CancellationToken cancellationToken = default);

    /// <summary>Store the test plan for a TODO.</summary>
    Task<SetTodoTestPlanResult> SetTestPlanAsync(string workspacePath, string todoId, SetTodoTestPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>Update the execution status for a TODO.</summary>
    Task<UpdateTodoStatusResult> UpdateStatusAsync(string workspacePath, string todoId, UpdateTodoStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>Append a checkpoint to a TODO.</summary>
    Task<AppendTodoCheckpointResult> AppendCheckpointAsync(string workspacePath, string todoId, AppendTodoCheckpointRequest request, CancellationToken cancellationToken = default);

    /// <summary>Record a validation result for a TODO.</summary>
    Task<RecordTodoValidationResultResult> RecordValidationResultAsync(string workspacePath, string todoId, RecordTodoValidationResultRequest request, CancellationToken cancellationToken = default);

    /// <summary>Link historical session turns to a TODO.</summary>
    Task<LinkTodoToSessionTurnsResult> LinkTodoToSessionTurnsAsync(string workspacePath, string todoId, LinkTodoToSessionTurnsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Perform a safe Android ADB step.</summary>
    Task<AdbStepResult> AdbStepAsync(string workspacePath, AdbStepRequest request, CancellationToken cancellationToken = default);
}
