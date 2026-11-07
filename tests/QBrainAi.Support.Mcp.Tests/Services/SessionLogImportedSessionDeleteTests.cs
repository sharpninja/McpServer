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

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-TRANSCRIPT-004 / TEST-MCP-TRANSCRIPT-012: management operations on imported transcript
/// sessions. Transcript imports persist provider-native identifiers (UUID session ids, tool-call
/// request ids) through the <c>sourceFilePath</c> import path of SubmitAsync. Turn-level keyed
/// operations (delete turn, replace/clear section, delete item) must accept those identifiers so
/// turns can be repaired by resubmission; session delete stays canonical-only by policy, so
/// imported sessions are never deletable. Uses an in-memory SQLite McpDbContext seeded through
/// SubmitAsync with a source file path.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-TRANSCRIPT-004; TEST-MCP-TRANSCRIPT-012; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 3 Category=Integration cases (DeleteSessionAsync_ImportedProviderNativeIds_IsRejected, DeleteTurnAsync_ImportedProviderNativeIds_DeletesTurn and ReplaceTurnSectionAsync_ImportedProviderNativeIds_ReplacesSection) and the helpers only they use (ImportedSessionId, ImportedRequestId and SeedImportedSession) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (WorkspacePath, Agent, OpenConnection and BuildSut) moved once into Services/SessionLogImportedSessionDeleteTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class SessionLogImportedSessionDeleteTests
{
    /// <summary>Whitespace identifiers stay rejected on the delete path.</summary>
    [Fact]
    public async Task DeleteSessionAsync_WhitespaceSessionId_Throws()
    {
        using var connection = OpenConnection();
        var (sut, db) = BuildSut(connection);
        using (db)
            await Assert.ThrowsAsync<ArgumentException>(() => sut.DeleteSessionAsync(Agent, "   ", TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    /// <summary>Non-canonical ids are rejected by session delete before any lookup.</summary>
    [Fact]
    public async Task DeleteSessionAsync_NonCanonicalId_IsRejected()
    {
        using var connection = OpenConnection();
        var (sut, db) = BuildSut(connection);
        using (db)
            await Assert.ThrowsAsync<ArgumentException>(() => sut.DeleteSessionAsync(Agent, "no-such-imported-session", TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }
}
