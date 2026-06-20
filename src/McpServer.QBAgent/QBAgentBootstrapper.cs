using McpServer.McpAgent;

namespace McpServer.QBAgent;

/// <summary>FR-MCP-QBAGENT-001: Outcome of bootstrapping QBAgent from a workspace marker file.</summary>
public enum QBAgentBootstrapStatus
{
    /// <summary>Marker found and valid; QBAgent is bound to the QuadBrain endpoint from the marker.</summary>
    Started,

    /// <summary>No marker file in the start directory; QBAgent must exit gracefully.</summary>
    NoMarker,

    /// <summary>Marker present but missing or invalid required fields; QBAgent cannot bind to QuadBrain.</summary>
    InvalidMarker,
}

/// <summary>FR-MCP-QBAGENT-001: Result of bootstrapping QBAgent from a workspace marker file.</summary>
/// <param name="Status">Bootstrap status.</param>
/// <param name="Options">Agent options bound to the marker's QuadBrain endpoint and API key, when <see cref="QBAgentBootstrapStatus.Started"/>.</param>
/// <param name="Message">Human-readable status message.</param>
public sealed record QBAgentBootstrapResult(QBAgentBootstrapStatus Status, McpAgentOptions? Options, string Message);

/// <summary>
/// FR-MCP-QBAGENT-001 / TR-MCP-QBAGENT-001: Bootstraps QBAgent from the <c>AGENTS-README-FIRST.yaml</c> marker
/// in the directory the agent was started in. QBAgent communicates exclusively with the MCP Server QuadBrain
/// service: it binds the QuadBrain endpoint (<c>baseUrl</c>) and the <c>apiKey</c> from the marker and applies
/// the QBAgent (QuadBrain-only) identity under the standard (non-ACID) execution profile so the agent can execute
/// action tools (ACID tight-coupling is intentionally not applied to QBAgent). When no marker is present QBAgent
/// exits gracefully and contacts no endpoint.
/// </summary>
public static class QBAgentBootstrapper
{
    /// <summary>The marker file name read from the agent's start directory.</summary>
    public const string MarkerFileName = "AGENTS-README-FIRST.yaml";

    /// <summary>Bootstraps QBAgent from the marker file in <paramref name="startDirectory"/>.</summary>
    /// <param name="startDirectory">The directory the agent was started in.</param>
    /// <returns>The bootstrap result, including bound options when a valid marker is present.</returns>
    public static QBAgentBootstrapResult Bootstrap(string startDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);

        var markerPath = Path.Combine(startDirectory, MarkerFileName);
        if (!File.Exists(markerPath))
        {
            return new QBAgentBootstrapResult(
                QBAgentBootstrapStatus.NoMarker,
                null,
                $"No {MarkerFileName} marker found in '{startDirectory}'. QBAgent requires a workspace marker to reach the QuadBrain service; exiting gracefully.");
        }

        var fields = ReadTopLevelScalars(markerPath);
        fields.TryGetValue("baseUrl", out var baseUrl);
        fields.TryGetValue("apiKey", out var apiKey);
        fields.TryGetValue("workspacePath", out var workspacePath);

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            return new QBAgentBootstrapResult(
                QBAgentBootstrapStatus.InvalidMarker,
                null,
                $"Marker '{markerPath}' is missing a required baseUrl and/or apiKey. QBAgent cannot bind to the QuadBrain service.");
        }

        if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var baseUri))
        {
            return new QBAgentBootstrapResult(
                QBAgentBootstrapStatus.InvalidMarker,
                null,
                $"Marker '{markerPath}' has an invalid baseUrl '{baseUrl}'. QBAgent cannot bind to the QuadBrain service.");
        }

        // QBAgent identity, standard (non-ACID) execution profile so the agent can execute action tools
        // (ACID tight-coupling is not required for QBAgent). QuadBrain is reached as an OpenAI model.
        var definition = QBAgentDefinition.Instance;
        var options = new McpAgentOptions
        {
            BaseUrl = baseUri,
            ApiKey = apiKey.Trim(),
            WorkspacePath = string.IsNullOrWhiteSpace(workspacePath) ? null : workspacePath.Trim(),
            AgentId = definition.AgentId,
            AgentName = definition.AgentName,
            SourceType = definition.SourceType,
            Description = definition.Description,
            RequireAuthentication = true,
        };

        return new QBAgentBootstrapResult(
            QBAgentBootstrapStatus.Started,
            options,
            $"QBAgent bound to the QuadBrain service at {baseUri} (workspace: {options.WorkspacePath ?? "marker-scoped"}).");
    }

    /// <summary>
    /// Reads only the top-level scalar (<c>key: value</c>) fields of the marker. Nested maps, list items, and
    /// block scalars (such as the embedded <c>prompt</c>) are intentionally skipped so the agent reads only the
    /// authoritative top-level connection fields (baseUrl, apiKey, workspacePath).
    /// </summary>
    private static Dictionary<string, string> ReadTopLevelScalars(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in File.ReadLines(path))
        {
            if (raw.Length == 0 || char.IsWhiteSpace(raw[0]) || raw[0] is '#' or '-')
                continue;

            var separator = raw.IndexOf(':');
            if (separator <= 0)
                continue;

            var key = raw[..separator].Trim();
            var value = raw[(separator + 1)..].Trim();
            if (value.Length == 0 || value[0] is '|' or '>')
                continue;

            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            result.TryAdd(key, value);
        }

        return result;
    }
}
