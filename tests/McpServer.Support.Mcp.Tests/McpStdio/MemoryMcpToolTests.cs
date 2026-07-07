using System.Text.Json;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-MCP-MEMORY-003: Verifies that the STDIO MCP memory tools delegate to
/// <see cref="IMemoryService"/> and preserve structured JSON contracts.
/// </summary>
public sealed class MemoryMcpToolTests : IDisposable
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly McpDbContext _db;
    private readonly IMemoryService _memoryService = Substitute.For<IMemoryService>();
    private readonly FwhMcpTools _tools;

    /// <summary>Initializes the memory STDIO tool fixture with substituted collaborators.</summary>
    public MemoryMcpToolTests()
    {
        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"MemoryMcpToolTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(dbOptions);
        _db.Database.EnsureCreated();

        _tools = CreateTools(_db, _memoryService);
    }

    /// <summary>Disposes the in-memory metadata context used by the fixture.</summary>
    public void Dispose()
    {
        _db.Dispose();
    }

    /// <summary>TEST-MCP-MEMORY-003: memory_list forwards filters and returns the query result.</summary>
    [Fact]
    public async Task MemoryList_DelegatesToMemoryService()
    {
        _memoryService.ListAsync(
                Arg.Any<MemoryListRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new MemoryQueryResult([CreateMemory("MEMORY-AGENT-001")], 1));

        var json = await _tools.MemoryList(@"F:\GitHub\McpServer", "global", "agent", "remember", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<MemoryQueryResult>(json, s_jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(1, result!.TotalCount);
        Assert.Equal("MEMORY-AGENT-001", result.Items[0].Id);
        await _memoryService.Received(1).ListAsync(
            Arg.Is<MemoryListRequest>(request => request != null
                && request.Scope == MemoryScope.Global
                && request.Category == "agent"
                && request.Keyword == "remember"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-MEMORY-003: memory_list accepts Effective as the explicit default scope.</summary>
    [Fact]
    public async Task MemoryList_WithEffectiveScope_ForwardsNullScope()
    {
        _memoryService.ListAsync(
                Arg.Any<MemoryListRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new MemoryQueryResult([], 0));

        var json = await _tools.MemoryList(@"F:\GitHub\McpServer", "Effective", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<MemoryQueryResult>(json, s_jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result!.TotalCount);
        await _memoryService.Received(1).ListAsync(
            Arg.Is<MemoryListRequest>(request => request != null && request.Scope == null),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-MEMORY-003: memory_get returns the visible memory item by id.</summary>
    [Fact]
    public async Task MemoryGet_DelegatesToMemoryService()
    {
        _memoryService.GetAsync("MEMORY-AGENT-001", Arg.Any<CancellationToken>())
            .Returns(CreateMemory("MEMORY-AGENT-001"));

        var json = await _tools.MemoryGet(@"F:\GitHub\McpServer", "MEMORY-AGENT-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<MemoryItem>(json, s_jsonOptions);

        Assert.NotNull(result);
        Assert.Equal("MEMORY-AGENT-001", result!.Id);
        await _memoryService.Received(1).GetAsync("MEMORY-AGENT-001", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-MEMORY-003: memory_add forwards the creation request and returns mutation state.</summary>
    [Fact]
    public async Task MemoryAdd_DelegatesToMemoryService()
    {
        _memoryService.AddAsync(
                Arg.Any<MemoryAddRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(true, Memory: CreateMemory("MEMORY-AGENT-001", MemoryScope.Global)));

        var json = await _tools.MemoryAdd(
            @"F:\GitHub\McpServer",
            "agent",
            "Preserve exact PowerShell quoting.",
            "Global",
            "MEMORY-AGENT-001",
            "Codex", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<MemoryMutationResult>(json, s_jsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("MEMORY-AGENT-001", result.Memory!.Id);
        await _memoryService.Received(1).AddAsync(
            Arg.Is<MemoryAddRequest>(request => request != null
                && request.Id == "MEMORY-AGENT-001"
                && request.Category == "agent"
                && request.Scope == MemoryScope.Global
                && request.Text == "Preserve exact PowerShell quoting."
                && request.UpdatedBy == "Codex"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-161: memory_add uses the transaction-gated mutation service when registered.</summary>
    [Fact]
    public async Task MemoryAdd_WhenTransactionGateRegistered_DelegatesToTransactionGate()
    {
        var memoryMutations = Substitute.For<ITransactionGatedMemoryService>();
        memoryMutations.AddAsync(
                Arg.Any<MemoryAddRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(true, Memory: CreateMemory("MEMORY-AGENT-001", MemoryScope.Global)));
        var tools = CreateTools(_db, _memoryService, memoryMutations);

        var json = await tools.MemoryAdd(
            @"F:\GitHub\McpServer",
            "agent",
            "Preserve exact PowerShell quoting.",
            "Global",
            "MEMORY-AGENT-001",
            "Codex", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<MemoryMutationResult>(json, s_jsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        await memoryMutations.Received(1).AddAsync(
            Arg.Is<MemoryAddRequest>(request => request != null
                && request.Id == "MEMORY-AGENT-001"
                && request.Category == "agent"
                && request.Scope == MemoryScope.Global
                && request.Text == "Preserve exact PowerShell quoting."
                && request.UpdatedBy == "Codex"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await _memoryService.DidNotReceiveWithAnyArgs().AddAsync(default!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-MEMORY-003: memory_update forwards only supplied replacement fields.</summary>
    [Fact]
    public async Task MemoryUpdate_DelegatesToMemoryService()
    {
        _memoryService.UpdateAsync(
                "MEMORY-AGENT-001",
                Arg.Any<MemoryUpdateRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(true, Memory: CreateMemory("MEMORY-AGENT-001", MemoryScope.Workspace)));

        var json = await _tools.MemoryUpdate(
            @"F:\GitHub\McpServer",
            "MEMORY-AGENT-001",
            "agent",
            "Use supported wrappers for MCP state.",
            "Workspace",
            "Codex", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<MemoryMutationResult>(json, s_jsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        await _memoryService.Received(1).UpdateAsync(
            "MEMORY-AGENT-001",
            Arg.Is<MemoryUpdateRequest>(request => request != null
                && request.Category == "agent"
                && request.Scope == MemoryScope.Workspace
                && request.Text == "Use supported wrappers for MCP state."
                && request.UpdatedBy == "Codex"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-TXN-004: memory_update uses the transaction-gated mutation service when registered.</summary>
    [Fact]
    public async Task MemoryUpdate_WhenTransactionGateRegistered_DelegatesToTransactionGate()
    {
        var memoryMutations = Substitute.For<ITransactionGatedMemoryService>();
        memoryMutations.UpdateAsync(
                "MEMORY-AGENT-001",
                Arg.Any<MemoryUpdateRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(true, Memory: CreateMemory("MEMORY-AGENT-001", MemoryScope.Workspace)));
        var tools = CreateTools(_db, _memoryService, memoryMutations);

        var json = await tools.MemoryUpdate(
            @"F:\GitHub\McpServer",
            "MEMORY-AGENT-001",
            text: "Use supported wrappers for MCP state.", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<MemoryMutationResult>(json, s_jsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        await memoryMutations.Received(1).UpdateAsync(
            "MEMORY-AGENT-001",
            Arg.Is<MemoryUpdateRequest>(request => request != null
                && request.Text == "Use supported wrappers for MCP state."),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await _memoryService.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-MEMORY-003: memory_remove forwards the delete request and returns mutation state.</summary>
    [Fact]
    public async Task MemoryRemove_DelegatesToMemoryService()
    {
        _memoryService.RemoveAsync("MEMORY-AGENT-001", Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(true));

        var json = await _tools.MemoryRemove(@"F:\GitHub\McpServer", "MEMORY-AGENT-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<MemoryMutationResult>(json, s_jsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        await _memoryService.Received(1).RemoveAsync("MEMORY-AGENT-001", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-TXN-004: memory_remove uses the transaction-gated mutation service when registered.</summary>
    [Fact]
    public async Task MemoryRemove_WhenTransactionGateRegistered_DelegatesToTransactionGate()
    {
        var memoryMutations = Substitute.For<ITransactionGatedMemoryService>();
        memoryMutations.RemoveAsync("MEMORY-AGENT-001", Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(true));
        var tools = CreateTools(_db, _memoryService, memoryMutations);

        var json = await tools.MemoryRemove(@"F:\GitHub\McpServer", "MEMORY-AGENT-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<MemoryMutationResult>(json, s_jsonOptions);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        await memoryMutations.Received(1).RemoveAsync("MEMORY-AGENT-001", Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await _memoryService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    /// <summary>Creates a memory item fixture with deterministic timestamps.</summary>
    private static MemoryItem CreateMemory(string id, MemoryScope scope = MemoryScope.Global)
        => new()
        {
            Id = id,
            Category = "AGENT",
            Scope = scope,
            WorkspacePath = scope == MemoryScope.Workspace ? @"F:\GitHub\McpServer" : null,
            Text = "Preserve exact PowerShell quoting.",
            Version = 1,
            CreatedAtUtc = DateTimeOffset.Parse("2026-06-08T07:00:00Z"),
            UpdatedAtUtc = DateTimeOffset.Parse("2026-06-08T07:00:00Z"),
            UpdatedBy = "Codex",
        };

    /// <summary>Builds the shared <see cref="FwhMcpTools"/> fixture for memory tool tests.</summary>
    private static FwhMcpTools CreateTools(
        McpDbContext db,
        IMemoryService memoryService,
        ITransactionGatedMemoryService? memoryMutations = null)
    {
        var ingestionOptions = MsOptions.Options.Create(new IngestionOptions { RepoRoot = "." });
        var workspaceContext = new WorkspaceContext { WorkspacePath = "." };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        var chunker = new Chunker();
        var repoIngestor = new RepoIngestor(chunker, ingestionOptions, workspaceContext, NullLogger<RepoIngestor>.Instance);
        var sessionLogIngestor = new SessionLogIngestor(chunker, ingestionOptions, workspaceContext, Substitute.For<ISessionLogService>(), NullLogger<SessionLogIngestor>.Instance);
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
            Substitute.For<IWebsiteIngestor>(),
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
            Substitute.For<IRepoFileService>(),
            coordinator,
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IContextSearchService>(),
            Substitute.For<IGraphRagService>(),
            workspaceAccessor,
            Substitute.For<ITodoPromptService>(),
            Substitute.For<ISessionLogService>(),
            memoryService,
            gitHubCliService,
            Substitute.For<IRequirementsDocumentService>(),
            desktopLaunchService,
            httpContextAccessor,
            workspaceContext,
            Substitute.For<IWorkspaceService>(),
            Substitute.For<IWorkspacePolicyService>(),
            todoServiceResolver,
            todoCreationService,
            todoUpdateService,
            Substitute.For<ITodoExecutionService>(),
            Substitute.For<IPromptTemplateService>(),
            NullLogger<FwhMcpTools>.Instance,
            memoryMutations);
    }
}
