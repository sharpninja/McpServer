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
            new ToolBucketEntity { Id = 1, Name = "bucket-a", Owner = "org", Repo = "repo-a", WorkspaceId = @"C:\ws\alpha" },
            new ToolBucketEntity { Id = 2, Name = "bucket-b", Owner = "org", Repo = "repo-b", WorkspaceId = @"C:\ws\beta" });
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

        Assert.Single(buckets);
        Assert.Equal("bucket-a", buckets[0].Name);
    }

    [Fact]
    public void IgnoreQueryFilters_ReturnsAll()
    {
        using var ctx = CreateContext(@"C:\ws\alpha");

        var allDocs = ctx.Documents.IgnoreQueryFilters().ToList();

        Assert.Equal(2, allDocs.Count);
    }

    [Fact]
    public void EmptyWorkspaceId_ReturnsAll()
    {
        // When workspace context is empty, all rows are visible (backward compat)
        using var ctx = CreateContext(string.Empty);

        var allDocs = ctx.Documents.ToList();

        Assert.Equal(2, allDocs.Count);
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
