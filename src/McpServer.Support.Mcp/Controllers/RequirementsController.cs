using System.IO.Compression;
using System.Text;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// REST endpoints for managing requirements documents (FR/TR/TEST/mapping) and generating canonical Markdown/workspace output.
/// </summary>
[ApiController]
[Route("mcpserver/requirements")]
public sealed class RequirementsController : ControllerBase
{
    private readonly IRequirementsDocumentService _requirements;
    private readonly RequirementsOptions _requirementsOptions;
    private readonly WorkspaceContext _workspaceContext;
    private readonly ILogger<RequirementsController> _logger;


    /// <summary>Initializes a new instance of the <see cref="RequirementsController"/> class.</summary>
    public RequirementsController(IRequirementsDocumentService requirements,
        IOptions<RequirementsOptions> requirementsOptions,
        WorkspaceContext workspaceContext,
        ILogger<RequirementsController> logger)
    {
        _logger = logger;
        _requirements = requirements;
        _requirementsOptions = requirementsOptions?.Value ?? throw new ArgumentNullException(nameof(requirementsOptions));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
    }

    /// <summary>Gets all Functional Requirement entries.</summary>
    [HttpGet("fr")]
    public async Task<ActionResult<IReadOnlyList<FrEntry>>> GetFrAsync(CancellationToken cancellationToken)
        => Ok(await _requirements.GetAllFrAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Gets a Functional Requirement entry by id.</summary>
    [HttpGet("fr/{id}")]
    public async Task<ActionResult<FrEntry>> GetFrByIdAsync(string id, CancellationToken cancellationToken)
    {
        var entry = await _requirements.GetFrAsync(id, cancellationToken).ConfigureAwait(false);
        return entry is null ? NotFound(new { error = $"FR '{id}' not found." }) : Ok(entry);
    }

    /// <summary>Creates a new Functional Requirement entry.</summary>
    [HttpPost("fr")]
    public async Task<ActionResult<FrEntry>> CreateFrAsync([FromBody] CreateFrRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        var entry = new FrEntry(request.Id, request.Title, request.Body);
        try
        {
            await _requirements.AddFrAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        catch (RequirementsConflictException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return BadRequest(new { error = ex.Message });
        }

        return Created(new Uri($"/mcpserver/requirements/fr/{Uri.EscapeDataString(entry.Id)}", UriKind.Relative), entry);
    }

    /// <summary>Updates an existing Functional Requirement entry.</summary>
    [HttpPut("fr/{id}")]
    public async Task<ActionResult<FrEntry>> UpdateFrAsync(string id, [FromBody] UpdateFrRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        var entry = new FrEntry(id, request.Title, request.Body);
        try
        {
            await _requirements.UpdateFrAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        catch (RequirementsNotFoundException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return BadRequest(new { error = ex.Message });
        }

        return Ok(entry);
    }

    /// <summary>Deletes a Functional Requirement entry by id.</summary>
    [HttpDelete("fr/{id}")]
    public async Task<IActionResult> DeleteFrAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _requirements.DeleteFrAsync(id, cancellationToken).ConfigureAwait(false);
        }
        catch (RequirementsNotFoundException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return NotFound(new { error = ex.Message });
        }

        return Ok(new { success = true });
    }

    /// <summary>Gets all Technical Requirement entries.</summary>
    [HttpGet("tr")]
    public async Task<ActionResult<IReadOnlyList<TrEntry>>> GetTrAsync(CancellationToken cancellationToken)
        => Ok(await _requirements.GetAllTrAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Gets a Technical Requirement entry by id.</summary>
    [HttpGet("tr/{id}")]
    public async Task<ActionResult<TrEntry>> GetTrByIdAsync(string id, CancellationToken cancellationToken)
    {
        var entry = await _requirements.GetTrAsync(id, cancellationToken).ConfigureAwait(false);
        return entry is null ? NotFound(new { error = $"TR '{id}' not found." }) : Ok(entry);
    }

    /// <summary>Creates a new Technical Requirement entry.</summary>
    [HttpPost("tr")]
    public async Task<ActionResult<TrEntry>> CreateTrAsync([FromBody] CreateTrRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        var entry = new TrEntry(request.Id, request.Title ?? string.Empty, request.Body);
        try
        {
            await _requirements.AddTrAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        catch (RequirementsConflictException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return BadRequest(new { error = ex.Message });
        }

        return Created(new Uri($"/mcpserver/requirements/tr/{Uri.EscapeDataString(entry.Id)}", UriKind.Relative), entry);
    }

    /// <summary>Updates an existing Technical Requirement entry.</summary>
    [HttpPut("tr/{id}")]
    public async Task<ActionResult<TrEntry>> UpdateTrAsync(string id, [FromBody] UpdateTrRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        var entry = new TrEntry(id, request.Title ?? string.Empty, request.Body);
        try
        {
            await _requirements.UpdateTrAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        catch (RequirementsNotFoundException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return BadRequest(new { error = ex.Message });
        }

        return Ok(entry);
    }

    /// <summary>Deletes a Technical Requirement entry by id.</summary>
    [HttpDelete("tr/{id}")]
    public async Task<IActionResult> DeleteTrAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _requirements.DeleteTrAsync(id, cancellationToken).ConfigureAwait(false);
        }
        catch (RequirementsNotFoundException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return NotFound(new { error = ex.Message });
        }

        return Ok(new { success = true });
    }

    /// <summary>Gets all Testing Requirement entries.</summary>
    [HttpGet("test")]
    public async Task<ActionResult<IReadOnlyList<TestEntry>>> GetTestAsync(CancellationToken cancellationToken)
        => Ok(await _requirements.GetAllTestAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Gets a Testing Requirement entry by id.</summary>
    [HttpGet("test/{id}")]
    public async Task<ActionResult<TestEntry>> GetTestByIdAsync(string id, CancellationToken cancellationToken)
    {
        var entry = await _requirements.GetTestAsync(id, cancellationToken).ConfigureAwait(false);
        return entry is null ? NotFound(new { error = $"TEST '{id}' not found." }) : Ok(entry);
    }

    /// <summary>Creates a new Testing Requirement entry.</summary>
    [HttpPost("test")]
    public async Task<ActionResult<TestEntry>> CreateTestAsync([FromBody] CreateTestRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        var entry = new TestEntry(request.Id, request.Condition);
        try
        {
            await _requirements.AddTestAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        catch (RequirementsConflictException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return BadRequest(new { error = ex.Message });
        }

        return Created(new Uri($"/mcpserver/requirements/test/{Uri.EscapeDataString(entry.Id)}", UriKind.Relative), entry);
    }

    /// <summary>Updates an existing Testing Requirement entry.</summary>
    [HttpPut("test/{id}")]
    public async Task<ActionResult<TestEntry>> UpdateTestAsync(string id, [FromBody] UpdateTestRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        var entry = new TestEntry(id, request.Condition);
        try
        {
            await _requirements.UpdateTestAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        catch (RequirementsNotFoundException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return BadRequest(new { error = ex.Message });
        }

        return Ok(entry);
    }

    /// <summary>Deletes a Testing Requirement entry by id.</summary>
    [HttpDelete("test/{id}")]
    public async Task<IActionResult> DeleteTestAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _requirements.DeleteTestAsync(id, cancellationToken).ConfigureAwait(false);
        }
        catch (RequirementsNotFoundException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return NotFound(new { error = ex.Message });
        }

        return Ok(new { success = true });
    }

    /// <summary>Gets the full FR-to-TR mapping table.</summary>
    [HttpGet("mapping")]
    public async Task<ActionResult<IReadOnlyList<FrTrMapping>>> GetMappingsAsync(CancellationToken cancellationToken)
        => Ok(await _requirements.GetAllMappingsAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Gets a single FR-to-TR mapping row by FR id.</summary>
    [HttpGet("mapping/{frId}")]
    public async Task<ActionResult<FrTrMapping>> GetMappingByIdAsync(string frId, CancellationToken cancellationToken)
    {
        var mapping = await _requirements.GetMappingAsync(frId, cancellationToken).ConfigureAwait(false);
        return mapping is null ? NotFound(new { error = $"Mapping row '{frId}' not found." }) : Ok(mapping);
    }

    /// <summary>Creates or updates an FR-to-TR mapping row.</summary>
    [HttpPut("mapping/{frId}")]
    public async Task<ActionResult<FrTrMapping>> UpsertMappingAsync(string frId, [FromBody] UpsertFrTrMappingRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        var mapping = new FrTrMapping(frId, request.TrIds ?? Array.Empty<string>(), request.TestIds ?? Array.Empty<string>());
        try
        {
            await _requirements.UpsertMappingAsync(mapping, cancellationToken).ConfigureAwait(false);
        }
        catch (RequirementsNotFoundException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return BadRequest(new { error = ex.Message });
        }

        return Ok(mapping);
    }

    /// <summary>Deletes an FR-to-TR mapping row by FR id.</summary>
    [HttpDelete("mapping/{frId}")]
    public async Task<IActionResult> DeleteMappingAsync(string frId, CancellationToken cancellationToken)
    {
        try
        {
            await _requirements.DeleteMappingAsync(frId, cancellationToken).ConfigureAwait(false);
        }
        catch (RequirementsNotFoundException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return NotFound(new { error = ex.Message });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Bulk-ingests requirements markdown and upserts FR/TR/TEST/mapping entities.
    /// </summary>
    /// <param name="request">Optional markdown payloads. When omitted, configured markdown files are read from disk.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("ingest")]
    public async Task<ActionResult<RequirementsIngestResult>> IngestAsync(
        [FromBody] RequirementsIngestRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var sourceFormat = NormalizeSourceFormat(request?.SourceFormat, request?.Documents);
            var wikiSelection = sourceFormat == "wiki"
                ? RequirementsWikiDocumentSelector.Select(request?.Documents ?? new Dictionary<string, RequirementsIngestDocument>(), request?.PreferredWikiFormat)
                : null;

            var functionalMarkdown = request?.FunctionalMarkdown;
            var technicalMarkdown = request?.TechnicalMarkdown;
            var testingMarkdown = request?.TestingMarkdown;
            var mappingMarkdown = request?.MappingMarkdown;

            if (wikiSelection is not null)
            {
                functionalMarkdown = wikiSelection.FunctionalMarkdown;
                technicalMarkdown = wikiSelection.TechnicalMarkdown;
                testingMarkdown = wikiSelection.TestingMarkdown;
                mappingMarkdown = wikiSelection.MappingMarkdown;
            }

            if (wikiSelection is null
                && string.IsNullOrWhiteSpace(functionalMarkdown)
                && string.IsNullOrWhiteSpace(technicalMarkdown)
                && string.IsNullOrWhiteSpace(testingMarkdown)
                && string.IsNullOrWhiteSpace(mappingMarkdown))
            {
                functionalMarkdown = ReadMarkdownFile(ResolveRequirementsFilePath(RequirementsDocumentRenderer.FunctionalFileName, _requirementsOptions.FunctionalRequirementsPath));
                technicalMarkdown = ReadMarkdownFile(ResolveRequirementsFilePath(RequirementsDocumentRenderer.TechnicalFileName, _requirementsOptions.TechnicalRequirementsPath));
                testingMarkdown = ReadMarkdownFile(ResolveRequirementsFilePath(RequirementsDocumentRenderer.TestingFileName, _requirementsOptions.TestingRequirementsPath));
                mappingMarkdown = ReadMarkdownFile(ResolveRequirementsFilePath(RequirementsDocumentRenderer.MappingFileName, _requirementsOptions.MappingPath));
            }

            var hasFunctionalDocument = functionalMarkdown is not null;
            var hasTechnicalDocument = technicalMarkdown is not null;
            var hasTestingDocument = testingMarkdown is not null;
            var hasMappingDocument = mappingMarkdown is not null;

            var frEntries = RequirementsDocumentParser.ParseFunctional(functionalMarkdown);
            var trEntries = RequirementsDocumentParser.ParseTechnical(technicalMarkdown);
            var testEntries = RequirementsDocumentParser.ParseTesting(testingMarkdown);
            var mappingEntries = RequirementsDocumentParser.ParseMapping(mappingMarkdown);

            var authoritative = wikiSelection is not null;
            var (frAdded, frUpdated, frDeleted, frIgnored) = authoritative && hasFunctionalDocument
                ? await SyncFunctionalAsync(frEntries, cancellationToken).ConfigureAwait(false)
                : await UpsertFunctionalAsync(frEntries, cancellationToken).ConfigureAwait(false);
            var (trAdded, trUpdated, trDeleted, trIgnored) = authoritative && hasTechnicalDocument
                ? await SyncTechnicalAsync(trEntries, cancellationToken).ConfigureAwait(false)
                : await UpsertTechnicalAsync(trEntries, cancellationToken).ConfigureAwait(false);
            var (testAdded, testUpdated, testDeleted, testIgnored) = authoritative && hasTestingDocument
                ? await SyncTestingAsync(testEntries, cancellationToken).ConfigureAwait(false)
                : await UpsertTestingAsync(testEntries, cancellationToken).ConfigureAwait(false);
            var (mappingAdded, mappingUpdated, mappingDeleted, mappingIgnored) = authoritative && hasMappingDocument
                ? await SyncMappingAsync(mappingEntries, cancellationToken).ConfigureAwait(false)
                : await UpsertMappingAsync(mappingEntries, cancellationToken).ConfigureAwait(false);

            var result = new RequirementsIngestResult
            {
                FunctionalParsed = frEntries.Count,
                FunctionalAdded = frAdded,
                FunctionalUpdated = frUpdated,
                FunctionalDeleted = frDeleted,
                FunctionalIgnored = frIgnored,
                TechnicalParsed = trEntries.Count,
                TechnicalAdded = trAdded,
                TechnicalUpdated = trUpdated,
                TechnicalDeleted = trDeleted,
                TechnicalIgnored = trIgnored,
                TestingParsed = testEntries.Count,
                TestingAdded = testAdded,
                TestingUpdated = testUpdated,
                TestingDeleted = testDeleted,
                TestingIgnored = testIgnored,
                MappingParsed = mappingEntries.Count,
                MappingAdded = mappingAdded,
                MappingUpdated = mappingUpdated,
                MappingDeleted = mappingDeleted,
                MappingIgnored = mappingIgnored,
                SelectedWikiFormat = wikiSelection?.Platform,
                SelectedWikiReason = wikiSelection?.Reason,
                SelectedManifestGeneratedAtUtc = wikiSelection?.ManifestGeneratedAtUtc,
                SelectedLatestFileModifiedUtc = wikiSelection?.LatestFileModifiedUtc,
                Warnings = wikiSelection?.Warnings ?? []
            };

            return Ok(result);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return NotFound(new { error = ex.Message });
        }
        catch (IOException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Generates a requirements document or exports all requirements documents to the workspace.</summary>
    /// <param name="doc">Document selector: functional, technical, testing, mapping, or all.</param>
    /// <param name="format">Output format: markdown or wiki.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("generate")]
    public async Task<IActionResult> GenerateAsync(
        [FromQuery] string doc = "all",
        [FromQuery] string format = "markdown",
        CancellationToken cancellationToken = default)
    {
        if (!TryParseDocType(doc, out var docType))
            return BadRequest(new { error = $"Unsupported doc value '{doc}'. Expected functional|technical|testing|mapping|all." });

        var normalizedFormat = (format ?? "markdown").Trim().ToLowerInvariant();
        if (normalizedFormat == "wiki")
        {
            if (docType != RequirementsDocType.All)
                return BadRequest(new { error = "Wiki generation requires doc=all." });

            var wikiExport = await _requirements.GenerateWikiAsync(ResolveWikiOutputRoot(), ct: cancellationToken).ConfigureAwait(false);
            var zipBytes = CreateWikiExportZip(wikiExport);
            return File(zipBytes, "application/zip", "requirements-wiki-documents.zip");
        }

        if (normalizedFormat is not "markdown" and not "yaml")
            return BadRequest(new { error = $"Unsupported format value '{format}'. Expected markdown|yaml|wiki." });

        if (docType == RequirementsDocType.All)
        {
            var export = await _requirements.GenerateAllAsync(ResolveProjectOutputRoot(), ct: cancellationToken).ConfigureAwait(false);
            return Ok(export);
        }

        var (content, mimeType) = await _requirements.GenerateDocumentAsync(docType, cancellationToken).ConfigureAwait(false);
        var fileName = docType switch
        {
            RequirementsDocType.Functional => RequirementsDocumentRenderer.FunctionalFileName,
            RequirementsDocType.Technical => RequirementsDocumentRenderer.TechnicalFileName,
            RequirementsDocType.Testing => RequirementsDocumentRenderer.TestingFileName,
            RequirementsDocType.Mapping => RequirementsDocumentRenderer.MappingFileName,
            _ => "requirements.md"
        };

        return File(Encoding.UTF8.GetBytes(content), mimeType, fileName);
    }

    private static byte[] CreateWikiExportZip(RequirementsDocumentExportResult wikiExport)
    {
        ArgumentNullException.ThrowIfNull(wikiExport);

        var outputRoot = Path.GetFullPath(wikiExport.OutputRoot);
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in wikiExport.Files)
            {
                var fullPath = Path.GetFullPath(file.FullPath);
                if (!fullPath.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Generated wiki file is outside the output root: {file.FullPath}");
                }

                if (!System.IO.File.Exists(fullPath))
                {
                    throw new FileNotFoundException("Generated wiki file was not found.", fullPath);
                }

                var relativePath = string.IsNullOrWhiteSpace(file.RelativePath)
                    ? Path.GetRelativePath(outputRoot, fullPath)
                    : file.RelativePath;
                relativePath = relativePath.Replace('\\', '/');
                archive.CreateEntryFromFile(fullPath, relativePath, CompressionLevel.Fastest);
            }
        }

        return stream.ToArray();
    }

    internal static bool TryParseDocType(string? raw, out RequirementsDocType docType)
    {
        switch ((raw ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "functional":
            case "fr":
                docType = RequirementsDocType.Functional;
                return true;
            case "technical":
            case "tr":
                docType = RequirementsDocType.Technical;
                return true;
            case "testing":
            case "test":
                docType = RequirementsDocType.Testing;
                return true;
            case "mapping":
                docType = RequirementsDocType.Mapping;
                return true;
            case "all":
                docType = RequirementsDocType.All;
                return true;
            default:
                docType = default;
                return false;
        }
    }

    private string ReadMarkdownFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new IOException("A configured requirements file path is missing.");
        if (!System.IO.File.Exists(path))
            throw new FileNotFoundException($"Requirements markdown file was not found: {path}", path);
        return System.IO.File.ReadAllText(path);
    }

    private string ResolveRequirementsFilePath(string fileName, string configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(_workspaceContext.WorkspacePath))
            return Path.Combine(_workspaceContext.WorkspacePath, "docs", "Project", fileName);

        return configuredPath;
    }

    private string ResolveProjectOutputRoot()
    {
        if (!string.IsNullOrWhiteSpace(_workspaceContext.WorkspacePath))
            return Path.Combine(_workspaceContext.WorkspacePath, "docs", "Project");

        var configuredPaths = new[]
        {
            _requirementsOptions.FunctionalRequirementsPath,
            _requirementsOptions.TechnicalRequirementsPath,
            _requirementsOptions.TestingRequirementsPath,
            _requirementsOptions.MappingPath
        };

        var configuredDirectory = configuredPaths
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Select(Path.GetDirectoryName)
            .FirstOrDefault(static path => !string.IsNullOrWhiteSpace(path));

        if (!string.IsNullOrWhiteSpace(configuredDirectory))
            return configuredDirectory!;

        throw new IOException("A workspace path or configured requirements file path is required for requirements export.");
    }

    private string ResolveWikiOutputRoot() => Path.Combine(ResolveProjectOutputRoot(), "wiki");

    private static string NormalizeSourceFormat(string? sourceFormat, IReadOnlyDictionary<string, RequirementsIngestDocument>? documents)
    {
        var normalized = string.IsNullOrWhiteSpace(sourceFormat)
            ? "auto"
            : sourceFormat.Trim().ToLowerInvariant();

        return normalized switch
        {
            "auto" => documents is { Count: > 0 } && ContainsWikiFolder(documents.Keys) ? "wiki" : "canonical",
            "canonical" => "canonical",
            "wiki" => "wiki",
            _ => throw new ArgumentException($"Unsupported sourceFormat '{sourceFormat}'. Expected auto|canonical|wiki.", nameof(sourceFormat))
        };
    }

    private static bool ContainsWikiFolder(IEnumerable<string> paths)
        => paths.Any(static path =>
        {
            var normalized = path.Replace('\\', '/').Trim('/');
            return normalized.StartsWith("azure/", StringComparison.OrdinalIgnoreCase)
                   || normalized.StartsWith("github/", StringComparison.OrdinalIgnoreCase)
                   || normalized.Contains("/azure/", StringComparison.OrdinalIgnoreCase)
                   || normalized.Contains("/github/", StringComparison.OrdinalIgnoreCase);
        });

    private async Task<(int Added, int Updated, int Deleted, int Ignored)> UpsertFunctionalAsync(
        IReadOnlyList<FrEntry> entries,
        CancellationToken cancellationToken)
    {
        var existing = (await _requirements.GetAllFrAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var updated = 0;
        foreach (var entry in entries)
        {
            if (existing.ContainsKey(entry.Id))
            {
                await _requirements.UpdateFrAsync(entry, cancellationToken).ConfigureAwait(false);
                updated++;
            }
            else
            {
                await _requirements.AddFrAsync(entry, cancellationToken).ConfigureAwait(false);
                added++;
            }
        }

        return (added, updated, 0, 0);
    }

    private async Task<(int Added, int Updated, int Deleted, int Ignored)> UpsertTechnicalAsync(
        IReadOnlyList<TrEntry> entries,
        CancellationToken cancellationToken)
    {
        var existing = (await _requirements.GetAllTrAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var updated = 0;
        foreach (var entry in entries)
        {
            if (existing.ContainsKey(entry.Id))
            {
                await _requirements.UpdateTrAsync(entry, cancellationToken).ConfigureAwait(false);
                updated++;
            }
            else
            {
                await _requirements.AddTrAsync(entry, cancellationToken).ConfigureAwait(false);
                added++;
            }
        }

        return (added, updated, 0, 0);
    }

    private async Task<(int Added, int Updated, int Deleted, int Ignored)> UpsertTestingAsync(
        IReadOnlyList<TestEntry> entries,
        CancellationToken cancellationToken)
    {
        var existing = (await _requirements.GetAllTestAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var updated = 0;
        foreach (var entry in entries)
        {
            if (existing.ContainsKey(entry.Id))
            {
                await _requirements.UpdateTestAsync(entry, cancellationToken).ConfigureAwait(false);
                updated++;
            }
            else
            {
                await _requirements.AddTestAsync(entry, cancellationToken).ConfigureAwait(false);
                added++;
            }
        }

        return (added, updated, 0, 0);
    }

    private async Task<(int Added, int Updated, int Deleted, int Ignored)> UpsertMappingAsync(
        IReadOnlyList<FrTrMapping> entries,
        CancellationToken cancellationToken)
    {
        var existing = (await _requirements.GetAllMappingsAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(entry => entry.FrId, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var updated = 0;
        foreach (var entry in entries)
        {
            if (existing.ContainsKey(entry.FrId))
                updated++;
            else
                added++;

            await _requirements.UpsertMappingAsync(entry, cancellationToken).ConfigureAwait(false);
        }

        return (added, updated, 0, 0);
    }

    private async Task<(int Added, int Updated, int Deleted, int Ignored)> SyncFunctionalAsync(
        IReadOnlyList<FrEntry> entries,
        CancellationToken cancellationToken)
    {
        var existing = (await _requirements.GetAllFrAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        var incoming = entries.ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var updated = 0;
        var ignored = 0;
        foreach (var entry in entries)
        {
            if (!existing.TryGetValue(entry.Id, out var existingEntry))
            {
                await _requirements.AddFrAsync(entry, cancellationToken).ConfigureAwait(false);
                added++;
            }
            else if (existingEntry.Title == entry.Title && existingEntry.Body == entry.Body)
            {
                ignored++;
            }
            else
            {
                await _requirements.UpdateFrAsync(entry, cancellationToken).ConfigureAwait(false);
                updated++;
            }
        }

        var deleted = 0;
        foreach (var existingEntry in existing.Values.Where(entry => !incoming.ContainsKey(entry.Id)))
        {
            await _requirements.DeleteFrAsync(existingEntry.Id, cancellationToken).ConfigureAwait(false);
            deleted++;
        }

        return (added, updated, deleted, ignored);
    }

    private async Task<(int Added, int Updated, int Deleted, int Ignored)> SyncTechnicalAsync(
        IReadOnlyList<TrEntry> entries,
        CancellationToken cancellationToken)
    {
        var existing = (await _requirements.GetAllTrAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        var incoming = entries.ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var updated = 0;
        var ignored = 0;
        foreach (var entry in entries)
        {
            if (!existing.TryGetValue(entry.Id, out var existingEntry))
            {
                await _requirements.AddTrAsync(entry, cancellationToken).ConfigureAwait(false);
                added++;
            }
            else if (existingEntry.Title == entry.Title && existingEntry.Body == entry.Body)
            {
                ignored++;
            }
            else
            {
                await _requirements.UpdateTrAsync(entry, cancellationToken).ConfigureAwait(false);
                updated++;
            }
        }

        var deleted = 0;
        foreach (var existingEntry in existing.Values.Where(entry => !incoming.ContainsKey(entry.Id)))
        {
            await _requirements.DeleteTrAsync(existingEntry.Id, cancellationToken).ConfigureAwait(false);
            deleted++;
        }

        return (added, updated, deleted, ignored);
    }

    private async Task<(int Added, int Updated, int Deleted, int Ignored)> SyncTestingAsync(
        IReadOnlyList<TestEntry> entries,
        CancellationToken cancellationToken)
    {
        var existing = (await _requirements.GetAllTestAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        var incoming = entries.ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var updated = 0;
        var ignored = 0;
        foreach (var entry in entries)
        {
            if (!existing.TryGetValue(entry.Id, out var existingEntry))
            {
                await _requirements.AddTestAsync(entry, cancellationToken).ConfigureAwait(false);
                added++;
            }
            else if (existingEntry.Condition == entry.Condition)
            {
                ignored++;
            }
            else
            {
                await _requirements.UpdateTestAsync(entry, cancellationToken).ConfigureAwait(false);
                updated++;
            }
        }

        var deleted = 0;
        foreach (var existingEntry in existing.Values.Where(entry => !incoming.ContainsKey(entry.Id)))
        {
            await _requirements.DeleteTestAsync(existingEntry.Id, cancellationToken).ConfigureAwait(false);
            deleted++;
        }

        return (added, updated, deleted, ignored);
    }

    private async Task<(int Added, int Updated, int Deleted, int Ignored)> SyncMappingAsync(
        IReadOnlyList<FrTrMapping> entries,
        CancellationToken cancellationToken)
    {
        var existing = (await _requirements.GetAllMappingsAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(entry => entry.FrId, StringComparer.OrdinalIgnoreCase);
        var incoming = entries.ToDictionary(entry => entry.FrId, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var updated = 0;
        var ignored = 0;
        foreach (var entry in entries)
        {
            if (!existing.TryGetValue(entry.FrId, out var existingEntry))
            {
                await _requirements.UpsertMappingAsync(entry, cancellationToken).ConfigureAwait(false);
                added++;
            }
            else if (MappingsEqual(existingEntry, entry))
            {
                ignored++;
            }
            else
            {
                await _requirements.UpsertMappingAsync(entry, cancellationToken).ConfigureAwait(false);
                updated++;
            }
        }

        var deleted = 0;
        foreach (var existingEntry in existing.Values.Where(entry => !incoming.ContainsKey(entry.FrId)))
        {
            await _requirements.DeleteMappingAsync(existingEntry.FrId, cancellationToken).ConfigureAwait(false);
            deleted++;
        }

        return (added, updated, deleted, ignored);
    }

    private static bool MappingsEqual(FrTrMapping left, FrTrMapping right)
        => StringSetEqual(left.TrIds, right.TrIds) && StringSetEqual(left.TestIds, right.TestIds);

    private static bool StringSetEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
        => left.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .SequenceEqual(right.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
}
