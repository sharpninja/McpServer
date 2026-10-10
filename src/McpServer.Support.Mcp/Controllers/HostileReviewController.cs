using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>FR-MCP-HOSTILEREVIEW-006: REST submit/status/get/query only. No repair or apply.</summary>
[ApiController]
[Route("mcpserver/hostile-review")]
public sealed class HostileReviewController : ControllerBase
{
    private readonly IHostileReviewService _service;

    /// <summary>TR-MCP-HOSTILEREVIEW-006: Constructor.</summary>
    public HostileReviewController(IHostileReviewService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>FR-MCP-HOSTILEREVIEW-001: Submit a bounded review request.</summary>
    [HttpPost("submit")]
    public Task<HostileReviewResult> SubmitAsync([FromBody] HostileReviewSubmitRequest request, CancellationToken cancellationToken)
        => _service.SubmitAsync(request, cancellationToken);

    /// <summary>FR-MCP-HOSTILEREVIEW-006: Read-only status.</summary>
    [HttpGet("{requestId}/status")]
    public Task<HostileReviewResult> StatusAsync(string requestId, CancellationToken cancellationToken)
        => _service.StatusAsync(requestId, cancellationToken);

    /// <summary>FR-MCP-HOSTILEREVIEW-004: Read-only get.</summary>
    [HttpGet("{requestId}")]
    public Task<HostileReviewResult> GetAsync(string requestId, CancellationToken cancellationToken)
        => _service.GetAsync(requestId, cancellationToken);

    /// <summary>FR-MCP-HOSTILEREVIEW-005: AND-filtered query.</summary>
    [HttpPost("query")]
    public Task<IReadOnlyList<HostileReviewResult>> QueryAsync([FromBody] HostileReviewQueryRequest request, CancellationToken cancellationToken)
        => _service.QueryAsync(request, cancellationToken);
}
