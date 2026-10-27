using System.Security.Cryptography;
using System.Text;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>Exact immutable memory snapshot pinned by a process manifest.</summary>
public sealed record ProcessPolicySnapshotResult(
    string Status, string? Id = null, int Version = 0, string? Scope = null,
    string? Text = null, string? Sha256 = null);

/// <summary>
/// Reads the authoritative version row for an exact memory identity, scope,
/// version, and content digest. A caller must verify the manifest separately.
/// </summary>
public sealed class ProcessPolicySnapshotReader
{
    private readonly McpDbContext _db;

    /// <summary>Create a reader over the current workspace-filtered MCP store.</summary>
    public ProcessPolicySnapshotReader(McpDbContext db) =>
        _db = db ?? throw new ArgumentNullException(nameof(db));

    /// <summary>Read one pinned snapshot and reject missing or mismatched metadata.</summary>
    public async Task<ProcessPolicySnapshotResult> GetExactAsync(
        string id, int version, string scope, string expectedSha256)
    {
        if (string.IsNullOrWhiteSpace(id) || version < 1 ||
            string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(expectedSha256))
            return new("PROCESS_CONTEXT_UNAVAILABLE");

        try
        {
            // The memory query retains the context's workspace visibility filter.
            var parent = await _db.Memories.AsNoTracking()
                .FirstOrDefaultAsync(memory => memory.Id == id);
            if (parent is null ||
                !string.Equals(parent.Id, id, StringComparison.Ordinal))
                return new("confirmed_not_found");

            if (!string.Equals(parent.Scope, scope, StringComparison.Ordinal))
                return new("PROCESS_POLICY_STALE");

            // Take two so a malformed duplicate cannot silently choose a version.
            var versions = await _db.MemoryVersions.AsNoTracking()
                .Where(snapshot => snapshot.MemoryId == id &&
                    snapshot.VersionNumber == version)
                .Take(2).ToArrayAsync();
            if (versions.Length != 1 ||
                !string.Equals(versions[0].MemoryId, id, StringComparison.Ordinal))
                return new("PROCESS_POLICY_STALE");

            var text = versions[0].Content;
            var digest = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            if (!string.Equals(digest, expectedSha256, StringComparison.OrdinalIgnoreCase))
                return new("PROCESS_POLICY_STALE");

            return new("ok", id, version, scope, text, digest);
        }
        catch (Exception)
        {
            // A failed MCP storage read can never turn into an empty or advisory policy.
            return new("PROCESS_CONTEXT_UNAVAILABLE");
        }
    }
}
