using QBrainAi.SessionLog.Transcripts;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>Tests for transcript persistence through the existing session-log service.</summary>
/// <remarks>2026-11-07, FR-MCP-SESSIONLOGCTX-001-007; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/TranscriptSessionLogPersisterTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; only the namespace changed.</remarks>
public sealed class TranscriptSessionLogPersisterTests
{
    /// <summary>
    /// AC-FR-MCP-SESSIONLOGCTX-001-007: omitted import fields persist extractor result or None
    /// through the real SessionLogService import path.
    /// </summary>
    /// <remarks>Integration: uses the real import extractor and its read-only profile scan without asserting profile contents (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task Import_OmittedFields_PersistsExtractorResultOrNone()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<QBrainAi.Support.Mcp.Storage.McpDbContext>()
            .UseInMemoryDatabase("import-omit-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new QBrainAi.Support.Mcp.Storage.McpDbContext(options);
        db.Database.EnsureCreated();
        var workspace = Path.Combine(Path.GetTempPath(), "import-omit-" + Guid.NewGuid().ToString("N"));
        db.OverrideWorkspaceId(workspace);
        var service = new SessionLogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<SessionLogService>.Instance, workspaceContext: new WorkspaceContext { WorkspacePath = workspace });
        var persister = new TranscriptSessionLogPersister(service);
        var sourcePath = Path.Combine(Path.GetTempPath(), "omit.jsonl");
        var yamlPath = Path.Combine(Path.GetTempPath(), "omit.hash.sessionlog.yaml");
        var request = new TranscriptIngestionRequest(sourcePath)
        {
            Agent = "Codex",
            WorkspacePath = workspace,
            Persist = true
        };
        var session = new TranscriptSession(
            TranscriptSourceKind.Codex,
            "Codex-20260710T010203Z-omit",
            [
                new TranscriptEvent(
                    "event-user",
                    1,
                    "user",
                    "response_item",
                    [new TranscriptContentBlock("text", "working MCP-IMPORT-001 on docs/plans/imported.md")],
                    DateTimeOffset.Parse("2026-07-10T01:02:03Z")),
            ],
            "sourceType: Codex\n",
            nativeSessionId: "native-omit",
            model: "gpt-5",
            workspacePath: workspace,
            sourceFiles: [sourcePath]);
        var receipt = new TranscriptSessionReceipt(
            TranscriptSourceKind.Codex,
            "native-omit",
            session.SessionId,
            "hash-omit",
            "pending",
            yamlPath,
            Path.Combine(Path.GetTempPath(), "omit.recovery.yaml"));

        await persister.PersistAsync(request, session, receipt, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = Assert.Single(db.SessionLogTurns);
        Assert.False(string.IsNullOrWhiteSpace(stored.PlanFile));
        Assert.False(string.IsNullOrWhiteSpace(stored.TodoId));
        Assert.NotEqual((string?)null, stored.PlanFile);
        Assert.True(stored.PlanFile == "None" || stored.PlanFile.Contains("docs/plans/imported.md", StringComparison.Ordinal));
        Assert.True(stored.TodoId == "None" || stored.TodoId == "MCP-IMPORT-001");
    }
}