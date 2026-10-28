namespace QBrainAi.Repl.Host;

/// <summary>
/// TR-MCP-REPL-008: the file system the marker walk and its diagnostics read, that is whether a marker file exists
/// at a path and the lines of that file. The resolver reads the host's file system; the unit tests pass an in-memory
/// one, so the walk cases (AC-1 to AC-4) depend on no folder of the host they run on.
/// </summary>
internal interface IMarkerFileSystem
{
    /// <summary>Returns whether a file exists at <paramref name="path"/>.</summary>
    /// <param name="path">The full path of the file.</param>
    bool FileExists(string path);

    /// <summary>Returns the lines of the file at <paramref name="path"/>.</summary>
    /// <param name="path">The full path of the file.</param>
    string[] ReadAllLines(string path);
}
