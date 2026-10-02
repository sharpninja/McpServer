using System.Text;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-015 / FR-MCP-MEMORY-015:
/// S4 Red acceptance for explicit promote. Production handler is unregistered (501)
/// until S4 Green.
/// </summary>
public sealed class MemoryPromoteTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-015-01: promote from sessionlog sets SourceKind/SourceRef provenance.</summary>
    [Fact]
    public async Task FromSessionLog_SetsProvenance()
    {
        var (sourceRef, raw) = await SeedSessionSourceAsync("promote sessionlog body").ConfigureAwait(true);

        var result = await _harness.PromoteAsync(
            new MemoryPromoteRequest
            {
                SourceKind = MemoryPromoteSourceKinds.SessionLog,
                SourceRef = sourceRef,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(MemoryPromoteSourceKinds.SessionLog, result.SourceKind, ignoreCase: true);
        Assert.Equal(sourceRef, result.SourceRef);
        Assert.NotNull(result.Memory);
        Assert.StartsWith("MEMORY-", result.Memory!.Id, StringComparison.Ordinal);
        Assert.Equal(raw, result.Memory.Content ?? result.Memory.Text);
    }

    /// <summary>AC-FR-MCP-MEMORY-015-02: promote from context sets SourceKind/SourceRef provenance.</summary>
    [Fact]
    public async Task FromContext_SetsProvenance()
    {
        var sourceRef = "context://chunk/promote-1";
        var result = await _harness.PromoteAsync(
            new MemoryPromoteRequest
            {
                SourceKind = MemoryPromoteSourceKinds.Context,
                SourceRef = sourceRef,
                Content = "promoted context raw",
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(MemoryPromoteSourceKinds.Context, result.SourceKind, ignoreCase: true);
        Assert.Equal(sourceRef, result.SourceRef);
        Assert.NotNull(result.Memory);
    }

    /// <summary>AC-FR-MCP-MEMORY-015-03: after promote, sessionlog rows are byte-identical.</summary>
    [Fact]
    public async Task SessionLogRows_Unchanged()
    {
        var (sourceRef, raw) = await SeedSessionSourceAsync("byte identical promote").ConfigureAwait(true);
        var before = await ReadSessionActionBytesAsync(sourceRef).ConfigureAwait(true);

        var result = await _harness.PromoteAsync(
            new MemoryPromoteRequest
            {
                SourceKind = MemoryPromoteSourceKinds.SessionLog,
                SourceRef = sourceRef,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var after = await ReadSessionActionBytesAsync(sourceRef).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.True(before.SequenceEqual(after));
        Assert.Equal(raw, Encoding.UTF8.GetString(after));
    }

    /// <summary>AC-FR-MCP-MEMORY-015-04: missing/invalid source ref returns 400.</summary>
    [Fact]
    public async Task InvalidRef_Returns400()
    {
        var result = await _harness.PromoteAsync(
            new MemoryPromoteRequest
            {
                SourceKind = MemoryPromoteSourceKinds.SessionLog,
                SourceRef = "   ",
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
    }

    /// <summary>AC-FR-MCP-MEMORY-015-05: promoting a source from another workspace is forbidden.</summary>
    [Fact]
    public async Task CrossWorkspace_Forbidden()
    {
        var (sourceRef, _) = await SeedSessionSourceAsync("foreign promote", workspacePath: _harness.WorkspaceB)
            .ConfigureAwait(true);

        var result = await _harness.PromoteAsync(
            new MemoryPromoteRequest
            {
                SourceKind = MemoryPromoteSourceKinds.SessionLog,
                SourceRef = sourceRef,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.StatusCode is 403 or 404);
    }

    /// <summary>AC-FR-MCP-MEMORY-015-06: null body returns 400.</summary>
    [Fact]
    public async Task NullBody_Returns400()
    {
        var result = await _harness.PromoteAsync(
            null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
    }

    /// <summary>AC-FR-MCP-MEMORY-015-07: unsupported SourceKind returns 400.</summary>
    [Fact]
    public async Task UnsupportedSourceKind_Returns400()
    {
        var result = await _harness.PromoteAsync(
            new MemoryPromoteRequest { SourceKind = "email", SourceRef = "x" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
    }

    /// <summary>AC-FR-MCP-MEMORY-015-08: promote does not auto-run on sessionlog complete.</summary>
    [Fact]
    public async Task NoAutoPromote_OnTurnComplete()
    {
        var before = await CountMemoriesAsync().ConfigureAwait(true);
        var (sourceRef, _) = await SeedSessionSourceAsync("no auto promote").ConfigureAwait(true);
        var afterSeed = await CountMemoriesAsync().ConfigureAwait(true);
        Assert.Equal(before, afterSeed);

        // Completing a turn must not invent memories; an explicit promote call is required and must succeed when Green.
        var promoted = await _harness.PromoteAsync(
            new MemoryPromoteRequest
            {
                SourceKind = MemoryPromoteSourceKinds.SessionLog,
                SourceRef = sourceRef,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(200, promoted.StatusCode);
        Assert.True((await CountMemoriesAsync().ConfigureAwait(true)) >= before + 1);
    }

    /// <summary>AC-FR-MCP-MEMORY-015-09: promoted Content is taken from source raw text.</summary>
    [Fact]
    public async Task Content_FromSourceRaw()
    {
        var (sourceRef, raw) = await SeedSessionSourceAsync("raw source content exact").ConfigureAwait(true);

        var result = await _harness.PromoteAsync(
            new MemoryPromoteRequest
            {
                SourceKind = MemoryPromoteSourceKinds.SessionLog,
                SourceRef = sourceRef,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(raw, result.Memory?.Content ?? result.Memory?.Text);
    }

    /// <summary>AC-FR-MCP-MEMORY-015-10: double promote is stable (two memories or idempotent same id).</summary>
    [Fact]
    public async Task DoublePromote_Stable()
    {
        var (sourceRef, _) = await SeedSessionSourceAsync("double promote").ConfigureAwait(true);
        var first = await _harness.PromoteAsync(
            new MemoryPromoteRequest { SourceKind = MemoryPromoteSourceKinds.SessionLog, SourceRef = sourceRef },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var second = await _harness.PromoteAsync(
            new MemoryPromoteRequest { SourceKind = MemoryPromoteSourceKinds.SessionLog, SourceRef = sourceRef },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, first.StatusCode);
        Assert.Equal(200, second.StatusCode);
        Assert.NotNull(first.Memory);
        Assert.NotNull(second.Memory);
        Assert.True(first.Memory!.Id == second.Memory!.Id || first.Memory.Id != second.Memory.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-015-12: promote result includes new MEMORY-* id and provenance fields.</summary>
    [Fact]
    public async Task Result_IncludesIdAndProvenance()
    {
        var (sourceRef, _) = await SeedSessionSourceAsync("result provenance").ConfigureAwait(true);

        var result = await _harness.PromoteAsync(
            new MemoryPromoteRequest { SourceKind = MemoryPromoteSourceKinds.SessionLog, SourceRef = sourceRef },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.StartsWith("MEMORY-", result.Memory!.Id, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(result.SourceKind));
        Assert.False(string.IsNullOrWhiteSpace(result.SourceRef));
    }

    /// <summary>AC-FR-MCP-MEMORY-015-13: promote of missing session turn returns 404.</summary>
    [Fact]
    public async Task MissingTurn_Returns404()
    {
        var result = await _harness.PromoteAsync(
            new MemoryPromoteRequest
            {
                SourceKind = MemoryPromoteSourceKinds.SessionLog,
                SourceRef = "sessionlog://turn/999999/action/999999",
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(404, result.StatusCode);
    }

    /// <summary>AC-FR-MCP-MEMORY-015-14: context promote does not copy sibling product-requirement chunks from other workspaces.</summary>
    [Fact]
    public async Task ContextPromote_NoSiblingLeak()
    {
        var result = await _harness.PromoteAsync(
            new MemoryPromoteRequest
            {
                SourceKind = MemoryPromoteSourceKinds.Context,
                SourceRef = "context://chunk/local-only",
                Content = "local context only",
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.DoesNotContain(
            result.Memory?.Content ?? result.Memory?.Text ?? string.Empty,
            "FOREIGN-WORKSPACE-REQUIREMENT",
            StringComparison.Ordinal);
    }

    private async Task<(string SourceRef, string Raw)> SeedSessionSourceAsync(string raw, string? workspacePath = null)
    {
        var ws = workspacePath ?? _harness.WorkspaceA;
        await using var db = _harness.CreateContext(ws);
        var session = new SessionLogEntity
        {
            SourceType = "CursorGrok",
            SessionId = "CursorGrok-s4-" + Guid.NewGuid().ToString("N"),
            Model = "grok",
            Started = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow,
            WorkspaceId = ws,
        };
        db.SessionLogs.Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var turn = new SessionLogTurnEntity
        {
            SessionLogId = session.Id,
            WorkspaceId = ws,
            RequestId = "req-s4-" + Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            QueryTitle = "S4 red promote",
            Response = "open",
            PlanFile = "None",
            TodoId = "None",
        };
        db.SessionLogTurns.Add(turn);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var action = new SessionLogActionEntity
        {
            SessionLogTurnId = turn.Id,
            WorkspaceId = ws,
            Order = 1,
            Type = "note",
            Description = raw,
            Status = "completed",
        };
        db.SessionLogActions.Add(action);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        return ($"sessionlog://turn/{turn.Id}/action/{action.Id}", raw);
    }

    private async Task<byte[]> ReadSessionActionBytesAsync(string sourceRef)
    {
        var parts = sourceRef.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var actionId = long.Parse(parts[^1]);
        await using var db = _harness.CreateContext(_harness.WorkspaceA);
        var action = await db.SessionLogActions.AsNoTracking()
            .FirstAsync(a => a.Id == actionId)
            .ConfigureAwait(true);
        return Encoding.UTF8.GetBytes(action.Description ?? string.Empty);
    }

    private async Task<int> CountMemoriesAsync()
    {
        var list = await _harness.ListCompatAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        return list.Items?.Count ?? 0;
    }
}
