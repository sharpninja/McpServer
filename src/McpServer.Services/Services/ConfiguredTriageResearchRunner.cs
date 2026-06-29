using McpServer.Common.AgentCli;
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
    private const string CodexTimeoutMessage = "Codex CLI triage run was cancelled or timed out.";
    private const string OneShotTimeoutMessage = "One-shot CLI agent run was cancelled or timed out.";

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

        var clientOptions = new AgentCliClientOptions
        {
            AgentPath = configured.AgentPath.Trim(),
            Model = string.IsNullOrWhiteSpace(configured.AgentModel) ? "auto" : configured.AgentModel.Trim(),
            Silent = true,
            Timeout = configured.MaxRunTime,
            WorkingDirectory = request.WorkspacePath,
            AgentOutputReceivedAsync = request.OutputReceivedAsync is null
                ? null
                : (streamName, text) => request.OutputReceivedAsync(new TriageResearchOutputUpdate(streamName, text)),
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

        return result.State == AgentCliResultState.Success
            ? new TriageResearchRunResult(true, result.Body, null, result.Stdout, result.Stderr, result.ExitCode)
            : new TriageResearchRunResult(false, result.Body, BuildFailureError(result), result.Stdout, result.Stderr, result.ExitCode);
    }

    private static string BuildFailureError(AgentCliResult result)
    {
        if (result.State == AgentCliResultState.Timeout ||
            ContainsFailureText(result.Stderr, CodexTimeoutMessage) ||
            ContainsFailureText(result.Body, CodexTimeoutMessage))
        {
            return CodexTimeoutMessage;
        }

        if (ContainsFailureText(result.Stderr, OneShotTimeoutMessage) ||
            ContainsFailureText(result.Body, OneShotTimeoutMessage))
        {
            return OneShotTimeoutMessage;
        }

        if (result.State == AgentCliResultState.SpawnError)
            return "Triage agent could not be started. See captured agent output for details.";

        var exitCode = result.ExitCode is null
            ? string.Empty
            : $" with exit code {result.ExitCode.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return $"Triage agent failed with state {result.State}{exitCode}. See captured agent output for details.";
    }

    private static bool ContainsFailureText(string? value, string expected)
        => !string.IsNullOrWhiteSpace(value) &&
           value.Contains(expected, StringComparison.OrdinalIgnoreCase);
}
