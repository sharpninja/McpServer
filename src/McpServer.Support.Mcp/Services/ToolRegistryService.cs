using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// CRUD and keyword search for tool definitions.
/// Keyword queries match against tags and return the union of global tools
/// plus tools scoped to the specified workspace.
/// </summary>
public sealed class ToolRegistryService : IToolRegistryService
{
    private readonly McpDbContext _db;
    private readonly ILogger<ToolRegistryService> _logger;

    /// <summary>Initializes a new instance of the <see cref="ToolRegistryService"/> class.</summary>
    public ToolRegistryService(McpDbContext db, ILogger<ToolRegistryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ToolSearchResult> SearchAsync(string keyword, string? workspacePath = null, CancellationToken ct = default)
    {
        var kw = (keyword ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(kw))
            return new ToolSearchResult([], 0);

        // Singularize/pluralize-tolerant: match if the tag starts with the keyword or vice-versa.
        var query = _db.ToolDefinitions
            .Include(t => t.Tags)
            .Where(t => t.Tags.Any(tag =>
                tag.Tag.Contains(kw) || kw.Contains(tag.Tag))
                || t.Name.Contains(kw)
                || t.Description.Contains(kw));

        // Scope: global + specified workspace.
        query = FilterScope(query, workspacePath);

        var entities = await query.OrderBy(t => t.Name).ToListAsync(ct).ConfigureAwait(false);
        var dtos = entities.Select(ToDto).ToList();
        return new ToolSearchResult(dtos, dtos.Count);
    }

    /// <inheritdoc />
    public async Task<ToolDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.ToolDefinitions.Include(t => t.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
        return entity is null ? null : ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<ToolSearchResult> ListAsync(string? workspacePath = null, CancellationToken ct = default)
    {
        var query = _db.ToolDefinitions.Include(t => t.Tags).AsNoTracking();
        query = FilterScope(query, workspacePath);

        var entities = await query.OrderBy(t => t.Name).ToListAsync(ct).ConfigureAwait(false);
        var dtos = entities.Select(ToDto).ToList();
        return new ToolSearchResult(dtos, dtos.Count);
    }

    /// <inheritdoc />
    public async Task<ToolMutationResult> CreateAsync(ToolCreateRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? "").Trim();
        if (string.IsNullOrEmpty(name))
            return new ToolMutationResult(false, "Tool name is required.");

        var wsPath = NormalizeWorkspacePath(request.WorkspacePath);

        // Unique name per scope.
        var exists = await _db.ToolDefinitions.AnyAsync(
            t => t.Name == name && t.WorkspacePath == wsPath, ct).ConfigureAwait(false);
        if (exists)
            return new ToolMutationResult(false, $"Tool '{name}' already exists in this scope.");

        var now = DateTimeOffset.UtcNow;
        var entity = new ToolDefinitionEntity
        {
            Name = name,
            Description = (request.Description ?? "").Trim(),
            ParameterSchema = request.ParameterSchema,
            CommandTemplate = request.CommandTemplate,
            WorkspacePath = wsPath,
            DateTimeCreated = now,
            DateTimeModified = now,
        };

        foreach (var tag in NormalizeTags(request.Tags))
            entity.Tags.Add(new ToolDefinitionTagEntity { Tag = tag });

        _db.ToolDefinitions.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Tool created: {Name} (scope: {Scope})", name, wsPath ?? "global");
        return new ToolMutationResult(true, Tool: ToDto(entity));
    }

    /// <inheritdoc />
    public async Task<ToolMutationResult> UpdateAsync(int id, ToolUpdateRequest request, CancellationToken ct = default)
    {
        var entity = await _db.ToolDefinitions.Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
        if (entity is null)
            return new ToolMutationResult(false, $"Tool {id} not found.");

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrEmpty(name))
                return new ToolMutationResult(false, "Tool name cannot be empty.");
            entity.Name = name;
        }

        if (request.Description is not null)
            entity.Description = request.Description.Trim();

        if (request.ParameterSchema is not null)
            entity.ParameterSchema = request.ParameterSchema;

        if (request.CommandTemplate is not null)
            entity.CommandTemplate = request.CommandTemplate;

        if (request.WorkspacePath is not null)
            entity.WorkspacePath = NormalizeWorkspacePath(request.WorkspacePath);

        if (request.Tags is not null)
        {
            entity.Tags.Clear();
            foreach (var tag in NormalizeTags(request.Tags))
                entity.Tags.Add(new ToolDefinitionTagEntity { Tag = tag });
        }

        entity.DateTimeModified = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Tool updated: {Name} (id: {Id})", entity.Name, entity.Id);
        return new ToolMutationResult(true, Tool: ToDto(entity));
    }

    /// <inheritdoc />
    public async Task<ToolMutationResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.ToolDefinitions.Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
        if (entity is null)
            return new ToolMutationResult(false, $"Tool {id} not found.");

        var dto = ToDto(entity);
        _db.ToolDefinitions.Remove(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Tool deleted: {Name} (id: {Id})", dto.Name, dto.Id);
        return new ToolMutationResult(true, Tool: dto);
    }

    private static IQueryable<ToolDefinitionEntity> FilterScope(IQueryable<ToolDefinitionEntity> query, string? workspacePath)
    {
        var ws = NormalizeWorkspacePath(workspacePath);
        return ws is null
            ? query.Where(t => t.WorkspacePath == null)
            : query.Where(t => t.WorkspacePath == null || t.WorkspacePath == ws);
    }

    private static string? NormalizeWorkspacePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        return Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string>? tags)
    {
        if (tags is null or { Count: 0 }) return [];
        return tags
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static ToolDto ToDto(ToolDefinitionEntity e) => new(
        e.Id,
        e.Name,
        e.Description,
        e.Tags.Select(t => t.Tag).OrderBy(t => t).ToList(),
        e.ParameterSchema,
        e.CommandTemplate,
        e.WorkspacePath,
        e.DateTimeCreated,
        e.DateTimeModified);
}
