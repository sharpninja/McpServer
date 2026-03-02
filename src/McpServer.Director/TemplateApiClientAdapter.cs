// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using System.Text.Json;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="ITemplateApiClient"/> backed by <see cref="McpServerClient"/>.
/// </summary>
internal sealed class TemplateApiClientAdapter : ITemplateApiClient
{
    private readonly DirectorMcpContext _context;
    private readonly ILogger<TemplateApiClientAdapter> _logger;

    /// <summary>Initializes a new instance of the <see cref="TemplateApiClientAdapter"/> class.</summary>
    public TemplateApiClientAdapter(DirectorMcpContext context,
        ILogger<TemplateApiClientAdapter>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<TemplateApiClientAdapter>.Instance;
    }

    /// <inheritdoc />
    public async Task<ListTemplatesResult> ListTemplatesAsync(
        string? category, string? tag, string? keyword,
        CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client.Template.QueryAsync(category, tag, keyword, cancellationToken).ConfigureAwait(false);

        var items = response.Items
            .Select(i => new TemplateListItem(i.Id, i.Title, i.Category, i.Tags.ToList(), i.Description))
            .ToList();

        return new ListTemplatesResult(items, response.TotalCount);
    }

    /// <inheritdoc />
    public async Task<TemplateDetail?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
            var item = await client.Template.GetAsync(templateId, cancellationToken).ConfigureAwait(false);
            return MapDetail(item);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TemplateMutationOutcome> CreateTemplateAsync(
        CreateTemplateCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
            var result = await client.Template.CreateAsync(new TemplateCreateRequest
            {
                Id = command.Id,
                Title = command.Title,
                Category = command.Category,
                Content = command.Content,
                Tags = command.Tags?.ToList(),
                Description = command.Description,
                Engine = command.Engine,
            }, cancellationToken).ConfigureAwait(false);

            return MapMutationOutcome(result);
        }
        catch (McpConflictException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new TemplateMutationOutcome(false, ex.Message, null);
        }
        catch (McpValidationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new TemplateMutationOutcome(false, ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<TemplateMutationOutcome> UpdateTemplateAsync(
        UpdateTemplateCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
            var result = await client.Template.UpdateAsync(command.TemplateId, new TemplateUpdateRequest
            {
                Title = command.Title,
                Category = command.Category,
                Content = command.Content,
                Tags = command.Tags?.ToList(),
                Description = command.Description,
                Engine = command.Engine,
            }, cancellationToken).ConfigureAwait(false);

            return MapMutationOutcome(result);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new TemplateMutationOutcome(false, ex.Message, null);
        }
        catch (McpValidationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new TemplateMutationOutcome(false, ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<TemplateMutationOutcome> DeleteTemplateAsync(
        string templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
            var result = await client.Template.DeleteAsync(templateId, cancellationToken).ConfigureAwait(false);
            return MapMutationOutcome(result);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new TemplateMutationOutcome(false, ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<TemplateTestOutcome> TestTemplateAsync(
        TestTemplateQuery query, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var variables = string.IsNullOrWhiteSpace(query.VariablesJson)
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(query.VariablesJson) ?? new();

        var request = new TemplateTestRequest { Variables = variables, InlineTemplate = query.InlineTemplate };

        TemplateTestResult result;
        if (!string.IsNullOrWhiteSpace(query.TemplateId))
        {
            result = await client.Template.TestAsync(query.TemplateId, request, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            result = await client.Template.TestInlineAsync(request, cancellationToken).ConfigureAwait(false);
        }

        return new TemplateTestOutcome(
            result.Success,
            result.RenderedContent,
            result.Error,
            result.MissingVariables?.ToList());
    }

    private static TemplateDetail? MapDetail(TemplateItem? item)
    {
        if (item is null)
            return null;

        return new TemplateDetail(
            item.Id,
            item.Title,
            item.Category,
            item.Tags.ToList(),
            item.Description,
            item.Engine,
            item.Variables.Select(v => new TemplateVariableDetail(
                v.Name, v.Description, v.Required, v.Example, v.DefaultValue)).ToList(),
            item.Content);
    }

    private static TemplateMutationOutcome MapMutationOutcome(TemplateMutationResult result)
    {
        return new TemplateMutationOutcome(
            result.Success,
            result.Error,
            MapDetail(result.Item));
    }
}
