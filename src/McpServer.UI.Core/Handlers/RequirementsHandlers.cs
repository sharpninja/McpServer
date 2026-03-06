using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="ListFunctionalRequirementsQuery"/>.</summary>
internal sealed class ListFunctionalRequirementsQueryHandler : IQueryHandler<ListFunctionalRequirementsQuery, FunctionalRequirementListResult>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListFunctionalRequirementsQueryHandler> _logger;

    public ListFunctionalRequirementsQueryHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<ListFunctionalRequirementsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<FunctionalRequirementListResult>> HandleAsync(ListFunctionalRequirementsQuery query, CallContext context)
        => await RequirementsHandlerHelpers.ReadAsync(
            _authorizationPolicy,
            _logger,
            () => _client.ListFunctionalRequirementsAsync(context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="GetFunctionalRequirementQuery"/>.</summary>
internal sealed class GetFunctionalRequirementQueryHandler : IQueryHandler<GetFunctionalRequirementQuery, FunctionalRequirementItem?>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetFunctionalRequirementQueryHandler> _logger;

    public GetFunctionalRequirementQueryHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<GetFunctionalRequirementQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<FunctionalRequirementItem?>> HandleAsync(GetFunctionalRequirementQuery query, CallContext context)
        => await RequirementsHandlerHelpers.ReadAsync(
            _authorizationPolicy,
            _logger,
            () => _client.GetFunctionalRequirementAsync(query.Id, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="CreateFunctionalRequirementCommand"/>.</summary>
internal sealed class CreateFunctionalRequirementCommandHandler : ICommandHandler<CreateFunctionalRequirementCommand, FunctionalRequirementItem>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CreateFunctionalRequirementCommandHandler> _logger;

    public CreateFunctionalRequirementCommandHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<CreateFunctionalRequirementCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<FunctionalRequirementItem>> HandleAsync(CreateFunctionalRequirementCommand command, CallContext context)
        => await RequirementsHandlerHelpers.WriteAsync(
            _authorizationPolicy,
            _logger,
            () => _client.CreateFunctionalRequirementAsync(command, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="UpdateFunctionalRequirementCommand"/>.</summary>
internal sealed class UpdateFunctionalRequirementCommandHandler : ICommandHandler<UpdateFunctionalRequirementCommand, FunctionalRequirementItem>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpdateFunctionalRequirementCommandHandler> _logger;

    public UpdateFunctionalRequirementCommandHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<UpdateFunctionalRequirementCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<FunctionalRequirementItem>> HandleAsync(UpdateFunctionalRequirementCommand command, CallContext context)
        => await RequirementsHandlerHelpers.WriteAsync(
            _authorizationPolicy,
            _logger,
            () => _client.UpdateFunctionalRequirementAsync(command, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="DeleteFunctionalRequirementCommand"/>.</summary>
internal sealed class DeleteFunctionalRequirementCommandHandler : ICommandHandler<DeleteFunctionalRequirementCommand, RequirementsMutationOutcome>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<DeleteFunctionalRequirementCommandHandler> _logger;

    public DeleteFunctionalRequirementCommandHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<DeleteFunctionalRequirementCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<RequirementsMutationOutcome>> HandleAsync(DeleteFunctionalRequirementCommand command, CallContext context)
        => await RequirementsHandlerHelpers.WriteAsync(
            _authorizationPolicy,
            _logger,
            () => _client.DeleteFunctionalRequirementAsync(command, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="ListTechnicalRequirementsQuery"/>.</summary>
internal sealed class ListTechnicalRequirementsQueryHandler : IQueryHandler<ListTechnicalRequirementsQuery, TechnicalRequirementListResult>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListTechnicalRequirementsQueryHandler> _logger;

    public ListTechnicalRequirementsQueryHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<ListTechnicalRequirementsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TechnicalRequirementListResult>> HandleAsync(ListTechnicalRequirementsQuery query, CallContext context)
        => await RequirementsHandlerHelpers.ReadAsync(
            _authorizationPolicy,
            _logger,
            () => _client.ListTechnicalRequirementsAsync(context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="GetTechnicalRequirementQuery"/>.</summary>
internal sealed class GetTechnicalRequirementQueryHandler : IQueryHandler<GetTechnicalRequirementQuery, TechnicalRequirementItem?>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetTechnicalRequirementQueryHandler> _logger;

    public GetTechnicalRequirementQueryHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<GetTechnicalRequirementQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TechnicalRequirementItem?>> HandleAsync(GetTechnicalRequirementQuery query, CallContext context)
        => await RequirementsHandlerHelpers.ReadAsync(
            _authorizationPolicy,
            _logger,
            () => _client.GetTechnicalRequirementAsync(query.Id, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="CreateTechnicalRequirementCommand"/>.</summary>
internal sealed class CreateTechnicalRequirementCommandHandler : ICommandHandler<CreateTechnicalRequirementCommand, TechnicalRequirementItem>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CreateTechnicalRequirementCommandHandler> _logger;

    public CreateTechnicalRequirementCommandHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<CreateTechnicalRequirementCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TechnicalRequirementItem>> HandleAsync(CreateTechnicalRequirementCommand command, CallContext context)
        => await RequirementsHandlerHelpers.WriteAsync(
            _authorizationPolicy,
            _logger,
            () => _client.CreateTechnicalRequirementAsync(command, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="UpdateTechnicalRequirementCommand"/>.</summary>
internal sealed class UpdateTechnicalRequirementCommandHandler : ICommandHandler<UpdateTechnicalRequirementCommand, TechnicalRequirementItem>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpdateTechnicalRequirementCommandHandler> _logger;

    public UpdateTechnicalRequirementCommandHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<UpdateTechnicalRequirementCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TechnicalRequirementItem>> HandleAsync(UpdateTechnicalRequirementCommand command, CallContext context)
        => await RequirementsHandlerHelpers.WriteAsync(
            _authorizationPolicy,
            _logger,
            () => _client.UpdateTechnicalRequirementAsync(command, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="DeleteTechnicalRequirementCommand"/>.</summary>
internal sealed class DeleteTechnicalRequirementCommandHandler : ICommandHandler<DeleteTechnicalRequirementCommand, RequirementsMutationOutcome>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<DeleteTechnicalRequirementCommandHandler> _logger;

    public DeleteTechnicalRequirementCommandHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<DeleteTechnicalRequirementCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<RequirementsMutationOutcome>> HandleAsync(DeleteTechnicalRequirementCommand command, CallContext context)
        => await RequirementsHandlerHelpers.WriteAsync(
            _authorizationPolicy,
            _logger,
            () => _client.DeleteTechnicalRequirementAsync(command, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="ListTestingRequirementsQuery"/>.</summary>
internal sealed class ListTestingRequirementsQueryHandler : IQueryHandler<ListTestingRequirementsQuery, TestingRequirementListResult>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListTestingRequirementsQueryHandler> _logger;

    public ListTestingRequirementsQueryHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<ListTestingRequirementsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TestingRequirementListResult>> HandleAsync(ListTestingRequirementsQuery query, CallContext context)
        => await RequirementsHandlerHelpers.ReadAsync(
            _authorizationPolicy,
            _logger,
            () => _client.ListTestingRequirementsAsync(context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="GetTestingRequirementQuery"/>.</summary>
internal sealed class GetTestingRequirementQueryHandler : IQueryHandler<GetTestingRequirementQuery, TestingRequirementItem?>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetTestingRequirementQueryHandler> _logger;

    public GetTestingRequirementQueryHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<GetTestingRequirementQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TestingRequirementItem?>> HandleAsync(GetTestingRequirementQuery query, CallContext context)
        => await RequirementsHandlerHelpers.ReadAsync(
            _authorizationPolicy,
            _logger,
            () => _client.GetTestingRequirementAsync(query.Id, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="CreateTestingRequirementCommand"/>.</summary>
internal sealed class CreateTestingRequirementCommandHandler : ICommandHandler<CreateTestingRequirementCommand, TestingRequirementItem>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CreateTestingRequirementCommandHandler> _logger;

    public CreateTestingRequirementCommandHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<CreateTestingRequirementCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TestingRequirementItem>> HandleAsync(CreateTestingRequirementCommand command, CallContext context)
        => await RequirementsHandlerHelpers.WriteAsync(
            _authorizationPolicy,
            _logger,
            () => _client.CreateTestingRequirementAsync(command, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="UpdateTestingRequirementCommand"/>.</summary>
internal sealed class UpdateTestingRequirementCommandHandler : ICommandHandler<UpdateTestingRequirementCommand, TestingRequirementItem>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpdateTestingRequirementCommandHandler> _logger;

    public UpdateTestingRequirementCommandHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<UpdateTestingRequirementCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TestingRequirementItem>> HandleAsync(UpdateTestingRequirementCommand command, CallContext context)
        => await RequirementsHandlerHelpers.WriteAsync(
            _authorizationPolicy,
            _logger,
            () => _client.UpdateTestingRequirementAsync(command, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="DeleteTestingRequirementCommand"/>.</summary>
internal sealed class DeleteTestingRequirementCommandHandler : ICommandHandler<DeleteTestingRequirementCommand, RequirementsMutationOutcome>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<DeleteTestingRequirementCommandHandler> _logger;

    public DeleteTestingRequirementCommandHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<DeleteTestingRequirementCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<RequirementsMutationOutcome>> HandleAsync(DeleteTestingRequirementCommand command, CallContext context)
        => await RequirementsHandlerHelpers.WriteAsync(
            _authorizationPolicy,
            _logger,
            () => _client.DeleteTestingRequirementAsync(command, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="ListRequirementMappingsQuery"/>.</summary>
internal sealed class ListRequirementMappingsQueryHandler : IQueryHandler<ListRequirementMappingsQuery, RequirementMappingListResult>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListRequirementMappingsQueryHandler> _logger;

    public ListRequirementMappingsQueryHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<ListRequirementMappingsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<RequirementMappingListResult>> HandleAsync(ListRequirementMappingsQuery query, CallContext context)
        => await RequirementsHandlerHelpers.ReadAsync(
            _authorizationPolicy,
            _logger,
            () => _client.ListMappingsAsync(context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="GetRequirementMappingQuery"/>.</summary>
internal sealed class GetRequirementMappingQueryHandler : IQueryHandler<GetRequirementMappingQuery, RequirementMappingItem?>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetRequirementMappingQueryHandler> _logger;

    public GetRequirementMappingQueryHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<GetRequirementMappingQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<RequirementMappingItem?>> HandleAsync(GetRequirementMappingQuery query, CallContext context)
        => await RequirementsHandlerHelpers.ReadAsync(
            _authorizationPolicy,
            _logger,
            () => _client.GetMappingAsync(query.FrId, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="UpsertRequirementMappingCommand"/>.</summary>
internal sealed class UpsertRequirementMappingCommandHandler : ICommandHandler<UpsertRequirementMappingCommand, RequirementMappingItem>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpsertRequirementMappingCommandHandler> _logger;

    public UpsertRequirementMappingCommandHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<UpsertRequirementMappingCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<RequirementMappingItem>> HandleAsync(UpsertRequirementMappingCommand command, CallContext context)
        => await RequirementsHandlerHelpers.WriteAsync(
            _authorizationPolicy,
            _logger,
            () => _client.UpsertMappingAsync(command, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="DeleteRequirementMappingCommand"/>.</summary>
internal sealed class DeleteRequirementMappingCommandHandler : ICommandHandler<DeleteRequirementMappingCommand, RequirementsMutationOutcome>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<DeleteRequirementMappingCommandHandler> _logger;

    public DeleteRequirementMappingCommandHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<DeleteRequirementMappingCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<RequirementsMutationOutcome>> HandleAsync(DeleteRequirementMappingCommand command, CallContext context)
        => await RequirementsHandlerHelpers.WriteAsync(
            _authorizationPolicy,
            _logger,
            () => _client.DeleteMappingAsync(command, context.CancellationToken)).ConfigureAwait(true);
}

/// <summary>Handles <see cref="GenerateRequirementsDocumentQuery"/>.</summary>
internal sealed class GenerateRequirementsDocumentQueryHandler : IQueryHandler<GenerateRequirementsDocumentQuery, GeneratedRequirementsDocument>
{
    private readonly IRequirementsApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GenerateRequirementsDocumentQueryHandler> _logger;

    public GenerateRequirementsDocumentQueryHandler(IRequirementsApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<GenerateRequirementsDocumentQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GeneratedRequirementsDocument>> HandleAsync(GenerateRequirementsDocumentQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.RequirementsGenerate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.RequirementsGenerate);
            return Result<GeneratedRequirementsDocument>.Failure(RequirementsHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.GenerateAsync(query, context.CancellationToken).ConfigureAwait(true);
            return Result<GeneratedRequirementsDocument>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GeneratedRequirementsDocument>.Failure(ex);
        }
    }
}

internal static class RequirementsHandlerHelpers
{
    public static async Task<Result<T>> ReadAsync<T>(
        IAuthorizationPolicyService authorizationPolicy,
        ILogger logger,
        Func<Task<T>> operation)
    {
        if (!authorizationPolicy.CanExecuteAction(McpActionKeys.RequirementsRead))
        {
            var requiredRole = authorizationPolicy.GetRequiredRole(McpActionKeys.RequirementsRead);
            return Result<T>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            return Result<T>.Success(await operation().ConfigureAwait(true));
        }
        catch (Exception ex)
        {
            logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<T>.Failure(ex);
        }
    }

    public static async Task<Result<T>> WriteAsync<T>(
        IAuthorizationPolicyService authorizationPolicy,
        ILogger logger,
        Func<Task<T>> operation)
    {
        if (!authorizationPolicy.CanExecuteAction(McpActionKeys.RequirementsWrite))
        {
            var requiredRole = authorizationPolicy.GetRequiredRole(McpActionKeys.RequirementsWrite);
            return Result<T>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            return Result<T>.Success(await operation().ConfigureAwait(true));
        }
        catch (Exception ex)
        {
            logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<T>.Failure(ex);
        }
    }

    public static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
