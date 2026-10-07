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
    [Fact]
    public void Canonicalize_RelativeAndNested_MatchGetFullPath()
    {
        var relative = Path.Combine(".", "nested", "..", "workspace");
        var canonical = HandoffWorkspacePaths.Canonicalize(relative);
        Assert.Equal(Path.GetFullPath(relative), canonical);
        Assert.True(Path.IsPathRooted(canonical));
    }

    /// <summary>
    /// TEST-MCP-FED-PATH-001 / FR-MCP-FED-PATH-001-AC1/AC2: a foreign-platform absolute path
    /// (POSIX on a Windows runner, Windows on a Linux runner) is canonicalized lexically and never
    /// re-rooted on the host. Every MCP tool workspace override flows through this method.
    /// </summary>
    [Fact]
    public void Canonicalize_ForeignAbsolutePath_IsNotReRootedOnHost()
    {
        var foreign = OperatingSystem.IsWindows() ? "/home/sharpninja/github/McpServer" : @"C:\Users\kingd";
        var foreignPlatform = OperatingSystem.IsWindows()
            ? QBrainAi.Client.WorkspacePathPlatform.CaseSensitive
            : QBrainAi.Client.WorkspacePathPlatform.Windows;

        Assert.Equal(
            QBrainAi.Client.WorkspaceIdentityPath.NormalizeLexicalPath(foreign, foreignPlatform),
            HandoffWorkspacePaths.Canonicalize(foreign));
    }

    /// <summary>
    /// TEST-MCP-FED-PATH-001: with a simulated Linux hub, a Windows workspace stays unmangled and
    /// a relative path anchors at the hub working directory.
    /// </summary>
    [Fact]
    public void Canonicalize_SimulatedLinuxHost_UsesPathSyntax()
    {
        var host = Substitute.For<QBrainAi.Client.IWorkspaceHostEnvironment>();
        host.Platform.Returns(QBrainAi.Client.WorkspacePathPlatform.CaseSensitive);
        host.CurrentDirectory.Returns("/opt/mcpserver/app");
        host.NormalizeNativePath(Arg.Any<string>()).Returns(call =>
        {
            var path = call.Arg<string>()!;
            return QBrainAi.Client.WorkspaceIdentityPath.NormalizeLexicalPath(
                path.StartsWith('/') ? path : "/opt/mcpserver/app/" + path,
                QBrainAi.Client.WorkspacePathPlatform.CaseSensitive);
        });
        var linux = new QBrainAi.Client.WorkspacePathNormalizer(host);

        Assert.Equal(linux.Normalize(@"C:\Users\kingd"), HandoffWorkspacePaths.Canonicalize(@"C:\Users\kingd", linux));
        Assert.Equal("/opt/mcpserver/app/ws", HandoffWorkspacePaths.Canonicalize("ws", linux));
    }

    /// <summary>P1-8: blank paths are rejected.</summary>
    [Fact]
    public void TryCanonicalize_Blank_Fails()
    {
        Assert.False(HandoffWorkspacePaths.TryCanonicalize("  ", out _, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    /// <summary>P1-8: two workspaces cannot see each other's handoff runs.</summary>
    [Fact]
    public async Task IngestAsync_CrossWorkspace_DoesNotLeakRuns()
    {
        var leftRoot = Path.Combine(Path.GetTempPath(), "handoff-ws-left", Guid.NewGuid().ToString("N"));
        var rightRoot = Path.Combine(Path.GetTempPath(), "handoff-ws-right", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(leftRoot);
        Directory.CreateDirectory(rightRoot);
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

        Directory.Delete(leftRoot, recursive: true);
        Directory.Delete(rightRoot, recursive: true);
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
