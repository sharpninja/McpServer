using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Storage;
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
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Initializes a new instance of the <see cref="DatabaseFederationStateAdapterBase"/> class.</summary>
    /// <param name="domain">Mutable state domain handled by the adapter.</param>
    /// <param name="scopeFactory">Scope factory used to resolve <see cref="McpDbContext"/>.</param>
    protected DatabaseFederationStateAdapterBase(string domain, IServiceScopeFactory scopeFactory)
        : base(domain)
    {
        _scopeFactory = scopeFactory;
    }

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
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        return await ReadPayloadAsync(db, resourceId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> GetExplicitVersionAsync(string resourceId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
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

    private async Task<WorkspaceDto?> GetWorkspaceAsync(string resourceId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var workspaceService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        return await workspaceService.GetAsync(resourceId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Federation adapter for authoritative TODO state.</summary>
public sealed class TodoFederationStateAdapter : DatabaseFederationStateAdapterBase
{
    /// <summary>Initializes a new instance of the <see cref="TodoFederationStateAdapter"/> class.</summary>
    /// <param name="scopeFactory">Scope factory used to resolve <see cref="McpDbContext"/>.</param>
    public TodoFederationStateAdapter(IServiceScopeFactory scopeFactory)
        : base("todo", scopeFactory)
    {
    }

    /// <inheritdoc />
    protected override async Task<object?> ReadPayloadAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken)
    {
        var item = await db.TodoItems
            .AsNoTracking()
            .Where(t => t.Id == resourceId)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Section,
                t.Priority,
                t.Done,
                t.Estimate,
                t.Note,
                t.DescriptionJson,
                t.TechnicalDetailsJson,
                t.ImplementationTasksJson,
                t.CompletedDate,
                t.DoneSummary,
                t.Remaining,
                t.PriorityNote,
                t.Reference,
                t.DependsOnJson,
                t.FunctionalRequirementsJson,
                t.TechnicalRequirementsJson,
                t.ItemKind,
                t.SectionOrder,
                t.ItemOrder,
                t.PhaseLabel,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
            return null;

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
    protected override async Task<object?> ReadPayloadAsync(McpDbContext db, string resourceId, CancellationToken cancellationToken)
    {
        var definitions = await db.AgentDefinitions
            .AsNoTracking()
            .Where(a => a.Id == resourceId || resourceId == "*")
            .OrderBy(a => a.Id)
            .Select(a => new
            {
                a.Id,
                a.DisplayName,
                a.DefaultLaunchCommand,
                a.DefaultInstructionFile,
                a.DefaultModelsJson,
                a.DefaultBranchStrategy,
                a.DefaultSeedPrompt,
                a.IsBuiltIn,
                a.CreatedAt,
                a.ModifiedAt,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var workspaceConfigs = await db.AgentWorkspaces
            .AsNoTracking()
            .Where(a => a.AgentDefinitionId == resourceId || resourceId == "*")
            .OrderBy(a => a.AgentDefinitionId)
            .ThenBy(a => a.WorkspacePath)
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
                a.ModelsOverrideJson,
                a.BranchStrategyOverride,
                a.SeedPromptOverride,
                a.MarkerAdditions,
                a.InstructionFilesOverrideJson,
                a.RestartPolicy,
                a.AddedAt,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return definitions.Count == 0 && workspaceConfigs.Count == 0 ? null : new { definitions, workspaceConfigs };
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
