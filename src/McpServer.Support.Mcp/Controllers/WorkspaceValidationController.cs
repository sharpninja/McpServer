using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>FR-MCP-HYGIENE-005: REST workspace validation. Read-only. No repair.</summary>
[ApiController]
[Route("mcpserver/workspace-validation")]
public sealed class WorkspaceValidationController : ControllerBase
{
    private readonly IWorkspaceValidationService _service;

    /// <summary>TR-MCP-HYGIENE-005: Constructor.</summary>
    public WorkspaceValidationController(IWorkspaceValidationService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>FR-MCP-HYGIENE-001: Run read-only hygiene validation.</summary>
    [HttpPost("validate")]
    public Task<WorkspaceValidationResult> ValidateAsync([FromBody] WorkspaceValidationRequest request, CancellationToken cancellationToken)
        => _service.ValidateAsync(request, cancellationToken);
}
