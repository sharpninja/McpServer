using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Factory for constructing TODO service implementations from configured storage provider settings.
/// </summary>
/// <remarks>
/// TR-MCP-TODO-005 (provider-agnostic): canonical provider values are
/// <c>yaml</c> and <c>database</c>. The legacy <c>sqlite</c> value is aliased to
/// <c>database</c> (see <c>McpInstanceResolver.ValidateTodoStorage</c>) so it
/// flows through <c>McpDbContext</c> + <c>McpDatabaseProviderFactory</c>
/// (TR-MCP-CFG-007) rather than a hardcoded sqlite file.
/// </remarks>
internal sealed class TodoServiceFactory : ITodoServiceFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<IngestionOptions> _ingestionOptions;
    private readonly IOptions<TodoStorageOptions> _storageOptions;
    private readonly IWriteAuditLog _auditLog;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IChangeEventBus? _eventBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="TodoServiceFactory"/> class.
    /// </summary>
    public TodoServiceFactory(
        IServiceScopeFactory scopeFactory,
        IOptions<IngestionOptions> ingestionOptions,
        IOptions<TodoStorageOptions> storageOptions,
        IWriteAuditLog auditLog,
        ILoggerFactory loggerFactory,
        IChangeEventBus? eventBus = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _ingestionOptions = ingestionOptions ?? throw new ArgumentNullException(nameof(ingestionOptions));
        _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public ITodoService CreatePrimary() => GetProvider() switch
    {
        TodoProviderKind.Database => new EfTodoService(
            _scopeFactory,
            _ingestionOptions,
            _storageOptions,
            _auditLog,
            _loggerFactory.CreateLogger<EfTodoService>(),
            _eventBus),
        _ => new TodoService(_ingestionOptions, _auditLog, _loggerFactory.CreateLogger<TodoService>(), _eventBus),
    };

    /// <inheritdoc />
    public ITodoService CreateForWorkspace(string workspacePath, WorkspaceContext workspaceContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        ArgumentNullException.ThrowIfNull(workspaceContext);

        var todoRelPath = workspaceContext.TodoFilePath ?? "docs/Project/TODO.yaml";
        var todoFullPath = Path.GetFullPath(
            Path.IsPathRooted(todoRelPath) ? todoRelPath : Path.Combine(workspacePath, todoRelPath));

        if (GetProvider() == TodoProviderKind.Database)
        {
            // Database provider is process-wide; workspace path is preserved for future projection hooks
            // but the database itself is selected by Mcp:Database:Provider (TR-MCP-CFG-007).
            return new EfTodoService(
                _scopeFactory,
                _ingestionOptions,
                _storageOptions,
                _auditLog,
                _loggerFactory.CreateLogger<EfTodoService>(),
                _eventBus);
        }

        return new TodoService(todoFullPath, _auditLog, _loggerFactory.CreateLogger<TodoService>(), _eventBus);
    }

    private TodoProviderKind GetProvider()
    {
        var raw = (_storageOptions.Value.Provider ?? TodoStorageOptions.DatabaseProvider).Trim().ToUpperInvariant();
        return raw switch
        {
            "YAML" => TodoProviderKind.Yaml,
            "DATABASE" or "SQLITE" => TodoProviderKind.Database,
            _ => TodoProviderKind.Database,
        };
    }

    private enum TodoProviderKind
    {
        Yaml,
        Database,
    }
}
