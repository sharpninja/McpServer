namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-MEMORY-018-17: Claude Code recorded/stub adapter.</summary>
public sealed class MemoryBenchClaudeCodeAdapter : IMemoryBenchPluginAdapter
{
    /// <inheritdoc />
    public string PluginId => "claude-code";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchTurnContext context)
        => MemoryBenchRecordedTurn.Execute(PluginId, "ANTHROPIC_API_KEY", context);
}

/// <summary>FR-MCP-MEMORY-018-18: Claude Cowork recorded/stub adapter.</summary>
public sealed class MemoryBenchClaudeCoworkAdapter : IMemoryBenchPluginAdapter
{
    /// <inheritdoc />
    public string PluginId => "claude-cowork";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchTurnContext context)
        => MemoryBenchRecordedTurn.Execute(PluginId, "ANTHROPIC_API_KEY", context);
}

/// <summary>FR-MCP-MEMORY-018-19: Cline recorded/stub adapter.</summary>
public sealed class MemoryBenchClineAdapter : IMemoryBenchPluginAdapter
{
    /// <inheritdoc />
    public string PluginId => "cline";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchTurnContext context)
        => MemoryBenchRecordedTurn.Execute(PluginId, "CLINE_API_KEY", context);
}

/// <summary>FR-MCP-MEMORY-018-20: Cline v2 recorded/stub adapter.</summary>
public sealed class MemoryBenchClineV2Adapter : IMemoryBenchPluginAdapter
{
    /// <inheritdoc />
    public string PluginId => "cline-v2";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchTurnContext context)
        => MemoryBenchRecordedTurn.Execute(PluginId, "CLINE_API_KEY", context);
}

/// <summary>FR-MCP-MEMORY-018-21: Codex recorded/stub adapter.</summary>
public sealed class MemoryBenchCodexAdapter : IMemoryBenchPluginAdapter
{
    /// <inheritdoc />
    public string PluginId => "codex";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchTurnContext context)
        => MemoryBenchRecordedTurn.Execute(PluginId, "OPENAI_API_KEY", context);
}

/// <summary>FR-MCP-MEMORY-018-22: Copilot recorded/stub adapter.</summary>
public sealed class MemoryBenchCopilotAdapter : IMemoryBenchPluginAdapter
{
    /// <inheritdoc />
    public string PluginId => "copilot";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchTurnContext context)
        => MemoryBenchRecordedTurn.Execute(PluginId, "COPILOT_GITHUB_TOKEN", context);
}

/// <summary>FR-MCP-MEMORY-018-23: OpenCode recorded/stub adapter.</summary>
public sealed class MemoryBenchOpencodeAdapter : IMemoryBenchPluginAdapter
{
    /// <inheritdoc />
    public string PluginId => "opencode";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchTurnContext context)
        => MemoryBenchRecordedTurn.Execute(PluginId, "OPENCODE_API_KEY", context);
}

/// <summary>FR-MCP-MEMORY-019: Claude Code multi-turn recorded/stub adapter.</summary>
public sealed class MemoryBenchClaudeCodeMultiTurnAdapter : IMemoryBenchMultiTurnAdapter
{
    /// <inheritdoc />
    public string PluginId => "claude-code";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchMultiTurnContext context)
        => MemoryBenchRecordedTurn.ExecuteMultiTurn(PluginId, "ANTHROPIC_API_KEY", context);
}

/// <summary>FR-MCP-MEMORY-019: Claude Cowork multi-turn recorded/stub adapter.</summary>
public sealed class MemoryBenchClaudeCoworkMultiTurnAdapter : IMemoryBenchMultiTurnAdapter
{
    /// <inheritdoc />
    public string PluginId => "claude-cowork";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchMultiTurnContext context)
        => MemoryBenchRecordedTurn.ExecuteMultiTurn(PluginId, "ANTHROPIC_API_KEY", context);
}

/// <summary>FR-MCP-MEMORY-019: Cline multi-turn recorded/stub adapter.</summary>
public sealed class MemoryBenchClineMultiTurnAdapter : IMemoryBenchMultiTurnAdapter
{
    /// <inheritdoc />
    public string PluginId => "cline";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchMultiTurnContext context)
        => MemoryBenchRecordedTurn.ExecuteMultiTurn(PluginId, "CLINE_API_KEY", context);
}

/// <summary>FR-MCP-MEMORY-019: Cline v2 multi-turn recorded/stub adapter.</summary>
public sealed class MemoryBenchClineV2MultiTurnAdapter : IMemoryBenchMultiTurnAdapter
{
    /// <inheritdoc />
    public string PluginId => "cline-v2";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchMultiTurnContext context)
        => MemoryBenchRecordedTurn.ExecuteMultiTurn(PluginId, "CLINE_API_KEY", context);
}

/// <summary>FR-MCP-MEMORY-019: Codex multi-turn recorded/stub adapter.</summary>
public sealed class MemoryBenchCodexMultiTurnAdapter : IMemoryBenchMultiTurnAdapter
{
    /// <inheritdoc />
    public string PluginId => "codex";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchMultiTurnContext context)
        => MemoryBenchRecordedTurn.ExecuteMultiTurn(PluginId, "OPENAI_API_KEY", context);
}

/// <summary>FR-MCP-MEMORY-019: Copilot multi-turn recorded/stub adapter.</summary>
public sealed class MemoryBenchCopilotMultiTurnAdapter : IMemoryBenchMultiTurnAdapter
{
    /// <inheritdoc />
    public string PluginId => "copilot";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchMultiTurnContext context)
        => MemoryBenchRecordedTurn.ExecuteMultiTurn(PluginId, "COPILOT_GITHUB_TOKEN", context);
}

/// <summary>FR-MCP-MEMORY-019: OpenCode multi-turn recorded/stub adapter.</summary>
public sealed class MemoryBenchOpencodeMultiTurnAdapter : IMemoryBenchMultiTurnAdapter
{
    /// <inheritdoc />
    public string PluginId => "opencode";

    /// <inheritdoc />
    public string ValidationEntrypoint => "recorded-fixture";

    /// <inheritdoc />
    public MemoryBenchTurnResult Execute(MemoryBenchMultiTurnContext context)
        => MemoryBenchRecordedTurn.ExecuteMultiTurn(PluginId, "OPENCODE_API_KEY", context);
}
