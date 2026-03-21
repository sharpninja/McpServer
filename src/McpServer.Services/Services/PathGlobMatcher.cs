using System;
using System.Collections.Generic;
using System.IO.Enumeration;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013/TR-MCP-DESKTOP-001: Shared segment-wise glob matcher used by repo and
/// desktop-launch path allowlists.
/// </summary>
internal static class PathGlobMatcher
{
    private static readonly char[] s_trimSlashChars = ['/', '\\'];

    /// <summary>
    /// TR-PLANNED-013/TR-MCP-DESKTOP-001: Returns whether the candidate path matches any configured glob pattern.
    /// </summary>
    /// <param name="candidatePath">Candidate path to evaluate.</param>
    /// <param name="patterns">Glob patterns using <c>*</c>, <c>?</c>, and <c>**</c> semantics.</param>
    /// <returns><see langword="true"/> when any pattern matches the candidate path; otherwise <see langword="false"/>.</returns>
    public static bool MatchesAny(string candidatePath, IReadOnlyList<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        var candidateSegments = SplitPathSegments(NormalizePath(candidatePath));
        foreach (var pattern in patterns)
        {
            var patternSegments = SplitPathSegments(NormalizePath(pattern));
            if (GlobMatches(candidateSegments, 0, patternSegments, 0))
                return true;
        }

        return false;
    }

    /// <summary>
    /// TR-PLANNED-013/TR-MCP-DESKTOP-001: Returns whether the directory path is an ancestor
    /// of any configured glob pattern.
    /// </summary>
    /// <param name="directoryPath">Directory path to evaluate.</param>
    /// <param name="patterns">Glob patterns using <c>*</c>, <c>?</c>, and <c>**</c> semantics.</param>
    /// <returns><see langword="true"/> when the directory can lead to an allowed path; otherwise <see langword="false"/>.</returns>
    public static bool MayMatchDirectoryPrefix(string directoryPath, IReadOnlyList<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        var directorySegments = SplitPathSegments(NormalizePath(directoryPath));
        foreach (var pattern in patterns)
        {
            var patternSegments = SplitPathSegments(NormalizePath(pattern));
            if (PatternMayMatchDirectoryPrefix(directorySegments, 0, patternSegments, 0))
                return true;
        }

        return false;
    }

    private static bool GlobMatches(string[] candidateSegments, int candidateIndex, string[] patternSegments, int patternIndex)
    {
        patternIndex = CollapseDoubleStar(patternSegments, patternIndex);
        if (patternIndex == patternSegments.Length)
            return candidateIndex == candidateSegments.Length;

        if (patternSegments[patternIndex] == "**")
        {
            if (patternIndex == patternSegments.Length - 1)
                return true;

            for (var nextCandidateIndex = candidateIndex; nextCandidateIndex <= candidateSegments.Length; nextCandidateIndex++)
            {
                if (GlobMatches(candidateSegments, nextCandidateIndex, patternSegments, patternIndex + 1))
                    return true;
            }

            return false;
        }

        if (candidateIndex == candidateSegments.Length)
            return false;

        if (!FileSystemName.MatchesSimpleExpression(patternSegments[patternIndex], candidateSegments[candidateIndex], ignoreCase: true))
            return false;

        return GlobMatches(candidateSegments, candidateIndex + 1, patternSegments, patternIndex + 1);
    }

    private static bool PatternMayMatchDirectoryPrefix(string[] directorySegments, int directoryIndex, string[] patternSegments, int patternIndex)
    {
        patternIndex = CollapseDoubleStar(patternSegments, patternIndex);
        if (directoryIndex == directorySegments.Length)
            return true;

        if (patternIndex == patternSegments.Length)
            return false;

        if (patternSegments[patternIndex] == "**")
        {
            return PatternMayMatchDirectoryPrefix(directorySegments, directoryIndex, patternSegments, patternIndex + 1)
                   || PatternMayMatchDirectoryPrefix(directorySegments, directoryIndex + 1, patternSegments, patternIndex);
        }

        if (!FileSystemName.MatchesSimpleExpression(patternSegments[patternIndex], directorySegments[directoryIndex], ignoreCase: true))
            return false;

        return PatternMayMatchDirectoryPrefix(directorySegments, directoryIndex + 1, patternSegments, patternIndex + 1);
    }

    private static int CollapseDoubleStar(string[] patternSegments, int patternIndex)
    {
        while (patternIndex + 1 < patternSegments.Length
               && patternSegments[patternIndex] == "**"
               && patternSegments[patternIndex + 1] == "**")
        {
            patternIndex++;
        }

        return patternIndex;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ".";

        var normalized = path.Replace('\\', '/').TrimStart(s_trimSlashChars);
        return string.IsNullOrEmpty(normalized) ? "." : normalized;
    }

    private static string[] SplitPathSegments(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || string.Equals(path, ".", StringComparison.Ordinal))
            return [];

        return path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
