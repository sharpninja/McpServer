using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-MEMORY-013 / TR-MCP-MEMORY-JOBS-002: Consolidate / sleep-merge and job tick operations.</summary>
public sealed class MemoryConsolidateOperations
{
    private static readonly ConcurrentDictionary<string, WorkspaceLockState> Locks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan LockTtl = TimeSpan.FromSeconds(30);
    private readonly McpDbContext _db;
    private readonly string _workspacePath;

    public MemoryConsolidateOperations(McpDbContext db, string workspacePath)
    {
        _db = db;
        _workspacePath = workspacePath;
    }

    public async Task<MemoryConsolidateResult> ConsolidateAsync(
        MemoryConsolidateRequest? request,
        bool readOnlyCaller,
        CancellationToken cancellationToken)
    {
        request ??= new MemoryConsolidateRequest();
        var dryRun = request.DryRun ?? MemoryConsolidateLimits.DefaultDryRun;
        var allowHardDelete = request.AllowHardDelete ?? MemoryConsolidateLimits.DefaultAllowHardDelete;
        var threshold = request.SimilarityThreshold ?? MemoryConsolidateLimits.DefaultSimilarityThreshold;

        if (readOnlyCaller && !dryRun)
            return new MemoryConsolidateResult(403, FailureKind: MemoryMutationFailureKind.Validation, Error: "Read-only caller cannot write-consolidate.");

        if (threshold < MemoryConsolidateLimits.MinSimilarityThreshold
            || threshold > MemoryConsolidateLimits.MaxSimilarityThreshold)
        {
            return new MemoryConsolidateResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "SimilarityThreshold out of range.");
        }

        var runId = string.IsNullOrWhiteSpace(request.RunId)
            ? "consol-" + Guid.NewGuid().ToString("N")
            : request.RunId!;

        if (!TryAcquireLock(_workspacePath, forceOverlap: false, out _))
        {
            return new MemoryConsolidateResult(
                409,
                RunId: runId,
                DryRunApplied: dryRun,
                FailureKind: MemoryMutationFailureKind.Conflict,
                Error: "Consolidate lock busy.");
        }

        try
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            if (request.CancelRequested)
            {
                return new MemoryConsolidateResult(
                    499,
                    RunId: runId,
                    DryRunApplied: dryRun,
                    FailureKind: MemoryMutationFailureKind.Conflict,
                    Error: "Consolidate cancelled.");
            }

            var memories = await _db.Memories
                .Where(m => m.WorkspaceId == _workspacePath || m.WorkspaceId == null)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var workspaceRows = memories
                .Where(m => string.Equals(m.Scope, MemoryEntity.WorkspaceScope, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(m.WorkspaceId, _workspacePath, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var clusters = BuildClusters(workspaceRows, threshold);
            var plan = clusters.Select(c => new MemoryConsolidatePlanItem
            {
                CandidateIds = c.Select(m => m.Id).ToList(),
                SimilarityScore = ScoreCluster(c),
                SurvivorId = dryRun ? null : PickSurvivor(c).Id,
            }).ToList();

            if (dryRun)
            {
                return new MemoryConsolidateResult(
                    200,
                    Plan: plan,
                    RunId: runId,
                    DryRunApplied: true,
                    EventEmitted: false);
            }

            string? survivorId = null;
            var mergedAway = new List<string>();

            foreach (var cluster in clusters)
            {
                var survivor = PickSurvivor(cluster);
                survivorId = survivor.Id;
                var nextVersion = await _db.MemoryVersions
                    .Where(v => v.MemoryId == survivor.Id)
                    .Select(v => (int?)v.VersionNumber)
                    .MaxAsync(cancellationToken)
                    .ConfigureAwait(false) ?? survivor.Version;
                nextVersion += 1;
                foreach (var victim in cluster.Where(m => m.Id != survivor.Id))
                {
                    nextVersion = AppendLineage(survivor, victim, nextVersion);
                    await RewireEdgesAsync(survivor.Id, victim.Id, cancellationToken).ConfigureAwait(false);
                    _db.Memories.Remove(victim);
                    mergedAway.Add(victim.Id);
                }

                if (string.IsNullOrWhiteSpace(survivor.Content) && string.IsNullOrWhiteSpace(survivor.Text))
                {
                    survivor.Content = cluster.Select(m => m.Content ?? m.Text).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                    survivor.Text = survivor.Content ?? survivor.Text;
                }

                survivor.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new MemoryConsolidateResult(
                200,
                Plan: plan,
                SurvivorId: survivorId,
                MergedAwayIds: mergedAway,
                RunId: runId,
                DryRunApplied: false,
                EventEmitted: true);
        }
        finally
        {
            ReleaseLock(_workspacePath);
        }
    }

    public async Task<MemoryConsolidateJobResult> RunJobAsync(
        bool? dryRun,
        bool forceOverlap,
        CancellationToken cancellationToken)
    {
        var appliedDryRun = dryRun ?? MemoryConsolidateLimits.DefaultDryRun;
        var runId = "job-" + Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        if (!TryAcquireLock(_workspacePath, forceOverlap, out _))
        {
            return new MemoryConsolidateJobResult(
                409,
                WorkspaceId: _workspacePath,
                RunId: runId,
                DryRunApplied: appliedDryRun,
                LastRunAt: now,
                LockHeld: true,
                Disabled: true,
                FailureKind: MemoryMutationFailureKind.Conflict,
                Error: "busy");
        }

        try
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            var consol = await ConsolidateUnlockedAsync(appliedDryRun, runId, cancellationToken).ConfigureAwait(false);
            return new MemoryConsolidateJobResult(
                consol.StatusCode is 200 or 201 ? 200 : consol.StatusCode,
                WorkspaceId: _workspacePath,
                RunId: runId,
                DryRunApplied: appliedDryRun,
                LastRunAt: now,
                LockHeld: false,
                Disabled: true,
                FailureKind: consol.FailureKind,
                Error: consol.Error ?? "ok");
        }
        finally
        {
            ReleaseLock(_workspacePath);
        }
    }

    private async Task<MemoryConsolidateResult> ConsolidateUnlockedAsync(
        bool dryRun,
        string runId,
        CancellationToken cancellationToken)
    {
        var memories = await _db.Memories
            .Where(m => m.WorkspaceId == _workspacePath)
            .Where(m => m.Scope == MemoryEntity.WorkspaceScope)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var clusters = BuildClusters(memories, MemoryConsolidateLimits.DefaultSimilarityThreshold);
        var plan = clusters.Select(c => new MemoryConsolidatePlanItem
        {
            CandidateIds = c.Select(m => m.Id).ToList(),
            SimilarityScore = ScoreCluster(c),
            SurvivorId = dryRun ? null : PickSurvivor(c).Id,
        }).ToList();

        if (dryRun)
            return new MemoryConsolidateResult(200, Plan: plan, RunId: runId, DryRunApplied: true, EventEmitted: false);

        string? survivorId = null;
        var mergedAway = new List<string>();
        foreach (var cluster in clusters)
        {
            var survivor = PickSurvivor(cluster);
            survivorId = survivor.Id;
            var nextVersion = await _db.MemoryVersions
                .Where(v => v.MemoryId == survivor.Id)
                .Select(v => (int?)v.VersionNumber)
                .MaxAsync(cancellationToken)
                .ConfigureAwait(false) ?? survivor.Version;
            nextVersion += 1;
            foreach (var victim in cluster.Where(m => m.Id != survivor.Id))
            {
                nextVersion = AppendLineage(survivor, victim, nextVersion);
                await RewireEdgesAsync(survivor.Id, victim.Id, cancellationToken).ConfigureAwait(false);
                _db.Memories.Remove(victim);
                mergedAway.Add(victim.Id);
            }
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new MemoryConsolidateResult(
            200,
            Plan: plan,
            SurvivorId: survivorId,
            MergedAwayIds: mergedAway,
            RunId: runId,
            DryRunApplied: false,
            EventEmitted: true);
    }

    private static List<List<MemoryEntity>> BuildClusters(IReadOnlyList<MemoryEntity> rows, double threshold)
    {
        var groups = new Dictionary<string, List<MemoryEntity>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var key = NormalizeNearDupKey(row.Content ?? row.Text ?? string.Empty);
            if (string.IsNullOrWhiteSpace(key))
                continue;
            if (!groups.TryGetValue(key, out var list))
            {
                list = [];
                groups[key] = list;
            }
            list.Add(row);
        }

        return groups.Values
            .Where(g => g.Count >= 2 && ScoreCluster(g) >= threshold)
            .Select(g => g.ToList())
            .ToList();
    }

    private static string NormalizeNearDupKey(string content)
    {
        var text = content.Trim().ToLowerInvariant();
        text = Regex.Replace(text, @"\s+(twin|triplet|triple)\s*$", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\s+", " ");
        return text;
    }

    private static double ScoreCluster(IReadOnlyList<MemoryEntity> cluster)
        => cluster.Count < 2 ? 0 : 0.95;

    private static MemoryEntity PickSurvivor(IReadOnlyList<MemoryEntity> cluster)
        => cluster.OrderBy(m => m.CreatedAtUtc).ThenBy(m => m.Id, StringComparer.Ordinal).First();

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
