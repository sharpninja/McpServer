using McpServer.Cqrs;

namespace McpServer.UI.Core.Tests.TestInfrastructure;

/// <summary>
/// Shared factory for creating CQRS call contexts in tests.
/// </summary>
internal static class CallContextFactory
{
    public static CallContext Create(CancellationToken cancellationToken = default)
        => new()
        {
            CancellationToken = cancellationToken
        };
}
