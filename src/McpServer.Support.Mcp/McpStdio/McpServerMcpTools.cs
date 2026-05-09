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
/// TR-PLANNED-013: MCP tools exposed over STDIO; same capabilities as HTTP /mcpserver/context, /mcpserver/repo.
/// Includes TODO, Session Log, and GitHub tools for full STDIO parity.
/// </summary>
[McpServerToolType]
public sealed class FwhMcpTools
{
    private static readonly JsonSerializerOptions s_caseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions s_camelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
    private readonly McpDbContext _db;
    private readonly IRepoFileService _repoFileService;
    private readonly IngestionCoordinator _coordinator;
    private readonly ISyncStatusStore _syncStatusStore;
    private readonly IContextSearchService _searchService;
    private readonly IGraphRagService _graphRagService;
    private readonly WorkspaceServiceAccessor _workspaceAccessor;
    private readonly ITodoPromptService _todoPromptService;
    private readonly ISessionLogService _sessionLogService;
    private readonly IGitHubCliService _gitHubCliService;
    private readonly IRequirementsDocumentService _requirementsDocumentService;
    private readonly DesktopLaunchService _desktopLaunchService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly WorkspaceContext _workspaceContext;
    private readonly IWorkspaceService _workspaceService;
    private readonly IWorkspacePolicyService _workspacePolicyService;
    private readonly TodoServiceResolver _todoServiceResolver;
    private readonly TodoCreationService _todoCreationService;
    private readonly TodoUpdateService _todoUpdateService;
    private readonly ITodoExecutionService _todoExecutionService;
    private readonly IPromptTemplateService _promptTemplateService;
    private readonly ILogger<FwhMcpTools> _logger;


    /// <summary>TR-PLANNED-013, TR-MCP-MT-001: Constructor for DI. Uses WorkspaceServiceAccessor for workspace-aware TODO resolution.</summary>
    public FwhMcpTools(McpDbContext db,
        IRepoFileService repoFileService,
        IngestionCoordinator coordinator,
        ISyncStatusStore syncStatusStore,
        IContextSearchService searchService,
        IGraphRagService graphRagService,
        WorkspaceServiceAccessor workspaceAccessor,
        ITodoPromptService todoPromptService,
        ISessionLogService sessionLogService,
        IGitHubCliService gitHubCliService,
        IRequirementsDocumentService requirementsDocumentService,
        DesktopLaunchService desktopLaunchService,
        IHttpContextAccessor httpContextAccessor,
        WorkspaceContext workspaceContext,
        IWorkspaceService workspaceService,
        IWorkspacePolicyService workspacePolicyService,
        TodoServiceResolver todoServiceResolver,
        TodoCreationService todoCreationService,
        TodoUpdateService todoUpdateService,
        ITodoExecutionService todoExecutionService,
        IPromptTemplateService promptTemplateService,
        ILogger<FwhMcpTools> logger)
    {
        _logger = logger;
        _db = db;
        _repoFileService = repoFileService;
        _coordinator = coordinator;
        _syncStatusStore = syncStatusStore;
        _searchService = searchService;
        _graphRagService = graphRagService;
        _workspaceAccessor = workspaceAccessor;
        _todoPromptService = todoPromptService;
        _sessionLogService = sessionLogService;
        _gitHubCliService = gitHubCliService;
        _requirementsDocumentService = requirementsDocumentService;
        _desktopLaunchService = desktopLaunchService;
        _httpContextAccessor = httpContextAccessor;
        _workspaceContext = workspaceContext;
        _workspaceService = workspaceService;
        _workspacePolicyService = workspacePolicyService;
        _todoServiceResolver = todoServiceResolver;
        _todoCreationService = todoCreationService;
        _todoUpdateService = todoUpdateService;
        _todoExecutionService = todoExecutionService;
        _promptTemplateService = promptTemplateService;
    }

    /// <summary>
    /// TR-MCP-MT-001: Overrides the scoped workspace context when an explicit workspace path
    /// is provided by the MCP tool caller. Sets both the scoped <see cref="WorkspaceContext"/>
    /// and the <see cref="McpDbContext"/> workspace ID so query filters and auto-stamping apply correctly.
    /// </summary>
    private void ApplyWorkspaceOverride(string workspacePath)
    {
        _workspaceContext.WorkspacePath = workspacePath;
        _workspaceContext.SessionsPath = Path.Combine(workspacePath, "docs", "sessions");
        _workspaceContext.ExternalDocsPath = Path.Combine(workspacePath, "docs", "external");

        var ctx = _httpContextAccessor.HttpContext?.RequestServices.GetService<WorkspaceContext>();
        if (ctx is not null)
        {
            ctx.WorkspacePath = workspacePath;
            ctx.SessionsPath = _workspaceContext.SessionsPath;
            ctx.ExternalDocsPath = _workspaceContext.ExternalDocsPath;
        }

        _db.OverrideWorkspaceId(workspacePath);
    }

    private static string SerializeJson(object value) => JsonSerializer.Serialize(value, s_camelCaseOptions);

    /// <summary>Applies a natural-language workspace policy directive.</summary>
    [McpServerTool(Name = "workspace_policy_apply"), Description("Apply a natural-language workspace policy directive (ban/unban/clear for licenses, countries, organizations, or individuals).")]
    public async Task<string> WorkspacePolicyApply(
        [Description("Workspace path hint for current-scope resolution (required)")] string workspacePath,
        [Description("Natural-language policy directive")] string directive,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            return JsonSerializer.Serialize(new { success = false, error = "workspacePath is required." });
        if (string.IsNullOrWhiteSpace(directive))
            return JsonSerializer.Serialize(new { success = false, error = "directive is required." });

        ApplyWorkspaceOverride(workspacePath);
        var result = await _workspacePolicyService.ApplyAsync(
            new WorkspacePolicyApplyRequest
            {
                WorkspacePath = workspacePath,
                Directive = directive,
            },
            cancellationToken).ConfigureAwait(false);

        return JsonSerializer.Serialize(result);
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

    /// <summary>FR-MCP-065, TR-MCP-INGEST-003: Ingest context directly from a website URL.</summary>
    [McpServerTool(Name = "context_ingest_website"), Description("Ingest context directly from a website URL with bounded crawl controls.")]
    public async Task<string> ContextIngestWebsite(
        [Description("Website URL to ingest")] string url,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Crawl same-host subpages")] bool includeSubpages = false,
        [Description("Maximum pages to fetch (default 20)")] int maxPages = 20,
        [Description("Maximum crawl depth when subpages are enabled (default 1)")] int maxDepth = 1,
        [Description("Maximum bytes downloaded per page (default 262144)")] int maxBytesPerPage = 262144,
        [Description("Force refresh semantics for existing documents")] bool forceRefresh = false,
        [Description("Trigger GraphRAG index after ingest")] bool triggerGraphRagIndex = false,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _coordinator.IngestWebsiteAsync(new WebsiteIngestRequest
        {
            Url = url,
            IncludeSubpages = includeSubpages,
            MaxPages = maxPages,
            MaxDepth = maxDepth,
            MaxBytesPerPage = maxBytesPerPage,
            ForceRefresh = forceRefresh,
            TriggerGraphRagIndex = triggerGraphRagIndex,
        }, cancellationToken).ConfigureAwait(false);

        if (triggerGraphRagIndex)
        {
            try
            {
                await _graphRagService.IndexAsync(new GraphRagIndexRequest { Force = forceRefresh }, cancellationToken).ConfigureAwait(false);
                result.GraphRagIndexed = true;
            }
            catch (Exception ex)
            {
                result.GraphRagIndexed = false;
                result.GraphRagIndexError = ex.Message;
            }
        }

        return JsonSerializer.Serialize(result);
    }

    /// <summary>Get GraphRAG readiness status for the workspace.</summary>
    [McpServerTool(Name = "graphrag_status"), Description("Get GraphRAG status for the workspace (initialized, indexed, backend, last index time).")]
    public async Task<string> GraphRagStatus(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var status = await _graphRagService.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(status);
    }

    /// <summary>Trigger GraphRAG indexing for the workspace.</summary>
    [McpServerTool(Name = "graphrag_index"), Description("Initialize or rebuild GraphRAG index for the workspace.")]
    public async Task<string> GraphRagIndex(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Force re-index if true")] bool force = false,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var statusBefore = await _graphRagService.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        if (!force && string.Equals(statusBefore.State, "indexing", StringComparison.OrdinalIgnoreCase))
            return JsonSerializer.Serialize(new { error = "GraphRAG index already in progress for this workspace.", code = "index_conflict" });
        try
        {
            var status = await _graphRagService.IndexAsync(new GraphRagIndexRequest { Force = force }, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(status);
        }
        catch (InvalidOperationException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, code = "index_conflict" });
        }
    }

    /// <summary>Run a GraphRAG query with citations and optional context chunks.</summary>
    [McpServerTool(Name = "graphrag_query"), Description("Run a GraphRAG query for the workspace and return answer, citations, and optional context chunks.")]
    public async Task<string> GraphRagQuery(
        [Description("Query text")] string query,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Query mode (local/global/drift), optional")] string? mode = null,
        [Description("Maximum context chunks to return (default 20)")] int maxChunks = 20,
        [Description("Include context chunks in response (default true)")] bool includeContextChunks = true,
        [Description("Maximum entities to include (default service setting)")] int? maxEntities = null,
        [Description("Maximum relationships to include (default service setting)")] int? maxRelationships = null,
        [Description("Community depth for graph summarization (default service setting)")] int? communityDepth = null,
        [Description("Response token budget hint (optional)")] int? responseTokenBudget = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.QueryAsync(new GraphRagQueryRequest
        {
            Query = query,
            Mode = mode,
            MaxChunks = maxChunks,
            IncludeContextChunks = includeContextChunks,
            MaxEntities = maxEntities,
            MaxRelationships = maxRelationships,
            CommunityDepth = communityDepth,
            ResponseTokenBudget = responseTokenBudget,
        }, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    // ── Ad-Hoc Text Ingestion (FR-MCP-078, TR-GRAPHRAG-ADHOC-001) ──

    /// <summary>FR-MCP-078: Ingest raw text into the GraphRAG corpus.</summary>
    /// <returns>JSON string with document ID, chunk count, and source metadata.</returns>
    [McpServerTool(Name = "graphrag_ingest_text"), Description("Ingest raw text into the GraphRAG corpus, creating a document and chunks.")]
    public async Task<string> GraphRagIngestText(
        [Description("Text content to ingest")] string content,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional document title")] string? title = null,
        [Description("Source type classification (default 'adhoc-text')")] string? sourceType = null,
        [Description("Source key / path for the document")] string? sourceKey = null,
        [Description("Trigger full reindex after ingestion")] bool triggerReindex = false,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        if (string.IsNullOrWhiteSpace(content))
            return JsonSerializer.Serialize(new { error = "content is required" });
        var result = await _graphRagService.IngestTextAsync(new GraphRagIngestTextRequest
        {
            Content = content,
            Title = title,
            SourceType = sourceType,
            SourceKey = sourceKey,
            TriggerReindex = triggerReindex,
        }, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-080: List documents in the GraphRAG corpus.</summary>
    /// <returns>JSON string with paginated document list.</returns>
    [McpServerTool(Name = "graphrag_list_documents"), Description("List documents in the GraphRAG corpus with pagination and optional source type filter.")]
    public async Task<string> GraphRagListDocuments(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Number of documents to skip (default 0)")] int skip = 0,
        [Description("Maximum documents to return (default 50)")] int take = 50,
        [Description("Optional source type filter")] string? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.ListDocumentsAsync(skip, take, sourceType, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-080: Get all chunks for a specific document.</summary>
    /// <returns>JSON string with document chunks or error.</returns>
    [McpServerTool(Name = "graphrag_get_document_chunks"), Description("Retrieve all chunks for a specific GraphRAG document by document ID.")]
    public async Task<string> GraphRagGetDocumentChunks(
        [Description("Document ID")] string documentId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.GetDocumentChunksAsync(documentId, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return JsonSerializer.Serialize(new { error = $"Document '{documentId}' not found" });
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-080: Delete a document and its chunks from the corpus.</summary>
    /// <returns>JSON string with deletion result.</returns>
    [McpServerTool(Name = "graphrag_delete_document"), Description("Delete a document and its chunks from the GraphRAG corpus.")]
    public async Task<string> GraphRagDeleteDocument(
        [Description("Document ID to delete")] string documentId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.DeleteDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    // ── Entity CRUD (FR-MCP-079, TR-GRAPHRAG-ADHOC-002) ──

    /// <summary>FR-MCP-079: Create a new graph entity.</summary>
    /// <returns>JSON string with the created entity.</returns>
    [McpServerTool(Name = "graphrag_create_entity"), Description("Create a new graph entity in the GraphRAG knowledge graph.")]
    public async Task<string> GraphRagCreateEntity(
        [Description("Entity display name")] string name,
        [Description("Entity type (e.g. person, organization, concept)")] string entityType,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional description")] string? description = null,
        [Description("Optional JSON metadata")] string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.CreateEntityAsync(new GraphEntityRequest
        {
            Name = name,
            EntityType = entityType,
            Description = description,
            Metadata = metadata,
        }, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: List graph entities with pagination.</summary>
    /// <returns>JSON string with paginated entity list.</returns>
    [McpServerTool(Name = "graphrag_list_entities"), Description("List graph entities with pagination and optional type filter.")]
    public async Task<string> GraphRagListEntities(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Number of entities to skip (default 0)")] int skip = 0,
        [Description("Maximum entities to return (default 50)")] int take = 50,
        [Description("Optional entity type filter")] string? entityType = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.ListEntitiesAsync(skip, take, entityType, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: Get a graph entity by ID.</summary>
    /// <returns>JSON string with entity or error.</returns>
    [McpServerTool(Name = "graphrag_get_entity"), Description("Retrieve a graph entity by its ID.")]
    public async Task<string> GraphRagGetEntity(
        [Description("Entity ID")] string entityId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.GetEntityAsync(entityId, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return JsonSerializer.Serialize(new { error = $"Entity '{entityId}' not found" });
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: Update an existing graph entity.</summary>
    /// <returns>JSON string with updated entity or error.</returns>
    [McpServerTool(Name = "graphrag_update_entity"), Description("Update an existing graph entity by ID.")]
    public async Task<string> GraphRagUpdateEntity(
        [Description("Entity ID to update")] string entityId,
        [Description("Updated entity name")] string name,
        [Description("Updated entity type")] string entityType,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Updated description")] string? description = null,
        [Description("Updated JSON metadata")] string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.UpdateEntityAsync(entityId, new GraphEntityRequest
        {
            Name = name,
            EntityType = entityType,
            Description = description,
            Metadata = metadata,
        }, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return JsonSerializer.Serialize(new { error = $"Entity '{entityId}' not found" });
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: Delete a graph entity by ID.</summary>
    /// <returns>JSON string indicating deletion success.</returns>
    [McpServerTool(Name = "graphrag_delete_entity"), Description("Delete a graph entity by its ID.")]
    public async Task<string> GraphRagDeleteEntity(
        [Description("Entity ID to delete")] string entityId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var deleted = await _graphRagService.DeleteEntityAsync(entityId, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(new { deleted, entityId });
    }

    // ── Relationship CRUD (FR-MCP-079, TR-GRAPHRAG-ADHOC-002) ──

    /// <summary>FR-MCP-079: Create a new graph relationship.</summary>
    /// <returns>JSON string with the created relationship.</returns>
    [McpServerTool(Name = "graphrag_create_relationship"), Description("Create a new graph relationship between two entities.")]
    public async Task<string> GraphRagCreateRelationship(
        [Description("Source entity ID")] string sourceEntityId,
        [Description("Target entity ID")] string targetEntityId,
        [Description("Relationship type")] string relationshipType,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional description")] string? description = null,
        [Description("Weight/strength (default 1.0)")] double weight = 1.0,
        [Description("Optional JSON metadata")] string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.CreateRelationshipAsync(new GraphRelationshipRequest
        {
            SourceEntityId = sourceEntityId,
            TargetEntityId = targetEntityId,
            RelationshipType = relationshipType,
            Description = description,
            Weight = weight,
            Metadata = metadata,
        }, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: List graph relationships with pagination.</summary>
    /// <returns>JSON string with paginated relationship list.</returns>
    [McpServerTool(Name = "graphrag_list_relationships"), Description("List graph relationships with pagination and optional entity/type filters.")]
    public async Task<string> GraphRagListRelationships(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Number of relationships to skip (default 0)")] int skip = 0,
        [Description("Maximum relationships to return (default 50)")] int take = 50,
        [Description("Optional entity ID filter")] string? entityId = null,
        [Description("Optional relationship type filter")] string? relationshipType = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.ListRelationshipsAsync(skip, take, entityId, relationshipType, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: Get a graph relationship by ID.</summary>
    /// <returns>JSON string with relationship or error.</returns>
    [McpServerTool(Name = "graphrag_get_relationship"), Description("Retrieve a graph relationship by its ID.")]
    public async Task<string> GraphRagGetRelationship(
        [Description("Relationship ID")] string relationshipId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.GetRelationshipAsync(relationshipId, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return JsonSerializer.Serialize(new { error = $"Relationship '{relationshipId}' not found" });
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: Update an existing graph relationship.</summary>
    /// <returns>JSON string with updated relationship or error.</returns>
    [McpServerTool(Name = "graphrag_update_relationship"), Description("Update an existing graph relationship by ID.")]
    public async Task<string> GraphRagUpdateRelationship(
        [Description("Relationship ID to update")] string relationshipId,
        [Description("Source entity ID")] string sourceEntityId,
        [Description("Target entity ID")] string targetEntityId,
        [Description("Relationship type")] string relationshipType,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Updated description")] string? description = null,
        [Description("Updated weight (default 1.0)")] double weight = 1.0,
        [Description("Updated JSON metadata")] string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.UpdateRelationshipAsync(relationshipId, new GraphRelationshipRequest
        {
            SourceEntityId = sourceEntityId,
            TargetEntityId = targetEntityId,
            RelationshipType = relationshipType,
            Description = description,
            Weight = weight,
            Metadata = metadata,
        }, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return JsonSerializer.Serialize(new { error = $"Relationship '{relationshipId}' not found" });
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: Delete a graph relationship by ID.</summary>
    /// <returns>JSON string indicating deletion success.</returns>
    [McpServerTool(Name = "graphrag_delete_relationship"), Description("Delete a graph relationship by its ID.")]
    public async Task<string> GraphRagDeleteRelationship(
        [Description("Relationship ID to delete")] string relationshipId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        var deleted = await _graphRagService.DeleteRelationshipAsync(relationshipId, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(new { deleted, relationshipId });
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

    /// <summary>TR-MCP-TODO-005: Get append-only audit history for a TODO item.</summary>
    [McpServerTool(Name = "todo_audit"), Description("Get append-only audit history for a TODO item by id.")]
    public async Task<string> TodoAudit(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Maximum entries to return (default 50)")] int limit = 50,
        [Description("Entries to skip before returning results (default 0)")] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _workspaceAccessor.GetTodoService().GetAuditAsync(id, limit, offset, cancellationToken).ConfigureAwait(false);
            if (result.TotalCount == 0)
                return JsonSerializer.Serialize(new { error = $"TODO audit '{id}' not found" });

            return JsonSerializer.Serialize(new { entries = result.Entries, totalCount = result.TotalCount });
        }
        catch (NotSupportedException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-MCP-TODO-006: Get SQLite-authoritative TODO projection status.</summary>
    [McpServerTool(Name = "todo_projection_status"), Description("Get projection status for SQLite-backed TODO storage.")]
    public async Task<string> TodoProjectionStatus(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _workspaceAccessor.GetTodoService().GetProjectionStatusAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result);
        }
        catch (NotSupportedException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-MCP-TODO-006: Repair TODO.yaml projection from SQLite-authoritative TODO storage.</summary>
    [McpServerTool(Name = "todo_projection_repair"), Description("Repair TODO.yaml projection from authoritative SQLite-backed TODO storage.")]
    public async Task<string> TodoProjectionRepair(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _workspaceAccessor.GetTodoService().RepairProjectionAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result);
        }
        catch (NotSupportedException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
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
        [Description("Item id (e.g. MVP-APP-006 or ISSUE-NEW)")] string id,
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
            var result = await _todoCreationService.CreateAsync(req, cancellationToken).ConfigureAwait(false);
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
            var result = await _todoUpdateService.UpdateAsync(id, req, cancellationToken).ConfigureAwait(false);
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

    /// <summary>Move a TODO item from the source workspace to a different target workspace.</summary>
    [McpServerTool(Name = "todo_move"), Description("Move a TODO item from one workspace to another by its ID.")]
    public async Task<string> TodoMove(
        [Description("TODO item id")] string id,
        [Description("Source workspace path (required)")] string workspacePath,
        [Description("Target workspace path to move the item to")] string targetWorkspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var sourceService = _workspaceAccessor.GetTodoService();
            var item = await sourceService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (item is null) return JsonSerializer.Serialize(new { error = $"Item '{id}' not found in source workspace." });

            var targetWs = await _workspaceService.GetAsync(targetWorkspacePath, cancellationToken).ConfigureAwait(false);
            if (targetWs is null) return JsonSerializer.Serialize(new { error = $"Target workspace '{targetWorkspacePath}' not found." });

            var targetContext = new WorkspaceContext
            {
                WorkspacePath = targetWs.WorkspacePath,
                WorkspaceName = targetWs.Name,
                DataDirectory = targetWs.DataDirectory,
                TodoFilePath = targetWs.TodoPath,
            };
            var targetService = _todoServiceResolver.Resolve(targetContext);

            var createReq = new TodoCreateRequest
            {
                Id = item.Id, Title = item.Title, Section = item.Section, Priority = item.Priority,
                Estimate = item.Estimate, Description = item.Description, TechnicalDetails = item.TechnicalDetails,
                ImplementationTasks = item.ImplementationTasks, Note = item.Note, Remaining = item.Remaining,
                DependsOn = item.DependsOn, FunctionalRequirements = item.FunctionalRequirements,
                TechnicalRequirements = item.TechnicalRequirements,
            };

            var createResult = await targetService.CreateAsync(createReq, cancellationToken).ConfigureAwait(false);
            if (!createResult.Success) return JsonSerializer.Serialize(new { error = $"Failed to create in target: {createResult.Error}" });

            var deleteResult = await sourceService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            if (!deleteResult.Success) return JsonSerializer.Serialize(new { error = $"Created in target but failed to delete from source: {deleteResult.Error}" });

            return JsonSerializer.Serialize(new { success = true, movedTo = targetWs.WorkspacePath });
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
            return await CollectStreamAsync(_todoPromptService.StreamPlanAsync(id, null, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>Create a bounded Byrd iteration phase.</summary>
    [McpServerTool(Name = "create_iteration_phase"), Description("Create a bounded Byrd iteration phase aligned to requirements and scope.")]
    public async Task<string> CreateIterationPhase(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Phase name")] string name,
        [Description("Phase summary")] string summary,
        [Description("Linked requirement IDs")] string[]? requirementIds = null,
        [Description("Entry criteria")] string[]? entryCriteria = null,
        [Description("Exit criteria")] string[]? exitCriteria = null,
        [Description("Originating plan ID")] string? createdFromPlanId = null,
        [Description("Branch associated with the phase")] string? branch = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.CreateIterationPhaseAsync(workspacePath, new CreateIterationPhaseRequest
            {
                Name = name,
                Summary = summary,
                RequirementIds = requirementIds,
                EntryCriteria = entryCriteria,
                ExitCriteria = exitCriteria,
                CreatedFromPlanId = createdFromPlanId,
                Branch = branch,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Create Byrd execution TODOs from a plan.</summary>
    [McpServerTool(Name = "create_todos_from_plan"), Description("Decompose an approved plan into executable TODO items inside an iteration phase.")]
    public async Task<string> CreateTodosFromPlan(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Iteration phase ID")] string phaseId,
        [Description("Plan ID")] string planId,
        [Description("Planned TODO definitions")] PlanTodoInput[]? todos = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.CreateTodosFromPlanAsync(workspacePath, new CreateTodosFromPlanRequest
            {
                PhaseId = phaseId,
                PlanId = planId,
                Todos = todos,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Return the active Byrd execution TODO.</summary>
    [McpServerTool(Name = "get_active_todo"), Description("Return the single TODO Codex should work on next.")]
    public async Task<string> GetActiveTodo(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.GetActiveTodoAsync(workspacePath, cancellationToken).ConfigureAwait(false);
            return result is null
                ? SerializeJson(new { error = "No active TODO was found." })
                : SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Return the bounded execution context for a Byrd TODO.</summary>
    [McpServerTool(Name = "get_todo_execution_context"), Description("Hydrate a single bounded working set for a Byrd execution TODO.")]
    public async Task<string> GetTodoExecutionContext(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Maximum requirement snippets to return (default 5)")] int requirementSnippetLimit = 5,
        [Description("Maximum recent turn summaries to return (default 5)")] int sessionTurnSummaryLimit = 5,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.GetExecutionContextAsync(
                workspacePath,
                todoId,
                requirementSnippetLimit,
                sessionTurnSummaryLimit,
                cancellationToken).ConfigureAwait(false);
            return result is null
                ? SerializeJson(new { error = $"Execution TODO '{todoId}' was not found." })
                : SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Return the execution delta for a Byrd TODO since a checkpoint.</summary>
    [McpServerTool(Name = "get_todo_delta_context"), Description("Fetch only what changed since the last checkpoint for a Byrd TODO.")]
    public async Task<string> GetTodoDeltaContext(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Checkpoint ID to diff from")] string? sinceCheckpointId = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.GetDeltaContextAsync(workspacePath, todoId, sinceCheckpointId, cancellationToken).ConfigureAwait(false);
            return result is null
                ? SerializeJson(new { error = $"Execution TODO '{todoId}' was not found." })
                : SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Store the test plan for a Byrd TODO.</summary>
    [McpServerTool(Name = "set_todo_test_plan"), Description("Store test files and commands before implementation begins.")]
    public async Task<string> SetTodoTestPlan(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Whether unit tests are defined")] bool unitTestsDefined,
        [Description("Whether integration tests are defined")] bool integrationTestsDefined = false,
        [Description("Test file paths")] string[]? testFilePaths = null,
        [Description("Test commands")] string[]? testCommands = null,
        [Description("Whether unit tests are already passing")] bool? unitTestsPassing = null,
        [Description("Whether integration tests are already passing")] bool? integrationTestsPassing = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.SetTestPlanAsync(workspacePath, todoId, new SetTodoTestPlanRequest
            {
                UnitTestsDefined = unitTestsDefined,
                IntegrationTestsDefined = integrationTestsDefined,
                TestFilePaths = testFilePaths,
                TestCommands = testCommands,
                UnitTestsPassing = unitTestsPassing,
                IntegrationTestsPassing = integrationTestsPassing,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Move a Byrd TODO through its execution states.</summary>
    [McpServerTool(Name = "update_todo_status"), Description("Move a Byrd TODO through its execution states with process enforcement.")]
    public async Task<string> UpdateTodoStatus(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Target execution status")] TodoExecutionStatus targetStatus,
        [Description("Optional transition reason")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.UpdateStatusAsync(workspacePath, todoId, new UpdateTodoStatusRequest
            {
                TargetStatus = targetStatus,
                Reason = reason,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Append a checkpoint to a Byrd TODO.</summary>
    [McpServerTool(Name = "append_todo_checkpoint"), Description("Record progress, decisions, failures, or validation results for a Byrd TODO.")]
    public async Task<string> AppendTodoCheckpoint(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Checkpoint kind")] TodoCheckpointKind kind,
        [Description("Checkpoint summary")] string summary,
        [Description("Suggested next action")] string? nextAction = null,
        [Description("Requirement IDs")] string[]? requirementIds = null,
        [Description("Session turn IDs")] string[]? sessionTurnIds = null,
        [Description("Artifact IDs")] string[]? artifactIds = null,
        [Description("Commit SHAs")] string[]? commitShas = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.AppendCheckpointAsync(workspacePath, todoId, new AppendTodoCheckpointRequest
            {
                Kind = kind,
                Summary = summary,
                NextAction = nextAction,
                RequirementIds = requirementIds,
                SessionTurnIds = sessionTurnIds,
                ArtifactIds = artifactIds,
                CommitShas = commitShas,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Record the validation result for a Byrd TODO.</summary>
    [McpServerTool(Name = "record_todo_validation_result"), Description("Persist validation state, including device validation artifacts, for a Byrd TODO.")]
    public async Task<string> RecordTodoValidationResult(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Validation result string")] string result,
        [Description("Validation summary")] string? summary = null,
        [Description("Artifact IDs")] string[]? artifactIds = null,
        [Description("Session turn IDs")] string[]? sessionTurnIds = null,
        [Description("Whether unit tests are passing")] bool? unitTestsPassing = null,
        [Description("Whether integration tests are passing")] bool? integrationTestsPassing = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var payload = await _todoExecutionService.RecordValidationResultAsync(workspacePath, todoId, new RecordTodoValidationResultRequest
            {
                Result = result,
                Summary = summary,
                ArtifactIds = artifactIds,
                SessionTurnIds = sessionTurnIds,
                UnitTestsPassing = unitTestsPassing,
                IntegrationTestsPassing = integrationTestsPassing,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Return the next ready Byrd TODO.</summary>
    [McpServerTool(Name = "get_next_ready_todo"), Description("Advance work without rereading the whole plan.")]
    public async Task<string> GetNextReadyTodo(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.GetNextReadyTodoAsync(workspacePath, cancellationToken).ConfigureAwait(false);
            return result is null
                ? SerializeJson(new { error = "No ready TODO was found." })
                : SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Link historical session turns to a Byrd TODO.</summary>
    [McpServerTool(Name = "link_todo_to_session_turns"), Description("Attach historical evidence to a Byrd TODO without duplicating log content.")]
    public async Task<string> LinkTodoToSessionTurns(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Session turn IDs")] string[]? sessionTurnIds = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.LinkTodoToSessionTurnsAsync(workspacePath, todoId, new LinkTodoToSessionTurnsRequest
            {
                SessionTurnIds = sessionTurnIds,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Perform a safe Android ADB step.</summary>
    [McpServerTool(Name = "adb_step"), Description("Perform a fixed safe ADB action such as screenshot, tap, swipe, text, keyevent, wait, launch_app, or get_focus.")]
    public async Task<string> AdbStep(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("ADB action")] AdbStepAction action,
        [Description("Optional device serial")] string? deviceSerial = null,
        [Description("Capture a screenshot after the action")] bool captureScreenshot = false,
        [Description("Optional user-facing instruction")] string? instruction = null,
        [Description("Tap X coordinate")] int? x = null,
        [Description("Tap Y coordinate")] int? y = null,
        [Description("Swipe start X coordinate")] int? startX = null,
        [Description("Swipe start Y coordinate")] int? startY = null,
        [Description("Swipe end X coordinate")] int? endX = null,
        [Description("Swipe end Y coordinate")] int? endY = null,
        [Description("Optional duration in milliseconds")] int? durationMs = null,
        [Description("Text payload")] string? text = null,
        [Description("Key event value")] string? keyEvent = null,
        [Description("Package name to launch")] string? packageName = null,
        [Description("Activity name for explicit launches")] string? activityName = null,
        [Description("Wait duration in milliseconds")] int? waitMilliseconds = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.AdbStepAsync(workspacePath, new AdbStepRequest
            {
                DeviceSerial = deviceSerial,
                Action = action,
                CaptureScreenshot = captureScreenshot,
                Instruction = instruction,
                X = x,
                Y = y,
                StartX = startX,
                StartY = startY,
                EndX = endX,
                EndY = endY,
                DurationMs = durationMs,
                Text = text,
                KeyEvent = keyEvent,
                PackageName = packageName,
                ActivityName = activityName,
                WaitMilliseconds = waitMilliseconds,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
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

    /// <summary>REQ-MGMT-001: Generate requirements documents as Markdown or workspace files.</summary>
    [McpServerTool(Name = "requirements_generate"), Description("Generate requirements documents. doc = functional|technical|testing|mapping|all (default all). format = markdown|wiki. doc=all writes files to the workspace and returns export metadata.")]
    public async Task<string> RequirementsGenerate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Document selector: functional, technical, testing, mapping, or all")] string? doc = "all",
        [Description("Output format: markdown or wiki")] string? format = "markdown",
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseRequirementsDocType(doc, out var docType))
                return JsonSerializer.Serialize(new { error = "Unsupported doc. Expected functional|technical|testing|mapping|all." });

            var normalizedFormat = (format ?? "markdown").Trim().ToLowerInvariant();
            if (normalizedFormat == "wiki")
            {
                if (docType != RequirementsDocType.All)
                    return JsonSerializer.Serialize(new { error = "Wiki generation requires doc=all." });

                var export = await _requirementsDocumentService.GenerateWikiAsync(
                    Path.Combine(workspacePath, "docs", "Project", "wiki"),
                    ct: cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Serialize(export, s_camelCaseOptions);
            }

            if (normalizedFormat is not "markdown" and not "yaml")
                return JsonSerializer.Serialize(new { error = "Unsupported format. Expected markdown|yaml|wiki." });

            if (docType == RequirementsDocType.All)
            {
                var export = await _requirementsDocumentService.GenerateAllAsync(
                    Path.Combine(workspacePath, "docs", "Project"),
                    ct: cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Serialize(export, s_camelCaseOptions);
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
    [McpServerTool(Name = "requirements_create"), Description("Create a requirement entry. type = fr|tr|test|mapping. For mapping, body is comma-separated TR ids and testIds is comma-separated TEST ids.")]
    public async Task<string> RequirementsCreate(
        [Description("Entry type: fr, tr, test, or mapping")] string type,
        [Description("Entry id (FR/TR/TEST id or FR id for mapping rows)")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Title (required for fr; optional for tr; ignored for test/mapping)")] string? title = null,
        [Description("Body text (required for fr/tr/test; for mapping use comma-separated TR ids)")] string? body = null,
        [Description("Comma-separated TEST ids for mapping rows")] string? testIds = null,
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
                    var mapping = new FrTrMapping(id, ParseMappingIds(body), ParseMappingIds(testIds));
                    await _requirementsDocumentService.UpsertMappingAsync(mapping, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = mapping });
                }
                default:
                    return JsonSerializer.Serialize(new { error = "Unsupported type." });
            }
        }
        catch (RequirementsRepositoryException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
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
        [Description("Updated comma-separated TEST ids for mapping rows")] string? testIds = null,
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
                        : ParseMappingIds(body);
                    var targetTestIds = testIds is null && existing is not null
                        ? existing.TestIds
                        : ParseMappingIds(testIds);
                    var updated = new FrTrMapping(id, trIds, targetTestIds);
                    await _requirementsDocumentService.UpsertMappingAsync(updated, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = updated });
                }
                default:
                    return JsonSerializer.Serialize(new { error = "Unsupported type." });
            }
        }
        catch (RequirementsRepositoryException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
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
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
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

    /// <summary>Launch a process on the interactive desktop using CreateProcessWithTokenW.</summary>
    /// <returns>JSON result with processId, exitCode, or error.</returns>
    [McpServerTool(Name = "desktop_launch"), Description("Launch a desktop process using CreateProcessWithTokenW. Use this to open GUI applications on the user's interactive desktop.")]
    public async Task<string> DesktopLaunch(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Full path to executable")] string executablePath,
        [Description("Command-line arguments")] string? arguments = null,
        [Description("Working directory for the process")] string? workingDirectory = null,
        [Description("JSON object of environment variables to set")] string? environmentVariables = null,
        [Description("If true, launch without a visible window")] bool createNoWindow = false,
        [Description("Window style: Normal, Hidden, Minimized, Maximized")] string windowStyle = "Normal",
        [Description("If true, wait for the process to exit before returning")] bool waitForExit = false,
        [Description("Timeout in ms when waiting for exit")] int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            Dictionary<string, string>? environmentVariablesMap = null;
            if (!string.IsNullOrWhiteSpace(environmentVariables))
            {
                try
                {
                    environmentVariablesMap = JsonSerializer.Deserialize<Dictionary<string, string>>(environmentVariables, s_caseInsensitiveOptions);
                }
                catch (JsonException ex)
                {
                    return JsonSerializer.Serialize(
                        new DesktopLaunchResult
                        {
                            Success = false,
                            ErrorMessage = $"Invalid environmentVariables JSON: {ex.Message}"
                        },
                        s_caseInsensitiveOptions);
                }
            }

            var result = await _desktopLaunchService.LaunchAsync(
                    workspacePath,
                    new DesktopLaunchRequest
                    {
                        ExecutablePath = executablePath,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,
                        EnvironmentVariables = environmentVariablesMap,
                        CreateNoWindow = createNoWindow,
                        WindowStyle = windowStyle,
                        WaitForExit = waitForExit,
                        TimeoutMs = timeoutMs
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(
                new DesktopLaunchResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                },
                s_caseInsensitiveOptions);
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

    private static IReadOnlyList<string> ParseMappingIds(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return Array.Empty<string>();

        return body
            .Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    // ── Prompt Template Tools ──

    /// <summary>MCP tool: list/filter prompt templates.</summary>
    [McpServerTool(Name = "prompt_template_list"), Description("List prompt templates. Optional filters: category, tag, keyword.")]
    public async Task<string> PromptTemplateList(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional category filter")] string? category = null,
        [Description("Optional tag filter")] string? tag = null,
        [Description("Optional keyword search")] string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var result = await _promptTemplateService.QueryAsync(category, tag, keyword, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MCP tool: get a single prompt template.</summary>
    [McpServerTool(Name = "prompt_template_get"), Description("Get a single prompt template by ID.")]
    public async Task<string> PromptTemplateGet(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Template identifier")] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var result = await _promptTemplateService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (result is null)
                return JsonSerializer.Serialize(new { error = $"Template '{id}' not found." });
            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MCP tool: create a prompt template.</summary>
    [McpServerTool(Name = "prompt_template_create"), Description("Create a new prompt template.")]
    public async Task<string> PromptTemplateCreate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Unique kebab-case ID")] string id,
        [Description("Template title")] string title,
        [Description("Grouping category")] string category,
        [Description("Template body content (Handlebars)")] string content,
        [Description("Comma-separated tags")] string? tags = null,
        [Description("Template description")] string? description = null,
        [Description("Rendering engine (default: handlebars)")] string? engine = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var request = new Models.PromptTemplateCreateRequest
            {
                Id = id,
                Title = title,
                Category = category,
                Content = content,
                Tags = string.IsNullOrWhiteSpace(tags)
                    ? null
                    : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                Description = description,
                Engine = engine,
            };
            var result = await _promptTemplateService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MCP tool: update a prompt template.</summary>
    [McpServerTool(Name = "prompt_template_update"), Description("Update an existing prompt template. Null fields are not changed.")]
    public async Task<string> PromptTemplateUpdate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Template identifier")] string id,
        [Description("Updated title")] string? title = null,
        [Description("Updated category")] string? category = null,
        [Description("Updated content")] string? content = null,
        [Description("Updated comma-separated tags")] string? tags = null,
        [Description("Updated description")] string? description = null,
        [Description("Updated engine")] string? engine = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var request = new Models.PromptTemplateUpdateRequest
            {
                Title = title,
                Category = category,
                Content = content,
                Tags = tags is not null
                    ? tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                    : null,
                Description = description,
                Engine = engine,
            };
            var result = await _promptTemplateService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MCP tool: delete a prompt template.</summary>
    [McpServerTool(Name = "prompt_template_delete"), Description("Delete a prompt template by ID.")]
    public async Task<string> PromptTemplateDelete(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Template identifier")] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var result = await _promptTemplateService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MCP tool: test/render a prompt template with sample data.</summary>
    [McpServerTool(Name = "prompt_template_test"), Description("Test/render a prompt template with sample variable data. Provide templateId for stored templates or inlineTemplate for ad-hoc testing.")]
    public async Task<string> PromptTemplateTest(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("JSON object of variable values")] string variablesJson,
        [Description("Template ID (for stored templates)")] string? templateId = null,
        [Description("Inline template content (for ad-hoc testing)")] string? inlineTemplate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var variables = string.IsNullOrWhiteSpace(variablesJson)
                ? new Dictionary<string, object?>()
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(variablesJson, s_caseInsensitiveOptions) ?? new();

            Models.PromptTemplateTestResult result;
            if (!string.IsNullOrWhiteSpace(templateId))
            {
                var request = new Models.PromptTemplateTestRequest { Variables = variables };
                result = await _promptTemplateService.TestAsync(templateId, request, cancellationToken).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(inlineTemplate))
            {
                var request = new Models.PromptTemplateTestRequest { Variables = variables, InlineTemplate = inlineTemplate };
                result = await _promptTemplateService.TestInlineAsync(request, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return JsonSerializer.Serialize(new { error = "Either templateId or inlineTemplate must be provided." });
            }

            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
