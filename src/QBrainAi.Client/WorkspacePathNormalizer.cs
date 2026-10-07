namespace QBrainAi.Client;

/// <summary>
/// TR-MCP-FED-PATH-001: Host operating-system facts used when a request-supplied workspace path
/// must be resolved. Injected so a server running on one OS can be tested as if it ran on another.
/// </summary>
public interface IWorkspaceHostEnvironment
{
    /// <summary>Path semantics native to the host operating system.</summary>
    WorkspacePathPlatform Platform { get; }

    /// <summary>Process working directory used to anchor host-native relative paths.</summary>
    string CurrentDirectory { get; }

    /// <summary>
    /// Returns the canonical absolute form of a host-native path (full-path resolution plus
    /// lexical normalization). Never called for foreign-platform paths.
    /// </summary>
    /// <param name="path">A path whose syntax is native to <see cref="Platform"/>.</param>
    string NormalizeNativePath(string path);
}

/// <summary>
/// FR-MCP-FED-PATH-001 / TR-MCP-FED-PATH-001: Single entry point for interpreting a workspace
/// path supplied by a request. Platform is decided by the path's own syntax (drive letter, UNC,
/// POSIX root); only host-native paths are resolved against the host, so a Linux hub never
/// prefixes a Windows path with its working directory.
/// </summary>
public interface IWorkspacePathNormalizer
{
    /// <summary>Detects the path platform from syntax; relative paths belong to the host.</summary>
    /// <param name="workspacePath">Request-supplied workspace path.</param>
    WorkspacePathPlatform DetectPlatform(string workspacePath);

    /// <summary>Returns the canonical workspace path for a request-supplied value.</summary>
    /// <param name="workspacePath">Request-supplied workspace path.</param>
    /// <exception cref="ArgumentException">The path is null, empty, or whitespace.</exception>
    string Normalize(string workspacePath);

    /// <summary>
    /// Returns the final path segment using the separators of the path's own platform
    /// (host <see cref="Path.GetFileName(string)"/> treats a backslash as text on POSIX hosts).
    /// </summary>
    /// <param name="workspacePath">Request-supplied workspace path.</param>
    string GetLeafName(string workspacePath);

    /// <summary>
    /// Returns whether the path's syntax is native to the host, i.e. whether the host file system
    /// can address it. Foreign-platform workspaces (a Windows proxy's path on a Linux hub) are
    /// identities only and must never be created or opened on the host.
    /// </summary>
    /// <param name="workspacePath">Request-supplied workspace path.</param>
    bool IsHostNative(string workspacePath);

    /// <summary>
    /// Joins relative segments onto a workspace root using the root's own platform syntax
    /// (host <see cref="Path.Combine(string[])"/> mixes separators for foreign roots). A rooted
    /// segment replaces everything before it, matching <see cref="Path.Combine(string[])"/>.
    /// </summary>
    /// <param name="rootPath">Workspace root (any platform).</param>
    /// <param name="segments">Relative or rooted segments to append.</param>
    string Combine(string rootPath, params string[] segments);
}

/// <summary>Default <see cref="IWorkspacePathNormalizer"/> implementation.</summary>
public sealed class WorkspacePathNormalizer : IWorkspacePathNormalizer
{
    /// <summary>
    /// Normalizer for the running process host. Correct in production on any OS; tests inject a
    /// simulated <see cref="IWorkspaceHostEnvironment"/> instead.
    /// </summary>
    public static WorkspacePathNormalizer Process { get; } = new(SystemWorkspaceHostEnvironment.Instance);

    private readonly IWorkspaceHostEnvironment _host;

    /// <summary>Initializes a new instance of the <see cref="WorkspacePathNormalizer"/> class.</summary>
    /// <param name="host">Host environment used for host-native paths.</param>
    public WorkspacePathNormalizer(IWorkspaceHostEnvironment host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    /// <inheritdoc />
    public WorkspacePathPlatform DetectPlatform(string workspacePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        return DetectTrimmed(workspacePath.Trim());
    }

    /// <inheritdoc />
    public string Normalize(string workspacePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        var trimmed = workspacePath.Trim();
        var platform = DetectTrimmed(trimmed);

        // Only host-native syntax may be resolved against the host; a foreign absolute path is
        // already absolute in its own world and is normalized lexically.
        return platform == _host.Platform
            ? _host.NormalizeNativePath(trimmed)
            : WorkspaceIdentityPath.NormalizeLexicalPath(trimmed, platform);
    }

    /// <inheritdoc />
    public string GetLeafName(string workspacePath)
    {
        var normalized = Normalize(workspacePath);
        var separators = DetectTrimmed(normalized) == WorkspacePathPlatform.Windows
            ? new[] { '\\', '/' }
            : new[] { '/' };
        var segments = normalized.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        // A bare root ("C:\", "/", "\\srv\share\") has no leaf; report the root itself.
        return segments.Length == 0 || normalized.TrimEnd(separators).EndsWith(':')
            ? normalized
            : segments[^1];
    }

    /// <inheritdoc />
    public string Combine(string rootPath, params string[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        var current = Normalize(rootPath);
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment))
                continue;

            var trimmed = segment.Trim();
            if (IsWindowsRooted(trimmed) || trimmed.StartsWith('/'))
            {
                current = Normalize(trimmed);
                continue;
            }

            // '/' is a separator in both syntaxes; lexical normalization of the root's own
            // platform collapses it (and any dot segments) into canonical form.
            current = WorkspaceIdentityPath.NormalizeLexicalPath(
                current.TrimEnd('/', '\\') + "/" + trimmed,
                DetectTrimmed(current));
        }

        return current;
    }

    /// <inheritdoc />
    public bool IsHostNative(string workspacePath) =>
        DetectPlatform(workspacePath) == _host.Platform;

    private WorkspacePathPlatform DetectTrimmed(string path)
    {
        if (IsWindowsRooted(path))
            return WorkspacePathPlatform.Windows;

        if (path.StartsWith('/'))
            return WorkspacePathPlatform.CaseSensitive;

        return _host.Platform;
    }

    private static bool IsWindowsRooted(string path) =>
        (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && IsSeparator(path[2])) ||
        path.StartsWith(@"\\", StringComparison.Ordinal) ||
        path.StartsWith("//", StringComparison.Ordinal);

    private static bool IsSeparator(char value) => value is '/' or '\\';
}

/// <summary>
/// TR-MCP-FED-PATH-001: <see cref="IWorkspaceHostEnvironment"/> backed by the running process.
/// </summary>
public sealed class SystemWorkspaceHostEnvironment : IWorkspaceHostEnvironment
{
    /// <summary>Shared instance for the current process.</summary>
    public static SystemWorkspaceHostEnvironment Instance { get; } = new();

    /// <inheritdoc />
    public WorkspacePathPlatform Platform =>
        OperatingSystem.IsWindows() ? WorkspacePathPlatform.Windows : WorkspacePathPlatform.CaseSensitive;

    /// <inheritdoc />
    public string CurrentDirectory => Environment.CurrentDirectory;

    /// <inheritdoc />
    public string NormalizeNativePath(string path) =>
        WorkspaceIdentityPath.NormalizePath(path, Platform);
}
