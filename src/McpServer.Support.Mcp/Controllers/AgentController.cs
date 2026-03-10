using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// REST API controller for agent management. Exposed on the primary instance only.
/// Management endpoints (mutations) require JWT Bearer authentication.
/// Read endpoints use standard workspace API key auth.
/// </summary>
[ApiController]
[Route("mcpserver/agents")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<AgentController> _logger;

    /// <summary>Initializes a new instance of <see cref="AgentController"/>.</summary>
    public AgentController(IAgentService agentService,
        ILogger<AgentController> logger)
    {
        _logger = logger;
        _agentService = agentService;
    }

    // --- Agent Definitions (global) ---

    /// <summary>List all agent type definitions.</summary>
    [HttpGet("definitions")]
    public async Task<ActionResult<AgentDefinitionListResult>> ListDefinitions(CancellationToken ct)
        => Ok(await _agentService.ListDefinitionsAsync(ct).ConfigureAwait(false));

    /// <summary>Get a specific agent type definition.</summary>
    [HttpGet("definitions/{agentType}")]
    public async Task<ActionResult<AgentDefinitionDto>> GetDefinition(string agentType, CancellationToken ct)
    {
        var result = await _agentService.GetDefinitionAsync(agentType, ct).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create or update an agent type definition. Requires JWT auth.</summary>
    [HttpPost("definitions")]
    [Authorize(Policy = "AgentManager")]
    public async Task<ActionResult<AgentMutationResult>> UpsertDefinition([FromBody] AgentDefinitionRequest request, CancellationToken ct)
    {
        if (request is null) return BadRequest("Request body is required.");
        var result = await _agentService.UpsertDefinitionAsync(request, ct).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete an agent type definition. Requires JWT auth.</summary>
    [HttpDelete("definitions/{agentType}")]
    [Authorize(Policy = "AgentManager")]
    public async Task<ActionResult<AgentMutationResult>> DeleteDefinition(string agentType, CancellationToken ct)
    {
        var result = await _agentService.DeleteDefinitionAsync(agentType, ct).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Seed built-in agent defaults. Requires JWT auth.</summary>
    [HttpPost("definitions/seed")]
    [Authorize(Policy = "AgentManager")]
    public async Task<ActionResult> SeedDefaults(CancellationToken ct)
    {
        var count = await _agentService.SeedBuiltInDefaultsAsync(ct).ConfigureAwait(false);
        return Ok(new { seeded = count });
    }

    // --- Workspace Agent Configurations ---

    /// <summary>List agents configured for this workspace.</summary>
    [HttpGet]
    public async Task<ActionResult<AgentWorkspaceListResult>> ListWorkspaceAgents(
        [FromQuery] string? workspace, CancellationToken ct)
    {
        var workspacePath = ResolveWorkspacePath(workspace);
        if (workspacePath is null) return BadRequest("Workspace path required.");
        return Ok(await _agentService.ListWorkspaceAgentsAsync(workspacePath, ct).ConfigureAwait(false));
    }

    /// <summary>Get a specific agent's workspace configuration.</summary>
    [HttpGet("{agentId}")]
    public async Task<ActionResult<AgentWorkspaceConfigDto>> GetWorkspaceAgent(
        string agentId, [FromQuery] string? workspace, CancellationToken ct)
    {
        var workspacePath = ResolveWorkspacePath(workspace);
        if (workspacePath is null) return BadRequest("Workspace path required.");
        var result = await _agentService.GetWorkspaceAgentAsync(workspacePath, agentId, ct).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Add or update an agent in a workspace. Requires JWT auth.</summary>
    [HttpPost("{agentId}")]
    [Authorize(Policy = "AgentManager")]
    public async Task<ActionResult<AgentMutationResult>> UpsertWorkspaceAgent(
        string agentId, [FromBody] AgentWorkspaceRequest request, [FromQuery] string? workspace, CancellationToken ct)
    {
        var workspacePath = ResolveWorkspacePath(workspace);
        if (workspacePath is null) return BadRequest("Workspace path required.");
        if (request is null) return BadRequest("Request body is required.");
        var effectiveRequest = request with { AgentId = agentId };
        var result = await _agentService.UpsertWorkspaceAgentAsync(workspacePath, effectiveRequest, ct).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Remove an agent from a workspace. Requires JWT auth.</summary>
    [HttpDelete("{agentId}")]
    [Authorize(Policy = "AgentManager")]
    public async Task<ActionResult<AgentMutationResult>> DeleteWorkspaceAgent(
        string agentId, [FromQuery] string? workspace, CancellationToken ct)
    {
        var workspacePath = ResolveWorkspacePath(workspace);
        if (workspacePath is null) return BadRequest("Workspace path required.");
        var result = await _agentService.DeleteWorkspaceAgentAsync(workspacePath, agentId, ct).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Ban an agent. Requires JWT auth.</summary>
    [HttpPost("{agentId}/ban")]
    [Authorize(Policy = "AgentManager")]
    public async Task<ActionResult<AgentMutationResult>> BanAgent(
        string agentId, [FromBody] AgentBanRequest request, [FromQuery] string? workspace, CancellationToken ct)
    {
        if (request is null) return BadRequest("Request body is required.");
        var workspacePath = request.Global ? null : ResolveWorkspacePath(workspace);
        if (!request.Global && workspacePath is null) return BadRequest("Workspace path required for non-global ban.");
        var result = await _agentService.BanAgentAsync(agentId, request, workspacePath, ct).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Unban an agent. Requires JWT auth.</summary>
    [HttpPost("{agentId}/unban")]
    [Authorize(Policy = "AgentManager")]
    public async Task<ActionResult<AgentMutationResult>> UnbanAgent(
        string agentId, [FromQuery] string? workspace, [FromQuery] bool global = false, CancellationToken ct = default)
    {
        var workspacePath = global ? null : ResolveWorkspacePath(workspace);
        if (!global && workspacePath is null) return BadRequest("Workspace path required for non-global unban.");
        var result = await _agentService.UnbanAgentAsync(agentId, workspacePath, ct).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // --- Runtime Process Lifecycle ---

    /// <summary>Launches a configured agent runtime for the current workspace.</summary>
    [HttpPost("{agentId}/launch")]
    [Authorize(Policy = "AgentManager")]
    public async Task<ActionResult<AgentProcessInfo>> LaunchAgent(
        string agentId,
        [FromQuery] string? workspace,
        CancellationToken ct)
    {
        var workspacePath = ResolveWorkspacePath(workspace);
        if (workspacePath is null) return BadRequest("Workspace path required.");

        try
        {
            var result = await _agentService.LaunchAgentAsync(workspacePath, agentId, ct).ConfigureAwait(false);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not configured", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already running", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Stops a running configured agent runtime for the current workspace.</summary>
    [HttpPost("{agentId}/stop")]
    [Authorize(Policy = "AgentManager")]
    public async Task<ActionResult> StopAgent(
        string agentId,
        [FromQuery] string? workspace,
        CancellationToken ct)
    {
        var workspacePath = ResolveWorkspacePath(workspace);
        if (workspacePath is null) return BadRequest("Workspace path required.");

        try
        {
            var stopped = await _agentService.StopAgentAsync(workspacePath, agentId, ct).ConfigureAwait(false);
            return stopped ? Ok(new { success = true }) : Conflict(new { error = $"Agent '{agentId}' is not currently running." });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not configured", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Gets runtime process status for the specified workspace agent.</summary>
    [HttpGet("{agentId}/process-status")]
    public async Task<ActionResult<AgentProcessInfo>> GetProcessStatus(
        string agentId,
        [FromQuery] string? workspace,
        CancellationToken ct)
    {
        var workspacePath = ResolveWorkspacePath(workspace);
        if (workspacePath is null) return BadRequest("Workspace path required.");

        var status = await _agentService.GetAgentProcessStatusAsync(workspacePath, agentId, ct).ConfigureAwait(false);
        return status is null ? NotFound() : Ok(status);
    }

    /// <summary>Lists running agent runtimes, optionally filtered to a workspace.</summary>
    [HttpGet("running")]
    public async Task<ActionResult> ListRunningAgents([FromQuery] string? workspace, CancellationToken ct)
    {
        var workspacePath = ResolveWorkspacePath(workspace);
        var items = await _agentService.ListRunningAgentsAsync(workspacePath, ct).ConfigureAwait(false);
        return Ok(new { agents = items });
    }

    // --- Lifecycle Events ---

    /// <summary>Log an agent lifecycle event.</summary>
    [HttpPost("{agentId}/events")]
    [Authorize(Policy = "AgentManager")]
    public async Task<ActionResult<AgentMutationResult>> LogEvent(
        string agentId, [FromBody] AgentEventRequest request, [FromQuery] string? workspace, CancellationToken ct)
    {
        var workspacePath = ResolveWorkspacePath(workspace);
        if (workspacePath is null) return BadRequest("Workspace path required.");
        if (request is null) return BadRequest("Request body is required.");
        var effectiveRequest = request with { AgentId = agentId };
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("preferred_username")?.Value;
        var result = await _agentService.LogEventAsync(workspacePath, effectiveRequest, userId, ct).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get event history for an agent in a workspace.</summary>
    [HttpGet("{agentId}/events")]
    public async Task<ActionResult<AgentEventListResult>> GetEvents(
        string agentId, [FromQuery] string? workspace, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var workspacePath = ResolveWorkspacePath(workspace);
        if (workspacePath is null) return BadRequest("Workspace path required.");
        return Ok(await _agentService.GetEventsAsync(workspacePath, agentId, limit, ct).ConfigureAwait(false));
    }

    /// <summary>Validate the agents.yaml file for a workspace.</summary>
    [HttpGet("validate")]
    public ActionResult Validate([FromQuery] string? workspace)
    {
        var workspacePath = ResolveWorkspacePath(workspace);
        if (workspacePath is null) return BadRequest("Workspace path required.");

        var agentsYamlPath = Path.Combine(workspacePath, "agents.yaml");
        if (!System.IO.File.Exists(agentsYamlPath))
            return Ok(new { valid = false, error = "agents.yaml not found", path = agentsYamlPath });

        try
        {
            var content = System.IO.File.ReadAllText(agentsYamlPath);
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            deserializer.Deserialize<object>(content);
            return Ok(new { valid = true, path = agentsYamlPath });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Ok(new { valid = false, error = ex.Message, path = agentsYamlPath });
        }
    }

    // --- Helpers ---

    private string? ResolveWorkspacePath(string? workspace)
    {
        if (!string.IsNullOrWhiteSpace(workspace))
            return workspace;

        return HttpContext.Items.TryGetValue("WorkspacePath", out var wp) ? wp as string : null;
    }
}
