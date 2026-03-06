// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="GetTemplateQuery"/> via the template API client.</summary>
internal sealed class GetTemplateQueryHandler : IQueryHandler<GetTemplateQuery, TemplateDetail?>
{
    private readonly ITemplateApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetTemplateQueryHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetTemplateQueryHandler"/> class.</summary>
    public GetTemplateQueryHandler(
        ITemplateApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetTemplateQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TemplateDetail?>> HandleAsync(GetTemplateQuery query, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(query.TemplateId))
            return Result<TemplateDetail?>.Failure("TemplateId is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TemplateGet))
        {
            var role = _authorizationPolicy.GetRequiredRole(McpActionKeys.TemplateGet);
            return Result<TemplateDetail?>.Failure(
                string.IsNullOrWhiteSpace(role) ? "Permission denied." : $"Permission denied: requires {role}.");
        }

        try
        {
            var result = await _client.GetTemplateAsync(query.TemplateId, context.CancellationToken);
            return Result<TemplateDetail?>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TemplateDetail?>.Failure(ex);
        }
    }
}
