namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 AC3: strict JSON semantic evaluation of a redacted persistence receipt.
/// </summary>
public sealed class AiStrategyEvaluation
{
    /// <summary>True when the receipt is semantically complete and valid.</summary>
    public required bool Valid { get; init; }

    /// <summary>Required workflow fields missing from the receipt.</summary>
    public required IReadOnlyList<string> MissingFields { get; init; }

    /// <summary>Contradictions between the receipt and deterministic evidence.</summary>
    public required IReadOnlyList<string> Contradictions { get; init; }
}
