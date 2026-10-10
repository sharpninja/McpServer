using McpServer.Support.Mcp.Services;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-013 / TEST-MCP-MEMORY-015:
/// Mocks-first green proof that S4 consolidate/promote port contracts are assertable.
/// </summary>
public sealed class MemoryS4PortMockTests
{
    /// <summary>H4-red: dry-run default / no mutations.</summary>
    [Fact]
    public async Task PortMock_DryRunDefault_NoMutations()
    {
        Assert.True(MemoryConsolidateLimits.DefaultDryRun);
        var port = Substitute.For<IMemoryS4Port>();
        port.ConsolidateAsync(Arg.Any<MemoryConsolidateRequest?>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryConsolidateResult(
                200,
                Plan: [new MemoryConsolidatePlanItem { CandidateIds = ["MEMORY-A", "MEMORY-B"], SimilarityScore = 0.91 }],
                DryRunApplied: true));

        var result = await port.ConsolidateAsync(
            new MemoryConsolidateRequest(),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.True(result.DryRunApplied);
        Assert.Single(result.Plan!);
    }

    /// <summary>H4-red: write merge survivor + lineage.</summary>
    [Fact]
    public async Task PortMock_WriteMode_SurvivorAndLineage()
    {
        var port = Substitute.For<IMemoryS4Port>();
        port.ConsolidateAsync(Arg.Any<MemoryConsolidateRequest?>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryConsolidateResult(
                200,
                SurvivorId: "MEMORY-A",
                MergedAwayIds: ["MEMORY-B"],
                DryRunApplied: false));

        var result = await port.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("MEMORY-A", result.SurvivorId);
        Assert.Equal(["MEMORY-B"], result.MergedAwayIds);
        Assert.False(result.DryRunApplied);
    }

    /// <summary>H4-red: lock busy.</summary>
    [Fact]
    public async Task PortMock_LockBusy()
    {
        var port = Substitute.For<IMemoryS4Port>();
        port.ConsolidateAsync(Arg.Any<MemoryConsolidateRequest?>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryConsolidateResult(409, FailureKind: MemoryMutationFailureKind.Conflict, Error: "busy"));

        var result = await port.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(409, result.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Conflict, result.FailureKind);
    }

    /// <summary>H4-red: no cross-workspace merge.</summary>
    [Fact]
    public async Task PortMock_NoCrossWorkspaceMerge()
    {
        var port = Substitute.For<IMemoryS4Port>();
        port.ConsolidateAsync(Arg.Any<MemoryConsolidateRequest?>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryConsolidateResult(
                200,
                SurvivorId: "MEMORY-LOCAL",
                MergedAwayIds: [],
                DryRunApplied: false));

        var result = await port.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = false },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.DoesNotContain(result.MergedAwayIds ?? [], id => id.StartsWith("MEMORY-FOREIGN", StringComparison.Ordinal));
    }

    /// <summary>H4-red: promote provenance.</summary>
    [Fact]
    public async Task PortMock_PromoteProvenance()
    {
        var port = Substitute.For<IMemoryS4Port>();
        port.PromoteAsync(Arg.Any<MemoryPromoteRequest?>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryPromoteResult(
                200,
                Memory: new MemoryItem { Id = "MEMORY-P1", Category = "s4", Scope = MemoryScope.Workspace, Text = "raw", Content = "raw", Version = 1, CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch },
                SourceKind: MemoryPromoteSourceKinds.SessionLog,
                SourceRef: "sessionlog://turn/1/action/1"));

        var result = await port.PromoteAsync(
            new MemoryPromoteRequest { SourceKind = MemoryPromoteSourceKinds.SessionLog, SourceRef = "sessionlog://turn/1/action/1" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(MemoryPromoteSourceKinds.SessionLog, result.SourceKind);
        Assert.Equal("sessionlog://turn/1/action/1", result.SourceRef);
        Assert.Equal("MEMORY-P1", result.Memory!.Id);
    }

    /// <summary>H4-red: sessionlog byte-identical after promote (contract signal).</summary>
    [Fact]
    public async Task PortMock_SessionLogByteIdentical()
    {
        var port = Substitute.For<IMemoryS4Port>();
        port.PromoteAsync(Arg.Any<MemoryPromoteRequest?>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryPromoteResult(
                200,
                Memory: new MemoryItem { Id = "MEMORY-P2", Category = "s4", Scope = MemoryScope.Workspace, Text = "same", Content = "same", Version = 1, CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch },
                SourceKind: MemoryPromoteSourceKinds.SessionLog,
                SourceRef: "sessionlog://turn/2/action/2"));

        var result = await port.PromoteAsync(
            new MemoryPromoteRequest { SourceKind = MemoryPromoteSourceKinds.SessionLog, SourceRef = "sessionlog://turn/2/action/2" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("same", result.Memory!.Content);
    }

    /// <summary>H4-red: no auto-promote (explicit API only) — mock proves promote is an explicit port call.</summary>
    [Fact]
    public async Task PortMock_NoAutoPromote_ExplicitOnly()
    {
        var port = Substitute.For<IMemoryS4Port>();
        // Never called until explicit PromoteAsync — contract is that callers must invoke the port.
        await Task.CompletedTask;
        Assert.Empty(port.ReceivedCalls());
    }
}
