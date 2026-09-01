using McpServer.Support.Mcp.Ingestion;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-MT-001: Centralized accessor for workspace-specific services from singleton contexts.
/// Uses <see cref="IHttpContextAccessor"/> to resolve the scoped <see cref="WorkspaceContext"/>
/// and provides workspace-specific <see cref="ITodoService"/> and workspace path.
/// Falls back to the primary workspace when called outside an HTTP request (e.g., STDIO, background tasks).
/// </summary>
public sealed class WorkspaceServiceAccessor
{
    private static readonly AsyncLocal<string?> OverrideWorkspacePath = new();
    private readonly TodoServiceResolver _todoResolver;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _defaultWorkspacePath;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceServiceAccessor"/> class.</summary>
    public WorkspaceServiceAccessor(
        TodoServiceResolver todoResolver,
        IHttpContextAccessor httpContextAccessor,
        IOptions<IngestionOptions> ingestionOptions)
    {
        _todoResolver = todoResolver ?? throw new ArgumentNullException(nameof(todoResolver));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        ArgumentNullException.ThrowIfNull(ingestionOptions);
        _defaultWorkspacePath = Path.GetFullPath(ingestionOptions.Value.RepoRoot ?? ".");
    }

    /// <summary>
    /// Pushes an explicit workspace onto the current async flow and restores the previous
    /// value when the returned scope is disposed. Nested and parallel STDIO callers must use
    /// <c>using</c> so a later call cannot leak into an earlier one.
    /// </summary>
    /// <param name="workspacePath">Workspace root to bind for the duration of the scope.</param>
    /// <returns>A scope that restores the prior override on dispose.</returns>
    public IDisposable PushWorkspace(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            throw new ArgumentException("Workspace path is required.", nameof(workspacePath));

        var canonical = HandoffWorkspacePaths.Canonicalize(workspacePath);
        var previous = OverrideWorkspacePath.Value;
        OverrideWorkspacePath.Value = canonical;
        return new WorkspaceOverrideScope(() => OverrideWorkspacePath.Value = previous);
    }

    /// <summary>Resolves the workspace-specific <see cref="ITodoService"/> for the current HTTP request, or the primary singleton.</summary>
    public ITodoService GetTodoService()
    {
        var overridePath = OverrideWorkspacePath.Value;
        if (!string.IsNullOrWhiteSpace(overridePath))
            return _todoResolver.Resolve(new WorkspaceContext { WorkspacePath = overridePath });

        var ctx = GetWorkspaceContext();
        return _todoResolver.Resolve(ctx ?? new WorkspaceContext());
    }

    /// <summary>Returns the workspace root path for the current request, or the primary workspace path.</summary>
    public string GetWorkspacePath()
    {
        var raw = OverrideWorkspacePath.Value
            ?? GetWorkspaceContext()?.WorkspacePath
            ?? _defaultWorkspacePath;
        return HandoffWorkspacePaths.TryCanonicalize(raw, out var canonical, out _)
            ? canonical
            : _defaultWorkspacePath;
    }

    private WorkspaceContext? GetWorkspaceContext()
    {
        return _httpContextAccessor.HttpContext?.RequestServices.GetService<WorkspaceContext>();
    }

    private sealed class WorkspaceOverrideScope : IDisposable, IAsyncDisposable
    {
        private readonly Action _restore;
        private bool _disposed;

        public WorkspaceOverrideScope(Action restore) => _restore = restore;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _restore();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
