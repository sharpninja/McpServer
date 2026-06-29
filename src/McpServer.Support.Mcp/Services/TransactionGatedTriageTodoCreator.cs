using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TRIAGE-004: Routes triage-created TODO items through the same transaction gate as public TODO mutations.
/// </summary>
public sealed class TransactionGatedTriageTodoCreator : ITriageTodoCreator
{
    private readonly ITransactionGatedTodoMutationService _todoMutations;

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedTriageTodoCreator"/> class.</summary>
    /// <param name="todoMutations">The transaction-gated TODO mutation service.</param>
    public TransactionGatedTriageTodoCreator(ITransactionGatedTodoMutationService todoMutations)
    {
        _todoMutations = todoMutations ?? throw new ArgumentNullException(nameof(todoMutations));
    }

    /// <inheritdoc />
    public Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
        => _todoMutations.CreateAsync(request, cancellationToken);
}
