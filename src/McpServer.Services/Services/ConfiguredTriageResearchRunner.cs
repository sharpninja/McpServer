using McpServer.Common.Copilot;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TRIAGE-003: Direct configured-agent runner for asynchronous triage research.
/// </summary>
internal sealed class ConfiguredTriageResearchRunner(
    IOptions<TriageOptions> options,
    IAgentExecutionStrategyResolver strategyResolver)
    : ITriageResearchRunner
{
    /// <inheritdoc />
    public async Task<TriageResearchRunResult> RunAsync(
        TriageResearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var configured = options.Value;
        if (string.IsNullOrWhiteSpace(configured.AgentPath))
        {
            return new TriageResearchRunResult(false, null, "Triage agent is not configured.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(configured.MaxRunTime);

        var clientOptions = new CopilotClientOptions
        {
            AgentPath = configured.AgentPath.Trim(),
            Model = string.IsNullOrWhiteSpace(configured.AgentModel) ? "gpt-5.3-codex" : configured.AgentModel.Trim(),
            Silent = true,
            Timeout = configured.MaxRunTime,
            WorkingDirectory = request.WorkspacePath,
        };

        foreach (var pair in configured.AgentParameters)
        {
            clientOptions.EnvironmentVariables[pair.Key] = pair.Value;
        }

        var strategy = strategyResolver.Resolve(configured.ExecutionStrategy);
        await using var session = await strategy.CreateSessionAsync(
            new AgentExecutionSessionRequest(
                request.Prompt,
                request.WorkspacePath,
                configured.AgentName,
                configured.ExecutionStrategy,
                clientOptions),
            timeout.Token).ConfigureAwait(false);

        var result = await session.ReadInitialResponseAsync(timeout.Token).ConfigureAwait(false);
        await session.EndAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        return result.State == CopilotResultState.Success
            ? new TriageResearchRunResult(true, result.Body, null, result.Stdout, result.Stderr, result.ExitCode)
            : new TriageResearchRunResult(false, result.Body, result.Stderr, result.Stdout, result.Stderr, result.ExitCode);
    }
}
