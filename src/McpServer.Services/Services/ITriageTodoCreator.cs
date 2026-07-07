using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TRIAGE-004: Creates TODO items produced by triage research through the host-selected mutation path.
/// </summary>
public interface ITriageTodoCreator
{
    /// <summary>Creates the supplied triage TODO in the active workspace.</summary>
    /// <param name="request">The TODO create request produced by triage research.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The TODO mutation result from the selected mutation path.</returns>
    Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-TRIAGE-004: Default triage TODO creator used by hosts that do not provide a transaction-gated creator.
/// </summary>
public sealed class DirectTriageTodoCreator : ITriageTodoCreator
{
    private readonly ITodoService _todoService;

    /// <summary>Initializes a new instance of the <see cref="DirectTriageTodoCreator"/> class.</summary>
    /// <param name="todoService">The TODO service used for direct creation.</param>
    public DirectTriageTodoCreator(ITodoService todoService)
    {
        _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
    }

    /// <inheritdoc />
    public Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
        => _todoService.CreateAsync(request, cancellationToken);
}
