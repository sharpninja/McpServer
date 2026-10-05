using QBrainAi.SessionLog.Transcripts;

namespace QBrainAi.Support.Mcp.Tests.Ingestion;

/// <summary>
/// TEST-MCP-SESSIONLIFE-004: ingestion replays pending import-recovery envelopes through the
/// source adapter and session persister, and deletes only the envelope.
/// </summary>
public sealed class ImportRecoveryIngestionTests
{
    /// <summary>
    /// A pending envelope is replayed from its source file. The canonical artifact and source file stay.
    /// The persister receives the normalized session, not the envelope document.
    /// </summary>
    [Fact]
    public async Task IngestPath_ReplaysPendingEnvelopeThroughPersister_DeletesOnlyEnvelope()
    {
        var root = Path.Combine(Path.GetTempPath(), "import-ingest-" + Guid.NewGuid().ToString("N"));
        var input = Path.Combine(root, "inbox");
        var pending = Path.Combine(root, ".mcpServer", "Codex", "failsafe", "pending");
        Directory.CreateDirectory(input);
        Directory.CreateDirectory(pending);
        try
        {
            var source = Path.Combine(root, "session.jsonl");
            var artifact = Path.Combine(root, "session.yaml");
            await File.WriteAllTextAsync(source, "{\"type\":\"session_meta\"}\n", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(artifact, "session: keep\n", TestContext.Current.CancellationToken);
            var envelope = Path.Combine(pending, "ok.importRecovery.yaml");
            var envelopeYaml = "importRecovery:\n"
                + "  sourceKind: Codex\n"
                + "  rootId: root-1\n"
                + "  sessionId: Codex-20260923T205606Z-life\n"
                + "  sourceHash: hash-1\n"
                + "  persisted: false\n"
                + "  degraded: true\n"
                + "  yamlArtifactPath: " + YamlQuote(artifact) + "\n"
                + "  sourceFiles:\n"
                + "    - " + YamlQuote(source) + "\n";
            await File.WriteAllTextAsync(envelope, envelopeYaml, TestContext.Current.CancellationToken);
            var kept = Path.Combine(pending, "no.importRecovery.yaml");
            await File.WriteAllTextAsync(kept, "importRecovery:\n  sourceKind: Codex\n  persisted: false\n  degraded: true\n  yamlArtifactPath: " + YamlQuote(artifact) + "\n", TestContext.Current.CancellationToken);

            var persister = new RecordingPersister();
            var service = new TranscriptIngestionService(
                new EmptyDetector(),
                [new CodexSourceAdapter()],
                projectors: null,
                persister);
            var result = await service.IngestPathAsync(
                new TranscriptIngestionRequest(input)
                {
                    Persist = true,
                    Agent = "Codex",
                    WorkspacePath = root,
                    Recursive = false,
                },
                TestContext.Current.CancellationToken);

            Assert.NotNull(result);
            Assert.False(File.Exists(envelope));
            Assert.True(File.Exists(kept));
            Assert.True(File.Exists(artifact));
            Assert.True(File.Exists(source));
            var call = Assert.Single(persister.Calls);
            Assert.Equal(artifact.Replace('\\', '/'), call.Receipt.YamlArtifactPath.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("importRecovery:", call.Session.CanonicalYaml, StringComparison.Ordinal);
            Assert.Contains(
                call.Session.SourceFiles,
                file => string.Equals(file.Replace('\\', '/'), source.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string YamlQuote(string path)
    {
        return "\"" + path.Replace('\\', '/').Replace("\"", string.Empty, StringComparison.Ordinal) + "\"";
    }

    private sealed class EmptyDetector : ITranscriptBundleDetector
    {
        public Task<IReadOnlyList<TranscriptBundle>> DetectAsync(string path, bool recursive, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TranscriptBundle>>([]);
    }

    private sealed class CodexSourceAdapter : ITranscriptSourceAdapter
    {
        public TranscriptSourceKind SourceKind => TranscriptSourceKind.Codex;

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

    private sealed class RecordingPersister : ITranscriptSessionPersister
    {
        public List<(TranscriptSession Session, TranscriptSessionReceipt Receipt)> Calls { get; } = [];

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
