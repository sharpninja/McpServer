using System.Text.Json;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Manages tool buckets — GitHub repositories containing JSON tool manifests.
/// Reads manifest files from GitHub-backed bucket repositories, then installs
/// tool definitions into the local database (global or workspace-scoped).
/// </summary>
public sealed class ToolBucketService : IToolBucketService
{
    private static readonly HttpClient s_defaultHttpClient = CreateDefaultHttpClient();

    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly McpDbContext _db;
    private readonly IChangeEventBus? _eventBus;
    private readonly IProcessRunner _processRunner;
    private readonly IToolRegistryService _toolRegistry;
    private readonly ILogger<ToolBucketService> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>Initializes a new instance of the <see cref="ToolBucketService"/> class.</summary>
    public ToolBucketService(
        McpDbContext db,
        IProcessRunner processRunner,
        IToolRegistryService toolRegistry,
        ILogger<ToolBucketService> logger,
        IChangeEventBus? eventBus = null,
        HttpClient? httpClient = null)
    {
        _db = db;
        _eventBus = eventBus;
        _processRunner = processRunner;
        _toolRegistry = toolRegistry;
        _logger = logger;
        _httpClient = httpClient ?? s_defaultHttpClient;
        EnsureGitHubHeaders(_httpClient);
    }

    /// <inheritdoc />
    public async Task<BucketListResult> ListBucketsAsync(CancellationToken ct = default)
    {
        var entities = await GetVisibleBucketsQuery()
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var dtos = entities.Select(ToDto).ToList();
        return new BucketListResult(dtos, dtos.Count);
    }

    /// <inheritdoc />
    public async Task<BucketMutationResult> AddBucketAsync(BucketAddRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(name))
            return new BucketMutationResult(false, "Bucket name is required.");

        var exists = await _db.ToolBuckets
            .IgnoreQueryFilters()
            .AnyAsync(b => b.Name == name, ct)
            .ConfigureAwait(false);
        if (exists)
            return new BucketMutationResult(false, $"Bucket '{name}' already exists.");

        var entity = new ToolBucketEntity
        {
            Name = name,
            Owner = (request.Owner ?? "").Trim(),
            Repo = (request.Repo ?? "").Trim(),
            Branch = string.IsNullOrWhiteSpace(request.Branch) ? "main" : request.Branch.Trim(),
            ManifestPath = string.IsNullOrWhiteSpace(request.ManifestPath) ? "/" : request.ManifestPath.Trim(),
            DateTimeCreated = DateTimeOffset.UtcNow,
        };

        if (string.IsNullOrEmpty(entity.Owner) || string.IsNullOrEmpty(entity.Repo))
            return new BucketMutationResult(false, "Owner and Repo are required.");

        _db.ToolBuckets.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await PublishChangeSafeAsync(ChangeEventActions.Created, name, ct).ConfigureAwait(false);

        _logger.LogInformation("Bucket added: {Name} ({Owner}/{Repo})", name, entity.Owner, entity.Repo);
        return new BucketMutationResult(true, Bucket: ToDto(entity));
    }

    /// <inheritdoc />
    public async Task<BucketMutationResult> RemoveBucketAsync(string bucketName, bool uninstallTools = false, CancellationToken ct = default)
    {
        var name = (bucketName ?? "").Trim().ToLowerInvariant();
        var entity = await GetVisibleBucketsQuery().FirstOrDefaultAsync(b => b.Name == name, ct).ConfigureAwait(false);
        if (entity is null)
            return new BucketMutationResult(false, $"Bucket '{name}' not found.");

        if (uninstallTools)
        {
            var tools = await _db.ToolDefinitions.Where(t => t.BucketName == name).ToListAsync(ct).ConfigureAwait(false);
            _db.ToolDefinitions.RemoveRange(tools);
            _logger.LogInformation("Uninstalled {Count} tools from bucket '{Name}'", tools.Count, name);
            foreach (var tool in tools)
            {
                await PublishToolRegistryChangeSafeAsync(ChangeEventActions.Deleted, tool.Id.ToString(), ct).ConfigureAwait(false);
            }
        }

        var dto = ToDto(entity);
        _db.ToolBuckets.Remove(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await PublishChangeSafeAsync(ChangeEventActions.Deleted, name, ct).ConfigureAwait(false);

        _logger.LogInformation("Bucket removed: {Name}", name);
        return new BucketMutationResult(true, Bucket: dto);
    }

    /// <inheritdoc />
    public async Task<BucketBrowseResult> BrowseAsync(string bucketName, CancellationToken ct = default)
    {
        var name = (bucketName ?? "").Trim().ToLowerInvariant();
        var bucket = await GetVisibleBucketsQuery().AsNoTracking().FirstOrDefaultAsync(b => b.Name == name, ct).ConfigureAwait(false);
        if (bucket is null)
            return new BucketBrowseResult(false, $"Bucket '{name}' not found.");

        var manifests = await FetchManifestsAsync(bucket, ct).ConfigureAwait(false);
        if (manifests is null)
            return new BucketBrowseResult(false, $"Failed to read manifests from {bucket.Owner}/{bucket.Repo}.");

        return new BucketBrowseResult(true, Tools: manifests);
    }

    /// <inheritdoc />
    public async Task<ToolMutationResult> InstallAsync(string bucketName, string toolName, string? workspacePath = null, CancellationToken ct = default)
    {
        var name = (bucketName ?? "").Trim().ToLowerInvariant();
        var bucket = await GetVisibleBucketsQuery().AsNoTracking().FirstOrDefaultAsync(b => b.Name == name, ct).ConfigureAwait(false);
        if (bucket is null)
            return new ToolMutationResult(false, $"Bucket '{name}' not found.");

        var manifests = await FetchManifestsAsync(bucket, ct).ConfigureAwait(false);
        if (manifests is null)
            return new ToolMutationResult(false, $"Failed to read manifests from {bucket.Owner}/{bucket.Repo}.");

        var manifest = manifests.FirstOrDefault(m => string.Equals(m.Name, toolName, StringComparison.OrdinalIgnoreCase));
        if (manifest is null)
            return new ToolMutationResult(false, $"Tool '{toolName}' not found in bucket '{name}'.");

        // Create via the registry service (handles uniqueness, normalization).
        var result = await _toolRegistry.CreateAsync(new ToolCreateRequest(
            manifest.Name,
            manifest.Description,
            manifest.Tags,
            manifest.ParameterSchema,
            manifest.CommandTemplate,
            workspacePath), ct).ConfigureAwait(false);

        if (!result.Success)
            return result;

        // Stamp the bucket provenance.
        var entity = await _db.ToolDefinitions.FirstOrDefaultAsync(t => t.Id == result.Tool!.Id, ct).ConfigureAwait(false);
        if (entity is not null)
        {
            entity.BucketName = name;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        _logger.LogInformation("Installed tool '{Tool}' from bucket '{Bucket}'", manifest.Name, name);
        return result;
    }

    /// <inheritdoc />
    public async Task<BucketSyncResult> SyncAsync(string bucketName, CancellationToken ct = default)
    {
        var name = (bucketName ?? "").Trim().ToLowerInvariant();
        var bucket = await GetVisibleBucketsQuery().FirstOrDefaultAsync(b => b.Name == name, ct).ConfigureAwait(false);
        if (bucket is null)
            return new BucketSyncResult(false, $"Bucket '{name}' not found.");

        var manifests = await FetchManifestsAsync(bucket, ct).ConfigureAwait(false);
        if (manifests is null)
            return new BucketSyncResult(false, $"Failed to read manifests from {bucket.Owner}/{bucket.Repo}.");

        var installed = await _db.ToolDefinitions
            .Include(t => t.Tags)
            .Where(t => t.BucketName == name)
            .ToListAsync(ct).ConfigureAwait(false);

        int updated = 0, unchanged = 0;
        var manifestByName = manifests.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var tool in installed)
        {
            if (!manifestByName.TryGetValue(tool.Name, out var manifest))
            {
                unchanged++;
                continue;
            }

            var changed = false;
            if (tool.Description != manifest.Description) { tool.Description = manifest.Description; changed = true; }
            if (tool.ParameterSchema != manifest.ParameterSchema) { tool.ParameterSchema = manifest.ParameterSchema; changed = true; }
            if (tool.CommandTemplate != manifest.CommandTemplate) { tool.CommandTemplate = manifest.CommandTemplate; changed = true; }

            var currentTags = tool.Tags.Select(t => t.Tag).OrderBy(t => t).ToList();
            var newTags = manifest.Tags.Select(t => t.Trim().ToLowerInvariant()).OrderBy(t => t).Distinct().ToList();
            if (!currentTags.SequenceEqual(newTags))
            {
                tool.Tags.Clear();
                foreach (var tag in newTags)
                    tool.Tags.Add(new ToolDefinitionTagEntity { Tag = tag });
                changed = true;
            }

            if (changed)
            {
                tool.DateTimeModified = DateTimeOffset.UtcNow;
                updated++;
            }
            else
            {
                unchanged++;
            }
        }

        bucket.DateTimeLastSynced = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await PublishChangeSafeAsync(ChangeEventActions.Updated, name, ct).ConfigureAwait(false);

        _logger.LogInformation("Bucket sync '{Name}': {Updated} updated, {Unchanged} unchanged", name, updated, unchanged);
        return new BucketSyncResult(true, Updated: updated, Unchanged: unchanged);
    }

    /// <summary>Fetches all .json manifests from the bucket repo.</summary>
    private async Task<IReadOnlyList<ToolManifest>?> FetchManifestsAsync(ToolBucketEntity bucket, CancellationToken ct)
    {
        var cliManifests = await FetchManifestsWithGitHubCliAsync(bucket, ct).ConfigureAwait(false);
        if (cliManifests is not null)
            return cliManifests;

        return await FetchManifestsWithHttpAsync(bucket, ct).ConfigureAwait(false);
    }

    /// <summary>Fetches all .json manifests from the bucket repo using <c>gh api</c>.</summary>
    private async Task<IReadOnlyList<ToolManifest>?> FetchManifestsWithGitHubCliAsync(ToolBucketEntity bucket, CancellationToken ct)
    {
        // List directory contents via GitHub REST API.
        var apiPath = $"/repos/{bucket.Owner}/{bucket.Repo}/contents{NormApiPath(bucket.ManifestPath)}?ref={bucket.Branch}";
        var listResult = await _processRunner.RunAsync("gh", $"api \"{apiPath}\"", ct).ConfigureAwait(false);
        if (listResult.ExitCode != 0 || string.IsNullOrWhiteSpace(listResult.Stdout))
        {
            _logger.LogWarning("Failed to list bucket contents: {Stderr}", listResult.Stderr);
            return null;
        }

        JsonElement[] files;
        try
        {
            files = JsonSerializer.Deserialize<JsonElement[]>(listResult.Stdout, s_jsonOpts) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse directory listing for bucket {Name}", bucket.Name);
            return null;
        }

        return await BuildManifestsAsync(
            files,
            async (downloadUrl, fileName) =>
            {
                // Fetch individual manifest via raw download URL.
                var fileResult = await _processRunner.RunAsync("gh", $"api \"{downloadUrl}\"", ct).ConfigureAwait(false);
                if (fileResult.ExitCode != 0 || string.IsNullOrWhiteSpace(fileResult.Stdout))
                {
                    _logger.LogWarning("Failed to fetch bucket manifest {File}: {Stderr}", fileName, fileResult.Stderr);
                    return null;
                }

                return fileResult.Stdout;
            }).ConfigureAwait(false);
    }

    /// <summary>Fetches all .json manifests from the bucket repo using unauthenticated GitHub HTTP APIs.</summary>
    private async Task<IReadOnlyList<ToolManifest>?> FetchManifestsWithHttpAsync(ToolBucketEntity bucket, CancellationToken ct)
    {
        var apiPath = $"https://api.github.com/repos/{Uri.EscapeDataString(bucket.Owner)}/{Uri.EscapeDataString(bucket.Repo)}/contents{NormApiPath(bucket.ManifestPath)}?ref={Uri.EscapeDataString(bucket.Branch)}";
        using var response = await _httpClient.GetAsync(apiPath, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning(
                "Failed to list bucket contents via GitHub HTTP: {StatusCode}; {Body}",
                (int)response.StatusCode,
                Truncate(body));
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        JsonElement[] files;
        try
        {
            files = JsonSerializer.Deserialize<JsonElement[]>(json, s_jsonOpts) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse HTTP directory listing for bucket {Name}", bucket.Name);
            return null;
        }

        return await BuildManifestsAsync(
            files,
            async (downloadUrl, fileName) =>
            {
                try
                {
                    return await _httpClient.GetStringAsync(downloadUrl, ct).ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch bucket manifest {File} via HTTP", fileName);
                    return null;
                }
            }).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ToolManifest>> BuildManifestsAsync(
        JsonElement[] files,
        Func<string, string, Task<string?>> fetchContentAsync)
    {
        var manifests = new List<ToolManifest>();
        foreach (var file in files)
        {
            var fileName = file.GetProperty("name").GetString() ?? "";
            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            var downloadUrl = file.TryGetProperty("download_url", out var urlProp) ? urlProp.GetString() : null;
            if (string.IsNullOrEmpty(downloadUrl))
                continue;

            var manifestJson = await fetchContentAsync(downloadUrl, fileName).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(manifestJson))
                continue;

            try
            {
                var manifest = JsonSerializer.Deserialize<ToolManifestFile>(manifestJson, s_jsonOpts);
                if (manifest is not null && !string.IsNullOrWhiteSpace(manifest.Name))
                {
                    manifests.Add(new ToolManifest(
                        manifest.Name.Trim(),
                        (manifest.Description ?? "").Trim(),
                        (manifest.Tags ?? []).Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).Distinct().ToList(),
                        manifest.ParameterSchema,
                        manifest.CommandTemplate,
                        fileName));
                }
            }
            catch (JsonException)
            {
                _logger.LogWarning("Skipping invalid manifest: {File}", fileName);
            }
        }

        return manifests;
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        var client = new HttpClient();
        EnsureGitHubHeaders(client);
        return client;
    }

    private static void EnsureGitHubHeaders(HttpClient client)
    {
        if (!client.DefaultRequestHeaders.UserAgent.Any())
            client.DefaultRequestHeaders.UserAgent.ParseAdd("McpServer-ToolBucketService/1.0");

        if (!client.DefaultRequestHeaders.Accept.Any())
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    private static string Truncate(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= 500 ? value : value[..500];
    }

    private IQueryable<ToolBucketEntity> GetVisibleBucketsQuery()
    {
        var workspaceId = _db.CurrentWorkspaceId;
        var query = _db.ToolBuckets.IgnoreQueryFilters();
        if (string.IsNullOrWhiteSpace(workspaceId))
            return query.Where(b => b.WorkspaceId == string.Empty);

        // Default buckets are seeded without a workspace and must remain visible to every workspace.
        return query.Where(b => b.WorkspaceId == string.Empty || b.WorkspaceId == workspaceId);
    }

    private static string NormApiPath(string path)
    {
        var p = (path ?? "/").Trim().Replace('\\', '/');
        if (!p.StartsWith('/')) p = "/" + p;
        return p.TrimEnd('/');
    }

    private static BucketDto ToDto(ToolBucketEntity e) => new(
        e.Id, e.Name, e.Owner, e.Repo, e.Branch, e.ManifestPath,
        e.DateTimeCreated, e.DateTimeLastSynced);

    /// <summary>Internal deserialization model for JSON manifest files in a bucket repo.</summary>
    private sealed class ToolManifestFile
    {
        /// <summary>Tool name.</summary>
        public string? Name { get; set; }

        /// <summary>Tool description.</summary>
        public string? Description { get; set; }

        /// <summary>Keyword tags.</summary>
        public List<string>? Tags { get; set; }

        /// <summary>JSON schema for parameters.</summary>
        public string? ParameterSchema { get; set; }

        /// <summary>Command template.</summary>
        public string? CommandTemplate { get; set; }
    }

    private async Task PublishChangeSafeAsync(string action, string entityId, CancellationToken ct)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.ToolBucket,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = $"mcp://workspace/tool_bucket/{entityId}",
                },
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing tool bucket change event for {EntityId}", entityId);
        }
    }

    private async Task PublishToolRegistryChangeSafeAsync(string action, string entityId, CancellationToken ct)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.ToolRegistry,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = $"mcp://workspace/tool_registry/{entityId}",
                },
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing tool registry change event for {EntityId}", entityId);
        }
    }
}
