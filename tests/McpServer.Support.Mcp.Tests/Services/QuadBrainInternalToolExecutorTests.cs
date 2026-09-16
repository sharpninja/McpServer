using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-QBEXEC-002: Verifies the concrete QuadBrain internal-tool executor routes every catalog mcp_* name
/// through in-process application/CQRS or transaction-gated services. Unknown catalog-absent mcp_ names return
/// Unhandled. Tests mock those services and do not depend on the Windows service.
/// </summary>
public sealed class QuadBrainInternalToolExecutorTests
{
    private readonly ITransactionGatedTodoMutationService _todo = Substitute.For<ITransactionGatedTodoMutationService>();
    private readonly ITodoService _todoQueries = Substitute.For<ITodoService>();
    private readonly ITodoPromptService _todoPrompts = Substitute.For<ITodoPromptService>();
    private readonly IRepoFileService _repo = Substitute.For<IRepoFileService>();
    private readonly IRequirementsDocumentService _requirements = Substitute.For<IRequirementsDocumentService>();
    private readonly ISessionLogService _sessionLog = Substitute.For<ISessionLogService>();
    private readonly IGraphRagService _graphRag = Substitute.For<IGraphRagService>();
    private readonly IDesktopLaunchService _desktop = Substitute.For<IDesktopLaunchService>();
    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
    private readonly IQuadBrainPowerShellSessions _powerShell = Substitute.For<IQuadBrainPowerShellSessions>();
    private readonly WorkspaceContext _workspace = new() { WorkspacePath = @"C:\ws" };

    public QuadBrainInternalToolExecutorTests()
    {
        _todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));
        _todo.UpdateAsync(Arg.Any<string>(), Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));
        _todo.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));
        _todoQueries.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([], 0));
        _todoQueries.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem { Id = "PLAN-X-001", Title = "Do it", Section = "Planning", Priority = "high", Done = false });
        _todoPrompts.StreamPlanAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyLines());
        _todoPrompts.StreamStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EmptyLines());
        _todoPrompts.StreamImplementAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EmptyLines());
        _repo.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RepoFileReadResult("a.txt", "hello", true));
        _repo.ListAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new RepoListResult(".", []));
        _repo.WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RepoWriteResult(true, null));
        _repo.EditAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new RepoEditResult(true, 1, null));
        _requirements.QueryFrAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<FrEntry>());
        _requirements.QueryTrAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TrEntry>());
        _requirements.QueryTestAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TestEntry>());
        _requirements.GetFrAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FrEntry("FR-MCP-X-001", "t", "b"));
        _requirements.GetTrAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TrEntry("TR-MCP-X-001", "t", "b"));
        _requirements.GetTestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TestEntry("TEST-MCP-X-001", "does X"));
        _sessionLog.OpenSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _sessionLog.SetSessionTitleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);
        _sessionLog.UpsertTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>())
            .Returns(1L);
        _sessionLog.QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult { TotalCount = 0, Limit = 5, Offset = 0, Items = [] });
        _graphRag.IngestTextAsync(Arg.Any<GraphRagIngestTextRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagIngestTextResponse { DocumentId = "d1", SourceType = "adhoc-text", SourceKey = "k" });
        _graphRag.ListDocumentsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagDocumentListResponse { Documents = [], TotalCount = 0 });
        _graphRag.GetDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagDocumentChunksResponse { DocumentId = "d1", Chunks = [], TotalChunks = 0 });
        _graphRag.DeleteDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagDocumentDeleteResponse { DocumentId = "d1", Success = true });
        _graphRag.CreateEntityAsync(Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphEntityResponse { Id = "e1", Name = "n", EntityType = "t" });
        _graphRag.ListEntitiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new GraphEntityListResponse { Entities = [], TotalCount = 0 });
        _graphRag.GetEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GraphEntityResponse { Id = "e1", Name = "n", EntityType = "t" });
        _graphRag.UpdateEntityAsync(Arg.Any<string>(), Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphEntityResponse { Id = "e1", Name = "n", EntityType = "t" });
        _graphRag.DeleteEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _graphRag.CreateRelationshipAsync(Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRelationshipResponse { Id = "r1", SourceEntityId = "e1", TargetEntityId = "e2", RelationshipType = "rel" });
        _graphRag.ListRelationshipsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRelationshipListResponse { Relationships = [], TotalCount = 0 });
        _graphRag.GetRelationshipAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRelationshipResponse { Id = "r1", SourceEntityId = "e1", TargetEntityId = "e2", RelationshipType = "rel" });
        _graphRag.UpdateRelationshipAsync(Arg.Any<string>(), Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRelationshipResponse { Id = "r1", SourceEntityId = "e1", TargetEntityId = "e2", RelationshipType = "rel" });
        _graphRag.DeleteRelationshipAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _desktop.LaunchAsync(Arg.Any<string>(), Arg.Any<DesktopLaunchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DesktopLaunchResult { Success = true, ProcessId = 1 });
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "ok", null));
        _powerShell.Create(Arg.Any<string>()).Returns("sess-1");
        _powerShell.ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "ok", null));
        _powerShell.Close(Arg.Any<string>()).Returns(true);
    }

    private QuadBrainInternalToolExecutor CreateSut() => new(
        _todo,
        _todoQueries,
        _todoPrompts,
        _repo,
        _requirements,
        _sessionLog,
        _graphRag,
        _desktop,
        _processRunner,
        _workspace,
        _powerShell);

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
            turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
            Call("mcp_todo_update", "{\"id\":\"X\",\"done\":true}"), turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(outcome.Success);
        await _todo.Received(1).UpdateAsync("X", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>mcp_todo_update without an id fails without calling the service.</summary>
    [Fact]
    public async Task Execute_McpTodoUpdate_MissingId_Fails()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(Call("mcp_todo_update", "{\"done\":true}"), turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
            Call("mcp_repo_edit", "{\"path\":\"a.cs\",\"oldString\":\"x\",\"newString\":\"y\"}"), turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
            Call("mcp_repo_write", "{\"path\":\"a.txt\",\"content\":\"hello\"}"), turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
            Call("mcp_repo_write", "{\"path\":\"a.txt\",\"content\":\"x\"}"), turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
            turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
            turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
            turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
            Call("mcp_requirements_create_fr", "{\"title\":\"t\",\"body\":\"b\"}"), turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
            turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(outcome.Handled);
        Assert.False(outcome.Success);
        Assert.Contains("duplicate id", outcome.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBEXEC-002 AC-2: mcp_requirements_list_fr is handled through QueryFrAsync, not Unhandled.</summary>
    [Fact]
    public async Task Execute_McpRequirementsListFr_RoutesThroughQuery()
    {
        _requirements.QueryFrAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns([new FrEntry("FR-MCP-X-001", "t", "b")]);
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(Call("mcp_requirements_list_fr", "{}"), turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(outcome.Handled);
        Assert.True(outcome.Success);
        Assert.Contains("FR-MCP-X-001", outcome.ResultJson!, StringComparison.Ordinal);
        await _requirements.Received(1).QueryFrAsync(null, null, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>FR-MCP-QBEXEC-002: mcp_todo_query runs through ITodoService (CQRS app service), not HTTP.</summary>
    [Fact]
    public async Task Execute_McpTodoQuery_RoutesThroughTodoService()
    {
        _todoQueries.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult(
                [new TodoFlatItem { Id = "PLAN-X-001", Title = "Do it", Section = "Planning", Priority = "high", Done = false }],
                1));
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_todo_query", "{\"done\":false}"),
            turnId: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(outcome.Handled);
        Assert.True(outcome.Success);
        Assert.Contains("PLAN-X-001", outcome.ResultJson!, StringComparison.Ordinal);
        await _todoQueries.Received(1).QueryAsync(
            Arg.Is<TodoQueryRequest>(r => r != null && r.Done == false),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await _todo.DidNotReceive().CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>FR-MCP-QBEXEC-002: mcp_todo_get runs through ITodoService.GetByIdAsync.</summary>
    [Fact]
    public async Task Execute_McpTodoGet_RoutesThroughTodoService()
    {
        _todoQueries.GetByIdAsync("PLAN-X-001", Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem { Id = "PLAN-X-001", Title = "Do it", Section = "Planning", Priority = "high", Done = false });
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_todo_get", "{\"id\":\"PLAN-X-001\"}"),
            turnId: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(outcome.Success);
        Assert.Contains("Do it", outcome.ResultJson!, StringComparison.Ordinal);
        await _todoQueries.Received(1).GetByIdAsync("PLAN-X-001", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>An unknown mcp_ tool returns Unhandled.</summary>
    [Fact]
    public async Task Execute_UnknownTool_ReturnsUnhandled()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(Call("mcp_not_a_real_tool", "{}"), turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(outcome.Handled);
        Assert.Same(InternalToolExecutionOutcome.Unhandled, outcome);
    }

    /// <summary>Malformed arguments fail gracefully rather than throwing.</summary>
    [Fact]
    public async Task Execute_MalformedArguments_FailsGracefully()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(Call("mcp_repo_write", "{not json"), turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(outcome.Handled);
        Assert.False(outcome.Success);
    }

    /// <summary>TEST-MCP-QBEXEC-002: every catalog mcp_* name is Handled through mocked in-process services.</summary>
    [Theory]
    [MemberData(nameof(CatalogToolNames))]
    public async Task Execute_CatalogName_IsHandled(string name)
    {
        var sut = CreateSut();
        var arguments = await CatalogArgumentsAsync(sut, name).ConfigureAwait(true);

        var outcome = await sut.TryExecuteAsync(
            Call(name, arguments),
            turnId: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(outcome.Handled);
        Assert.NotSame(InternalToolExecutionOutcome.Unhandled, outcome);
        Assert.True(outcome.Success, $"{name} failed: {outcome.Error}");
        await AssertMatchingServiceInvokedAsync(name).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-QBEXEC-002: hosted-agent published mcp_* names are a subset of the executor catalog.</summary>
    [Fact]
    public void Catalog_ContainsEveryHostedAgentPublishedName()
    {
        foreach (var name in QuadBrainMcpToolCatalog.HostedAgentPublishedNames)
            Assert.Contains(name, QuadBrainMcpToolCatalog.All);
    }

    /// <summary>FR-MCP-QBEXEC-002 AC-8 / TR-MCP-QBEXEC-002 AC-2: executor does not take HttpClient.</summary>
    [Fact]
    public void Executor_DoesNotDependOnHttpClient()
        => QuadBrainExecutorTestFixture.AssertNoHttpClientDependency();

    /// <summary>mcp_client_invoke unknown HTTP clients stay Handled Fail, never Unhandled and never HttpClient.</summary>
    [Fact]
    public async Task Execute_McpClientInvoke_UnknownExternalClient_IsHandledFailure()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_client_invoke", "{\"clientName\":\"github\",\"methodName\":\"ListIssuesAsync\"}"),
            turnId: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(outcome.Handled);
        Assert.False(outcome.Success);
        Assert.NotSame(InternalToolExecutionOutcome.Unhandled, outcome);
        Assert.Contains("named mcp_* tool", outcome.Error!, StringComparison.Ordinal);
    }

    /// <summary>mcp_client_invoke session.query routes through ISessionLogService, not HTTP.</summary>
    [Fact]
    public async Task Execute_McpClientInvoke_SessionQuery_RoutesThroughSessionLog()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_client_invoke", "{\"clientName\":\"session\",\"methodName\":\"query\"}"),
            turnId: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(outcome.Success);
        await _sessionLog.Received(1).QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-QBEXEC-002: catalog-absent mcp_* names stay Unhandled.</summary>
    [Fact]
    public async Task Execute_UnknownCatalogAbsentMcpName_ReturnsUnhandled()
    {
        var sut = CreateSut();

        var outcome = await sut.TryExecuteAsync(
            Call("mcp_not_in_catalog", "{}"),
            turnId: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(outcome.Handled);
        Assert.Same(InternalToolExecutionOutcome.Unhandled, outcome);
    }

    public static TheoryData<string> CatalogToolNames()
    {
        var data = new TheoryData<string>();
        foreach (var name in QuadBrainMcpToolCatalog.All)
            data.Add(name);
        return data;
    }

    private static async Task<string> CatalogArgumentsAsync(QuadBrainInternalToolExecutor sut, string name)
    {
        if (name is "mcp_powershell_session_command" or "mcp_powershell_session_close")
        {
            var created = await sut.TryExecuteAsync(
                Call("mcp_powershell_session_create", "{}"),
                turnId: null,
                cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(created.Success);
            using var document = System.Text.Json.JsonDocument.Parse(created.ResultJson!);
            var sessionId = document.RootElement.GetProperty("sessionId").GetString();
            return name == "mcp_powershell_session_command"
                ? $"{{\"sessionId\":\"{sessionId}\",\"command\":\"Get-Date\"}}"
                : $"{{\"sessionId\":\"{sessionId}\"}}";
        }

        return name switch
        {
            "mcp_todo_create" => "{\"id\":\"PLAN-X-001\",\"title\":\"t\",\"section\":\"mvp-app\",\"priority\":\"high\"}",
            "mcp_todo_update" or "mcp_todo_delete" or "mcp_todo_get" or "mcp_todo_plan" or "mcp_todo_status" or "mcp_todo_implementation"
                => "{\"id\":\"PLAN-X-001\"}",
            "mcp_repo_read" => "{\"path\":\"a.txt\"}",
            "mcp_repo_write" => "{\"path\":\"a.txt\",\"content\":\"hello\"}",
            "mcp_repo_edit" => "{\"path\":\"a.cs\",\"oldString\":\"x\",\"newString\":\"y\"}",
            "mcp_requirements_get_fr" or "mcp_requirements_update_fr" or "mcp_requirements_create_fr"
                => "{\"id\":\"FR-MCP-X-001\",\"title\":\"t\",\"body\":\"b\"}",
            "mcp_requirements_get_tr" or "mcp_requirements_update_tr" or "mcp_requirements_create_tr"
                => "{\"id\":\"TR-MCP-X-001\",\"title\":\"t\",\"body\":\"b\"}",
            "mcp_requirements_get_test" or "mcp_requirements_update_test" or "mcp_requirements_create_test"
                => "{\"id\":\"TEST-MCP-X-001\",\"condition\":\"does X\"}",
            "mcp_session_bootstrap" => "{\"agent\":\"GrokCode\",\"sessionId\":\"GrokCode-20260910T000000Z-test\"}",
            "mcp_session_update" => "{\"agent\":\"GrokCode\",\"sessionId\":\"GrokCode-20260910T000000Z-test\",\"title\":\"t\"}",
            "mcp_session_turn_begin" or "mcp_session_turn_update" or "mcp_session_turn_complete"
                => "{\"agent\":\"GrokCode\",\"sessionId\":\"GrokCode-20260910T000000Z-test\",\"requestId\":\"req-20260910T000000Z-test\"}",
            "mcp_desktop_launch" => "{\"executablePath\":\"C:\\\\Windows\\\\System32\\\\notepad.exe\"}",
            "mcp_graphrag_ingest_text" => "{\"content\":\"hello\"}",
            "mcp_graphrag_get_document_chunks" or "mcp_graphrag_delete_document" => "{\"documentId\":\"d1\"}",
            "mcp_graphrag_create_entity" or "mcp_graphrag_update_entity" => "{\"entityId\":\"e1\",\"name\":\"n\",\"entityType\":\"t\"}",
            "mcp_graphrag_get_entity" or "mcp_graphrag_delete_entity" => "{\"entityId\":\"e1\"}",
            "mcp_graphrag_create_relationship" or "mcp_graphrag_update_relationship"
                => "{\"relationshipId\":\"r1\",\"sourceEntityId\":\"e1\",\"targetEntityId\":\"e2\",\"relationshipType\":\"rel\"}",
            "mcp_graphrag_get_relationship" or "mcp_graphrag_delete_relationship" => "{\"relationshipId\":\"r1\"}",
            "mcp_client_invoke" => "{\"clientName\":\"todo\",\"methodName\":\"query\"}",
            "mcp_git" => "{\"command\":\"status\"}",
            _ => "{}",
        };
    }

    private static async IAsyncEnumerable<string> EmptyLines()
    {
        await Task.CompletedTask;
        yield break;
    }

    private async Task AssertMatchingServiceInvokedAsync(string name)
    {
        switch (name)
        {
            case "mcp_todo_query":
            case "mcp_client_invoke":
                await _todoQueries.Received().QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_todo_get":
                await _todoQueries.Received().GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_todo_create":
                await _todo.Received().CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_todo_update":
                await _todo.Received().UpdateAsync(Arg.Any<string>(), Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_todo_delete":
                await _todo.Received().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_todo_plan":
                _todoPrompts.Received().StreamPlanAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
                break;
            case "mcp_todo_status":
                _todoPrompts.Received().StreamStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
                break;
            case "mcp_todo_implementation":
                _todoPrompts.Received().StreamImplementAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
                break;
            case "mcp_repo_read":
                await _repo.Received().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_repo_list":
                await _repo.Received().ListAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_repo_write":
                await _repo.Received().WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_repo_edit":
                await _repo.Received().EditAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_list_fr":
                await _requirements.Received().QueryFrAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_list_tr":
                await _requirements.Received().QueryTrAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_list_test":
                await _requirements.Received().QueryTestAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_get_fr":
                await _requirements.Received().GetFrAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_get_tr":
                await _requirements.Received().GetTrAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_get_test":
                await _requirements.Received().GetTestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_create_fr":
                await _requirements.Received().AddFrAsync(Arg.Any<FrEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_update_fr":
                await _requirements.Received().UpdateFrAsync(Arg.Any<FrEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_create_tr":
                await _requirements.Received().AddTrAsync(Arg.Any<TrEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_update_tr":
                await _requirements.Received().UpdateTrAsync(Arg.Any<TrEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_create_test":
                await _requirements.Received().AddTestAsync(Arg.Any<TestEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_update_test":
                await _requirements.Received().UpdateTestAsync(Arg.Any<TestEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_session_bootstrap":
                await _sessionLog.Received().OpenSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_session_update":
                await _sessionLog.Received().SetSessionTitleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_session_turn_begin":
            case "mcp_session_turn_update":
            case "mcp_session_turn_complete":
                await _sessionLog.Received().UpsertTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_session_query_history":
                await _sessionLog.Received().QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_desktop_launch":
                await _desktop.Received().LaunchAsync(Arg.Any<string>(), Arg.Any<DesktopLaunchRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_powershell_session_create":
                _powerShell.Received().Create(Arg.Any<string>());
                break;
            case "mcp_powershell_session_close":
                _powerShell.Received().Close(Arg.Any<string>());
                break;
            case "mcp_powershell_session_command":
                await _powerShell.Received().ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_git":
                await _processRunner.Received().RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_ingest_text":
                await _graphRag.Received().IngestTextAsync(Arg.Any<GraphRagIngestTextRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_list_documents":
                await _graphRag.Received().ListDocumentsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_get_document_chunks":
                await _graphRag.Received().GetDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_delete_document":
                await _graphRag.Received().DeleteDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_create_entity":
                await _graphRag.Received().CreateEntityAsync(Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_list_entities":
                await _graphRag.Received().ListEntitiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_get_entity":
                await _graphRag.Received().GetEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_update_entity":
                await _graphRag.Received().UpdateEntityAsync(Arg.Any<string>(), Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_delete_entity":
                await _graphRag.Received().DeleteEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_create_relationship":
                await _graphRag.Received().CreateRelationshipAsync(Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_list_relationships":
                await _graphRag.Received().ListRelationshipsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_get_relationship":
                await _graphRag.Received().GetRelationshipAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_update_relationship":
                await _graphRag.Received().UpdateRelationshipAsync(Arg.Any<string>(), Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_delete_relationship":
                await _graphRag.Received().DeleteRelationshipAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            default:
                Assert.Fail($"No mock-invocation mapping for '{name}'.");
                break;
        }
    }
}
