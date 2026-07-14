namespace McpServer.Support.Mcp.Requirements;

/// <summary>TR-MCP-DOCFXWIKI-001: Shared workspace path containment and reparse-point validation for wiki export inputs.</summary>
internal static class RequirementsWikiPathSecurity
{
    internal static string? ResolveWorkspaceContainedPath(
        string workspaceRoot,
        string normalizedRelativePath,
        string label,
        ICollection<string> errors)
    {
        var root = Path.GetFullPath(workspaceRoot);
        var fullPath = Path.GetFullPath(Path.Combine(root, normalizedRelativePath));
        if (!IsContainedByRoot(root, fullPath))
        {
            errors.Add($"{label} escapes the workspace root.");
            return null;
        }

        if (EscapesWorkspaceThroughReparsePoint(root, fullPath))
            errors.Add($"{label} escapes the workspace root through a reparse point.");

        return fullPath;
    }

    internal static void ThrowIfPathEscapesRoot(string rootPath, string fullPath, string label)
    {
        var root = Path.GetFullPath(rootPath);
        var path = Path.GetFullPath(fullPath);
        if (!IsContainedByRoot(root, path))
            throw new InvalidOperationException($"{label} escapes root '{root}'.");

        if (EscapesWorkspaceThroughReparsePoint(root, path))
            throw new InvalidOperationException($"{label} escapes root '{root}' through a reparse point.");
    }

    internal static bool IsContainedByRoot(string rootPath, string fullPath)
    {
        var root = Path.GetFullPath(rootPath);
        var path = Path.GetFullPath(fullPath);
        return path.Equals(root, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(EnsureTrailingSeparator(root), StringComparison.OrdinalIgnoreCase);
    }

    internal static bool EscapesWorkspaceThroughReparsePoint(string workspaceRoot, string fullPath)
    {
        var root = Path.GetFullPath(workspaceRoot);
        var path = Path.GetFullPath(fullPath);
        if (!IsContainedByRoot(root, path))
            return true;

        var relative = Path.GetRelativePath(root, path);
        if (relative == ".")
            return false;

        var current = root;
        foreach (var segment in relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            FileSystemInfo? info = Directory.Exists(current)
                ? new DirectoryInfo(current)
                : File.Exists(current) ? new FileInfo(current) : null;
            if (info is null || !info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                continue;

            try
            {
                var target = info.ResolveLinkTarget(returnFinalTarget: true);
                if (target is null)
                    return true;

                if (!IsContainedByRoot(root, target.FullName))
                    return true;
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        return false;
    }

    internal static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}
