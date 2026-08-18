using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>TR-HANDOFF-SURFACE-001: Production workflow.handoff wrapper that delegates to HandoffClient.</summary>
public sealed class HandoffWorkflow : IHandoffWorkflow
{
    private readonly HandoffClient _client;

    /// <summary>Initializes a new instance of the <see cref="HandoffWorkflow"/> class.</summary>
    public HandoffWorkflow(HandoffClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public Task<HandoffIngestionResult> IngestAsync(HandoffIngestionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _client.IngestHandoffAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<HandoffIngestionResult> GetAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Run id is required.", nameof(runId));
        return _client.GetHandoffRunAsync(runId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<HandoffIngestionResult> ApproveAsync(string runId, HandoffApprovalRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Run id is required.", nameof(runId));
        ArgumentNullException.ThrowIfNull(request);
        return _client.ApproveHandoffAsync(runId, request, cancellationToken);
    }
}
