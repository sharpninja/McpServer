// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="ListTemplatesQuery"/> via the template API client.</summary>
internal sealed class ListTemplatesQueryHandler : IQueryHandler<ListTemplatesQuery, ListTemplatesResult>
{
    private readonly ITemplateApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListTemplatesQueryHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="ListTemplatesQueryHandler"/> class.</summary>
    public ListTemplatesQueryHandler(
        ITemplateApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ListTemplatesQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ListTemplatesResult>> HandleAsync(ListTemplatesQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TemplateList))
        {
            var role = _authorizationPolicy.GetRequiredRole(McpActionKeys.TemplateList);
            return Result<ListTemplatesResult>.Failure(
                string.IsNullOrWhiteSpace(role) ? "Permission denied." : $"Permission denied: requires {role}.");
        }

        try
        {
            var result = await _client.ListTemplatesAsync(
                query.Category, query.Tag, query.Keyword, context.CancellationToken).ConfigureAwait(false);
            return Result<ListTemplatesResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ListTemplatesResult>.Failure(ex);
        }
    }
}
