using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-078/079/080, TR-GRAPHRAG-ADHOC-001/002/003: Controller exposing GraphRAG lifecycle,
/// ad-hoc text ingestion, document management, entity CRUD, and relationship CRUD endpoints.
/// </summary>
[ApiController]
[Route("mcpserver/graphrag")]
[Produces("application/json")]
public sealed class GraphRagController : ControllerBase
{
    private readonly IGraphRagService _graphRagService;

    /// <summary>Initializes a new instance of the <see cref="GraphRagController"/> class.</summary>
    /// <param name="graphRagService">The GraphRAG service.</param>
    public GraphRagController(IGraphRagService graphRagService)
    {
        _graphRagService = graphRagService;
    }

    /// <summary>Gets the current GraphRAG status for the workspace or global scope.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(GraphRagStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GraphRagStatusResponse>> GetStatusAsync(
        [FromQuery] GraphRagStorageScope scope = GraphRagStorageScope.Workspace,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _graphRagService.GetStatusAsync(scope, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Triggers a GraphRAG index operation.</summary>
    [HttpPost("index")]
    [ProducesResponseType(typeof(GraphRagStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GraphRagStatusResponse>> IndexAsync([FromBody] GraphRagIndexRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _graphRagService.IndexAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message, code = "index_conflict" });
        }
    }

    /// <summary>Runs a GraphRAG query with citations and optional context chunks.</summary>
    [HttpPost("query")]
    [ProducesResponseType(typeof(GraphRagQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GraphRagQueryResponse>> QueryAsync([FromBody] GraphRagQueryRequest? request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Query))
            return BadRequest(new { error = "query is required" });
        if (request.MaxChunks is < 1 or > 100)
            return BadRequest(new { error = "maxChunks must be between 1 and 100" });
        if (request.MaxEntities is < 1 or > 1000)
            return BadRequest(new { error = "maxEntities must be between 1 and 1000" });
        if (request.MaxRelationships is < 1 or > 1000)
            return BadRequest(new { error = "maxRelationships must be between 1 and 1000" });
        if (request.CommunityDepth is < 1 or > 10)
            return BadRequest(new { error = "communityDepth must be between 1 and 10" });

        return Ok(await _graphRagService.QueryAsync(request, cancellationToken).ConfigureAwait(false));
    }

    // ── Ad-Hoc Text Ingestion (FR-MCP-078, TR-GRAPHRAG-ADHOC-001) ──

    /// <summary>FR-MCP-078: Ingests raw text into the GraphRAG corpus.</summary>
    [HttpPost("documents/ingest")]
    [ProducesResponseType(typeof(GraphRagIngestTextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GraphRagIngestTextResponse>> IngestTextAsync([FromBody] GraphRagIngestTextRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "content is required" });

        var response = await _graphRagService.IngestTextAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    // ── Document Management (FR-MCP-080, TR-GRAPHRAG-ADHOC-003) ──

    /// <summary>FR-MCP-080: Lists documents in the GraphRAG corpus with pagination.</summary>
    [HttpGet("documents")]
    [ProducesResponseType(typeof(GraphRagDocumentListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GraphRagDocumentListResponse>> ListDocumentsAsync(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _graphRagService.ListDocumentsAsync(skip, take, sourceType, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>FR-MCP-080: Retrieves all chunks for a specific document.</summary>
    [HttpGet("documents/{documentId}/chunks")]
    [ProducesResponseType(typeof(GraphRagDocumentChunksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GraphRagDocumentChunksResponse>> GetDocumentChunksAsync(string documentId, CancellationToken cancellationToken)
    {
        var response = await _graphRagService.GetDocumentChunksAsync(documentId, cancellationToken).ConfigureAwait(false);
        if (response is null)
            return NotFound(new { error = $"Document '{documentId}' not found" });
        return Ok(response);
    }

    /// <summary>FR-MCP-080: Deletes a document and its chunks from the corpus.</summary>
    [HttpDelete("documents/{documentId}")]
    [ProducesResponseType(typeof(GraphRagDocumentDeleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GraphRagDocumentDeleteResponse>> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken)
    {
        var response = await _graphRagService.DeleteDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
        if (!response.Success)
            return NotFound(new { error = $"Document '{documentId}' not found" });
        return Ok(response);
    }

    // ── Entity CRUD (FR-MCP-079, TR-GRAPHRAG-ADHOC-002) ──

    /// <summary>FR-MCP-079: Creates a new graph entity.</summary>
    [HttpPost("entities")]
    [ProducesResponseType(typeof(GraphEntityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GraphEntityResponse>> CreateEntityAsync([FromBody] GraphEntityRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.EntityType))
            return BadRequest(new { error = "name and entityType are required" });

        var response = await _graphRagService.CreateEntityAsync(request, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetEntityAsync), new { entityId = response.Id }, response);
    }

    /// <summary>FR-MCP-079: Lists graph entities with pagination and optional type filter.</summary>
    [HttpGet("entities")]
    [ProducesResponseType(typeof(GraphEntityListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GraphEntityListResponse>> ListEntitiesAsync(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? entityType = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _graphRagService.ListEntitiesAsync(skip, take, entityType, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>FR-MCP-079: Retrieves a graph entity by ID.</summary>
    [HttpGet("entities/{entityId}")]
    [ProducesResponseType(typeof(GraphEntityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GraphEntityResponse>> GetEntityAsync(string entityId, CancellationToken cancellationToken)
    {
        var response = await _graphRagService.GetEntityAsync(entityId, cancellationToken).ConfigureAwait(false);
        if (response is null)
            return NotFound(new { error = $"Entity '{entityId}' not found" });
        return Ok(response);
    }

    /// <summary>FR-MCP-079: Updates an existing graph entity.</summary>
    [HttpPut("entities/{entityId}")]
    [ProducesResponseType(typeof(GraphEntityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GraphEntityResponse>> UpdateEntityAsync(string entityId, [FromBody] GraphEntityRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "request body is required" });

        var response = await _graphRagService.UpdateEntityAsync(entityId, request, cancellationToken).ConfigureAwait(false);
        if (response is null)
            return NotFound(new { error = $"Entity '{entityId}' not found" });
        return Ok(response);
    }

    /// <summary>FR-MCP-079: Deletes a graph entity by ID.</summary>
    [HttpDelete("entities/{entityId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntityAsync(string entityId, CancellationToken cancellationToken)
    {
        var deleted = await _graphRagService.DeleteEntityAsync(entityId, cancellationToken).ConfigureAwait(false);
        if (!deleted)
            return NotFound(new { error = $"Entity '{entityId}' not found" });
        return NoContent();
    }

    // ── Relationship CRUD (FR-MCP-079, TR-GRAPHRAG-ADHOC-002) ──

    /// <summary>FR-MCP-079: Creates a new graph relationship.</summary>
    [HttpPost("relationships")]
    [ProducesResponseType(typeof(GraphRelationshipResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GraphRelationshipResponse>> CreateRelationshipAsync([FromBody] GraphRelationshipRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.SourceEntityId) || string.IsNullOrWhiteSpace(request.TargetEntityId) || string.IsNullOrWhiteSpace(request.RelationshipType))
            return BadRequest(new { error = "sourceEntityId, targetEntityId, and relationshipType are required" });

        var response = await _graphRagService.CreateRelationshipAsync(request, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetRelationshipAsync), new { relationshipId = response.Id }, response);
    }

    /// <summary>FR-MCP-079: Lists graph relationships with pagination and optional filters.</summary>
    [HttpGet("relationships")]
    [ProducesResponseType(typeof(GraphRelationshipListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GraphRelationshipListResponse>> ListRelationshipsAsync(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? entityId = null,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _graphRagService.ListRelationshipsAsync(skip, take, entityId, type, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>FR-MCP-079: Retrieves a graph relationship by ID.</summary>
    [HttpGet("relationships/{relationshipId}")]
    [ProducesResponseType(typeof(GraphRelationshipResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GraphRelationshipResponse>> GetRelationshipAsync(string relationshipId, CancellationToken cancellationToken)
    {
        var response = await _graphRagService.GetRelationshipAsync(relationshipId, cancellationToken).ConfigureAwait(false);
        if (response is null)
            return NotFound(new { error = $"Relationship '{relationshipId}' not found" });
        return Ok(response);
    }

    /// <summary>FR-MCP-079: Updates an existing graph relationship.</summary>
    [HttpPut("relationships/{relationshipId}")]
    [ProducesResponseType(typeof(GraphRelationshipResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GraphRelationshipResponse>> UpdateRelationshipAsync(string relationshipId, [FromBody] GraphRelationshipRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "request body is required" });

        var response = await _graphRagService.UpdateRelationshipAsync(relationshipId, request, cancellationToken).ConfigureAwait(false);
        if (response is null)
            return NotFound(new { error = $"Relationship '{relationshipId}' not found" });
        return Ok(response);
    }

    /// <summary>FR-MCP-079: Deletes a graph relationship by ID.</summary>
    [HttpDelete("relationships/{relationshipId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRelationshipAsync(string relationshipId, CancellationToken cancellationToken)
    {
        var deleted = await _graphRagService.DeleteRelationshipAsync(relationshipId, cancellationToken).ConfigureAwait(false);
        if (!deleted)
            return NotFound(new { error = $"Relationship '{relationshipId}' not found" });
        return NoContent();
    }
}
