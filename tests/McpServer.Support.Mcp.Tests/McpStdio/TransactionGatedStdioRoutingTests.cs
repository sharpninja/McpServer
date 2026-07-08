using System.Text.Json;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.AgentHelp;
using McpServer.Support.Mcp.Storage;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-MCP-161: Verifies STDIO tools route already-decorated mutation seams through injected services.
/// </summary>
public sealed class TransactionGatedStdioRoutingTests : IDisposable
{
    private const string WorkspacePath = @"F:\GitHub\McpServer";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly McpDbContext _db;
    private readonly IRepoFileService _repoFileService = Substitute.For<IRepoFileService>();
    private readonly ISessionLogService _sessionLogService = Substitute.For<ISessionLogService>();
    private readonly IPromptTemplateService _promptTemplateService = Substitute.For<IPromptTemplateService>();
    private readonly IRequirementsDocumentService _requirementsDocumentService = Substitute.For<IRequirementsDocumentService>();
    private readonly FwhMcpTools _tools;

    /// <summary>Initializes a STDIO tool fixture with substituted decorated mutation services.</summary>
    public TransactionGatedStdioRoutingTests()
    {
        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"TransactionGatedStdioRoutingTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(dbOptions);
        _db.Database.EnsureCreated();
        _tools = CreateTools(_db, _repoFileService, _sessionLogService, _promptTemplateService, _requirementsDocumentService);
    }

    /// <summary>Disposes the in-memory metadata context used by the fixture.</summary>
    public void Dispose()
    {
        _db.Dispose();
    }

    /// <summary>todo_projection_repair delegates to the transaction-gated TODO mutation service.</summary>
    [Fact]
    public async Task TodoProjectionRepair_DelegatesToInjectedTodoMutationService()
    {
        var todoMutations = Substitute.For<ITransactionGatedTodoMutationService>();
        todoMutations.RepairProjectionAsync(Arg.Any<CancellationToken>())
            .Returns(new TodoProjectionRepairResult(
                false,
                "TODO projection repair is not transaction compensated while required turn transactions are active.",
                new TodoProjectionStatusResult(
                    "turn-transaction-gate",
                    "turn-transaction-gate",
                    "TODO.yaml",
                    false,
                    false,
                    true,
                    "2026-06-14T00:00:00.0000000Z",
                    Message: "TODO projection repair is not transaction compensated while required turn transactions are active.")));
        var tools = CreateTools(
            _db,
            _repoFileService,
            _sessionLogService,
            _promptTemplateService,
            _requirementsDocumentService,
            todoMutations);

        var json = await tools.TodoProjectionRepair(WorkspacePath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<TodoProjectionRepairResult>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Contains("not transaction compensated", result.Error, StringComparison.Ordinal);
        await todoMutations.Received(1).RepairProjectionAsync(Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>repo_write delegates to the injected repo file service, which DI decorates for transaction gating.</summary>
    [Fact]
    public async Task RepoWrite_DelegatesToInjectedRepoFileService()
    {
        _repoFileService.WriteAsync("docs/Project/txn.md", "content", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RepoWriteResult(true, null)));

        var json = await _tools.RepoWrite("docs/Project/txn.md", "content", WorkspacePath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("written").GetBoolean());
        await _repoFileService.Received(1)
            .WriteAsync("docs/Project/txn.md", "content", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>prompt_template_create delegates to the injected prompt-template service.</summary>
    [Fact]
    public async Task PromptTemplateCreate_DelegatesToInjectedPromptTemplateService()
    {
        _promptTemplateService.CreateAsync(Arg.Any<PromptTemplateCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PromptTemplateMutationResult(true, Item: CreateTemplate("txn-stdio"))));

        var json = await _tools.PromptTemplateCreate(
                WorkspacePath,
                "txn-stdio",
                "Transaction stdio",
                "txn",
                "Hello {{name}}",
                tags: "routing,stdio",
                description: "Route through decorated service.",
                engine: "handlebars", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<PromptTemplateMutationResult>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        await _promptTemplateService.Received(1)
            .CreateAsync(
                Arg.Is<PromptTemplateCreateRequest>(request => request != null
                    && request.Id == "txn-stdio"
                    && request.Title == "Transaction stdio"
                    && request.Category == "txn"
                    && request.Content == "Hello {{name}}"
                    && request.Tags != null
                    && request.Tags.SequenceEqual(new[] { "routing", "stdio" })),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>prompt_template_update delegates to the injected prompt-template service.</summary>
    [Fact]
    public async Task PromptTemplateUpdate_DelegatesToInjectedPromptTemplateService()
    {
        _promptTemplateService.UpdateAsync("txn-stdio", Arg.Any<PromptTemplateUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PromptTemplateMutationResult(true, Item: CreateTemplate("txn-stdio"))));

        var json = await _tools.PromptTemplateUpdate(
                WorkspacePath,
                "txn-stdio",
                title: "Updated",
                content: "Updated {{name}}",
                tags: "routing", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<PromptTemplateMutationResult>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        await _promptTemplateService.Received(1)
            .UpdateAsync(
                "txn-stdio",
                Arg.Is<PromptTemplateUpdateRequest>(request => request != null
                    && request.Title == "Updated"
                    && request.Content == "Updated {{name}}"
                    && request.Tags != null
                    && request.Tags.SequenceEqual(new[] { "routing" })),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>prompt_template_delete delegates to the injected prompt-template service.</summary>
    [Fact]
    public async Task PromptTemplateDelete_DelegatesToInjectedPromptTemplateService()
    {
        _promptTemplateService.DeleteAsync("txn-stdio", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PromptTemplateMutationResult(true, Item: CreateTemplate("txn-stdio"))));

        var json = await _tools.PromptTemplateDelete(WorkspacePath, "txn-stdio", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<PromptTemplateMutationResult>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        await _promptTemplateService.Received(1)
            .DeleteAsync("txn-stdio", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>requirements_generate markdown export delegates to the injected requirements service.</summary>
    [Fact]
    public async Task RequirementsGenerateAllMarkdown_DelegatesToInjectedRequirementsService()
    {
        _requirementsDocumentService.GenerateAllAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateExport("markdown", "all")));

        var json = await _tools.RequirementsGenerate(WorkspacePath, doc: "all", format: "markdown", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<RequirementsDocumentExportResult>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        await _requirementsDocumentService.Received(1)
            .GenerateAllAsync(
                Path.Combine(WorkspacePath, "docs", "Project"),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>requirements_generate wiki export delegates to the injected requirements service.</summary>
    [Fact]
    public async Task RequirementsGenerateWiki_DelegatesToInjectedRequirementsService()
    {
        _requirementsDocumentService.GenerateWikiAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateExport("wiki", "all")));

        var json = await _tools.RequirementsGenerate(WorkspacePath, doc: "all", format: "wiki", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<RequirementsDocumentExportResult>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        await _requirementsDocumentService.Received(1)
            .GenerateWikiAsync(
                Path.Combine(WorkspacePath, "docs", "Project", "wiki"),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>requirements_create FR delegates to the injected requirements service.</summary>
    [Fact]
    public async Task RequirementsCreateFunctional_DelegatesToInjectedRequirementsService()
    {
        _requirementsDocumentService.AddFrAsync(Arg.Any<FrEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var json = await _tools.RequirementsCreate(
                "fr",
                "FR-MCP-TXNSTDIO-001",
                WorkspacePath,
                title: "STDIO transaction route",
                body: "Route FR creation through the decorated service.", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        await _requirementsDocumentService.Received(1)
            .AddFrAsync(
                Arg.Is<FrEntry>(entry => entry != null
                    && entry.Id == "FR-MCP-TXNSTDIO-001"
                    && entry.Title == "STDIO transaction route"
                    && entry.Body == "Route FR creation through the decorated service."),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>requirements_update FR reads and updates through the injected requirements service.</summary>
    [Fact]
    public async Task RequirementsUpdateFunctional_DelegatesToInjectedRequirementsService()
    {
        _requirementsDocumentService.GetFrAsync("FR-MCP-TXNSTDIO-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<FrEntry?>(new FrEntry("FR-MCP-TXNSTDIO-001", "Old", "Old body")));
        _requirementsDocumentService.UpdateFrAsync(Arg.Any<FrEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var json = await _tools.RequirementsUpdate(
                "fr",
                "FR-MCP-TXNSTDIO-001",
                WorkspacePath,
                title: "Updated",
                body: "Updated body", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        await _requirementsDocumentService.Received(1)
            .UpdateFrAsync(
                Arg.Is<FrEntry>(entry => entry != null
                    && entry.Id == "FR-MCP-TXNSTDIO-001"
                    && entry.Title == "Updated"
                    && entry.Body == "Updated body"),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>requirements_update mapping delegates to the injected requirements service.</summary>
    [Fact]
    public async Task RequirementsUpdateMapping_DelegatesToInjectedRequirementsService()
    {
        _requirementsDocumentService.GetMappingAsync("FR-MCP-TXNSTDIO-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<FrTrMapping?>(new FrTrMapping("FR-MCP-TXNSTDIO-001", ["TR-OLD-001"], ["TEST-OLD-001"])));
        _requirementsDocumentService.UpsertMappingAsync(Arg.Any<FrTrMapping>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var json = await _tools.RequirementsUpdate(
                "mapping",
                "FR-MCP-TXNSTDIO-001",
                WorkspacePath,
                body: "TR-MCP-TXN-001,TR-MCP-TXN-002",
                testIds: "TEST-MCP-161", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        await _requirementsDocumentService.Received(1)
            .UpsertMappingAsync(
                Arg.Is<FrTrMapping>(mapping => mapping != null
                    && mapping.FrId == "FR-MCP-TXNSTDIO-001"
                    && mapping.TrIds.SequenceEqual(new[] { "TR-MCP-TXN-001", "TR-MCP-TXN-002" })
                    && mapping.TestIds.SequenceEqual(new[] { "TEST-MCP-161" })),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>requirements_delete FR delegates to the injected requirements service.</summary>
    [Fact]
    public async Task RequirementsDeleteFunctional_DelegatesToInjectedRequirementsService()
    {
        _requirementsDocumentService.DeleteFrAsync("FR-MCP-TXNSTDIO-001", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var json = await _tools.RequirementsDelete("fr", "FR-MCP-TXNSTDIO-001", WorkspacePath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        await _requirementsDocumentService.Received(1)
            .DeleteFrAsync("FR-MCP-TXNSTDIO-001", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>sessionlog_replace_turn delegates to the injected session-log service.</summary>
    [Fact]
    public async Task SessionLogReplaceTurn_DelegatesToInjectedSessionLogService()
    {
        _sessionLogService.ReplaceTurnAsync("Codex", "Codex-20260614T120000Z-stdio", Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>())
            .Returns(42L);
        var turnJson = JsonSerializer.Serialize(new UnifiedRequestEntryDto
        {
            QueryText = "replace through decorated service",
            Actions =
            [
                new UnifiedActionDto
                {
                    Order = 0,
                    Description = "replace",
                    Type = "edit",
                    Status = "completed",
                },
            ],
        });

        var json = await _tools.SessionLogReplaceTurn(
                "Codex",
                "Codex-20260614T120000Z-stdio",
                "req-20260614T120000Z-sessionlog-stdio",
                turnJson,
                WorkspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.True(document.RootElement.GetProperty("replaced").GetBoolean());
        await _sessionLogService.Received(1)
            .ReplaceTurnAsync(
                "Codex",
                "Codex-20260614T120000Z-stdio",
                Arg.Is<UnifiedRequestEntryDto>(turn => turn != null
                    && turn.RequestId == "req-20260614T120000Z-sessionlog-stdio"
                    && turn.QueryText == "replace through decorated service"
                    && turn.Actions != null
                    && turn.Actions.Count == 1),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>sessionlog_delete_session delegates to the injected session-log service.</summary>
    [Fact]
    public async Task SessionLogDeleteSession_DelegatesToInjectedSessionLogService()
    {
        _sessionLogService.DeleteSessionAsync("Codex", "Codex-20260614T120000Z-stdio", Arg.Any<CancellationToken>())
            .Returns(true);

        var json = await _tools.SessionLogDeleteSession(
                "Codex",
                "Codex-20260614T120000Z-stdio",
                WorkspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.True(document.RootElement.GetProperty("deleted").GetBoolean());
        await _sessionLogService.Received(1)
            .DeleteSessionAsync("Codex", "Codex-20260614T120000Z-stdio", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>github_create_issue delegates to the injected GitHub CLI service.</summary>
    [Fact]
    public async Task GitHubCreateIssue_DelegatesToInjectedGitHubCliService()
    {
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        gitHubCliService.CreateIssueAsync("txn issue", "blocked", Arg.Any<CancellationToken>())
            .Returns(new GitHubCreateIssueResult(false, null, null, "GitHub mutations are not transaction compensated."));
        var tools = CreateTools(
            _db,
            _repoFileService,
            _sessionLogService,
            _promptTemplateService,
            _requirementsDocumentService,
            gitHubCliService: gitHubCliService);

        var json = await tools.GitHubCreateIssue("txn issue", "blocked", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.Contains("not transaction compensated", document.RootElement.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
        await gitHubCliService.Received(1)
            .CreateIssueAsync("txn issue", "blocked", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>context_ingest_website fails closed before calling the website ingestor when required transactions are active.</summary>
    [Fact]
    public async Task ContextIngestWebsite_WhenTransactionsRequired_ReturnsErrorWithoutCallingWebsiteIngestor()
    {
        var websiteIngestor = Substitute.For<IWebsiteIngestor>();
        var tools = CreateTools(
            _db,
            _repoFileService,
            _sessionLogService,
            _promptTemplateService,
            _requirementsDocumentService,
            websiteIngestor: websiteIngestor,
            transactionCoordinator: new CapturingCoordinator(enabled: true));

        var json = await tools.ContextIngestWebsite("https://example.test/docs", WorkspacePath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.Equal("turn_transaction_gate", document.RootElement.GetProperty("code").GetString());
        Assert.Contains("not transaction compensated", document.RootElement.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
        await websiteIngestor.DidNotReceive()
            .IngestAsync(Arg.Any<WebsiteIngestRequest>(), Arg.Any<Func<WebsiteIngestPage, Task>?>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>sync_run fails closed before starting durable context sync when required transactions are active.</summary>
    [Fact]
    public async Task SyncRun_WhenTransactionsRequired_ReturnsErrorWithoutWritingContextRows()
    {
        var tools = CreateTools(
            _db,
            _repoFileService,
            _sessionLogService,
            _promptTemplateService,
            _requirementsDocumentService,
            transactionCoordinator: new CapturingCoordinator(enabled: true));

        var json = await tools.SyncRun(WorkspacePath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.Equal("turn_transaction_gate", document.RootElement.GetProperty("code").GetString());
        Assert.Contains("not transaction compensated", document.RootElement.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await _db.Documents.ToListAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    private static PromptTemplate CreateTemplate(string id)
        => new()
        {
            Id = id,
            Title = "Transaction stdio",
            Category = "txn",
            Content = "Hello {{name}}",
            Tags = ["routing", "stdio"],
            Description = "Route through decorated service.",
        };

    private static RequirementsDocumentExportResult CreateExport(string format, string docType)
        => new()
        {
            Success = true,
            Format = format,
            DocType = docType,
            GeneratedAtUtc = DateTimeOffset.Parse("2026-06-14T06:00:00Z"),
            OutputRoot = WorkspacePath,
            Files =
            [
                new RequirementsDocumentExportFile
                {
                    RelativePath = "Functional-Requirements.md",
                    FullPath = Path.Combine(WorkspacePath, "docs", "Project", "Functional-Requirements.md"),
                    ContentType = "text/markdown",
                    LastModifiedUtc = DateTimeOffset.Parse("2026-06-14T06:00:00Z"),
                }
            ],
        };

    private static FwhMcpTools CreateTools(
        McpDbContext db,
        IRepoFileService repoFileService,
        ISessionLogService sessionLogService,
        IPromptTemplateService promptTemplateService,
        IRequirementsDocumentService requirementsDocumentService,
        ITransactionGatedTodoMutationService? todoMutations = null,
        IGitHubCliService? gitHubCliService = null,
        IWebsiteIngestor? websiteIngestor = null,
        ITurnTransactionCoordinator? transactionCoordinator = null,
        TurnTransactionOptions? transactionOptions = null)
    {
        var ingestionOptions = MsOptions.Options.Create(new IngestionOptions { RepoRoot = "." });
        var workspaceContext = new WorkspaceContext { WorkspacePath = "." };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        gitHubCliService ??= Substitute.For<IGitHubCliService>();
        websiteIngestor ??= Substitute.For<IWebsiteIngestor>();
        var chunker = new Chunker();
        var repoIngestor = new RepoIngestor(chunker, ingestionOptions, workspaceContext, NullLogger<RepoIngestor>.Instance);
        var sessionLogIngestor = new SessionLogIngestor(chunker, ingestionOptions, workspaceContext, sessionLogService, NullLogger<SessionLogIngestor>.Instance);
        var externalDocsIngestor = new ExternalDocsIngestor(chunker, ingestionOptions, workspaceContext, NullLogger<ExternalDocsIngestor>.Instance);
        var gitHubIngestor = new GitHubIngestor(chunker, gitHubCliService, NullLogger<GitHubIngestor>.Instance);
        var issueIngestor = new IssueIngestor(chunker, gitHubCliService, NullLogger<IssueIngestor>.Instance);
        var coordinator = new IngestionCoordinator(
            db,
            repoIngestor,
            sessionLogIngestor,
            externalDocsIngestor,
            gitHubIngestor,
            issueIngestor,
            websiteIngestor,
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<IVectorIndexService>(),
            null,
            workspaceContext,
            NullLogger<IngestionCoordinator>.Instance);
        var todoService = Substitute.For<ITodoService>();
        var todoServiceResolver = new TodoServiceResolver(todoService, ingestionOptions, Substitute.For<ITodoServiceFactory>());
        var workspaceAccessor = new WorkspaceServiceAccessor(todoServiceResolver, httpContextAccessor, ingestionOptions);
        var desktopLaunchService = new DesktopLaunchService(
            Substitute.For<IConfiguration>(),
            MsOptions.Options.Create(new DesktopLaunchOptions()),
            Substitute.For<IProcessRunner>(),
            NullLogger<DesktopLaunchService>.Instance);
        var todoCreationService = new TodoCreationService(workspaceAccessor, gitHubCliService, NullLogger<TodoCreationService>.Instance);
        var todoUpdateService = new TodoUpdateService(workspaceAccessor, null, NullLogger<TodoUpdateService>.Instance);

        return new FwhMcpTools(
            db,
            repoFileService,
            coordinator,
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IContextSearchService>(),
            Substitute.For<IGraphRagService>(),
            workspaceAccessor,
            Substitute.For<ITodoPromptService>(),
            sessionLogService,
            Substitute.For<IMemoryService>(),
            gitHubCliService,
            requirementsDocumentService,
            desktopLaunchService,
            httpContextAccessor,
            workspaceContext,
            Substitute.For<IWorkspaceService>(),
            Substitute.For<IWorkspacePolicyService>(),
            todoServiceResolver,
            todoCreationService,
            todoUpdateService,
            Substitute.For<ITodoExecutionService>(),
            promptTemplateService,
            NullLogger<FwhMcpTools>.Instance,
            todoMutations: todoMutations,
            transactionCoordinator: transactionCoordinator,
            transactionOptions: MsOptions.Options.Create(transactionOptions ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }),
            agentHelpService: Substitute.For<IAgentHelpConversationService>());
    }

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        private readonly TurnTransactionStatusResponse _status;

        public CapturingCoordinator(bool enabled, bool degraded = false, string message = "")
        {
            _status = new TurnTransactionStatusResponse
            {
                Enabled = enabled,
                Degraded = degraded,
                Message = message,
            };
        }

        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public TurnTransactionStatusResponse GetStatus() => _status;
    }
}
