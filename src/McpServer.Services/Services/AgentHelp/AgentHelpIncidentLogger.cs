using System.Text.Json;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services.AgentHelp;

/// <summary>
/// FR-MCP-HELP-004: Persists guard incidents as JSON files for Agent Help sessions.
/// TR-MCP-HELP-005: Writes one incident file per blocked inbound message.
/// </summary>
public sealed class AgentHelpIncidentLogger
{
    private readonly IOptionsMonitor<AgentHelpOptions> _options;
    private readonly ILogger<AgentHelpIncidentLogger> _logger;

    /// <summary>
    /// TR-MCP-HELP-005: Creates a new incident logger.
    /// </summary>
    public AgentHelpIncidentLogger(
        IOptionsMonitor<AgentHelpOptions> options,
        ILogger<AgentHelpIncidentLogger> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// FR-MCP-HELP-004: Writes a guard incident record to disk.
    /// </summary>
    public async Task<AgentHelpIncidentRecord> WriteAsync(
        string workspaceDataRoot,
        AgentHelpIncidentRecord incident,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceDataRoot);
        ArgumentNullException.ThrowIfNull(incident);
        cancellationToken.ThrowIfCancellationRequested();

        var incidentDir = Path.Combine(workspaceDataRoot, _options.CurrentValue.IncidentDirectory);
        Directory.CreateDirectory(incidentDir);

        var fileName = $"{incident.TimestampUtc.Replace(":", "-", StringComparison.Ordinal)}-{incident.IncidentId}.json";
        var filePath = Path.Combine(incidentDir, fileName);
        var json = JsonSerializer.Serialize(incident, AgentHelpJsonContext.Default.AgentHelpIncidentRecord);
        await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);

        _logger.LogWarning(
            "Recorded Agent Help guard incident: Incident={IncidentId}; Session={SessionId}; Rule={RuleId}",
            incident.IncidentId,
            incident.SessionId,
            incident.RuleId);

        return incident;
    }

    /// <summary>
    /// FR-MCP-HELP-004: Reads all incident records for a session.
    /// </summary>
    public async Task<IReadOnlyList<AgentHelpIncidentRecord>> ReadBySessionAsync(
        string workspaceDataRoot,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceDataRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        cancellationToken.ThrowIfCancellationRequested();

        var incidentDir = Path.Combine(workspaceDataRoot, _options.CurrentValue.IncidentDirectory);
        if (!Directory.Exists(incidentDir))
            return [];

        var results = new List<AgentHelpIncidentRecord>();
        foreach (var filePath in Directory.EnumerateFiles(incidentDir, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                var incident = JsonSerializer.Deserialize(json, AgentHelpJsonContext.Default.AgentHelpIncidentRecord);
                if (incident is not null
                    && string.Equals(incident.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(incident);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Skipped invalid Agent Help incident file {FilePath}", filePath);
            }
        }

        return results;
    }
}