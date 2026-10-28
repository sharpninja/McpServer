namespace QBrainAi.TestSupport.TemporaryInstance;

/// <summary>
/// TR-MCP-NUKE-019 AC-6: the checks an integration case uses to prove it runs on a temporary instance it owns, never
/// the live server, its marker or its API key. Pure functions over strings and numbers (no file system, network or
/// environment), linked into the unit project that proves their contract (QBrainAi.Repl.Core.Tests) and the
/// integration projects whose cases call them. Each check returns null when the input passes and a message naming the
/// violation otherwise, so a caller asserts <c>Assert.Null(check)</c> and the failure message says what was wrong.
/// </summary>
internal static class TemporaryInstanceChecks
{
    /// <summary>
    /// Checks the REPL host's start arguments: a <c>--workspace-path</c> and a <c>--marker-file</c> are present and
    /// both lie strictly inside <paramref name="ownedRoot"/>, compared segment by segment ('/' and '\' both separate
    /// segments), and neither path holds a '.' or '..' segment.
    /// </summary>
    /// <param name="arguments">The argument list the REPL host was started with.</param>
    /// <param name="ownedRoot">The temporary instance's root folder.</param>
    /// <returns>Null when the arguments pass; otherwise the violation.</returns>
    internal static string? CheckReplStartArguments(IReadOnlyList<string> arguments, string ownedRoot)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownedRoot);

        var workspace = ValueAfter(arguments, "--workspace-path");
        if (workspace is null)
            return "The REPL host was started without --workspace-path.";

        var marker = ValueAfter(arguments, "--marker-file");
        if (marker is null)
            return "The REPL host was started without --marker-file.";

        if (!IsStrictlyInside(workspace, ownedRoot))
            return $"The REPL host's workspace '{workspace}' is not inside the temporary instance root '{ownedRoot}'.";

        return IsStrictlyInside(marker, ownedRoot)
            ? null
            : $"The REPL host's marker '{marker}' is not inside the temporary instance root '{ownedRoot}'.";
    }

    /// <summary>
    /// Checks a health answer from the temporary instance: the status is 200 and the body holds the request's nonce
    /// (ordinal), the byte-for-byte echo the server's liveness contract gives.
    /// </summary>
    /// <param name="statusCode">The HTTP status code of the answer.</param>
    /// <param name="body">The answer body, or null.</param>
    /// <param name="nonce">The nonce the request sent.</param>
    /// <returns>Null when the answer passes; otherwise the violation.</returns>
    internal static string? CheckHealthAnswer(int statusCode, string? body, string nonce)
    {
        if (string.IsNullOrEmpty(nonce))
            return "The health request carried no nonce to echo.";

        if (statusCode != 200)
            return $"The temporary instance answered health with status {statusCode}, not 200.";

        return body is not null && body.Contains(nonce, StringComparison.Ordinal)
            ? null
            : $"The temporary instance's health answer did not echo the nonce '{nonce}'.";
    }

    /// <summary>TR-MCP-NUKE-019: the most starts a case makes of a temporary instance that keeps losing its port.</summary>
    internal const int MaxStarts = 3;

    /// <summary>
    /// TR-MCP-NUKE-019: decides whether a temporary instance that failed to start is started again on a newly
    /// picked port. Only an instance whose process exited and whose output says its address was already in use
    /// (ignoring case) is restarted, and only while fewer than <see cref="MaxStarts"/> starts were made; any other start
    /// failure, an instance still running, or a third lost port is not, so the case fails with the instance's output.
    /// </summary>
    /// <param name="starts">The starts made so far, the failed one included.</param>
    /// <param name="exited">Whether the instance process has exited.</param>
    /// <param name="output">The instance process output.</param>
    /// <returns>True when the case starts a new instance on a new port.</returns>
    internal static bool ShouldRestartOnNewPort(int starts, bool exited, string output) =>
        starts < MaxStarts
        && exited
        && (output ?? string.Empty).Contains("address already in use", StringComparison.OrdinalIgnoreCase);

    private static string? ValueAfter(IReadOnlyList<string> arguments, string name)
    {
        for (var index = 0; index < arguments.Count - 1; index++)
        {
            if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                return arguments[index + 1];
        }

        return null;
    }

    private static bool IsStrictlyInside(string path, string root)
    {
        var pathSegments = Segments(path);
        if (pathSegments.Any(segment => segment is "." or ".."))
            return false;

        var rootSegments = Segments(root);
        if (pathSegments.Length <= rootSegments.Length)
            return false;

        for (var index = 0; index < rootSegments.Length; index++)
        {
            if (!string.Equals(pathSegments[index], rootSegments[index], StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static string[] Segments(string path) =>
        path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
}
