using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// Tool registry endpoints for discovering, managing, and installing tool definitions.
/// Tools are searchable by keyword tags. They can be global or workspace-scoped;
/// keyword queries return the union of both sets. Tools can also be installed
/// from GitHub-backed bucket repositories (similar to Scoop package manager).
/// </summary>
[ApiController]
[Route("mcpserver/tools")]
public sealed class ToolRegistryController : ControllerBase
{
    private readonly IToolRegistryService _registry;
    private readonly IToolBucketService _bucketService;

    /// <summary>Initializes a new instance of the <see cref="ToolRegistryController"/> class.</summary>
    public ToolRegistryController(IToolRegistryService registry, IToolBucketService bucketService)
    {
        _registry = registry;
        _bucketService = bucketService;
    }

    // ── Tool search & CRUD ─────────────────────────────────────────────

    /// <summary>Search tools by keyword. Returns global tools plus tools for the given workspace.</summary>
    /// <param name="keyword">Keyword to match against tool tags, name, and description.</param>
    /// <param name="workspace">Optional workspace path to include workspace-scoped tools.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching tool definitions.</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ToolSearchResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ToolSearchResult>> SearchAsync(
        [FromQuery] string keyword,
        [FromQuery] string? workspace = null,
        CancellationToken ct = default)
    {
        var result = await _registry.SearchAsync(keyword, workspace, ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>List all tools, optionally filtered to a workspace (always includes global).</summary>
    /// <param name="workspace">Optional workspace path to include workspace-scoped tools.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ToolSearchResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ToolSearchResult>> ListAsync(
        [FromQuery] string? workspace = null,
        CancellationToken ct = default)
    {
        var result = await _registry.ListAsync(workspace, ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Get a single tool by id.</summary>
    /// <param name="id">Tool definition id.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ToolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ToolDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var dto = await _registry.GetAsync(id, ct).ConfigureAwait(false);
        if (dto is null)
            return NotFound(new { error = "Tool not found." });
        return Ok(dto);
    }

    /// <summary>Register a new tool definition (global or workspace-scoped).</summary>
    /// <param name="request">Tool creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(ToolMutationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ToolMutationResult), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolMutationResult>> CreateAsync(
        [FromBody] ToolCreateRequest request,
        CancellationToken ct = default)
    {
        var result = await _registry.CreateAsync(request, ct).ConfigureAwait(false);
        if (!result.Success)
            return Conflict(result);
        return Created(new Uri($"/mcpserver/tools/{result.Tool!.Id}", UriKind.Relative), result);
    }

    /// <summary>Update an existing tool definition. Null fields are left unchanged.</summary>
    /// <param name="id">Tool definition id.</param>
    /// <param name="request">Partial update request.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ToolMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ToolMutationResult), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ToolMutationResult>> UpdateAsync(
        int id,
        [FromBody] ToolUpdateRequest request,
        CancellationToken ct = default)
    {
        var result = await _registry.UpdateAsync(id, request, ct).ConfigureAwait(false);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Delete a tool definition.</summary>
    /// <param name="id">Tool definition id.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ToolMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ToolMutationResult), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ToolMutationResult>> DeleteAsync(
        int id,
        CancellationToken ct = default)
    {
        var result = await _registry.DeleteAsync(id, ct).ConfigureAwait(false);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    // ── Bucket management ──────────────────────────────────────────────

    /// <summary>List all registered tool buckets.</summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("buckets")]
    [ProducesResponseType(typeof(BucketListResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BucketListResult>> ListBucketsAsync(CancellationToken ct = default)
    {
        var result = await _bucketService.ListBucketsAsync(ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Add a GitHub repository as a tool bucket.</summary>
    /// <param name="request">Bucket add request with owner, repo, and optional branch/path.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("buckets")]
    [ProducesResponseType(typeof(BucketMutationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BucketMutationResult), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BucketMutationResult>> AddBucketAsync(
        [FromBody] BucketAddRequest request,
        CancellationToken ct = default)
    {
        var result = await _bucketService.AddBucketAsync(request, ct).ConfigureAwait(false);
        if (!result.Success)
            return Conflict(result);
        return Created(new Uri($"/mcpserver/tools/buckets", UriKind.Relative), result);
    }

    /// <summary>Remove a bucket and optionally uninstall all tools installed from it.</summary>
    /// <param name="name">Bucket name.</param>
    /// <param name="uninstallTools">If true, also remove tools installed from this bucket.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("buckets/{name}")]
    [ProducesResponseType(typeof(BucketMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BucketMutationResult), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BucketMutationResult>> RemoveBucketAsync(
        string name,
        [FromQuery] bool uninstallTools = false,
        CancellationToken ct = default)
    {
        var result = await _bucketService.RemoveBucketAsync(name, uninstallTools, ct).ConfigureAwait(false);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Browse available tool manifests in a bucket (reads from GitHub).</summary>
    /// <param name="name">Bucket name.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("buckets/{name}/browse")]
    [ProducesResponseType(typeof(BucketBrowseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BucketBrowseResult), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BucketBrowseResult>> BrowseBucketAsync(
        string name,
        CancellationToken ct = default)
    {
        var result = await _bucketService.BrowseAsync(name, ct).ConfigureAwait(false);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Install a tool from a bucket into the server (global) or a workspace.</summary>
    /// <param name="name">Bucket name.</param>
    /// <param name="toolName">Tool name from the bucket manifest.</param>
    /// <param name="workspace">Optional workspace path to scope the tool to.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("buckets/{name}/install")]
    [ProducesResponseType(typeof(ToolMutationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ToolMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ToolMutationResult), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ToolMutationResult>> InstallFromBucketAsync(
        string name,
        [FromQuery] string toolName,
        [FromQuery] string? workspace = null,
        CancellationToken ct = default)
    {
        var result = await _bucketService.InstallAsync(name, toolName, workspace, ct).ConfigureAwait(false);
        if (!result.Success)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(result);
            return Conflict(result);
        }
        return Created(new Uri($"/mcpserver/tools/{result.Tool!.Id}", UriKind.Relative), result);
    }

    /// <summary>Sync all installed tools from a bucket to pick up manifest changes.</summary>
    /// <param name="name">Bucket name.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("buckets/{name}/sync")]
    [ProducesResponseType(typeof(BucketSyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BucketSyncResult), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BucketSyncResult>> SyncBucketAsync(
        string name,
        CancellationToken ct = default)
    {
        var result = await _bucketService.SyncAsync(name, ct).ConfigureAwait(false);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }
}
