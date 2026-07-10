namespace McpServer.SessionLog.Transcripts;

internal static class TranscriptPathSecurity
{
    internal static string ValidateReadablePath(TranscriptIngestionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.WorkspacePath))
            return Path.GetFullPath(request.Path);

        var sourcePath = ResolveCanonicalPath(request.Path);
        var roots = new List<string> { ResolveCanonicalPath(request.WorkspacePath!) };
        roots.AddRange(request.ProviderTranscriptRoots.Where(root => !string.IsNullOrWhiteSpace(root)).Select(ResolveCanonicalPath));

        if (roots.Any(root => IsUnderRoot(sourcePath, root)))
            return sourcePath;

        throw new UnauthorizedAccessException("Transcript path resolves outside the workspace and configured provider transcript roots: " + sourcePath);
    }

    private static string ResolveCanonicalPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var finalPath = ResolveFinalTarget(fullPath);
        return EnsureDirectorySuffix(Path.GetFullPath(finalPath));
    }

    private static string ResolveFinalTarget(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
            {
                var info = new FileInfo(fullPath);
                if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    return info.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? fullPath;
            }
            else if (Directory.Exists(fullPath))
            {
                var info = new DirectoryInfo(fullPath);
                if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    return info.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? fullPath;
            }
        }
        catch (IOException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }

        return fullPath;
    }

    private static bool IsUnderRoot(string path, string root)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (string.Equals(path.TrimEnd(Path.DirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar), comparison))
            return true;

        return path.StartsWith(root, comparison);
    }

    private static string EnsureDirectorySuffix(string path)
    {
        if (File.Exists(path))
            return path;

        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
