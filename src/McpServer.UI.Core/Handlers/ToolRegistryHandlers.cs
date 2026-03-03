using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="ListToolsQuery"/>.</summary>
internal sealed class ListToolsQueryHandler : IQueryHandler<ListToolsQuery, ListToolsResult>
{
    private readonly IToolRegistryApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListToolsQueryHandler> _logger;

    public ListToolsQueryHandler(
        IToolRegistryApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ListToolsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<ListToolsResult>> HandleAsync(ListToolsQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ToolRegistryList))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ToolRegistryList);
            return Result<ListToolsResult>.Failure(ToolRegistryHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.ListToolsAsync(query, context.CancellationToken).ConfigureAwait(false);
            return Result<ListToolsResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ListToolsResult>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="SearchToolsQuery"/>.</summary>
internal sealed class SearchToolsQueryHandler : IQueryHandler<SearchToolsQuery, ListToolsResult>
{
    private readonly IToolRegistryApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<SearchToolsQueryHandler> _logger;

    public SearchToolsQueryHandler(
        IToolRegistryApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<SearchToolsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<ListToolsResult>> HandleAsync(SearchToolsQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ToolRegistrySearch))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ToolRegistrySearch);
            return Result<ListToolsResult>.Failure(ToolRegistryHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.SearchToolsAsync(query, context.CancellationToken).ConfigureAwait(false);
            return Result<ListToolsResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ListToolsResult>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="GetToolQuery"/>.</summary>
internal sealed class GetToolQueryHandler : IQueryHandler<GetToolQuery, ToolDetail?>
{
    private readonly IToolRegistryApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetToolQueryHandler> _logger;

    public GetToolQueryHandler(
        IToolRegistryApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetToolQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<ToolDetail?>> HandleAsync(GetToolQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ToolRegistryGet))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ToolRegistryGet);
            return Result<ToolDetail?>.Failure(ToolRegistryHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.GetToolAsync(query.ToolId, context.CancellationToken).ConfigureAwait(false);
            return Result<ToolDetail?>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ToolDetail?>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="CreateToolCommand"/>.</summary>
internal sealed class CreateToolCommandHandler : ICommandHandler<CreateToolCommand, ToolMutationOutcome>
{
    private readonly IToolRegistryApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CreateToolCommandHandler> _logger;

    public CreateToolCommandHandler(
        IToolRegistryApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<CreateToolCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<ToolMutationOutcome>> HandleAsync(CreateToolCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ToolRegistryMutate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ToolRegistryMutate);
            return Result<ToolMutationOutcome>.Failure(ToolRegistryHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.CreateToolAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<ToolMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ToolMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="UpdateToolCommand"/>.</summary>
internal sealed class UpdateToolCommandHandler : ICommandHandler<UpdateToolCommand, ToolMutationOutcome>
{
    private readonly IToolRegistryApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpdateToolCommandHandler> _logger;

    public UpdateToolCommandHandler(
        IToolRegistryApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<UpdateToolCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<ToolMutationOutcome>> HandleAsync(UpdateToolCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ToolRegistryMutate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ToolRegistryMutate);
            return Result<ToolMutationOutcome>.Failure(ToolRegistryHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.UpdateToolAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<ToolMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ToolMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="DeleteToolCommand"/>.</summary>
internal sealed class DeleteToolCommandHandler : ICommandHandler<DeleteToolCommand, ToolMutationOutcome>
{
    private readonly IToolRegistryApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<DeleteToolCommandHandler> _logger;

    public DeleteToolCommandHandler(
        IToolRegistryApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<DeleteToolCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<ToolMutationOutcome>> HandleAsync(DeleteToolCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ToolRegistryMutate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ToolRegistryMutate);
            return Result<ToolMutationOutcome>.Failure(ToolRegistryHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.DeleteToolAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<ToolMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ToolMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="ListBucketsQuery"/>.</summary>
internal sealed class ListBucketsQueryHandler : IQueryHandler<ListBucketsQuery, ListBucketsResult>
{
    private readonly IToolRegistryApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListBucketsQueryHandler> _logger;

    public ListBucketsQueryHandler(
        IToolRegistryApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ListBucketsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<ListBucketsResult>> HandleAsync(ListBucketsQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ToolRegistryBucketList))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ToolRegistryBucketList);
            return Result<ListBucketsResult>.Failure(ToolRegistryHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.ListBucketsAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<ListBucketsResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ListBucketsResult>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="AddBucketCommand"/>.</summary>
internal sealed class AddBucketCommandHandler : ICommandHandler<AddBucketCommand, BucketMutationOutcome>
{
    private readonly IToolRegistryApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<AddBucketCommandHandler> _logger;

    public AddBucketCommandHandler(
        IToolRegistryApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<AddBucketCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<BucketMutationOutcome>> HandleAsync(AddBucketCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ToolRegistryBucketMutate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ToolRegistryBucketMutate);
            return Result<BucketMutationOutcome>.Failure(ToolRegistryHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.AddBucketAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<BucketMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<BucketMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="RemoveBucketCommand"/>.</summary>
internal sealed class RemoveBucketCommandHandler : ICommandHandler<RemoveBucketCommand, BucketMutationOutcome>
{
    private readonly IToolRegistryApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<RemoveBucketCommandHandler> _logger;

    public RemoveBucketCommandHandler(
        IToolRegistryApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<RemoveBucketCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<BucketMutationOutcome>> HandleAsync(RemoveBucketCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ToolRegistryBucketMutate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ToolRegistryBucketMutate);
            return Result<BucketMutationOutcome>.Failure(ToolRegistryHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.RemoveBucketAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<BucketMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<BucketMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="BrowseBucketQuery"/>.</summary>
internal sealed class BrowseBucketQueryHandler : IQueryHandler<BrowseBucketQuery, BucketBrowseOutcome>
{
    private readonly IToolRegistryApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<BrowseBucketQueryHandler> _logger;

    public BrowseBucketQueryHandler(
        IToolRegistryApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<BrowseBucketQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<BucketBrowseOutcome>> HandleAsync(BrowseBucketQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ToolRegistryBucketBrowse))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ToolRegistryBucketBrowse);
            return Result<BucketBrowseOutcome>.Failure(ToolRegistryHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.BrowseBucketAsync(query, context.CancellationToken).ConfigureAwait(false);
            return Result<BucketBrowseOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<BucketBrowseOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="InstallFromBucketCommand"/>.</summary>
internal sealed class InstallFromBucketCommandHandler : ICommandHandler<InstallFromBucketCommand, ToolMutationOutcome>
{
    private readonly IToolRegistryApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<InstallFromBucketCommandHandler> _logger;

    public InstallFromBucketCommandHandler(
        IToolRegistryApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<InstallFromBucketCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<ToolMutationOutcome>> HandleAsync(InstallFromBucketCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ToolRegistryBucketMutate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ToolRegistryBucketMutate);
            return Result<ToolMutationOutcome>.Failure(ToolRegistryHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.InstallFromBucketAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<ToolMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ToolMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="SyncBucketCommand"/>.</summary>
internal sealed class SyncBucketCommandHandler : ICommandHandler<SyncBucketCommand, BucketSyncOutcome>
{
    private readonly IToolRegistryApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<SyncBucketCommandHandler> _logger;

    public SyncBucketCommandHandler(
        IToolRegistryApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<SyncBucketCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<BucketSyncOutcome>> HandleAsync(SyncBucketCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ToolRegistryBucketMutate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ToolRegistryBucketMutate);
            return Result<BucketSyncOutcome>.Failure(ToolRegistryHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.SyncBucketAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<BucketSyncOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<BucketSyncOutcome>.Failure(ex);
        }
    }
}

internal static class ToolRegistryHandlerHelpers
{
    public static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
