using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>Host-provided API abstraction for context endpoints.</summary>
public interface IContextApiClient
{
    /// <summary>Searches indexed context.</summary>
    Task<ContextSearchPayload> SearchAsync(SearchContextQuery query, CancellationToken cancellationToken = default);

    /// <summary>Builds a deterministic context pack.</summary>
    Task<ContextPackPayload> PackAsync(PackContextQuery query, CancellationToken cancellationToken = default);

    /// <summary>Lists indexed sources.</summary>
    Task<ContextSourcesPayload> ListSourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>Triggers an index rebuild.</summary>
    Task<ContextRebuildResult> RebuildIndexAsync(CancellationToken cancellationToken = default);
}
