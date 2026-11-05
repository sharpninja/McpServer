using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>FR-MCP-MEMORY-013 / TR-MCP-MEMORY-JOBS-002: Consolidate / sleep-merge and job tick operations.</summary>
public sealed partial class MemoryConsolidateOperations
{
    private static readonly ConcurrentDictionary<string, WorkspaceLockState> Locks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan LockTtl = TimeSpan.FromSeconds(30);
    private readonly McpDbContext _db;
    private readonly string _workspacePath;
    private readonly TimeProvider _time;

    /// <summary>Creates operations bound to an already-scoped <see cref="McpDbContext"/>.</summary>
    /// <param name="db">The scoped database context.</param>
    /// <param name="workspacePath">The active workspace.</param>
    /// <param name="time">The clock the workspace lock and its TTL are read from; null uses <see cref="TimeProvider.System"/>.</param>
    /// <remarks>2026-11-05, TR-MCP-MEMORY-JOBS-002; TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): added the optional <paramref name="time"/> clock, read for the lock's acquisition time and TTL takeover instead of DateTimeOffset.UtcNow, because the sibling sweep of hostile validation round 12 finding R12-F2 showed the forced-overlap TTL decision ran on the live clock in a unit case (mock gate ug-r13z4-mockgate-b-1).</remarks>
    public MemoryConsolidateOperations(McpDbContext db, string workspacePath, TimeProvider? time = null)
    {
        _db = db;
        // FR-MCP-PATHNORM-001: stored WorkspaceId values are canonical; filter and lock in the same form.
        _workspacePath = WorkspaceIdentity.CanonicalizeWorkspaceId(workspacePath);
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Plans or applies a consolidate merge for the active workspace.</summary>
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

        if (!TryAcquireLock(_workspacePath, forceOverlap: false, _time.GetUtcNow(), out _))
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

    /// <summary>Runs a scheduled consolidate tick / stats probe.</summary>
    public async Task<MemoryConsolidateJobResult> RunJobAsync(
        bool? dryRun,
        bool forceOverlap,
        CancellationToken cancellationToken)
    {
        var appliedDryRun = dryRun ?? MemoryConsolidateLimits.DefaultDryRun;
        var runId = "job-" + Guid.NewGuid().ToString("N");
        var now = _time.GetUtcNow();

        if (!TryAcquireLock(_workspacePath, forceOverlap, now, out _))
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

}
