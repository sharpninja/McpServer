// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// REST endpoints for prompt template CRUD and test/render operations.
/// Route: <c>mcpserver/templates</c>.
/// </summary>
[ApiController]
[Route("mcpserver/templates")]
public sealed class PromptTemplateController : ControllerBase
{
    private readonly IPromptTemplateService _service;

    /// <summary>Initializes a new instance of the <see cref="PromptTemplateController"/> class.</summary>
    /// <param name="service">Prompt template service.</param>
    public PromptTemplateController(IPromptTemplateService service)
    {
        _service = service;
    }

    /// <summary>List/filter prompt templates.</summary>
    /// <param name="category">Optional exact category filter.</param>
    /// <param name="tag">Optional tag filter.</param>
    /// <param name="keyword">Optional keyword search.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query result with matching templates.</returns>
    [HttpGet]
    public async Task<ActionResult<PromptTemplateQueryResult>> Query(
        [FromQuery] string? category = null,
        [FromQuery] string? tag = null,
        [FromQuery] string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.QueryAsync(category, tag, keyword, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Get a single template by ID.</summary>
    /// <param name="id">Template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template, or 404 if not found.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<PromptTemplate>> GetById(
        string id,
        CancellationToken cancellationToken = default)
    {
        var template = await _service.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (template is null)
            return NotFound(new { error = $"Template '{id}' not found." });
        return Ok(template);
    }

    /// <summary>Create a new prompt template.</summary>
    /// <param name="request">Create request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created template or error.</returns>
    [HttpPost]
    public async Task<ActionResult<PromptTemplateMutationResult>> Create(
        [FromBody] PromptTemplateCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
        {
            if (result.Error?.Contains("already exists", System.StringComparison.OrdinalIgnoreCase) == true)
                return Conflict(result);
            return BadRequest(result);
        }

        return Created($"mcpserver/templates/{request.Id}", result);
    }

    /// <summary>Update an existing prompt template.</summary>
    /// <param name="id">Template identifier.</param>
    /// <param name="request">Update request with nullable fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated template or error.</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<PromptTemplateMutationResult>> Update(
        string id,
        [FromBody] PromptTemplateUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Delete a prompt template.</summary>
    /// <param name="id">Template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deleted template or error.</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult<PromptTemplateMutationResult>> Delete(
        string id,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Test/render a stored template with sample data.</summary>
    /// <param name="id">Template identifier.</param>
    /// <param name="request">Test request with variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Rendered content or error details.</returns>
    [HttpPost("{id}/test")]
    public async Task<ActionResult<PromptTemplateTestResult>> Test(
        string id,
        [FromBody] PromptTemplateTestRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.TestAsync(id, request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>Test/render an inline template (not stored) with sample data.</summary>
    /// <param name="request">Test request with inline template and variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Rendered content or error details.</returns>
    [HttpPost("test")]
    public async Task<ActionResult<PromptTemplateTestResult>> TestInline(
        [FromBody] PromptTemplateTestRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.TestInlineAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>Resolve a stored template by ID using a dictionary of values and return populated prompt text.</summary>
    /// <param name="id">Template identifier.</param>
    /// <param name="request">Resolve request with values dictionary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resolve result containing populated prompt text or validation error details.</returns>
    [HttpPost("{id}/resolve")]
    public async Task<ActionResult<PromptTemplateResolveResult>> Resolve(
        string id,
        [FromBody] PromptTemplateResolveRequest request,
        CancellationToken cancellationToken = default)
    {
        var test = await _service.TestAsync(
                id,
                new PromptTemplateTestRequest { Variables = request.Values },
                cancellationToken)
            .ConfigureAwait(false);

        if (!test.Success)
        {
            return BadRequest(new PromptTemplateResolveResult
            {
                Success = false,
                TemplateId = id,
                Error = test.Error,
                MissingVariables = test.MissingVariables,
            });
        }

        return Ok(new PromptTemplateResolveResult
        {
            Success = true,
            TemplateId = id,
            Prompt = test.RenderedContent,
        });
    }
}
