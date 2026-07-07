using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Services.FederationAdapters;

/// <summary>
/// Shared behavior for federation state adapters, including deterministic
/// version hashing and default idempotency semantics.
/// </summary>
public abstract class FederationStateAdapterBase : IFederationStateAdapter
{
    /// <summary>JSON serializer options used for adapter payloads.</summary>
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Initializes a new instance of the <see cref="FederationStateAdapterBase"/> class.</summary>
    /// <param name="domain">Mutable state domain handled by the adapter.</param>
    protected FederationStateAdapterBase(string domain)
    {
        Domain = domain;
    }

    /// <inheritdoc />
    public string Domain { get; }

    /// <inheritdoc />
    public virtual bool IsLocalOnly => false;

    /// <inheritdoc />
    public abstract ValueTask<FederationStateSnapshot> SnapshotAsync(string resourceId, CancellationToken cancellationToken);

    /// <inheritdoc />
    public virtual ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
        => new(new FederationApplyResult
        {
            Applied = false,
            Conflict = true,
            Message = $"Federation apply for domain '{Domain}' requires signed operation envelopes.",
        });

    /// <inheritdoc />
    public abstract ValueTask<string?> GetVersionAsync(string resourceId, CancellationToken cancellationToken);

    /// <inheritdoc />
    public virtual string GetIdempotencyKey(FederationStateOperation operation)
        => string.IsNullOrWhiteSpace(operation.SourceOperationId)
            ? operation.OperationId
            : operation.SourceOperationId!;

    /// <inheritdoc />
    public virtual bool IsEcho(FederationStateOperation operation)
        => !string.IsNullOrWhiteSpace(operation.SourceOperationId)
            && string.Equals(operation.SourceOperationId, operation.OperationId, StringComparison.Ordinal);

    /// <summary>Computes a stable SHA-256 version token for serialized payload content.</summary>
    /// <param name="payloadJson">Serialized payload JSON.</param>
    /// <returns>A lowercase hexadecimal SHA-256 token.</returns>
    protected static string VersionFromPayload(string payloadJson)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

/// <summary>
/// Shared database-backed snapshot behavior for domains persisted in
/// <see cref="McpDbContext"/>.
/// </summary>
public abstract class DatabaseFederationStateAdapterBase : FederationStateAdapterBase
{
    /// <summary>Initializes a new instance of the <see cref="DatabaseFederationStateAdapterBase"/> class.</summary>
    /// <param name="domain">Mutable state domain handled by the adapter.</param>
    /// <param name="scopeFactory">Scope factory used to resolve <see cref="McpDbContext"/>.</param>
    protected DatabaseFederationStateAdapterBase(string domain, IServiceScopeFactory scopeFactory)
        : base(domain)
    {
        ScopeFactory = scopeFactory;
    }

    /// <summary>Scope factory used to resolve scoped services and database contexts.</summary>
    protected IServiceScopeFactory ScopeFactory { get; }

    /// <inheritdoc />
    public override async ValueTask<FederationStateSnapshot> SnapshotAsync(string resourceId, CancellationToken cancellationToken)
    {
        var payload = await ReadPayloadAsync(resourceId, cancellationToken).ConfigureAwait(false);
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        return new FederationStateSnapshot
        {
            Domain = Domain,
            ResourceId = resourceId,
            PayloadJson = payloadJson,
            Version = payload is null ? null : await GetVersionAsync(resourceId, cancellationToken).ConfigureAwait(false),
        };
    }

    /// <inheritdoc />
    public override async ValueTask<string?> GetVersionAsync(string resourceId, CancellationToken cancellationToken)
    {
        var explicitVersion = await GetExplicitVersionAsync(resourceId, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(explicitVersion))
            return explicitVersion;

        var payload = await ReadPayloadAsync(resourceId, cancellationToken).ConfigureAwait(false);
        return payload is null ? null : VersionFromPayload(JsonSerializer.Serialize(payload, JsonOptions));
    }

    /// <summary>Reads a domain payload from the database.</summary>
    /// <param name="db">Database context.</param>
    /// <param name="resourceId">Domain resource identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The snapshot payload, or <c>null</c> when the resource does not exist.</returns>
    protected abstract Task<object?> ReadPayloadAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken);

    /// <summary>Reads an authoritative version token when the domain has one.</summary>
    /// <param name="db">Database context.</param>
    /// <param name="resourceId">Domain resource identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The explicit version token, or <c>null</c> to hash the snapshot payload.</returns>
    protected virtual Task<string?> ReadExplicitVersionAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);

    private async Task<object?> ReadPayloadAsync(string resourceId, CancellationToken cancellationToken)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        return await ReadPayloadAsync(db, resourceId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> GetExplicitVersionAsync(string resourceId, CancellationToken cancellationToken)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        return await ReadExplicitVersionAsync(db, resourceId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Federation adapter for workspace registration metadata.</summary>
public sealed class WorkspaceFederationStateAdapter : FederationStateAdapterBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceFederationStateAdapter"/> class.</summary>
    /// <param name="scopeFactory">Scope factory used to resolve <see cref="IWorkspaceService"/>.</param>
    public WorkspaceFederationStateAdapter(IServiceScopeFactory scopeFactory)
        : base("workspace")
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public override async ValueTask<FederationStateSnapshot> SnapshotAsync(string resourceId, CancellationToken cancellationToken)
    {
        var workspace = await GetWorkspaceAsync(resourceId, cancellationToken).ConfigureAwait(false);
        var payloadJson = JsonSerializer.Serialize(workspace, JsonOptions);
        return new FederationStateSnapshot
        {
            Domain = Domain,
            ResourceId = resourceId,
            PayloadJson = payloadJson,
            Version = workspace is null ? null : workspace.DateTimeModified.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
        };
    }

    /// <inheritdoc />
    public override async ValueTask<string?> GetVersionAsync(string resourceId, CancellationToken cancellationToken)
    {
        var workspace = await GetWorkspaceAsync(resourceId, cancellationToken).ConfigureAwait(false);
        return workspace is null
            ? null
            : workspace.DateTimeModified.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override async ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var workspaceService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var resourceId = ResolveResourceId(operation);

        if (IsDelete(operation))
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                return Conflict("Workspace delete operation does not identify a workspace path.");

            var deleted = await workspaceService.DeleteAsync(resourceId, cancellationToken).ConfigureAwait(false);
            return deleted.Success
                ? new FederationApplyResult { Applied = true, Version = null }
                : Conflict(deleted.Error ?? "Workspace delete failed.");
        }

        WorkspaceDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WorkspaceDto>(operation.PayloadJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            return Conflict($"Workspace federation payload is invalid JSON: {ex.Message}");
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.WorkspacePath))
            return Conflict("Workspace federation payload does not include a workspace path.");

        var existing = await workspaceService.GetAsync(payload.WorkspacePath, cancellationToken).ConfigureAwait(false);
        var result = existing is null
            ? await workspaceService.CreateAsync(new WorkspaceCreateRequest
            {
                WorkspacePath = payload.WorkspacePath,
                Name = payload.Name,
                TodoPath = payload.TodoPath,
                DataDirectory = payload.DataDirectory,
                TunnelProvider = payload.TunnelProvider,
                RunAs = payload.RunAs,
                IsPrimary = payload.IsPrimary,
                IsEnabled = payload.IsEnabled,
                PromptTemplate = payload.PromptTemplate,
                StatusPrompt = payload.StatusPrompt,
                ImplementPrompt = payload.ImplementPrompt,
                PlanPrompt = payload.PlanPrompt,
                BannedLicenses = payload.BannedLicenses,
                BannedCountriesOfOrigin = payload.BannedCountriesOfOrigin,
                BannedOrganizations = payload.BannedOrganizations,
                BannedIndividuals = payload.BannedIndividuals,
            }, cancellationToken).ConfigureAwait(false)
            : await workspaceService.UpdateAsync(payload.WorkspacePath, new WorkspaceUpdateRequest
            {
                Name = payload.Name,
                TodoPath = payload.TodoPath,
                DataDirectory = payload.DataDirectory ?? string.Empty,
                TunnelProvider = payload.TunnelProvider ?? string.Empty,
                RunAs = payload.RunAs ?? string.Empty,
                IsPrimary = payload.IsPrimary,
                IsEnabled = payload.IsEnabled,
                PromptTemplate = payload.PromptTemplate ?? string.Empty,
                StatusPrompt = payload.StatusPrompt ?? string.Empty,
                ImplementPrompt = payload.ImplementPrompt ?? string.Empty,
                PlanPrompt = payload.PlanPrompt ?? string.Empty,
                BannedLicenses = payload.BannedLicenses,
                BannedCountriesOfOrigin = payload.BannedCountriesOfOrigin,
                BannedOrganizations = payload.BannedOrganizations,
                BannedIndividuals = payload.BannedIndividuals,
            }, cancellationToken).ConfigureAwait(false);

        return result.Success
            ? new FederationApplyResult
            {
                Applied = true,
                Version = await GetVersionAsync(payload.WorkspacePath, cancellationToken).ConfigureAwait(false),
            }
            : Conflict(result.Error ?? "Workspace apply failed.");
    }

    private async Task<WorkspaceDto?> GetWorkspaceAsync(string resourceId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var workspaceService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        return await workspaceService.GetAsync(resourceId, cancellationToken).ConfigureAwait(false);
    }

    private static string? ResolveResourceId(FederationStateOperation operation)
        => !string.IsNullOrWhiteSpace(operation.ResourceId)
            ? operation.ResourceId.Trim()
            : TryReadString(operation.PayloadJson, "workspacePath");

    private static string? TryReadString(string payloadJson, string propertyName)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            return document.RootElement.TryGetProperty(propertyName, out var property) ? property.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsDelete(FederationStateOperation operation)
        => string.Equals(operation.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase);

    private static FederationApplyResult Conflict(string message)
        => new()
        {
            Applied = false,
            Conflict = true,
            Message = message,
        };
}

/// <summary>Federation adapter for authoritative TODO state.</summary>
public sealed class TodoFederationStateAdapter : DatabaseFederationStateAdapterBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Initializes a new instance of the <see cref="TodoFederationStateAdapter"/> class.</summary>
    /// <param name="scopeFactory">Scope factory used to resolve <see cref="McpDbContext"/>.</param>
    public TodoFederationStateAdapter(IServiceScopeFactory scopeFactory)
        : base("todo", scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public override async ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            var method = (operation.HttpMethod ?? string.Empty).Trim().ToUpperInvariant();
            await using var scope = _scopeFactory.CreateAsyncScope();
            var todoService = scope.ServiceProvider.GetRequiredService<ITodoService>();
            return method switch
            {
                "POST" => await ApplyCreateAsync(todoService, operation, cancellationToken).ConfigureAwait(false),
                "PUT" or "PATCH" => await ApplyUpdateAsync(todoService, operation, cancellationToken).ConfigureAwait(false),
                "DELETE" => await ApplyDeleteAsync(todoService, operation, cancellationToken).ConfigureAwait(false),
                _ => new FederationApplyResult
                {
                    Applied = false,
                    Conflict = true,
                    Message = $"TODO federation apply does not support HTTP method '{operation.HttpMethod ?? "<none>"}'.",
                },
            };
        }
        catch (JsonException ex)
        {
            return new FederationApplyResult
            {
                Applied = false,
                Conflict = true,
                Message = $"TODO federation payload is invalid JSON: {ex.Message}",
            };
        }
    }

    /// <inheritdoc />
    protected override async Task<object?> ReadPayloadAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken)
    {
        var row = await db.TodoItems
            .AsNoTracking()
            .Where(t => t.Id == resourceId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
            return null;

        // TR-MCP-TODO-005: lists live in 4NF child tables; the wire snapshot keeps its
        // serialized-JSON field shape so federated peers are unaffected by the decomposition.
        var listRows = await db.TodoItemListItems
            .AsNoTracking()
            .Where(r => r.WorkspaceId == row.WorkspaceId && r.TodoId == row.Id)
            .OrderBy(r => r.Ordinal)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var taskRows = await db.TodoItemTasks
            .AsNoTracking()
            .Where(r => r.WorkspaceId == row.WorkspaceId && r.TodoId == row.Id)
            .OrderBy(r => r.Ordinal)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        string? SerializeListType(string listType)
        {
            var values = listRows.Where(r => r.ListType == listType).Select(r => r.Value).ToList();
            return values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);
        }

        var item = new
        {
            row.Id,
            row.Title,
            row.Section,
            row.Priority,
            row.Done,
            row.Estimate,
            row.Note,
            DescriptionJson = SerializeListType("Description"),
            TechnicalDetailsJson = SerializeListType("TechnicalDetail"),
            ImplementationTasksJson = taskRows.Count == 0
                ? null
                : JsonSerializer.Serialize(taskRows.Select(t => new { task = t.Task, done = t.Done }).ToList(), JsonOptions),
            row.CompletedDate,
            row.DoneSummary,
            row.Remaining,
            row.PriorityNote,
            row.Reference,
            DependsOnJson = SerializeListType("DependsOn"),
            FunctionalRequirementsJson = SerializeListType("FunctionalRequirement"),
            TechnicalRequirementsJson = SerializeListType("TechnicalRequirement"),
            row.ItemKind,
            row.SectionOrder,
            row.ItemOrder,
            row.PhaseLabel,
        };

        var audit = await db.TodoAuditHistory
            .AsNoTracking()
            .Where(a => a.TodoId == resourceId)
            .OrderByDescending(a => a.Version)
            .ThenByDescending(a => a.AuditId)
            .Take(20)
            .Select(a => new
            {
                a.Version,
                a.Action,
                a.RecordedAtUtc,
                a.Source,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new { item, audit };
    }

    /// <inheritdoc />
    protected override async Task<string?> ReadExplicitVersionAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken)
        => await db.TodoAuditHistory
            .AsNoTracking()
            .Where(a => a.TodoId == resourceId)
            .OrderByDescending(a => a.Version)
            .Select(a => (int?)a.Version)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) is { } version
            ? version.ToString(CultureInfo.InvariantCulture)
            : null;

    private async Task<FederationApplyResult> ApplyCreateAsync(
        ITodoService todoService,
        FederationStateOperation operation,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<TodoCreateRequest>(operation.PayloadJson, JsonOptions);
        if (request is null)
            return Conflict("TODO create payload is empty.");

        var result = await todoService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return await FromTodoMutationAsync(result, request.Id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<FederationApplyResult> ApplyUpdateAsync(
        ITodoService todoService,
        FederationStateOperation operation,
        CancellationToken cancellationToken)
    {
        var todoId = ResolveTodoId(operation);
        if (string.IsNullOrWhiteSpace(todoId))
            return Conflict("TODO update operation does not identify a TODO id.");

        var request = JsonSerializer.Deserialize<TodoUpdateRequest>(operation.PayloadJson, JsonOptions);
        if (request is null)
            return Conflict("TODO update payload is empty.");

        var result = await todoService.UpdateAsync(todoId, request, cancellationToken).ConfigureAwait(false);
        return await FromTodoMutationAsync(result, todoId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<FederationApplyResult> ApplyDeleteAsync(
        ITodoService todoService,
        FederationStateOperation operation,
        CancellationToken cancellationToken)
    {
        var todoId = ResolveTodoId(operation);
        if (string.IsNullOrWhiteSpace(todoId))
            return Conflict("TODO delete operation does not identify a TODO id.");

        var result = await todoService.DeleteAsync(todoId, cancellationToken).ConfigureAwait(false);
        return await FromTodoMutationAsync(result, todoId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<FederationApplyResult> FromTodoMutationAsync(
        TodoMutationResult result,
        string resourceId,
        CancellationToken cancellationToken)
    {
        if (!result.Success)
        {
            return new FederationApplyResult
            {
                Applied = false,
                Conflict = true,
                Message = result.Error,
                Version = await GetVersionAsync(resourceId, cancellationToken).ConfigureAwait(false),
            };
        }

        var versionResourceId = result.Item?.Id ?? resourceId;
        return new FederationApplyResult
        {
            Applied = true,
            Version = await GetVersionAsync(versionResourceId, cancellationToken).ConfigureAwait(false),
        };
    }

    private static FederationApplyResult Conflict(string message)
        => new()
        {
            Applied = false,
            Conflict = true,
            Message = message,
        };

    private static string? ResolveTodoId(FederationStateOperation operation)
    {
        if (!string.IsNullOrWhiteSpace(operation.ResourceId) &&
            !operation.ResourceId.StartsWith("/mcpserver/todo", StringComparison.OrdinalIgnoreCase))
        {
            return operation.ResourceId.Trim();
        }

        var path = operation.Path;
        if (string.IsNullOrWhiteSpace(path))
            path = operation.ResourceId;
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var pathOnly = path.Split('?', 2)[0].TrimEnd('/');
        var marker = "/mcpserver/todo/";
        var markerIndex = pathOnly.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return markerIndex < 0
            ? null
            : Uri.UnescapeDataString(pathOnly[(markerIndex + marker.Length)..]);
    }
}

/// <summary>TR-MCP-FED-MEMORY-001: Federation adapter for authoritative memory state.</summary>
public sealed class MemoryFederationStateAdapter : DatabaseFederationStateAdapterBase
{
    private static readonly Regex s_memoryIdRegex = new(
        "^MEMORY-[A-Z0-9]+(?:-[A-Z0-9]+)*-[0-9]{3,}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex s_categoryUnsafeCharactersRegex = new(
        "[^A-Z0-9]+",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex s_repeatedHyphenRegex = new(
        "-+",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    /// <summary>Initializes a new instance of the <see cref="MemoryFederationStateAdapter"/> class.</summary>
    /// <param name="scopeFactory">Scope factory used to resolve <see cref="McpDbContext"/>.</param>
    public MemoryFederationStateAdapter(IServiceScopeFactory scopeFactory)
        : base("memory", scopeFactory)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            var method = (operation.HttpMethod ?? string.Empty).Trim().ToUpperInvariant();
            await using var scope = ScopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            return method switch
            {
                "POST" => await ApplyCreateAsync(db, operation, cancellationToken).ConfigureAwait(false),
                "PUT" or "PATCH" => await ApplyUpdateAsync(db, operation, cancellationToken).ConfigureAwait(false),
                "DELETE" => await ApplyDeleteAsync(db, operation, cancellationToken).ConfigureAwait(false),
                _ => Conflict($"Memory federation apply does not support HTTP method '{operation.HttpMethod ?? "<none>"}'."),
            };
        }
        catch (JsonException ex)
        {
            return Conflict($"Memory federation payload is invalid JSON: {ex.Message}");
        }
    }

    /// <inheritdoc />
    protected override async Task<object?> ReadPayloadAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken)
    {
        var id = NormalizeId(resourceId);
        if (!IsValidMemoryId(id))
            return null;

        var row = await db.Memories
            .IgnoreQueryFilters()
            .Where(memory => memory.Id == id)
            .Select(memory => new
            {
                memory.Id,
                memory.Category,
                memory.Scope,
                memory.WorkspaceId,
                memory.Text,
                memory.Version,
                memory.CreatedAtUtc,
                memory.UpdatedAtUtc,
                memory.UpdatedBy,
                IsDeleted = EF.Property<bool>(memory, "IsDeleted"),
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null || row.IsDeleted)
            return null;

        return new MemoryItem
        {
            Id = row.Id,
            Category = row.Category,
            Scope = ToMemoryScope(row.Scope),
            WorkspacePath = row.WorkspaceId,
            Text = row.Text,
            Version = row.Version,
            CreatedAtUtc = row.CreatedAtUtc,
            UpdatedAtUtc = row.UpdatedAtUtc,
            UpdatedBy = row.UpdatedBy,
        };
    }

    /// <inheritdoc />
    protected override async Task<string?> ReadExplicitVersionAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken)
    {
        var id = NormalizeId(resourceId);
        if (!IsValidMemoryId(id))
            return null;

        var row = await db.Memories
            .IgnoreQueryFilters()
            .Where(memory => memory.Id == id)
            .Select(memory => new
            {
                memory.Version,
                IsDeleted = EF.Property<bool>(memory, "IsDeleted"),
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return row is null || row.IsDeleted
            ? null
            : row.Version.ToString(CultureInfo.InvariantCulture);
    }

    private static async Task<FederationApplyResult> ApplyCreateAsync(
        McpDbContext db,
        FederationStateOperation operation,
        CancellationToken cancellationToken)
    {
        var payload = DeserializePayload(operation.PayloadJson);
        var id = NormalizeId(payload.Id);
        if (!IsValidMemoryId(id))
            return Conflict("Memory create payload must include an explicit id matching MEMORY-{CATEGORY}-{NNN}.");

        var category = NormalizeCategory(payload.Category);
        if (category is null)
            return Conflict("Memory create payload must include a non-empty category.");

        if (string.IsNullOrWhiteSpace(payload.Text))
            return Conflict("Memory create payload must include non-empty text.");

        var scope = payload.Scope ?? MemoryScope.Workspace;
        var workspaceId = ResolveWorkspaceId(scope, operation);
        if (scope == MemoryScope.Workspace && workspaceId is null)
            return Conflict("Workspace memory create requires a global workspace id.");

        var existing = await db.Memories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(memory => memory.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            var version = existing.Version.ToString(CultureInfo.InvariantCulture);
            if (IsDeleted(db, existing))
                return Conflict($"Memory '{id}' already exists as a deleted row.", version);

            return IsEquivalentCreate(existing, category, scope, workspaceId, payload.Text)
                ? new FederationApplyResult { Applied = false, AlreadyApplied = true, Version = version }
                : Conflict($"Memory '{id}' already exists with different state.", version);
        }

        var now = DateTimeOffset.UtcNow;
        var createdAt = payload.CreatedAtUtc ?? now;
        var updatedAt = payload.UpdatedAtUtc ?? createdAt;
        db.Memories.Add(new MemoryEntity
        {
            Id = id,
            Category = category,
            Scope = ToEntityScope(scope),
            WorkspaceId = workspaceId,
            Text = payload.Text,
            Version = payload.Version is > 0 ? payload.Version.Value : 1,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = updatedAt,
            UpdatedBy = NormalizeOptional(payload.UpdatedBy),
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new FederationApplyResult
        {
            Applied = true,
            Version = await ReadCurrentVersionAsync(db, id, cancellationToken).ConfigureAwait(false),
        };
    }

    private static async Task<FederationApplyResult> ApplyUpdateAsync(
        McpDbContext db,
        FederationStateOperation operation,
        CancellationToken cancellationToken)
    {
        var id = ResolveMemoryId(operation);
        if (!IsValidMemoryId(id))
            return Conflict("Memory update operation does not identify a valid memory id.");

        var entity = await db.Memories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(memory => memory.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null || IsDeleted(db, entity))
            return Conflict($"Memory '{id}' is missing or deleted.");

        if (!OwnsWorkspace(entity, operation.GlobalWorkspaceId))
            return Conflict(
                $"Workspace memory '{id}' cannot be applied to global workspace '{operation.GlobalWorkspaceId ?? "<none>"}'.",
                entity.Version.ToString(CultureInfo.InvariantCulture));

        var payload = DeserializePayload(operation.PayloadJson);
        if (payload.Category is not null)
        {
            var category = NormalizeCategory(payload.Category);
            if (category is null)
                return Conflict("Memory update category cannot be empty.");

            entity.Category = category;
        }

        if (payload.Scope is { } scope)
        {
            var workspaceId = ResolveWorkspaceId(scope, operation);
            if (scope == MemoryScope.Workspace && workspaceId is null)
                return Conflict("Workspace memory update requires a global workspace id.");

            entity.Scope = ToEntityScope(scope);
            entity.WorkspaceId = workspaceId;
        }

        if (payload.Text is not null)
        {
            if (string.IsNullOrWhiteSpace(payload.Text))
                return Conflict("Memory update text cannot be empty.");

            entity.Text = payload.Text;
        }

        entity.Version++;
        entity.UpdatedAtUtc = payload.UpdatedAtUtc ?? DateTimeOffset.UtcNow;
        if (payload.UpdatedBy is not null)
            entity.UpdatedBy = NormalizeOptional(payload.UpdatedBy);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new FederationApplyResult
        {
            Applied = true,
            Version = entity.Version.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static async Task<FederationApplyResult> ApplyDeleteAsync(
        McpDbContext db,
        FederationStateOperation operation,
        CancellationToken cancellationToken)
    {
        var id = ResolveMemoryId(operation);
        if (!IsValidMemoryId(id))
            return Conflict("Memory delete operation does not identify a valid memory id.");

        var entity = await db.Memories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(memory => memory.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null || IsDeleted(db, entity))
            return new FederationApplyResult { Applied = true, Version = null };

        if (!OwnsWorkspace(entity, operation.GlobalWorkspaceId))
            return Conflict(
                $"Workspace memory '{id}' cannot be deleted from global workspace '{operation.GlobalWorkspaceId ?? "<none>"}'.",
                entity.Version.ToString(CultureInfo.InvariantCulture));

        db.Memories.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new FederationApplyResult { Applied = true, Version = null };
    }

    private static async Task<string?> ReadCurrentVersionAsync(McpDbContext db, string id, CancellationToken cancellationToken)
    {
        var version = await db.Memories
            .IgnoreQueryFilters()
            .Where(memory => memory.Id == id && !EF.Property<bool>(memory, "IsDeleted"))
            .Select(memory => (int?)memory.Version)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return version?.ToString(CultureInfo.InvariantCulture);
    }

    private static MemoryApplyPayload DeserializePayload(string payloadJson)
        => JsonSerializer.Deserialize<MemoryApplyPayload>(payloadJson, JsonOptions) ?? new MemoryApplyPayload();

    private static bool IsEquivalentCreate(
        MemoryEntity existing,
        string category,
        MemoryScope scope,
        string? workspaceId,
        string text)
        => string.Equals(existing.Category, category, StringComparison.Ordinal) &&
           string.Equals(existing.Scope, ToEntityScope(scope), StringComparison.Ordinal) &&
           string.Equals(existing.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase) &&
           string.Equals(existing.Text, text, StringComparison.Ordinal);

    private static bool OwnsWorkspace(MemoryEntity entity, string? globalWorkspaceId)
        => !string.Equals(entity.Scope, MemoryEntity.WorkspaceScope, StringComparison.Ordinal) ||
           (!string.IsNullOrWhiteSpace(globalWorkspaceId) &&
            string.Equals(entity.WorkspaceId, globalWorkspaceId.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? ResolveWorkspaceId(MemoryScope scope, FederationStateOperation operation)
        => scope == MemoryScope.Global ? null : NormalizeOptional(operation.GlobalWorkspaceId);

    private static string ResolveMemoryId(FederationStateOperation operation)
    {
        if (!string.IsNullOrWhiteSpace(operation.ResourceId) &&
            !operation.ResourceId.StartsWith("/mcpserver/memory", StringComparison.OrdinalIgnoreCase))
        {
            return NormalizeId(operation.ResourceId);
        }

        var path = operation.Path;
        if (string.IsNullOrWhiteSpace(path))
            path = operation.ResourceId;
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var pathOnly = path.Split('?', 2)[0].TrimEnd('/');
        var marker = "/mcpserver/memory/";
        var markerIndex = pathOnly.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return markerIndex < 0
            ? string.Empty
            : NormalizeId(Uri.UnescapeDataString(pathOnly[(markerIndex + marker.Length)..]));
    }

    private static bool IsDeleted(McpDbContext db, MemoryEntity entity)
        => db.Entry(entity).Property<bool>("IsDeleted").CurrentValue;

    private static string NormalizeId(string? id)
        => (id ?? string.Empty).Trim().ToUpperInvariant();

    private static bool IsValidMemoryId(string? id)
        => !string.IsNullOrWhiteSpace(id) && s_memoryIdRegex.IsMatch(NormalizeId(id));

    private static string? NormalizeCategory(string? category)
    {
        var trimmed = NormalizeOptional(category);
        if (trimmed is null)
            return null;

        var normalized = s_categoryUnsafeCharactersRegex.Replace(trimmed.ToUpperInvariant(), "-").Trim('-');
        normalized = s_repeatedHyphenRegex.Replace(normalized, "-");
        return normalized.Length == 0 ? null : normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string ToEntityScope(MemoryScope scope)
        => scope == MemoryScope.Global ? MemoryEntity.GlobalScope : MemoryEntity.WorkspaceScope;

    private static MemoryScope ToMemoryScope(string scope)
        => string.Equals(scope, MemoryEntity.GlobalScope, StringComparison.Ordinal) ? MemoryScope.Global : MemoryScope.Workspace;

    private static FederationApplyResult Conflict(string message, string? version = null)
        => new()
        {
            Applied = false,
            Conflict = true,
            Version = version,
            Message = message,
        };

    private sealed record MemoryApplyPayload
    {
        public string? Id { get; init; }

        public string? Category { get; init; }

        public MemoryScope? Scope { get; init; }

        public string? Text { get; init; }

        public int? Version { get; init; }

        public DateTimeOffset? CreatedAtUtc { get; init; }

        public DateTimeOffset? UpdatedAtUtc { get; init; }

        public string? UpdatedBy { get; init; }
    }
}

/// <summary>Federation adapter for session log state.</summary>
public sealed class SessionLogFederationStateAdapter : DatabaseFederationStateAdapterBase
{
    /// <summary>Initializes a new instance of the <see cref="SessionLogFederationStateAdapter"/> class.</summary>
    /// <param name="scopeFactory">Scope factory used to resolve <see cref="McpDbContext"/>.</param>
    public SessionLogFederationStateAdapter(IServiceScopeFactory scopeFactory)
        : base("session_log", scopeFactory)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var key = SessionLogKey.Parse(operation.ResourceId ?? string.Empty);

        await using var scope = ScopeFactory.CreateAsyncScope();
        if (string.Equals(operation.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(key.SessionId))
                return Conflict("Session log delete operation does not identify a session id.");

            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            var query = SessionLogGraphQuery(db).Where(s => s.SessionId == key.SessionId);
            if (!string.IsNullOrWhiteSpace(key.SourceType))
                query = query.Where(s => s.SourceType == key.SourceType);
            var session = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (session is null)
                return new FederationApplyResult { Applied = true, Version = null };

            if (IsSoftDeleted(db, session))
                return new FederationApplyResult { Applied = true, Version = null };

            SoftDeleteSessionGraph(db, session, "federation_delete_replay");
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new FederationApplyResult { Applied = true, Version = null };
        }

        UnifiedSessionLogDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<UnifiedSessionLogDto>(operation.PayloadJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            return Conflict($"Session log federation payload is invalid JSON: {ex.Message}");
        }

        if (payload is null)
            return Conflict("Session log federation payload is empty.");
        payload.SourceType ??= key.SourceType;
        payload.SessionId ??= key.SessionId;
        if (string.IsNullOrWhiteSpace(payload.SourceType) || string.IsNullOrWhiteSpace(payload.SessionId))
            return Conflict("Session log federation payload must include sourceType and sessionId.");

        var sessionLogService = scope.ServiceProvider.GetRequiredService<ISessionLogService>();
        await sessionLogService.SubmitAsync(payload, contentHash: operation.OperationId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var version = await GetVersionAsync($"{payload.SourceType}/{payload.SessionId}", cancellationToken).ConfigureAwait(false);
        return new FederationApplyResult { Applied = true, Version = version };
    }

    /// <inheritdoc />
    protected override async Task<object?> ReadPayloadAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken)
    {
        var key = SessionLogKey.Parse(resourceId);
        var query = db.SessionLogs.AsNoTracking().Where(s => s.SessionId == key.SessionId);
        if (!string.IsNullOrWhiteSpace(key.SourceType))
            query = query.Where(s => s.SourceType == key.SourceType);

        var session = await query
            .Select(s => new
            {
                s.Id,
                s.SourceType,
                s.SessionId,
                s.AgentDefinitionId,
                s.Title,
                s.Model,
                s.Started,
                s.LastUpdated,
                s.Status,
                s.TurnCount,
                s.TotalTokens,
                s.ContentHash,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            return null;

        var turns = await db.SessionLogTurns
            .AsNoTracking()
            .Where(t => t.SessionLogId == session.Id)
            .OrderBy(t => t.Timestamp)
            .ThenBy(t => t.Id)
            .Select(t => new
            {
                t.RequestId,
                t.Timestamp,
                t.Model,
                t.ModelProvider,
                t.QueryTitle,
                t.Status,
                t.TokenCount,
                t.FailureNote,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new
        {
            session.SourceType,
            session.SessionId,
            session.AgentDefinitionId,
            session.Title,
            session.Model,
            session.Started,
            session.LastUpdated,
            session.Status,
            session.TurnCount,
            session.TotalTokens,
            session.ContentHash,
            turns,
        };
    }

    /// <inheritdoc />
    protected override async Task<string?> ReadExplicitVersionAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken)
    {
        var key = SessionLogKey.Parse(resourceId);
        var query = db.SessionLogs.AsNoTracking().Where(s => s.SessionId == key.SessionId);
        if (!string.IsNullOrWhiteSpace(key.SourceType))
            query = query.Where(s => s.SourceType == key.SourceType);

        return await query
            .Select(s => !string.IsNullOrWhiteSpace(s.ContentHash)
                ? s.ContentHash
                : s.LastUpdated.HasValue
                    ? s.LastUpdated.Value.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
                    : s.TurnCount.ToString(CultureInfo.InvariantCulture))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static FederationApplyResult Conflict(string message)
        => new()
        {
            Applied = false,
            Conflict = true,
            Message = message,
        };

    private static IQueryable<SessionLogEntity> SessionLogGraphQuery(McpDbContext db)
        => db.SessionLogs
            .IgnoreQueryFilters()
            .Include(session => session.Turns.OrderBy(turn => turn.Id))
                .ThenInclude(turn => turn.Actions)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.Tags)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.ContextItems)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.ProcessingDialog)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.Commits)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.StringListItems)
            .AsSplitQuery();

    private static void SoftDeleteSessionGraph(McpDbContext db, SessionLogEntity session, string reason)
    {
        var deletedAtUtc = DateTimeOffset.UtcNow;
        foreach (var turn in session.Turns)
        {
            foreach (var action in turn.Actions)
                MarkSoftDeleted(db, action, deletedAtUtc, reason);
            foreach (var tag in turn.Tags)
                MarkSoftDeleted(db, tag, deletedAtUtc, reason);
            foreach (var context in turn.ContextItems)
                MarkSoftDeleted(db, context, deletedAtUtc, reason);
            foreach (var dialog in turn.ProcessingDialog)
                MarkSoftDeleted(db, dialog, deletedAtUtc, reason);
            foreach (var commit in turn.Commits)
                MarkSoftDeleted(db, commit, deletedAtUtc, reason);
            foreach (var item in turn.StringListItems)
                MarkSoftDeleted(db, item, deletedAtUtc, reason);

            MarkSoftDeleted(db, turn, deletedAtUtc, reason);
        }

        MarkSoftDeleted(db, session, deletedAtUtc, reason);
    }

    private static void MarkSoftDeleted(McpDbContext db, object entity, DateTimeOffset deletedAtUtc, string reason)
    {
        var entry = db.Entry(entity);
        SetShadowValue(entry, "IsDeleted", true);
        SetShadowValue(entry, "DeletedAtUtc", deletedAtUtc);
        SetShadowValue(entry, "DeletedBy", nameof(SessionLogFederationStateAdapter));
        SetShadowValue(entry, "DeleteReason", reason);
    }

    private static bool IsSoftDeleted(McpDbContext db, object entity)
    {
        var entry = db.Entry(entity);
        return entry.Metadata.FindProperty("IsDeleted") is not null
               && entry.Property("IsDeleted").CurrentValue is true;
    }

    private static void SetShadowValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName, object? value)
    {
        if (entry.Metadata.FindProperty(propertyName) is null)
            return;

        var property = entry.Property(propertyName);
        property.CurrentValue = value;
        if (entry.State != EntityState.Added)
            property.IsModified = true;
    }
}

/// <summary>Federation adapter for requirements and traceability links.</summary>
public sealed class RequirementsFederationStateAdapter : DatabaseFederationStateAdapterBase
{
    /// <summary>Initializes a new instance of the <see cref="RequirementsFederationStateAdapter"/> class.</summary>
    /// <param name="scopeFactory">Scope factory used to resolve <see cref="McpDbContext"/>.</param>
    public RequirementsFederationStateAdapter(IServiceScopeFactory scopeFactory)
        : base("requirements", scopeFactory)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var requirementKey = RequirementKey.Parse(operation.ResourceId ?? string.Empty);
        await using var scope = ScopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();

        if (string.Equals(operation.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(requirementKey.Id))
                return Conflict("Requirements delete operation does not identify a requirement id.");

            var query = db.Requirements.Where(r => r.Id == requirementKey.Id);
            if (!string.IsNullOrWhiteSpace(requirementKey.Kind))
                query = query.Where(r => r.Kind == requirementKey.Kind);
            var rows = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
            if (rows.Count == 0)
                return new FederationApplyResult { Applied = true, Version = null };

            var ids = rows.Select(r => r.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var links = await db.RequirementTraceabilityLinks
                .Where(l => ids.Contains(l.FrId) || ids.Contains(l.TargetId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            db.RequirementTraceabilityLinks.RemoveRange(links);
            db.Requirements.RemoveRange(rows);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new FederationApplyResult { Applied = true, Version = null };
        }

        RequirementsPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<RequirementsPayload>(operation.PayloadJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            return Conflict($"Requirements federation payload is invalid JSON: {ex.Message}");
        }

        if (payload is null || payload.Requirements.Count == 0)
            return Conflict("Requirements federation payload does not contain requirement rows.");

        foreach (var item in payload.Requirements)
            await UpsertRequirementAsync(db, item, cancellationToken).ConfigureAwait(false);

        foreach (var link in payload.Links)
            await UpsertRequirementLinkAsync(db, link, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var version = await GetVersionAsync(operation.ResourceId ?? payload.Requirements[0].Id, cancellationToken).ConfigureAwait(false);
        return new FederationApplyResult { Applied = true, Version = version };
    }

    /// <inheritdoc />
    protected override async Task<object?> ReadPayloadAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken)
    {
        var requirementKey = RequirementKey.Parse(resourceId);
        var requirementsQuery = db.Requirements.AsNoTracking();
        requirementsQuery = string.IsNullOrWhiteSpace(requirementKey.Kind)
            ? requirementsQuery.Where(r => r.Id == requirementKey.Id)
            : requirementsQuery.Where(r => r.Kind == requirementKey.Kind && r.Id == requirementKey.Id);

        var requirements = await requirementsQuery
            .OrderBy(r => r.Kind)
            .ThenBy(r => r.Id)
            .Select(r => new
            {
                r.Kind,
                r.Id,
                r.Title,
                r.Body,
                r.CreatedAtUtc,
                r.UpdatedAtUtc,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (requirements.Count == 0)
            return null;

        var ids = requirements.Select(r => r.Id).ToList();
        var links = await db.RequirementTraceabilityLinks
            .AsNoTracking()
            .Where(l => ids.Contains(l.FrId) || ids.Contains(l.TargetId))
            .OrderBy(l => l.FrId)
            .ThenBy(l => l.TargetKind)
            .ThenBy(l => l.TargetId)
            .Select(l => new
            {
                l.FrId,
                l.TargetKind,
                l.TargetId,
                l.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new { requirements, links };
    }

    private static async Task UpsertRequirementAsync(
        McpDbContext db,
        RequirementPayload item,
        CancellationToken cancellationToken)
    {
        var kind = string.IsNullOrWhiteSpace(item.Kind) ? "fr" : item.Kind.Trim().ToLowerInvariant();
        var id = item.Id.Trim();
        var entity = await db.Requirements
            .FirstOrDefaultAsync(r => r.Kind == kind && r.Id == id, cancellationToken)
            .ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        if (entity is null)
        {
            entity = new RequirementEntity
            {
                Kind = kind,
                Id = id,
                CreatedAtUtc = string.IsNullOrWhiteSpace(item.CreatedAtUtc) ? now : item.CreatedAtUtc,
            };
            db.Requirements.Add(entity);
        }

        entity.Title = item.Title ?? string.Empty;
        entity.Body = item.Body ?? string.Empty;
        entity.UpdatedAtUtc = string.IsNullOrWhiteSpace(item.UpdatedAtUtc) ? now : item.UpdatedAtUtc;
    }

    private static async Task UpsertRequirementLinkAsync(
        McpDbContext db,
        RequirementLinkPayload link,
        CancellationToken cancellationToken)
    {
        var frId = link.FrId.Trim();
        var targetKind = link.TargetKind.Trim().ToLowerInvariant();
        var targetId = link.TargetId.Trim();
        var entity = await db.RequirementTraceabilityLinks
            .FirstOrDefaultAsync(l =>
                l.FrId == frId &&
                l.TargetKind == targetKind &&
                l.TargetId == targetId,
                cancellationToken)
            .ConfigureAwait(false);
        if (entity is not null)
            return;

        db.RequirementTraceabilityLinks.Add(new RequirementTraceabilityLinkEntity
        {
            FrId = frId,
            TargetKind = targetKind,
            TargetId = targetId,
            CreatedAtUtc = string.IsNullOrWhiteSpace(link.CreatedAtUtc)
                ? DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
                : link.CreatedAtUtc,
        });
    }

    private static FederationApplyResult Conflict(string message)
        => new()
        {
            Applied = false,
            Conflict = true,
            Message = message,
        };

    private sealed class RequirementsPayload
    {
        public List<RequirementPayload> Requirements { get; set; } = [];

        public List<RequirementLinkPayload> Links { get; set; } = [];
    }

    private sealed class RequirementPayload
    {
        public string Kind { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string? Title { get; set; }

        public string? Body { get; set; }

        public string? CreatedAtUtc { get; set; }

        public string? UpdatedAtUtc { get; set; }
    }

    private sealed class RequirementLinkPayload
    {
        public string FrId { get; set; } = string.Empty;

        public string TargetKind { get; set; } = string.Empty;

        public string TargetId { get; set; } = string.Empty;

        public string? CreatedAtUtc { get; set; }
    }
}

/// <summary>Federation adapter for tool bucket and tool definition metadata.</summary>
public sealed class ToolsBucketsFederationStateAdapter : DatabaseFederationStateAdapterBase
{
    /// <summary>Initializes a new instance of the <see cref="ToolsBucketsFederationStateAdapter"/> class.</summary>
    /// <param name="scopeFactory">Scope factory used to resolve <see cref="McpDbContext"/>.</param>
    public ToolsBucketsFederationStateAdapter(IServiceScopeFactory scopeFactory)
        : base("tools_buckets", scopeFactory)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await using var scope = ScopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();

        if (string.Equals(operation.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase))
        {
            var resourceId = operation.ResourceId;
            if (string.IsNullOrWhiteSpace(resourceId))
                return Conflict("Tools/buckets delete operation does not identify a resource id.");

            var tools = await db.ToolDefinitions
                .Include(t => t.Tags)
                .Where(t => t.Name == resourceId || t.BucketName == resourceId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var buckets = await db.ToolBuckets
                .Where(b => b.Name == resourceId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            db.ToolDefinitions.RemoveRange(tools);
            db.ToolBuckets.RemoveRange(buckets);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new FederationApplyResult { Applied = true, Version = null };
        }

        ToolsBucketsPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ToolsBucketsPayload>(operation.PayloadJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            return Conflict($"Tools/buckets federation payload is invalid JSON: {ex.Message}");
        }

        if (payload is null || (payload.Buckets.Count == 0 && payload.Tools.Count == 0))
            return Conflict("Tools/buckets federation payload does not contain bucket or tool rows.");

        foreach (var bucket in payload.Buckets)
            await UpsertBucketAsync(db, bucket, cancellationToken).ConfigureAwait(false);
        foreach (var tool in payload.Tools)
            await UpsertToolAsync(db, tool, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new FederationApplyResult
        {
            Applied = true,
            Version = await GetVersionAsync(operation.ResourceId ?? "*", cancellationToken).ConfigureAwait(false),
        };
    }

    /// <inheritdoc />
    protected override async Task<object?> ReadPayloadAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken)
    {
        var buckets = await db.ToolBuckets
            .AsNoTracking()
            .Where(b => b.Name == resourceId || resourceId == "*")
            .OrderBy(b => b.Name)
            .Select(b => new
            {
                b.Name,
                b.Owner,
                b.Repo,
                b.Branch,
                b.ManifestPath,
                b.DateTimeCreated,
                b.DateTimeLastSynced,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var tools = await db.ToolDefinitions
            .AsNoTracking()
            .Where(t => t.BucketName == resourceId || t.Name == resourceId || resourceId == "*")
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.Name,
                t.Description,
                t.ParameterSchema,
                t.CommandTemplate,
                t.WorkspacePath,
                t.BucketName,
                t.DateTimeCreated,
                t.DateTimeModified,
                Tags = t.Tags.OrderBy(tag => tag.Tag).Select(tag => tag.Tag).ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return buckets.Count == 0 && tools.Count == 0 ? null : new { buckets, tools };
    }

    private static async Task UpsertBucketAsync(McpDbContext db, ToolBucketPayload bucket, CancellationToken cancellationToken)
    {
        var name = bucket.Name.Trim();
        var entity = await db.ToolBuckets
            .FirstOrDefaultAsync(b => b.Name == name, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            entity = new ToolBucketEntity
            {
                Name = name,
                Owner = bucket.Owner ?? string.Empty,
                Repo = bucket.Repo ?? string.Empty,
                DateTimeCreated = bucket.DateTimeCreated ?? DateTimeOffset.UtcNow,
            };
            db.ToolBuckets.Add(entity);
        }

        entity.Owner = bucket.Owner ?? entity.Owner;
        entity.Repo = bucket.Repo ?? entity.Repo;
        entity.Branch = string.IsNullOrWhiteSpace(bucket.Branch) ? entity.Branch : bucket.Branch;
        entity.ManifestPath = string.IsNullOrWhiteSpace(bucket.ManifestPath) ? entity.ManifestPath : bucket.ManifestPath;
        entity.DateTimeLastSynced = bucket.DateTimeLastSynced;
    }

    private static async Task UpsertToolAsync(McpDbContext db, ToolDefinitionPayload tool, CancellationToken cancellationToken)
    {
        var name = tool.Name.Trim();
        var workspacePath = tool.WorkspacePath;
        var entity = await db.ToolDefinitions
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Name == name && t.WorkspacePath == workspacePath, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            entity = new ToolDefinitionEntity
            {
                Name = name,
                Description = tool.Description ?? string.Empty,
                DateTimeCreated = tool.DateTimeCreated ?? DateTimeOffset.UtcNow,
            };
            db.ToolDefinitions.Add(entity);
        }

        entity.Description = tool.Description ?? entity.Description;
        entity.ParameterSchema = tool.ParameterSchema;
        entity.CommandTemplate = tool.CommandTemplate;
        entity.WorkspacePath = workspacePath;
        entity.BucketName = tool.BucketName;
        entity.DateTimeModified = tool.DateTimeModified ?? DateTimeOffset.UtcNow;
        entity.Tags.Clear();
        foreach (var tag in tool.Tags.Distinct(StringComparer.OrdinalIgnoreCase))
            entity.Tags.Add(new ToolDefinitionTagEntity { Tag = tag });
    }

    private static FederationApplyResult Conflict(string message)
        => new()
        {
            Applied = false,
            Conflict = true,
            Message = message,
        };

    private sealed class ToolsBucketsPayload
    {
        public List<ToolBucketPayload> Buckets { get; set; } = [];

        public List<ToolDefinitionPayload> Tools { get; set; } = [];
    }

    private sealed class ToolBucketPayload
    {
        public string Name { get; set; } = string.Empty;

        public string? Owner { get; set; }

        public string? Repo { get; set; }

        public string? Branch { get; set; }

        public string? ManifestPath { get; set; }

        public DateTimeOffset? DateTimeCreated { get; set; }

        public DateTimeOffset? DateTimeLastSynced { get; set; }
    }

    private sealed class ToolDefinitionPayload
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? ParameterSchema { get; set; }

        public string? CommandTemplate { get; set; }

        public string? WorkspacePath { get; set; }

        public string? BucketName { get; set; }

        public DateTimeOffset? DateTimeCreated { get; set; }

        public DateTimeOffset? DateTimeModified { get; set; }

        public List<string> Tags { get; set; } = [];
    }
}

/// <summary>Federation adapter for persisted agent definitions and workspace configuration.</summary>
public sealed class AgentsFederationStateAdapter : DatabaseFederationStateAdapterBase
{
    /// <summary>Initializes a new instance of the <see cref="AgentsFederationStateAdapter"/> class.</summary>
    /// <param name="scopeFactory">Scope factory used to resolve <see cref="McpDbContext"/>.</param>
    public AgentsFederationStateAdapter(IServiceScopeFactory scopeFactory)
        : base("agents", scopeFactory)
    {
    }

    /// <inheritdoc />
    public override async ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await using var scope = ScopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();

        if (string.Equals(operation.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase))
        {
            var resourceId = operation.ResourceId;
            if (string.IsNullOrWhiteSpace(resourceId))
                return Conflict("Agents delete operation does not identify an agent definition id.");

            var workspaceConfigs = await db.AgentWorkspaces
                .Where(a => a.AgentDefinitionId == resourceId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var definitions = await db.AgentDefinitions
                .Where(a => a.Id == resourceId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            db.AgentWorkspaces.RemoveRange(workspaceConfigs);
            db.AgentDefinitions.RemoveRange(definitions);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new FederationApplyResult { Applied = true, Version = null };
        }

        AgentsPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AgentsPayload>(operation.PayloadJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            return Conflict($"Agents federation payload is invalid JSON: {ex.Message}");
        }

        if (payload is null || (payload.Definitions.Count == 0 && payload.WorkspaceConfigs.Count == 0))
            return Conflict("Agents federation payload does not contain agent definition or workspace rows.");

        foreach (var definition in payload.Definitions)
            await UpsertAgentDefinitionAsync(db, definition, cancellationToken).ConfigureAwait(false);
        foreach (var workspace in payload.WorkspaceConfigs)
            await UpsertAgentWorkspaceAsync(db, workspace, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new FederationApplyResult
        {
            Applied = true,
            Version = await GetVersionAsync(operation.ResourceId ?? "*", cancellationToken).ConfigureAwait(false),
        };
    }

    /// <inheritdoc />
    protected override async Task<object?> ReadPayloadAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken)
    {
        // Lists live in 4NF child tables (auto-included); the wire snapshot keeps its
        // serialized-JSON field shape so federated peers are unaffected by the decomposition.
        var definitionRows = await db.AgentDefinitions
            .AsNoTracking()
            .Where(a => a.Id == resourceId || resourceId == "*")
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var definitions = definitionRows
            .Select(a => new
            {
                a.Id,
                a.DisplayName,
                a.DefaultLaunchCommand,
                a.DefaultInstructionFile,
                DefaultModelsJson = JsonSerializer.Serialize(
                    a.Models.OrderBy(m => m.Ordinal).Select(m => m.Model).ToList(), JsonOptions),
                a.DefaultBranchStrategy,
                a.DefaultSeedPrompt,
                a.IsBuiltIn,
                a.CreatedAt,
                a.ModifiedAt,
            })
            .ToList();

        var workspaceRows = await db.AgentWorkspaces
            .AsNoTracking()
            .Where(a => a.AgentDefinitionId == resourceId || resourceId == "*")
            .OrderBy(a => a.AgentDefinitionId)
            .ThenBy(a => a.WorkspacePath)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        string? SerializeOverride(AgentWorkspaceEntity a, string listType)
        {
            var values = a.ListItems
                .Where(r => r.ListType == listType)
                .OrderBy(r => r.Ordinal)
                .Select(r => r.Value)
                .ToList();
            return values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);
        }

        var workspaceConfigs = workspaceRows
            .Select(a => new
            {
                a.AgentDefinitionId,
                a.WorkspacePath,
                a.Enabled,
                a.Banned,
                a.BannedReason,
                a.BannedUntilPr,
                a.AgentIsolation,
                a.LaunchCommandOverride,
                ModelsOverrideJson = SerializeOverride(a, "ModelOverride"),
                a.BranchStrategyOverride,
                a.SeedPromptOverride,
                a.MarkerAdditions,
                InstructionFilesOverrideJson = SerializeOverride(a, "InstructionFileOverride"),
                a.RestartPolicy,
                a.AddedAt,
            })
            .ToList();

        return definitions.Count == 0 && workspaceConfigs.Count == 0 ? null : new { definitions, workspaceConfigs };
    }

    private static async Task UpsertAgentDefinitionAsync(
        McpDbContext db,
        AgentDefinitionPayload definition,
        CancellationToken cancellationToken)
    {
        var id = definition.Id.Trim();
        var entity = await db.AgentDefinitions
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            entity = new AgentDefinitionEntity
            {
                Id = id,
                CreatedAt = definition.CreatedAt ?? DateTime.UtcNow,
            };
            db.AgentDefinitions.Add(entity);
        }

        entity.DisplayName = definition.DisplayName ?? entity.DisplayName;
        entity.DefaultLaunchCommand = definition.DefaultLaunchCommand ?? entity.DefaultLaunchCommand;
        entity.DefaultInstructionFile = definition.DefaultInstructionFile ?? entity.DefaultInstructionFile;
        if (definition.DefaultModelsJson is not null)
        {
            var models = ParseWireList(definition.DefaultModelsJson) ?? [];
            entity.Models.Clear();
            for (var i = 0; i < models.Count; i++)
                entity.Models.Add(new AgentDefinitionModelEntity { WorkspaceId = entity.WorkspaceId, Ordinal = i, Model = models[i] });
        }

        entity.DefaultBranchStrategy = definition.DefaultBranchStrategy ?? entity.DefaultBranchStrategy;
        entity.DefaultSeedPrompt = definition.DefaultSeedPrompt ?? entity.DefaultSeedPrompt;
        entity.IsBuiltIn = definition.IsBuiltIn;
        entity.ModifiedAt = definition.ModifiedAt ?? DateTime.UtcNow;
    }

    private static List<string>? ParseWireList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task UpsertAgentWorkspaceAsync(
        McpDbContext db,
        AgentWorkspacePayload workspace,
        CancellationToken cancellationToken)
    {
        var agentDefinitionId = workspace.AgentDefinitionId.Trim();
        var workspacePath = workspace.WorkspacePath.Trim();
        var entity = await db.AgentWorkspaces
            .FirstOrDefaultAsync(a => a.AgentDefinitionId == agentDefinitionId && a.WorkspacePath == workspacePath, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            entity = new AgentWorkspaceEntity
            {
                AgentDefinitionId = agentDefinitionId,
                WorkspacePath = workspacePath,
                AddedAt = workspace.AddedAt ?? DateTime.UtcNow,
            };
            db.AgentWorkspaces.Add(entity);
        }

        entity.Enabled = workspace.Enabled;
        entity.Banned = workspace.Banned;
        entity.BannedReason = workspace.BannedReason;
        entity.BannedUntilPr = workspace.BannedUntilPr;
        entity.AgentIsolation = string.IsNullOrWhiteSpace(workspace.AgentIsolation) ? entity.AgentIsolation : workspace.AgentIsolation;
        entity.LaunchCommandOverride = workspace.LaunchCommandOverride;
        entity.ListItems.Clear();
        var modelOverride = ParseWireList(workspace.ModelsOverrideJson) ?? [];
        for (var i = 0; i < modelOverride.Count; i++)
            entity.ListItems.Add(new AgentWorkspaceListItemEntity { WorkspaceId = entity.WorkspaceId, ListType = "ModelOverride", Ordinal = i, Value = modelOverride[i] });
        var fileOverride = ParseWireList(workspace.InstructionFilesOverrideJson) ?? [];
        for (var i = 0; i < fileOverride.Count; i++)
            entity.ListItems.Add(new AgentWorkspaceListItemEntity { WorkspaceId = entity.WorkspaceId, ListType = "InstructionFileOverride", Ordinal = i, Value = fileOverride[i] });
        entity.BranchStrategyOverride = workspace.BranchStrategyOverride;
        entity.SeedPromptOverride = workspace.SeedPromptOverride;
        entity.MarkerAdditions = workspace.MarkerAdditions ?? string.Empty;
        entity.RestartPolicy = string.IsNullOrWhiteSpace(workspace.RestartPolicy) ? entity.RestartPolicy : workspace.RestartPolicy;
    }

    private static FederationApplyResult Conflict(string message)
        => new()
        {
            Applied = false,
            Conflict = true,
            Message = message,
        };

    private sealed class AgentsPayload
    {
        public List<AgentDefinitionPayload> Definitions { get; set; } = [];

        public List<AgentWorkspacePayload> WorkspaceConfigs { get; set; } = [];
    }

    private sealed class AgentDefinitionPayload
    {
        public string Id { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public string? DefaultLaunchCommand { get; set; }

        public string? DefaultInstructionFile { get; set; }

        public string? DefaultModelsJson { get; set; }

        public string? DefaultBranchStrategy { get; set; }

        public string? DefaultSeedPrompt { get; set; }

        public bool IsBuiltIn { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? ModifiedAt { get; set; }
    }

    private sealed class AgentWorkspacePayload
    {
        public string AgentDefinitionId { get; set; } = string.Empty;

        public string WorkspacePath { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;

        public bool Banned { get; set; }

        public string? BannedReason { get; set; }

        public int? BannedUntilPr { get; set; }

        public string? AgentIsolation { get; set; }

        public string? LaunchCommandOverride { get; set; }

        public string? ModelsOverrideJson { get; set; }

        public string? BranchStrategyOverride { get; set; }

        public string? SeedPromptOverride { get; set; }

        public string? MarkerAdditions { get; set; }

        public string? InstructionFilesOverrideJson { get; set; }

        public string? RestartPolicy { get; set; }

        public DateTime? AddedAt { get; set; }
    }
}

/// <summary>Federation adapter for domains that are intentionally local-only.</summary>
public sealed class LocalOnlyFederationStateAdapter : FederationStateAdapterBase
{
    private readonly string _reason;

    /// <summary>Initializes a new instance of the <see cref="LocalOnlyFederationStateAdapter"/> class.</summary>
    /// <param name="domain">Mutable state domain.</param>
    /// <param name="reason">Reason the domain is excluded from replication.</param>
    public LocalOnlyFederationStateAdapter(string domain, string reason)
        : base(domain)
    {
        _reason = reason;
    }

    /// <inheritdoc />
    public override bool IsLocalOnly => true;

    /// <inheritdoc />
    public override ValueTask<FederationStateSnapshot> SnapshotAsync(string resourceId, CancellationToken cancellationToken)
    {
        var payloadJson = JsonSerializer.Serialize(new { localOnly = true, reason = _reason }, JsonOptions);
        return new(new FederationStateSnapshot
        {
            Domain = Domain,
            ResourceId = resourceId,
            PayloadJson = payloadJson,
            Version = null,
        });
    }

    /// <inheritdoc />
    public override ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
        => new(new FederationApplyResult
        {
            Applied = false,
            Conflict = true,
            Message = $"Domain '{Domain}' is local-only: {_reason}",
        });

    /// <inheritdoc />
    public override ValueTask<string?> GetVersionAsync(string resourceId, CancellationToken cancellationToken)
        => new((string?)null);
}

/// <summary>Composite key parser for session log adapter resource identifiers.</summary>
internal readonly record struct SessionLogKey(string? SourceType, string SessionId)
{
    /// <summary>Parses resource identifiers in <c>source/session</c>, <c>source:session</c>, or <c>session</c> form.</summary>
    public static SessionLogKey Parse(string resourceId)
    {
        var trimmed = resourceId.Trim();
        var slashIndex = trimmed.IndexOf('/', StringComparison.Ordinal);
        if (slashIndex > 0 && slashIndex < trimmed.Length - 1)
            return new SessionLogKey(trimmed[..slashIndex], trimmed[(slashIndex + 1)..]);

        var colonIndex = trimmed.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex > 0 && colonIndex < trimmed.Length - 1)
            return new SessionLogKey(trimmed[..colonIndex], trimmed[(colonIndex + 1)..]);

        return new SessionLogKey(null, trimmed);
    }
}

/// <summary>Composite key parser for requirement adapter resource identifiers.</summary>
internal readonly record struct RequirementKey(string? Kind, string Id)
{
    /// <summary>Parses resource identifiers in <c>kind/id</c>, <c>kind:id</c>, or <c>id</c> form.</summary>
    public static RequirementKey Parse(string resourceId)
    {
        var trimmed = resourceId.Trim();
        var slashIndex = trimmed.IndexOf('/', StringComparison.Ordinal);
        if (slashIndex > 0 && slashIndex < trimmed.Length - 1)
            return new RequirementKey(NormalizeKind(trimmed[..slashIndex]), trimmed[(slashIndex + 1)..]);

        var colonIndex = trimmed.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex > 0 && colonIndex < trimmed.Length - 1)
            return new RequirementKey(NormalizeKind(trimmed[..colonIndex]), trimmed[(colonIndex + 1)..]);

        return new RequirementKey(null, trimmed);
    }

    private static string NormalizeKind(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "functional" => "fr",
            "technical" => "tr",
            "testing" => "test",
            var kind => kind,
        };
}
