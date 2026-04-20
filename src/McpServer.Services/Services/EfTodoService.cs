using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TODO-005 / TR-MCP-TODO-006 (provider-agnostic): EF Core-backed TODO
/// service that routes persistence through <c>McpDbContext</c> and therefore
/// through whichever provider <c>Mcp:Database:Provider</c> selects via
/// <c>McpDatabaseProviderFactory</c> (TR-MCP-CFG-007).
/// </summary>
/// <remarks>
/// Byrd Development Process (phase 3 skeleton): members are declared and wired
/// into DI to unblock the compile gate. Method bodies throw
/// <see cref="NotImplementedException"/> and are ported incrementally from
/// <c>SqliteTodoService</c>, each guarded by its matching tests.
/// </remarks>
internal sealed class EfTodoService : ITodoService, ITodoStore, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<IngestionOptions> _ingestionOptions;
    private readonly IOptions<TodoStorageOptions> _storageOptions;
    private readonly IWriteAuditLog _auditLog;
    private readonly ILogger<EfTodoService> _logger;
    private readonly IChangeEventBus? _eventBus;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="EfTodoService"/> class.
    /// </summary>
    public EfTodoService(
        IServiceScopeFactory scopeFactory,
        IOptions<IngestionOptions> ingestionOptions,
        IOptions<TodoStorageOptions> storageOptions,
        IWriteAuditLog auditLog,
        ILogger<EfTodoService> logger,
        IChangeEventBus? eventBus = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _ingestionOptions = ingestionOptions ?? throw new ArgumentNullException(nameof(ingestionOptions));
        _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public void Dispose() => _writeLock.Dispose();

    /// <inheritdoc />
    public Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("EfTodoService.QueryAsync: phase 3 port pending (TR-MCP-TODO-005).");

    /// <inheritdoc />
    public Task<TodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("EfTodoService.GetByIdAsync: phase 3 port pending (TR-MCP-TODO-005).");

    /// <inheritdoc />
    public Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("EfTodoService.CreateAsync: phase 3 port pending (TR-MCP-TODO-005).");

    /// <inheritdoc />
    public Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("EfTodoService.UpdateAsync: phase 3 port pending (TR-MCP-TODO-005).");

    /// <inheritdoc />
    public Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("EfTodoService.DeleteAsync: phase 3 port pending (TR-MCP-TODO-005).");

    /// <inheritdoc />
    public Task<TodoAuditQueryResult> GetAuditAsync(string id, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("EfTodoService.GetAuditAsync: phase 3 port pending (TR-MCP-TODO-005).");

    /// <inheritdoc />
    public Task<TodoProjectionStatusResult> GetProjectionStatusAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException("EfTodoService.GetProjectionStatusAsync: phase 3 port pending (TR-MCP-TODO-006).");

    /// <inheritdoc />
    public Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException("EfTodoService.RepairProjectionAsync: phase 3 port pending (TR-MCP-TODO-006).");
}
