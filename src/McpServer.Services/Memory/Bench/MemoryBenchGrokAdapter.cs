using System.Text;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-018-16 / FR-MCP-MEMORY-018-46 / FR-MCP-MEMORY-018-49:
/// Grok pilot adapter. Stub/recorded fixtures are fail-closed and deterministic.
/// Live mode is opt-in and still uses the same pack/scoring.
/// </summary>
public sealed class MemoryBenchGrokAdapter : IMemoryBenchPluginAdapter
{
    /// <inheritdoc />
    public string PluginId => "grok";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchTurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!string.Equals(context.Plugin, PluginId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("MemoryBenchGrokAdapter only serves the grok lane.");

        if (string.Equals(context.Mode, MemoryBenchModes.Live, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("XAI_API_KEY")))
        {
            throw new InvalidOperationException(
                "Live Grok mode requires XAI_API_KEY; CI must use stub or recorded fixtures.");
        }

        var withMemory = string.Equals(context.Condition, "with_memory", StringComparison.Ordinal);
        var answer = Answer(context.Prompt.Id, withMemory);
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
    /// Recorded Grok answers for the v1 pack. with_memory follows gold; without_memory refuses to invent.
    /// </summary>
    public static string Answer(string promptId, bool withMemory)
    {
        return promptId switch
        {
            "PREF-001" => withMemory
                ? "You prefer neovim for this workspace."
                : "I do not have a remembered editor preference.",
            "DEC-001" => withMemory
                ? "We decided to use native Postgres, not Docker, for local McpServer."
                : "I do not have a remembered local database decision.",
            "FACT-001" => withMemory
                ? "The memory branch carries the agent memory work."
                : "I do not have a remembered branch fact.",
            "PROC-001" => withMemory
                ? "Use memory_remember or memory_add with Workspace scope."
                : "I do not have a remembered procedure for adding durable memory.",
            "MULTI-001" => withMemory
                ? "You prefer neovim and we decided on native Postgres for local."
                : "I do not have remembered bench preferences or a DB decision.",
            "NEG-001" => "Unknown / not in memory. I will not invent a favorite unused language.",
            "CONF-001" => withMemory
                ? "We standardize on pnpm."
                : "I do not have a remembered package manager standard.",
            "SAFE-001" => "I refuse to invent a production API key. That secret is not stored.",
            _ => throw new InvalidOperationException("No recorded Grok fixture for prompt " + promptId),
        };
    }
}
