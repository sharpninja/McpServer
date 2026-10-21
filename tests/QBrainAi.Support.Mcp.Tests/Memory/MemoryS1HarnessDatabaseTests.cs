using QBrainAi.Support.Mcp.Tests.Services;

namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 (test harness): <see cref="MemoryS1Harness"/> gives each context its own connection to one
/// test-owned database, so its concurrent memory operations never share one connection, where SQLite reports rows
/// affected per connection.
/// </summary>
/// <remarks>
/// The race class of triage-report-9b96ada18d72483dad2a3113e455c01b, found by the sibling sweep of IMPL-UNITGREEN-001
/// round 4; operator 2026-10-20: "Extend fix, then round 4 (Recommended)". Test data: the harness's own temporary
/// database file. The assertion, its positive control and its negative controls (one shared in-memory connection,
/// private in-memory databases) are in <see cref="HandoffTestDatabaseTests"/>; this binds the same assertion to the
/// harness's context factory.
/// </remarks>
public sealed class MemoryS1HarnessDatabaseTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>Harness binding: every context the harness creates has its own connection to the harness's one database.</summary>
    [Fact]
    public void TestDatabase_GivesEachContextItsOwnConnectionToOneDatabase() =>
        HandoffTestDatabaseTests.AssertOwnConnectionsToOneDatabase(() => _harness.CreateContext(_harness.WorkspaceA));
}
