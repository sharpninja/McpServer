// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Abstraction over the prompt template HTTP API client for CQRS handlers.
/// </summary>
public interface ITemplateApiClient
{
    /// <summary>List/filter prompt templates.</summary>
    /// <param name="category">Optional category filter.</param>
    /// <param name="tag">Optional tag filter.</param>
    /// <param name="keyword">Optional keyword search.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List result with matching templates.</returns>
    Task<ListTemplatesResult> ListTemplatesAsync(string? category, string? tag, string? keyword, CancellationToken cancellationToken = default);

    /// <summary>Get a single template by ID.</summary>
    /// <param name="templateId">Template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Template detail, or null if not found.</returns>
    Task<TemplateDetail?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default);

    /// <summary>Create a new template.</summary>
    /// <param name="command">Create command with required fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mutation outcome.</returns>
    Task<TemplateMutationOutcome> CreateTemplateAsync(CreateTemplateCommand command, CancellationToken cancellationToken = default);

    /// <summary>Update an existing template.</summary>
    /// <param name="command">Update command with nullable fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mutation outcome.</returns>
    Task<TemplateMutationOutcome> UpdateTemplateAsync(UpdateTemplateCommand command, CancellationToken cancellationToken = default);

    /// <summary>Delete a template.</summary>
    /// <param name="templateId">Template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mutation outcome.</returns>
    Task<TemplateMutationOutcome> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default);

    /// <summary>Test/render a template with sample data.</summary>
    /// <param name="query">Test query with template ID or inline content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test outcome with rendered content or error.</returns>
    Task<TemplateTestOutcome> TestTemplateAsync(TestTemplateQuery query, CancellationToken cancellationToken = default);
}
