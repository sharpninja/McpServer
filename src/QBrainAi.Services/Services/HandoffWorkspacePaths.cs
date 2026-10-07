using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>TR-HANDOFF-SURFACE-001: One shared canonical workspace path for all handoff scopes.</summary>
public static class HandoffWorkspacePaths
{
    /// <summary>
    /// Converts a caller workspace path to the single absolute value pushed into
    /// <c>WorkspaceContext</c>, <c>McpDbContext</c>, and <c>WorkspaceServiceAccessor</c>.
    /// </summary>
    public static bool TryCanonicalize(string? workspacePath, out string canonical, out string? error) =>
        TryCanonicalize(workspacePath, WorkspacePathNormalizer.Process, out canonical, out error);

    /// <summary>
    /// TR-MCP-FED-PATH-001: Canonicalizes through an explicit path normalizer (tests inject a
    /// simulated host).
    /// </summary>
    public static bool TryCanonicalize(
        string? workspacePath,
        IWorkspacePathNormalizer normalizer,
        out string canonical,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(normalizer);
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            error = "Workspace path is required.";
            return false;
        }

        try
        {
            // TR-MCP-FED-PATH-001: path syntax decides the platform; only host-native paths are
            // resolved against the host.
            canonical = normalizer.Normalize(workspacePath);
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
    public static string Canonicalize(string workspacePath) =>
        Canonicalize(workspacePath, WorkspacePathNormalizer.Process);

    /// <summary>TR-MCP-FED-PATH-001: Canonicalizes through an explicit normalizer or throws.</summary>
    public static string Canonicalize(string workspacePath, IWorkspacePathNormalizer normalizer)
    {
        if (!TryCanonicalize(workspacePath, normalizer, out var canonical, out var error))
            throw new ArgumentException(error, nameof(workspacePath));
        return canonical;
    }
}
