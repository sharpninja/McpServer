using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly ILogger<DesktopController> _logger;
    private readonly IOptions<DesktopLaunchOptions> _desktopLaunchOptions;
    private readonly WorkspaceContext _workspaceContext;

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Initializes the controller with the shared
    /// desktop-launch service and resolved workspace context.
    /// </summary>
    /// <param name="desktopLaunchService">Service that invokes the launcher executable.</param>
    /// <param name="desktopLaunchOptions">Privileged desktop-launch configuration.</param>
    /// <param name="logger">Logger used for denied desktop-launch requests.</param>
    /// <param name="workspaceContext">Resolved workspace context for the current request.</param>
    public DesktopController(
        DesktopLaunchService desktopLaunchService,
        IOptions<DesktopLaunchOptions> desktopLaunchOptions,
        ILogger<DesktopController> logger,
        WorkspaceContext workspaceContext)
    {
        _desktopLaunchService = desktopLaunchService ?? throw new ArgumentNullException(nameof(desktopLaunchService));
        _desktopLaunchOptions = desktopLaunchOptions ?? throw new ArgumentNullException(nameof(desktopLaunchOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        if (!HasAuthorizedDesktopLaunchToken())
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { error = $"Desktop launch requires the configured {DesktopLaunchOptions.AccessTokenHeaderName} header." });
        }

        var result = await _desktopLaunchService
            .LaunchAsync(_workspaceContext.WorkspacePath, request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(result);
    }

    private bool HasAuthorizedDesktopLaunchToken()
    {
        var configuredToken = _desktopLaunchOptions.Value.AccessToken;
        if (string.IsNullOrWhiteSpace(configuredToken))
        {
            _logger.LogWarning(
                "Rejected desktop launch for workspace {WorkspacePath} because no desktop-launch access token is configured.",
                _workspaceContext.WorkspacePath);
            return false;
        }

        if (!Request.Headers.TryGetValue(DesktopLaunchOptions.AccessTokenHeaderName, out var providedValues))
        {
            _logger.LogWarning(
                "Rejected desktop launch for workspace {WorkspacePath} because the desktop-launch access token header was missing.",
                _workspaceContext.WorkspacePath);
            return false;
        }

        var providedToken = providedValues.ToString();
        if (string.IsNullOrWhiteSpace(providedToken))
        {
            _logger.LogWarning(
                "Rejected desktop launch for workspace {WorkspacePath} because the desktop-launch access token header was empty.",
                _workspaceContext.WorkspacePath);
            return false;
        }

        var configuredBytes = Encoding.UTF8.GetBytes(configuredToken);
        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        var matches = configuredBytes.Length == providedBytes.Length
                      && CryptographicOperations.FixedTimeEquals(configuredBytes, providedBytes);
        if (!matches)
        {
            _logger.LogWarning(
                "Rejected desktop launch for workspace {WorkspacePath} because the desktop-launch access token was invalid.",
                _workspaceContext.WorkspacePath);
        }

        return matches;
    }
}
