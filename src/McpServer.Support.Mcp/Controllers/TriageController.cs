using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-TRIAGE-001..003: REST API for incidental bug triage intake, group status,
/// flush, and retry operations.
/// </summary>
[ApiController]
[Route("mcpserver/triage")]
public sealed class TriageController : ControllerBase
{
    private readonly ITriageService _triageService;
    private readonly McpDatabaseRuntimeOptions? _databaseRuntimeOptions;
    private readonly ILogger<TriageController> _logger;

    /// <summary>Initializes a new instance of the <see cref="TriageController"/> class.</summary>
    public TriageController(
        ITriageService triageService,
        McpDatabaseRuntimeOptions? databaseRuntimeOptions = null,
        ILogger<TriageController>? logger = null)
    {
        _triageService = triageService ?? throw new ArgumentNullException(nameof(triageService));
        _databaseRuntimeOptions = databaseRuntimeOptions;
        _logger = logger ?? NullLogger<TriageController>.Instance;
    }

    /// <summary>FR-MCP-TRIAGE-001: Submit an incidental bug report and return accepted queue state.</summary>
    [HttpPost("reports")]
    public async Task<ActionResult<TriageReportSubmitResult>> SubmitReportAsync(
        [FromBody] TriageReportRequest? request,
        CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(SubmitReportAsync));
        if (request is null)
            return BadRequest(new TriageReportSubmitResult { Success = false, Error = "Request body is required." });

        var result = await _triageService.SubmitReportAsync(request, cancellationToken).ConfigureAwait(false);
        return result.Success
            ? Accepted(result)
            : BadRequest(result);
    }

    /// <summary>FR-MCP-TRIAGE-001: Get a submitted triage report by id.</summary>
    [HttpGet("reports/{id}")]
    public async Task<ActionResult<TriageReportDetail>> GetReportAsync(string id, CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(GetReportAsync));
        try
        {
            return Ok(await _triageService.GetReportAsync(id, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>FR-MCP-TRIAGE-002: Query triage groups by optional status and workspace.</summary>
    [HttpGet("groups")]
    public async Task<ActionResult<TriageGroupQueryResult>> QueryGroupsAsync(
        [FromQuery] string? status,
        [FromQuery] string? workspacePath,
        CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(QueryGroupsAsync));
        return Ok(await _triageService.QueryGroupsAsync(status, workspacePath, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>FR-TRIAGE-001: Get triage queue, report-group queue, and AI run history dashboard state.</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<TriageDashboardResult>> GetDashboardAsync(
        [FromQuery] string? workspacePath,
        CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(GetDashboardAsync));
        return Ok(await _triageService.GetDashboardAsync(workspacePath, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>FR-MCP-TRIAGE-002: Get a triage group by id.</summary>
    [HttpGet("groups/{id}")]
    public async Task<ActionResult<TriageGroupDetail>> GetGroupAsync(string id, CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(GetGroupAsync));
        try
        {
            return Ok(await _triageService.GetGroupAsync(id, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>FR-TRIAGE-001: Query AI triage research runs by optional status, group, and workspace filters.</summary>
    [HttpGet("runs")]
    public async Task<ActionResult<TriageRunQueryResult>> QueryRunsAsync(
        [FromQuery] string? status,
        [FromQuery] string? groupId,
        [FromQuery] string? workspacePath,
        CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(QueryRunsAsync));
        return Ok(await _triageService.QueryRunsAsync(status, groupId, workspacePath, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>FR-TRIAGE-001: Get AI triage research run detail by id.</summary>
    [HttpGet("runs/{id}")]
    public async Task<ActionResult<TriageResearchRunDetail>> GetRunAsync(string id, CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(GetRunAsync));
        try
        {
            return Ok(await _triageService.GetRunAsync(id, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>FR-TRIAGE-002: Query TODO ids created by triage with persisted creation timestamps.</summary>
    [HttpGet("todos")]
    public async Task<ActionResult<TriageCreatedTodoQueryResult>> QueryCreatedTodosAsync(
        [FromQuery] string? workspacePath,
        CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(QueryCreatedTodosAsync));
        return Ok(await _triageService.QueryCreatedTodosAsync(workspacePath, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>FR-MCP-TRIAGE-002: Flush a group so it is eligible for immediate research.</summary>
    [HttpPost("groups/{id}/flush")]
    public async Task<ActionResult<TriageGroupDetail>> FlushGroupAsync(string id, CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(FlushGroupAsync));
        try
        {
            return Ok(await _triageService.FlushGroupAsync(id, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>FR-MCP-TRIAGE-002: Retry a failed triage group.</summary>
    [HttpPost("groups/{id}/retry")]
    public async Task<ActionResult<TriageGroupDetail>> RetryGroupAsync(
        string id,
        [FromQuery] bool force,
        CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(RetryGroupAsync));
        try
        {
            return Ok(await _triageService.RetryGroupAsync(id, force, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>FR-MCP-TRIAGE-005: Soft-delete a triage group and its reports.</summary>
    [HttpDelete("groups/{id}")]
    public async Task<ActionResult<TriageGroupDeleteResult>> DeleteGroupAsync(
        string id,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(DeleteGroupAsync));
        try
        {
            return Ok(await _triageService.DeleteGroupAsync(id, reason, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>FR-TRIAGE-003: Create a new triage group from selected reports and groups.</summary>
    [HttpPost("groups/new")]
    public async Task<ActionResult<TriageGroupEditResult>> CreateGroupFromSelectionAsync(
        [FromBody] TriageGroupSelectionRequest? request,
        CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(CreateGroupFromSelectionAsync));
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        try
        {
            return Ok(await _triageService.CreateGroupFromSelectionAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>FR-TRIAGE-003: Move selected reports and groups into an existing triage group.</summary>
    [HttpPost("groups/{id}/consolidate")]
    public async Task<ActionResult<TriageGroupEditResult>> ConsolidateIntoGroupAsync(
        string id,
        [FromBody] TriageGroupSelectionRequest? request,
        CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(ConsolidateIntoGroupAsync));
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        try
        {
            return Ok(await _triageService.ConsolidateIntoGroupAsync(id, request, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>FR-TRIAGE-003: Merge selected source groups into an existing target triage group.</summary>
    [HttpPost("groups/{id}/merge")]
    public async Task<ActionResult<TriageGroupEditResult>> MergeGroupsAsync(
        string id,
        [FromBody] TriageGroupSelectionRequest? request,
        CancellationToken cancellationToken)
    {
        LogDatabaseConnectionString(nameof(MergeGroupsAsync));
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        try
        {
            return Ok(await _triageService.MergeGroupsAsync(id, request, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private void LogDatabaseConnectionString(string operation)
    {
        if (_databaseRuntimeOptions is null)
            return;

        var providerOptions = _databaseRuntimeOptions.ProviderOptions;
        _logger.LogInformation(
            "Triage request {Operation} using database provider {ProviderName} with exact connection string {ConnectionString}",
            operation,
            providerOptions.ProviderName,
            providerOptions.ConnectionString);
    }
}
