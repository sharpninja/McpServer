using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Provides REPL-facing transcript ingestion and normalization commands.
/// </summary>
public interface ITranscriptIngestionWorkflow
{
    /// <summary>
    /// Ingests transcript input through the typed session-log client.
    /// </summary>
    /// <param name="request">Path ingestion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ingestion run receipt.</returns>
    Task<TranscriptIngestRunResponse> IngestTranscriptsAsync(
        TranscriptIngestPathRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Normalizes transcript input through the typed session-log client.
    /// </summary>
    /// <param name="request">Normalization request expressed with the path ingestion contract.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Normalization run receipt.</returns>
    Task<TranscriptIngestRunResponse> NormalizeTranscriptsAsync(
        TranscriptIngestPathRequest request,
        CancellationToken cancellationToken = default);
}
