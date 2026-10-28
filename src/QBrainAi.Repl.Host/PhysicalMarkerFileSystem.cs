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
}
