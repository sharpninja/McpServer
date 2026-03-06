// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="CreateTemplateCommand"/> via the template API client.</summary>
internal sealed class CreateTemplateCommandHandler : ICommandHandler<CreateTemplateCommand, TemplateMutationOutcome>
{
    private readonly ITemplateApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CreateTemplateCommandHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="CreateTemplateCommandHandler"/> class.</summary>
    public CreateTemplateCommandHandler(
        ITemplateApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<CreateTemplateCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TemplateMutationOutcome>> HandleAsync(CreateTemplateCommand command, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
            return Result<TemplateMutationOutcome>.Failure("Id is required.");
        if (string.IsNullOrWhiteSpace(command.Title))
            return Result<TemplateMutationOutcome>.Failure("Title is required.");
        if (string.IsNullOrWhiteSpace(command.Category))
            return Result<TemplateMutationOutcome>.Failure("Category is required.");
        if (string.IsNullOrWhiteSpace(command.Content))
            return Result<TemplateMutationOutcome>.Failure("Content is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TemplateCreate))
        {
            var role = _authorizationPolicy.GetRequiredRole(McpActionKeys.TemplateCreate);
            return Result<TemplateMutationOutcome>.Failure(
                string.IsNullOrWhiteSpace(role) ? "Permission denied." : $"Permission denied: requires {role}.");
        }

        try
        {
            var result = await _client.CreateTemplateAsync(command, context.CancellationToken);
            return Result<TemplateMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TemplateMutationOutcome>.Failure(ex);
        }
    }
}
