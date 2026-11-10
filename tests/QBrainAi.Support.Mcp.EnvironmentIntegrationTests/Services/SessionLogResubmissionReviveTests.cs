using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.SessionLogResubmissionReviveTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TR-MCP-TRANSCRIPT-004 / TEST-MCP-TRANSCRIPT-012: resubmission revives a soft-deleted session.
/// Sessions are soft-delete only; the unique (WorkspaceId, SourceType, SessionId) index keeps the
/// tombstone holding the key, so SubmitAsync must recognize a tombstoned session with the same key,
/// restore its row graph, and apply the resubmitted turn data instead of failing on the unique
/// index. Uses an in-memory SQLite McpDbContext; tombstones are produced through
/// DeleteSessionAsync for canonical ids and through direct soft-delete shadow-property updates for
/// imported provider-native ids (which session delete rejects by policy).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-TRANSCRIPT-004; TEST-MCP-TRANSCRIPT-012; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/SessionLogResubmissionReviveTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case and the helpers only it uses (ImportedSessionId, ImportedRequestId, BuildImportedDto and TombstoneSessionGraph) moved unchanged with the class attributes; the helpers both halves use come from Services/SessionLogResubmissionReviveTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class SessionLogResubmissionReviveTests
{
    private const string ImportedSessionId = "019f2580-48c8-7912-b6a9-27f61b18d0d3";
    private const string ImportedRequestId = "fc_08eff9c03a00059d016a470a6942688197";

    /// <summary>Resubmitting an imported session after its rows were tombstoned revives it through the import path.</summary>
    /// <remarks>Integration: resubmits through the real import path, whose extractor may scan the profile read-only without asserting its contents (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task SubmitAsync_ImportResubmitAfterTombstone_RevivesSession()
    {
        using var connection = OpenConnection();
        Submit(connection, BuildImportedDto(output: "stale output"), sourceFilePath: @"F:\imports\rollout.jsonl");
        TombstoneSessionGraph(connection, ImportedSessionId);

        var (probe, probeDb) = BuildSut(connection);
        using (probeDb)
            Assert.Null(await probe.GetAsync(Agent, ImportedSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true));

        Submit(connection, BuildImportedDto(output: "corrected output"), sourceFilePath: @"F:\imports\rollout.jsonl");

        var (reader, readerDb) = BuildSut(connection);
        using (readerDb)
        {
            var session = await reader.GetAsync(Agent, ImportedSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.NotNull(session);
            var turn = Assert.Single(session!.Turns ?? []);
            Assert.Equal(ImportedRequestId, turn.RequestId);
            Assert.Equal("corrected output", turn.Response);
        }
    }

    private static UnifiedSessionLogDto BuildImportedDto(string output) => new()
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
                Response = output,
                Status = "completed",
                PlanFile = "None",
                TodoId = "None",
            },
        ],
    };

    /// <summary>Tombstones the session row and every turn row via the soft-delete shadow properties.</summary>
    private static void TombstoneSessionGraph(SqliteConnection connection, string sessionId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE SessionLogTurns SET IsDeleted = 1, DeletedAtUtc = @now, DeletedBy = 'test', DeleteReason = 'test-tombstone' " +
            "WHERE SessionLogId IN (SELECT Id FROM SessionLogs WHERE SessionId = @sessionId); " +
            "UPDATE SessionLogs SET IsDeleted = 1, DeletedAtUtc = @now, DeletedBy = 'test', DeleteReason = 'test-tombstone' " +
            "WHERE SessionId = @sessionId;";
        command.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("@sessionId", sessionId);
        command.ExecuteNonQuery();
    }
}
