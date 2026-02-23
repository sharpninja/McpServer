using System.Text.Json;
using System.Text.Json.Nodes;
using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly IOptionsMonitor<MarkerPromptOptions> _promptOptions;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceController"/> class.</summary>
    public WorkspaceController(
        IWorkspaceService workspaceService,
        IWorkspaceProcessManager processManager,
        IConfiguration configuration,
        IWebHostEnvironment env,
        IOptionsMonitor<MarkerPromptOptions> promptOptions)
    {
        _workspaceService = workspaceService;
        _processManager = processManager;
        _configuration = configuration;
        _env = env;
        _promptOptions = promptOptions;
    }

    /// <summary>
    /// List all registered workspaces. This endpoint is publicly accessible.
    /// Each workspace includes <c>isPrimary</c> and <c>isEnabled</c> flags.
    /// The primary workspace is served by the host process itself (no child app).
    /// Disabled workspaces are skipped during auto-start.
    /// </summary>
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

    /// <summary>
    /// Create (register) a new workspace.
    /// Set <c>isPrimary</c> to mark this workspace as the primary instance (served by the host process;
    /// no child app is spun up). Set <c>isEnabled</c> to false to register without auto-starting.
    /// If no workspace is marked primary, the enabled workspace with the lowest port is used.
    /// </summary>
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
            await _processManager.StartAsync(request.WorkspacePath, workspace.WorkspacePort, ct,
                workspace.DataDirectory, workspace.PromptTemplate).ConfigureAwait(false);

        var key = EncodeKey(request.WorkspacePath);
        return Created(new Uri($"/mcp/workspace/{key}", UriKind.Relative), result);
    }

    /// <summary>
    /// Update a workspace by Base64URL-encoded path key.
    /// Supports updating <c>isPrimary</c> (bool) and <c>isEnabled</c> (bool) flags.
    /// Null fields are not changed.
    /// </summary>
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

        // Regenerate marker files when the workspace prompt template changes.
        if (request.PromptTemplate is not null)
            await _processManager.RegenerateAllMarkersAsync(ct).ConfigureAwait(false);

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

    /// <summary>
    /// Start the hosted MCP instance for a workspace.
    /// If the workspace is the primary instance, only writes the marker file — the host process already serves it.
    /// Returns 404 if the workspace is not registered. Disabled workspaces can still be started manually.
    /// </summary>
    [HttpPost("{key}/start")]
    public async Task<ActionResult<WorkspaceProcessStatus>> StartAsync(string key, CancellationToken ct)
    {
        var path = DecodeKey(key);
        if (path is null)
            return BadRequest(new WorkspaceProcessStatus(false, Error: "Invalid workspace key."));

        var workspace = await _workspaceService.GetAsync(path, ct).ConfigureAwait(false);
        if (workspace is null)
            return NotFound(new WorkspaceProcessStatus(false, Error: "Workspace not found."));

        var status = await _processManager.StartAsync(path, workspace.WorkspacePort, ct,
            workspace.DataDirectory, workspace.PromptTemplate).ConfigureAwait(false);
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

    /// <summary>
    /// Get the global marker prompt template. Only available on the primary workspace.
    /// Returns the configured template, or the built-in default when none is configured.
    /// </summary>
    [HttpGet("prompt")]
    [SkipApiKeyAuth]
    public async Task<ActionResult<GlobalPromptResult>> GetGlobalPromptAsync(CancellationToken ct)
    {
        var primary = await FindPrimaryWorkspaceAsync(ct).ConfigureAwait(false);
        if (primary is null)
            return NotFound(new { error = "No primary workspace configured." });

        // Only the primary workspace may serve this endpoint.
        if (!IsPrimaryInstance(primary))
            return StatusCode(403, new { error = "Global prompt is only available on the primary workspace." });

        var template = _promptOptions.CurrentValue.MarkerPromptTemplate;
        var isDefault = string.IsNullOrWhiteSpace(template);
        return Ok(new GlobalPromptResult(
            Template: isDefault ? MarkerFileService.DefaultPromptTemplate : template!,
            IsDefault: isDefault));
    }

    /// <summary>
    /// Update the global marker prompt template. Only available on the primary workspace.
    /// Send an empty or null <c>template</c> to revert to the built-in default.
    /// The template supports <c>{baseUrl}</c> placeholder for runtime substitution.
    /// </summary>
    [HttpPut("prompt")]
    public async Task<ActionResult<GlobalPromptResult>> UpdateGlobalPromptAsync(
        [FromBody] GlobalPromptUpdateRequest? request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        var primary = await FindPrimaryWorkspaceAsync(ct).ConfigureAwait(false);
        if (primary is null)
            return NotFound(new { error = "No primary workspace configured." });

        if (!IsPrimaryInstance(primary))
            return StatusCode(403, new { error = "Global prompt is only available on the primary workspace." });

        var newTemplate = string.IsNullOrWhiteSpace(request.Template) ? null : request.Template.Trim();

        // Persist to appsettings.json using the same atomic JSON patching as WorkspaceService.
        var appsettingsPath = ResolveAppsettingsPath();
        var jsonText = await System.IO.File.ReadAllTextAsync(appsettingsPath, ct).ConfigureAwait(false);
        var doc = JsonNode.Parse(jsonText, new JsonNodeOptions { PropertyNameCaseInsensitive = true })!;
        var mcp = doc["Mcp"] as JsonObject ?? new JsonObject();

        if (newTemplate is null)
            mcp.Remove("MarkerPromptTemplate");
        else
            mcp["MarkerPromptTemplate"] = newTemplate;

        doc["Mcp"] = mcp;
        await System.IO.File.WriteAllTextAsync(appsettingsPath, doc.ToJsonString(s_jsonOptions), ct).ConfigureAwait(false);

        if (_configuration is IConfigurationRoot root)
            root.Reload();

        // Regenerate all marker files so running workspaces pick up the new global prompt.
        // Pass the new template explicitly to avoid IOptionsMonitor staleness after reload.
        await _processManager.RegenerateAllMarkersAsync(ct, globalPromptOverride: newTemplate ?? string.Empty).ConfigureAwait(false);

        var isDefault = newTemplate is null;
        return Ok(new GlobalPromptResult(
            Template: isDefault ? MarkerFileService.DefaultPromptTemplate : newTemplate!,
            IsDefault: isDefault));
    }

    private async Task<WorkspaceDto?> FindPrimaryWorkspaceAsync(CancellationToken ct)
    {
        var list = await _workspaceService.ListAsync(ct).ConfigureAwait(false);
        return list.Items
            .Where(w => w.IsPrimary && w.IsEnabled)
            .OrderBy(w => w.WorkspacePort)
            .FirstOrDefault()
            ?? list.Items
                .Where(w => w.IsEnabled)
                .OrderBy(w => w.WorkspacePort)
                .FirstOrDefault();
    }

    private bool IsPrimaryInstance(WorkspaceDto primary)
    {
        // Check if this process is the one serving the primary workspace by comparing ports.
        var listeningUrls = HttpContext.Connection.LocalPort;
        return primary.WorkspacePort == listeningUrls;
    }

    /// <summary>
    /// Resolves the path to <c>appsettings.json</c>, falling back to the application base directory
    /// when the file does not exist under the content root path
    /// (which may point to a workspace root rather than the install directory).
    /// </summary>
    private string ResolveAppsettingsPath()
    {
        var fromContentRoot = Path.Combine(_env.ContentRootPath, "appsettings.json");
        if (System.IO.File.Exists(fromContentRoot)) return fromContentRoot;

        var fromBaseDir = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (System.IO.File.Exists(fromBaseDir)) return fromBaseDir;

        return fromContentRoot;
    }

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

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
