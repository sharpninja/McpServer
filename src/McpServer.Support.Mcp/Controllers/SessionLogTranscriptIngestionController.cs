using System.Globalization;
using System.IO.Compression;
using McpServer.SessionLog.Transcripts;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>HTTP endpoints for importing provider transcripts into canonical session logs.</summary>
[ApiController]
[Route("mcpserver/sessionlog/ingest")]
public sealed class SessionLogTranscriptIngestionController : ControllerBase
{
    private const long MaxUploadRequestBytes = 512L * 1024 * 1024;
    private const long MaxExpandedUploadBytes = 2L * 1024 * 1024 * 1024;
    private const long MaxSourceFileBytes = 256L * 1024 * 1024;
    private const int MaxArchiveEntries = 10_000;
    private const double MaxCompressionRatio = 20.0;

    private readonly ITranscriptIngestionService _ingestionService;
    private readonly WorkspaceContext _workspaceContext;
    private readonly ILogger<SessionLogTranscriptIngestionController> _logger;

    /// <summary>Initializes a transcript ingestion controller.</summary>
    /// <param name="ingestionService">Shared transcript ingestion service.</param>
    /// <param name="workspaceContext">Resolved workspace context for the current request.</param>
    /// <param name="logger">Controller logger.</param>
    public SessionLogTranscriptIngestionController(
        ITranscriptIngestionService ingestionService,
        WorkspaceContext workspaceContext,
        ILogger<SessionLogTranscriptIngestionController> logger)
    {
        _ingestionService = ingestionService ?? throw new ArgumentNullException(nameof(ingestionService));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Ingests transcripts from a server-local file or folder path.</summary>
    /// <param name="request">Path ingestion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Run receipt with per-session import artifacts.</returns>
    [HttpPost("path")]
    public async Task<ActionResult<TranscriptIngestRunResponse>> IngestPathAsync(
        [FromBody] TranscriptIngestPathRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });
        if (string.IsNullOrWhiteSpace(request.Path))
            return BadRequest(new { error = "Transcript path is required." });
        if (string.IsNullOrWhiteSpace(request.Agent))
            return BadRequest(new { error = "Agent is required." });
        if (string.IsNullOrWhiteSpace(_workspaceContext.WorkspacePath))
            return NotFound(new { error = "Workspace could not be resolved." });

        var compatibilityProfile = request.EmitNormalizedProfile
            ? request.CompatibilityProfile
            : TranscriptCompatibilityProfile.None;
        var ingestionRequest = new TranscriptIngestionRequest(request.Path)
        {
            SourceKind = request.Source,
            Recursive = request.Recursive,
            Strict = request.Strict,
            Persist = request.Persist,
            CompatibilityProfile = compatibilityProfile,
            Agent = request.Agent,
            WorkspacePath = _workspaceContext.WorkspacePath,
        };

        try
        {
            var result = await _ingestionService.IngestPathAsync(ingestionRequest, cancellationToken).ConfigureAwait(false);
            return BuildRunResponse(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Rejected transcript ingestion path {Path} for workspace {WorkspacePath}.", request.Path, _workspaceContext.WorkspacePath);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Ingests transcripts from multipart upload files or a ZIP bundle.</summary>
    /// <param name="request">Multipart upload request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Run receipt with per-session import artifacts.</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxUploadRequestBytes)]
    public async Task<ActionResult<TranscriptIngestRunResponse>> IngestUploadAsync(
        [FromForm] TranscriptIngestUploadRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });
        if (string.IsNullOrWhiteSpace(request.Agent))
            return BadRequest(new { error = "Agent is required." });
        if (request.Files.Count == 0)
            return BadRequest(new { error = "At least one transcript upload file is required." });
        if (string.IsNullOrWhiteSpace(_workspaceContext.WorkspacePath))
            return NotFound(new { error = "Workspace could not be resolved." });

        var agent = SanitizePathSegment(request.Agent);
        var runId = CreateUploadRunId();
        var stagingRoot = Path.Combine(_workspaceContext.WorkspacePath, ".mcpServer", agent, "transcripts", "staging", runId);
        Directory.CreateDirectory(stagingRoot);

        try
        {
            var stagedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in request.Files)
                await StageUploadFileAsync(file, stagingRoot, stagedPaths, cancellationToken).ConfigureAwait(false);

            var compatibilityProfile = request.EmitNormalizedProfile
                ? request.CompatibilityProfile
                : TranscriptCompatibilityProfile.None;
            var ingestionRequest = new TranscriptIngestionRequest(stagingRoot)
            {
                SourceKind = request.Source,
                Recursive = request.Recursive,
                Strict = request.Strict,
                Persist = request.Persist,
                CompatibilityProfile = compatibilityProfile,
                Agent = request.Agent,
                WorkspacePath = _workspaceContext.WorkspacePath,
                RunId = runId,
            };

            var result = await _ingestionService.IngestPathAsync(ingestionRequest, cancellationToken).ConfigureAwait(false);
            return BuildRunResponse(result);
        }
        catch (TranscriptUploadLimitExceededException ex)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Rejected transcript upload for workspace {WorkspacePath}.", _workspaceContext.WorkspacePath);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        finally
        {
            if (Directory.Exists(stagingRoot))
                Directory.Delete(stagingRoot, recursive: true);
        }
    }

    private static ActionResult<TranscriptIngestRunResponse> BuildRunResponse(TranscriptIngestionResult result)
    {
        var response = TranscriptIngestRunResponse.FromResult(result);
        if (HasPartialBundleFailure(result))
        {
            return new ObjectResult(response)
            {
                StatusCode = StatusCodes.Status207MultiStatus,
            };
        }

        return new OkObjectResult(response);
    }

    private static bool HasPartialBundleFailure(TranscriptIngestionResult result)
    {
        var hasSuccessfulOutput = result.Receipts.Count > 0 || result.Sessions.Count > 0;
        if (!hasSuccessfulOutput)
            return false;

        return result.Diagnostics.Any(diagnostic =>
            diagnostic.Code.Equals("normalize_failed", StringComparison.OrdinalIgnoreCase)
            || diagnostic.Code.Equals("adapter_missing", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task StageUploadFileAsync(
        IFormFile file,
        string stagingRoot,
        HashSet<string> stagedPaths,
        CancellationToken cancellationToken)
    {
        if (file.Length > MaxSourceFileBytes)
            throw new TranscriptUploadLimitExceededException($"Transcript upload file '{file.FileName}' exceeds the 256 MiB source file limit.");

        if (IsZipUpload(file))
        {
            await ExtractZipUploadAsync(file, stagingRoot, stagedPaths, cancellationToken).ConfigureAwait(false);
            return;
        }

        var destination = ResolveUploadDestination(stagingRoot, file.FileName, stagedPaths);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await using var input = file.OpenReadStream();
        await using var output = System.IO.File.Create(destination);
        await CopyWithLimitAsync(input, output, MaxSourceFileBytes, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ExtractZipUploadAsync(
        IFormFile file,
        string stagingRoot,
        HashSet<string> stagedPaths,
        CancellationToken cancellationToken)
    {
        using var archive = new ZipArchive(file.OpenReadStream(), ZipArchiveMode.Read, leaveOpen: false);
        if (archive.Entries.Count > MaxArchiveEntries)
            throw new TranscriptUploadLimitExceededException("Transcript ZIP upload exceeds the 10,000 entry limit.");

        long expandedBytes = 0;
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.Name))
                continue;
            if (IsZipLink(entry))
                throw new InvalidDataException($"Transcript ZIP entry '{entry.FullName}' is a link and is not allowed.");
            if (entry.Length > MaxSourceFileBytes)
                throw new TranscriptUploadLimitExceededException($"Transcript ZIP entry '{entry.FullName}' exceeds the 256 MiB source file limit.");
            if (entry.CompressedLength == 0 && entry.Length > 0)
                throw new TranscriptUploadLimitExceededException($"Transcript ZIP entry '{entry.FullName}' exceeds the compression ratio limit.");
            if (entry.CompressedLength > 0 && entry.Length / (double)entry.CompressedLength > MaxCompressionRatio)
                throw new TranscriptUploadLimitExceededException($"Transcript ZIP entry '{entry.FullName}' exceeds the compression ratio limit.");

            expandedBytes += entry.Length;
            if (expandedBytes > MaxExpandedUploadBytes)
                throw new TranscriptUploadLimitExceededException("Transcript ZIP upload exceeds the 2 GiB expanded content limit.");

            var destination = ResolveUploadDestination(stagingRoot, entry.FullName, stagedPaths);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await using var input = entry.Open();
            await using var output = System.IO.File.Create(destination);
            await CopyWithLimitAsync(input, output, MaxSourceFileBytes, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task CopyWithLimitAsync(Stream input, Stream output, long maxBytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long total = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
                break;
            total += read;
            if (total > maxBytes)
                throw new TranscriptUploadLimitExceededException("Transcript upload source file exceeded the configured size limit while copying.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }

    private static string ResolveUploadDestination(string stagingRoot, string relativeName, HashSet<string> stagedPaths)
    {
        var normalized = (relativeName ?? string.Empty).Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(normalized) || normalized.StartsWith("/", StringComparison.Ordinal) || Path.IsPathRooted(normalized))
            throw new InvalidDataException("Transcript upload file names must be relative paths.");

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
            throw new InvalidDataException($"Transcript upload path '{relativeName}' escapes the upload root.");

        var destination = Path.GetFullPath(Path.Combine(new[] { stagingRoot }.Concat(segments).ToArray()));
        var root = EnsureDirectorySuffix(Path.GetFullPath(stagingRoot));
        if (!destination.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidDataException($"Transcript upload path '{relativeName}' escapes the upload root.");
        if (!stagedPaths.Add(destination))
            throw new InvalidDataException($"Transcript upload contains duplicate path '{relativeName}'.");
        return destination;
    }

    private static bool IsZipUpload(IFormFile file)
        => file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
           || string.Equals(file.ContentType, "application/zip", StringComparison.OrdinalIgnoreCase)
           || string.Equals(file.ContentType, "application/x-zip-compressed", StringComparison.OrdinalIgnoreCase);

    private static bool IsZipLink(ZipArchiveEntry entry)
    {
        const int unixFileTypeMask = 0xF000;
        const int unixSymbolicLink = 0xA000;
        var unixMode = (entry.ExternalAttributes >> 16) & unixFileTypeMask;
        return unixMode == unixSymbolicLink;
    }

    private static string CreateUploadRunId()
        => "upload-" + DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N")[..8];

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var chars = value.Select(ch => invalid.Contains(ch) || char.IsControl(ch) ? '_' : ch).ToArray();
        var sanitized = new string(chars).Trim('.', ' ');
        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
    }

    private static string EnsureDirectorySuffix(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;

    private sealed class TranscriptUploadLimitExceededException : Exception
    {
        internal TranscriptUploadLimitExceededException(string message)
            : base(message)
        {
        }
    }
}