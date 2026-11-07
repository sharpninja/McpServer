using QBrainAi.SessionLog.Transcripts;

namespace QBrainAi.Support.Mcp.Tests.Ingestion;

/// <summary>
/// TEST-MCP-SESSIONLIFE-004: the transcript-ingestion fakes that ImportRecoveryIngestionTests (the unit case in
/// QBrainAi.Support.Mcp.Tests) and ImportRecoveryEnvelopeTests (the integration case in
/// QBrainAi.Support.Mcp.EnvironmentIntegrationTests) both use to replay a pending import-recovery envelope: a bundle
/// detector that finds nothing, a Codex source adapter that normalizes a bundle into a fixed session, and a persister that
/// records what it receives and refuses envelope YAML. Defined once and compiled into both projects (the integration
/// project links this file).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-SESSIONLIFE-004; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted in slice S9 of hostile validation finding R14-F4 from the private copies of EmptyDetector, CodexSourceAdapter and RecordingPersister that ImportRecoveryIngestionTests and ImportRecoveryEnvelopeTests each declared; the two copies were identical in behavior (same detector result, same normalized session, same refusal and recording; only their XML documentation differed), so both classes now use this one definition (DRY). Only the accessibility changed, from private nested types to internal types nested in this class.</remarks>
internal static class ImportRecoveryIngestionFakes
{
    /// <summary>A bundle detector that finds no bundle, so ingestion reaches only the pending import-recovery envelopes.</summary>
    /// <remarks>2026-11-07, TEST-MCP-SESSIONLIFE-004; TR-MCP-NUKE-019 AC-7: extracted unchanged in slice S9.</remarks>
    internal sealed class EmptyDetector : ITranscriptBundleDetector
    {
        /// <inheritdoc />
        public Task<IReadOnlyList<TranscriptBundle>> DetectAsync(string path, bool recursive, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TranscriptBundle>>([]);
    }

    /// <summary>A Codex source adapter that normalizes any bundle into one fixed Codex session carrying the bundle's files.</summary>
    /// <remarks>2026-11-07, TEST-MCP-SESSIONLIFE-004; TR-MCP-NUKE-019 AC-7: extracted unchanged in slice S9.</remarks>
    internal sealed class CodexSourceAdapter : ITranscriptSourceAdapter
    {
        /// <inheritdoc />
        public TranscriptSourceKind SourceKind => TranscriptSourceKind.Codex;

        /// <inheritdoc />
        public Task<TranscriptSession> NormalizeAsync(TranscriptBundle bundle, CancellationToken cancellationToken = default)
        {
            var session = new TranscriptSession(
                TranscriptSourceKind.Codex,
                "Codex-20260923T205606Z-life",
                [],
                "session: from-source\n",
                sourceFiles: bundle.Files);
            return Task.FromResult(session);
        }
    }

    /// <summary>A session persister that records each session and receipt it receives and refuses envelope YAML.</summary>
    /// <remarks>2026-11-07, TEST-MCP-SESSIONLIFE-004; TR-MCP-NUKE-019 AC-7: extracted unchanged in slice S9.</remarks>
    internal sealed class RecordingPersister : ITranscriptSessionPersister
    {
        /// <summary>Persisted sessions.</summary>
        public List<(TranscriptSession Session, TranscriptSessionReceipt Receipt)> Calls { get; } = [];

        /// <inheritdoc />
        public Task<string> PersistAsync(
            TranscriptIngestionRequest request,
            TranscriptSession session,
            TranscriptSessionReceipt receipt,
            CancellationToken cancellationToken = default)
        {
            if (session.CanonicalYaml.Contains("importRecovery:", StringComparison.Ordinal))
                throw new InvalidOperationException("Envelope YAML must not be submitted.");
            Calls.Add((session, receipt));
            return Task.FromResult("sessionLogId:1");
        }
    }
}
