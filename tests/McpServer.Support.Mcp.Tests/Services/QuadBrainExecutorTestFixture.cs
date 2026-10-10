using System.Net.Http;
using System.Reflection;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Shared mocked executor used by QBEXEC catalog theories. No Windows service.</summary>
internal sealed class QuadBrainExecutorTestFixture
{
    public ITransactionGatedTodoMutationService Todo { get; } = Substitute.For<ITransactionGatedTodoMutationService>();
    public ITodoService TodoQueries { get; } = Substitute.For<ITodoService>();
    public ITodoPromptService TodoPrompts { get; } = Substitute.For<ITodoPromptService>();
    public IRepoFileService Repo { get; } = Substitute.For<IRepoFileService>();
    public IRequirementsDocumentService Requirements { get; } = Substitute.For<IRequirementsDocumentService>();
    public ISessionLogService SessionLog { get; } = Substitute.For<ISessionLogService>();
    public IGraphRagService GraphRag { get; } = Substitute.For<IGraphRagService>();
    public IDesktopLaunchService Desktop { get; } = Substitute.For<IDesktopLaunchService>();
    public IProcessRunner ProcessRunner { get; } = Substitute.For<IProcessRunner>();
    public IQuadBrainPowerShellSessions PowerShell { get; } = Substitute.For<IQuadBrainPowerShellSessions>();
    public WorkspaceContext Workspace { get; } = new() { WorkspacePath = @"C:\ws" };

    public QuadBrainExecutorTestFixture()
    {
        Todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>()).Returns(new TodoMutationResult(true));
        Todo.UpdateAsync(Arg.Any<string>(), Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>()).Returns(new TodoMutationResult(true));
        Todo.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new TodoMutationResult(true));
        TodoQueries.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>()).Returns(new TodoQueryResult([], 0));
        TodoQueries.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem { Id = "PLAN-X-001", Title = "Do it", Section = "Planning", Priority = "high", Done = false });
        TodoPrompts.StreamPlanAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(EmptyLines());
        TodoPrompts.StreamStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(EmptyLines());
        TodoPrompts.StreamImplementAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(EmptyLines());
        Repo.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new RepoFileReadResult("a.txt", "hello", true));
        Repo.ListAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(new RepoListResult(".", []));
        Repo.WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new RepoWriteResult(true, null));
        Repo.EditAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new RepoEditResult(true, 1, null));
        Requirements.QueryFrAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<FrEntry>());
        Requirements.QueryTrAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<TrEntry>());
        Requirements.QueryTestAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<TestEntry>());
        Requirements.GetFrAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new FrEntry("FR-MCP-X-001", "t", "b"));
        Requirements.GetTrAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new TrEntry("TR-MCP-X-001", "t", "b"));
        Requirements.GetTestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new TestEntry("TEST-MCP-X-001", "does X"));
        SessionLog.OpenSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(true);
        SessionLog.SetSessionTitleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        SessionLog.UpsertTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>()).Returns(1L);
        SessionLog.QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult { TotalCount = 0, Limit = 5, Offset = 0, Items = [] });
        GraphRag.IngestTextAsync(Arg.Any<GraphRagIngestTextRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagIngestTextResponse { DocumentId = "d1", SourceType = "adhoc-text", SourceKey = "k" });
        GraphRag.ListDocumentsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagDocumentListResponse { Documents = [], TotalCount = 0 });
        GraphRag.GetDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagDocumentChunksResponse { DocumentId = "d1", Chunks = [], TotalChunks = 0 });
        GraphRag.DeleteDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagDocumentDeleteResponse { DocumentId = "d1", Success = true });
        GraphRag.CreateEntityAsync(Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphEntityResponse { Id = "e1", Name = "n", EntityType = "t" });
        GraphRag.ListEntitiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new GraphEntityListResponse { Entities = [], TotalCount = 0 });
        GraphRag.GetEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GraphEntityResponse { Id = "e1", Name = "n", EntityType = "t" });
        GraphRag.UpdateEntityAsync(Arg.Any<string>(), Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphEntityResponse { Id = "e1", Name = "n", EntityType = "t" });
        GraphRag.DeleteEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        GraphRag.CreateRelationshipAsync(Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRelationshipResponse { Id = "r1", SourceEntityId = "e1", TargetEntityId = "e2", RelationshipType = "rel" });
        GraphRag.ListRelationshipsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRelationshipListResponse { Relationships = [], TotalCount = 0 });
        GraphRag.GetRelationshipAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRelationshipResponse { Id = "r1", SourceEntityId = "e1", TargetEntityId = "e2", RelationshipType = "rel" });
        GraphRag.UpdateRelationshipAsync(Arg.Any<string>(), Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRelationshipResponse { Id = "r1", SourceEntityId = "e1", TargetEntityId = "e2", RelationshipType = "rel" });
        GraphRag.DeleteRelationshipAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        Desktop.LaunchAsync(Arg.Any<string>(), Arg.Any<DesktopLaunchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DesktopLaunchResult { Success = true, ProcessId = 1 });
        ProcessRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>()).Returns(new ProcessRunResult(0, "ok", null));
        PowerShell.Create(Arg.Any<string>()).Returns("sess-1");
        PowerShell.ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new ProcessRunResult(0, "ok", null));
        PowerShell.Close(Arg.Any<string>()).Returns(true);
    }

    public QuadBrainInternalToolExecutor CreateExecutor() => new(
        Todo, TodoQueries, TodoPrompts, Repo, Requirements, SessionLog, GraphRag, Desktop, ProcessRunner, Workspace, PowerShell);

    public static OpenAiToolCall Call(string name, string arguments)
        => new() { Id = $"call_{name}", Function = new OpenAiFunctionCall { Name = name, Arguments = arguments } };

    public static async Task<string> CatalogArgumentsAsync(QuadBrainInternalToolExecutor sut, string name)
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

    public static void AssertNoHttpClientDependency()
    {
        var ctor = typeof(QuadBrainInternalToolExecutor).GetConstructors().Single();
        Assert.DoesNotContain(ctor.GetParameters(), p => p.ParameterType == typeof(HttpClient) || p.ParameterType.Name.Contains("HttpClient", StringComparison.Ordinal));
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Assert.DoesNotContain(typeof(QuadBrainInternalToolExecutor).GetFields(flags), f => f.FieldType == typeof(HttpClient));
    }

    public async Task AssertMatchingServiceInvokedAsync(string name)
    {
        switch (name)
        {
            case "mcp_todo_query":
            case "mcp_client_invoke":
                await TodoQueries.Received().QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_todo_get":
                await TodoQueries.Received().GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_todo_create":
                await Todo.Received().CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_todo_update":
                await Todo.Received().UpdateAsync(Arg.Any<string>(), Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_todo_delete":
                await Todo.Received().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_todo_plan":
                TodoPrompts.Received().StreamPlanAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
                break;
            case "mcp_todo_status":
                TodoPrompts.Received().StreamStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
                break;
            case "mcp_todo_implementation":
                TodoPrompts.Received().StreamImplementAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
                break;
            case "mcp_repo_read":
                await Repo.Received().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_repo_list":
                await Repo.Received().ListAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_repo_write":
                await Repo.Received().WriteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_repo_edit":
                await Repo.Received().EditAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_list_fr":
                await Requirements.Received().QueryFrAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_list_tr":
                await Requirements.Received().QueryTrAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_list_test":
                await Requirements.Received().QueryTestAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_get_fr":
                await Requirements.Received().GetFrAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_get_tr":
                await Requirements.Received().GetTrAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_get_test":
                await Requirements.Received().GetTestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_create_fr":
                await Requirements.Received().AddFrAsync(Arg.Any<FrEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_update_fr":
                await Requirements.Received().UpdateFrAsync(Arg.Any<FrEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_create_tr":
                await Requirements.Received().AddTrAsync(Arg.Any<TrEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_update_tr":
                await Requirements.Received().UpdateTrAsync(Arg.Any<TrEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_create_test":
                await Requirements.Received().AddTestAsync(Arg.Any<TestEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_requirements_update_test":
                await Requirements.Received().UpdateTestAsync(Arg.Any<TestEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_session_bootstrap":
                await SessionLog.Received().OpenSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_session_update":
                await SessionLog.Received().SetSessionTitleAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_session_turn_begin":
            case "mcp_session_turn_update":
            case "mcp_session_turn_complete":
                await SessionLog.Received().UpsertTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_session_query_history":
                await SessionLog.Received().QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_desktop_launch":
                await Desktop.Received().LaunchAsync(Arg.Any<string>(), Arg.Any<DesktopLaunchRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_powershell_session_create":
                PowerShell.Received().Create(Arg.Any<string>());
                break;
            case "mcp_powershell_session_close":
                PowerShell.Received().Close(Arg.Any<string>());
                break;
            case "mcp_powershell_session_command":
                await PowerShell.Received().ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_git":
                await ProcessRunner.Received().RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_ingest_text":
                await GraphRag.Received().IngestTextAsync(Arg.Any<GraphRagIngestTextRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_list_documents":
                await GraphRag.Received().ListDocumentsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_get_document_chunks":
                await GraphRag.Received().GetDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_delete_document":
                await GraphRag.Received().DeleteDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_create_entity":
                await GraphRag.Received().CreateEntityAsync(Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_list_entities":
                await GraphRag.Received().ListEntitiesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_get_entity":
                await GraphRag.Received().GetEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_update_entity":
                await GraphRag.Received().UpdateEntityAsync(Arg.Any<string>(), Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_delete_entity":
                await GraphRag.Received().DeleteEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_create_relationship":
                await GraphRag.Received().CreateRelationshipAsync(Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_list_relationships":
                await GraphRag.Received().ListRelationshipsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_get_relationship":
                await GraphRag.Received().GetRelationshipAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_update_relationship":
                await GraphRag.Received().UpdateRelationshipAsync(Arg.Any<string>(), Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            case "mcp_graphrag_delete_relationship":
                await GraphRag.Received().DeleteRelationshipAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
                break;
            default:
                Assert.Fail($"No mock-invocation mapping for '{name}'.");
                break;
        }
    }

    private static async IAsyncEnumerable<string> EmptyLines()
    {
        await Task.CompletedTask;
        yield break;
    }
}
