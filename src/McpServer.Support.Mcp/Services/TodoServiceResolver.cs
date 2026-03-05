using System.Collections.Concurrent;
using McpServer.Support.Mcp.Ingestion;
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
    private readonly ITodoServiceFactory _todoServiceFactory;
    private readonly string _primaryWorkspacePath;

    /// <summary>Initializes a new instance of the <see cref="TodoServiceResolver"/> class.</summary>
    internal TodoServiceResolver(
        ITodoService primaryService,
        IOptions<IngestionOptions> ingestionOptions,
        ITodoServiceFactory todoServiceFactory)
    {
        _primaryService = primaryService ?? throw new ArgumentNullException(nameof(primaryService));
        ArgumentNullException.ThrowIfNull(ingestionOptions);
        _todoServiceFactory = todoServiceFactory ?? throw new ArgumentNullException(nameof(todoServiceFactory));

        _primaryWorkspacePath = Path.GetFullPath(ingestionOptions.Value.RepoRoot ?? ".");
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
        return _todoServiceFactory.CreateForWorkspace(workspacePath, ctx);
    }
}
