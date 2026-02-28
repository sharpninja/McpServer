// TR-PLANNED-013 / FR-SUPPORT-010: MCP tools for STDIO transport (mirrors HTTP API capabilities).

using System.ComponentModel;
using System.Text.Json;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.McpStdio;

/// <summary>
/// TR-PLANNED-013: MCP tools exposed over STDIO; same capabilities as HTTP /mcp/context, /mcp/repo, /mcp/sync.
/// Includes TODO, Session Log, and GitHub tools for full STDIO parity.
/// </summary>
[McpServerToolType]
public sealed class FwhMcpTools
{
    private static readonly JsonSerializerOptions s_caseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly McpDbContext _db;
    private readonly IRepoFileService _repoFileService;
    private readonly IngestionCoordinator _coordinator;
    private readonly ISyncStatusStore _syncStatusStore;
    private readonly IContextSearchService _searchService;
    private readonly WorkspaceServiceAccessor _workspaceAccessor;
    private readonly ITodoPromptService _todoPromptService;
    private readonly ISessionLogService _sessionLogService;
    private readonly IGitHubCliService _gitHubCliService;
    private readonly IRequirementsDocumentService _requirementsDocumentService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<FwhMcpTools> _logger;


    /// <summary>TR-PLANNED-013, TR-MCP-MT-001: Constructor for DI. Uses WorkspaceServiceAccessor for workspace-aware TODO resolution.</summary>
    public FwhMcpTools(McpDbContext db,
        IRepoFileService repoFileService,
        IngestionCoordinator coordinator,
        ISyncStatusStore syncStatusStore,
        IContextSearchService searchService,
        WorkspaceServiceAccessor workspaceAccessor,
        ITodoPromptService todoPromptService,
        ISessionLogService sessionLogService,
        IGitHubCliService gitHubCliService,
        IRequirementsDocumentService requirementsDocumentService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<FwhMcpTools> logger)
    {
        _logger = logger;
        _db = db;
        _repoFileService = repoFileService;
        _coordinator = coordinator;
        _syncStatusStore = syncStatusStore;
        _searchService = searchService;
        _workspaceAccessor = workspaceAccessor;
        _todoPromptService = todoPromptService;
        _sessionLogService = sessionLogService;
        _gitHubCliService = gitHubCliService;
        _requirementsDocumentService = requirementsDocumentService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// TR-MCP-MT-001: Overrides the scoped workspace context when an explicit workspace path
    /// is provided by the MCP tool caller. Sets both the scoped <see cref="WorkspaceContext"/>
    /// and the <see cref="McpDbContext"/> workspace ID so query filters and auto-stamping apply correctly.
    /// </summary>
    private void ApplyWorkspaceOverride(string workspacePath)
    {
        var ctx = _httpContextAccessor.HttpContext?.RequestServices.GetService<WorkspaceContext>();
        if (ctx is not null)
            ctx.WorkspacePath = workspacePath;

        _db.OverrideWorkspaceId(workspacePath);
    }

    /// <summary>Search indexed context chunks by query text.</summary>
    /// <returns>JSON string with matching chunks and source keys.</returns>
    [McpServerTool(Name = "context_search"), Description("Search indexed context chunks by query text. Optional sourceType filter and limit (1-100).")]
    public async Task<string> ContextSearch(
        [Description("Search query text")] string query,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Max chunks to return (default 20)")] int limit = 20,
        [Description("Optional source type filter")] string? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
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
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Max chunks in pack (default 20)")] int limit = 20,
        [Description("Optional query id for reproducibility")] string? queryId = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
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
    public async Task<string> ContextSources(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
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
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
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
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Relative path (optional, default repo root)")] string? path = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _repoFileService.ListAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(new { path = result.Path, entries = result.Entries.Select(e => new { e.Name, e.IsDirectory }).ToList() });
    }

    /// <summary>Write content to a path (audit logged).</summary>
    /// <returns>JSON string indicating write success or error.</returns>
    [McpServerTool(Name = "repo_write"), Description("Write content to a path. Path must be allowed; audit logged.")]
    public async Task<string> RepoWrite(
        [Description("Relative path from repo root")] string path,
        [Description("File content to write")] string content,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
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
    public async Task<string> SyncRun(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
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
    public string SyncStatus(
        [Description("Workspace path (required)")] string workspacePath)
    {
        ApplyWorkspaceOverride(workspacePath);
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
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Section filter (e.g. mvp-app)")] string? section = null,
        [Description("Priority filter (high/medium/low)")] string? priority = null,
        [Description("Done filter (true/false)")] bool? done = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _workspaceAccessor.GetTodoService().QueryAsync(new TodoQueryRequest { Section = section, Priority = priority, Done = done }, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { items = result.Items, totalCount = result.TotalCount });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-013: Get a single TODO by id.</summary>
    [McpServerTool(Name = "todo_get"), Description("Get a single TODO item by its id (e.g. MVP-APP-001).")]
    public async Task<string> TodoGet(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var item = await _workspaceAccessor.GetTodoService().GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (item == null) return JsonSerializer.Serialize(new { error = $"TODO '{id}' not found" });
            return JsonSerializer.Serialize(item);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-013: Create a new TODO item.</summary>
    [McpServerTool(Name = "todo_create"), Description("Create a new TODO item. Requires id, title, section, priority.")]
    public async Task<string> TodoCreate(
        [Description("Item id (e.g. MVP-APP-006)")] string id,
        [Description("Item title")] string title,
        [Description("Section (e.g. mvp-app)")] string section,
        [Description("Priority (high/medium/low)")] string priority,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Estimate string")] string? estimate = null,
        [Description("Description text")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
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
            var result = await _workspaceAccessor.GetTodoService().CreateAsync(req, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true, item = result.Item });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-013: Update an existing TODO item.</summary>
    [McpServerTool(Name = "todo_update"), Description("Update a TODO item by id. Only provided fields are changed.")]
    public async Task<string> TodoUpdate(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Updated title")] string? title = null,
        [Description("Updated priority")] string? priority = null,
        [Description("Mark as done")] bool? done = null,
        [Description("Updated note")] string? note = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var req = new TodoUpdateRequest { Title = title, Priority = priority, Done = done, Note = note };
            var result = await _workspaceAccessor.GetTodoService().UpdateAsync(id, req, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true, item = result.Item });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-013: Delete a TODO item by id.</summary>
    [McpServerTool(Name = "todo_delete"), Description("Delete a TODO item by id.")]
    public async Task<string> TodoDelete(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _workspaceAccessor.GetTodoService().DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MVP-MCP-002: Invoke Copilot to generate a status report for a TODO item.</summary>
    [McpServerTool(Name = "todo_status"), Description("Invoke Copilot to generate a status report for a TODO item in the workspace.")]
    public async Task<string> TodoStatus(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            return await CollectStreamAsync(_todoPromptService.StreamStatusAsync(id, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MVP-MCP-002: Invoke Copilot to implement a TODO item in the workspace.</summary>
    [McpServerTool(Name = "todo_implement"), Description("Invoke Copilot to implement a TODO item, working through each task in the workspace.")]
    public async Task<string> TodoImplement(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            return await CollectStreamAsync(_todoPromptService.StreamImplementAsync(id, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MVP-MCP-002: Invoke Copilot to create a detailed implementation plan for a TODO item.</summary>
    [McpServerTool(Name = "todo_plan"), Description("Invoke Copilot to create a detailed implementation plan for a TODO item in the workspace.")]
    public async Task<string> TodoPlan(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            return await CollectStreamAsync(_todoPromptService.StreamPlanAsync(id, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static async Task<string> CollectStreamAsync(IAsyncEnumerable<string> lines)
    {
        var sb = new System.Text.StringBuilder();
        await foreach (var line in lines.ConfigureAwait(false))
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append(line);
        }
        return sb.ToString();
    }

    // ── GROUP A2: Requirements management tools ──────────────────────────

    /// <summary>REQ-MGMT-001: List requirements entries by type (fr/tr/test/mapping/all).</summary>
    [McpServerTool(Name = "requirements_list"), Description("List requirements entries. type = fr|tr|test|mapping|all (default all).")]
    public async Task<string> RequirementsList(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Entry type: fr, tr, test, mapping, or all")] string? type = "all",
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseRequirementsEntityType(type, out var entityType))
                return JsonSerializer.Serialize(new { error = "Unsupported type. Expected fr|tr|test|mapping|all." });

            return entityType switch
            {
                RequirementsEntityType.Functional => JsonSerializer.Serialize(new { type = "fr", items = await _requirementsDocumentService.GetAllFrAsync(cancellationToken).ConfigureAwait(false) }),
                RequirementsEntityType.Technical => JsonSerializer.Serialize(new { type = "tr", items = await _requirementsDocumentService.GetAllTrAsync(cancellationToken).ConfigureAwait(false) }),
                RequirementsEntityType.Testing => JsonSerializer.Serialize(new { type = "test", items = await _requirementsDocumentService.GetAllTestAsync(cancellationToken).ConfigureAwait(false) }),
                RequirementsEntityType.Mapping => JsonSerializer.Serialize(new { type = "mapping", items = await _requirementsDocumentService.GetAllMappingsAsync(cancellationToken).ConfigureAwait(false) }),
                RequirementsEntityType.All => JsonSerializer.Serialize(new
                {
                    functional = await _requirementsDocumentService.GetAllFrAsync(cancellationToken).ConfigureAwait(false),
                    technical = await _requirementsDocumentService.GetAllTrAsync(cancellationToken).ConfigureAwait(false),
                    testing = await _requirementsDocumentService.GetAllTestAsync(cancellationToken).ConfigureAwait(false),
                    mapping = await _requirementsDocumentService.GetAllMappingsAsync(cancellationToken).ConfigureAwait(false)
                }),
                _ => JsonSerializer.Serialize(new { error = "Unsupported type." })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>REQ-MGMT-001: Generate requirements documents as Markdown (doc=all concatenates all docs).</summary>
    [McpServerTool(Name = "requirements_generate"), Description("Generate requirements documents as Markdown. doc = functional|technical|testing|mapping|all (default all).")]
    public async Task<string> RequirementsGenerate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Document selector: functional, technical, testing, mapping, or all")] string? doc = "all",
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseRequirementsDocType(doc, out var docType))
                return JsonSerializer.Serialize(new { error = "Unsupported doc. Expected functional|technical|testing|mapping|all." });

            if (docType == RequirementsDocType.All)
            {
                var functional = await _requirementsDocumentService.GenerateDocumentAsync(RequirementsDocType.Functional, cancellationToken).ConfigureAwait(false);
                var technical = await _requirementsDocumentService.GenerateDocumentAsync(RequirementsDocType.Technical, cancellationToken).ConfigureAwait(false);
                var testing = await _requirementsDocumentService.GenerateDocumentAsync(RequirementsDocType.Testing, cancellationToken).ConfigureAwait(false);
                var mapping = await _requirementsDocumentService.GenerateDocumentAsync(RequirementsDocType.Mapping, cancellationToken).ConfigureAwait(false);
                return string.Join(
                    "\n\n---\n\n",
                    functional.Content.TrimEnd(),
                    technical.Content.TrimEnd(),
                    testing.Content.TrimEnd(),
                    mapping.Content.TrimEnd());
            }

            var result = await _requirementsDocumentService.GenerateDocumentAsync(docType, cancellationToken).ConfigureAwait(false);
            return result.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>REQ-MGMT-001: Create a requirement or mapping row.</summary>
    [McpServerTool(Name = "requirements_create"), Description("Create a requirement entry. type = fr|tr|test|mapping. For mapping, body is a comma-separated TR id list.")]
    public async Task<string> RequirementsCreate(
        [Description("Entry type: fr, tr, test, or mapping")] string type,
        [Description("Entry id (FR/TR/TEST id or FR id for mapping rows)")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Title (required for fr; optional for tr; ignored for test/mapping)")] string? title = null,
        [Description("Body text (required for fr/tr/test; for mapping use comma-separated TR ids)")] string? body = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseRequirementsEntityType(type, out var entityType) || entityType == RequirementsEntityType.All)
                return JsonSerializer.Serialize(new { error = "Unsupported type. Expected fr|tr|test|mapping." });

            switch (entityType)
            {
                case RequirementsEntityType.Functional:
                {
                    var entry = new FrEntry(id, title ?? string.Empty, body ?? string.Empty);
                    await _requirementsDocumentService.AddFrAsync(entry, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = entry });
                }
                case RequirementsEntityType.Technical:
                {
                    var entry = new TrEntry(id, title ?? string.Empty, body ?? string.Empty);
                    await _requirementsDocumentService.AddTrAsync(entry, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = entry });
                }
                case RequirementsEntityType.Testing:
                {
                    var condition = string.IsNullOrWhiteSpace(body) ? (title ?? string.Empty) : body;
                    var entry = new TestEntry(id, condition);
                    await _requirementsDocumentService.AddTestAsync(entry, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = entry });
                }
                case RequirementsEntityType.Mapping:
                {
                    var mapping = new FrTrMapping(id, ParseMappingTrIds(body));
                    await _requirementsDocumentService.UpsertMappingAsync(mapping, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = mapping });
                }
                default:
                    return JsonSerializer.Serialize(new { error = "Unsupported type." });
            }
        }
        catch (RequirementsRepositoryException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>REQ-MGMT-001: Update a requirement or mapping row. Omitted fields remain unchanged.</summary>
    [McpServerTool(Name = "requirements_update"), Description("Update a requirement entry. type = fr|tr|test|mapping. Omitted title/body values keep the current value.")]
    public async Task<string> RequirementsUpdate(
        [Description("Entry type: fr, tr, test, or mapping")] string type,
        [Description("Entry id (FR/TR/TEST id or FR id for mapping rows)")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Updated title (fr/tr only)")] string? title = null,
        [Description("Updated body text or mapping TR id list")] string? body = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseRequirementsEntityType(type, out var entityType) || entityType == RequirementsEntityType.All)
                return JsonSerializer.Serialize(new { error = "Unsupported type. Expected fr|tr|test|mapping." });

            switch (entityType)
            {
                case RequirementsEntityType.Functional:
                {
                    var existing = await _requirementsDocumentService.GetFrAsync(id, cancellationToken).ConfigureAwait(false);
                    if (existing is null) return JsonSerializer.Serialize(new { error = $"FR '{id}' not found." });
                    var updated = existing with
                    {
                        Title = title ?? existing.Title,
                        Body = body ?? existing.Body
                    };
                    await _requirementsDocumentService.UpdateFrAsync(updated, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = updated });
                }
                case RequirementsEntityType.Technical:
                {
                    var existing = await _requirementsDocumentService.GetTrAsync(id, cancellationToken).ConfigureAwait(false);
                    if (existing is null) return JsonSerializer.Serialize(new { error = $"TR '{id}' not found." });
                    var updated = existing with
                    {
                        Title = title ?? existing.Title,
                        Body = body ?? existing.Body
                    };
                    await _requirementsDocumentService.UpdateTrAsync(updated, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = updated });
                }
                case RequirementsEntityType.Testing:
                {
                    var existing = await _requirementsDocumentService.GetTestAsync(id, cancellationToken).ConfigureAwait(false);
                    if (existing is null) return JsonSerializer.Serialize(new { error = $"TEST '{id}' not found." });
                    var updated = existing with
                    {
                        Condition = body ?? title ?? existing.Condition
                    };
                    await _requirementsDocumentService.UpdateTestAsync(updated, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = updated });
                }
                case RequirementsEntityType.Mapping:
                {
                    var existing = await _requirementsDocumentService.GetMappingAsync(id, cancellationToken).ConfigureAwait(false);
                    var trIds = body is null && existing is not null
                        ? existing.TrIds
                        : ParseMappingTrIds(body);
                    var updated = new FrTrMapping(id, trIds);
                    await _requirementsDocumentService.UpsertMappingAsync(updated, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = updated });
                }
                default:
                    return JsonSerializer.Serialize(new { error = "Unsupported type." });
            }
        }
        catch (RequirementsRepositoryException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>REQ-MGMT-001: Delete a requirement or mapping row by id.</summary>
    [McpServerTool(Name = "requirements_delete"), Description("Delete a requirement entry. type = fr|tr|test|mapping.")]
    public async Task<string> RequirementsDelete(
        [Description("Entry type: fr, tr, test, or mapping")] string type,
        [Description("Entry id (FR/TR/TEST id or FR id for mapping rows)")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseRequirementsEntityType(type, out var entityType) || entityType == RequirementsEntityType.All)
                return JsonSerializer.Serialize(new { error = "Unsupported type. Expected fr|tr|test|mapping." });

            switch (entityType)
            {
                case RequirementsEntityType.Functional:
                    await _requirementsDocumentService.DeleteFrAsync(id, cancellationToken).ConfigureAwait(false);
                    break;
                case RequirementsEntityType.Technical:
                    await _requirementsDocumentService.DeleteTrAsync(id, cancellationToken).ConfigureAwait(false);
                    break;
                case RequirementsEntityType.Testing:
                    await _requirementsDocumentService.DeleteTestAsync(id, cancellationToken).ConfigureAwait(false);
                    break;
                case RequirementsEntityType.Mapping:
                    await _requirementsDocumentService.DeleteMappingAsync(id, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    return JsonSerializer.Serialize(new { error = "Unsupported type." });
            }

            return JsonSerializer.Serialize(new { success = true });
        }
        catch (RequirementsRepositoryException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // ── GROUP B: Session Log tools ───────────────────────────────────────

    /// <summary>TR-PLANNED-013: Submit a session log payload.</summary>
    [McpServerTool(Name = "sessionlog_submit"), Description("Submit (upsert) a session log. Body is JSON string conforming to UnifiedSessionLogDto.")]
    public async Task<string> SessionLogSubmit(
        [Description("JSON string of the session log payload")] string json,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var dto = JsonSerializer.Deserialize<UnifiedSessionLogDto>(json, s_caseInsensitiveOptions);
            if (dto == null) return JsonSerializer.Serialize(new { error = "Invalid JSON" });
            var id = await _sessionLogService.SubmitAsync(dto, cancellationToken: cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { success = true, id });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-013: Query session logs.</summary>
    [McpServerTool(Name = "sessionlog_query"), Description("Query session logs with optional filters: agent, model, text, from, to, limit.")]
    public async Task<string> SessionLogQuery(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Agent filter (e.g. cursor, copilot)")] string? agent = null,
        [Description("Model filter")] string? model = null,
        [Description("Text search")] string? text = null,
        [Description("From date (ISO 8601)")] string? from = null,
        [Description("To date (ISO 8601)")] string? to = null,
        [Description("Max results (default 100)")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
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
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-013: Append processing dialog items to an existing session log entry.</summary>
    [McpServerTool(Name = "sessionlog_dialog"), Description("Append processing dialog items to a session log entry.")]
    public async Task<string> SessionLogDialog(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id")] string requestId,
        [Description("JSON array of dialog items")] string itemsJson,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var items = JsonSerializer.Deserialize<List<ProcessingDialogItemDto>>(itemsJson, s_caseInsensitiveOptions);
            if (items == null || items.Count == 0) return JsonSerializer.Serialize(new { error = "items required" });
            var count = await _sessionLogService.AppendProcessingDialogAsync(agent, sessionId, requestId, items, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { success = true, totalDialogItems = count });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
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
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
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
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
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
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
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
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
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
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private enum RequirementsEntityType
    {
        Functional,
        Technical,
        Testing,
        Mapping,
        All
    }

    private static bool TryParseRequirementsDocType(string? raw, out RequirementsDocType docType)
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

    private static bool TryParseRequirementsEntityType(string? raw, out RequirementsEntityType entityType)
    {
        switch ((raw ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "functional":
            case "fr":
                entityType = RequirementsEntityType.Functional;
                return true;
            case "technical":
            case "tr":
                entityType = RequirementsEntityType.Technical;
                return true;
            case "testing":
            case "test":
                entityType = RequirementsEntityType.Testing;
                return true;
            case "mapping":
                entityType = RequirementsEntityType.Mapping;
                return true;
            case "all":
                entityType = RequirementsEntityType.All;
                return true;
            default:
                entityType = default;
                return false;
        }
    }

    private static IReadOnlyList<string> ParseMappingTrIds(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return Array.Empty<string>();

        return body
            .Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
