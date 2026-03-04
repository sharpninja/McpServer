using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

#pragma warning disable CS1591

[ApiController]
[Route("mcpserver/graphrag")]
public sealed class GraphRagController : ControllerBase
{
    private readonly IGraphRagService _graphRagService;

    public GraphRagController(IGraphRagService graphRagService)
    {
        _graphRagService = graphRagService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<GraphRagStatusResponse>> GetStatusAsync(CancellationToken cancellationToken)
    {
        return Ok(await _graphRagService.GetStatusAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("index")]
    public async Task<ActionResult<GraphRagStatusResponse>> IndexAsync([FromBody] GraphRagIndexRequest? request, CancellationToken cancellationToken)
    {
        return Ok(await _graphRagService.IndexAsync(request, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("query")]
    public async Task<ActionResult<GraphRagQueryResponse>> QueryAsync([FromBody] GraphRagQueryRequest? request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Query))
            return BadRequest(new { error = "query is required" });

        return Ok(await _graphRagService.QueryAsync(request, cancellationToken).ConfigureAwait(false));
    }
}

#pragma warning restore CS1591
