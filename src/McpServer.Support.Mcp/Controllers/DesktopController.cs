using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-047/TR-MCP-DESKTOP-001: Desktop process launch endpoint for authenticated
/// workspace-scoped HTTP clients and hosted agents.
/// </summary>
[ApiController]
[Route("mcpserver/desktop")]
public sealed class DesktopController : ControllerBase
{
    private readonly DesktopLaunchService _desktopLaunchService;
    private readonly WorkspaceContext _workspaceContext;

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Initializes the controller with the shared
    /// desktop-launch service and resolved workspace context.
    /// </summary>
    /// <param name="desktopLaunchService">Service that invokes the launcher executable.</param>
    /// <param name="workspaceContext">Resolved workspace context for the current request.</param>
    public DesktopController(
        DesktopLaunchService desktopLaunchService,
        WorkspaceContext workspaceContext)
    {
        _desktopLaunchService = desktopLaunchService ?? throw new ArgumentNullException(nameof(desktopLaunchService));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
    }

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Launches a local process on the interactive desktop for
    /// the currently resolved workspace.
    /// </summary>
    /// <param name="request">Structured desktop-launch request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The normalized launch result returned by the shared desktop-launch service.</returns>
    [HttpPost("launch")]
    public async Task<ActionResult<DesktopLaunchResult>> LaunchAsync(
        [FromBody] DesktopLaunchRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });
        if (string.IsNullOrWhiteSpace(_workspaceContext.WorkspacePath))
            return NotFound(new { error = "Workspace could not be resolved." });

        var result = await _desktopLaunchService
            .LaunchAsync(_workspaceContext.WorkspacePath, request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(result);
    }
}
