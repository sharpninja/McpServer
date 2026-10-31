using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MsOptions = Microsoft.Extensions.Options.Options;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>TEST-HANDOFF-006: one canonical workspace path is pushed into every handoff scope.</summary>
public sealed class HandoffWorkspacePathsTests
{
    /// <summary>P1-8: relative and nested paths canonicalize to the same absolute value.</summary>
    /// <remarks>Integration: binds physical current-directory resolution (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Canonicalize_RelativeAndNested_MatchGetFullPath()
    {
        var relative = Path.Combine(".", "nested", "..", "workspace");
        var canonical = HandoffWorkspacePaths.Canonicalize(relative);
        Assert.Equal(ExpectedWorkspaceKey.Of(Path.GetFullPath(relative)), canonical);
        Assert.True(Path.IsPathRooted(canonical));
    }

    /// <summary>P1-8: blank paths are rejected.</summary>
    [Fact]
    public void TryCanonicalize_Blank_Fails()
    {
        Assert.False(HandoffWorkspacePaths.TryCanonicalize("  ", out _, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    /// <summary>P1-8: explicit virtual workspace identifiers isolate content-only handoff runs in an in-memory database.</summary>
    [Fact]
    public async Task IngestAsync_CrossWorkspace_DoesNotLeakRuns()
    {
        const string leftRoot = "Q:/fixture/handoff-left";
        const string rightRoot = "Q:/fixture/handoff-right";
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options;
        using (var bootstrap = new McpDbContext(options, new WorkspaceContext { WorkspacePath = leftRoot }))
            bootstrap.Database.EnsureCreated();

        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffExtractionResult
            {
                Success = true,
                ResponseText = """{"id":"MCP-HANDOFFDEMO-301","title":"Demo","section":"QBrain.AI","priority":"high","estimate":"2h","description":["Do the work"],"technicalDetails":["Use the service"],"implementationTasks":[{"task":"Write tests","done":false}],"dependsOn":[],"functionalRequirements":["FR-HANDOFF-001"],"technicalRequirements":["TR-HANDOFF-CONTRACT-001"],"confidence":0.8,"unknownSourceNotes":[]}""",
                AgentName = "plan-agent",
                PromptVersion = HandoffPromptDefaults.PromptVersion,
                TemplateVersion = HandoffPromptDefaults.TemplateId,
                Model = "test-model",
            });

        var left = CreateService(options, leftRoot, extractor);
        var created = await left.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "left-only",
            Mode = HandoffIngestionMode.DraftOnly,
        }, TestContext.Current.CancellationToken);
        Assert.True(created.Success, created.Error);

        var right = CreateService(options, rightRoot, extractor);
        var missing = await right.GetRunAsync(created.Provenance!.RunId, TestContext.Current.CancellationToken);
        Assert.False(missing.Success);
        Assert.Equal(HandoffErrorCodes.RunNotFound, missing.ErrorCode);

    }

    private static IHandoffIngestionService CreateService(DbContextOptions<McpDbContext> options, string workspace, IHandoffOneShotExtractor extractor)
    {
        var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = workspace });
        if (!db.Requirements.Any())
        {
            db.Requirements.AddRange(
                new RequirementEntity { WorkspaceId = workspace, Kind = "fr", Id = "FR-HANDOFF-001", Title = "FR", Body = "b", Priority = "high", Status = "pending" },
                new RequirementEntity { WorkspaceId = workspace, Kind = "tr", Id = "TR-HANDOFF-CONTRACT-001", Title = "TR", Body = "b", Priority = "high", Status = "pending" });
            db.SaveChanges();
        }

        var ingestionOptions = MsOptions.Create(new IngestionOptions { RepoRoot = workspace });
        var accessor = new WorkspaceServiceAccessor(
            new TodoServiceResolver(Substitute.For<ITodoService>(), ingestionOptions, Substitute.For<ITodoServiceFactory>()),
            Substitute.For<IHttpContextAccessor>(),
            ingestionOptions);
        accessor.PushWorkspace(workspace);
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
}
