// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="TestTemplateQuery"/> via the template API client.</summary>
internal sealed class TestTemplateQueryHandler : IQueryHandler<TestTemplateQuery, TemplateTestOutcome>
{
    private readonly ITemplateApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<TestTemplateQueryHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="TestTemplateQueryHandler"/> class.</summary>
    public TestTemplateQueryHandler(
        ITemplateApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<TestTemplateQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TemplateTestOutcome>> HandleAsync(TestTemplateQuery query, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(query.TemplateId) && string.IsNullOrWhiteSpace(query.InlineTemplate))
            return Result<TemplateTestOutcome>.Failure("Either TemplateId or InlineTemplate is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TemplateTest))
        {
            var role = _authorizationPolicy.GetRequiredRole(McpActionKeys.TemplateTest);
            return Result<TemplateTestOutcome>.Failure(
                string.IsNullOrWhiteSpace(role) ? "Permission denied." : $"Permission denied: requires {role}.");
        }

        try
        {
            var result = await _client.TestTemplateAsync(query, context.CancellationToken).ConfigureAwait(false);
            return Result<TemplateTestOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TemplateTestOutcome>.Failure(ex);
        }
    }
}
