using QBrainAi.Client;

namespace QBrainAi.Repl.Host;

/// <summary>TR-MCP-REPL-008: the host's file system, which the marker walk and its diagnostics read outside the tests.</summary>
internal sealed class PhysicalMarkerFileSystem : IMarkerFileSystem
{
    /// <summary>Gets the one instance the resolver uses.</summary>
    internal static PhysicalMarkerFileSystem Instance { get; } = new();

    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc />
    public string[] ReadAllLines(string path) => File.ReadAllLines(path);

    /// <summary>Resolves a directory through the native filesystem path grammar.</summary>
    public string GetFullPath(string path) => new DirectoryInfo(path).FullName;

    /// <summary>Returns the native directory's parent, stopping at the native filesystem root.</summary>
    public string? GetParentPath(string path) => new DirectoryInfo(path).Parent?.FullName;

    /// <summary>Combines a native directory and the marker name.</summary>
    public string GetMarkerPath(string directory) => Path.Combine(directory, "AGENTS-README-FIRST.yaml");

    /// <summary>Reads the physical cache through the existing alias, expiry and agent-isolation rules.</summary>
    public bool TryUseCachedVerification(MarkerFileClientOptionsResolver.MarkerSettings marker, string agent,
        out QBrainAiClientOptions? options) => MarkerFileClientOptionsResolver.TryUseCachedVerification(marker, agent, out options);

    /// <summary>Saves to the physical cache under the existing best-effort locking rules.</summary>
    public void SaveVerifiedCacheEntry(MarkerFileClientOptionsResolver.MarkerSettings marker, string agent) =>
        MarkerFileClientOptionsResolver.SaveVerifiedCacheEntry(marker, agent);
}
