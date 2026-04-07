using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for core GraphRagService lifecycle operations (status, init, index, query).</summary>
public sealed class GraphRagServiceTests : IDisposable
{
    private readonly string _workspacePath;

    public GraphRagServiceTests()
    {
        _workspacePath = Path.Combine(Path.GetTempPath(), $"graphrag-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspacePath);
    }

    [Fact]
    public async Task InitializeAsync_CreatesRequiredDirectoryStructure()
    {
        var sut = CreateSut(enabled: true);

        var status = await sut.InitializeAsync().ConfigureAwait(true);

        Assert.True(status.IsInitialized);
        Assert.True(Directory.Exists(Path.Combine(status.GraphRoot, "input")));
        Assert.True(Directory.Exists(Path.Combine(status.GraphRoot, "output")));
        Assert.True(Directory.Exists(Path.Combine(status.GraphRoot, "cache")));
        Assert.True(Directory.Exists(Path.Combine(status.GraphRoot, "logs")));
        Assert.True(Directory.Exists(Path.Combine(status.GraphRoot, "config")));
    }

    [Fact]
    public async Task IndexAsync_WritesReadyArtifactAndMarksIndexed()
    {
        var sut = CreateSut(enabled: true);
        await sut.InitializeAsync().ConfigureAwait(true);

        var status = await sut.IndexAsync(new GraphRagIndexRequest { Force = true }).ConfigureAwait(true);

        Assert.True(status.IsIndexed);
        Assert.NotNull(status.LastIndexedAtUtc);
        Assert.Equal("ready", status.State);
        var artifact = Path.Combine(status.GraphRoot, "output", "graphrag-index-ready.json");
        Assert.True(File.Exists(artifact));
    }

    [Fact]
    public async Task QueryAsync_WhenDisabled_ReturnsFallbackReason()
    {
        var sut = CreateSut(enabled: false);

        var response = await sut.QueryAsync(new GraphRagQueryRequest
        {
            Query = "auth",
            IncludeContextChunks = true
        }).ConfigureAwait(true);

        Assert.True(response.FallbackUsed);
        Assert.Equal("graphrag_disabled", response.FallbackReason);
        Assert.Equal("context-search", response.QueryCorpus);
    }

    [Fact]
    public async Task Status_InternalFallback_ReportsCorpusAndInputDiagnostics()
    {
        var sut = CreateSut(enabled: true);
        var initialized = await sut.InitializeAsync().ConfigureAwait(true);
        var localDocPath = Path.Combine(initialized.GraphRoot, "input", "docs", "prg", "Commodore_64_Programmers_Reference_Guide.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(localDocPath)!);
        await File.WriteAllTextAsync(localDocPath, "Video Bank Selection").ConfigureAwait(true);

        var status = await sut.GetStatusAsync().ConfigureAwait(true);

        Assert.Equal("graphrag-input", status.IndexCorpus);
        Assert.Equal("context-search", status.QueryCorpus);
        Assert.Equal(Path.Combine(status.GraphRoot, "input"), status.InputPath);
        Assert.Equal(1, status.InputDocumentCount);
        Assert.Contains("internal-fallback", status.VisibilityNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IndexAsync_WhenExternalCommandFails_PersistsFailureStatus()
    {
        var processRunner = Substitute.For<IProcessRunner>();
        processRunner
            .RunAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(2, null, "backend failed"));

        var sut = CreateSut(
            enabled: true,
            backendCommand: "fake-graphrag.exe",
            processRunner: processRunner);

        var status = await sut.IndexAsync(new GraphRagIndexRequest { Force = false }).ConfigureAwait(true);

        Assert.False(status.IsIndexed);
        Assert.Equal("degraded", status.State);
        Assert.Equal("index_failed", status.FailureCode);
        Assert.NotNull(status.LastError);
    }

    [Fact]
    public async Task Status_WithRootedPath_UsesWorkspaceIsolatedSubfolders()
    {
        var sharedRoot = Path.Combine(_workspacePath, "shared-graphrag-root");
        Directory.CreateDirectory(sharedRoot);

        var workspaceA = Path.Combine(_workspacePath, "ws-a");
        var workspaceB = Path.Combine(_workspacePath, "ws-b");
        Directory.CreateDirectory(workspaceA);
        Directory.CreateDirectory(workspaceB);

        var sutA = CreateSut(enabled: true, workspacePath: workspaceA, rootPath: sharedRoot);
        var sutB = CreateSut(enabled: true, workspacePath: workspaceB, rootPath: sharedRoot);

        var statusA = await sutA.InitializeAsync().ConfigureAwait(true);
        var statusB = await sutB.InitializeAsync().ConfigureAwait(true);

        Assert.NotEqual(statusA.GraphRoot, statusB.GraphRoot);
        Assert.StartsWith(sharedRoot, statusA.GraphRoot, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(sharedRoot, statusB.GraphRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InitializeAsync_RemovesStaleTemporaryArtifacts()
    {
        var sut = CreateSut(enabled: true);
        var status = await sut.InitializeAsync().ConfigureAwait(true);

        var stale = Path.Combine(status.GraphRoot, "cache", "old.tmp");
        await File.WriteAllTextAsync(stale, "stale").ConfigureAwait(true);
        File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddHours(-3));

        _ = await sut.InitializeAsync().ConfigureAwait(true);

        Assert.False(File.Exists(stale));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_workspacePath))
                Directory.Delete(_workspacePath, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for temp test workspace.
        }
    }

    private GraphRagService CreateSut(
        bool enabled,
        string? backendCommand = null,
        IProcessRunner? processRunner = null,
        string? workspacePath = null,
        string? rootPath = null)
    {
        var effectiveWorkspacePath = workspacePath ?? _workspacePath;
        var options = Microsoft.Extensions.Options.Options.Create(new GraphRagOptions
        {
            Enabled = enabled,
            RootPath = rootPath ?? "mcp-data/graphrag",
            BackendCommand = backendCommand,
            BackendArgs = "{operation} --graphRoot {graphRoot} --workspace {workspacePath}",
            ArtifactVersion = "v1"
        });
        var ingestion = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = effectiveWorkspacePath });
        var workspaceContext = new WorkspaceContext { WorkspacePath = effectiveWorkspacePath };
        var contextSearch = Substitute.For<IContextSearchService>();
        contextSearch
            .SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ContextSearchResult(
                [new ScoredChunk
                {
                    ChunkId = "c1",
                    DocumentId = "d1",
                    Content = "Auth Service handles tokens.",
                    TokenCount = 6,
                    ChunkIndex = 0,
                    Score = 0.1
                }],
                ["repo:src/AuthService.cs"]));
        var runner = processRunner ?? Substitute.For<IProcessRunner>();
        if (processRunner is null)
        {
            runner
                .RunAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ProcessRunResult(0, "{\"answer\":\"ok\",\"sourceKeys\":[\"repo:src/AuthService.cs\"]}", null));
        }
        var adapters = new IGraphRagBackendAdapter[]
        {
            new InternalFallbackGraphRagBackendAdapter(),
            new ExternalCommandGraphRagBackendAdapter(runner, NullLogger<ExternalCommandGraphRagBackendAdapter>.Instance)
        };

        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"GraphRagServiceTests_{Guid.NewGuid():N}")
            .Options;
        var db = new McpDbContext(dbOptions);
        db.Database.EnsureCreated();
        db.OverrideWorkspaceId(effectiveWorkspacePath);

        var embeddingService = Substitute.For<IEmbeddingService>();
        embeddingService.Dimensions.Returns(384);
        embeddingService.IsAvailable.Returns(true);
        embeddingService.GenerateEmbedding(Arg.Any<string>()).Returns(new float[384]);

        var vectorIndexService = Substitute.For<IVectorIndexService>();

        return new GraphRagService(
            options,
            ingestion,
            workspaceContext,
            contextSearch,
            adapters,
            NullLogger<GraphRagService>.Instance,
            db,
            embeddingService,
            vectorIndexService);
    }
}
