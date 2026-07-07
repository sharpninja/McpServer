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

        // FR-MCP-TRIAGE-006: run the primary strategy, then advance to the secondary and tertiary
        // strategies when a tier fails with a retryable API error (4xx/rate-limit/unavailable) or times out.
        var tiers = BuildTiers(configured);
        TriageResearchRunResult? lastFailure = null;
        for (var i = 0; i < tiers.Count; i++)
        {
            var (result, timedOut) = await RunTierAsync(tiers[i], request, configured, cancellationToken).ConfigureAwait(false);
            if (result.State == AgentCliResultState.Success)
            {
                return new TriageResearchRunResult(true, result.Body, null, result.Stdout, result.Stderr, result.ExitCode);
            }

            lastFailure = new TriageResearchRunResult(false, result.Body, BuildFailureError(result), result.Stdout, result.Stderr, result.ExitCode);

            var hasNextTier = i < tiers.Count - 1;
            if (hasNextTier && TriageFallbackClassifier.ShouldFallback(result, configured, timedOut))
            {
                continue;
            }

            return lastFailure;
        }

        return lastFailure ?? new TriageResearchRunResult(false, null, "Triage agent is not configured.");
    }

    private async Task<(AgentCliResult Result, bool TimedOut)> RunTierAsync(
        TriageAgentTier tier,
        TriageResearchRequest request,
        TriageOptions configured,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(configured.MaxRunTime);

        var clientOptions = new AgentCliClientOptions
        {
            AgentPath = tier.AgentPath.Trim(),
            Model = string.IsNullOrWhiteSpace(tier.AgentModel) ? "auto" : tier.AgentModel.Trim(),
            Silent = true,
            Timeout = configured.MaxRunTime,
            WorkingDirectory = request.WorkspacePath,
            AgentOutputReceivedAsync = request.OutputReceivedAsync is null
                ? null
                : (streamName, text) => request.OutputReceivedAsync(new TriageResearchOutputUpdate(streamName, text)),
        };

        foreach (var pair in tier.AgentParameters)
        {
            clientOptions.EnvironmentVariables[pair.Key] = pair.Value;
        }

        var strategy = strategyResolver.Resolve(tier.ExecutionStrategy);
        await using var session = await strategy.CreateSessionAsync(
            new AgentExecutionSessionRequest(
                request.Prompt,
                request.WorkspacePath,
                tier.AgentName,
                tier.ExecutionStrategy,
                clientOptions),
            timeout.Token).ConfigureAwait(false);

        var result = await session.ReadInitialResponseAsync(timeout.Token).ConfigureAwait(false);
        await session.EndAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        var timedOut = timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
        return (result, timedOut);
    }

    private static IReadOnlyList<TriageAgentTier> BuildTiers(TriageOptions configured)
    {
        var tiers = new List<TriageAgentTier>
        {
            new(
                configured.ExecutionStrategy,
                configured.AgentPath!,
                configured.AgentModel,
                configured.AgentName,
                configured.AgentParameters),
        };

        AppendTier(tiers, configured.Secondary);
        AppendTier(tiers, configured.Tertiary);
        return tiers;
    }

    private static void AppendTier(List<TriageAgentTier> tiers, TriageFallbackAgent? agent)
    {
        if (agent is not null && !string.IsNullOrWhiteSpace(agent.AgentPath))
        {
            tiers.Add(new(
                agent.ExecutionStrategy,
                agent.AgentPath!.Trim(),
                agent.AgentModel,
                agent.AgentName,
                agent.AgentParameters));
        }
    }

    private sealed record TriageAgentTier(
        string ExecutionStrategy,
        string AgentPath,
        string AgentModel,
        string? AgentName,
        Dictionary<string, string> AgentParameters);

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
