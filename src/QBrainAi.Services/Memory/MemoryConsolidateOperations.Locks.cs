using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace QBrainAi.Support.Mcp.Services;

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

    /// <summary>
    /// TR-MCP-MEMORY-JOBS-002: takes the workspace's consolidation lock at <paramref name="now"/>. A free lock is taken; a
    /// held lock is taken over only by a forced overlap when it was acquired more than the 30 second TTL before
    /// <paramref name="now"/>; otherwise the caller is told the lock is held.
    /// </summary>
    /// <param name="workspace">The canonical workspace key.</param>
    /// <param name="forceOverlap">Whether a held lock past its TTL may be taken over.</param>
    /// <param name="now">The time of the attempt, from the operations' clock.</param>
    /// <param name="lockHeld">True when the attempt failed because the lock is held.</param>
    /// <returns>True when the lock was taken.</returns>
    /// <remarks>2026-11-05, TR-MCP-MEMORY-JOBS-002; TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): takes the time of the attempt from its caller instead of reading DateTimeOffset.UtcNow, and is internal so a unit test binds the TTL decision to scripted times (hostile validation round 12 finding R12-F2, sibling S37; mock gate ug-r13z4-mockgate-b-1).</remarks>
    internal static bool TryAcquireLock(string workspace, bool forceOverlap, DateTimeOffset now, out bool lockHeld)
    {
        lockHeld = false;
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

    /// <summary>TR-MCP-MEMORY-JOBS-002: releases the workspace's consolidation lock.</summary>
    /// <param name="workspace">The canonical workspace key.</param>
    /// <remarks>2026-11-05, TR-MCP-MEMORY-JOBS-002; TR-MCP-USECASE-018 AC-006 (appended): internal so a unit test that holds the lock at a scripted time releases it (hostile validation round 12 finding R12-F2, sibling S37).</remarks>
    internal static void ReleaseLock(string workspace)
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
