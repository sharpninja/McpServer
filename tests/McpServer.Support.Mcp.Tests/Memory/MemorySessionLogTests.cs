using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-015 / FR-MCP-MEMORY-010: Mutations append sessionlog actions when a turn is open.
/// </summary>
public sealed class MemorySessionLogTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-37: remember/add/update/remove append a sessionlog action when a turn is open.</summary>
    [Fact]
    public async Task Mutations_AppendActionWhenTurnOpen()
    {
        long turnId;
        await using (var db = _harness.CreateContext(_harness.WorkspaceA))
        {
            var session = new SessionLogEntity
            {
                SourceType = "CursorGrok",
                SessionId = "CursorGrok-20260918T210000Z-s1",
                Model = "grok",
                Started = DateTimeOffset.UtcNow,
                LastUpdated = DateTimeOffset.UtcNow,
                WorkspaceId = _harness.WorkspaceA,
            };
            db.SessionLogs.Add(session);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var turn = new SessionLogTurnEntity
            {
                SessionLogId = session.Id,
                RequestId = "req-20260918T210000Z-001-s1-red",
                Timestamp = DateTimeOffset.UtcNow,
                QueryTitle = "S1 red",
                Response = "open",
            };
            db.SessionLogTurns.Add(turn);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            turnId = turn.Id;
        }

        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "slog",
            Text = "sessionlog mutation",
            UpdatedBy = "CursorGrok",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        await using var after = _harness.CreateContext(_harness.WorkspaceA);
        var actions = await after.SessionLogActions
            .IgnoreQueryFilters()
            .Where(action => action.SessionLogTurnId == turnId)
            .ToListAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Contains(actions, action =>
            (action.Description ?? string.Empty).Contains(added.Memory!.Id, StringComparison.OrdinalIgnoreCase)
            || (action.Type ?? string.Empty).Contains("memory", StringComparison.OrdinalIgnoreCase));
    }
}
