using System.Diagnostics.CodeAnalysis;
using McpServer.Cqrs;
using McpServer.Support.Mcp.Products;
using McpServer.Support.Mcp.Products.Commands;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Products.Queries;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// TR-MCP-PRODUCT-API-001 / FR-MCP-PRODUCT-001..002: Thin REST adapter over product CQRS handlers.
/// </summary>
[ApiController]
[Route("mcpserver/products")]
[Produces("application/json")]
public sealed class ProductsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;
    private readonly WorkspaceContext _workspaceContext;

    /// <summary>TR-MCP-PRODUCT-API-001: Initializes the controller with CQRS dispatcher and workspace context.</summary>
    public ProductsController(IDispatcher dispatcher, WorkspaceContext workspaceContext)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
    }

    /// <summary>FR-MCP-PRODUCT-001: Create a product owned by the caller.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<ProductDto>> CreateAsync(
        [FromBody] CreateProductRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        var result = await _dispatcher.SendAsync(
            new CreateProductCommand(GetWorkspacePath(), request),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result.Error);

        return Created(
            new Uri($"/mcpserver/products/{result.Value!.Key}", UriKind.Relative),
            result.Value);
    }

    /// <summary>FR-MCP-PRODUCT-001: List products visible to the caller.</summary>
    [HttpGet]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> ListAsync(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.QueryAsync(
            new ListProductsQuery(GetWorkspacePath()),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result.Error);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-PRODUCT-001: Get one visible product.</summary>
    [HttpGet("{key}")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<ProductDto>> GetAsync(string key, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.QueryAsync(
            new GetProductQuery(GetWorkspacePath(), key),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result.Error);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-PRODUCT-001: Owner updates name/description.</summary>
    [HttpPatch("{key}")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<ProductDto>> UpdateAsync(
        string key,
        [FromBody] UpdateProductRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        var result = await _dispatcher.SendAsync(
            new UpdateProductCommand(GetWorkspacePath(), key, request),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result.Error);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-PRODUCT-001: Owner soft-deletes a product.</summary>
    [HttpDelete("{key}")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<IActionResult> DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync(
            new DeleteProductCommand(GetWorkspacePath(), key),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result.Error);
        return NoContent();
    }

    /// <summary>FR-MCP-PRODUCT-002: List members.</summary>
    [HttpGet("{key}/members")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<ProductDto>> ListMembersAsync(string key, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.QueryAsync(
            new ListProductMembersQuery(GetWorkspacePath(), key),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result.Error);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-PRODUCT-002: Owner adds a member.</summary>
    [HttpPut("{key}/members/{workspaceId}")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<ProductDto>> AddMemberAsync(
        string key,
        string workspaceId,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync(
            new AddProductMemberCommand(GetWorkspacePath(), key, workspaceId),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result.Error);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-PRODUCT-002: Owner removes a member, or a member leaves.</summary>
    [HttpDelete("{key}/members/{workspaceId}")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<ProductDto>> RemoveMemberAsync(
        string key,
        string workspaceId,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync(
            new RemoveProductMemberCommand(GetWorkspacePath(), key, workspaceId),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result.Error);
        return Ok(result.Value);
    }

    private string GetWorkspacePath()
        => _workspaceContext.WorkspacePath ?? string.Empty;

    private ActionResult MapFailure(string? error)
    {
        var message = string.IsNullOrWhiteSpace(error) ? "Unexpected product operation failure." : error;
        if (message.StartsWith(ProductResultCodes.BadRequest, StringComparison.Ordinal)
            || message.StartsWith("400", StringComparison.Ordinal))
            return BadRequest(new { error = message });
        if (message.StartsWith(ProductResultCodes.Forbidden, StringComparison.Ordinal)
            || message.StartsWith("403", StringComparison.Ordinal))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = message });
        if (message.StartsWith(ProductResultCodes.NotFound, StringComparison.Ordinal)
            || message.StartsWith("404", StringComparison.Ordinal)
            || message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return NotFound(new { error = message });
        if (message.StartsWith(ProductResultCodes.Conflict, StringComparison.Ordinal)
            || message.StartsWith("409", StringComparison.Ordinal)
            || message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { error = message });

        return StatusCode(StatusCodes.Status500InternalServerError, new { error = message });
    }
}
