using System.Globalization;
using System.Text;
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

    /// <summary>Verifies manual normalization writes canonical and compatibility artifacts without primary session persistence.</summary>
    [Fact]
    public async Task IngestionService_NormalizationWritesArtifactsWithoutSessionPersistence()
    {
        var tempWorkspace = Path.Combine(Path.GetTempPath(), "mcp-transcript-normalize", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempWorkspace);
        try
        {
            var transcriptDirectory = Path.Combine(tempWorkspace, "transcripts");
            Directory.CreateDirectory(transcriptDirectory);
            File.Copy(Path.Combine(ResolveRealFixtureRoot(), "codex", "session.jsonl"), Path.Combine(transcriptDirectory, "session.jsonl"));
            var inputPath = Path.Combine("transcripts", "session.jsonl");
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(inputPath)
            {
                SourceKind = TranscriptSourceKind.Codex,
                Persist = false,
                Agent = "Codex",
                WorkspacePath = tempWorkspace,
                CompatibilityProfile = TranscriptCompatibilityProfile.Grok,
                RunId = "run-normalize-artifacts"
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.False(result.Persisted);
            Assert.False(result.Degraded);
            Assert.Empty(result.ImportRecoveryPaths);
            var receipt = Assert.Single(result.Receipts);
            Assert.Equal("normalized", receipt.Status);
            Assert.Equal("codex-real-fixture-session", receipt.SessionId);
            Assert.True(File.Exists(receipt.YamlArtifactPath), receipt.YamlArtifactPath);
            Assert.False(File.Exists(receipt.ImportRecoveryPath), receipt.ImportRecoveryPath);
            Assert.NotNull(receipt.CompatibilityArtifactPath);
            Assert.True(File.Exists(receipt.CompatibilityArtifactPath), receipt.CompatibilityArtifactPath);
            Assert.Contains("codex-real-fixture-session", await File.ReadAllTextAsync(receipt.YamlArtifactPath, TestContext.Current.CancellationToken).ConfigureAwait(true), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempWorkspace))
                Directory.Delete(tempWorkspace, recursive: true);
        }
    }

    /// <summary>Verifies unsupported Codex JSONL records are diagnosed instead of silently discarded.</summary>
    [Fact]
    public async Task IngestionService_CodexUnsupportedRecordsEmitDiagnostics()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-transcript-codex-diagnostics", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var transcriptPath = Path.Combine(tempDirectory, "session.jsonl");
            await File.WriteAllLinesAsync(transcriptPath, [
                "{\"type\":\"session_meta\",\"payload\":{\"id\":\"codex-diagnostic-fixture\",\"cwd\":\"F:/GitHub/Sample\"}}",
                "{\"type\":\"response_item\",\"payload\":{\"id\":\"msg-1\",\"role\":\"user\",\"content\":[{\"type\":\"input_text\",\"text\":\"hello\"}]}}",
                "{\"type\":\"unmapped_record\",\"payload\":{\"reason\":\"should diagnose\"}}",
                "{\"type\":\"response_item\",\"payload\":{\"id\":\"msg-missing-role\",\"content\":[{\"type\":\"output_text\",\"text\":\"dropped\"}]}}"
            ], TestContext.Current.CancellationToken).ConfigureAwait(true);
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(transcriptPath)
            {
                SourceKind = TranscriptSourceKind.Codex,
                Persist = false
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            var session = Assert.Single(result.Sessions);
            Assert.Equal("codex-diagnostic-fixture", session.SessionId);
            Assert.Single(session.Events);
            Assert.Contains(session.Diagnostics, diagnostic => diagnostic.Code == "codex_unknown_record" && diagnostic.Severity == "warning");
            Assert.Contains(session.Diagnostics, diagnostic => diagnostic.Code == "codex_missing_role" && diagnostic.Severity == "warning");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>Verifies unsupported Claude JSONL records are diagnosed instead of silently discarded.</summary>
    [Fact]
    public async Task IngestionService_ClaudeUnsupportedRecordsEmitDiagnostics()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-transcript-claude-diagnostics", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var transcriptPath = Path.Combine(tempDirectory, "session.jsonl");
            await File.WriteAllLinesAsync(transcriptPath, [
                "{\"type\":\"user\",\"sessionId\":\"claude-diagnostic-fixture\",\"cwd\":\"F:/GitHub/Sample\",\"uuid\":\"claude-msg-1\",\"message\":{\"role\":\"user\",\"content\":[{\"type\":\"text\",\"text\":\"hello\"}]}}",
                "{\"type\":\"summary\",\"sessionId\":\"claude-diagnostic-fixture\",\"summary\":\"should diagnose\"}",
                "{\"type\":\"assistant\",\"sessionId\":\"claude-diagnostic-fixture\",\"uuid\":\"claude-missing-message\"}"
            ], TestContext.Current.CancellationToken).ConfigureAwait(true);
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(transcriptPath)
            {
                SourceKind = TranscriptSourceKind.Claude,
                Persist = false
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            var session = Assert.Single(result.Sessions);
            Assert.Equal("claude-diagnostic-fixture", session.SessionId);
            Assert.Single(session.Events);
            Assert.Contains(session.Diagnostics, diagnostic => diagnostic.Code == "claude_unknown_record" && diagnostic.Severity == "warning");
            Assert.Contains(session.Diagnostics, diagnostic => diagnostic.Code == "claude_missing_message" && diagnostic.Severity == "warning");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>Verifies unsupported Grok JSONL records are diagnosed instead of silently discarded.</summary>
    [Fact]
    public async Task IngestionService_GrokUnsupportedRecordsEmitDiagnostics()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-transcript-grok-diagnostics", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var transcriptPath = Path.Combine(tempDirectory, "events.jsonl");
            await File.WriteAllLinesAsync(transcriptPath, [
                "{\"type\":\"chat_message\",\"role\":\"assistant\",\"message\":\"hello from grok\",\"model\":\"grok-test\"}",
                "{\"type\":\"mcp_config_resolved\",\"servers\":[{\"name\":\"mcpserver\"}]}",
                "{\"type\":\"chat_message\",\"message\":\"missing role\"}"
            ], TestContext.Current.CancellationToken).ConfigureAwait(true);
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(transcriptPath)
            {
                SourceKind = TranscriptSourceKind.Grok,
                Persist = false
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            var session = Assert.Single(result.Sessions);
            Assert.Single(session.Events);
            Assert.Contains(session.Diagnostics, diagnostic => diagnostic.Code == "grok_unknown_record" && diagnostic.Severity == "warning");
            Assert.Contains(session.Diagnostics, diagnostic => diagnostic.Code == "grok_missing_role" && diagnostic.Severity == "warning");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>Verifies unsupported Copilot event records are diagnosed instead of silently discarded.</summary>
    [Fact]
    public async Task IngestionService_CopilotUnsupportedRecordsEmitDiagnostics()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-transcript-copilot-diagnostics", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var transcriptPath = Path.Combine(tempDirectory, "events.jsonl");
            await File.WriteAllLinesAsync(transcriptPath, [
                "{\"type\":\"user.message\",\"id\":\"copilot-user\",\"data\":{\"message\":\"hello from copilot\"}}",
                "{\"type\":\"assistant.turn_start\",\"id\":\"copilot-turn\",\"data\":{\"turnId\":\"0\"}}",
                "{\"type\":\"assistant.message\",\"id\":\"copilot-missing-data\"}"
            ], TestContext.Current.CancellationToken).ConfigureAwait(true);
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(transcriptPath)
            {
                SourceKind = TranscriptSourceKind.Copilot,
                Persist = false
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            var session = Assert.Single(result.Sessions);
            Assert.Single(session.Events);
            Assert.Contains(session.Diagnostics, diagnostic => diagnostic.Code == "copilot_unknown_record" && diagnostic.Severity == "warning");
            Assert.Contains(session.Diagnostics, diagnostic => diagnostic.Code == "copilot_missing_data" && diagnostic.Severity == "warning");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>Verifies malformed Cline paired JSON is diagnosed instead of silently discarded.</summary>
    [Fact]
    public async Task IngestionService_ClineMalformedMessagesEmitDiagnostics()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-transcript-cline-diagnostics", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var sessionPath = Path.Combine(tempDirectory, "session.json");
            var messagesPath = Path.Combine(tempDirectory, "messages.json");
            await File.WriteAllTextAsync(sessionPath, "{\"session_id\":\"cline-diagnostic-fixture\",\"model\":\"cline-test\",\"workspace_root\":\"F:/GitHub/Sample\"}", TestContext.Current.CancellationToken).ConfigureAwait(true);
            await File.WriteAllTextAsync(messagesPath, "{\"sessionId\":\"cline-diagnostic-fixture\",\"messages\":{\"invalid\":true}}", TestContext.Current.CancellationToken).ConfigureAwait(true);
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(tempDirectory)
            {
                SourceKind = TranscriptSourceKind.Cline,
                Persist = false,
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            var session = Assert.Single(result.Sessions);
            Assert.Empty(session.Events);
            Assert.Contains(session.Diagnostics, diagnostic => diagnostic.Code == "cline_missing_messages" && diagnostic.Severity == "warning");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>Verifies unsupported OpenCode JSONL records are diagnosed instead of silently discarded.</summary>
    [Fact]
    public async Task IngestionService_OpenCodeJsonlUnsupportedRecordsEmitDiagnostics()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-transcript-opencode-diagnostics", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var transcriptPath = Path.Combine(tempDirectory, "events.jsonl");
            await File.WriteAllLinesAsync(transcriptPath, [
                "{\"type\":\"step_start\",\"timestamp\":1783672962383,\"sessionID\":\"ses_opencode_diagnostic_fixture\",\"part\":{\"id\":\"opencode-step\",\"type\":\"step-start\"}}",
                "{\"type\":\"text\",\"timestamp\":1783672963404,\"sessionID\":\"ses_opencode_diagnostic_fixture\",\"part\":{\"id\":\"opencode-text\",\"type\":\"text\",\"text\":\"hello from opencode\"}}",
                "{\"type\":\"text\",\"timestamp\":1783672963500,\"sessionID\":\"ses_opencode_diagnostic_fixture\"}"
            ], TestContext.Current.CancellationToken).ConfigureAwait(true);
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(transcriptPath)
            {
                SourceKind = TranscriptSourceKind.OpenCode,
                Persist = false,
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            var session = Assert.Single(result.Sessions);
            Assert.Single(session.Events);
            Assert.Contains(session.Diagnostics, diagnostic => diagnostic.Code == "opencode_jsonl_unknown_record" && diagnostic.Severity == "warning");
            Assert.Contains(session.Diagnostics, diagnostic => diagnostic.Code == "opencode_jsonl_missing_part" && diagnostic.Severity == "warning");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>Verifies OpenCode JSONL step starts without finishes are diagnosed as incomplete turns.</summary>
    [Fact]
    public async Task IngestionService_OpenCodeJsonlIncompleteStepEmitsDiagnostic()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-transcript-opencode-incomplete-step", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var transcriptPath = Path.Combine(tempDirectory, "events.jsonl");
            await File.WriteAllLinesAsync(transcriptPath, [
                "{\"type\":\"step_start\",\"timestamp\":1783672962383,\"sessionID\":\"ses_opencode_incomplete_fixture\",\"part\":{\"id\":\"opencode-step-start\",\"messageID\":\"msg-opencode-incomplete\",\"type\":\"step-start\"}}",
                "{\"type\":\"text\",\"timestamp\":1783672963404,\"sessionID\":\"ses_opencode_incomplete_fixture\",\"part\":{\"id\":\"opencode-text\",\"messageID\":\"msg-opencode-incomplete\",\"type\":\"text\",\"text\":\"partial opencode response\"}}"
            ], TestContext.Current.CancellationToken).ConfigureAwait(true);
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(transcriptPath)
            {
                SourceKind = TranscriptSourceKind.OpenCode,
                Persist = false,
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            var session = Assert.Single(result.Sessions);
            Assert.Single(session.Events);
            Assert.Contains(session.Diagnostics, diagnostic => diagnostic.Code == "opencode_jsonl_incomplete_step" && diagnostic.Severity == "warning");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
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
            var transcriptDirectory = Path.Combine(tempWorkspace, "transcripts");
            Directory.CreateDirectory(transcriptDirectory);
            File.Copy(Path.Combine(ResolveRealFixtureRoot(), "codex", "session.jsonl"), Path.Combine(transcriptDirectory, "session.jsonl"));
            var inputPath = Path.Combine("transcripts", "session.jsonl");
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

    /// <summary>
    /// Verifies JSONL normalization accepts a single record larger than the former 8 MiB line ceiling.
    /// Fixture: a generated Codex JSONL file whose one record carries a 9 MiB output_text payload, which is
    /// above the retired 8 MiB bound and far below the Int32.MaxValue bound that replaced it.
    /// Validates FR-MCP-TRANSCRIPT-009, TR-MCP-TRANSCRIPT-010, TEST-MCP-TRANSCRIPT-013.
    /// </summary>
    [Fact]
    public async Task IngestionService_AcceptsJsonlLineAboveFormerCeiling()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-transcript-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var path = Path.Combine(tempDirectory, "large-line.jsonl");
        try
        {
            var largeText = new string('x', (9 * 1024 * 1024) + 1);
            await File.WriteAllTextAsync(path, "{\"type\":\"response_item\",\"payload\":{\"role\":\"assistant\",\"content\":[{\"type\":\"output_text\",\"text\":\"" + largeText + "\"}]}}", TestContext.Current.CancellationToken).ConfigureAwait(true);
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(path)
            {
                SourceKind = TranscriptSourceKind.Codex,
                Persist = false
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.NotEmpty(result.Sessions);
            Assert.DoesNotContain(
                result.Diagnostics,
                diagnostic => diagnostic.Message.Contains("MiB limit", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies the streaming JSONL reader produces the same normalized events as whole-file reading did.
    /// Fixture: a generated Codex JSONL file of 250 small records, including blank separator lines that the
    /// reader must skip, asserting every record survives the streaming rewrite in order.
    /// Validates TR-MCP-TRANSCRIPT-010, TEST-MCP-TRANSCRIPT-013.
    /// </summary>
    [Fact]
    public async Task IngestionService_StreamingReaderPreservesAllRecords()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-transcript-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var path = Path.Combine(tempDirectory, "many-records.jsonl");
        try
        {
            var builder = new StringBuilder();
            for (var index = 0; index < 250; index++)
            {
                builder.Append("{\"type\":\"response_item\",\"payload\":{\"role\":\"assistant\",\"content\":[{\"type\":\"output_text\",\"text\":\"record-")
                    .Append(index.ToString(CultureInfo.InvariantCulture))
                    .AppendLine("\"}]}}");
                if (index % 50 == 0)
                    builder.AppendLine();
            }

            await File.WriteAllTextAsync(path, builder.ToString(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(path)
            {
                SourceKind = TranscriptSourceKind.Codex,
                Persist = false
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            var session = Assert.Single(result.Sessions);
            Assert.Equal(250, session.Events.Count);
            Assert.Contains("record-0", session.Events[0].Content[0].Text ?? string.Empty, StringComparison.Ordinal);
            Assert.Contains("record-249", session.Events[^1].Content[0].Text ?? string.Empty, StringComparison.Ordinal);
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
