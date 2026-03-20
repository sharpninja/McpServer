using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Notifications;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Factory for constructing TODO service implementations from configured storage provider settings.
/// </summary>
internal sealed class TodoServiceFactory : ITodoServiceFactory
{
    private readonly IOptions<IngestionOptions> _ingestionOptions;
    private readonly IOptions<TodoStorageOptions> _storageOptions;
    private readonly IWriteAuditLog _auditLog;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IChangeEventBus? _eventBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="TodoServiceFactory"/> class.
    /// </summary>
    public TodoServiceFactory(
        IOptions<IngestionOptions> ingestionOptions,
        IOptions<TodoStorageOptions> storageOptions,
        IWriteAuditLog auditLog,
        ILoggerFactory loggerFactory,
        IChangeEventBus? eventBus = null)
    {
        _ingestionOptions = ingestionOptions ?? throw new ArgumentNullException(nameof(ingestionOptions));
        _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public ITodoService CreatePrimary()
    {
        return GetProvider() == "SQLITE"
            ? new SqliteTodoService(_ingestionOptions, _storageOptions, _auditLog, _loggerFactory.CreateLogger<SqliteTodoService>(), _eventBus)
            : new TodoService(_ingestionOptions, _auditLog, _loggerFactory.CreateLogger<TodoService>(), _eventBus);
    }

    /// <inheritdoc />
    public ITodoService CreateForWorkspace(string workspacePath, WorkspaceContext workspaceContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        ArgumentNullException.ThrowIfNull(workspaceContext);

        var todoRelPath = workspaceContext.TodoFilePath ?? "docs/Project/TODO.yaml";
        var todoFullPath = Path.GetFullPath(
            Path.IsPathRooted(todoRelPath) ? todoRelPath : Path.Combine(workspacePath, todoRelPath));

        if (GetProvider() == "SQLITE")
        {
            var dataDir = workspaceContext.DataDirectory ?? workspacePath;
            var dataSource = Path.GetFullPath(Path.Combine(dataDir, "mcp.db"));
            return new SqliteTodoService(dataSource, todoFullPath, _auditLog, _loggerFactory.CreateLogger<SqliteTodoService>(), _eventBus);
        }

        return new TodoService(todoFullPath, _auditLog, _loggerFactory.CreateLogger<TodoService>(), _eventBus);
    }

    private string GetProvider()
    {
        return (_storageOptions.Value.Provider ?? "sqlite").Trim().ToUpperInvariant();
    }
}
