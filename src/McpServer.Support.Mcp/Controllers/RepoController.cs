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
