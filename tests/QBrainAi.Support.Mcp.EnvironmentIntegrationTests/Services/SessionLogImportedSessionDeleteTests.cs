using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.SessionLogImportedSessionDeleteTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TR-MCP-TRANSCRIPT-004 / TEST-MCP-TRANSCRIPT-012: management operations on imported transcript
/// sessions. Transcript imports persist provider-native identifiers (UUID session ids, tool-call
/// request ids) through the <c>sourceFilePath</c> import path of SubmitAsync. Turn-level keyed
/// operations (delete turn, replace/clear section, delete item) must accept those identifiers so
/// turns can be repaired by resubmission; session delete stays canonical-only by policy, so
/// imported sessions are never deletable. Uses an in-memory SQLite McpDbContext seeded through
/// SubmitAsync with a source file path.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-TRANSCRIPT-004; TEST-MCP-TRANSCRIPT-012; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/SessionLogImportedSessionDeleteTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 3 Category=Integration cases and the helpers only they use (ImportedSessionId, ImportedRequestId and SeedImportedSession) moved unchanged with the class attributes; the helpers both halves use come from Services/SessionLogImportedSessionDeleteTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class SessionLogImportedSessionDeleteTests
{
    private const string ImportedSessionId = "019f2580-48c8-7912-b6a9-27f61b18d0d3";
    private const string ImportedRequestId = "fc_08eff9c03a00059d016a470a6942688197";

    /// <summary>Session delete stays canonical-only: imported UUID session ids are rejected.</summary>
    /// <remarks>Integration: seeds through the real import path, whose extractor may scan the profile read-only without asserting its contents (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task DeleteSessionAsync_ImportedProviderNativeIds_IsRejected()
    {
        using var connection = OpenConnection();
        SeedImportedSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            await Assert.ThrowsAsync<ArgumentException>(() => sut.DeleteSessionAsync(Agent, ImportedSessionId, TestContext.Current.CancellationToken)).ConfigureAwait(true);

        var (reader, readerDb) = BuildSut(connection);
        using (readerDb)
            Assert.NotNull(await reader.GetAsync(Agent, ImportedSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>Imported turns with provider-native request ids must be deletable.</summary>
    /// <remarks>Integration: seeds through the real import path, whose extractor may scan the profile read-only without asserting its contents (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task DeleteTurnAsync_ImportedProviderNativeIds_DeletesTurn()
    {
        using var connection = OpenConnection();
        SeedImportedSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
        {
            var deleted = await sut.DeleteTurnAsync(Agent, ImportedSessionId, ImportedRequestId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(deleted);
        }

        var (reader, readerDb) = BuildSut(connection);
        using (readerDb)
        {
            var session = await reader.GetAsync(Agent, ImportedSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.NotNull(session);
            Assert.DoesNotContain(session!.Turns ?? [], turn => turn.RequestId == ImportedRequestId);
        }
    }

    /// <summary>Imported turns must accept section-level replace operations.</summary>
    /// <remarks>Integration: seeds through the real import path, whose extractor may scan the profile read-only without asserting its contents (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ReplaceTurnSectionAsync_ImportedProviderNativeIds_ReplacesSection()
    {
        using var connection = OpenConnection();
        SeedImportedSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
        {
            var replaced = await sut.ReplaceTurnSectionAsync(Agent, ImportedSessionId, ImportedRequestId, "tags", new UnifiedRequestEntryDto
            {
                RequestId = ImportedRequestId,
                Tags = ["retagged"],
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(replaced);
        }

        var (reader, readerDb) = BuildSut(connection);
        using (readerDb)
        {
            var session = await reader.GetAsync(Agent, ImportedSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            var turn = session!.Turns!.Single(item => item.RequestId == ImportedRequestId);
            Assert.Equal(["retagged"], turn.Tags);
        }
    }

    /// <summary>Seeds one imported session (SubmitAsync with sourceFilePath) carrying provider-native ids.</summary>
    private static void SeedImportedSession(SqliteConnection connection)
    {
        var (sut, db) = BuildSut(connection);
        using (db)
        {
            sut.SubmitAsync(new UnifiedSessionLogDto
            {
                SourceType = Agent,
                SessionId = ImportedSessionId,
                Title = "Imported Codex transcript",
                Started = "2026-07-02T20:03:25Z",
                LastUpdated = "2026-07-13T17:21:42Z",
                Status = "completed",
                Turns =
                [
                    new UnifiedRequestEntryDto
                    {
                        RequestId = ImportedRequestId,
                        Timestamp = "2026-07-03T01:03:38Z",
                        QueryTitle = "shell_command",
                        Status = "completed",
                        Tags = ["transcript-import"],
                    },
                    new UnifiedRequestEntryDto
                    {
                        RequestId = "codex-event-2",
                        Timestamp = "2026-07-03T01:03:45Z",
                        QueryTitle = "output",
                        Status = "completed",
                    },
                ]
            }, sourceFilePath: @"F:\imports\rollout.jsonl", contentHash: "hash-1").GetAwaiter().GetResult();
        }
    }
}
