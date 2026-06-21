using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-QBEXEC-002: Verifies the concrete QuadBrain internal-tool executor routes MCP-internal mutations
/// (TODO, repo, and FR/TR/TEST requirements) through the transaction-gated services and returns Unhandled for
/// unknown/read-only tools (FR-MCP-QBEXEC-001 AC-4, FR-MCP-QBEXEC-002).
/// </summary>
public sealed class QuadBrainInternalToolExecutorTests
{
    private readonly ITransactionGatedTodoMutationService _todo = Substitute.For<ITransactionGatedTodoMutationService>();
    private readonly IRepoFileService _repo = Substitute.For<IRepoFileService>();
    private readonly IRequirementsDocumentService _requirements = Substitute.For<IRequirementsDocumentService>();

    private QuadBrainInternalToolExecutor CreateSut() => new(_todo, _repo, _requirements);

    private static OpenAiToolCall Call(string name, string arguments)
        => new() { Function = new OpenAiFunctionCall { Name = name, Arguments = arguments } };

    /// <summary>mcp_todo_create routes through the transaction-gated create and reports success.</summary>
    [Fact]
    public async Task Execute_McpTodoCreate_RoutesThroughGatedCreate()
    {
        _todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_todo_create", "{\"id\":\"PLAN-X-001\",\"title\":\"t\",\"section\":\"mvp-app\",\"priority\":\"high\"}"),
            turnId: null).ConfigureAwait(true);

        Assert.True(outcome.Handled);
        Assert.True(outcome.Success);
        await _todo.Received(1).CreateAsync(
            Arg.Is<TodoCreateRequest>(r => r != null && r.Id == "PLAN-X-001" && r.Section == "mvp-app"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>mcp_todo_update routes through the transaction-gated update with the parsed id.</summary>
    [Fact]
    public async Task Execute_McpTodoUpdate_RoutesThroughGatedUpdate()
    {
        _todo.UpdateAsync("X", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_todo_update", "{\"id\":\"X\",\"done\":true}"), turnId: null).ConfigureAwait(true);

        Assert.True(outcome.Success);
        await _todo.Received(1).UpdateAsync("X", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>mcp_todo_update without an id fails without calling the service.</summary>
    [Fact]
    public async Task Execute_McpTodoUpdate_MissingId_Fails()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(Call("mcp_todo_update", "{\"done\":true}"), turnId: null).ConfigureAwait(true);

        Assert.True(outcome.Handled);
        Assert.False(outcome.Success);
        Assert.Contains("id", outcome.Error!, StringComparison.Ordinal);
        await _todo.DidNotReceive().UpdateAsync(Arg.Any<string>(), Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>mcp_repo_edit routes through the transaction-gated repo edit.</summary>
    [Fact]
    public async Task Execute_McpRepoEdit_RoutesThroughRepoService()
    {
        _repo.EditAsync("a.cs", "x", "y", false, null, Arg.Any<CancellationToken>())
            .Returns(new RepoEditResult(true, 1, null));
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_repo_edit", "{\"path\":\"a.cs\",\"oldString\":\"x\",\"newString\":\"y\"}"), turnId: null).ConfigureAwait(true);

        Assert.True(outcome.Success);
        await _repo.Received(1).EditAsync("a.cs", "x", "y", false, null, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>mcp_repo_write routes through the transaction-gated repo write.</summary>
    [Fact]
    public async Task Execute_McpRepoWrite_RoutesThroughRepoService()
    {
        _repo.WriteAsync("a.txt", "hello", Arg.Any<CancellationToken>())
            .Returns(new RepoWriteResult(true, null));
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_repo_write", "{\"path\":\"a.txt\",\"content\":\"hello\"}"), turnId: null).ConfigureAwait(true);

        Assert.True(outcome.Success);
        await _repo.Received(1).WriteAsync("a.txt", "hello", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>A failed mutation maps to a Fail outcome carrying the error.</summary>
    [Fact]
    public async Task Execute_MutationFails_ReturnsFail()
    {
        _repo.WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RepoWriteResult(false, "transaction rejected"));
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_repo_write", "{\"path\":\"a.txt\",\"content\":\"x\"}"), turnId: null).ConfigureAwait(true);

        Assert.True(outcome.Handled);
        Assert.False(outcome.Success);
        Assert.Contains("transaction rejected", outcome.Error!, StringComparison.Ordinal);
    }

    /// <summary>mcp_requirements_create_fr routes through the transaction-gated FR add (FR-MCP-QBEXEC-001 AC-4).</summary>
    [Fact]
    public async Task Execute_McpRequirementsCreateFr_RoutesThroughGatedAdd()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_requirements_create_fr", "{\"id\":\"FR-MCP-X-001\",\"title\":\"t\",\"body\":\"b\"}"),
            turnId: null).ConfigureAwait(true);

        Assert.True(outcome.Success);
        await _requirements.Received(1).AddFrAsync(
            Arg.Is<FrEntry>(e => e != null && e.Id == "FR-MCP-X-001" && e.Title == "t" && e.Body == "b"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>mcp_requirements_update_tr routes through the transaction-gated TR update.</summary>
    [Fact]
    public async Task Execute_McpRequirementsUpdateTr_RoutesThroughGatedUpdate()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_requirements_update_tr", "{\"id\":\"TR-MCP-X-001\",\"title\":\"t\",\"body\":\"b\"}"),
            turnId: null).ConfigureAwait(true);

        Assert.True(outcome.Success);
        await _requirements.Received(1).UpdateTrAsync(
            Arg.Is<TrEntry>(e => e != null && e.Id == "TR-MCP-X-001"), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>mcp_requirements_create_test routes the condition through the transaction-gated TEST add.</summary>
    [Fact]
    public async Task Execute_McpRequirementsCreateTest_RoutesThroughGatedAdd()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_requirements_create_test", "{\"id\":\"TEST-MCP-X-001\",\"condition\":\"does X\"}"),
            turnId: null).ConfigureAwait(true);

        Assert.True(outcome.Success);
        await _requirements.Received(1).AddTestAsync(
            Arg.Is<TestEntry>(e => e != null && e.Id == "TEST-MCP-X-001" && e.Condition == "does X"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>A requirements mutation without an id fails without calling the service.</summary>
    [Fact]
    public async Task Execute_McpRequirementsCreateFr_MissingId_Fails()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_requirements_create_fr", "{\"title\":\"t\",\"body\":\"b\"}"), turnId: null).ConfigureAwait(true);

        Assert.True(outcome.Handled);
        Assert.False(outcome.Success);
        Assert.Contains("id", outcome.Error!, StringComparison.Ordinal);
        await _requirements.DidNotReceive().AddFrAsync(Arg.Any<FrEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>A requirements service failure maps to a Fail outcome rather than throwing.</summary>
    [Fact]
    public async Task Execute_McpRequirementsCreateFr_WhenServiceThrows_ReturnsFail()
    {
        _requirements.AddFrAsync(Arg.Any<FrEntry>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException("duplicate id")));
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_requirements_create_fr", "{\"id\":\"FR-MCP-X-001\",\"title\":\"t\",\"body\":\"b\"}"),
            turnId: null).ConfigureAwait(true);

        Assert.True(outcome.Handled);
        Assert.False(outcome.Success);
        Assert.Contains("duplicate id", outcome.Error!, StringComparison.Ordinal);
    }

    /// <summary>Read-only requirements tools (list/get) return Unhandled (left for the agent).</summary>
    [Fact]
    public async Task Execute_McpRequirementsListFr_ReturnsUnhandled()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(Call("mcp_requirements_list_fr", "{}"), turnId: null).ConfigureAwait(true);

        Assert.False(outcome.Handled);
        Assert.Same(InternalToolExecutionOutcome.Unhandled, outcome);
    }

    /// <summary>An unknown or read-only internal tool returns Unhandled (left for the agent as a note).</summary>
    [Fact]
    public async Task Execute_UnknownTool_ReturnsUnhandled()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(Call("mcp_todo_query", "{}"), turnId: null).ConfigureAwait(true);

        Assert.False(outcome.Handled);
        Assert.Same(InternalToolExecutionOutcome.Unhandled, outcome);
    }

    /// <summary>Malformed arguments fail gracefully rather than throwing.</summary>
    [Fact]
    public async Task Execute_MalformedArguments_FailsGracefully()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(Call("mcp_repo_write", "{not json"), turnId: null).ConfigureAwait(true);

        Assert.True(outcome.Handled);
        Assert.False(outcome.Success);
    }
}
