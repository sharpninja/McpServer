// SPDX-License-Identifier: Apache-2.0
using Microsoft.EntityFrameworkCore;
using QBrainAi.Support.Mcp.Storage.Entities;

namespace QBrainAi.Support.Mcp.Storage;

/// <summary>TR-QB-AUDIT-001: Event evidence shares the existing workspace-scoped database and transaction.</summary>
public sealed partial class McpDbContext
{
    /// <summary>Immutable captured events and retained terminal rejections.</summary>
    public DbSet<SessionAuditEventEntity> SessionAuditEvents => Set<SessionAuditEventEntity>();

    private void ConfigureSessionAuditEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionAuditEventEntity>(entity =>
        {
            entity.HasIndex(row => row.EventId).IsUnique();
            entity.HasIndex(row => new { row.SessionLogTurnId, row.PositionKey }).IsUnique()
                .HasFilter((Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true) ? "[PositionKey] IS NOT NULL" : "\"PositionKey\" IS NOT NULL");
            entity.HasOne<SessionLogTurnEntity>().WithMany().HasForeignKey(row => row.SessionLogTurnId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter("Workspace", row => !string.IsNullOrEmpty(_workspaceId) && row.WorkspaceId == _workspaceId);
        });
    }
}
