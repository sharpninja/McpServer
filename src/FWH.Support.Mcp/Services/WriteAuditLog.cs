using System.Collections.Concurrent;

namespace FWH.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: In-memory audit log for repo writes (last N entries).
/// </summary>
public sealed class WriteAuditLog : IWriteAuditLog
{
    private const int DefaultCapacity = 200;
    private readonly ConcurrentQueue<WriteAuditEntry> _entries = new();
    private readonly int _maxEntries;

    /// <summary>Initializes a new instance of the <see cref="WriteAuditLog"/> class.</summary>
    /// <param name="maxEntries">Maximum entries to retain in memory.</param>
    public WriteAuditLog(int maxEntries = DefaultCapacity)
    {
        _maxEntries = maxEntries > 0 ? maxEntries : DefaultCapacity;
    }

    /// <inheritdoc />
    public void RecordWrite(string relativePath, DateTime at)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        _entries.Enqueue(new WriteAuditEntry(relativePath, at));
        while (_entries.Count > _maxEntries && _entries.TryDequeue(out _)) { }
    }

    /// <inheritdoc />
    public IReadOnlyList<WriteAuditEntry> GetRecent(int count = 50)
    {
        var list = _entries.ToArray();
        if (list.Length <= count) return list;
        return list.AsSpan(list.Length - count).ToArray();
    }
}
