using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-010: S1 Red acceptance for memory_remember.
/// </summary>
public sealed class MemoryRememberTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-01: memory_remember with a valid payload returns 200/201 and a MEMORY-* id.</summary>
    [Fact]
    public async Task Remember_ValidPayload_ReturnsMemoryId()
    {
        var result = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Title = "Editor preference",
            Content = "BENCH-PREF-EDITOR=neovim",
            Type = "preference",
            Tags = ["bench", "pref"],
            Confidence = 0.95,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.StatusCode is 200 or 201, result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.MemoryId));
        Assert.StartsWith("MEMORY-", result.MemoryId, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-07: Null remember/add body returns 400 with validation FailureKind.</summary>
    [Fact]
    public async Task NullBody_Returns400()
    {
        var remember = await _harness.RememberAsync(null, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var controller = MemoryS1Harness.CreateController(SubstituteService());
        var add = await controller.AddAsync(null, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(
            remember.StatusCode == 400 || MemoryS1Harness.StatusOf(add.Result!) == 400,
            remember.Error ?? "null remember body must be 400");
        Assert.True(
            remember.FailureKind == MemoryMutationFailureKind.Validation
            || add.Result is BadRequestObjectResult);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-21: Client-supplied active MEMORY-* id returns 409 Conflict.</summary>
    [Fact]
    public async Task DuplicateActiveId_Returns409()
    {
        var first = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Id = "MEMORY-CUSTOM-001",
            Content = "first",
            Type = "fact",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        if (first.StatusCode is not (200 or 201))
        {
            var compat = await _harness.AddCompatAsync(new MemoryAddRequest
            {
                Id = "MEMORY-CUSTOM-001",
                Category = "custom",
                Text = "first",
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(compat.Success, compat.Error);
        }

        var duplicate = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Id = "MEMORY-CUSTOM-001",
            Content = "second",
            Type = "fact",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(409, duplicate.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Conflict, duplicate.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-22: Invalid client-supplied MEMORY-* id returns 400.</summary>
    [Fact]
    public async Task InvalidIdFormat_Returns400()
    {
        var result = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Id = "not-a-memory-id",
            Content = "invalid",
            Type = "fact",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, result.FailureKind);
    }

    private static McpServer.Support.Mcp.Services.IMemoryService SubstituteService()
        => NSubstitute.Substitute.For<McpServer.Support.Mcp.Services.IMemoryService>();
}
