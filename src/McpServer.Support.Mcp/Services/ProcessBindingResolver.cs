using System.Collections.Concurrent;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// One approved process assignment candidate. Persistence and operator approval
/// verification are performed by the caller before a row reaches this resolver.
/// </summary>
public sealed class ProcessBindingRecord
{
    /// <summary>global, workspace, or todo.</summary>
    public string Scope { get; init; } = "";

    /// <summary>Canonical identity within the selected scope.</summary>
    public string ScopeId { get; init; } = "";

    /// <summary>Immutable binding identity.</summary>
    public string BindingId { get; init; } = "";

    /// <summary>Methodology identifier supplied by the approved manifest.</summary>
    public string MethodologyId { get; init; } = "";

    /// <summary>Approved binding revision.</summary>
    public int Revision { get; init; }

    /// <summary>Whether the caller verified the operator approval receipt.</summary>
    public bool SignedApproval { get; init; }

    /// <summary>Assigned or ExplicitlyUnmanaged.</summary>
    public string AssignmentState { get; init; } = "";
}

/// <summary>Typed generic binding resolution result.</summary>
/// <param name="Status">ok or PROCESS_CONTEXT_UNAVAILABLE.</param>
/// <param name="AssignmentState">Assigned, ExplicitlyUnmanaged, or Unavailable.</param>
/// <param name="BindingId">Pinned binding identity when available.</param>
/// <param name="MethodologyId">Approved methodology identifier when available.</param>
/// <param name="BindingRevision">Pinned binding revision when available.</param>
public sealed record ProcessBindingResolution(
    string Status, string AssignmentState, string BindingId,
    string MethodologyId, int BindingRevision);

/// <summary>
/// Resolves approved bindings in TODO, workspace, global order and pins the
/// selected revision for an active slice. This selection core contains no
/// methodology-specific policy. A durable store must supply candidate rows and
/// persist slice pins before this result can authorize a guarded transition.
/// </summary>
public sealed class ProcessBindingResolver
{
    private readonly List<ProcessBindingRecord> _rows;
    private readonly ConcurrentDictionary<(string Workspace, string Todo, string Slice),
        ProcessBindingResolution> _pins = new();

    /// <summary>Create a resolver over current approved candidate rows.</summary>
    public ProcessBindingResolver(List<ProcessBindingRecord> rows)
    {
        _rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    /// <summary>Resolve or return the previously pinned binding for a slice.</summary>
    public ProcessBindingResolution Resolve(string workspace, string todo, string slice)
    {
        if (string.IsNullOrWhiteSpace(workspace) ||
            string.IsNullOrWhiteSpace(todo) ||
            string.IsNullOrWhiteSpace(slice))
            return Unavailable();

        var key = (workspace, todo, slice);
        if (_pins.TryGetValue(key, out var pinned))
            return pinned;

        // Use one candidate snapshot so mutation cannot mix scopes within a call.
        var candidates = _rows.ToArray();
        foreach (var (scope, scopeId) in new[] {
            ("todo", todo), ("workspace", workspace), ("global", "*") })
        {
            var matches = candidates.Where(row =>
                string.Equals(row.Scope, scope, StringComparison.Ordinal) &&
                string.Equals(row.ScopeId, scopeId, StringComparison.Ordinal)).ToArray();
            if (matches.Length == 0)
                continue;
            if (matches.Length != 1 || !matches[0].SignedApproval)
                return Unavailable();

            var row = matches[0];
            if (row.AssignmentState is not ("Assigned" or "ExplicitlyUnmanaged") ||
                string.IsNullOrWhiteSpace(row.BindingId))
                return Unavailable();

            var selected = new ProcessBindingResolution("ok", row.AssignmentState,
                row.BindingId, row.MethodologyId, row.Revision);
            return _pins.GetOrAdd(key, selected);
        }

        return Unavailable();
    }

    private static ProcessBindingResolution Unavailable() =>
        new("PROCESS_CONTEXT_UNAVAILABLE", "Unavailable", "", "", 0);
}
