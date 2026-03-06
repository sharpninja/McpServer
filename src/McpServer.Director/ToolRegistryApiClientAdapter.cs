using McpServer.Client;
using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="IToolRegistryApiClient"/> backed by <see cref="McpServerClient"/>.
/// </summary>
internal sealed class ToolRegistryApiClientAdapter : IToolRegistryApiClient
{
    private readonly DirectorMcpContext _context;
    private readonly ILogger<ToolRegistryApiClientAdapter> _logger;

    public ToolRegistryApiClientAdapter(
        DirectorMcpContext context,
        ILogger<ToolRegistryApiClientAdapter>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<ToolRegistryApiClientAdapter>.Instance;
    }

    public async Task<ListToolsResult> ListToolsAsync(ListToolsQuery query, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Tools.ListAsync(query.WorkspacePath, cancellationToken).ConfigureAwait(true);
        return new ListToolsResult(result.Tools.Select(MapToolListItem).ToList(), result.TotalCount);
    }

    public async Task<ListToolsResult> SearchToolsAsync(SearchToolsQuery query, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Tools.SearchAsync(query.Keyword, query.WorkspacePath, cancellationToken).ConfigureAwait(true);
        return new ListToolsResult(result.Tools.Select(MapToolListItem).ToList(), result.TotalCount);
    }

    public async Task<ToolDetail?> GetToolAsync(int toolId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
            var result = await client.Tools.GetAsync(toolId, cancellationToken).ConfigureAwait(true);
            return MapToolDetail(result);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<ToolMutationOutcome> CreateToolAsync(CreateToolCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Tools.CreateAsync(new ToolCreateRequest
        {
            Name = command.Name,
            Description = command.Description,
            Tags = command.Tags,
            ParameterSchema = command.ParameterSchema,
            CommandTemplate = command.CommandTemplate,
            WorkspacePath = command.WorkspacePath
        }, cancellationToken).ConfigureAwait(true);

        return new ToolMutationOutcome(result.Success, result.Error, result.Tool is null ? null : MapToolDetail(result.Tool));
    }

    public async Task<ToolMutationOutcome> UpdateToolAsync(UpdateToolCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Tools.UpdateAsync(command.ToolId, new ToolUpdateRequest
        {
            Name = command.Name,
            Description = command.Description,
            Tags = command.Tags,
            ParameterSchema = command.ParameterSchema,
            CommandTemplate = command.CommandTemplate,
            WorkspacePath = command.WorkspacePath
        }, cancellationToken).ConfigureAwait(true);

        return new ToolMutationOutcome(result.Success, result.Error, result.Tool is null ? null : MapToolDetail(result.Tool));
    }

    public async Task<ToolMutationOutcome> DeleteToolAsync(DeleteToolCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Tools.DeleteAsync(command.ToolId, cancellationToken).ConfigureAwait(true);
        return new ToolMutationOutcome(result.Success, result.Error, result.Tool is null ? null : MapToolDetail(result.Tool));
    }

    public async Task<ListBucketsResult> ListBucketsAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Tools.ListBucketsAsync(cancellationToken).ConfigureAwait(true);
        return new ListBucketsResult(result.Buckets.Select(MapBucketDetail).ToList(), result.TotalCount);
    }

    public async Task<BucketMutationOutcome> AddBucketAsync(AddBucketCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Tools.AddBucketAsync(new BucketAddRequest
        {
            Name = command.Name,
            Owner = command.Owner,
            Repo = command.Repo,
            Branch = command.Branch,
            ManifestPath = command.ManifestPath
        }, cancellationToken).ConfigureAwait(true);

        return new BucketMutationOutcome(result.Success, result.Error, result.Bucket is null ? null : MapBucketDetail(result.Bucket));
    }

    public async Task<BucketMutationOutcome> RemoveBucketAsync(RemoveBucketCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Tools.DeleteBucketAsync(command.Name, command.UninstallTools, cancellationToken).ConfigureAwait(true);
        return new BucketMutationOutcome(result.Success, result.Error, result.Bucket is null ? null : MapBucketDetail(result.Bucket));
    }

    public async Task<BucketBrowseOutcome> BrowseBucketAsync(BrowseBucketQuery query, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Tools.BrowseBucketAsync(query.Name, cancellationToken).ConfigureAwait(true);
        return new BucketBrowseOutcome(
            result.Success,
            result.Error,
            result.Tools?.Select(t => new BucketToolManifest(
                t.Name,
                t.Description,
                t.Tags,
                t.ParameterSchema,
                t.CommandTemplate,
                t.ManifestFile)).ToList() ?? []);
    }

    public async Task<ToolMutationOutcome> InstallFromBucketAsync(InstallFromBucketCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Tools.InstallFromBucketAsync(command.BucketName, command.ToolName, command.WorkspacePath, cancellationToken).ConfigureAwait(true);
        return new ToolMutationOutcome(result.Success, result.Error, result.Tool is null ? null : MapToolDetail(result.Tool));
    }

    public async Task<BucketSyncOutcome> SyncBucketAsync(SyncBucketCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Tools.SyncBucketAsync(command.Name, cancellationToken).ConfigureAwait(true);
        return new BucketSyncOutcome(result.Success, result.Error, result.Updated, result.Added, result.Unchanged);
    }

    private async Task<McpServerClient> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_context.HasControlConnection)
            return await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(true);
        return await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(true);
    }

    private static ToolListItem MapToolListItem(ToolDto dto)
        => new(dto.Id, dto.Name, dto.Description, dto.Tags, dto.WorkspacePath);

    private static ToolDetail MapToolDetail(ToolDto dto)
        => new(
            dto.Id,
            dto.Name,
            dto.Description,
            dto.Tags,
            dto.ParameterSchema,
            dto.CommandTemplate,
            dto.WorkspacePath,
            dto.DateTimeCreated,
            dto.DateTimeModified);

    private static BucketDetail MapBucketDetail(BucketDto dto)
        => new(
            dto.Id,
            dto.Name,
            dto.Owner,
            dto.Repo,
            dto.Branch,
            dto.ManifestPath,
            dto.DateTimeCreated,
            dto.DateTimeLastSynced);
}
