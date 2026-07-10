using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Default REPL transcript workflow backed by <see cref="SessionLogClient"/>.
/// </summary>
public sealed class TranscriptIngestionWorkflow : ITranscriptIngestionWorkflow
{
    private readonly SessionLogClient _sessionLogClient;

    /// <summary>
    /// Initializes a transcript ingestion workflow.
    /// </summary>
    /// <param name="sessionLogClient">Typed session-log client.</param>
    public TranscriptIngestionWorkflow(SessionLogClient sessionLogClient)
    {
        _sessionLogClient = sessionLogClient ?? throw new ArgumentNullException(nameof(sessionLogClient));
    }

    /// <inheritdoc />
    public Task<TranscriptIngestRunResponse> IngestTranscriptsAsync(
        TranscriptIngestPathRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _sessionLogClient.IngestTranscriptPathAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TranscriptIngestRunResponse> NormalizeTranscriptsAsync(
        TranscriptIngestPathRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _sessionLogClient.IngestTranscriptPathAsync(request, cancellationToken);
    }
}
