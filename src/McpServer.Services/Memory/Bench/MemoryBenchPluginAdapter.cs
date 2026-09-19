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
/// FR-MCP-MEMORY-018-07: Resolves adapters. Grok is required; others are opt-in after H7a.
/// </summary>
public static class MemoryBenchAdapterRegistry
{
    /// <summary>Default required plugin.</summary>
    public const string DefaultPlugin = "grok";

    /// <summary>Creates the default Grok adapter.</summary>
    public static IMemoryBenchPluginAdapter CreateGrok() => new MemoryBenchGrokAdapter();

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

            throw new InvalidOperationException(
                $"Plugin '{plugin}' is deferred until H7a AGREE. Default/required adapter is grok.");
        }

        return map;
    }
}
