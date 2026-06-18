using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// TR-PLANNED-013: Repository file read/write and list (repo.read, repo.write, repo.list).
/// FR-SUPPORT-010: Path allowlist enforced; audit log for writes.
/// </summary>
[ApiController]
[Route("mcpserver/repo")]
public sealed class RepoController : ControllerBase
{
    private readonly IRepoFileService _repoFileService;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    public RepoController(IRepoFileService repoFileService)
    {
        _repoFileService = repoFileService;
    }

    /// <summary>TR-PLANNED-013: Read file contents (repo.read).</summary>
    /// <param name="path">Relative path from repo root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("file")]
    public async Task<ActionResult<object>> ReadFileAsync([FromQuery] string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest(new { error = "path is required" });
        var result = await _repoFileService.ReadAsync(path, cancellationToken).ConfigureAwait(false);
        if (result == null)
            return BadRequest(new { error = "path not allowed or not found" });
        return Ok(new { path = result.RelativePath, content = result.Content, exists = result.Exists });
    }

    /// <summary>TR-PLANNED-013: Write file contents (repo.write).</summary>
    /// <param name="request">Path and content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("file")]
    public async Task<ActionResult<object>> WriteFileAsync([FromBody] RepoWriteRequest? request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Path))
            return BadRequest(new { error = "path and content are required" });
        var result = await _repoFileService.WriteAsync(request.Path, request.Content ?? string.Empty, cancellationToken).ConfigureAwait(false);
        if (!result.Written)
            return BadRequest(new { error = result.Error ?? "write failed" });
        return Ok(new { path = request.Path, written = true });
    }

    /// <summary>FR-MCP-QBTOOLS-006: Apply a targeted string replacement to a file (repo.edit).</summary>
    /// <param name="request">Path, oldString, newString, and edit options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("edit")]
    public async Task<ActionResult<object>> EditFileAsync([FromBody] RepoEditRequest? request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Path))
            return BadRequest(new { error = "path is required" });
        if (request.OldString is null || request.NewString is null)
            return BadRequest(new { error = "oldString and newString are required" });

        var result = await _repoFileService.EditAsync(
            request.Path,
            request.OldString,
            request.NewString,
            request.ReplaceAll,
            request.ExpectedOccurrences,
            cancellationToken).ConfigureAwait(false);

        // Return 200 with a structured result (written + replacements + error) so the agent edit_file tool reasons
        // over the outcome rather than handling an HTTP error for an expected miss (ambiguous/not-found).
        return Ok(new { path = request.Path, written = result.Written, replacements = result.Replacements, error = result.Error });
    }

    /// <summary>TR-PLANNED-013: List files/directories (repo.list).</summary>
    /// <param name="path">Relative path from repo root (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("list")]
    public async Task<ActionResult<object>> ListAsync([FromQuery] string? path, CancellationToken cancellationToken)
    {
        var result = await _repoFileService.ListAsync(path, cancellationToken).ConfigureAwait(false);
        return Ok(new
        {
            path = result.Path,
            entries = result.Entries.Select(e => new { e.Name, e.IsDirectory }).ToList()
        });
    }
}

/// <summary>Request for repo file write. TR-PLANNED-013.</summary>
public sealed class RepoWriteRequest
{
    /// <summary>Relative path from repo root.</summary>
    public string? Path { get; set; }

    /// <summary>File content.</summary>
    public string? Content { get; set; }
}

/// <summary>FR-MCP-QBTOOLS-006: Request for a targeted repo file edit.</summary>
public sealed class RepoEditRequest
{
    /// <summary>Relative path from repo root.</summary>
    public string? Path { get; set; }

    /// <summary>Exact text to find.</summary>
    public string? OldString { get; set; }

    /// <summary>Replacement text.</summary>
    public string? NewString { get; set; }

    /// <summary>When true, replaces every occurrence instead of requiring a unique match.</summary>
    public bool ReplaceAll { get; set; }

    /// <summary>Optional expected match-count guard.</summary>
    public int? ExpectedOccurrences { get; set; }
}
