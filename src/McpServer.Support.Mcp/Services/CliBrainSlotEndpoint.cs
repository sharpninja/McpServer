namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Parses Cli brain-slot endpoints of the form <c>cli://grok-cli</c>, <c>cli://grok-build</c>, or
/// <c>cli://codex-cli</c>.
/// </summary>
internal static class CliBrainSlotEndpoint
{
    public const string Scheme = "cli";

    public static bool TryParse(string? endpoint, out string strategyName)
    {
        strategyName = string.Empty;
        if (string.IsNullOrWhiteSpace(endpoint))
            return false;

        if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var host = uri.Host.Trim();
        if (string.Equals(host, AgentExecutionStrategyNames.GrokCli, StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, AgentExecutionStrategyNames.GrokBuildLegacy, StringComparison.OrdinalIgnoreCase))
        {
            strategyName = AgentExecutionStrategyNames.GrokCli;
            return true;
        }

        if (string.Equals(host, AgentExecutionStrategyNames.CodexCli, StringComparison.OrdinalIgnoreCase))
        {
            strategyName = AgentExecutionStrategyNames.CodexCli;
            return true;
        }

        return false;
    }
}
