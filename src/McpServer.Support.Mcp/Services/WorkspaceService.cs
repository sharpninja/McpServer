using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-009 / TR-MCP-WS-002: Workspace CRUD, auto-port assignment, and init scaffolding.
/// </summary>
public sealed class WorkspaceService : IWorkspaceService
{
    private const int BaseAutoPort = 7148;
    private const string DefaultTodoPath = "docs/todo.yaml";

    private readonly McpDbContext _db;
    private readonly ILogger<WorkspaceService> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceService"/> class.</summary>
    public WorkspaceService(McpDbContext db, ILogger<WorkspaceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WorkspaceListResult> ListAsync(CancellationToken ct = default)
    {
        var entities = await _db.Workspaces.AsNoTracking().OrderBy(w => w.Name).ToListAsync(ct).ConfigureAwait(false);
        var items = entities.Select(ToDto).ToList();
        return new WorkspaceListResult(items, items.Count);
    }

    /// <inheritdoc />
    public async Task<WorkspaceDto?> GetAsync(string workspacePath, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        var entity = await _db.Workspaces.AsNoTracking().FirstOrDefaultAsync(w => w.WorkspacePath == normalized, ct).ConfigureAwait(false);
        return entity is null ? null : ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<WorkspaceMutationResult> CreateAsync(WorkspaceCreateRequest request, CancellationToken ct = default)
    {
        var normalized = NormalizePath(request.WorkspacePath);

        if (string.IsNullOrWhiteSpace(normalized))
            return new WorkspaceMutationResult(false, "WorkspacePath is required.");

        if (!Path.IsPathRooted(normalized))
            return new WorkspaceMutationResult(false, "WorkspacePath must be an absolute path.");

        var existing = await _db.Workspaces.AsNoTracking().FirstOrDefaultAsync(w => w.WorkspacePath == normalized, ct).ConfigureAwait(false);
        if (existing is not null)
            return new WorkspaceMutationResult(false, $"Workspace already registered: {normalized}");

        var port = request.WorkspacePort;
        if (port <= 0)
            port = await GetNextAvailablePortAsync(ct).ConfigureAwait(false);
        else
        {
            var portTaken = await _db.Workspaces.AsNoTracking().AnyAsync(w => w.WorkspacePort == port, ct).ConfigureAwait(false);
            if (portTaken)
                return new WorkspaceMutationResult(false, $"Port {port} is already in use by another workspace.");
        }

        var name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name.Trim() : DeriveNameFromPath(normalized);
        var todoPath = !string.IsNullOrWhiteSpace(request.TodoPath) ? request.TodoPath.Trim() : DefaultTodoPath;
        var now = DateTimeOffset.UtcNow;

        var entity = new WorkspaceEntity
        {
            WorkspacePath = normalized,
            Name = name,
            TodoPath = todoPath,
            WorkspacePort = port,
            TunnelProvider = string.IsNullOrWhiteSpace(request.TunnelProvider) ? null : request.TunnelProvider.Trim(),
            RunAs = string.IsNullOrWhiteSpace(request.RunAs) ? null : request.RunAs.Trim(),
            DateTimeCreated = now,
            DateTimeModified = now,
        };

        _db.Workspaces.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Workspace created: {Name} at {Path} on port {Port}", entity.Name, entity.WorkspacePath, entity.WorkspacePort);
        return new WorkspaceMutationResult(true, Workspace: ToDto(entity));
    }

    /// <inheritdoc />
    public async Task<WorkspaceMutationResult> UpdateAsync(string workspacePath, WorkspaceUpdateRequest request, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        var entity = await _db.Workspaces.FirstOrDefaultAsync(w => w.WorkspacePath == normalized, ct).ConfigureAwait(false);
        if (entity is null)
            return new WorkspaceMutationResult(false, $"Workspace not found: {normalized}");

        if (request.Name is not null)
            entity.Name = string.IsNullOrWhiteSpace(request.Name) ? DeriveNameFromPath(normalized) : request.Name.Trim();

        if (request.TodoPath is not null)
            entity.TodoPath = string.IsNullOrWhiteSpace(request.TodoPath) ? DefaultTodoPath : request.TodoPath.Trim();

        if (request.WorkspacePort is not null)
        {
            var newPort = request.WorkspacePort.Value;
            if (newPort <= 0)
                newPort = await GetNextAvailablePortAsync(ct).ConfigureAwait(false);
            else if (newPort != entity.WorkspacePort)
            {
                var portTaken = await _db.Workspaces.AsNoTracking().AnyAsync(w => w.WorkspacePort == newPort && w.WorkspacePath != normalized, ct).ConfigureAwait(false);
                if (portTaken)
                    return new WorkspaceMutationResult(false, $"Port {newPort} is already in use by another workspace.");
            }
            entity.WorkspacePort = newPort;
        }

        if (request.TunnelProvider is not null)
            entity.TunnelProvider = string.IsNullOrWhiteSpace(request.TunnelProvider) ? null : request.TunnelProvider.Trim();

        if (request.RunAs is not null)
            entity.RunAs = string.IsNullOrWhiteSpace(request.RunAs) ? null : request.RunAs.Trim();

        entity.DateTimeModified = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Workspace updated: {Name} at {Path}", entity.Name, entity.WorkspacePath);
        return new WorkspaceMutationResult(true, Workspace: ToDto(entity));
    }

    /// <inheritdoc />
    public async Task<WorkspaceMutationResult> DeleteAsync(string workspacePath, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        var entity = await _db.Workspaces.FirstOrDefaultAsync(w => w.WorkspacePath == normalized, ct).ConfigureAwait(false);
        if (entity is null)
            return new WorkspaceMutationResult(false, $"Workspace not found: {normalized}");

        var dto = ToDto(entity);
        _db.Workspaces.Remove(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Workspace deleted: {Name} at {Path}", dto.Name, dto.WorkspacePath);
        return new WorkspaceMutationResult(true, Workspace: dto);
    }

    /// <inheritdoc />
    public async Task<WorkspaceInitResult> InitAsync(string workspacePath, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        var entity = await _db.Workspaces.AsNoTracking().FirstOrDefaultAsync(w => w.WorkspacePath == normalized, ct).ConfigureAwait(false);
        if (entity is null)
            return new WorkspaceInitResult(false, $"Workspace not found: {normalized}");

        var filesCreated = new List<string>();

        try
        {
            // Ensure workspace root exists.
            if (!Directory.Exists(normalized))
            {
                Directory.CreateDirectory(normalized);
                filesCreated.Add(normalized);
            }

            // Scaffold todo.yaml.
            var todoFullPath = Path.GetFullPath(Path.Combine(normalized, entity.TodoPath));
            var todoDir = Path.GetDirectoryName(todoFullPath);
            if (!string.IsNullOrEmpty(todoDir) && !Directory.Exists(todoDir))
            {
                Directory.CreateDirectory(todoDir);
                filesCreated.Add(todoDir);
            }
            if (!File.Exists(todoFullPath))
            {
                await File.WriteAllTextAsync(todoFullPath, "# TODO items for this workspace\n", ct).ConfigureAwait(false);
                filesCreated.Add(todoFullPath);
            }

            // Scaffold empty mcp.db (touch file).
            var dbPath = Path.Combine(normalized, "mcp.db");
            if (!File.Exists(dbPath))
            {
                await File.WriteAllBytesAsync(dbPath, [], ct).ConfigureAwait(false);
                filesCreated.Add(dbPath);
            }

            _logger.LogInformation("Workspace initialized: {Path}, {Count} files created", normalized, filesCreated.Count);
            return new WorkspaceInitResult(true, FilesCreated: filesCreated);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to initialize workspace: {Path}", normalized);
            return new WorkspaceInitResult(false, ex.Message, filesCreated);
        }
    }

    private async Task<int> GetNextAvailablePortAsync(CancellationToken ct)
    {
        var maxPort = await _db.Workspaces.AsNoTracking().MaxAsync(w => (int?)w.WorkspacePort, ct).ConfigureAwait(false);
        return maxPort.HasValue && maxPort.Value >= BaseAutoPort ? maxPort.Value + 1 : BaseAutoPort;
    }

    private static string DeriveNameFromPath(string path)
    {
        var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(name) ? "workspace" : name;
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private static WorkspaceDto ToDto(WorkspaceEntity e) => new()
    {
        WorkspacePath = e.WorkspacePath,
        Name = e.Name,
        TodoPath = e.TodoPath,
        WorkspacePort = e.WorkspacePort,
        TunnelProvider = e.TunnelProvider,
        DateTimeCreated = e.DateTimeCreated,
        DateTimeModified = e.DateTimeModified,
        RunAs = e.RunAs,
    };
}
