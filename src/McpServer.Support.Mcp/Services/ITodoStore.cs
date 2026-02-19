namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Internal persistence abstraction for TODO storage backends.
/// Implementations may use YAML files or SQLite.
/// </summary>
internal interface ITodoStore
{
    Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken = default);

    Task<TodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default);

    Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default);

    Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
