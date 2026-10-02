namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-MEMORY-BENCH-002-02: Documented adapter interface used by the harness.
/// Hosts plug in via bats/ts/REPL or a recorded/stub entrypoint.
/// </summary>
public interface IMemoryBenchPluginAdapter
{
    /// <summary>Plugin id such as grok.</summary>
    string PluginId { get; }

    /// <summary>Supported validation entrypoint (bats, ts, REPL, or recorded-fixture).</summary>
    string ValidationEntrypoint { get; }

    /// <summary>Executes one pack cell. Must not require cloud in stub/recorded mode.</summary>
    MemoryBenchTurnResult Execute(MemoryBenchTurnContext context);
}

/// <summary>
/// FR-MCP-MEMORY-018-07 / FR-MCP-MEMORY-018-17..23:
/// Resolves adapters. Grok is the default; the other seven require H7a AGREE.
/// </summary>
public static class MemoryBenchAdapterRegistry
{
    /// <summary>Default required plugin.</summary>
    public const string DefaultPlugin = "grok";

    /// <summary>All eight pack plugins in checklist order (Grok first).</summary>
    public static readonly string[] AllPluginIds =
    [
        "grok",
        "claude-code",
        "claude-cowork",
        "cline",
        "cline-v2",
        "codex",
        "copilot",
        "opencode",
    ];

    /// <summary>Creates the default Grok adapter.</summary>
    public static IMemoryBenchPluginAdapter CreateGrok() => new MemoryBenchGrokAdapter();

    /// <summary>Creates the recorded/stub adapter for a known plugin id.</summary>
    public static IMemoryBenchPluginAdapter Create(string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        return pluginId.Trim().ToLowerInvariant() switch
        {
            "grok" => CreateGrok(),
            "claude-code" => new MemoryBenchClaudeCodeAdapter(),
            "claude-cowork" => new MemoryBenchClaudeCoworkAdapter(),
            "cline" => new MemoryBenchClineAdapter(),
            "cline-v2" => new MemoryBenchClineV2Adapter(),
            "codex" => new MemoryBenchCodexAdapter(),
            "copilot" => new MemoryBenchCopilotAdapter(),
            "opencode" => new MemoryBenchOpencodeAdapter(),
            _ => throw new InvalidOperationException($"Unknown MemoryBench plugin '{pluginId}'."),
        };
    }

    /// <summary>Resolves adapters for the requested plugins.</summary>
    public static IReadOnlyDictionary<string, IMemoryBenchPluginAdapter> Resolve(
        IReadOnlyList<string> plugins,
        IReadOnlyDictionary<string, IMemoryBenchPluginAdapter>? overrides = null)
    {
        ArgumentNullException.ThrowIfNull(plugins);
        var map = new Dictionary<string, IMemoryBenchPluginAdapter>(StringComparer.OrdinalIgnoreCase);
        foreach (var plugin in plugins)
        {
            if (overrides is not null && overrides.TryGetValue(plugin, out var over))
            {
                map[plugin] = over;
                continue;
            }

            if (string.Equals(plugin, DefaultPlugin, StringComparison.OrdinalIgnoreCase))
            {
                map[plugin] = CreateGrok();
                continue;
            }

            if (!MemoryBenchValueGate.IsH7aAgreed())
            {
                throw new InvalidOperationException(
                    $"Plugin '{plugin}' is deferred until H7a AGREE. Default/required adapter is grok.");
            }

            map[plugin] = Create(plugin);
        }

        return map;
    }
}
