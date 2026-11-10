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

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-TRANSCRIPT-004 / TEST-MCP-TRANSCRIPT-012: resubmission revives a soft-deleted session.
/// Sessions are soft-delete only; the unique (WorkspaceId, SourceType, SessionId) index keeps the
/// tombstone holding the key, so SubmitAsync must recognize a tombstoned session with the same key,
/// restore its row graph, and apply the resubmitted turn data instead of failing on the unique
/// index. Uses an in-memory SQLite McpDbContext; tombstones are produced through
/// DeleteSessionAsync for canonical ids and through direct soft-delete shadow-property updates for
/// imported provider-native ids (which session delete rejects by policy).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-TRANSCRIPT-004; TEST-MCP-TRANSCRIPT-012; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (SubmitAsync_ImportResubmitAfterTombstone_RevivesSession) and the helpers only it uses (ImportedSessionId, ImportedRequestId, BuildImportedDto and TombstoneSessionGraph) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (WorkspacePath, Agent, OpenConnection, BuildSut and Submit) moved once into Services/SessionLogResubmissionReviveTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class SessionLogResubmissionReviveTests
{
    private const string CanonicalSessionId = "Codex-20260714T120000Z-revive";
    private const string CanonicalRequestId = "req-20260714T120000Z-revive-turn";

    /// <summary>Resubmitting a canonically named session after DeleteSessionAsync revives it with the corrected data.</summary>
    [Fact]
    public async Task SubmitAsync_ResubmitAfterSessionDelete_RevivesSessionWithCorrectedTurns()
    {
        using var connection = OpenConnection();
        Submit(connection, BuildCanonicalDto(response: "stale response"));

        var (deleter, deleterDb) = BuildSut(connection);
        using (deleterDb)
            Assert.True(await deleter.DeleteSessionAsync(Agent, CanonicalSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true));

        Submit(connection, BuildCanonicalDto(response: "corrected response"));

        var (reader, readerDb) = BuildSut(connection);
        using (readerDb)
        {
            var session = await reader.GetAsync(Agent, CanonicalSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.NotNull(session);
            var turn = Assert.Single(session!.Turns ?? []);
            Assert.Equal("corrected response", turn.Response);
        }
    }

    private static UnifiedSessionLogDto BuildCanonicalDto(string response) => new()
    {
        SourceType = Agent,
        SessionId = CanonicalSessionId,
        Title = "Revive test",
        Started = "2026-07-14T12:00:00Z",
        LastUpdated = "2026-07-14T12:30:00Z",
        Status = "completed",
        Turns =
        [
            new UnifiedRequestEntryDto
            {
                RequestId = CanonicalRequestId,
                Timestamp = "2026-07-14T12:00:00Z",
                QueryText = "revive query",
                Response = response,
                Status = "completed",
                PlanFile = "None",
                TodoId = "None",
            },
        ],
    };
}
