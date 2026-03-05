using System.Collections.Concurrent;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-MT-001: Resolves the correct <see cref="ITodoService"/> for the current
/// <see cref="WorkspaceContext"/>. For the primary workspace, returns the existing
/// singleton; for other workspaces, creates and caches workspace-specific instances.
/// </summary>
public sealed class TodoServiceResolver : IDisposable
{
    private readonly ConcurrentDictionary<string, ITodoService> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ITodoService _primaryService;
    private readonly string _primaryWorkspacePath;
    private readonly string _storageProvider;
    private readonly IWriteAuditLog _auditLog;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>Initializes a new instance of the <see cref="TodoServiceResolver"/> class.</summary>
    public TodoServiceResolver(
        ITodoService primaryService,
        IOptions<IngestionOptions> ingestionOptions,
        IOptions<TodoStorageOptions> storageOptions,
        IWriteAuditLog auditLog,
        ILoggerFactory loggerFactory)
    {
        _primaryService = primaryService ?? throw new ArgumentNullException(nameof(primaryService));
        ArgumentNullException.ThrowIfNull(ingestionOptions);
        ArgumentNullException.ThrowIfNull(storageOptions);
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        _primaryWorkspacePath = Path.GetFullPath(ingestionOptions.Value.RepoRoot ?? ".");
        _storageProvider = (storageOptions.Value.Provider ?? "yaml").Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Resolves the <see cref="ITodoService"/> for the given workspace context.
    /// Returns the primary singleton when the context is unresolved or matches the primary workspace.
    /// </summary>
    public ITodoService Resolve(WorkspaceContext workspaceContext)
    {
        if (!workspaceContext.IsResolved)
            return _primaryService;

        var normalized = Path.GetFullPath(workspaceContext.WorkspacePath!);
        if (string.Equals(normalized, _primaryWorkspacePath, StringComparison.OrdinalIgnoreCase))
            return _primaryService;

        return _cache.GetOrAdd(normalized, _ => CreateForWorkspace(normalized, workspaceContext));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var svc in _cache.Values)
        {
            if (svc is IDisposable d)
                d.Dispose();
        }

        _cache.Clear();
    }

    private ITodoService CreateForWorkspace(string workspacePath, WorkspaceContext ctx)
    {
        if (_storageProvider == "SQLITE")
        {
            var dataDir = ctx.DataDirectory ?? workspacePath;
            var dataSource = Path.GetFullPath(Path.Combine(dataDir, "mcp.db"));
            return new SqliteTodoService(dataSource, _auditLog, _loggerFactory.CreateLogger<SqliteTodoService>());
        }

        var todoRelPath = ctx.TodoFilePath ?? "docs/Project/TODO.yaml";
        var todoFullPath = Path.GetFullPath(
            Path.IsPathRooted(todoRelPath) ? todoRelPath : Path.Combine(workspacePath, todoRelPath));
        return new TodoService(todoFullPath, _auditLog, _loggerFactory.CreateLogger<TodoService>());
    }
}
