using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Factory for constructing TODO service implementations from configured storage provider settings.
/// </summary>
/// <remarks>
/// TR-MCP-TODO-005 (provider-agnostic): the database is the sole TODO provider. The legacy
/// <c>sqlite</c> value is aliased to <c>database</c> (see <c>McpInstanceResolver.ValidateTodoStorage</c>)
/// so it flows through <c>McpDbContext</c> + <c>McpDatabaseProviderFactory</c> (TR-MCP-CFG-007).
/// The removed <c>yaml</c> provider is rejected; <c>TODO.yaml</c> is now only a read-only projection.
/// </remarks>
internal sealed class TodoServiceFactory : ITodoServiceFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<IngestionOptions> _ingestionOptions;
    private readonly IOptions<TodoStorageOptions> _storageOptions;
    private readonly IWriteAuditLog _auditLog;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IChangeEventBus? _eventBus;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="TodoServiceFactory"/> class.
    /// </summary>
    public TodoServiceFactory(
        IServiceScopeFactory scopeFactory,
        IOptions<IngestionOptions> ingestionOptions,
        IOptions<TodoStorageOptions> storageOptions,
        IWriteAuditLog auditLog,
        ILoggerFactory loggerFactory,
        IChangeEventBus? eventBus = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _ingestionOptions = ingestionOptions ?? throw new ArgumentNullException(nameof(ingestionOptions));
        _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _eventBus = eventBus;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public ITodoService CreatePrimary()
    {
        EnsureDatabaseProvider();
        return BuildEfTodoService();
    }

    /// <inheritdoc />
    public ITodoService CreateForWorkspace(string workspacePath, WorkspaceContext workspaceContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        ArgumentNullException.ThrowIfNull(workspaceContext);

        EnsureDatabaseProvider();

        // The database provider is process-wide (selected by Mcp:Database:Provider, TR-MCP-CFG-007);
        // workspaceContext is preserved for projection hooks but no longer selects a per-workspace store.
        return BuildEfTodoService();
    }

    private EfTodoService BuildEfTodoService() => new(
        _scopeFactory,
        _ingestionOptions,
        _storageOptions,
        _auditLog,
        _loggerFactory.CreateLogger<EfTodoService>(),
        _eventBus,
        _httpContextAccessor);

    private void EnsureDatabaseProvider()
    {
        var raw = (_storageOptions.Value.Provider ?? TodoStorageOptions.DatabaseProvider).Trim().ToUpperInvariant();
        if (raw == "YAML")
            throw new InvalidOperationException(
                "Mcp:TodoStorage:Provider='yaml' has been removed; the database is the sole source of truth and " +
                "TODO.yaml is a read-only projection. Set Mcp:TodoStorage:Provider='database'.");
    }
}
