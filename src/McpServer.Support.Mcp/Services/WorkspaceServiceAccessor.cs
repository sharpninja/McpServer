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

    /// <summary>Resolves the workspace-specific <see cref="ITodoService"/> for the current HTTP request, or the primary singleton.</summary>
    public ITodoService GetTodoService()
    {
        var ctx = GetWorkspaceContext();
        return _todoResolver.Resolve(ctx ?? new WorkspaceContext());
    }

    /// <summary>Returns the workspace root path for the current request, or the primary workspace path.</summary>
    public string GetWorkspacePath()
    {
        return GetWorkspaceContext()?.WorkspacePath ?? _defaultWorkspacePath;
    }

    private WorkspaceContext? GetWorkspaceContext()
    {
        return _httpContextAccessor.HttpContext?.RequestServices.GetService<WorkspaceContext>();
    }
}
