namespace QBrainAi.Support.Mcp.Services;

/// <summary>
/// TR-MCP-WS-005 / AC-TR-MCP-WS-005-001: every input that agent-plugin version resolution reads, in the order it reads
/// them: the plugin root an environment variable names, the plugin folder beside the workspace, and the plugin caches
/// under the user profile. Unit tests give resolution test-owned sources, so no unit test reads the process
/// environment, the machine or the real profile.
/// </summary>
/// <param name="EnvironmentVariable">Reads a plugin-root environment variable by name; null when it is not set.</param>
/// <param name="SiblingRoot">The plugin folder beside a workspace, from the workspace path and the plugin name; null when there is none.</param>
/// <param name="UserProfile">The profile whose plugin caches are scanned; null or empty for none.</param>
internal sealed record AgentPluginVersionSources(
    Func<string, string?> EnvironmentVariable,
    Func<string, string, string?> SiblingRoot,
    string? UserProfile)
{
    /// <summary>
    /// The running process's sources: its environment variables, the folder beside the workspace on disk, and the
    /// current user's profile. Read when each resolution starts.
    /// </summary>
    internal static AgentPluginVersionSources Process =>
        new(Environment.GetEnvironmentVariable, SiblingOf, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    /// <summary>The plugin folder beside <paramref name="workspacePath"/>; null when the workspace has no parent.</summary>
    /// <param name="workspacePath">Workspace path; none when empty.</param>
    /// <param name="pluginName">Plugin folder name.</param>
    /// <returns>The sibling plugin folder, or null.</returns>
    internal static string? SiblingOf(string workspacePath, string pluginName)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            return null;

        var parent = Path.GetDirectoryName(Path.GetFullPath(workspacePath));
        return string.IsNullOrWhiteSpace(parent) ? null : Path.Combine(parent, pluginName);
    }
}
