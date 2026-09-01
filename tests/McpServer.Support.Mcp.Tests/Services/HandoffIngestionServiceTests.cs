using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MsOptions = Microsoft.Extensions.Options.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-HANDOFF-002 through TEST-HANDOFF-007: real-service tests for source security,
/// extraction, validation, modes, TODO persistence, replay, and audit storage.
/// Uses an in-memory SQLite workspace and a mocked one-shot extractor.
/// </summary>
public sealed class HandoffIngestionServiceTests : IDisposable
{
    private readonly string _workspace;
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _dbOptions;
    private readonly ManualTimeProvider _time = new(new DateTimeOffset(2026, 8, 16, 18, 0, 0, TimeSpan.Zero));

    /// <summary>Creates an isolated workspace and SQLite database for handoff tests.</summary>
    public HandoffIngestionServiceTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "handoff-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var db = CreateDb(_workspace);
        db.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_workspace))
            Directory.Delete(_workspace, recursive: true);
    }

    /// <summary>TEST-HANDOFF-002: Markdown, text, JSON, and YAML files are accepted when contained.</summary>
    [Theory]
    [InlineData("notes.md", "# Handoff")]
    [InlineData("notes.txt", "plain text")]
    [InlineData("notes.json", "{\"title\":\"x\"}")]
    [InlineData("notes.yaml", "title: x")]
    public async Task IngestAsync_SupportedFormats_AreAccepted(string fileName, string body)
    {
        File.WriteAllText(Path.Combine(_workspace, fileName), body);
        var extractor = SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-001"));
        var sut = CreateService(extractor, Substitute.For<ITodoService>());

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Path,
            Path = fileName,
            Mode = HandoffIngestionMode.DraftOnly,
        }, TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Error);
        Assert.False(result.Created);
        Assert.Equal("MCP-HANDOFFDEMO-001", result.Draft!.Id);
    }

    /// <summary>TEST-HANDOFF-002: missing, unsupported, oversized, traversal, and external paths fail closed.</summary>
    [Fact]
    public async Task IngestAsync_UnsafeSources_ProduceDiagnosticsAndNoTodo()
    {
        var todo = Substitute.For<ITodoService>();
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        var sut = CreateService(extractor, todo);

        var missing = await sut.IngestAsync(new HandoffIngestionRequest { SourceKind = HandoffSourceKind.Path, Path = "missing.md" }, TestContext.Current.CancellationToken);
        var unsupported = await sut.IngestAsync(new HandoffIngestionRequest { SourceKind = HandoffSourceKind.Path, Path = "notes.bin" }, TestContext.Current.CancellationToken);
        File.WriteAllText(Path.Combine(_workspace, "notes.bin"), "x");
        unsupported = await sut.IngestAsync(new HandoffIngestionRequest { SourceKind = HandoffSourceKind.Path, Path = "notes.bin" }, TestContext.Current.CancellationToken);
        var traversal = await sut.IngestAsync(new HandoffIngestionRequest { SourceKind = HandoffSourceKind.Path, Path = "..\\outside.md" }, TestContext.Current.CancellationToken);
        var external = await sut.IngestAsync(new HandoffIngestionRequest { SourceKind = HandoffSourceKind.Path, Path = Path.Combine(Path.GetTempPath(), "outside.md") }, TestContext.Current.CancellationToken);
        var oversized = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = new string('x', HandoffPromptDefaults.MaxDecodedBytes + 1),
        }, TestContext.Current.CancellationToken);

        Assert.Contains(missing.Diagnostics, item => item.Code == "source_missing");
        Assert.Contains(unsupported.Diagnostics, item => item.Code == "source_unsupported");
        Assert.Contains(traversal.Diagnostics, item => item.Code == "source_traversal");
        Assert.Contains(external.Diagnostics, item => item.Code == "source_external");
        Assert.Contains(oversized.Diagnostics, item => item.Code == "source_oversized");
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
        await extractor.DidNotReceiveWithAnyArgs().ExtractAsync(default!, default!, default, default, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>TEST-HANDOFF-003: malformed extractor JSON never creates a TODO.</summary>
    [Fact]
    public async Task IngestAsync_MalformedExtractorJson_DoesNotCreateTodo()
    {
        var todo = Substitute.For<ITodoService>();
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffExtractionResult
            {
                Success = true,
                ResponseText = "```json\n{\"id\":\"MCP-X-001\"}\n```",
                PromptVersion = HandoffPromptDefaults.PromptVersion,
            });
        var sut = CreateService(extractor, todo);

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "handoff",
            Mode = HandoffIngestionMode.CreateWhenConfident,
        }, TestContext.Current.CancellationToken);

        Assert.Contains(result.Diagnostics, item => item.Code == "extract_malformed");
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>TEST-HANDOFF-003: unknown source notes are preserved.</summary>
    [Fact]
    public async Task IngestAsync_UnknownSourceNotes_AreNotDiscarded()
    {
        var extractor = SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-002", unknownNotes: "author missing"));
        var sut = CreateService(extractor, Substitute.For<ITodoService>());

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "handoff",
        }, TestContext.Current.CancellationToken);

        Assert.Contains("author missing", result.Draft!.UnknownSourceNotes);
    }

    /// <summary>TEST-HANDOFF-004: DraftOnly never mutates TODO state.</summary>
    [Fact]
    public async Task IngestAsync_DraftOnly_DoesNotCreateTodo()
    {
        var todo = Substitute.For<ITodoService>();
        var sut = CreateService(SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-003", confidence: 0.99)), todo);

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "handoff",
            Mode = HandoffIngestionMode.DraftOnly,
        }, TestContext.Current.CancellationToken);

        Assert.False(result.Created);
        Assert.False(result.RequiresReview);
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>TEST-HANDOFF-004: low confidence CreateWhenConfident requires review.</summary>
    [Fact]
    public async Task IngestAsync_LowConfidence_RequiresReview()
    {
        var todo = Substitute.For<ITodoService>();
        var sut = CreateService(SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-004", confidence: 0.4)), todo);

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "handoff",
            Mode = HandoffIngestionMode.CreateWhenConfident,
        }, TestContext.Current.CancellationToken);

        Assert.True(result.RequiresReview);
        Assert.False(result.Created);
        Assert.Contains(result.Diagnostics, item => item.Code == "mode_low_confidence");
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>TEST-HANDOFF-005: confident creation produces exactly one TODO through ITodoService.</summary>
    [Fact]
    public async Task IngestAsync_CreateWhenConfident_CreatesExactlyOneTodo()
    {
        var todo = Substitute.For<ITodoService>();
        todo.GetByIdAsync("MCP-HANDOFFDEMO-005", Arg.Any<CancellationToken>()).Returns((TodoFlatItem?)null);
        todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var created = ci.Arg<TodoCreateRequest>() ?? throw new InvalidOperationException("Create request was null.");
                return new TodoMutationResult(true, Item: new TodoFlatItem
                {
                    Id = created.Id,
                    Title = created.Title,
                    Section = created.Section,
                    Priority = created.Priority,
                    Done = false,
                });
            });
        var sut = CreateService(SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-005", confidence: 0.9)), todo);

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "handoff",
            Mode = HandoffIngestionMode.CreateWhenConfident,
        }, TestContext.Current.CancellationToken);

        Assert.True(result.Created, result.Error);
        Assert.Equal("MCP-HANDOFFDEMO-005", result.CreatedTodoId);
        await todo.Received(1).CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>TEST-HANDOFF-005: replay of the same workspace, hash, and prompt version returns the existing receipt.</summary>
    [Fact]
    public async Task IngestAsync_SameHashAndPrompt_ReplaysExistingRun()
    {
        var extractor = SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-006"));
        var sut = CreateService(extractor, Substitute.For<ITodoService>());
        var request = new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "same-handoff-body",
            Mode = HandoffIngestionMode.DraftOnly,
        };

        var first = await sut.IngestAsync(request, TestContext.Current.CancellationToken);
        var second = await sut.IngestAsync(request, TestContext.Current.CancellationToken);

        Assert.False(first.Replayed);
        Assert.True(second.Replayed);
        Assert.Equal(first.Provenance!.RunId, second.Provenance!.RunId);
        await extractor.Received(1).ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>TEST-HANDOFF-005: ID collisions require review and are never renamed.</summary>
    [Fact]
    public async Task IngestAsync_DuplicateTodoId_RequiresReview()
    {
        var todo = Substitute.For<ITodoService>();
        todo.GetByIdAsync("MCP-HANDOFFDEMO-007", Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem { Id = "MCP-HANDOFFDEMO-007", Title = "Existing", Section = "MCP Server", Priority = "high", Done = false });
        var sut = CreateService(SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-007", confidence: 0.95)), todo);

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "collision",
            Mode = HandoffIngestionMode.CreateWhenConfident,
        }, TestContext.Current.CancellationToken);

        Assert.True(result.RequiresReview);
        Assert.Contains(result.Diagnostics, item => item.Code == "todo_collision");
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>TEST-HANDOFF-007: persisted runs keep provenance and omit raw source content.</summary>
    [Fact]
    public async Task IngestAsync_PersistsProvenanceWithoutSourceContent()
    {
        var secret = "super-secret-handoff-body";
        var sut = CreateService(SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-008")), Substitute.For<ITodoService>());

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = secret,
        }, TestContext.Current.CancellationToken);

        using var db = CreateDb(_workspace);
        var entity = await db.HandoffIngestionRuns.Include(run => run.Diagnostics).SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(result.Provenance!.RunId, entity.RunId);
        Assert.False(string.IsNullOrWhiteSpace(entity.ContentSha256));
        Assert.DoesNotContain(secret, entity.DraftJson ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, entity.SourceLocator, StringComparison.Ordinal);
        Assert.All(entity.Diagnostics, item => Assert.DoesNotContain(secret, item.Message, StringComparison.Ordinal));
    }

    /// <summary>TEST-HANDOFF-004: approval revalidates and then creates through the TODO service.</summary>
    [Fact]
    public async Task ApproveAsync_RevalidatesThenCreates()
    {
        var todo = Substitute.For<ITodoService>();
        todo.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((TodoFlatItem?)null);
        todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var created = ci.Arg<TodoCreateRequest>() ?? throw new InvalidOperationException("Create request was null.");
                return new TodoMutationResult(true, Item: new TodoFlatItem
                {
                    Id = created.Id,
                    Title = "x",
                    Section = "s",
                    Priority = "high",
                    Done = false,
                });
            });
        var sut = CreateService(SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-009", confidence: 0.9)), todo);
        var ingested = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "approve-me",
            Mode = HandoffIngestionMode.RequireReview,
        }, TestContext.Current.CancellationToken);

        var approved = await sut.ApproveAsync(ingested.Provenance!.RunId, new HandoffApprovalRequest
        {
            Approved = true,
            Reviewer = "operator",
        }, TestContext.Current.CancellationToken);

        Assert.True(approved.Created);
        Assert.Equal("MCP-HANDOFFDEMO-009", approved.CreatedTodoId);
        await todo.Received(1).CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>TEST-HANDOFF-004: extractor cancellation propagates and never creates a TODO.</summary>
    [Fact]
    public async Task IngestAsync_ExtractorCancelled_DoesNotCreateTodo()
    {
        var todo = Substitute.For<ITodoService>();
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<HandoffExtractionResult>(_ => throw new OperationCanceledException());
        var sut = CreateService(extractor, todo);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "handoff",
            Mode = HandoffIngestionMode.CreateWhenConfident,
        }, TestContext.Current.CancellationToken));
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>TEST-HANDOFF-003: missing required extractor fields never create a TODO.</summary>
    [Fact]
    public async Task IngestAsync_MissingExtractorFields_DoesNotCreateTodo()
    {
        var todo = Substitute.For<ITodoService>();
        var extractor = SuccessfulExtractor("""{"id":"MCP-HANDOFFDEMO-011","section":"MCP Server","priority":"high","confidence":0.9}""");
        var sut = CreateService(extractor, todo);

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "handoff",
            Mode = HandoffIngestionMode.CreateWhenConfident,
        }, TestContext.Current.CancellationToken);

        Assert.False(result.Created);
        Assert.Contains(result.Diagnostics, item => item.Field == "title");
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>TEST-HANDOFF-004: ambiguous unknown-source notes require review instead of creating.</summary>
    [Fact]
    public async Task IngestAsync_AmbiguousHandoff_RequiresReview()
    {
        var todo = Substitute.For<ITodoService>();
        var sut = CreateService(
            SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-012", confidence: 0.5, unknownNotes: "scope is unclear")),
            todo);

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "ambiguous handoff",
            Mode = HandoffIngestionMode.CreateWhenConfident,
        }, TestContext.Current.CancellationToken);

        Assert.True(result.RequiresReview);
        Assert.False(result.Created);
        Assert.Contains("scope is unclear", result.Draft!.UnknownSourceNotes);
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>TEST-HANDOFF-005: TODO-service failure is recorded and does not invent a TODO.</summary>
    [Fact]
    public async Task IngestAsync_TodoServiceFailure_RequiresReview()
    {
        var todo = Substitute.For<ITodoService>();
        todo.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((TodoFlatItem?)null);
        todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(false, Error: "store unavailable"));
        var sut = CreateService(SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-013", confidence: 0.95)), todo);

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "handoff-create-fail",
            Mode = HandoffIngestionMode.CreateWhenConfident,
        }, TestContext.Current.CancellationToken);

        Assert.False(result.Created);
        Assert.True(result.RequiresReview);
        Assert.Contains(result.Diagnostics, item => item.Code == "todo_create_failed");
    }

    /// <summary>TEST-HANDOFF-005: force=true extracts again instead of replaying.</summary>
    [Fact]
    public async Task IngestAsync_ForceTrue_ExtractsAgain()
    {
        var extractor = SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-014"));
        var sut = CreateService(extractor, Substitute.For<ITodoService>());
        var request = new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "force-body",
            Mode = HandoffIngestionMode.DraftOnly,
            Force = true,
        };

        var first = await sut.IngestAsync(request, TestContext.Current.CancellationToken);
        var second = await sut.IngestAsync(request, TestContext.Current.CancellationToken);

        Assert.False(first.Replayed);
        Assert.False(second.Replayed);
        Assert.NotEqual(first.Provenance!.RunId, second.Provenance!.RunId);
        await extractor.Received(2).ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>TEST-HANDOFF-002: artifact sources resolve contained document chunks.</summary>
    [Fact]
    public async Task IngestAsync_ArtifactSource_UsesDocumentChunks()
    {
        using (var seed = CreateDb(_workspace))
        {
            seed.Documents.Add(new ContextDocumentEntity
            {
                Id = "artifact-handoff-001",
                WorkspaceId = _workspace,
                SourceType = "artifact",
                SourceKey = "artifact-handoff-001",
                IngestedAt = DateTime.UtcNow,
                ContentHash = "abc",
            });
            seed.Chunks.Add(new ContextChunkEntity
            {
                Id = "chunk-handoff-001",
                WorkspaceId = _workspace,
                DocumentId = "artifact-handoff-001",
                Content = "artifact handoff body",
                ChunkIndex = 0,
                TokenCount = 3,
            });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var sut = CreateService(SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-015")), Substitute.For<ITodoService>());
        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Artifact,
            ArtifactId = "artifact-handoff-001",
        }, TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Error);
        Assert.Equal(HandoffSourceKind.Artifact, result.Provenance!.SourceKind);
        Assert.StartsWith("artifact:", result.Provenance.SourceLocator, StringComparison.Ordinal);
    }

    /// <summary>TEST-HANDOFF-002: reparse-point escapes fail closed.</summary>
    [Fact]
    public async Task IngestAsync_ReparseEscape_IsRejected()
    {
        var outsideRoot = Path.Combine(Path.GetTempPath(), "handoff-outside-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outsideRoot);
        var outsideFile = Path.Combine(outsideRoot, "secret.md");
        File.WriteAllText(outsideFile, "secret-outside");
        var linkPath = Path.Combine(_workspace, "escape.md");
        try
        {
            File.CreateSymbolicLink(linkPath, outsideFile);
            var todo = Substitute.For<ITodoService>();
            var sut = CreateService(SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-016")), todo);

            var result = await sut.IngestAsync(new HandoffIngestionRequest
            {
                SourceKind = HandoffSourceKind.Path,
                Path = "escape.md",
            }, TestContext.Current.CancellationToken);

            Assert.Contains(result.Diagnostics, item => item.Code == "source_reparse" || item.Code == "source_external");
            await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(linkPath))
                File.Delete(linkPath);
            if (Directory.Exists(outsideRoot))
                Directory.Delete(outsideRoot, recursive: true);
        }
    }

    /// <summary>TEST-HANDOFF-007: runs from another workspace are not replayed.</summary>
    [Fact]
    public async Task IngestAsync_DifferentWorkspace_DoesNotReplay()
    {
        var otherWorkspace = Path.Combine(Path.GetTempPath(), "handoff-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(otherWorkspace);
        try
        {
            var extractor = SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-017"));
            var first = CreateService(extractor, Substitute.For<ITodoService>(), _workspace);
            var second = CreateService(extractor, Substitute.For<ITodoService>(), otherWorkspace);
            var request = new HandoffIngestionRequest
            {
                SourceKind = HandoffSourceKind.Content,
                Content = "shared-hash-body",
            };

            var a = await first.IngestAsync(request, TestContext.Current.CancellationToken);
            var b = await second.IngestAsync(request, TestContext.Current.CancellationToken);

            Assert.False(a.Replayed);
            Assert.False(b.Replayed);
            Assert.NotEqual(a.Provenance!.RunId, b.Provenance!.RunId);
            await extractor.Received(2).ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            if (Directory.Exists(otherWorkspace))
                Directory.Delete(otherWorkspace, recursive: true);
        }
    }

    /// <summary>TEST-HANDOFF-004: concurrent approvals create at most one TODO.</summary>
    [Fact]
    public async Task ApproveAsync_ConcurrentApprovals_CreateOnce()
    {
        var created = 0;
        var todo = Substitute.For<ITodoService>();
        todo.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => created == 0 ? null : new TodoFlatItem
        {
            Id = "MCP-HANDOFFDEMO-018",
            Title = "Demo",
            Section = "MCP Server",
            Priority = "high",
            Done = false,
        });
        todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                Interlocked.Increment(ref created);
                var request = ci.Arg<TodoCreateRequest>() ?? throw new InvalidOperationException("Create request was null.");
                return new TodoMutationResult(true, Item: new TodoFlatItem
                {
                    Id = request.Id,
                    Title = request.Title,
                    Section = request.Section,
                    Priority = request.Priority,
                    Done = false,
                });
            });
        var sut = CreateService(SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-018", confidence: 0.9)), todo);
        var ingested = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "approve-race",
            Mode = HandoffIngestionMode.RequireReview,
        }, TestContext.Current.CancellationToken);

        var first = sut.ApproveAsync(ingested.Provenance!.RunId, new HandoffApprovalRequest { Approved = true, Reviewer = "a" }, TestContext.Current.CancellationToken);
        var second = sut.ApproveAsync(ingested.Provenance.RunId, new HandoffApprovalRequest { Approved = true, Reviewer = "b" }, TestContext.Current.CancellationToken);
        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, created);
        Assert.Contains(results, item => item.Created || item.Replayed);
        Assert.All(results, item => Assert.Equal("MCP-HANDOFFDEMO-018", item.CreatedTodoId ?? ingested.Draft!.Id));
    }

    /// <summary>
    /// TEST-HANDOFF-004 / FR-HANDOFF-003: ingesting a draft whose description and technicalDetails
    /// are only blank lines produces field-specific diagnostics and never creates a TODO.
    /// </summary>
    [Fact]
    public async Task IngestAsync_BlankDescriptionAndTechnicalDetails_ReturnFieldDiagnosticsAndDoNotCreateTodo()
    {
        var todo = Substitute.For<ITodoService>();
        const string json =
            """{"id":"MCP-HANDOFFDEMO-019","title":"Demo","section":"MCP Server","priority":"high","estimate":"2h","description":[" ","\t"],"technicalDetails":["   "],"implementationTasks":[{"task":"Write tests","done":false}],"dependsOn":[],"functionalRequirements":["FR-HANDOFF-001"],"technicalRequirements":["TR-HANDOFF-CONTRACT-001"],"confidence":0.8,"unknownSourceNotes":[]}""";
        var sut = CreateService(SuccessfulExtractor(json), todo);

        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "blank fields",
            Mode = HandoffIngestionMode.CreateWhenConfident,
        }, TestContext.Current.CancellationToken);

        Assert.False(result.Created);
        Assert.Null(result.CreatedTodoId);
        Assert.Contains(result.Diagnostics, item => item.Field == "description" && item.Code == "draft_invalid_description");
        Assert.Contains(result.Diagnostics, item => item.Field == "technicalDetails" && item.Code == "draft_invalid_technicalDetails");
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    private IHandoffIngestionService CreateService(IHandoffOneShotExtractor extractor, ITodoService todo, string? workspacePath = null)
    {
        var workspace = workspacePath ?? _workspace;
        var db = CreateDb(workspace);
        var ingestionOptions = MsOptions.Create(new IngestionOptions { RepoRoot = workspace });
        var resolver = new TodoServiceResolver(todo, ingestionOptions, Substitute.For<ITodoServiceFactory>());
        var accessor = new WorkspaceServiceAccessor(resolver, Substitute.For<IHttpContextAccessor>(), ingestionOptions);
        SeedCanonicalRequirements(db, workspace);
        return new HandoffIngestionService(
            new HandoffSourceResolver(db),
            extractor,
            new HandoffTodoDraftParser(),
            new HandoffTodoDraftValidator(),
            new HandoffModePolicy(),
            accessor,
            db,
            new SessionLogSanitizer(MsOptions.Create(new SessionLogSanitizationOptions { RegexTimeoutMilliseconds = 5000 })),
            _time,
            new DurabilityDbFactory(_dbOptions, workspace));
    }

    private sealed class DurabilityDbFactory : IDbContextFactory<McpDbContext>
    {
        private readonly DbContextOptions<McpDbContext> _options;
        private readonly string _workspace;
        public DurabilityDbFactory(DbContextOptions<McpDbContext> options, string workspace)
        {
            _options = options;
            _workspace = workspace;
        }

        public McpDbContext CreateDbContext()
            => new(_options, new WorkspaceContext { WorkspacePath = _workspace });
    }

    private static void SeedCanonicalRequirements(McpDbContext db, string workspace)
    {
        if (db.Requirements.Any())
            return;

        db.Requirements.AddRange(
            new RequirementEntity
            {
                WorkspaceId = workspace,
                Kind = "fr",
                Id = "FR-HANDOFF-001",
                Title = "Handoff FR",
                Body = "Seeded",
                Priority = "high",
                Status = "pending",
            },
            new RequirementEntity
            {
                WorkspaceId = workspace,
                Kind = "tr",
                Id = "TR-HANDOFF-CONTRACT-001",
                Title = "Handoff TR",
                Body = "Seeded",
                Priority = "high",
                Status = "pending",
            });
        db.SaveChanges();
    }

    private McpDbContext CreateDb(string workspacePath)
        => new(_dbOptions, new WorkspaceContext { WorkspacePath = workspacePath });

    private static IHandoffOneShotExtractor SuccessfulExtractor(string json)
    {
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffExtractionResult
            {
                Success = true,
                ResponseText = json,
                AgentName = "plan-agent",
                PromptVersion = HandoffPromptDefaults.PromptVersion,
                TemplateVersion = HandoffPromptDefaults.TemplateId,
            });
        return extractor;
    }

    private static string ValidDraftJson(string id, double confidence = 0.8, string? unknownNotes = null)
        => $$"""
            {"id":"{{id}}","title":"Demo","section":"MCP Server","priority":"high","estimate":"2h","description":["Do the work"],"technicalDetails":["Use the service"],"implementationTasks":[{"task":"Write tests","done":false}],"dependsOn":[],"functionalRequirements":["FR-HANDOFF-001"],"technicalRequirements":["TR-HANDOFF-CONTRACT-001"],"confidence":{{confidence.ToString(System.Globalization.CultureInfo.InvariantCulture)}},"unknownSourceNotes":[{{(unknownNotes is null ? "" : $"\"{unknownNotes}\"")}}]}
            """;

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;
        public ManualTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
