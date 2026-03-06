// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="DeleteTemplateCommand"/> via the template API client.</summary>
internal sealed class DeleteTemplateCommandHandler : ICommandHandler<DeleteTemplateCommand, TemplateMutationOutcome>
{
    private readonly ITemplateApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<DeleteTemplateCommandHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeleteTemplateCommandHandler"/> class.</summary>
    public DeleteTemplateCommandHandler(
        ITemplateApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<DeleteTemplateCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TemplateMutationOutcome>> HandleAsync(DeleteTemplateCommand command, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(command.TemplateId))
            return Result<TemplateMutationOutcome>.Failure("TemplateId is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TemplateDelete))
        {
            var role = _authorizationPolicy.GetRequiredRole(McpActionKeys.TemplateDelete);
            return Result<TemplateMutationOutcome>.Failure(
                string.IsNullOrWhiteSpace(role) ? "Permission denied." : $"Permission denied: requires {role}.");
        }

        try
        {
            var result = await _client.DeleteTemplateAsync(command.TemplateId, context.CancellationToken).ConfigureAwait(true);
            return Result<TemplateMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TemplateMutationOutcome>.Failure(ex);
        }
    }
}
