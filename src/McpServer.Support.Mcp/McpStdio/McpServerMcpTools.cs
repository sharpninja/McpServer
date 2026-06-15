// TR-PLANNED-013 / FR-SUPPORT-010: MCP tools for STDIO transport (mirrors HTTP API capabilities).

using System.ComponentModel;
using System.Text.Json;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.McpStdio;

/// <summary>
/// TR-PLANNED-013: MCP tools exposed over STDIO; same capabilities as HTTP /mcpserver/context, /mcpserver/repo.
/// Includes TODO, Session Log, and GitHub tools for full STDIO parity.
/// </summary>
[McpServerToolType]
public sealed partial class FwhMcpTools
{
    private const string DeferredContextMutationMessage =
        "Context ingestion and rebuild mutations are not transaction compensated while required turn transactions are active.";

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
    private readonly IMemoryService _memoryService;
    private readonly ITransactionGatedMemoryService? _memoryMutations;
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
    private readonly ITransactionGatedTodoMutationService? _todoMutations;
    private readonly ITodoExecutionService _todoExecutionService;
    private readonly IPromptTemplateService _promptTemplateService;
    private readonly IBrainSlotRegistryService? _brainSlotRegistry;
    private readonly IBrainSlotInvocationService? _brainSlotInvocation;
    private readonly IQuadBrainOrchestrationService? _quadBrainOrchestration;
    private readonly ITurnTransactionCoordinator? _transactionCoordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;
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
        IMemoryService memoryService,
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
        ILogger<FwhMcpTools> logger,
        ITransactionGatedMemoryService? memoryMutations = null,
        ITransactionGatedTodoMutationService? todoMutations = null,
        ITurnTransactionCoordinator? transactionCoordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null,
        IBrainSlotRegistryService? brainSlotRegistry = null,
        IBrainSlotInvocationService? brainSlotInvocation = null,
        IQuadBrainOrchestrationService? quadBrainOrchestration = null)
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
        _memoryService = memoryService;
        _memoryMutations = memoryMutations;
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
        _todoMutations = todoMutations;
        _todoExecutionService = todoExecutionService;
        _promptTemplateService = promptTemplateService;
        _brainSlotRegistry = brainSlotRegistry;
        _brainSlotInvocation = brainSlotInvocation;
        _quadBrainOrchestration = quadBrainOrchestration;
        _transactionCoordinator = transactionCoordinator;
        _transactionOptions = transactionOptions;
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

    private bool ShouldDeferContextMutation(out string error)
    {
        error = string.Empty;
        if (_transactionCoordinator is null)
            return false;

        var status = _transactionCoordinator.GetStatus();
        if (status.Degraded)
        {
            error = string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message;
            return true;
        }

        if (!RequiresMutationTransactions(status))
            return false;

        error = DeferredContextMutationMessage;
        return true;
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);

    private static string SerializeJson(object value) => JsonSerializer.Serialize(value, s_camelCaseOptions);

    private static bool TryParseMemoryScope(string? value, out MemoryScope? scope, out string? error)
    {
        scope = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        if (int.TryParse(trimmed, out _)
            || !Enum.TryParse(trimmed, ignoreCase: true, out MemoryScope parsed)
            || !Enum.IsDefined(parsed))
        {
            error = "scope must be Global or Workspace.";
            return false;
        }

        scope = parsed;
        return true;
    }

    private static bool TryParseMemoryListScope(string? value, out MemoryScope? scope, out string? error)
    {
        scope = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value.Trim(), "Effective", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!TryParseMemoryScope(value, out scope, out error))
        {
            error = "scope must be Effective, Global, or Workspace.";
            return false;
        }

        return true;
    }

    private static bool TryParseMemoryScope(string? value, MemoryScope defaultScope, out MemoryScope scope, out string? error)
    {
        scope = defaultScope;
        if (!TryParseMemoryScope(value, out var parsed, out error))
        {
            return false;
        }

        scope = parsed ?? defaultScope;
        return true;
    }

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
        if (ShouldDeferContextMutation(out var transactionError))
            return JsonSerializer.Serialize(new { error = transactionError, code = "turn_transaction_gate" });

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

    // ── Memory tools ────────────────────────────────────────────────────

    /// <summary>TR-MCP-MEMORY-006: List effective memory items visible to the workspace.</summary>
    [McpServerTool(Name = "memory_list"), Description("List effective memory items. Optional filters: scope, category, keyword.")]
    public async Task<string> MemoryList(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional scope filter: Effective, Global, or Workspace")] string? scope = null,
        [Description("Optional category filter")] string? category = null,
        [Description("Optional keyword filter")] string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseMemoryListScope(scope, out var parsedScope, out var error))
            {
                return SerializeJson(new { error });
            }

            var result = await _memoryService.ListAsync(new MemoryListRequest
            {
                Scope = parsedScope,
                Category = category,
                Keyword = keyword,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>TR-MCP-MEMORY-006: Get a single visible memory item by id.</summary>
    [McpServerTool(Name = "memory_get"), Description("Get a single visible memory item by id.")]
    public async Task<string> MemoryGet(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Memory id")] string id,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var item = await _memoryService.GetAsync(id, cancellationToken).ConfigureAwait(false);
            return item is null
                ? SerializeJson(new { error = $"Memory '{id}' not found" })
                : SerializeJson(item);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>TR-MCP-MEMORY-006: Add a memory item in Global or Workspace scope.</summary>
    [McpServerTool(Name = "memory_add"), Description("Add a memory item. Defaults to Workspace scope.")]
    public async Task<string> MemoryAdd(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Memory category")] string category,
        [Description("Memory text")] string text,
        [Description("Memory scope: Global or Workspace (default Workspace)")] string? scope = null,
        [Description("Optional explicit memory id")] string? id = null,
        [Description("Optional updater identity")] string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseMemoryScope(scope, MemoryScope.Workspace, out var parsedScope, out var error))
            {
                return SerializeJson(new MemoryMutationResult(false, error, FailureKind: MemoryMutationFailureKind.Validation));
            }

            var request = new MemoryAddRequest
            {
                Id = id,
                Category = category,
                Scope = parsedScope,
                Text = text,
                UpdatedBy = updatedBy,
            };
            var result = _memoryMutations is null
                ? await _memoryService.AddAsync(request, cancellationToken).ConfigureAwait(false)
                : await _memoryMutations.AddAsync(request, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new MemoryMutationResult(false, ex.Message));
        }
    }

    /// <summary>TR-MCP-MEMORY-006: Update a visible memory item by id.</summary>
    [McpServerTool(Name = "memory_update"), Description("Update a memory item by id. Only provided fields are changed.")]
    public async Task<string> MemoryUpdate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Memory id")] string id,
        [Description("Optional category replacement")] string? category = null,
        [Description("Optional text replacement")] string? text = null,
        [Description("Optional scope replacement: Global or Workspace")] string? scope = null,
        [Description("Optional updater identity")] string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseMemoryScope(scope, out var parsedScope, out var error))
            {
                return SerializeJson(new MemoryMutationResult(false, error, FailureKind: MemoryMutationFailureKind.Validation));
            }

            var request = new MemoryUpdateRequest
            {
                Category = category,
                Scope = parsedScope,
                Text = text,
                UpdatedBy = updatedBy,
            };
            var result = _memoryMutations is null
                ? await _memoryService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false)
                : await _memoryMutations.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new MemoryMutationResult(false, ex.Message));
        }
    }

    /// <summary>TR-MCP-MEMORY-006: Remove a visible memory item by id.</summary>
    [McpServerTool(Name = "memory_remove"), Description("Remove a visible memory item by id.")]
    public async Task<string> MemoryRemove(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Memory id")] string id,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = _memoryMutations is null
                ? await _memoryService.RemoveAsync(id, cancellationToken).ConfigureAwait(false)
                : await _memoryMutations.RemoveAsync(id, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new MemoryMutationResult(false, ex.Message));
        }
    }
}
