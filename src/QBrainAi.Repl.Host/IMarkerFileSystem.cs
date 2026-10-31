using QBrainAi.Client;

namespace QBrainAi.Repl.Host;

/// <summary>
/// TR-MCP-REPL-008: the file system the marker walk and its diagnostics read, that is whether a marker file exists
/// at a path, its lines, directory navigation and verified-marker cache operations. The physical adapter owns all
/// host IO; an injected in-memory adapter owns its virtual root and cache, so a found marker never escapes the seam
/// to the real profile (FR-MCP-REPL-007, FR-MCP-REPL-008 ac-4; slice M SRC-7/RINT-13).
/// </summary>
internal interface IMarkerFileSystem
{
    /// <summary>Returns whether a file exists at <paramref name="path"/>.</summary>
    /// <param name="path">The full path of the file.</param>
    bool FileExists(string path);

    /// <summary>Returns the lines of the file at <paramref name="path"/>.</summary>
    /// <param name="path">The full path of the file.</param>
    string[] ReadAllLines(string path);

    /// <summary>Returns the full directory spelling under this filesystem's path grammar.</summary>
    /// <param name="path">Explicit directory path.</param>
    string GetFullPath(string path);

    /// <summary>Returns the parent directory, or null at this filesystem's root.</summary>
    /// <param name="path">Full directory path.</param>
    string? GetParentPath(string path);

    /// <summary>Combines a directory and the marker name under this filesystem's path grammar.</summary>
    /// <param name="directory">Full directory path.</param>
    string GetMarkerPath(string directory);

    /// <summary>Reads verification only from this adapter's cache.</summary>
    /// <param name="marker">The marker whose trust is queried.</param>
    /// <param name="agent">Canonical agent cache key.</param>
    /// <param name="options">Cached client options on a hit.</param>
    /// <returns>Whether this adapter has a verified matching marker.</returns>
    bool TryUseCachedVerification(MarkerFileClientOptionsResolver.MarkerSettings marker, string agent, out QBrainAiClientOptions? options);

    /// <summary>Saves successful verification only to this adapter's cache.</summary>
    /// <param name="marker">The verified marker.</param>
    /// <param name="agent">Canonical agent cache key.</param>
    void SaveVerifiedCacheEntry(MarkerFileClientOptionsResolver.MarkerSettings marker, string agent);
}
