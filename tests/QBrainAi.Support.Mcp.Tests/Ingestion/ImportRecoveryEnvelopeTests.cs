using QBrainAi.SessionLog.Transcripts;

namespace QBrainAi.Support.Mcp.Tests.Ingestion;

/// <summary>
/// TEST-MCP-SESSIONLIFE-004: transcript ingestion replays pending import-recovery envelopes
/// through the source adapter and session persister, then deletes only the envelope.
/// </summary>
public sealed class ImportRecoveryEnvelopeTests
{
    /// <summary>
    /// A pending envelope is replayed from its source file. The canonical artifact and source file stay.
    /// The persister receives the normalized session, not the envelope document.
    /// </summary>
    [Fact]
    public async Task IngestPath_ReplaysPendingEnvelopeThroughPersister_DeletesOnlyEnvelope()
    {
        var root = Path.Combine(Path.GetTempPath(), "import-envelope-" + Guid.NewGuid().ToString("N"));
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
            var fixture = Path.Combine(FindRepositoryRoot(), "tests", "QBrainAi.Support.Mcp.Tests", "Ingestion", "Fixtures", "import-recovery-envelope.yaml");
            var template = await File.ReadAllTextAsync(fixture, TestContext.Current.CancellationToken);
            var envelopeYaml = template
                .Replace("{{ARTIFACT}}", artifact.Replace('\\', '/'), StringComparison.Ordinal)
                .Replace("{{SOURCE}}", source.Replace('\\', '/'), StringComparison.Ordinal);
            var envelope = Path.Combine(pending, "ok.importRecovery.yaml");
            await File.WriteAllTextAsync(envelope, envelopeYaml, TestContext.Current.CancellationToken);
            var kept = Path.Combine(pending, "no.importRecovery.yaml");
            await File.WriteAllTextAsync(
                kept,
                "importRecovery:\n  sourceKind: Codex\n  persisted: false\n  degraded: true\n  yamlArtifactPath: \"" + artifact.Replace('\\', '/') + "\"\n",
                TestContext.Current.CancellationToken);

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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "QBrainAi.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("QBrainAi.sln not found.");
    }

    private sealed class EmptyDetector : ITranscriptBundleDetector
    {
        /// <inheritdoc />
        public Task<IReadOnlyList<TranscriptBundle>> DetectAsync(string path, bool recursive, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TranscriptBundle>>([]);
    }

    private sealed class CodexSourceAdapter : ITranscriptSourceAdapter
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

    private sealed class RecordingPersister : ITranscriptSessionPersister
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
