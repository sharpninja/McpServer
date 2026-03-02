using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Ingestion;

/// <summary>TR-PLANNED-013: Statistics from a session log import run.</summary>
public sealed record SessionLogImportResult
{
    /// <summary>Total JSON files scanned in the sessions directory.</summary>
    public int FilesScanned { get; init; }

    /// <summary>Files imported (new or updated in the database).</summary>
    public int Imported { get; init; }

    /// <summary>Files skipped because their content hash was unchanged.</summary>
    public int Skipped { get; init; }

    /// <summary>Files that failed to parse or read.</summary>
    public int Failed { get; init; }

    /// <summary>Total entries across all imported session logs.</summary>
    public int TotalEntries { get; init; }
}

/// <summary>
/// TR-PLANNED-013: Ingests session logs from docs/sessions/, normalizes to UnifiedModel.
/// FR-SUPPORT-010: Supports .md and .json session log files.
/// </summary>
public sealed class SessionLogIngestor
{
    private readonly Chunker _chunker;
    private readonly IngestionOptions _options;
    private readonly ISessionLogService _sessionLogService;
    private readonly ILogger<SessionLogIngestor> _logger;
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    /// <param name="chunker">Chunker for splitting content.</param>
    /// <param name="options">Ingestion options providing sessions path.</param>
    /// <param name="sessionLogService">Service for persisting session logs to 4NF tables.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public SessionLogIngestor(
        Chunker chunker,
        Microsoft.Extensions.Options.IOptions<IngestionOptions> options,
        ISessionLogService sessionLogService,
        ILogger<SessionLogIngestor> logger)
    {
        _chunker = chunker;
        _options = options?.Value ?? new IngestionOptions();
        _sessionLogService = sessionLogService ?? throw new ArgumentNullException(nameof(sessionLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>FR-SUPPORT-010: Ingests all session logs under SessionsPath; returns documents and chunks.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>List of ingested documents with their chunks.</returns>
    public async Task<IReadOnlyList<(ContextDocument Doc, IReadOnlyList<ContextChunk> Chunks)>> IngestAsync(
        CancellationToken cancellationToken = default)
    {
        var repoRoot = Path.GetFullPath(_options.RepoRoot);
        var sessionsDir = Path.Combine(repoRoot, _options.SessionsPath.TrimStart('.', Path.DirectorySeparatorChar));
        if (!Directory.Exists(sessionsDir))
        {
            return Array.Empty<(ContextDocument, IReadOnlyList<ContextChunk>)>();
        }

        var results = new List<(ContextDocument Doc, IReadOnlyList<ContextChunk> Chunks)>();

        foreach (var path in Directory.EnumerateFiles(sessionsDir, "*.*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ext = Path.GetExtension(path).ToUpperInvariant();
            if (!ext.Equals(".JSON", StringComparison.Ordinal) && !ext.Equals(".MD", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                if (content.Length > _options.MaxFileSizeBytes)
                {
                    continue;
                }

                var normalized = ext.Equals(".JSON", StringComparison.Ordinal)
                    ? NormalizeJsonSessionLog(content)
                    : NormalizeMarkdownSessionLog(content);
                var contentHash = ComputeHash(content);
                var relativePath = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                var documentId = "session-log:" + relativePath.Replace("/", "-", StringComparison.Ordinal).Replace(":", "-", StringComparison.Ordinal);
                var doc = new ContextDocument
                {
                    Id = documentId,
                    SourceType = "session-log",
                    SourceKey = relativePath,
                    IngestedAt = DateTime.UtcNow,
                    ContentHash = contentHash
                };
                var chunks = _chunker.Chunk(documentId, normalized);
                results.Add((doc, chunks));
            }
            catch (IOException ex)
            {
                _logger.LogWarning("{ExceptionDetail}", ex.ToString());
                // Skip unreadable files
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("{ExceptionDetail}", ex.ToString());
                // Skip invalid JSON
            }
        }

        return results;
    }

    private string NormalizeJsonSessionLog(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<UnifiedSessionLogDto>(json, s_jsonOptions);
            if (dto == null) return json;
            var sb = new StringBuilder();
            sb.Append("Session: ").Append(dto.Title ?? dto.SessionId ?? "unknown").AppendLine();
            sb.Append("Source: ").AppendLine(dto.SourceType ?? "");
            sb.Append("Entries: ").AppendLine(dto.EntryCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (dto.Entries != null)
            {
                foreach (var e in dto.Entries)
                {
                    sb.AppendLine("---");
                    sb.Append("Request: ").AppendLine(e.QueryTitle ?? e.RequestId ?? "");
                    if (!string.IsNullOrEmpty(e.QueryText)) sb.AppendLine(e.QueryText);
                    if (!string.IsNullOrEmpty(e.Response)) sb.AppendLine(e.Response);
                }
            }
            return sb.ToString();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return json;
        }
    }

    private static string NormalizeMarkdownSessionLog(string md) =>
        MarkdownSessionLogParser.NormalizeToStructuredText(md);

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToUpperInvariant();
    }

    /// <summary>
    /// TR-PLANNED-013: Imports all JSON session log files from SessionsPath into the 4NF session log tables.
    /// Skips .md files (no structured data). Uses upsert via <see cref="ISessionLogService.SubmitAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import statistics including files scanned, imported, skipped, failed, and total entries.</returns>
    public async Task<SessionLogImportResult> ImportToSessionLogTablesAsync(CancellationToken cancellationToken = default)
    {
        var repoRoot = Path.GetFullPath(_options.RepoRoot);
        var sessionsDir = Path.Combine(repoRoot, _options.SessionsPath.TrimStart('.', Path.DirectorySeparatorChar));
        if (!Directory.Exists(sessionsDir))
        {
            _logger.LogWarning("Sessions directory not found: {SessionsDir}", sessionsDir);
            return new SessionLogImportResult();
        }

        var imported = 0;
        var skipped = 0;
        var failed = 0;
        var totalEntries = 0;
        var filesScanned = 0;
        foreach (var path in Directory.EnumerateFiles(sessionsDir, "*.json", SearchOption.TopDirectoryOnly))
        {
            filesScanned++;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                if (content.Length > _options.MaxFileSizeBytes)
                {
                    _logger.LogDebug("Skipping oversized session log: {Path}", path);
                    skipped++;
                    continue;
                }

                var contentHash = ComputeHash(content);

                var dto = DeserializeSessionLog(content);
                if (dto is null || string.IsNullOrWhiteSpace(dto.SourceType) || string.IsNullOrWhiteSpace(dto.SessionId))
                {
                    _logger.LogDebug("Skipping session log with missing SourceType/SessionId: {Path}", path);
                    failed++;
                    continue;
                }

                // Skip files whose content hash hasn't changed since the last import
                if (await _sessionLogService.IsUnchangedAsync(dto.SourceType, dto.SessionId, contentHash, cancellationToken).ConfigureAwait(false))
                {
                    skipped++;
                    _logger.LogDebug("Skipping unchanged session log {SourceType}/{SessionId}: {Path}", dto.SourceType, dto.SessionId, path);
                    continue;
                }

                await _sessionLogService.SubmitAsync(dto, path, contentHash, cancellationToken).ConfigureAwait(false);
                imported++;
                totalEntries += dto.Entries?.Count ?? 0;
                _logger.LogDebug("Imported session log {SourceType}/{SessionId} from {Path}", dto.SourceType, dto.SessionId, path);
            }
            catch (JsonException ex)
            {
                failed++;
                _logger.LogWarning(ex, "Failed to parse session log JSON: {Path}", path);
            }
            catch (IOException ex)
            {
                failed++;
                _logger.LogWarning(ex, "Failed to read session log file: {Path}", path);
            }
            catch (DbUpdateException ex)
            {
                failed++;
                _logger.LogWarning(ex, "Failed to save session log to database: {Path} — {Msg}", path, ex.InnerException?.Message ?? ex.Message);
            }
        }

        _logger.LogInformation(
            "Session log import complete: {FilesScanned} scanned, {Imported} imported ({TotalEntries} entries), {Skipped} unchanged, {Failed} failed",
            filesScanned, imported, totalEntries, skipped, failed);

        // Also process .md files via MarkdownSessionLogParser for 4NF import
        foreach (var mdPath in Directory.EnumerateFiles(sessionsDir, "*.md", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            filesScanned++;
            try
            {
                var content = await File.ReadAllTextAsync(mdPath, cancellationToken).ConfigureAwait(false);
                if (content.Length > _options.MaxFileSizeBytes) { skipped++; continue; }
                var contentHash = ComputeHash(content);
                var dto = MarkdownSessionLogParser.TryParse(content, mdPath);
                if (dto is null || string.IsNullOrWhiteSpace(dto.SourceType) || string.IsNullOrWhiteSpace(dto.SessionId))
                {
                    continue;
                }
                if (await _sessionLogService.IsUnchangedAsync(dto.SourceType, dto.SessionId, contentHash, cancellationToken).ConfigureAwait(false))
                {
                    skipped++; continue;
                }
                await _sessionLogService.SubmitAsync(dto, mdPath, contentHash, cancellationToken).ConfigureAwait(false);
                imported++;
                totalEntries += dto.Entries?.Count ?? 0;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                _logger.LogWarning(ex, "Failed to parse Markdown session log: {Path}", mdPath);
            }
        }

        return new SessionLogImportResult
        {
            FilesScanned = filesScanned,
            Imported = imported,
            Skipped = skipped,
            Failed = failed,
            TotalEntries = totalEntries
        };
    }

    /// <summary>
    /// Deserializes a session log JSON file, handling the workspace field being either a string or object.
    /// </summary>
    private static UnifiedSessionLogDto? DeserializeSessionLog(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var dto = new UnifiedSessionLogDto
        {
            SourceType = root.TryGetProperty("sourceType", out var st) ? st.GetString() : null,
            SessionId = root.TryGetProperty("sessionId", out var si) ? si.GetString() : null,
            Title = root.TryGetProperty("title", out var t) ? t.GetString() : null,
            Model = root.TryGetProperty("model", out var m) ? m.GetString() : null,
            Started = root.TryGetProperty("started", out var s) ? s.GetString() : null,
            LastUpdated = root.TryGetProperty("lastUpdated", out var lu) ? lu.GetString() : null,
            Status = root.TryGetProperty("status", out var stat) ? stat.GetString() : null,
            EntryCount = root.TryGetProperty("entryCount", out var ec) && ec.ValueKind == JsonValueKind.Number ? ec.GetInt32() : 0,
            TotalTokens = root.TryGetProperty("totalTokens", out var tt) && tt.ValueKind == JsonValueKind.Number ? tt.GetInt32() : null,
            CursorSessionLabel = root.TryGetProperty("cursorSessionLabel", out var csl) ? csl.GetString() : null,
        };

        // Handle workspace: may be a string (path) or object (WorkspaceInfoDto)
        if (root.TryGetProperty("workspace", out var ws))
        {
            if (ws.ValueKind == JsonValueKind.String)
            {
                dto.Workspace = new WorkspaceInfoDto { Repository = ws.GetString() };
            }
            else if (ws.ValueKind == JsonValueKind.Object)
            {
                dto.Workspace = new WorkspaceInfoDto
                {
                    Project = ws.TryGetProperty("project", out var wp) ? wp.GetString() : null,
                    TargetFramework = ws.TryGetProperty("targetFramework", out var wtf) ? wtf.GetString() : null,
                    Repository = ws.TryGetProperty("repository", out var wr) ? wr.GetString() : null,
                    Branch = ws.TryGetProperty("branch", out var wb) ? wb.GetString() : null,
                };
            }
        }

        // Handle copilotStatistics
        if (root.TryGetProperty("copilotStatistics", out var cs) && cs.ValueKind == JsonValueKind.Object)
        {
            dto.CopilotStatistics = JsonSerializer.Deserialize<CopilotStatisticsDto>(cs.GetRawText(), s_jsonOptions);
        }

        // Entries: use standard deserialization (entries schema is consistent)
        if (root.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
        {
            dto.Entries = JsonSerializer.Deserialize<List<UnifiedRequestEntryDto>>(entries.GetRawText(), s_jsonOptions);
        }

        return dto;
    }
}
