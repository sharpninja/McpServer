// TR-PLANNED-013 / FR-SUPPORT-010: MCP tools for STDIO transport (mirrors HTTP API capabilities).

using System.ComponentModel;
using System.Text.Json;
using FWH.Support.Mcp.Ingestion;
using FWH.Support.Mcp.Models;
using FWH.Support.Mcp.Services;
using FWH.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace FWH.Support.Mcp.McpStdio;

/// <summary>
/// TR-PLANNED-013: MCP tools exposed over STDIO; same capabilities as HTTP /mcp/context, /mcp/repo, /mcp/sync.
/// Includes TODO, Session Log, and GitHub tools for full STDIO parity.
/// </summary>
[McpServerToolType]
public sealed class FwhMcpTools
{
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly McpDbContext _db;
    private readonly IRepoFileService _repoFileService;
    private readonly IngestionCoordinator _coordinator;
    private readonly ISyncStatusStore _syncStatusStore;
    private readonly IContextSearchService _searchService;
    private readonly ITodoService _todoService;
    private readonly ISessionLogService _sessionLogService;
    private readonly IGitHubCliService _gitHubCliService;

    /// <summary>TR-PLANNED-013: Constructor for DI.</summary>
    public FwhMcpTools(
        McpDbContext db,
        IRepoFileService repoFileService,
        IngestionCoordinator coordinator,
        ISyncStatusStore syncStatusStore,
        IContextSearchService searchService,
        ITodoService todoService,
        ISessionLogService sessionLogService,
        IGitHubCliService gitHubCliService)
    {
        _db = db;
        _repoFileService = repoFileService;
        _coordinator = coordinator;
        _syncStatusStore = syncStatusStore;
        _searchService = searchService;
        _todoService = todoService;
        _sessionLogService = sessionLogService;
        _gitHubCliService = gitHubCliService;
    }

    /// <summary>Search indexed context chunks by query text.</summary>
    /// <returns>JSON string with matching chunks and source keys.</returns>
    [McpServerTool(Name = "context_search"), Description("Search indexed context chunks by query text. Optional sourceType filter and limit (1-100).")]
    public async Task<string> ContextSearch(
        [Description("Search query text")] string query,
        [Description("Max chunks to return (default 20)")] int limit = 20,
        [Description("Optional source type filter")] string? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        var q = (query ?? string.Empty).Trim();
        var lim = Math.Clamp(limit, 1, 100);

        var result = await _searchService.SearchAsync(q, lim, sourceType, cancellationToken).ConfigureAwait(false);
        var chunks = result.Chunks.Select(c => new ContextChunk
        {
            Id = c.ChunkId,
            DocumentId = c.DocumentId,
            Content = c.Content,
            TokenCount = c.TokenCount,
            ChunkIndex = c.ChunkIndex
        }).ToList();
        return JsonSerializer.Serialize(new { query = q, chunks, sourceKeys = result.SourceKeys });
    }

    /// <summary>Get a deterministic context pack by query.</summary>
    /// <returns>JSON string with ordered context pack.</returns>
    [McpServerTool(Name = "context_pack"), Description("Get a deterministic context pack by query. Optional queryId and limit (1-100).")]
    public async Task<string> ContextPack(
        [Description("Search query text")] string query,
        [Description("Max chunks in pack (default 20)")] int limit = 20,
        [Description("Optional query id for reproducibility")] string? queryId = null,
        CancellationToken cancellationToken = default)
    {
        var q = (query ?? string.Empty).Trim();
        var lim = Math.Clamp(limit, 1, 100);
        var qid = queryId ?? Guid.NewGuid().ToString("N");
        var chunksQuery = _db.Chunks.AsNoTracking();
        if (!string.IsNullOrEmpty(q))
            chunksQuery = chunksQuery.Where(c => c.Content != null && c.Content.Contains(q));
        var chunkEntities = await chunksQuery
            .OrderBy(c => c.DocumentId)
            .ThenBy(c => c.ChunkIndex)
            .Take(lim)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var chunks = chunkEntities.Select(c => new ContextChunk
        {
            Id = c.Id,
            DocumentId = c.DocumentId,
            Content = c.Content ?? string.Empty,
            TokenCount = c.TokenCount,
            ChunkIndex = c.ChunkIndex
        }).ToList();
        var docIds = chunkEntities.Select(c => c.DocumentId).Distinct().ToList();
        var sourceKeys = await _db.Documents.Where(d => docIds.Contains(d.Id)).Select(d => d.SourceKey).ToListAsync(cancellationToken).ConfigureAwait(false);
        var pack = new ContextPack
        {
            QueryId = qid,
            Chunks = chunks,
            SourceKeys = sourceKeys
        };
        return JsonSerializer.Serialize(pack);
    }

    /// <summary>List indexed document sources.</summary>
    /// <returns>JSON string with source keys, types, and ingestion timestamps.</returns>
    [McpServerTool(Name = "context_sources"), Description("List indexed document sources (sourceKey, sourceType, ingestedAt).")]
    public async Task<string> ContextSources(CancellationToken cancellationToken = default)
    {
        var sources = await _db.Documents.AsNoTracking()
            .Select(d => new { d.SourceKey, d.SourceType, d.IngestedAt })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(new { sources });
    }

    /// <summary>Read file content by relative path from repo root.</summary>
    /// <returns>JSON string with file path, content, and existence flag.</returns>
    [McpServerTool(Name = "repo_read"), Description("Read file content by relative path from repo root. Path must be allowed.")]
    public async Task<string> RepoRead(
        [Description("Relative path from repo root")] string path,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return JsonSerializer.Serialize(new { error = "path is required" });
        var result = await _repoFileService.ReadAsync(path, cancellationToken).ConfigureAwait(false);
        if (result == null)
            return JsonSerializer.Serialize(new { error = "path not allowed or not found" });
        return JsonSerializer.Serialize(new { path = result.RelativePath, content = result.Content, exists = result.Exists });
    }

    /// <summary>List files and directories under a relative path.</summary>
    /// <returns>JSON string with path and directory entries.</returns>
    [McpServerTool(Name = "repo_list"), Description("List files and directories under a relative path. Empty path = repo root.")]
    public async Task<string> RepoList(
        [Description("Relative path (optional, default repo root)")] string? path = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _repoFileService.ListAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(new { path = result.Path, entries = result.Entries.Select(e => new { e.Name, e.IsDirectory }).ToList() });
    }

    /// <summary>Write content to a path (audit logged).</summary>
    /// <returns>JSON string indicating write success or error.</returns>
    [McpServerTool(Name = "repo_write"), Description("Write content to a path. Path must be allowed; audit logged.")]
    public async Task<string> RepoWrite(
        [Description("Relative path from repo root")] string path,
        [Description("File content to write")] string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return JsonSerializer.Serialize(new { error = "path and content are required" });
        var result = await _repoFileService.WriteAsync(path, content ?? string.Empty, cancellationToken).ConfigureAwait(false);
        if (!result.Written)
            return JsonSerializer.Serialize(new { error = result.Error ?? "write failed" });
        return JsonSerializer.Serialize(new { path, written = true });
    }

    /// <summary>Trigger full ingestion (repo, session logs, external docs).</summary>
    /// <returns>JSON string with sync run result.</returns>
    [McpServerTool(Name = "sync_run"), Description("Trigger full ingestion (repo, session logs, external docs). Returns run result.")]
    public async Task<string> SyncRun(CancellationToken cancellationToken = default)
    {
        var result = await _coordinator.RunAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(new
        {
            result.RunId,
            result.StartedAt,
            result.CompletedAt,
            result.Status,
            result.Error,
            result.DocumentsIngested,
            result.ChunksWritten
        });
    }

    /// <summary>Get last sync run status.</summary>
    /// <returns>JSON string with last run timestamps, status, and counts.</returns>
    [McpServerTool(Name = "sync_status"), Description("Get last sync run status (lastRun, status, error, counts).")]
    public string SyncStatus()
    {
        var last = _syncStatusStore.GetLast();
        if (last == null)
            return JsonSerializer.Serialize(new { lastRun = (DateTime?)null, status = "idle", error = (string?)null });
        return JsonSerializer.Serialize(new
        {
            lastRun = last.StartedAt,
            completedAt = last.CompletedAt,
            status = last.Status,
            error = last.Error,
            documentsIngested = last.DocumentsIngested,
            chunksWritten = last.ChunksWritten
        });
    }

    // ── GROUP A: TODO tools ──────────────────────────────────────────────

    /// <summary>TR-PLANNED-013: List/search TODO items.</summary>
    [McpServerTool(Name = "todo_list"), Description("Query TODO items. Optional filters: section, priority, done.")]
    public async Task<string> TodoList(
        [Description("Section filter (e.g. mvp-app)")] string? section = null,
        [Description("Priority filter (high/medium/low)")] string? priority = null,
        [Description("Done filter (true/false)")] bool? done = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _todoService.QueryAsync(new TodoQueryRequest { Section = section, Priority = priority, Done = done }, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { items = result.Items, totalCount = result.TotalCount });
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }

    /// <summary>TR-PLANNED-013: Get a single TODO by id.</summary>
    [McpServerTool(Name = "todo_get"), Description("Get a single TODO item by its id (e.g. MVP-APP-001).")]
    public async Task<string> TodoGet(
        [Description("TODO item id")] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _todoService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (item == null) return JsonSerializer.Serialize(new { error = $"TODO '{id}' not found" });
            return JsonSerializer.Serialize(item);
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }

    /// <summary>TR-PLANNED-013: Create a new TODO item.</summary>
    [McpServerTool(Name = "todo_create"), Description("Create a new TODO item. Requires id, title, section, priority.")]
    public async Task<string> TodoCreate(
        [Description("Item id (e.g. MVP-APP-006)")] string id,
        [Description("Item title")] string title,
        [Description("Section (e.g. mvp-app)")] string section,
        [Description("Priority (high/medium/low)")] string priority,
        [Description("Estimate string")] string? estimate = null,
        [Description("Description text")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var req = new TodoCreateRequest
            {
                Id = id,
                Title = title,
                Section = section,
                Priority = priority,
                Estimate = estimate,
                Description = description != null ? new[] { description } : null
            };
            var result = await _todoService.CreateAsync(req, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true, item = result.Item });
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }

    /// <summary>TR-PLANNED-013: Update an existing TODO item.</summary>
    [McpServerTool(Name = "todo_update"), Description("Update a TODO item by id. Only provided fields are changed.")]
    public async Task<string> TodoUpdate(
        [Description("TODO item id")] string id,
        [Description("Updated title")] string? title = null,
        [Description("Updated priority")] string? priority = null,
        [Description("Mark as done")] bool? done = null,
        [Description("Updated note")] string? note = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var req = new TodoUpdateRequest { Title = title, Priority = priority, Done = done, Note = note };
            var result = await _todoService.UpdateAsync(id, req, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true, item = result.Item });
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }

    /// <summary>TR-PLANNED-013: Delete a TODO item by id.</summary>
    [McpServerTool(Name = "todo_delete"), Description("Delete a TODO item by id.")]
    public async Task<string> TodoDelete(
        [Description("TODO item id")] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _todoService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true });
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }

    // ── GROUP B: Session Log tools ───────────────────────────────────────

    /// <summary>TR-PLANNED-013: Submit a session log payload.</summary>
    [McpServerTool(Name = "sessionlog_submit"), Description("Submit (upsert) a session log. Body is JSON string conforming to UnifiedSessionLogDto.")]
    public async Task<string> SessionLogSubmit(
        [Description("JSON string of the session log payload")] string json,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<UnifiedSessionLogDto>(json, CaseInsensitiveOptions);
            if (dto == null) return JsonSerializer.Serialize(new { error = "Invalid JSON" });
            var id = await _sessionLogService.SubmitAsync(dto, cancellationToken: cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { success = true, id });
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }

    /// <summary>TR-PLANNED-013: Query session logs.</summary>
    [McpServerTool(Name = "sessionlog_query"), Description("Query session logs with optional filters: agent, model, text, from, to, limit.")]
    public async Task<string> SessionLogQuery(
        [Description("Agent filter (e.g. cursor, copilot)")] string? agent = null,
        [Description("Model filter")] string? model = null,
        [Description("Text search")] string? text = null,
        [Description("From date (ISO 8601)")] string? from = null,
        [Description("To date (ISO 8601)")] string? to = null,
        [Description("Max results (default 100)")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var req = new SessionLogQueryRequest
            {
                Agent = agent,
                Model = model,
                Text = text,
                From = from != null ? DateTimeOffset.Parse(from, System.Globalization.CultureInfo.InvariantCulture) : null,
                To = to != null ? DateTimeOffset.Parse(to, System.Globalization.CultureInfo.InvariantCulture) : null,
                Limit = limit ?? 100
            };
            var result = await _sessionLogService.QueryAsync(req, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { totalCount = result.TotalCount, items = result.Items });
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }

    /// <summary>TR-PLANNED-013: Append processing dialog items to an existing session log entry.</summary>
    [McpServerTool(Name = "sessionlog_dialog"), Description("Append processing dialog items to a session log entry.")]
    public async Task<string> SessionLogDialog(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id")] string requestId,
        [Description("JSON array of dialog items")] string itemsJson,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<ProcessingDialogItemDto>>(itemsJson, CaseInsensitiveOptions);
            if (items == null || items.Count == 0) return JsonSerializer.Serialize(new { error = "items required" });
            var count = await _sessionLogService.AppendProcessingDialogAsync(agent, sessionId, requestId, items, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { success = true, totalDialogItems = count });
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }

    // ── GROUP C: GitHub tools ────────────────────────────────────────────

    /// <summary>TR-PLANNED-013: List GitHub issues.</summary>
    [McpServerTool(Name = "github_list_issues"), Description("List GitHub issues. Optional state filter and limit.")]
    public async Task<string> GitHubListIssues(
        [Description("State filter (open/closed/all)")] string? state = null,
        [Description("Max issues to return (default 30)")] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gitHubCliService.ListIssuesAsync(state, limit, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { issues = result.Issues });
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }

    /// <summary>TR-PLANNED-013: List GitHub pull requests.</summary>
    [McpServerTool(Name = "github_list_pulls"), Description("List GitHub pull requests. Optional state filter and limit.")]
    public async Task<string> GitHubListPulls(
        [Description("State filter (open/closed/all)")] string? state = null,
        [Description("Max PRs to return (default 30)")] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gitHubCliService.ListPullsAsync(state, limit, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { pulls = result.Pulls });
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }

    /// <summary>TR-PLANNED-013: Create a GitHub issue.</summary>
    [McpServerTool(Name = "github_create_issue"), Description("Create a GitHub issue with title and optional body.")]
    public async Task<string> GitHubCreateIssue(
        [Description("Issue title")] string title,
        [Description("Issue body")] string? body = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gitHubCliService.CreateIssueAsync(title, body, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true, number = result.Number, url = result.Url });
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }

    /// <summary>TR-PLANNED-013: Comment on a GitHub issue.</summary>
    [McpServerTool(Name = "github_comment_issue"), Description("Add a comment to a GitHub issue.")]
    public async Task<string> GitHubCommentIssue(
        [Description("Issue number or id")] string issueId,
        [Description("Comment body")] string body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gitHubCliService.CommentOnIssueAsync(issueId, body, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true });
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }

    /// <summary>TR-PLANNED-013: Comment on a GitHub pull request.</summary>
    [McpServerTool(Name = "github_comment_pull"), Description("Add a comment to a GitHub pull request.")]
    public async Task<string> GitHubCommentPull(
        [Description("PR number or id")] string prId,
        [Description("Comment body")] string body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gitHubCliService.CommentOnPullAsync(prId, body, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true });
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }); }
    }
}
