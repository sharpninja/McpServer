using System.Linq.Expressions;
using System.Data.Common;
using System.Text.Json;
using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Storage;

/// <summary>
/// TR-PLANNED-CORE-013: EF Core DbContext for MCP metadata and chunks.
/// FR-SUPPORT-010: SQLite storage for local MCP server.
/// TR-MCP-MT-003: Global query filter on WorkspaceId for multi-tenant data isolation.
/// </summary>
public sealed class McpDbContext : DbContext
{
    private string _workspaceId;

    /// <summary>TR-PLANNED-CORE-013: Constructor for DI with workspace context.</summary>
    public McpDbContext(DbContextOptions<McpDbContext> options, WorkspaceContext? workspaceContext = null)
        : base(options)
    {
        _workspaceId = workspaceContext?.WorkspacePath ?? string.Empty;
    }

    /// <summary>TR-MCP-MT-003: Gets the current workspace discriminator applied to this context instance.</summary>
    public string CurrentWorkspaceId => _workspaceId;

    /// <summary>TR-MCP-MT-001: Overrides the workspace ID for this context instance (e.g. from an MCP tool parameter).</summary>
    public void OverrideWorkspaceId(string workspaceId) => _workspaceId = workspaceId;

    /// <summary>TR-MCP-DB-001: Canonical database-authoritative workspace registry.</summary>
    public DbSet<WorkspaceEntity> Workspaces => Set<WorkspaceEntity>();

    /// <summary>TR-MCP-DB-004: Generic append-only mutable-entity audit ledger.</summary>
    public DbSet<DataAuditLogEntity> DataAuditLogs => Set<DataAuditLogEntity>();

    /// <summary>TR-PLANNED-CORE-013: Indexed documents.</summary>
    public DbSet<ContextDocumentEntity> Documents => Set<ContextDocumentEntity>();

    /// <summary>TR-PLANNED-CORE-013: Indexed chunks.</summary>
    public DbSet<ContextChunkEntity> Chunks => Set<ContextChunkEntity>();

    /// <summary>TR-PLANNED-CORE-013: Session logs (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogEntity> SessionLogs => Set<SessionLogEntity>();

    /// <summary>TR-PLANNED-CORE-013: Session log turns (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogTurnEntity> SessionLogTurns => Set<SessionLogTurnEntity>();

    /// <summary>TR-PLANNED-CORE-013: Session log turn actions (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogActionEntity> SessionLogActions => Set<SessionLogActionEntity>();

    /// <summary>TR-PLANNED-CORE-013: Session log turn tags (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogTurnTagEntity> SessionLogTurnTags => Set<SessionLogTurnTagEntity>();

    /// <summary>TR-PLANNED-CORE-013: Session log turn context items (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogTurnContextEntity> SessionLogTurnContexts => Set<SessionLogTurnContextEntity>();

    /// <summary>TR-PLANNED-CORE-013: Session log turn processing dialog items (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogProcessingDialogEntity> SessionLogProcessingDialogs => Set<SessionLogProcessingDialogEntity>();

    /// <summary>TR-PLANNED-CORE-013: Session log turn commits.</summary>
    public DbSet<SessionLogCommitEntity> SessionLogCommits => Set<SessionLogCommitEntity>();

    /// <summary>TR-PLANNED-CORE-013: Session log turn string-list items (design decisions, requirements, files modified, blockers).</summary>
    public DbSet<SessionLogTurnStringListEntity> SessionLogTurnStringLists => Set<SessionLogTurnStringListEntity>();

    /// <summary>Tool definitions discoverable by keyword search.</summary>
    public DbSet<ToolDefinitionEntity> ToolDefinitions => Set<ToolDefinitionEntity>();

    /// <summary>Keyword tags for tool definitions.</summary>
    public DbSet<ToolDefinitionTagEntity> ToolDefinitionTags => Set<ToolDefinitionTagEntity>();

    /// <summary>Tool bucket repositories (GitHub-backed manifest sources).</summary>
    public DbSet<ToolBucketEntity> ToolBuckets => Set<ToolBucketEntity>();

    /// <summary>Agent type definitions (built-in and custom).</summary>
    public DbSet<AgentDefinitionEntity> AgentDefinitions => Set<AgentDefinitionEntity>();

    /// <summary>Per-workspace agent configurations.</summary>
    public DbSet<AgentWorkspaceEntity> AgentWorkspaces => Set<AgentWorkspaceEntity>();

    /// <summary>Agent lifecycle event audit log.</summary>
    public DbSet<AgentEventLogEntity> AgentEventLogs => Set<AgentEventLogEntity>();

    /// <summary>FR-MCP-079: Explicit graph entity nodes.</summary>
    public DbSet<GraphEntityEntity> GraphEntities => Set<GraphEntityEntity>();

    /// <summary>FR-MCP-079: Explicit graph relationship edges.</summary>
    public DbSet<GraphRelationshipEntity> GraphRelationships => Set<GraphRelationshipEntity>();

    /// <summary>TR-MCP-TODO-005 (provider-agnostic): Authoritative TODO items.</summary>
    public DbSet<TodoItemEntity> TodoItems => Set<TodoItemEntity>();

    /// <summary>TR-MCP-DB-005: Normalized TODO-to-requirement link rows.</summary>
    public DbSet<TodoRequirementLinkEntity> TodoRequirementLinks => Set<TodoRequirementLinkEntity>();

    /// <summary>TR-MCP-TODO-005 (provider-agnostic): Append-only TODO audit rows.</summary>
    public DbSet<TodoAuditHistoryEntity> TodoAuditHistory => Set<TodoAuditHistoryEntity>();

    /// <summary>TR-MCP-TODO-005 / TR-MCP-TODO-006 (provider-agnostic): Singleton TODO document metadata.</summary>
    public DbSet<TodoDocumentMetadataEntity> TodoDocumentMetadata => Set<TodoDocumentMetadataEntity>();

    /// <summary>Authoritative workspace-scoped FR/TR/TEST requirements.</summary>
    public DbSet<RequirementEntity> Requirements => Set<RequirementEntity>();

    /// <summary>FR-MCP-REQSCOPE-001: workspace-scoped requirement scope layers.</summary>
    public DbSet<RequirementScopeLayerEntity> RequirementScopeLayers => Set<RequirementScopeLayerEntity>();

    /// <summary>Authoritative workspace-scoped FR-to-TR/TEST traceability links.</summary>
    public DbSet<RequirementTraceabilityLinkEntity> RequirementTraceabilityLinks => Set<RequirementTraceabilityLinkEntity>();

    /// <summary>TR-MCP-MEMORY-001: Authoritative raw-text MCP memories.</summary>
    public DbSet<MemoryEntity> Memories => Set<MemoryEntity>();

    /// <summary>TR-MCP-QUAD-001: Durable external brain-slot definitions.</summary>
    public DbSet<BrainSlotDefinitionEntity> BrainSlotDefinitions => Set<BrainSlotDefinitionEntity>();

    /// <summary>TR-MCP-QUAD-001: Durable external brain-slot invocation audit rows.</summary>
    public DbSet<BrainSlotInvocationEntity> BrainSlotInvocations => Set<BrainSlotInvocationEntity>();

    /// <summary>TR-MCP-TRIAGE-001: Durable incidental bug triage reports.</summary>
    public DbSet<TriageReportEntity> TriageReports => Set<TriageReportEntity>();

    /// <summary>TR-MCP-TRIAGE-001: Durable deterministic triage report groups.</summary>
    public DbSet<TriageGroupEntity> TriageGroups => Set<TriageGroupEntity>();

    /// <summary>TR-MCP-TRIAGE-001: Durable triage research run audit rows.</summary>
    public DbSet<TriageResearchRunEntity> TriageResearchRuns => Set<TriageResearchRunEntity>();

    /// <summary>FR-MCP-103: Enrolled local federation proxies known by the hub.</summary>
    public DbSet<FederationProxyEntity> FederationProxies => Set<FederationProxyEntity>();

    /// <summary>FR-MCP-103: Workspaces hosted by local federation proxies.</summary>
    public DbSet<FederationWorkspaceEntity> FederationWorkspaces => Set<FederationWorkspaceEntity>();

    /// <summary>FR-MCP-103: Queued, replayed, and acknowledged federation operations.</summary>
    public DbSet<FederationOperationEntity> FederationOperations => Set<FederationOperationEntity>();

    /// <summary>FR-MCP-103: Hub fanout rows waiting for proxy acknowledgement.</summary>
    public DbSet<FederationOutboxEntity> FederationOutbox => Set<FederationOutboxEntity>();

    /// <summary>FR-MCP-103: Conflicts created by stale proxy writes.</summary>
    public DbSet<FederationConflictEntity> FederationConflicts => Set<FederationConflictEntity>();

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        base.OnConfiguring(optionsBuilder);

        // EF Core 10 throws PendingModelChangesWarning by default when the runtime model
        // differs from the resolved snapshot. The production database strategy already
        // suppresses this warning (see SqliteMcpDatabaseProviderStrategy and friends).
        // Mirror that here so tests and ad-hoc consumers that construct the context with
        // a bare provider (e.g. UseSqlite without MigrationsAssembly) do not throw when
        // the snapshot lives in one of the per-provider migrations assemblies.
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<WorkspaceEntity>(e =>
        {
            e.HasKey(x => x.WorkspaceId);
            e.HasIndex(x => x.WorkspacePath).IsUnique();
            e.HasIndex(x => x.IsPrimary);
            e.HasIndex(x => x.IsEnabled);
            e.HasIndex(x => x.IsDeleted);
            e.HasData(new WorkspaceEntity
            {
                WorkspaceId = string.Empty,
                WorkspacePath = string.Empty,
                Name = "global",
                TodoPath = "docs/todo.yaml",
                IsEnabled = true,
                CurrentRequirementLayerKey = "layer-1",
                DateTimeCreated = DateTimeOffset.UnixEpoch,
                DateTimeModified = DateTimeOffset.UnixEpoch,
            });
        });

        modelBuilder.Entity<DataAuditLogEntity>(e =>
        {
            e.HasKey(x => x.AuditId);
            e.HasIndex(x => new { x.WorkspaceId, x.EntityKind, x.EntityKey });
            e.HasIndex(x => x.Action);
            e.HasIndex(x => x.OccurredAtUtc);
            e.HasIndex(x => x.CorrelationId);
            e.HasIndex(x => x.FederationOperationId);
        });

        modelBuilder.Entity<ContextDocumentEntity>(e =>
        {
            e.HasIndex(x => x.SourceType);
            e.HasIndex(x => x.SourceKey);
            e.HasIndex(x => x.IngestedAt);
        });
        modelBuilder.Entity<ContextChunkEntity>(e =>
        {
            e.HasIndex(x => x.DocumentId);
            e.HasOne(x => x.Document)
                .WithMany(x => x.Chunks)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SessionLogEntity>(e =>
        {
            e.HasIndex(x => new { x.WorkspaceId, x.SourceType, x.SessionId }).IsUnique();
            e.HasIndex(x => x.SourceType);
            e.HasIndex(x => x.Started);
            e.HasIndex(x => x.LastUpdated);
            e.HasIndex(x => x.AgentDefinitionId);
            e.HasOne(x => x.AgentDefinition)
                .WithMany()
                .HasForeignKey(x => x.AgentDefinitionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SessionLogTurnEntity>(e =>
        {
            e.HasIndex(x => new { x.SessionLogId, x.RequestId }).IsUnique();
            e.HasOne(x => x.SessionLog)
                .WithMany(x => x.Turns)
                .HasForeignKey(x => x.SessionLogId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SessionLogActionEntity>(e =>
        {
            e.HasOne(x => x.SessionLogTurn)
                .WithMany(x => x.Actions)
                .HasForeignKey(x => x.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SessionLogTurnTagEntity>(e =>
        {
            e.HasOne(x => x.SessionLogTurn)
                .WithMany(x => x.Tags)
                .HasForeignKey(x => x.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SessionLogTurnContextEntity>(e =>
        {
            e.HasOne(x => x.SessionLogTurn)
                .WithMany(x => x.ContextItems)
                .HasForeignKey(x => x.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SessionLogProcessingDialogEntity>(e =>
        {
            e.HasOne(x => x.SessionLogTurn)
                .WithMany(x => x.ProcessingDialog)
                .HasForeignKey(x => x.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SessionLogCommitEntity>(e =>
        {
            e.HasOne(x => x.SessionLogTurn)
                .WithMany(x => x.Commits)
                .HasForeignKey(x => x.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SessionLogTurnStringListEntity>(e =>
        {
            e.HasOne(x => x.SessionLogTurn)
                .WithMany(x => x.StringListItems)
                .HasForeignKey(x => x.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.SessionLogTurnId, x.ListType });
        });

        modelBuilder.Entity<ToolDefinitionEntity>(e =>
        {
            e.HasIndex(x => new { x.Name, x.WorkspacePath }).IsUnique();
            e.HasIndex(x => x.WorkspacePath);
        });

        modelBuilder.Entity<ToolDefinitionTagEntity>(e =>
        {
            e.HasIndex(x => x.Tag);
            e.HasIndex(x => new { x.ToolDefinitionId, x.Tag }).IsUnique();
            e.HasOne(x => x.ToolDefinition)
                .WithMany(x => x.Tags)
                .HasForeignKey(x => x.ToolDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ToolBucketEntity>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<AgentDefinitionEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.IsBuiltIn);
        });

        modelBuilder.Entity<AgentWorkspaceEntity>(e =>
        {
            e.HasIndex(x => new { x.AgentDefinitionId, x.WorkspacePath }).IsUnique();
            e.HasIndex(x => x.WorkspacePath);
            e.HasOne(x => x.AgentDefinition)
                .WithMany(x => x.WorkspaceConfigs)
                .HasForeignKey(x => x.AgentDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgentEventLogEntity>(e =>
        {
            e.HasIndex(x => x.AgentId);
            e.HasIndex(x => x.WorkspacePath);
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => x.EventType);
        });

        modelBuilder.Entity<GraphEntityEntity>(e =>
        {
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.EntityType);
        });

        modelBuilder.Entity<GraphRelationshipEntity>(e =>
        {
            e.HasIndex(x => x.SourceEntityId);
            e.HasIndex(x => x.TargetEntityId);
            e.HasIndex(x => x.RelationshipType);
            e.HasOne(x => x.SourceEntity)
                .WithMany(x => x.SourceRelationships)
                .HasForeignKey(x => x.SourceEntityId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TargetEntity)
                .WithMany(x => x.TargetRelationships)
                .HasForeignKey(x => x.TargetEntityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TodoItemEntity>(e =>
        {
            // TR-MCP-TODO-008: composite PK (WorkspaceId, Id) so the same canonical
            // TODO id may coexist across workspaces. Matches TR-MCP-MT-003 pattern.
            e.HasKey(x => new { x.WorkspaceId, x.Id });
            e.HasIndex(x => x.Section);
            e.HasIndex(x => x.Priority);
            e.HasIndex(x => x.Done);
        });

        modelBuilder.Entity<TodoRequirementLinkEntity>(e =>
        {
            e.HasKey(x => new { x.WorkspaceId, x.TodoId, x.RequirementKind, x.RequirementId });
            e.HasIndex(x => new { x.WorkspaceId, x.RequirementKind, x.RequirementId });
            e.HasOne(x => x.TodoItem)
                .WithMany()
                .HasForeignKey(x => new { x.WorkspaceId, x.TodoId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Requirement)
                .WithMany()
                .HasForeignKey(x => new { x.WorkspaceId, x.RequirementKind, x.RequirementId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TodoAuditHistoryEntity>(e =>
        {
            // TR-MCP-TODO-008: unique monotonic (TodoId, Version) scoped per workspace.
            e.HasIndex(x => new { x.WorkspaceId, x.TodoId, x.Version }).IsUnique();
            e.HasIndex(x => new { x.WorkspaceId, x.TodoId, x.RecordedAtUtc });
            e.HasIndex(x => x.Action);
        });

        modelBuilder.Entity<TodoDocumentMetadataEntity>(e =>
        {
            // TR-MCP-TODO-008: composite PK (WorkspaceId, SingletonId). Check constraint
            // becomes per-workspace (SingletonId = 1 for every workspace's singleton).
            //
            // Singleton pattern: SingletonId is a fixed sentinel (= 1), never auto-assigned.
            // Without ValueGeneratedNever(), SQL Server treats int PKs as IDENTITY and rejects
            // explicit-value inserts (error 544); SQLite silently accepts them so unit tests
            // against SQLite miss this. LegacyTodoSqliteMigrator (TR-MCP-TODO-007) inserts with
            // SingletonId = 1 explicitly, so this is required for cross-provider correctness.
            e.HasKey(x => new { x.WorkspaceId, x.SingletonId });
            e.Property(x => x.SingletonId).ValueGeneratedNever();
            e.ToTable(t => t.HasCheckConstraint(
                "CK_TodoDocumentMetadata_Singleton",
                "\"SingletonId\" = 1"));
        });

        modelBuilder.Entity<RequirementEntity>(e =>
        {
            e.HasKey(x => new { x.WorkspaceId, x.Kind, x.Id });
            e.HasIndex(x => new { x.WorkspaceId, x.Id });
            e.HasIndex(x => x.Kind);
            e.HasIndex(x => new { x.WorkspaceId, x.ScopeStartLayerKey });
            e.HasIndex(x => new { x.WorkspaceId, x.ScopeEndLayerKey });
            e.Property(x => x.Priority).HasDefaultValue("medium");
            e.Property(x => x.Status).HasDefaultValue("pending");
            e.Property(x => x.ScopeStartLayerKey).HasDefaultValue("layer-1");
        });

        modelBuilder.Entity<RequirementScopeLayerEntity>(e =>
        {
            e.HasKey(x => new { x.WorkspaceId, x.Key });
            e.HasIndex(x => new { x.WorkspaceId, x.Order }).IsUnique();
            e.HasIndex(x => new { x.WorkspaceId, x.ScopeEndLayerKey });
        });

        modelBuilder.Entity<RequirementTraceabilityLinkEntity>(e =>
        {
            e.HasKey(x => new { x.WorkspaceId, x.FrId, x.TargetKind, x.TargetId });
            e.HasIndex(x => new { x.WorkspaceId, x.TargetKind, x.TargetId });
            e.Property(x => x.SourceKind).HasDefaultValue("fr");
            e.HasOne(x => x.SourceRequirement)
                .WithMany()
                .HasForeignKey(x => new { x.WorkspaceId, x.SourceKind, x.FrId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TargetRequirement)
                .WithMany()
                .HasForeignKey(x => new { x.WorkspaceId, x.TargetKind, x.TargetId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MemoryEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Category);
            e.HasIndex(x => new { x.Scope, x.WorkspaceId, x.Category });
            e.HasIndex(x => x.UpdatedAtUtc);
            e.HasOne<WorkspaceEntity>()
                .WithMany()
                .HasForeignKey(x => x.WorkspaceId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BrainSlotDefinitionEntity>(e =>
        {
            e.HasKey(x => new { x.WorkspaceId, x.SlotId });
            e.HasIndex(x => new { x.WorkspaceId, x.Role, x.Enabled });
            e.HasIndex(x => new { x.WorkspaceId, x.Role })
                .IsUnique()
                .HasFilter(BrainSlotEnabledUniqueIndexFilter());
            e.HasIndex(x => new { x.WorkspaceId, x.PartyId });
        });

        modelBuilder.Entity<BrainSlotInvocationEntity>(e =>
        {
            e.HasIndex(x => new { x.WorkspaceId, x.SlotId, x.StartedAtUtc });
            e.HasIndex(x => new { x.WorkspaceId, x.TransactionId });
            e.HasOne<BrainSlotDefinitionEntity>()
                .WithMany()
                .HasForeignKey(x => new { x.WorkspaceId, x.SlotId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TriageReportEntity>(e =>
        {
            e.HasIndex(x => new { x.WorkspaceId, x.GroupId });
            e.HasIndex(x => new { x.WorkspaceId, x.Fingerprint });
            e.HasIndex(x => new { x.WorkspaceId, x.IdempotencyKey })
                .IsUnique()
                .HasFilter(TriageNullableUniqueIndexFilter(nameof(TriageReportEntity.IdempotencyKey)));
            e.HasIndex(x => x.CreatedUtc);
        });

        modelBuilder.Entity<TriageGroupEntity>(e =>
        {
            e.HasIndex(x => new { x.WorkspaceId, x.GroupKey }).IsUnique();
            e.HasIndex(x => new { x.WorkspaceId, x.Status, x.QuietDeadlineUtc });
            e.HasIndex(x => x.CreatedTodoId);
        });

        modelBuilder.Entity<TriageResearchRunEntity>(e =>
        {
            e.HasIndex(x => new { x.WorkspaceId, x.GroupId, x.StartedUtc });
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<FederationProxyEntity>(e =>
        {
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.LastHeartbeatUtc);
        });

        modelBuilder.Entity<FederationWorkspaceEntity>(e =>
        {
            e.HasIndex(x => x.GlobalWorkspaceId).IsUnique();
            e.HasIndex(x => new { x.ProxyId, x.WorkspacePath }).IsUnique();
            e.HasIndex(x => x.CanonicalWorkspaceId);
            e.HasIndex(x => x.ProxyId);
            e.HasOne(x => x.CanonicalWorkspace)
                .WithMany()
                .HasForeignKey(x => x.CanonicalWorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<FederationProxyEntity>()
                .WithMany()
                .HasForeignKey(x => x.ProxyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FederationOperationEntity>(e =>
        {
            e.HasIndex(x => new { x.ProxyId, x.Status });
            e.HasIndex(x => x.SourceOperationId);
            e.HasIndex(x => new { x.Domain, x.ResourceId });
            e.HasIndex(x => x.CreatedAtUtc);
            e.HasOne<FederationProxyEntity>()
                .WithMany()
                .HasForeignKey(x => x.ProxyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FederationOutboxEntity>(e =>
        {
            e.HasIndex(x => new { x.ProxyId, x.Sequence });
            e.HasIndex(x => x.OperationId);
            e.HasOne<FederationProxyEntity>()
                .WithMany()
                .HasForeignKey(x => x.ProxyId)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne<FederationOperationEntity>()
                .WithMany()
                .HasForeignKey(x => x.OperationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FederationConflictEntity>(e =>
        {
            e.HasIndex(x => new { x.ProxyId, x.ResolutionStatus });
            e.HasIndex(x => x.OperationId);
            e.HasIndex(x => new { x.Domain, x.ResourceId });
            e.HasOne<FederationOperationEntity>()
                .WithMany()
                .HasForeignKey(x => x.OperationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<FederationProxyEntity>()
                .WithMany()
                .HasForeignKey(x => x.ProxyId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ContextDocumentEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<ContextChunkEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<SessionLogEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        // BUG-SESSIONLOG-WS-001..004: session-log CHILD entities carry no workspace
        // query filter. Children are only reachable through their (filtered) parent
        // session; filtering them independently let stamping drift hide rows from
        // the EF graph, producing duplicate-key inserts on upsert, severed required
        // associations on bare submits, and corrupted turn counts. Direct child-set
        // queries must add an explicit parent-workspace predicate (see
        // AppendProcessingDialogAsync and TodoExecutionService turn lookups).
        modelBuilder.Entity<ToolDefinitionEntity>().HasQueryFilter("Workspace", e => e.WorkspaceId == string.Empty || (!string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId));
        modelBuilder.Entity<ToolDefinitionTagEntity>().HasQueryFilter("Workspace", e => e.WorkspaceId == string.Empty || (!string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId));
        modelBuilder.Entity<ToolBucketEntity>().HasQueryFilter("Workspace", e => e.WorkspaceId == string.Empty || (!string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId));
        modelBuilder.Entity<AgentDefinitionEntity>().HasQueryFilter("Workspace", e => e.WorkspaceId == string.Empty || (!string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId));
        modelBuilder.Entity<AgentWorkspaceEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<AgentEventLogEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<GraphEntityEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<GraphRelationshipEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);

        // TR-MCP-TODO-008: workspace-scoped TODO storage. Same pattern as the
        // other multi-tenant entities: never cross workspaces on reads, updates, deletes.
        modelBuilder.Entity<TodoItemEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<TodoAuditHistoryEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<TodoDocumentMetadataEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<RequirementEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<RequirementScopeLayerEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<RequirementTraceabilityLinkEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<MemoryEntity>().HasQueryFilter("Workspace", e =>
            e.Scope == MemoryEntity.GlobalScope
            || (!string.IsNullOrEmpty(_workspaceId)
                && e.Scope == MemoryEntity.WorkspaceScope
                && e.WorkspaceId == _workspaceId));
        modelBuilder.Entity<TriageReportEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<TriageGroupEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<TriageResearchRunEntity>().HasQueryFilter("Workspace", e => !string.IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId);
        // TR-MCP-QUAD-001: the QuadBrain subsystem is GLOBAL (one quad shared by every workspace and session).
        // Brain-slot definitions and their invocation audit rows are stored under the global workspace
        // (WorkspaceId == "") and visible in every workspace context; the per-session dimension is carried by the
        // invocation's TurnId/metadata, not the workspace. Invocations FK to definitions on (WorkspaceId, SlotId),
        // so both must share the same (global) workspace id.
        modelBuilder.Entity<BrainSlotDefinitionEntity>().HasQueryFilter("Workspace", e => e.WorkspaceId == string.Empty);
        modelBuilder.Entity<BrainSlotInvocationEntity>().HasQueryFilter("Workspace", e => e.WorkspaceId == string.Empty);

        modelBuilder.Entity<ContextDocumentEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<ContextChunkEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<SessionLogEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<SessionLogTurnEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<SessionLogActionEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<SessionLogTurnTagEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<SessionLogTurnContextEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<SessionLogProcessingDialogEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<ToolDefinitionEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<ToolDefinitionTagEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<ToolBucketEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<AgentDefinitionEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<AgentWorkspaceEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<AgentEventLogEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<GraphEntityEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<GraphRelationshipEntity>().HasIndex(e => e.WorkspaceId);

        // TR-MCP-TODO-008: WorkspaceId indexes on Todo entities. The composite PK
        // on TodoItemEntity / TodoDocumentMetadataEntity already indexes WorkspaceId
        // as leading key; we add a standalone index only on TodoAuditHistoryEntity
        // because its PK is AuditId (identity) and the unique (WorkspaceId, TodoId,
        // Version) index already covers the common filter paths.
        modelBuilder.Entity<TodoAuditHistoryEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<RequirementEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<RequirementScopeLayerEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<RequirementTraceabilityLinkEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<MemoryEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<BrainSlotInvocationEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<TriageReportEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<TriageGroupEntity>().HasIndex(e => e.WorkspaceId);
        modelBuilder.Entity<TriageResearchRunEntity>().HasIndex(e => e.WorkspaceId);

        ApplyDbFkConventions(modelBuilder);
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareDbFkChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        PrepareDbFkChanges();
        return base.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyDbFkConventions(ModelBuilder modelBuilder)
    {
        ApplyWorkspaceForeignKeys(modelBuilder);
        ApplySoftDeleteMetadata(modelBuilder);
        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    private string BrainSlotEnabledUniqueIndexFilter()
    {
        var providerName = Database.ProviderName ?? string.Empty;
        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            return "\"Enabled\" = TRUE AND \"IsDeleted\" = FALSE";
        if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            return "[Enabled] = 1 AND [IsDeleted] = 0";
        return "\"Enabled\" = 1 AND \"IsDeleted\" = 0";
    }

    private string TriageNullableUniqueIndexFilter(string propertyName)
    {
        var providerName = Database.ProviderName ?? string.Empty;
        if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            return $"[{propertyName}] IS NOT NULL";
        return $"\"{propertyName}\" IS NOT NULL";
    }

    private static void ApplyWorkspaceForeignKeys(ModelBuilder modelBuilder)
    {
        var workspaceClrType = typeof(WorkspaceEntity);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToArray())
        {
            if (entityType.ClrType == workspaceClrType)
                continue;

            if (entityType.ClrType == typeof(MemoryEntity))
                continue;

            if (entityType.FindProperty(nameof(WorkspaceEntity.WorkspaceId)) is null)
                continue;

            modelBuilder.Entity(entityType.ClrType)
                .HasOne(workspaceClrType)
                .WithMany()
                .HasForeignKey(nameof(WorkspaceEntity.WorkspaceId))
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    private static void ApplySoftDeleteMetadata(ModelBuilder modelBuilder)
    {
        foreach (var entityType in DurableEntityTypes(modelBuilder).ToArray())
        {
            var builder = modelBuilder.Entity(entityType.ClrType);
            builder.Property<bool>("IsDeleted").HasDefaultValue(false);
            builder.Property<DateTimeOffset?>("DeletedAtUtc");
            builder.Property<string?>("DeletedBy").HasMaxLength(256);
            builder.Property<string?>("DeleteReason").HasMaxLength(1024);
        }
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in DurableEntityTypes(modelBuilder).ToArray())
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var softDeleteFilter = Expression.Equal(
                Expression.Call(
                    typeof(EF),
                    nameof(EF.Property),
                    [typeof(bool)],
                    parameter,
                    Expression.Constant("IsDeleted")),
                Expression.Constant(false));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter("SoftDelete", Expression.Lambda(softDeleteFilter, parameter));
        }
    }

    private static IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType> DurableEntityTypes(ModelBuilder modelBuilder)
    {
        return modelBuilder.Model.GetEntityTypes()
            .Where(e => e.ClrType != typeof(DataAuditLogEntity))
            .Where(e => e.GetTableName() is not null)
            .Where(e => e.ClrType.Name.EndsWith("Entity", StringComparison.Ordinal));
    }

    private void PrepareDbFkChanges()
    {
        StampWorkspaceId();
        ApplySoftDeletes();
        BlockPhysicalDeletes();
        EnsureWorkspaceRows();
        AppendAuditRows();
        EnsureWorkspaceRows();
        SanitizeStrings();
    }

    private void ApplySoftDeletes()
    {
        var now = DateTimeOffset.UtcNow;
        var softDeletedEntries = ChangeTracker.Entries()
                     .Where(e => e.State == EntityState.Deleted && IsDurableAuditableEntry(e))
                     .ToArray();

        foreach (var entry in softDeletedEntries)
        {
            entry.State = EntityState.Modified;
            SetShadowValue(entry, "IsDeleted", true);
            SetShadowValue(entry, "DeletedAtUtc", now);
            SetShadowValue(entry, "DeletedBy", "McpDbContext");
            SetShadowValue(entry, "DeleteReason", "soft_delete");
        }

        SoftDeleteGraphRelationshipsForDeletedEntities(softDeletedEntries, now);
    }

    private void BlockPhysicalDeletes()
    {
        var physicalDeletes = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Deleted && IsPersistentEntityEntry(e))
            .Select(e => e.Metadata.ClrType.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (physicalDeletes.Length == 0)
            return;

        throw new InvalidOperationException(
            "Physical deletes are blocked for persistent MCP data. Use soft-delete metadata instead. Entities: "
            + string.Join(", ", physicalDeletes));
    }

    private void SoftDeleteGraphRelationshipsForDeletedEntities(
        IReadOnlyCollection<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> softDeletedEntries,
        DateTimeOffset now)
    {
        var graphEntityIds = softDeletedEntries
            .Select(e => e.Entity)
            .OfType<GraphEntityEntity>()
            .Select(e => e.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (graphEntityIds.Length == 0)
            return;

        var relationships = GraphRelationships
            .IgnoreQueryFilters()
            .Where(r => graphEntityIds.Contains(r.SourceEntityId) || graphEntityIds.Contains(r.TargetEntityId))
            .ToArray();

        foreach (var relationship in relationships)
        {
            var entry = Entry(relationship);
            if (TryGetSoftDeleteValue(entry))
                continue;

            entry.State = EntityState.Modified;
            SetShadowValue(entry, "IsDeleted", true);
            SetShadowValue(entry, "DeletedAtUtc", now);
            SetShadowValue(entry, "DeletedBy", "McpDbContext");
            SetShadowValue(entry, "DeleteReason", "parent_graph_entity_soft_delete");
        }
    }

    private void EnsureWorkspaceRows()
    {
        var workspaceIds = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity is not WorkspaceEntity)
            .Select(e => TryGetWorkspaceId(e))
            .Where(id => id is not null)
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var workspaceId in workspaceIds)
        {
            if (Workspaces.Local.Any(w => string.Equals(w.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase)))
                continue;

            try
            {
                if (Workspaces.IgnoreQueryFilters().Any(w => w.WorkspaceId == workspaceId))
                    continue;
            }
            catch (DbException ex) when (IsWorkspaceBootstrapSchemaUnavailable(ex))
            {
                continue;
            }

            Workspaces.Add(CreateImplicitWorkspace(workspaceId));
        }
    }

    private static bool IsWorkspaceBootstrapSchemaUnavailable(DbException exception)
    {
        return exception.Message.Contains("Workspaces", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("WorkspaceId", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("no such column", StringComparison.OrdinalIgnoreCase);
    }

    private static WorkspaceEntity CreateImplicitWorkspace(string workspaceId)
    {
        var now = DateTimeOffset.UtcNow;
        var path = workspaceId;
        var name = string.IsNullOrWhiteSpace(path)
            ? "global"
            : Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        return new WorkspaceEntity
        {
            WorkspaceId = workspaceId,
            WorkspacePath = path,
            Name = string.IsNullOrWhiteSpace(name) ? "workspace" : name,
            TodoPath = "docs/todo.yaml",
            IsEnabled = true,
            DateTimeCreated = now,
            DateTimeModified = now,
        };
    }

    private void AppendAuditRows()
    {
        if (!IsAuditTableAvailable())
            return;

        var now = DateTimeOffset.UtcNow;
        var auditRows = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(IsDurableAuditableEntry)
            .Select(e => CreateAuditRow(e, now))
            .Where(e => e is not null)
            .Select(e => e!)
            .ToArray();

        if (auditRows.Length == 0)
            return;

        DataAuditLogs.AddRange(auditRows);
    }

    private bool IsAuditTableAvailable()
    {
        try
        {
            _ = DataAuditLogs.IgnoreQueryFilters().Any();
            return true;
        }
        catch (DbException ex) when (IsWorkspaceBootstrapSchemaUnavailable(ex))
        {
            return false;
        }
    }

    private static DataAuditLogEntity? CreateAuditRow(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, DateTimeOffset now)
    {
        var action = entry.State switch
        {
            EntityState.Added => "create",
            EntityState.Deleted => "delete",
            EntityState.Modified when TryGetSoftDeleteValue(entry) => "delete",
            EntityState.Modified => "update",
            _ => null,
        };

        if (action is null)
            return null;

        return new DataAuditLogEntity
        {
            AuditId = Guid.NewGuid().ToString("N"),
            WorkspaceId = TryGetWorkspaceId(entry) ?? string.Empty,
            EntityKind = entry.Metadata.ClrType.Name,
            EntityKey = BuildEntityKey(entry),
            Action = action,
            Actor = "McpDbContext",
            SourceType = "McpDbContext",
            OccurredAtUtc = now,
            PreviousSnapshotJson = entry.State == EntityState.Added ? null : SnapshotJson(entry, originalValues: true),
            CurrentSnapshotJson = entry.State == EntityState.Deleted ? null : SnapshotJson(entry, originalValues: false),
        };
    }

    private static string BuildEntityKey(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        var properties = key?.Properties.Count > 0 ? key.Properties : entry.Properties.Select(p => p.Metadata).ToList();
        return string.Join("|", properties.Select(p =>
        {
            var value = entry.Property(p.Name).CurrentValue ?? entry.Property(p.Name).OriginalValue;
            return $"{p.Name}={value}";
        }));
    }

    private static string SnapshotJson(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, bool originalValues)
    {
        var snapshot = entry.Properties
            .OrderBy(p => p.Metadata.Name, StringComparer.Ordinal)
            .ToDictionary(
                p => p.Metadata.Name,
                p => SanitizeAuditValue(p.Metadata.Name, originalValues ? p.OriginalValue : p.CurrentValue));

        return JsonSerializer.Serialize(snapshot);
    }

    private static object? SanitizeAuditValue(string name, object? value)
    {
        if (value is null)
            return null;

        if (name.Contains("Token", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("ApiKey", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Key", StringComparison.OrdinalIgnoreCase))
        {
            return "[redacted]";
        }

        return value;
    }

    private static string? TryGetWorkspaceId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(WorkspaceEntity.WorkspaceId));
        return property?.CurrentValue as string ?? property?.OriginalValue as string;
    }

    private static bool TryGetSoftDeleteValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "IsDeleted");
        return property?.CurrentValue is true;
    }

    private static bool IsDurableAuditableEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        return entry.Entity is not DataAuditLogEntity
            && entry.Entity is not WorkspaceEntity { WorkspaceId: "" }
            && entry.Metadata.GetTableName() is not null
            && entry.Metadata.ClrType.Name.EndsWith("Entity", StringComparison.Ordinal)
            && entry.Properties.Any(p => p.Metadata.Name == "IsDeleted");
    }

    private static bool IsPersistentEntityEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        return entry.Metadata.GetTableName() is not null
            && entry.Metadata.ClrType.Name.EndsWith("Entity", StringComparison.Ordinal);
    }

    private static void SetShadowValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName, object? value)
    {
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        if (property is not null)
            property.CurrentValue = value;
    }

    private void StampWorkspaceId()
    {
        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => e.State == EntityState.Added))
        {
            var prop = entry.Properties
                .FirstOrDefault(p => p.Metadata.Name == nameof(ContextDocumentEntity.WorkspaceId));
            if (prop is not null && prop.CurrentValue is string val && val.Length == 0)
            {
                var resolvedWorkspaceId = ResolveWorkspaceIdForAddedEntity(entry.Entity);
                if (resolvedWorkspaceId is not null)
                    prop.CurrentValue = resolvedWorkspaceId;
            }
        }
    }

    private string? ResolveWorkspaceIdForAddedEntity(object entity)
    {
        return entity switch
        {
            ToolDefinitionEntity toolDefinition => toolDefinition.WorkspacePath ?? string.Empty,
            ToolDefinitionTagEntity toolDefinitionTag => ResolveToolDefinitionTagWorkspaceId(toolDefinitionTag),
            // TR-MCP-QUAD-001: QuadBrain brain-slot definitions and invocation rows are global; always stamp "".
            BrainSlotDefinitionEntity => string.Empty,
            BrainSlotInvocationEntity => string.Empty,
            // BUG-SESSIONLOG-WS-002: session-log children always inherit the parent
            // graph's stamp so a single session never holds mixed WorkspaceIds.
            SessionLogTurnEntity turn => FirstNonEmpty(turn.SessionLog?.WorkspaceId),
            SessionLogActionEntity child => FirstNonEmpty(child.SessionLogTurn?.WorkspaceId, child.SessionLogTurn?.SessionLog?.WorkspaceId),
            SessionLogTurnTagEntity child => FirstNonEmpty(child.SessionLogTurn?.WorkspaceId, child.SessionLogTurn?.SessionLog?.WorkspaceId),
            SessionLogTurnContextEntity child => FirstNonEmpty(child.SessionLogTurn?.WorkspaceId, child.SessionLogTurn?.SessionLog?.WorkspaceId),
            SessionLogProcessingDialogEntity child => FirstNonEmpty(child.SessionLogTurn?.WorkspaceId, child.SessionLogTurn?.SessionLog?.WorkspaceId),
            SessionLogCommitEntity child => FirstNonEmpty(child.SessionLogTurn?.WorkspaceId, child.SessionLogTurn?.SessionLog?.WorkspaceId),
            SessionLogTurnStringListEntity child => FirstNonEmpty(child.SessionLogTurn?.WorkspaceId, child.SessionLogTurn?.SessionLog?.WorkspaceId),
            _ when _workspaceId.Length > 0 => _workspaceId,
            _ => null,
        };

        string? FirstNonEmpty(params string?[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate))
                    return candidate;
            }

            return _workspaceId.Length > 0 ? _workspaceId : null;
        }
    }

    private string ResolveToolDefinitionTagWorkspaceId(ToolDefinitionTagEntity toolDefinitionTag)
    {
        if (toolDefinitionTag.ToolDefinition is not null)
            return toolDefinitionTag.ToolDefinition.WorkspaceId;

        return _workspaceId;
    }

    private void SanitizeStrings()
    {
        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.ClrType != typeof(string)) continue;
                if (prop.CurrentValue is not string s || s.Length == 0) continue;

                var sanitized = LineSanitizer.Sanitize(s);
                if (!ReferenceEquals(sanitized, s))
                    prop.CurrentValue = sanitized;
            }
        }
    }
}
