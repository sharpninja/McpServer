using System.Text.Json;
using McpServer.SessionLog.Transcripts;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>Consumer contract tests for the shared transcript parsing and canonical YAML pipeline.</summary>
public sealed class TranscriptCorePipelineTests
{
    /// <summary>Verifies recursive discovery detects every supported real transcript source kind.</summary>
    [Fact]
    public async Task Detector_DiscoversEverySupportedRealTranscriptSource()
    {
        var root = ResolveRealFixtureRoot();
        ITranscriptBundleDetector detector = new TranscriptBundleDetector();

        var bundles = await detector.DetectAsync(root, recursive: true, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var sourceKinds = bundles.Select(bundle => bundle.SourceKind).ToHashSet();

        Assert.Contains(TranscriptSourceKind.Claude, sourceKinds);
        Assert.Contains(TranscriptSourceKind.Codex, sourceKinds);
        Assert.Contains(TranscriptSourceKind.Grok, sourceKinds);
        Assert.Contains(TranscriptSourceKind.Cline, sourceKinds);
        Assert.Contains(TranscriptSourceKind.Copilot, sourceKinds);
        Assert.Contains(TranscriptSourceKind.OpenCode, sourceKinds);
    }

    /// <summary>Verifies every real transcript fixture can be normalized into a canonical session and YAML artifact.</summary>
    [Theory]
    [InlineData(TranscriptSourceKind.Codex, "codex/session.jsonl", "codex-real-fixture-session", "user", "assistant")]
    [InlineData(TranscriptSourceKind.Claude, "claude/session.jsonl", "claude-real-fixture-session", "user", "assistant")]
    [InlineData(TranscriptSourceKind.Grok, "grok/chat_history.jsonl", "grok-derived", "user", "assistant")]
    [InlineData(TranscriptSourceKind.Cline, "cline", "cline-real-fixture-session", "user", "diagnostic")]
    [InlineData(TranscriptSourceKind.Copilot, "copilot/events.jsonl", "copilot-derived", "user", "assistant")]
    [InlineData(TranscriptSourceKind.OpenCode, "opencode/export.json", "ses_opencode_real_fixture", "user", "assistant")]
    public async Task IngestionService_NormalizesRealTranscriptFixtures(
        TranscriptSourceKind sourceKind,
        string relativePath,
        string expectedSessionIdPrefix,
        string expectedFirstRole,
        string expectedSecondRole)
    {
        var inputPath = Path.Combine(ResolveRealFixtureRoot(), relativePath);
        var service = TranscriptIngestionService.CreateDefault();

        var result = await service.IngestPathAsync(new TranscriptIngestionRequest(inputPath)
        {
            SourceKind = sourceKind,
            Persist = false,
            CompatibilityProfile = TranscriptCompatibilityProfile.None
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var session = Assert.Single(result.Sessions);
        Assert.Equal(sourceKind, session.SourceKind);
        Assert.StartsWith(expectedSessionIdPrefix, session.SessionId, StringComparison.Ordinal);
        Assert.Contains(session.Events, item => item.Role.Equals(expectedFirstRole, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(session.Events, item => item.Role.Equals(expectedSecondRole, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Severity.Equals("error", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("sourceType: " + sourceKind, session.CanonicalYaml, StringComparison.Ordinal);
        Assert.Contains("sessionId: " + session.SessionId, session.CanonicalYaml, StringComparison.Ordinal);
        Assert.Contains("turns:", session.CanonicalYaml, StringComparison.Ordinal);
        Assert.Null(session.CompatibilityArtifact);
    }

    /// <summary>Verifies requested compatibility profiles emit parseable JSONL without replacing canonical YAML.</summary>
    [Theory]
    [InlineData(TranscriptCompatibilityProfile.Claude)]
    [InlineData(TranscriptCompatibilityProfile.Codex)]
    [InlineData(TranscriptCompatibilityProfile.Grok)]
    public async Task IngestionService_EmitsCompatibilityJsonlWhenProfileRequested(TranscriptCompatibilityProfile profile)
    {
        var inputPath = Path.Combine(ResolveRealFixtureRoot(), "codex", "session.jsonl");
        var service = TranscriptIngestionService.CreateDefault();

        var result = await service.IngestPathAsync(new TranscriptIngestionRequest(inputPath)
        {
            SourceKind = TranscriptSourceKind.Codex,
            Persist = false,
            CompatibilityProfile = profile
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var session = Assert.Single(result.Sessions);
        Assert.NotNull(session.CompatibilityArtifact);
        var artifact = session.CompatibilityArtifact;
        Assert.Equal(profile, artifact.Profile);
        Assert.EndsWith(".jsonl", artifact.FileName, StringComparison.Ordinal);
        Assert.Contains(session.SessionId, artifact.Content, StringComparison.Ordinal);
        Assert.Contains("sourceType: Codex", session.CanonicalYaml, StringComparison.Ordinal);

        var lines = artifact.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.NotEmpty(lines);
        Assert.All(lines, line =>
        {
            using var document = JsonDocument.Parse(line);
            Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        });
    }

    /// <summary>Verifies persisted transcript runs require workspace-owned cache identity.</summary>
    [Fact]
    public async Task IngestionService_PersistRequiresAgentAndWorkspacePath()
    {
        var inputPath = Path.Combine(ResolveRealFixtureRoot(), "codex", "session.jsonl");
        var service = TranscriptIngestionService.CreateDefault();

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.IngestPathAsync(new TranscriptIngestionRequest(inputPath)
            {
                SourceKind = TranscriptSourceKind.Codex,
                Persist = true
            }, TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        Assert.Contains("Agent", exception.Message, StringComparison.Ordinal);
        Assert.Contains("WorkspacePath", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Verifies persisted runs write canonical artifacts and retain pending recovery until real import succeeds.</summary>
    [Fact]
    public async Task IngestionService_PersistWritesRunArtifactsAndPendingFailsafeEnvelope()
    {
        var tempWorkspace = Path.Combine(Path.GetTempPath(), "mcp-transcript-workspace", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempWorkspace);
        try
        {
            var inputPath = Path.Combine(ResolveRealFixtureRoot(), "codex", "session.jsonl");
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(inputPath)
            {
                SourceKind = TranscriptSourceKind.Codex,
                Persist = true,
                Agent = "Codex",
                WorkspacePath = tempWorkspace,
                ProviderTranscriptRoots = [ResolveRealFixtureRoot()],
                RunId = "run-test",
                CompatibilityProfile = TranscriptCompatibilityProfile.Codex
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal("run-test", result.RunId);
            Assert.False(result.Persisted);
            Assert.True(result.Degraded);
            Assert.NotNull(result.ArtifactRootPath);
            Assert.NotEmpty(result.ImportRecoveryPaths);
            Assert.True(Directory.Exists(result.ArtifactRootPath));
            Assert.StartsWith(Path.Combine(tempWorkspace, ".mcpServer", "Codex"), result.ArtifactRootPath, StringComparison.OrdinalIgnoreCase);

            var receipt = Assert.Single(result.Receipts);
            Assert.Single(result.ImportRecoveryPaths, receipt.ImportRecoveryPath);
            Assert.Equal("pending", receipt.Status);
            Assert.Equal(receipt.SessionId, receipt.RootId);
            Assert.True(File.Exists(receipt.YamlArtifactPath));
            Assert.True(File.Exists(receipt.CompatibilityArtifactPath));
            Assert.True(File.Exists(receipt.ImportRecoveryPath));
            Assert.StartsWith(Path.Combine(tempWorkspace, ".mcpServer", "Codex", "failsafe", "pending"), receipt.ImportRecoveryPath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(receipt.RootId, Path.GetFileName(receipt.ImportRecoveryPath), StringComparison.Ordinal);
            Assert.Contains("sourceType: Codex", await File.ReadAllTextAsync(receipt.YamlArtifactPath, TestContext.Current.CancellationToken).ConfigureAwait(true), StringComparison.Ordinal);
            Assert.Contains("session_meta", await File.ReadAllTextAsync(receipt.CompatibilityArtifactPath!, TestContext.Current.CancellationToken).ConfigureAwait(true), StringComparison.Ordinal);
            Assert.Contains("importRecovery", await File.ReadAllTextAsync(receipt.ImportRecoveryPath, TestContext.Current.CancellationToken).ConfigureAwait(true), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempWorkspace))
                Directory.Delete(tempWorkspace, recursive: true);
        }
    }

    /// <summary>Verifies multi-root folder ingestion writes distinct pending failsafe documents.</summary>
    [Fact]
    public async Task IngestionService_PersistNamesFailsafeDocumentsByRootIdWithoutOverwrite()
    {
        var tempWorkspace = Path.Combine(Path.GetTempPath(), "mcp-transcript-workspace", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempWorkspace);
        try
        {
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(ResolveRealFixtureRoot())
            {
                SourceKind = TranscriptSourceKind.Auto,
                Persist = true,
                Agent = "Codex",
                WorkspacePath = tempWorkspace,
                ProviderTranscriptRoots = [ResolveRealFixtureRoot()],
                RunId = "run-multi-root"
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.True(result.Receipts.Count > 1);
            Assert.Equal(result.Receipts.Count, result.ImportRecoveryPaths.Count);
            Assert.Equal(result.Receipts.Count, result.ImportRecoveryPaths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(result.Receipts, receipt =>
            {
                Assert.True(File.Exists(receipt.ImportRecoveryPath));
                Assert.Contains(receipt.RootId, Path.GetFileName(receipt.ImportRecoveryPath), StringComparison.Ordinal);
            });
        }
        finally
        {
            if (Directory.Exists(tempWorkspace))
                Directory.Delete(tempWorkspace, recursive: true);
        }
    }

    /// <summary>Verifies persisted path ingestion accepts workspace-contained transcript paths.</summary>
    [Fact]
    public async Task IngestionService_PersistAllowsWorkspaceContainedPath()
    {
        var tempWorkspace = Path.Combine(Path.GetTempPath(), "mcp-transcript-workspace", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempWorkspace);
        try
        {
            var transcriptPath = Path.Combine(tempWorkspace, "session.jsonl");
            File.Copy(Path.Combine(ResolveRealFixtureRoot(), "codex", "session.jsonl"), transcriptPath);
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(transcriptPath)
            {
                SourceKind = TranscriptSourceKind.Codex,
                Persist = true,
                Agent = "Codex",
                WorkspacePath = tempWorkspace,
                RunId = "run-workspace-path"
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Single(result.Receipts);
        }
        finally
        {
            if (Directory.Exists(tempWorkspace))
                Directory.Delete(tempWorkspace, recursive: true);
        }
    }

    /// <summary>Verifies persisted path ingestion resolves workspace-relative transcript paths beneath the active workspace.</summary>
    [Fact]
    public async Task IngestionService_PersistAllowsWorkspaceRelativePath()
    {
        var tempWorkspace = Path.Combine(Path.GetTempPath(), "mcp-transcript-workspace", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempWorkspace);
        try
        {
            var transcriptDirectory = Path.Combine(tempWorkspace, "transcripts");
            Directory.CreateDirectory(transcriptDirectory);
            File.Copy(Path.Combine(ResolveRealFixtureRoot(), "codex", "session.jsonl"), Path.Combine(transcriptDirectory, "session.jsonl"));
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(Path.Combine("transcripts", "session.jsonl"))
            {
                SourceKind = TranscriptSourceKind.Codex,
                Persist = true,
                Agent = "Codex",
                WorkspacePath = tempWorkspace,
                RunId = "run-workspace-relative-path"
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            var receipt = Assert.Single(result.Receipts);
            Assert.Equal("codex-real-fixture-session", receipt.SessionId);
        }
        finally
        {
            if (Directory.Exists(tempWorkspace))
                Directory.Delete(tempWorkspace, recursive: true);
        }
    }

    /// <summary>Verifies configured provider transcript roots are allowed even when outside the workspace.</summary>
    [Fact]
    public async Task IngestionService_PersistAllowsConfiguredProviderTranscriptRoot()
    {
        var tempWorkspace = Path.Combine(Path.GetTempPath(), "mcp-transcript-workspace", Guid.NewGuid().ToString("N"));
        var providerRoot = Path.Combine(Path.GetTempPath(), "mcp-transcript-provider", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempWorkspace);
        Directory.CreateDirectory(providerRoot);
        try
        {
            var transcriptPath = Path.Combine(providerRoot, "session.jsonl");
            File.Copy(Path.Combine(ResolveRealFixtureRoot(), "codex", "session.jsonl"), transcriptPath);
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(transcriptPath)
            {
                SourceKind = TranscriptSourceKind.Codex,
                Persist = true,
                Agent = "Codex",
                WorkspacePath = tempWorkspace,
                ProviderTranscriptRoots = [providerRoot],
                RunId = "run-provider-root"
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Single(result.Receipts);
        }
        finally
        {
            if (Directory.Exists(tempWorkspace))
                Directory.Delete(tempWorkspace, recursive: true);
            if (Directory.Exists(providerRoot))
                Directory.Delete(providerRoot, recursive: true);
        }
    }

    /// <summary>Verifies traversal or external paths are rejected before any transcript read occurs.</summary>
    [Fact]
    public async Task IngestionService_PersistRejectsTraversalOutsideWorkspaceAndProviderRoots()
    {
        var tempParent = Path.Combine(Path.GetTempPath(), "mcp-transcript-security", Guid.NewGuid().ToString("N"));
        var tempWorkspace = Path.Combine(tempParent, "workspace");
        var externalRoot = Path.Combine(tempParent, "external");
        Directory.CreateDirectory(tempWorkspace);
        Directory.CreateDirectory(externalRoot);
        try
        {
            var externalTranscriptPath = Path.Combine(externalRoot, "session.jsonl");
            File.Copy(Path.Combine(ResolveRealFixtureRoot(), "codex", "session.jsonl"), externalTranscriptPath);
            var traversalPath = Path.Combine(tempWorkspace, "..", "external", "session.jsonl");
            var service = TranscriptIngestionService.CreateDefault();

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await service.IngestPathAsync(new TranscriptIngestionRequest(traversalPath)
                {
                    SourceKind = TranscriptSourceKind.Codex,
                    Persist = true,
                    Agent = "Codex",
                    WorkspacePath = tempWorkspace,
                    RunId = "run-reject-traversal"
                }, TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

            Assert.Contains("outside the workspace", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(tempParent))
                Directory.Delete(tempParent, recursive: true);
        }
    }

    /// <summary>Verifies JSONL normalization rejects individual records above the supported line-size bound.</summary>
    [Fact]
    public async Task IngestionService_RejectsOversizedJsonlLine()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-transcript-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var path = Path.Combine(tempDirectory, "oversized.jsonl");
        try
        {
            var oversizedText = new string('x', (8 * 1024 * 1024) + 1);
            await File.WriteAllTextAsync(path, "{\"type\":\"response_item\",\"payload\":{\"role\":\"assistant\",\"content\":[{\"type\":\"output_text\",\"text\":\"" + oversizedText + "\"}]}}", TestContext.Current.CancellationToken).ConfigureAwait(true);
            var service = TranscriptIngestionService.CreateDefault();

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
                await service.IngestPathAsync(new TranscriptIngestionRequest(path)
                {
                    SourceKind = TranscriptSourceKind.Codex,
                    Persist = false
                }, TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string ResolveRealFixtureRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tests", "McpServer.Support.Mcp.Tests", "Fixtures", "Transcripts", "real");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate real transcript fixture root from test output directory.");
    }

    private sealed class RecordingTranscriptPersister : ITranscriptSessionPersister
    {
        private readonly string _receipt;

        internal RecordingTranscriptPersister(string receipt)
        {
            _receipt = receipt;
        }

        internal List<TranscriptSessionReceipt> ReceiptsSeen { get; } = [];

        internal bool RecoveryFileExistedDuringPersist { get; private set; }

        public Task<string> PersistAsync(
            TranscriptIngestionRequest request,
            TranscriptSession session,
            TranscriptSessionReceipt receipt,
            CancellationToken cancellationToken = default)
        {
            ReceiptsSeen.Add(receipt);
            RecoveryFileExistedDuringPersist = File.Exists(receipt.ImportRecoveryPath);
            return Task.FromResult(_receipt);
        }
    }

    private sealed class FailingTranscriptPersister : ITranscriptSessionPersister
    {
        public Task<string> PersistAsync(
            TranscriptIngestionRequest request,
            TranscriptSession session,
            TranscriptSessionReceipt receipt,
            CancellationToken cancellationToken = default)
            => Task.FromException<string>(new InvalidOperationException("session log submit failed"));
    }
}
