using McpServer.Support.Mcp.Services;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 / TEST-MCP-MEMORY-014 / TEST-MCP-MEMORY-016:
/// Mocks-first green proof that S1 port contracts are assertable. Production S1
/// implementation is intentionally absent (Red tests live in the matrix classes).
/// </summary>
public sealed class MemoryS1PortMockTests
{
    /// <summary>AC-FR-MCP-MEMORY-010-01: mock remember returns a MEMORY-* id.</summary>
    [Fact]
    public async Task PortMock_Remember_ValidPayload_ReturnsMemoryId()
    {
        var port = Substitute.For<IMemoryS1Port>();
        port.RememberAsync(Arg.Any<MemoryRememberRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryRememberResult(201, "MEMORY-FACT-001"));

        var result = await port.RememberAsync(
            new MemoryRememberRequest { Content = "prefer neovim", Type = "preference" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(201, result.StatusCode);
        Assert.StartsWith("MEMORY-", result.MemoryId, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-08: mock remember rejects empty content with 400.</summary>
    [Fact]
    public async Task PortMock_Remember_EmptyContent_Returns400()
    {
        var port = Substitute.For<IMemoryS1Port>();
        port.RememberAsync(Arg.Any<MemoryRememberRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryRememberResult(400, FailureKind: MemoryMutationFailureKind.Validation));

        var result = await port.RememberAsync(
            new MemoryRememberRequest { Content = string.Empty, Type = "fact" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, result.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-014-03: mock revert restores snapshot content.</summary>
    [Fact]
    public async Task PortMock_Revert_RestoresSnapshot()
    {
        var port = Substitute.For<IMemoryS1Port>();
        port.RevertAsync("MEMORY-FACT-001", 1, Arg.Any<CancellationToken>())
            .Returns(new MemoryRevertResult(
                200,
                Memory: new MemoryItem
                {
                    Id = "MEMORY-FACT-001",
                    Category = "FACT",
                    Scope = MemoryScope.Workspace,
                    Text = "original",
                    Content = "original",
                    Version = 3,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                }));

        var result = await port.RevertAsync("MEMORY-FACT-001", 1, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("original", result.Memory?.Content);
    }
}
