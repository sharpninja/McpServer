using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Service interface for prompt template CRUD and test/render operations.
/// Implementations are registered as global singletons.
/// </summary>
public interface IPromptTemplateService
{
    /// <summary>Query templates with optional filters.</summary>
    /// <param name="category">Optional exact category filter.</param>
    /// <param name="tag">Optional tag filter (any match).</param>
    /// <param name="keyword">Optional keyword search across id, title, description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query result with matching templates and total count.</returns>
    Task<PromptTemplateQueryResult> QueryAsync(
        string? category = null,
        string? tag = null,
        string? keyword = null,
        CancellationToken cancellationToken = default);

    /// <summary>Get a single template by ID.</summary>
    /// <param name="id">Template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template, or null if not found.</returns>
    Task<PromptTemplate?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Create a new template.</summary>
    /// <param name="request">Create request with required fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mutation result indicating success or failure.</returns>
    Task<PromptTemplateMutationResult> CreateAsync(PromptTemplateCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Update an existing template.</summary>
    /// <param name="id">Template identifier.</param>
    /// <param name="request">Update request with nullable fields (null = no change).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mutation result indicating success or failure.</returns>
    Task<PromptTemplateMutationResult> UpdateAsync(string id, PromptTemplateUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Delete a template.</summary>
    /// <param name="id">Template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mutation result indicating success or failure.</returns>
    Task<PromptTemplateMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Test/render a stored template with sample data.</summary>
    /// <param name="id">Template identifier.</param>
    /// <param name="request">Test request with variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test result with rendered content or error details.</returns>
    Task<PromptTemplateTestResult> TestAsync(string id, PromptTemplateTestRequest request, CancellationToken cancellationToken = default);

    /// <summary>Test/render an inline template with sample data.</summary>
    /// <param name="request">Test request with inline template and variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test result with rendered content or error details.</returns>
    Task<PromptTemplateTestResult> TestInlineAsync(PromptTemplateTestRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-TXN-001: Captures and restores prompt-template storage for transactional mutation compensation.
/// </summary>
public interface IPromptTemplateCompensation
{
    /// <summary>Captures the current prompt-template storage snapshot.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The current prompt-template file snapshot.</returns>
    Task<PromptTemplateFileSnapshot> CaptureFileAsync(CancellationToken cancellationToken = default);

    /// <summary>Restores the prompt-template storage snapshot after a failed transaction commit.</summary>
    /// <param name="snapshot">Snapshot captured before the mutation.</param>
    /// <param name="expectedCurrentContentSha256">Expected current content hash after the rejected transaction write.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RestoreFileAsync(
        PromptTemplateFileSnapshot snapshot,
        string expectedCurrentContentSha256,
        CancellationToken cancellationToken = default);
}

/// <summary>TR-MCP-TXN-001: Prompt-template file state snapshot used for rollback compensation.</summary>
/// <param name="Exists">Whether the prompt-template storage file exists.</param>
/// <param name="Content">Raw file content, or <see langword="null"/> when the file does not exist.</param>
/// <param name="ContentSha256">SHA-256 hash of the raw file content, or empty when the file does not exist.</param>
public sealed record PromptTemplateFileSnapshot(bool Exists, string? Content, string ContentSha256);
