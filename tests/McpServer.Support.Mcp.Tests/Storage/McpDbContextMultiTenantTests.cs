using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>Tests for TR-MCP-MT-003: multi-tenant global query filters on <see cref="McpDbContext"/>.</summary>
public sealed class McpDbContextMultiTenantTests : IDisposable
{
    private readonly DbContextOptions<McpDbContext> _options;

    public McpDbContextMultiTenantTests()
    {
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"mt-test-{Guid.NewGuid()}")
            .Options;

        // Seed data for two workspaces
        using var ctx = CreateContext(string.Empty); // unscoped for seeding
        ctx.Documents.AddRange(
            new ContextDocumentEntity { Id = "doc-a", SourceKey = "a.md", SourceType = "file", ContentHash = "aaa", WorkspaceId = @"C:\ws\alpha" },
            new ContextDocumentEntity { Id = "doc-b", SourceKey = "b.md", SourceType = "file", ContentHash = "bbb", WorkspaceId = @"C:\ws\beta" });
        ctx.SessionLogs.AddRange(
            new SessionLogEntity { Id = 1, SessionId = "s1", SourceType = "cursor", WorkspaceId = @"C:\ws\alpha" },
            new SessionLogEntity { Id = 2, SessionId = "s2", SourceType = "copilot", WorkspaceId = @"C:\ws\beta" });
        ctx.ToolBuckets.AddRange(
            new ToolBucketEntity { Id = 3, Name = "bucket-global", Owner = "org", Repo = "repo-global", WorkspaceId = string.Empty },
            new ToolBucketEntity { Id = 1, Name = "bucket-a", Owner = "org", Repo = "repo-a", WorkspaceId = @"C:\ws\alpha" },
            new ToolBucketEntity { Id = 2, Name = "bucket-b", Owner = "org", Repo = "repo-b", WorkspaceId = @"C:\ws\beta" });
        ctx.AgentDefinitions.AddRange(
            new AgentDefinitionEntity
            {
                Id = "agent-global",
                WorkspaceId = string.Empty,
                DisplayName = "Global Agent",
                DefaultLaunchCommand = "pwsh",
                DefaultInstructionFile = "AGENTS-README-FIRST.yaml",
                DefaultModelsJson = "[]",
                DefaultBranchStrategy = "feature/global",
                DefaultSeedPrompt = "",
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            },
            new AgentDefinitionEntity
            {
                Id = "agent-alpha",
                WorkspaceId = @"C:\ws\alpha",
                DisplayName = "Alpha Agent",
                DefaultLaunchCommand = "pwsh",
                DefaultInstructionFile = "AGENTS-README-FIRST.yaml",
                DefaultModelsJson = "[]",
                DefaultBranchStrategy = "feature/alpha",
                DefaultSeedPrompt = "",
                IsBuiltIn = false,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            },
            new AgentDefinitionEntity
            {
                Id = "agent-beta",
                WorkspaceId = @"C:\ws\beta",
                DisplayName = "Beta Agent",
                DefaultLaunchCommand = "pwsh",
                DefaultInstructionFile = "AGENTS-README-FIRST.yaml",
                DefaultModelsJson = "[]",
                DefaultBranchStrategy = "feature/beta",
                DefaultSeedPrompt = "",
                IsBuiltIn = false,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            });
        ctx.SaveChanges();
    }

    [Fact]
    public void QueryFilter_ReturnsOnlyCurrentWorkspace()
    {
        using var ctx = CreateContext(@"C:\ws\alpha");

        var docs = ctx.Documents.ToList();

        Assert.Single(docs);
        Assert.Equal("a.md", docs[0].SourceKey);
    }

    [Fact]
    public void QueryFilter_WorkspaceB_ReturnsOnlyB()
    {
        using var ctx = CreateContext(@"C:\ws\beta");

        var docs = ctx.Documents.ToList();

        Assert.Single(docs);
        Assert.Equal("b.md", docs[0].SourceKey);
    }

    [Fact]
    public void QueryFilter_AppliesToSessionLogs()
    {
        using var ctx = CreateContext(@"C:\ws\alpha");

        var logs = ctx.SessionLogs.ToList();

        Assert.Single(logs);
        Assert.Equal("s1", logs[0].SessionId);
    }

    [Fact]
    public void QueryFilter_AppliesToToolBuckets()
    {
        using var ctx = CreateContext(@"C:\ws\alpha");

        var buckets = ctx.ToolBuckets.ToList();

        Assert.Equal(2, buckets.Count);
        Assert.Contains(buckets, bucket => bucket.Name == "bucket-a");
        Assert.Contains(buckets, bucket => bucket.Name == "bucket-global");
    }

    [Fact]
    public void IgnoreQueryFilters_ReturnsAll()
    {
        using var ctx = CreateContext(@"C:\ws\alpha");

        var allDocs = ctx.Documents.IgnoreQueryFilters().ToList();

        Assert.Equal(2, allDocs.Count);
    }

    [Fact]
    public void EmptyWorkspaceId_HidesTenantScopedDocuments()
    {
        using var ctx = CreateContext(string.Empty);

        var docs = ctx.Documents.ToList();

        Assert.Empty(docs);
    }

    [Fact]
    public void EmptyWorkspaceId_ReturnsOnlyGlobalToolBuckets()
    {
        using var ctx = CreateContext(string.Empty);

        var buckets = ctx.ToolBuckets.ToList();

        Assert.Single(buckets);
        Assert.Equal("bucket-global", buckets[0].Name);
    }

    [Fact]
    public void EmptyWorkspaceId_ReturnsOnlyGlobalAgentDefinitions()
    {
        using var ctx = CreateContext(string.Empty);

        var agents = ctx.AgentDefinitions.OrderBy(a => a.Id).ToList();

        Assert.Single(agents);
        Assert.Equal("agent-global", agents[0].Id);
    }

    [Fact]
    public void WorkspaceFilter_IncludesGlobalAgentDefinitions()
    {
        using var ctx = CreateContext(@"C:\ws\alpha");

        var agents = ctx.AgentDefinitions.OrderBy(a => a.Id).ToList();

        Assert.Equal(2, agents.Count);
        Assert.Equal(["agent-alpha", "agent-global"], agents.Select(a => a.Id).ToArray());
    }

    [Fact]
    public void Insert_RespectsCurrentWorkspaceScope()
    {
        using var ctx = CreateContext(@"C:\ws\alpha");

        // Insert a new doc (WorkspaceId manually set, as auto-set is a future enhancement)
        ctx.Documents.Add(new ContextDocumentEntity { Id = "doc-c", SourceKey = "c.md", SourceType = "file", ContentHash = "ccc", WorkspaceId = @"C:\ws\alpha" });
        ctx.SaveChanges();

        // Query from alpha scope sees 2 docs
        var docs = ctx.Documents.ToList();
        Assert.Equal(2, docs.Count);

        // Query from beta scope still sees 1 doc
        using var ctxB = CreateContext(@"C:\ws\beta");
        var docsB = ctxB.Documents.ToList();
        Assert.Single(docsB);
    }

    private McpDbContext CreateContext(string workspacePath)
    {
        var wsCtx = new WorkspaceContext { WorkspacePath = string.IsNullOrEmpty(workspacePath) ? null : workspacePath };
        return new McpDbContext(_options, wsCtx);
    }

    public void Dispose()
    {
        // In-memory DB is cleaned up when all contexts are disposed
    }
}
