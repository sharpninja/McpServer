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
/// <remarks>2026-11-07, TEST-HANDOFF-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (Canonicalize_RelativeAndNested_MatchGetFullPath) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</remarks>
public sealed class HandoffWorkspacePathsTests
{
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
