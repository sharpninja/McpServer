using QBrainAi.Support.Mcp.Indexing;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>Tests for core GraphRagService lifecycle operations (status, init, index, query).</summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-05</term><description>TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): CreateSut takes an optional clock handed to the service; the stale-artifact case and the D2 binding use a settable clock (hostile validation round 12 finding R12-F2); the other cases keep the default clock and are unchanged.</description></item>
/// </list>
/// </remarks>
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

        var status = await sut.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
        await sut.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var status = await sut.IndexAsync(new GraphRagIndexRequest { Force = true }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(response.FallbackUsed);
        Assert.Equal("graphrag_disabled", response.FallbackReason);
        Assert.Equal("context-search", response.QueryCorpus);
    }

    [Fact]
    public async Task Status_InternalFallback_ReportsCorpusAndInputDiagnostics()
    {
        var sut = CreateSut(enabled: true);
        var initialized = await sut.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var localDocPath = Path.Combine(initialized.GraphRoot, "input", "docs", "prg", "Commodore_64_Programmers_Reference_Guide.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(localDocPath)!);
        await File.WriteAllTextAsync(localDocPath, "Video Bank Selection", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var status = await sut.GetStatusAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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

        var status = await sut.IndexAsync(new GraphRagIndexRequest { Force = false }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(status.IsIndexed);
        Assert.Equal("degraded", status.State);
        Assert.Equal("index_failed", status.FailureCode);
        Assert.NotNull(status.LastError);
    }

    [Fact]
    public async Task InitializeAsync_GlobalScope_UsesDataFolderRoot()
    {
        var dataFolder = Path.Combine(_workspacePath, "mcp-data");
        Directory.CreateDirectory(dataFolder);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataFolder"] = dataFolder })
            .Build();

        var sut = CreateSut(enabled: true, configuration: configuration, globalRootPath: "graphrag-global");
        var status = await sut.InitializeAsync(GraphRagStorageScope.Global, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(GraphRagStorageScope.Global, status.Scope);
        Assert.Equal("(global)", status.WorkspacePath);
        Assert.StartsWith(Path.Combine(dataFolder, "graphrag-global"), status.GraphRoot, StringComparison.OrdinalIgnoreCase);
        Assert.True(Directory.Exists(Path.Combine(status.GraphRoot, "input")));
    }

    [Fact]
    public async Task QueryAsync_GlobalScope_SearchesGlobalInputCorpus()
    {
        var dataFolder = Path.Combine(_workspacePath, "mcp-data");
        Directory.CreateDirectory(dataFolder);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataFolder"] = dataFolder })
            .Build();

        var sut = CreateSut(enabled: true, configuration: configuration, globalRootPath: "graphrag-global");
        var initialized = await sut.InitializeAsync(GraphRagStorageScope.Global, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var docPath = Path.Combine(initialized.GraphRoot, "input", "canonical", "docs", "todo-schema.md");
        Directory.CreateDirectory(Path.GetDirectoryName(docPath)!);
        await File.WriteAllTextAsync(
            docPath,
            "Use workflow.todo.update with done: true and doneSummary.",
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sut.IndexAsync(
            new GraphRagIndexRequest { Scope = GraphRagStorageScope.Global, Force = true },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = await sut.QueryAsync(
            new GraphRagQueryRequest
            {
                Scope = GraphRagStorageScope.Global,
                Query = "workflow.todo.update doneSummary",
                IncludeContextChunks = true,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(GraphRagStorageScope.Global, response.Scope);
        Assert.Contains("workflow.todo.update", response.Answer, StringComparison.Ordinal);
        Assert.Contains(response.SourceKeys, key => key.StartsWith("global:", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("graphrag-global-input", response.QueryCorpus);
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

        var statusA = await sutA.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var statusB = await sutB.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotEqual(statusA.GraphRoot, statusB.GraphRoot);
        Assert.StartsWith(sharedRoot, statusA.GraphRoot, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(sharedRoot, statusB.GraphRoot, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initialization removes a temporary artifact older than two hours. The service reads a settable test clock and the
    /// file's last write is set from the same instant (TR-MCP-USECASE-018 AC-006), so the case does not depend on the
    /// system clock.
    /// </summary>
    /// <returns>A task that completes when the case finishes.</returns>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): the reference time and the file time come from a <see cref="QBrainAi.Support.Mcp.Tests.TestSupport.SettableTimeProvider"/> instead of DateTime.UtcNow; the assertion is unchanged (hostile validation round 12 finding R12-F2).</remarks>
    [Fact]
    public async Task InitializeAsync_RemovesStaleTemporaryArtifacts()
    {
        var now = QBrainAi.Support.Mcp.Tests.TestSupport.AgeBoundaryContractTests.References[0];
        var sut = CreateSut(enabled: true, clock: new QBrainAi.Support.Mcp.Tests.TestSupport.SettableTimeProvider(now));
        var status = await sut.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stale = Path.Combine(status.GraphRoot, "cache", "old.tmp");
        await File.WriteAllTextAsync(stale, "stale", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        File.SetLastWriteTimeUtc(stale, now.UtcDateTime.AddHours(-3));

        _ = await sut.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(File.Exists(stale));
    }

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 binding, decision D2 of
    /// <see cref="QBrainAi.Support.Mcp.Tests.TestSupport.AgeBoundaryContractTests"/>: for each scripted reference time and
    /// each contract row, a temporary file in the cache folder and a partial file in the output folder, both last written
    /// at the row's time, are deleted by initialization exactly when the contract says (kept at exactly two hours,
    /// because only an age above two hours is deleted). Test data: the contract's times; files named per row.
    /// </summary>
    /// <returns>A task that completes when every row has been asserted.</returns>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): added for hostile validation round 12 finding R12-F2 (the reviewer's proposed ArtifactCleanup_UsesScriptedAgeBoundary).</remarks>
    [Fact]
    public Task ArtifactCleanup_UsesScriptedAgeBoundary() =>
        QBrainAi.Support.Mcp.Tests.TestSupport.AgeBoundaryContractTests.AssertDecisionAsync("D2 temporary artifact cleanup", async (reference, recorded) =>
        {
            var sut = CreateSut(enabled: true, clock: new QBrainAi.Support.Mcp.Tests.TestSupport.SettableTimeProvider(reference));
            var status = await sut.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var row = Guid.NewGuid().ToString("N");
            var temporary = Path.Combine(status.GraphRoot, "cache", row + ".tmp");
            var partial = Path.Combine(status.GraphRoot, "output", row + ".partial");
            foreach (var file in new[] { temporary, partial })
            {
                await File.WriteAllTextAsync(file, "artifact", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
                File.SetLastWriteTimeUtc(file, recorded.UtcDateTime);
                Assert.Equal(recorded.UtcDateTime, File.GetLastWriteTimeUtc(file));
            }

            _ = await sut.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(File.Exists(temporary), File.Exists(partial));
            return !File.Exists(temporary);
        });

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

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006: a GraphRagService over the test workspace, an in-memory database, a substitute context
    /// search and embedding service, the internal fallback and external command back ends, and the clock passed in.
    /// </summary>
    /// <param name="enabled">Whether GraphRAG is enabled.</param>
    /// <param name="backendCommand">The external back-end command, or null.</param>
    /// <param name="processRunner">The process runner, or null for a substitute that answers "ok".</param>
    /// <param name="workspacePath">The workspace path, or null for the test workspace.</param>
    /// <param name="rootPath">The graph root path, or null for mcp-data/graphrag.</param>
    /// <param name="globalRootPath">The global graph root path, or null for graphrag-global.</param>
    /// <param name="configuration">The configuration, or null for one whose DataFolder is the test workspace.</param>
    /// <param name="clock">The clock the service reads, or null for its default clock.</param>
    /// <returns>The service under test.</returns>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the helper, changed in slice Z4 to take an optional clock handed to the service (hostile validation round 12 finding R12-F2), because the tenth holistic read before hostile validation round 13 (finding d1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private GraphRagService CreateSut(
        bool enabled,
        string? backendCommand = null,
        IProcessRunner? processRunner = null,
        string? workspacePath = null,
        string? rootPath = null,
        string? globalRootPath = null,
        IConfiguration? configuration = null,
        TimeProvider? clock = null)
    {
        var effectiveWorkspacePath = workspacePath ?? _workspacePath;
        var options = Microsoft.Extensions.Options.Options.Create(new GraphRagOptions
        {
            Enabled = enabled,
            RootPath = rootPath ?? "mcp-data/graphrag",
            GlobalRootPath = globalRootPath ?? "graphrag-global",
            BackendCommand = backendCommand,
            BackendArgs = "{operation} --graphRoot {graphRoot} --workspace {workspacePath}",
            ArtifactVersion = "v1"
        });
        var effectiveConfiguration = configuration ?? new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataFolder"] = _workspacePath })
            .Build();
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
            effectiveConfiguration,
            workspaceContext,
            contextSearch,
            adapters,
            NullLogger<GraphRagService>.Instance,
            db,
            embeddingService,
            vectorIndexService,
            timeProvider: clock);
    }
}
