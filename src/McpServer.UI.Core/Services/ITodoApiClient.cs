using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Host-provided API client abstraction for TODO operations used by UI.Core CQRS handlers.
/// </summary>
public interface ITodoApiClient
{
    /// <summary>
    /// Lists TODO items using the supplied filters.
    /// </summary>
    /// <param name="query">TODO query filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List result for the requested filters.</returns>
    Task<ListTodosResult> ListTodosAsync(ListTodosQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single TODO item by ID.
    /// </summary>
    /// <param name="todoId">TODO item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed TODO item, or <see langword="null"/> when not found.</returns>
    Task<TodoDetail?> GetTodoAsync(string todoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a TODO item.
    /// </summary>
    Task<TodoMutationOutcome> CreateTodoAsync(CreateTodoCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a TODO item.
    /// </summary>
    Task<TodoMutationOutcome> UpdateTodoAsync(UpdateTodoCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a TODO item.
    /// </summary>
    Task<TodoMutationOutcome> DeleteTodoAsync(DeleteTodoCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes requirements for a TODO item and returns discovered FR/TR associations.
    /// </summary>
    Task<TodoRequirementsAnalysis> AnalyzeTodoRequirementsAsync(string todoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates and aggregates the TODO status prompt stream.
    /// </summary>
    Task<TodoPromptOutput> GenerateTodoStatusPromptAsync(string todoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates and aggregates the TODO implement prompt stream.
    /// </summary>
    Task<TodoPromptOutput> GenerateTodoImplementPromptAsync(string todoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates and aggregates the TODO plan prompt stream.
    /// </summary>
    Task<TodoPromptOutput> GenerateTodoPlanPromptAsync(string todoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams TODO status prompt lines incrementally as they arrive from the server.
    /// </summary>
    IAsyncEnumerable<string> StreamTodoStatusPromptAsync(string todoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams TODO implement prompt lines incrementally as they arrive from the server.
    /// </summary>
    IAsyncEnumerable<string> StreamTodoImplementPromptAsync(string todoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams TODO plan prompt lines incrementally as they arrive from the server.
    /// </summary>
    IAsyncEnumerable<string> StreamTodoPlanPromptAsync(string todoId, CancellationToken cancellationToken = default);
}
