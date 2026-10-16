namespace QBrainAi.Common.AgentCli;

/// <summary>
/// TR-MCP-QBRAIN-005: Reads product environment variables with 1.x alias precedence.
/// QBRAINAI_* wins when set. The matching MCP_* variable is used when the new name is unset.
/// MCP_UNTRUSTED stays a literal sentinel and is not aliased.
/// </summary>
public static class ProductEnvironment
{
    /// <summary>The trust-failure sentinel. This literal does not change through 1.x.</summary>
    public const string UntrustedSentinel = "MCP_UNTRUSTED";

    /// <summary>
    /// Reads a legacy MCP_* variable, preferring QBRAINAI_* with the same suffix.
    /// </summary>
    /// <param name="legacyName">The existing variable name, including the MCP_ prefix.</param>
    /// <returns>The winning value, or null when neither variable is set.</returns>
    public static string? GetLegacy(string legacyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legacyName);
        if (string.Equals(legacyName, UntrustedSentinel, StringComparison.Ordinal))
            return Environment.GetEnvironmentVariable(UntrustedSentinel);

        if (legacyName.StartsWith("MCP_", StringComparison.Ordinal))
        {
            var modernName = "QBRAINAI_" + legacyName["MCP_".Length..];
            var modern = Environment.GetEnvironmentVariable(modernName);
            if (!string.IsNullOrWhiteSpace(modern))
                return modern;
        }

        return Environment.GetEnvironmentVariable(legacyName);
    }
}
