using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-HANDOFF-004 through TEST-HANDOFF-007: behavior coverage for the audit defects
/// (replay uniqueness, outcome mapping, invalid drafts, sanitization, cancellation).
/// </summary>
public sealed class HandoffAuditDefectTests : IDisposable
{
    private readonly string _workspace;
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _dbOptions;

    /// <summary>Creates an isolated SQLite workspace for audit-defect tests.</summary>
    public HandoffAuditDefectTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "handoff-audit", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(_connection).Options;
        using var db = CreateDb();
        db.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_workspace))
            Directory.Delete(_workspace, recursive: true);
    }

    /// <summary>Concurrent same-hash ingest creates one run and extracts once.</summary>
    [Fact]
    public async Task IngestAsync_ConcurrentSameHash_ReservesOnce()
    {
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                await Task.Delay(50, ci.Arg<CancellationToken>());
                return new HandoffExtractionResult
                {
                    Success = true,
                    ResponseText = ValidDraft("MCP-HANDOFFDEMO-030"),
                    AgentName = "plan-agent",
                    Model = "test-model",
                    PromptVersion = HandoffPromptDefaults.PromptVersion,
                };
            });

        var first = CreateService(extractor);
        var second = CreateService(extractor);
        var request = new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "same-concurrent-body",
            Mode = HandoffIngestionMode.DraftOnly,
        };

        var results = await Task.WhenAll(
            first.IngestAsync(request, TestContext.Current.CancellationToken),
            second.IngestAsync(request, TestContext.Current.CancellationToken));

        Assert.Single(results.Select(item => item.Provenance!.RunId).Distinct());
        Assert.Contains(results, item => item.Success && item.Draft is not null);
        Assert.Contains(results, item => item.ErrorCode == HandoffErrorCodes.InProgress || item.Replayed);
        await extractor.Received(1).ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Failed extraction is persisted as Success=false and GET returns that outcome.</summary>
    [Fact]
    public async Task GetRunAsync_FailedExtraction_DoesNotReportSuccess()
    {
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffExtractionResult { Success = false, Error = "extractor down" });
        var sut = CreateService(extractor);

        var ingested = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "fail-extract",
            Mode = HandoffIngestionMode.CreateWhenConfident,
        }, TestContext.Current.CancellationToken);

        var loaded = await sut.GetRunAsync(ingested.Provenance!.RunId, TestContext.Current.CancellationToken);

        Assert.False(ingested.Success);
        Assert.False(loaded.Success);
        Assert.Equal(HandoffReviewState.Failed, loaded.Provenance!.ReviewState);
        Assert.False(loaded.RequiresReview);
    }

    /// <summary>RequireReview of a malformed draft is failed and not approvable.</summary>
    [Fact]
    public async Task RequireReview_MalformedDraft_IsFailedAndNotApprovable()
    {
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffExtractionResult { Success = true, ResponseText = """{"extra":true}""" });
        var sut = CreateService(extractor);

        var ingested = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "bad-json-object",
            Mode = HandoffIngestionMode.RequireReview,
        }, TestContext.Current.CancellationToken);
        var approved = await sut.ApproveAsync(
            ingested.Provenance!.RunId,
            new HandoffApprovalRequest { Approved = true, Reviewer = "op" },
            TestContext.Current.CancellationToken);

        Assert.False(ingested.Success);
        Assert.False(ingested.RequiresReview);
        Assert.Equal(HandoffReviewState.Failed, ingested.Provenance.ReviewState);
        Assert.False(approved.Success);
        Assert.Equal("run_not_approvable", approved.ErrorCode);
    }

    /// <summary>Cancellation of ingest propagates and does not persist a success row.</summary>
    [Fact]
    public async Task IngestAsync_Cancelled_ThrowsWithoutSuccessfulPersist()
    {
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<HandoffExtractionResult>(_ => throw new OperationCanceledException());
        var sut = CreateService(extractor);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.IngestAsync(
            new HandoffIngestionRequest { SourceKind = HandoffSourceKind.Content, Content = "cancel-me" },
            cts.Token));
    }

    /// <summary>Secrets in draft text are redacted before durable persist.</summary>
    [Fact]
    public async Task IngestAsync_SecretInDraft_IsRedactedOnPersist()
    {
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffExtractionResult
            {
                Success = true,
                ResponseText = """{"id":"MCP-HANDOFFDEMO-031","title":"Demo","section":"MCP Server","priority":"high","description":["password=hunter2"],"technicalDetails":["ok"],"implementationTasks":[],"dependsOn":[],"functionalRequirements":[],"technicalRequirements":[],"confidence":0.8,"unknownSourceNotes":[]}""",
                Model = "test-model",
            });
        var sut = CreateService(extractor);

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "secret-draft",
        }, TestContext.Current.CancellationToken);

        using var db = CreateDb();
        var entity = await db.HandoffIngestionRuns.SingleAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("hunter2", entity.DraftJson ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("hunter2", result.Draft!.Description[0], StringComparison.Ordinal);
    }

    /// <summary>Parser rejects unknown fields and does not treat id-only as success.</summary>
    [Fact]
    public void Parser_UnknownField_IsNotSuccess()
    {
        var parsed = new HandoffTodoDraftParser().Parse("""{"id":"MCP-HANDOFFDEMO-032","mystery":1}""");
        Assert.False(parsed.Success);
        Assert.Contains(parsed.Diagnostics, item => item.Code == "extract_unknown_field");
        Assert.Contains(parsed.Diagnostics, item => item.Field == "title");
    }

    /// <summary>Separate service instances cannot regress a Created approval to PendingReview.</summary>
    [Fact]
    public async Task ApproveAsync_SeparateContexts_CreatedWins()
    {
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffExtractionResult { Success = true, ResponseText = ValidDraft("MCP-HANDOFFDEMO-033"), Model = "m" });
        var todo = Substitute.For<ITodoService>();
        todo.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((TodoFlatItem?)null);
        todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci => new TodoMutationResult(true, Item: new TodoFlatItem
            {
                Id = ci.Arg<TodoCreateRequest>()!.Id,
                Title = "t",
                Section = "s",
                Priority = "high",
                Done = false,
            }));

        var first = CreateService(extractor, todo);
        var ingested = await first.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "approve-race-body",
            Mode = HandoffIngestionMode.RequireReview,
        }, TestContext.Current.CancellationToken);

        var left = CreateService(extractor, todo);
        var right = CreateService(extractor, todo);
        var results = await Task.WhenAll(
            left.ApproveAsync(ingested.Provenance!.RunId, new HandoffApprovalRequest { Approved = true, Reviewer = "a" }, TestContext.Current.CancellationToken),
            right.ApproveAsync(ingested.Provenance.RunId, new HandoffApprovalRequest { Approved = true, Reviewer = "b" }, TestContext.Current.CancellationToken));

        Assert.Contains(results, item => item.Created || item.Replayed);
        Assert.DoesNotContain(results, item => item.Provenance?.ReviewState == HandoffReviewState.PendingReview && item.Created);
        using var db = CreateDb();
        var stored = await db.HandoffIngestionRuns.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(nameof(HandoffReviewState.Created), stored.ReviewState);
    }

    private IHandoffIngestionService CreateService(IHandoffOneShotExtractor extractor, ITodoService? todo = null)
    {
        todo ??= Substitute.For<ITodoService>();
        var db = CreateDb();
        if (!db.Requirements.Any())
        {
            db.Requirements.Add(new RequirementEntity
            {
                WorkspaceId = _workspace,
                Kind = "fr",
                Id = "FR-HANDOFF-001",
                Title = "FR",
                Body = "b",
                Priority = "high",
                Status = "pending",
            });
            db.Requirements.Add(new RequirementEntity
            {
                WorkspaceId = _workspace,
                Kind = "tr",
                Id = "TR-HANDOFF-CONTRACT-001",
                Title = "TR",
                Body = "b",
                Priority = "high",
                Status = "pending",
            });
            db.SaveChanges();
        }

        var ingestionOptions = MsOptions.Create(new IngestionOptions { RepoRoot = _workspace });
        var resolver = new TodoServiceResolver(todo, ingestionOptions, Substitute.For<ITodoServiceFactory>());
        var accessor = new WorkspaceServiceAccessor(resolver, Substitute.For<IHttpContextAccessor>(), ingestionOptions);
        return new HandoffIngestionService(
            new HandoffSourceResolver(db),
            extractor,
            new HandoffTodoDraftParser(),
            new HandoffTodoDraftValidator(),
            new HandoffModePolicy(),
            accessor,
            db,
            new SessionLogSanitizer(MsOptions.Create(new SessionLogSanitizationOptions { RegexTimeoutMilliseconds = 5000 })));
    }

    private McpDbContext CreateDb()
        => new(_dbOptions, new WorkspaceContext { WorkspacePath = _workspace });

    private static string ValidDraft(string id)
        => $$"""{"id":"{{id}}","title":"Demo","section":"MCP Server","priority":"high","estimate":"2h","description":["Do the work"],"technicalDetails":["Use the service"],"implementationTasks":[{"task":"Write tests","done":false}],"dependsOn":[],"functionalRequirements":["FR-HANDOFF-001"],"technicalRequirements":["TR-HANDOFF-CONTRACT-001"],"confidence":0.8,"unknownSourceNotes":[]}""";
}
