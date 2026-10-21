namespace QBrainAi.Support.Mcp.Services;

/// <summary>
/// TR-MCP-WS-005 / AC-TR-MCP-WS-005-001: every input that agent-plugin version resolution reads, in the order it reads
/// them: the plugin root an environment variable names, the plugin folder beside the workspace, and the plugin caches
/// under the user profile. Unit tests give resolution test-owned sources, so no unit test reads the process
/// environment, the machine or the real profile; the running process's sources are
/// <see cref="MarkerFileService.ProcessAgentPluginVersionSources"/>.
/// </summary>
/// <param name="EnvironmentVariable">Reads a plugin-root environment variable by name; null when it is not set.</param>
/// <param name="SiblingRoot">The plugin folder beside a workspace, from the workspace path and the plugin name; null when there is none.</param>
/// <param name="UserProfile">The profile whose plugin caches are scanned; null or empty for none.</param>
internal sealed record AgentPluginVersionSources(
    Func<string, string?> EnvironmentVariable,
    Func<string, string, string?> SiblingRoot,
    string? UserProfile);
