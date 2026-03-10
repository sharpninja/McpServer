using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Storage;

/// <summary>
/// TR-PLANNED-013: EF Core DbContext for MCP metadata and chunks.
/// FR-SUPPORT-010: SQLite storage for local MCP server.
/// TR-MCP-MT-003: Global query filter on WorkspaceId for multi-tenant data isolation.
/// </summary>
public sealed class McpDbContext : DbContext
{
    private string _workspaceId;

    /// <summary>TR-PLANNED-013: Constructor for DI with workspace context.</summary>
    public McpDbContext(DbContextOptions<McpDbContext> options, WorkspaceContext? workspaceContext = null)
        : base(options)
    {
        _workspaceId = workspaceContext?.WorkspacePath ?? string.Empty;
    }

    /// <summary>TR-MCP-MT-003: Gets the current workspace discriminator applied to this context instance.</summary>
    public string CurrentWorkspaceId => _workspaceId;

    /// <summary>TR-MCP-MT-001: Overrides the workspace ID for this context instance (e.g. from an MCP tool parameter).</summary>
    public void OverrideWorkspaceId(string workspaceId) => _workspaceId = workspaceId;

    /// <summary>TR-PLANNED-013: Indexed documents.</summary>
    public DbSet<ContextDocumentEntity> Documents => Set<ContextDocumentEntity>();

    /// <summary>TR-PLANNED-013: Indexed chunks.</summary>
    public DbSet<ContextChunkEntity> Chunks => Set<ContextChunkEntity>();

    /// <summary>TR-PLANNED-013: Session logs (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogEntity> SessionLogs => Set<SessionLogEntity>();

    /// <summary>TR-PLANNED-013: Session log entries (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogTurnEntity> SessionLogTurns => Set<SessionLogTurnEntity>();

    /// <summary>TR-PLANNED-013: Session log entry actions (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogActionEntity> SessionLogActions => Set<SessionLogActionEntity>();

    /// <summary>TR-PLANNED-013: Session log entry tags (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogTurnTagEntity> SessionLogTurnTags => Set<SessionLogTurnTagEntity>();

    /// <summary>TR-PLANNED-013: Session log entry context items (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogTurnContextEntity> SessionLogTurnContexts => Set<SessionLogTurnContextEntity>();

    /// <summary>TR-PLANNED-013: Session log entry processing dialog items (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogProcessingDialogEntity> SessionLogProcessingDialogs => Set<SessionLogProcessingDialogEntity>();

    /// <summary>TR-PLANNED-013: Session log entry commits.</summary>
    public DbSet<SessionLogCommitEntity> SessionLogCommits => Set<SessionLogCommitEntity>();

    /// <summary>TR-PLANNED-013: Session log entry string-list items (design decisions, requirements, files modified, blockers).</summary>
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

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
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
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionLogEntity>(e =>
        {
            e.HasIndex(x => new { x.SourceType, x.SessionId }).IsUnique();
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
                .WithMany(x => x.Entries)
                .HasForeignKey(x => x.SessionLogId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionLogActionEntity>(e =>
        {
            e.HasOne(x => x.SessionLogTurn)
                .WithMany(x => x.Actions)
                .HasForeignKey(x => x.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionLogTurnTagEntity>(e =>
        {
            e.HasOne(x => x.SessionLogTurn)
                .WithMany(x => x.Tags)
                .HasForeignKey(x => x.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionLogTurnContextEntity>(e =>
        {
            e.HasOne(x => x.SessionLogTurn)
                .WithMany(x => x.ContextItems)
                .HasForeignKey(x => x.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionLogProcessingDialogEntity>(e =>
        {
            e.HasOne(x => x.SessionLogTurn)
                .WithMany(x => x.ProcessingDialog)
                .HasForeignKey(x => x.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionLogCommitEntity>(e =>
        {
            e.HasOne(x => x.SessionLogTurn)
                .WithMany(x => x.Commits)
                .HasForeignKey(x => x.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionLogTurnStringListEntity>(e =>
        {
            e.HasOne(x => x.SessionLogTurn)
                .WithMany(x => x.StringListItems)
                .HasForeignKey(x => x.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Cascade);
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
                .OnDelete(DeleteBehavior.Cascade);
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
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentEventLogEntity>(e =>
        {
            e.HasIndex(x => x.AgentId);
            e.HasIndex(x => x.WorkspacePath);
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => x.EventType);
        });

        modelBuilder.Entity<ContextDocumentEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<ContextChunkEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<SessionLogEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<SessionLogTurnEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<SessionLogActionEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<SessionLogTurnTagEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<SessionLogTurnContextEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<SessionLogProcessingDialogEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<SessionLogCommitEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<SessionLogTurnStringListEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<ToolDefinitionEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == string.Empty || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<ToolDefinitionTagEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == string.Empty || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<ToolBucketEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<AgentDefinitionEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<AgentWorkspaceEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);
        modelBuilder.Entity<AgentEventLogEntity>().HasQueryFilter(e => _workspaceId == "" || e.WorkspaceId == _workspaceId);

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
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampWorkspaceId();
        SanitizeStrings();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampWorkspaceId();
        SanitizeStrings();
        return base.SaveChangesAsync(cancellationToken);
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
            _ when _workspaceId.Length > 0 => _workspaceId,
            _ => null,
        };
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
