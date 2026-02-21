using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-009 / TR-MCP-WS-004: Workspace registration, initialization, and process lifecycle endpoints.
/// All endpoints require a valid API key via the <c>X-Api-Key</c> header (or <c>api_key</c> query parameter).
/// Set <c>Mcp:ApiKey</c> in configuration to enable; when empty, endpoints are open.
/// </summary>
[ApiController]
[Route("mcp/workspace")]
[ApiKeyAuthFilter]
public sealed class WorkspaceController : ControllerBase
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IWorkspaceProcessManager _processManager;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceController"/> class.</summary>
    public WorkspaceController(IWorkspaceService workspaceService, IWorkspaceProcessManager processManager)
    {
        _workspaceService = workspaceService;
        _processManager = processManager;
    }

    /// <summary>List all registered workspaces. This endpoint is publicly accessible.</summary>
    [HttpGet]
    [SkipApiKeyAuth]
    public async Task<ActionResult<WorkspaceListResult>> ListAsync(CancellationToken ct)
    {
        var result = await _workspaceService.ListAsync(ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Get a single workspace by Base64URL-encoded path key. This endpoint is publicly accessible.</summary>
    [HttpGet("{key}")]
    [SkipApiKeyAuth]
    public async Task<ActionResult<WorkspaceDto>> GetAsync(string key, CancellationToken ct)
    {
        var path = DecodeKey(key);
        if (path is null)
            return BadRequest(new { error = "Invalid workspace key." });

        var dto = await _workspaceService.GetAsync(path, ct).ConfigureAwait(false);
        if (dto is null)
            return NotFound(new { error = $"Workspace not found." });
        return Ok(dto);
    }

    /// <summary>Create (register) a new workspace.</summary>
    [HttpPost]
    public async Task<ActionResult<WorkspaceMutationResult>> CreateAsync(
        [FromBody] WorkspaceCreateRequest? request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new WorkspaceMutationResult(false, "Request body is required."));

        var result = await _workspaceService.CreateAsync(request, ct).ConfigureAwait(false);
        if (!result.Success)
            return Conflict(result);

        // Auto-initialize and start the workspace instance immediately.
        await _workspaceService.InitAsync(request.WorkspacePath, ct).ConfigureAwait(false);
        var workspace = await _workspaceService.GetAsync(request.WorkspacePath, ct).ConfigureAwait(false);
        if (workspace is not null)
            await _processManager.StartAsync(request.WorkspacePath, workspace.WorkspacePort, ct).ConfigureAwait(false);

        var key = EncodeKey(request.WorkspacePath);
        return Created(new Uri($"/mcp/workspace/{key}", UriKind.Relative), result);
    }

    /// <summary>Update a workspace by Base64URL-encoded path key.</summary>
    [HttpPut("{key}")]
    public async Task<ActionResult<WorkspaceMutationResult>> UpdateAsync(
        string key,
        [FromBody] WorkspaceUpdateRequest? request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new WorkspaceMutationResult(false, "Request body is required."));

        var path = DecodeKey(key);
        if (path is null)
            return BadRequest(new WorkspaceMutationResult(false, "Invalid workspace key."));

        var result = await _workspaceService.UpdateAsync(path, request, ct).ConfigureAwait(false);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>Delete a workspace registration by Base64URL-encoded path key.</summary>
    [HttpDelete("{key}")]
    public async Task<ActionResult<WorkspaceMutationResult>> DeleteAsync(string key, CancellationToken ct)
    {
        var path = DecodeKey(key);
        if (path is null)
            return BadRequest(new WorkspaceMutationResult(false, "Invalid workspace key."));

        // Stop the process if running.
        await _processManager.StopAsync(path, ct).ConfigureAwait(false);

        var result = await _workspaceService.DeleteAsync(path, ct).ConfigureAwait(false);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>Initialize data files in a workspace (scaffold dirs, todo.yaml, mcp.db).</summary>
    [HttpPost("{key}/init")]
    public async Task<ActionResult<WorkspaceInitResult>> InitAsync(string key, CancellationToken ct)
    {
        var path = DecodeKey(key);
        if (path is null)
            return BadRequest(new WorkspaceInitResult(false, "Invalid workspace key."));

        var result = await _workspaceService.InitAsync(path, ct).ConfigureAwait(false);
        if (!result.Success)
            return UnprocessableEntity(result);

        return Ok(result);
    }

    /// <summary>Start the hosted MCP instance for a workspace.</summary>
    [HttpPost("{key}/start")]
    public async Task<ActionResult<WorkspaceProcessStatus>> StartAsync(string key, CancellationToken ct)
    {
        var path = DecodeKey(key);
        if (path is null)
            return BadRequest(new WorkspaceProcessStatus(false, Error: "Invalid workspace key."));

        var workspace = await _workspaceService.GetAsync(path, ct).ConfigureAwait(false);
        if (workspace is null)
            return NotFound(new WorkspaceProcessStatus(false, Error: "Workspace not found."));

        var status = await _processManager.StartAsync(path, workspace.WorkspacePort, ct).ConfigureAwait(false);
        return Ok(status);
    }

    /// <summary>Stop the hosted MCP instance for a workspace.</summary>
    [HttpPost("{key}/stop")]
    public async Task<ActionResult<WorkspaceProcessStatus>> StopAsync(string key, CancellationToken ct)
    {
        var path = DecodeKey(key);
        if (path is null)
            return BadRequest(new WorkspaceProcessStatus(false, Error: "Invalid workspace key."));

        var status = await _processManager.StopAsync(path, ct).ConfigureAwait(false);
        return Ok(status);
    }

    /// <summary>Get the process status of a workspace instance. This endpoint is publicly accessible.</summary>
    [HttpGet("{key}/status")]
    [SkipApiKeyAuth]
    public ActionResult<WorkspaceProcessStatus> GetStatus(string key)
    {
        var path = DecodeKey(key);
        if (path is null)
            return BadRequest(new WorkspaceProcessStatus(false, Error: "Invalid workspace key."));

        var status = _processManager.GetStatus(path);
        return Ok(status);
    }

    // Base64URL encode a workspace path for use as a URL key.
    private static string EncodeKey(string path)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(path.Trim());
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    // Decode a Base64URL key back to a workspace path.
    private static string? DecodeKey(string key)
    {
        try
        {
            var base64 = key.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            var bytes = Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }
}
