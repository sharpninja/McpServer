using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query for <c>/mcpserver/tunnel/list</c>.</summary>
public sealed record ListTunnelsQuery : IQuery<TunnelListSnapshot>;

/// <summary>Command for <c>/mcpserver/tunnel/{name}/enable</c>.</summary>
public sealed record EnableTunnelCommand(string ProviderName) : ICommand<TunnelProviderSnapshot>;

/// <summary>Command for <c>/mcpserver/tunnel/{name}/disable</c>.</summary>
public sealed record DisableTunnelCommand(string ProviderName) : ICommand<TunnelProviderSnapshot>;

/// <summary>Command for <c>/mcpserver/tunnel/{name}/start</c>.</summary>
public sealed record StartTunnelCommand(string ProviderName) : ICommand<TunnelProviderSnapshot>;

/// <summary>Command for <c>/mcpserver/tunnel/{name}/stop</c>.</summary>
public sealed record StopTunnelCommand(string ProviderName) : ICommand<TunnelProviderSnapshot>;

/// <summary>Command for <c>/mcpserver/tunnel/{name}/restart</c>.</summary>
public sealed record RestartTunnelCommand(string ProviderName) : ICommand<TunnelProviderSnapshot>;

/// <summary>Snapshot of a single tunnel provider.</summary>
public sealed record TunnelProviderSnapshot(
    string Provider,
    bool Enabled,
    bool IsRunning,
    string? PublicUrl,
    string? Error);

/// <summary>Snapshot of all tunnel providers.</summary>
public sealed record TunnelListSnapshot(IReadOnlyList<TunnelProviderSnapshot> Providers);
