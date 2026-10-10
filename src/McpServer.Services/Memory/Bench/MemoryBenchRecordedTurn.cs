using System.Text;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-018 / TR-MCP-MEMORY-BENCH-002-02:
/// Shared recorded/stub turn runner used by every plugin adapter.
/// Live mode is opt-in and fail-closed when the host key is missing.
/// </summary>
public static class MemoryBenchRecordedTurn
{
    /// <summary>
    /// Executes one v1 pack cell with the same recorded answers as the Grok pilot.
    /// </summary>
    public static MemoryBenchTurnResult Execute(
        string pluginId,
        string liveEnvironmentVariable,
        MemoryBenchTurnContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentException.ThrowIfNullOrWhiteSpace(liveEnvironmentVariable);
        ArgumentNullException.ThrowIfNull(context);
        if (!string.Equals(context.Plugin, pluginId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Memory bench adapter '{pluginId}' only serves that lane.");

        EnsureLiveKey(pluginId, liveEnvironmentVariable, context.Mode);

        var withMemory = string.Equals(context.Condition, "with_memory", StringComparison.Ordinal);
        var answer = MemoryBenchGrokAdapter.Answer(context.Prompt.Id, withMemory);
        var toolCalls = new List<string>();
        var toolPayload = string.Empty;
        if (withMemory && context.ToolsAvailable)
        {
            toolCalls.Add("memory_recall");
            toolPayload = "memory_recall query=" + context.Prompt.UserText + " results="
                + string.Join(';', context.EffectiveMemories.Select(memory => memory.Content));
        }

        var transcript = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(context.Injection))
        {
            transcript.AppendLine(context.Injection.TrimEnd());
            transcript.AppendLine();
        }

        transcript.AppendLine("user: " + context.Prompt.UserText);
        if (toolCalls.Count > 0)
            transcript.AppendLine("tool: memory_recall");
        transcript.AppendLine("assistant: " + answer);

        return new MemoryBenchTurnResult
        {
            Answer = answer,
            Transcript = transcript.ToString(),
            ToolCalls = toolCalls,
            ToolPayloadText = toolPayload,
            LatencyMs = 1,
            TokenSourceHint = string.Equals(context.Mode, MemoryBenchModes.Recorded, StringComparison.Ordinal)
                ? MemoryBenchTokenSources.Recorded
                : MemoryBenchTokenSources.Estimator,
        };
    }

    /// <summary>
    /// Executes one v2 multi-turn cell with the same recorded answers as the Grok pilot.
    /// </summary>
    public static MemoryBenchTurnResult ExecuteMultiTurn(
        string pluginId,
        string liveEnvironmentVariable,
        MemoryBenchMultiTurnContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentException.ThrowIfNullOrWhiteSpace(liveEnvironmentVariable);
        ArgumentNullException.ThrowIfNull(context);
        if (!string.Equals(context.Plugin, pluginId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Memory bench multi-turn adapter '{pluginId}' only serves that lane.");

        EnsureLiveKey(pluginId, liveEnvironmentVariable, context.Mode);

        var withMemory = string.Equals(context.Condition, "with_memory", StringComparison.Ordinal);
        var answer = MemoryBenchGrokMultiTurnAdapter.Answer(context.Job.Id, context.Turn.Id, withMemory);
        var toolCalls = new List<string>();
        var toolPayload = string.Empty;
        if (withMemory && context.ToolsAvailable && context.Turn.Role == "establish" && context.Turn.WriteMemories)
        {
            toolCalls.Add("memory_remember");
            toolPayload = "memory_remember scope=Workspace content="
                + string.Join(';', context.Turn.ExpectedMemories.Select(memory => memory.Content));
        }
        else if (withMemory && context.ToolsAvailable && context.Turn.Role == "query" && context.EffectiveMemories.Count > 0)
        {
            toolCalls.Add("memory_recall");
            toolPayload = "memory_recall query=" + context.Turn.UserText + " results="
                + string.Join(';', context.EffectiveMemories.Select(memory => memory.Content));
        }

        var transcript = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(context.Injection))
        {
            transcript.AppendLine(context.Injection.TrimEnd());
            transcript.AppendLine();
        }

        transcript.AppendLine("user: " + context.Turn.UserText);
        foreach (var tool in toolCalls)
            transcript.AppendLine("tool: " + tool);
        transcript.AppendLine("assistant: " + answer);

        return new MemoryBenchTurnResult
        {
            Answer = answer,
            Transcript = transcript.ToString(),
            ToolCalls = toolCalls,
            ToolPayloadText = toolPayload,
            LatencyMs = 1,
            TokenSourceHint = string.Equals(context.Mode, MemoryBenchModes.Recorded, StringComparison.Ordinal)
                ? MemoryBenchTokenSources.Recorded
                : MemoryBenchTokenSources.Estimator,
        };
    }

    /// <summary>Throws when live mode is requested without the host credential.</summary>
    public static void EnsureLiveKey(string pluginId, string liveEnvironmentVariable, string mode)
    {
        if (!string.Equals(mode, MemoryBenchModes.Live, StringComparison.Ordinal))
            return;

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(liveEnvironmentVariable)))
        {
            throw new InvalidOperationException(
                $"Live {pluginId} mode requires {liveEnvironmentVariable}; CI must use stub or recorded fixtures.");
        }
    }
}
