using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Services.AgentHelp;

/// <summary>
/// TEST-MCP-HELP-004: Simple options monitor for Agent Help unit tests.
/// </summary>
internal sealed class AgentHelpTestOptionsMonitor<T> : IOptionsMonitor<T>
    where T : class
{
    /// <summary>Creates a monitor that always returns the supplied value.</summary>
    public AgentHelpTestOptionsMonitor(T value) => CurrentValue = value;

    /// <inheritdoc />
    public T CurrentValue { get; }

    /// <inheritdoc />
    public T Get(string? name) => CurrentValue;

    /// <inheritdoc />
    public IDisposable? OnChange(Action<T, string?> listener) => null;
}