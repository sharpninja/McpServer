using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Options;

/// <summary>
/// TR-MCP-AGENT-004: Validates <see cref="AgentPoolOptions"/> configuration.
/// </summary>
public sealed class AgentPoolOptionsValidator : IValidateOptions<AgentPoolOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AgentPoolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        if (options.Agents.Count == 0)
            return ValidateOptionsResult.Fail("AgentPool requires at least one agent when enabled.");

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var interactiveDefaults = 0;
        var planDefaults = 0;
        var statusDefaults = 0;
        var implementDefaults = 0;

        foreach (var agent in options.Agents)
        {
            if (string.IsNullOrWhiteSpace(agent.AgentName))
                return ValidateOptionsResult.Fail("AgentPool agent entry requires AgentName.");

            if (string.IsNullOrWhiteSpace(agent.AgentPath))
                return ValidateOptionsResult.Fail($"AgentPool agent '{agent.AgentName}' requires AgentPath.");

            if (!seenNames.Add(agent.AgentName))
                return ValidateOptionsResult.Fail($"Duplicate AgentPool AgentName '{agent.AgentName}'.");

            if (agent.IsInteractiveDefault) interactiveDefaults++;
            if (agent.IsTodoPlanDefault) planDefaults++;
            if (agent.IsTodoStatusDefault) statusDefaults++;
            if (agent.IsTodoImplementDefault) implementDefaults++;
        }

        if (interactiveDefaults > 1)
            return ValidateOptionsResult.Fail("AgentPool allows at most one IsInteractiveDefault agent.");
        if (planDefaults > 1)
            return ValidateOptionsResult.Fail("AgentPool allows at most one IsTodoPlanDefault agent.");
        if (statusDefaults > 1)
            return ValidateOptionsResult.Fail("AgentPool allows at most one IsTodoStatusDefault agent.");
        if (implementDefaults > 1)
            return ValidateOptionsResult.Fail("AgentPool allows at most one IsTodoImplementDefault agent.");

        return ValidateOptionsResult.Success;
    }
}
