using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FederatedTodoService"/>. Validates merge semantics,
/// pass-through when federation is disabled, and graceful degradation on remote failure.
/// FR-MCP-082, TEST-MCP-FED-001.
/// </summary>
public sealed class FederatedTodoServiceTests
{
    private readonly ITodoService _inner = Substitute.For<ITodoService>();
    private readonly IFederationDataClient _client = Substitute.For<IFederationDataClient>();

    private static FederationRegistry CreateRegistry(bool enabled = false, string? defaultTarget = null)
    {
        var opts = new FederationOptions { Enabled = enabled };
        if (defaultTarget is not null)
        {
            opts.DefaultTarget = defaultTarget;
            opts.Targets.Add(new FederationTargetOptions { Name = defaultTarget, BaseUrl = "http://remote:7147" });
        }
        return new FederationRegistry(Microsoft.Extensions.Options.Options.Create(opts));
    }

    private FederatedTodoService CreateSut(FederationRegistry? registry = null)
    {
        registry ??= CreateRegistry();
        return new FederatedTodoService(
            _inner,
            registry,
            _client,
            NullLogger<FederatedTodoService>.Instance);
    }

    // --- QueryAsync ---

    /// <summary>When federation is disabled, delegates directly to the inner service.</summary>
    [Fact]
    public async Task QueryAsync_FederationDisabled_DelegatesToLocal()
    {
        var expected = new TodoQueryResult([MakeItem("A-001")], 1);
        _inner.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>()).Returns(expected);

        var sut = CreateSut(CreateRegistry(enabled: false));
        var result = await sut.QueryAsync(new TodoQueryRequest());

        Assert.Same(expected, result);
        await _client.DidNotReceiveWithAnyArgs().QueryTodosAsync(default!, default!, default);
    }

    /// <summary>When no federation target resolves, delegates directly to the inner service.</summary>
    [Fact]
    public async Task QueryAsync_NoTargetResolved_DelegatesToLocal()
    {
        var expected = new TodoQueryResult([MakeItem("A-001")], 1);
        _inner.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>()).Returns(expected);

        // Enabled but no targets configured
        var registry = CreateRegistry(enabled: true);
        var sut = CreateSut(registry);
        var result = await sut.QueryAsync(new TodoQueryRequest());

        Assert.Same(expected, result);
        await _client.DidNotReceiveWithAnyArgs().QueryTodosAsync(default!, default!, default);
    }

    /// <summary>When both local and remote return results, merges with local winning on ID collision.</summary>
    [Fact]
    public async Task QueryAsync_BothReturn_MergesLocalWins()
    {
        var localItem = MakeItem("A-001", "Local Title");
        var remoteItem1 = MakeItem("A-001", "Remote Title"); // collision — local wins
        var remoteItem2 = MakeItem("B-002", "Remote Only");

        _inner.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([localItem], 1));
        _client.QueryTodosAsync(Arg.Any<FederationTarget>(), Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([remoteItem1, remoteItem2], 2));

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.QueryAsync(new TodoQueryRequest());

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, i => i.Id == "A-001" && i.Title == "Local Title");
        Assert.Contains(result.Items, i => i.Id == "B-002" && i.Title == "Remote Only");
    }

    /// <summary>When the remote call throws, returns local-only results gracefully.</summary>
    [Fact]
    public async Task QueryAsync_RemoteFails_ReturnsLocalOnly()
    {
        var localItem = MakeItem("A-001");
        _inner.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([localItem], 1));
        _client.QueryTodosAsync(Arg.Any<FederationTarget>(), Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Remote unreachable"));

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.QueryAsync(new TodoQueryRequest());

        Assert.Single(result.Items);
        Assert.Equal("A-001", result.Items[0].Id);
    }

    /// <summary>When the remote returns null, returns local-only results.</summary>
    [Fact]
    public async Task QueryAsync_RemoteReturnsNull_ReturnsLocalOnly()
    {
        var localItem = MakeItem("A-001");
        _inner.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([localItem], 1));
        _client.QueryTodosAsync(Arg.Any<FederationTarget>(), Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns((TodoQueryResult?)null);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.QueryAsync(new TodoQueryRequest());

        Assert.Single(result.Items);
        Assert.Equal("A-001", result.Items[0].Id);
    }

    // --- GetByIdAsync ---

    /// <summary>When found locally, returns the local item without calling remote.</summary>
    [Fact]
    public async Task GetByIdAsync_FoundLocally_ReturnsLocal()
    {
        var localItem = MakeItem("A-001", "Local");
        _inner.GetByIdAsync("A-001", Arg.Any<CancellationToken>()).Returns(localItem);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.GetByIdAsync("A-001");

        Assert.NotNull(result);
        Assert.Equal("Local", result.Title);
        await _client.DidNotReceiveWithAnyArgs().GetTodoByIdAsync(default!, default!, default);
    }

    /// <summary>When not found locally, falls back to remote.</summary>
    [Fact]
    public async Task GetByIdAsync_NotLocalFoundRemote_ReturnsRemote()
    {
        var remoteItem = MakeItem("B-002", "Remote");
        _inner.GetByIdAsync("B-002", Arg.Any<CancellationToken>()).Returns((TodoFlatItem?)null);
        _client.GetTodoByIdAsync(Arg.Any<FederationTarget>(), "B-002", Arg.Any<CancellationToken>())
            .Returns(remoteItem);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.GetByIdAsync("B-002");

        Assert.NotNull(result);
        Assert.Equal("Remote", result.Title);
    }

    /// <summary>When not found in either, returns null.</summary>
    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _inner.GetByIdAsync("X-999", Arg.Any<CancellationToken>()).Returns((TodoFlatItem?)null);
        _client.GetTodoByIdAsync(Arg.Any<FederationTarget>(), "X-999", Arg.Any<CancellationToken>())
            .Returns((TodoFlatItem?)null);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.GetByIdAsync("X-999");

        Assert.Null(result);
    }

    // --- Write operations ---

    /// <summary>CreateAsync always delegates to the inner (local) service.</summary>
    [Fact]
    public async Task CreateAsync_AlwaysDelegatesToLocal()
    {
        var request = new TodoCreateRequest { Id = "C-001", Title = "New", Section = "s", Priority = "high" };
        var expected = new TodoMutationResult(true, Item: MakeItem("C-001"));
        _inner.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.CreateAsync(request);

        Assert.Same(expected, result);
    }

    /// <summary>DeleteAsync always delegates to the inner (local) service.</summary>
    [Fact]
    public async Task DeleteAsync_AlwaysDelegatesToLocal()
    {
        var expected = new TodoMutationResult(true);
        _inner.DeleteAsync("A-001", Arg.Any<CancellationToken>()).Returns(expected);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.DeleteAsync("A-001");

        Assert.Same(expected, result);
    }

    // --- Helpers ---

    private static TodoFlatItem MakeItem(string id, string title = "Test") => new()
    {
        Id = id,
        Title = title,
        Section = "test",
        Priority = "medium",
        Done = false,
    };
}
