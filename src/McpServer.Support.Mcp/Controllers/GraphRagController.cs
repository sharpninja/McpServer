using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

#pragma warning disable CS1591

[ApiController]
[Route("mcpserver/graphrag")]
[Produces("application/json")]
public sealed class GraphRagController : ControllerBase
{
    private readonly IGraphRagService _graphRagService;

    public GraphRagController(IGraphRagService graphRagService)
    {
        _graphRagService = graphRagService;
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(GraphRagStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GraphRagStatusResponse>> GetStatusAsync(CancellationToken cancellationToken)
    {
        return Ok(await _graphRagService.GetStatusAsync(cancellationToken).ConfigureAwait(false));
    }

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
}

#pragma warning restore CS1591
