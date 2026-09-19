using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

public sealed partial class MemoryConsolidateOperations
{
    private int AppendLineage(MemoryEntity survivor, MemoryEntity victim, int nextVersionNumber)
    {
        _db.MemoryVersions.Add(new MemoryVersionEntity
        {
            MemoryId = survivor.Id,
            VersionNumber = nextVersionNumber,
            Title = survivor.Title,
            Content = survivor.Content ?? survivor.Text ?? string.Empty,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        if (!_db.MemoryEdges.Local.Any(e =>
                e.FromMemoryId == survivor.Id && e.ToMemoryId == victim.Id && e.EdgeType == "merged-from"))
        {
            _db.MemoryEdges.Add(new MemoryEdgeEntity
            {
                FromMemoryId = survivor.Id,
                ToMemoryId = victim.Id,
                EdgeType = "merged-from",
                Weight = 1.0,
            });
        }

        return nextVersionNumber + 1;
    }

    private async Task RewireEdgesAsync(string survivorId, string victimId, CancellationToken cancellationToken)
    {
        var edges = await _db.MemoryEdges
            .Where(e => e.FromMemoryId == victimId || e.ToMemoryId == victimId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var edge in edges)
        {
            if (edge.FromMemoryId == victimId)
                edge.FromMemoryId = survivorId;
            if (edge.ToMemoryId == victimId)
                edge.ToMemoryId = survivorId;
            if (edge.FromMemoryId == edge.ToMemoryId)
                _db.MemoryEdges.Remove(edge);
        }
    }

    private static bool TryAcquireLock(string workspace, bool forceOverlap, out bool lockHeld)
    {
        lockHeld = false;
        var now = DateTimeOffset.UtcNow;
        var state = Locks.GetOrAdd(workspace, _ => new WorkspaceLockState());
        lock (state.Gate)
        {
            if (state.Held)
            {
                if (forceOverlap && now - state.AcquiredAtUtc > LockTtl)
                {
                    state.Held = true;
                    state.AcquiredAtUtc = now;
                    return true;
                }

                lockHeld = true;
                return false;
            }

            state.Held = true;
            state.AcquiredAtUtc = now;
            return true;
        }
    }

    private static void ReleaseLock(string workspace)
    {
        if (!Locks.TryGetValue(workspace, out var state))
            return;
        lock (state.Gate)
            state.Held = false;
    }

    private sealed class WorkspaceLockState
    {
        public object Gate { get; } = new();
        public bool Held;
        public DateTimeOffset AcquiredAtUtc;
    }
}
