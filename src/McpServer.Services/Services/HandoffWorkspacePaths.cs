namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-SURFACE-001: One shared canonical workspace path for all handoff scopes.</summary>
public static class HandoffWorkspacePaths
{
    /// <summary>
    /// Converts a caller workspace path to the single absolute value pushed into
    /// <c>WorkspaceContext</c>, <c>McpDbContext</c>, and <c>WorkspaceServiceAccessor</c>.
    /// </summary>
    public static bool TryCanonicalize(string? workspacePath, out string canonical, out string? error)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            error = "Workspace path is required.";
            return false;
        }

        try
        {
            canonical = Path.GetFullPath(workspacePath.Trim());
        }
        catch (Exception)
        {
            error = "Workspace path could not be canonicalized.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(canonical))
        {
            error = "Workspace path is required.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>Canonicalizes or throws <see cref="ArgumentException"/>.</summary>
    public static string Canonicalize(string workspacePath)
    {
        if (!TryCanonicalize(workspacePath, out var canonical, out var error))
            throw new ArgumentException(error, nameof(workspacePath));
        return canonical;
    }
}
