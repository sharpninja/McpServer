using System.Text;
using McpServer.Support.Mcp.Storage.Entities;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Resolves templated agent launch commands into executable command lines.
/// </summary>
public static class AgentProcessCommandResolver
{
    /// <summary>
    /// Resolves a launch command by replacing supported template variables.
    /// </summary>
    /// <param name="commandTemplate">The launch command template.</param>
    /// <param name="workspacePath">The effective workspace path.</param>
    /// <param name="agentId">The logical agent identifier.</param>
    /// <param name="branchName">The effective branch name.</param>
    /// <param name="modelList">The effective model list.</param>
    /// <param name="seedPrompt">The effective seed prompt.</param>
    /// <returns>The resolved launch command.</returns>
    public static string Resolve(
        string commandTemplate,
        string workspacePath,
        string agentId,
        string? branchName,
        IReadOnlyCollection<string>? modelList,
        string? seedPrompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandTemplate);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        return commandTemplate
            .Replace("{workspacePath}", workspacePath, StringComparison.Ordinal)
            .Replace("{agentId}", agentId, StringComparison.Ordinal)
            .Replace("{branchName}", branchName ?? string.Empty, StringComparison.Ordinal)
            .Replace("{modelList}", modelList is { Count: > 0 } ? string.Join(",", modelList) : string.Empty, StringComparison.Ordinal)
            .Replace("{seedPrompt}", seedPrompt ?? string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// Resolves the effective launch command from workspace configuration and definition defaults.
    /// </summary>
    /// <param name="workspaceConfig">Workspace-specific agent configuration.</param>
    /// <param name="definition">Global agent definition.</param>
    /// <param name="workspacePath">The effective workspace path.</param>
    /// <param name="branchName">The effective branch name.</param>
    /// <returns>The resolved launch command.</returns>
    public static string ResolveEffectiveCommand(
        AgentWorkspaceEntity workspaceConfig,
        AgentDefinitionEntity definition,
        string workspacePath,
        string? branchName)
    {
        ArgumentNullException.ThrowIfNull(workspaceConfig);
        ArgumentNullException.ThrowIfNull(definition);

        var template = !string.IsNullOrWhiteSpace(workspaceConfig.LaunchCommandOverride)
            ? workspaceConfig.LaunchCommandOverride
            : definition.DefaultLaunchCommand;

        if (string.IsNullOrWhiteSpace(template))
            throw new InvalidOperationException($"Agent '{definition.Id}' does not have a launch command configured.");

        var overrideModels = workspaceConfig.ListItems
            .Where(r => r.ListType == "ModelOverride")
            .OrderBy(r => r.Ordinal)
            .Select(r => r.Value)
            .ToList();
        var models = overrideModels.Count > 0
            ? overrideModels
            : definition.Models.OrderBy(m => m.Ordinal).Select(m => m.Model).ToList();

        var seedPrompt = !string.IsNullOrWhiteSpace(workspaceConfig.SeedPromptOverride)
            ? workspaceConfig.SeedPromptOverride
            : definition.DefaultSeedPrompt;

        return Resolve(template, workspacePath, definition.Id, branchName, models, seedPrompt);
    }

    /// <summary>
    /// Splits a resolved command line into executable and argument portions.
    /// </summary>
    /// <param name="resolvedCommand">The resolved command line.</param>
    /// <returns>The executable path/name and raw argument string.</returns>
    public static (string FileName, string Arguments) SplitCommand(string resolvedCommand)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedCommand);

        var trimmed = resolvedCommand.Trim();
        if (trimmed.StartsWith('"'))
        {
            var closingQuoteIndex = trimmed.IndexOf('"', 1);
            if (closingQuoteIndex < 0)
                throw new InvalidOperationException("Resolved command contains an unterminated quoted executable path.");

            var fileName = trimmed[1..closingQuoteIndex];
            var arguments = trimmed[(closingQuoteIndex + 1)..].TrimStart();
            return (fileName, arguments);
        }

        var parts = trimmed.Split(' ', 2, StringSplitOptions.TrimEntries);
        return (parts[0], parts.Length > 1 ? parts[1] : string.Empty);
    }

}
