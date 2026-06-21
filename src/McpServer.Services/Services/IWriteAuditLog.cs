namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-CORE-013: Audit log for repo write operations.
/// FR-SUPPORT-010: Enforce audit trail for repo.write.
/// </summary>
public interface IWriteAuditLog
{
    /// <summary>Records a write (path, timestamp, optional run id).</summary>
    /// <param name="relativePath">Relative path of the written file.</param>
    /// <param name="at">UTC timestamp of the write.</param>
    void RecordWrite(string relativePath, DateTime at);

    /// <summary>Returns the last N write entries (for status/debug).</summary>
    /// <param name="count">Maximum number of recent entries to return.</param>
    /// <returns>A read-only list of the most recent write audit entries.</returns>
    IReadOnlyList<WriteAuditEntry> GetRecent(int count = 50);
}

/// <summary>TR-PLANNED-CORE-013: Single audit entry.</summary>
/// <param name="RelativePath">Relative path of the written file.</param>
/// <param name="At">UTC timestamp of the write.</param>
public sealed record WriteAuditEntry(string RelativePath, DateTime At);
