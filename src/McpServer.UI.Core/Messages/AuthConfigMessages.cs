using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query for <c>/auth/config</c>.</summary>
public sealed record GetAuthConfigQuery : IQuery<AuthConfigSnapshot>;

/// <summary>Public auth configuration snapshot.</summary>
public sealed record AuthConfigSnapshot(
    bool Enabled,
    string? Authority,
    string? ClientId,
    string? Scopes,
    string? DeviceAuthorizationEndpoint,
    string? TokenEndpoint,
    DateTimeOffset RetrievedAt);
