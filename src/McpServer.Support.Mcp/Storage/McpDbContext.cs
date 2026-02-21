using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Storage;

/// <summary>
/// TR-PLANNED-013: EF Core DbContext for MCP metadata and chunks.
/// FR-SUPPORT-010: SQLite storage for local MCP server.
/// </summary>
public sealed class McpDbContext : DbContext
{
    /// <summary>TR-PLANNED-013: Constructor for DI.</summary>
    public McpDbContext(DbContextOptions<McpDbContext> options)
        : base(options)
    {
    }

    /// <summary>TR-PLANNED-013: Indexed documents.</summary>
    public DbSet<ContextDocumentEntity> Documents => Set<ContextDocumentEntity>();

    /// <summary>TR-PLANNED-013: Indexed chunks.</summary>
    public DbSet<ContextChunkEntity> Chunks => Set<ContextChunkEntity>();

    /// <summary>TR-PLANNED-013: Session logs (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogEntity> SessionLogs => Set<SessionLogEntity>();

    /// <summary>TR-PLANNED-013: Session log entries (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogEntryEntity> SessionLogEntries => Set<SessionLogEntryEntity>();

    /// <summary>TR-PLANNED-013: Session log entry actions (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogActionEntity> SessionLogActions => Set<SessionLogActionEntity>();

    /// <summary>TR-PLANNED-013: Session log entry tags (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogEntryTagEntity> SessionLogEntryTags => Set<SessionLogEntryTagEntity>();

    /// <summary>TR-PLANNED-013: Session log entry context items (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogEntryContextEntity> SessionLogEntryContexts => Set<SessionLogEntryContextEntity>();

    /// <summary>TR-PLANNED-013: Session log entry processing dialog items (MVP-SUPPORT-011).</summary>
    public DbSet<SessionLogProcessingDialogEntity> SessionLogProcessingDialogs => Set<SessionLogProcessingDialogEntity>();

    /// <summary>Registered workspaces for hosted MCP instances.</summary>
    public DbSet<WorkspaceEntity> Workspaces => Set<WorkspaceEntity>();

    /// <summary>Tool definitions discoverable by keyword search.</summary>
    public DbSet<ToolDefinitionEntity> ToolDefinitions => Set<ToolDefinitionEntity>();

    /// <summary>Keyword tags for tool definitions.</summary>
    public DbSet<ToolDefinitionTagEntity> ToolDefinitionTags => Set<ToolDefinitionTagEntity>();

    /// <summary>Tool bucket repositories (GitHub-backed manifest sources).</summary>
    public DbSet<ToolBucketEntity> ToolBuckets => Set<ToolBucketEntity>();

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

        // MVP-SUPPORT-011: Session log 4NF schema
        modelBuilder.Entity<SessionLogEntity>(e =>
        {
            e.HasIndex(x => new { x.SourceType, x.SessionId }).IsUnique();
            e.HasIndex(x => x.SourceType);
            e.HasIndex(x => x.Started);
            e.HasIndex(x => x.LastUpdated);
        });

        modelBuilder.Entity<SessionLogEntryEntity>(e =>
        {
            e.HasIndex(x => new { x.SessionLogId, x.RequestId }).IsUnique();
            e.HasOne(x => x.SessionLog)
                .WithMany(x => x.Entries)
                .HasForeignKey(x => x.SessionLogId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionLogActionEntity>(e =>
        {
            e.HasOne(x => x.SessionLogEntry)
                .WithMany(x => x.Actions)
                .HasForeignKey(x => x.SessionLogEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionLogEntryTagEntity>(e =>
        {
            e.HasOne(x => x.SessionLogEntry)
                .WithMany(x => x.Tags)
                .HasForeignKey(x => x.SessionLogEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionLogEntryContextEntity>(e =>
        {
            e.HasOne(x => x.SessionLogEntry)
                .WithMany(x => x.ContextItems)
                .HasForeignKey(x => x.SessionLogEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionLogProcessingDialogEntity>(e =>
        {
            e.HasOne(x => x.SessionLogEntry)
                .WithMany(x => x.ProcessingDialog)
                .HasForeignKey(x => x.SessionLogEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkspaceEntity>(e =>
        {
            e.HasIndex(x => x.WorkspacePort).IsUnique();
        });

        modelBuilder.Entity<ToolDefinitionEntity>(e =>
        {
            e.HasIndex(x => new { x.Name, x.WorkspacePath }).IsUnique();
            e.HasIndex(x => x.WorkspacePath);
            e.HasOne(x => x.Workspace)
                .WithMany()
                .HasForeignKey(x => x.WorkspacePath)
                .OnDelete(DeleteBehavior.Cascade);
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
    }
}
