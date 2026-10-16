namespace QBrainAi.Support.Mcp.Tests;

/// <summary>
/// TEST-MCP-PATHNORM-001: test-side oracle for FR-MCP-PATHNORM-001. Stored and returned workspace
/// keys use forward slashes, so a Windows drive or UNC path is expected with every backslash
/// replaced by <c>/</c>; any other value (POSIX path, hash id, opaque id) is expected unchanged.
/// Intended for the clean absolute paths tests build (no dot segments, no trailing separator). It
/// deliberately does not call production normalization code.
/// </summary>
internal static class ExpectedWorkspaceKey
{
    /// <summary>Returns the expected persisted form of <paramref name="path"/>.</summary>
    /// <param name="path">Workspace path as the test supplied it.</param>
    /// <returns>The expected canonical workspace key.</returns>
    public static string Of(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var drive = path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] is '\\' or '/';
        var unc = path.StartsWith(@"\\", StringComparison.Ordinal);
        return drive || unc ? path.Replace('\\', '/') : path;
    }
}
