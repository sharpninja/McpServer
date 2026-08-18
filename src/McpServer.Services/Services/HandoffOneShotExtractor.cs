using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-AGENT-001: One-shot extraction adapter.</summary>
public interface IHandoffOneShotExtractor
{
    /// <summary>Invoke the existing one-shot agent system with HandoffTodoDraft context.</summary>
    Task<HandoffExtractionResult> ExtractAsync(
        string workspacePath,
        string handoffText,
        string? agentName,
        string? promptTemplateId,
        CancellationToken cancellationToken);
}

/// <inheritdoc />
public sealed class HandoffOneShotExtractor : IHandoffOneShotExtractor
{
    private readonly IAgentPoolService _agentPoolService;

    /// <summary>TR-HANDOFF-AGENT-001: Constructor.</summary>
    public HandoffOneShotExtractor(IAgentPoolService agentPoolService)
    {
        _agentPoolService = agentPoolService ?? throw new ArgumentNullException(nameof(agentPoolService));
    }

    /// <inheritdoc />
    public async Task<HandoffExtractionResult> ExtractAsync(
        string workspacePath,
        string handoffText,
        string? agentName,
        string? promptTemplateId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = new AgentPoolOneShotRequest
        {
            AgentName = agentName,
            WorkspacePath = workspacePath,
            Context = AgentPoolOneShotContext.HandoffTodoDraft,
            PromptTemplateId = string.IsNullOrWhiteSpace(promptTemplateId) ? null : promptTemplateId,
            Id = "handoff-draft",
            Values = new Dictionary<string, object?>
            {
                ["handoffText"] = handoffText,
            },
            UseWorkspaceContext = true,
        };

        var enqueue = await _agentPoolService.EnqueueOneShotAsync(request, cancellationToken).ConfigureAwait(false);
        if (!enqueue.Success || string.IsNullOrWhiteSpace(enqueue.JobId))
        {
            return new HandoffExtractionResult
            {
                Success = false,
                AgentName = enqueue.AgentName,
                PromptVersion = HandoffPromptDefaults.PromptVersion,
                TemplateVersion = promptTemplateId ?? HandoffPromptDefaults.TemplateId,
                Error = enqueue.Error ?? "The one-shot extractor could not be queued.",
            };
        }

        await foreach (var evt in _agentPoolService.SubscribeJobStreamAsync(enqueue.JobId, cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(evt.EventType, "completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(evt.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                return new HandoffExtractionResult
                {
                    Success = true,
                    ResponseText = evt.Text,
                    AgentName = enqueue.AgentName,
                    Model = enqueue.Model ?? evt.Model,
                    PromptVersion = HandoffPromptDefaults.PromptVersion,
                    TemplateVersion = promptTemplateId ?? HandoffPromptDefaults.TemplateId,
                };
            }

            if (string.Equals(evt.EventType, "failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(evt.EventType, "canceled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(evt.Status, "failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(evt.Status, "canceled", StringComparison.OrdinalIgnoreCase))
            {
                return new HandoffExtractionResult
                {
                    Success = false,
                    AgentName = enqueue.AgentName,
                    PromptVersion = HandoffPromptDefaults.PromptVersion,
                    TemplateVersion = promptTemplateId ?? HandoffPromptDefaults.TemplateId,
                    Error = string.IsNullOrWhiteSpace(evt.Error) ? "The one-shot extractor failed." : evt.Error,
                };
            }
        }

        return new HandoffExtractionResult
        {
            Success = false,
            AgentName = enqueue.AgentName,
            PromptVersion = HandoffPromptDefaults.PromptVersion,
            TemplateVersion = promptTemplateId ?? HandoffPromptDefaults.TemplateId,
            Error = "The one-shot extractor ended without a terminal result.",
        };
    }
}

/// <summary>
/// TR-HANDOFF-AGENT-001: Fallback extractor used when <see cref="IAgentPoolService"/> is not registered.
/// Keeps handoff DI constructible so unrelated stdio tools do not fail closed.
/// </summary>
internal sealed class UnavailableHandoffOneShotExtractor : IHandoffOneShotExtractor
{
    /// <inheritdoc />
    public Task<HandoffExtractionResult> ExtractAsync(
        string workspacePath,
        string handoffText,
        string? agentName,
        string? promptTemplateId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = workspacePath;
        _ = handoffText;
        return Task.FromResult(new HandoffExtractionResult
        {
            Success = false,
            AgentName = agentName,
            PromptVersion = HandoffPromptDefaults.PromptVersion,
            TemplateVersion = promptTemplateId ?? HandoffPromptDefaults.TemplateId,
            Error = "Agent pool is not registered in this host, so handoff extraction is unavailable.",
        });
    }
}
