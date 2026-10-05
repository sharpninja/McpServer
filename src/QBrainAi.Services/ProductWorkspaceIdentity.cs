namespace QBrainAi.Support.Mcp;

/// <summary>
/// TR-MCP-QBRAIN-003: Product workspace names that remain valid through the 1.x alias window.
/// </summary>
public static class ProductWorkspaceIdentity
{
    /// <summary>Canonical workspace or folder name.</summary>
    public const string CanonicalName = "QBrainAi";

    /// <summary>Checkout and workspace name that remains valid until the repository rename.</summary>
    public const string LegacyName = "McpServer";

    /// <summary>
    /// True when the workspace name or the last folder of its path is the product checkout.
    /// </summary>
    /// <param name="name">Configured workspace name.</param>
    /// <param name="workspacePath">Configured workspace path.</param>
    /// <returns>True for <c>QBrainAi</c> or <c>McpServer</c>.</returns>
    public static bool IsProductWorkspace(string? name, string? workspacePath)
    {
        if (IsProductName(name))
            return true;

        if (string.IsNullOrWhiteSpace(workspacePath))
            return false;

        var trimmed = workspacePath.TrimEnd('\\', '/');
        var separator = trimmed.LastIndexOfAny(['\\', '/']);
        var folder = separator < 0 ? trimmed : trimmed[(separator + 1)..];
        return IsProductName(folder);
    }

    private static bool IsProductName(string? value)
        => string.Equals(value, CanonicalName, StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, LegacyName, StringComparison.OrdinalIgnoreCase);
}
