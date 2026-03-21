using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Verifies cross-workspace tool visibility so bucket installs with no explicit
/// workspace scope remain truly global and searchable from every workspace.
/// Validates FR-MCP-022 and TR-MCP-MT-003.
/// </summary>
public sealed class ToolRegistryScopeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>
    /// Creates an isolated relational database so tool registry and bucket
    /// services execute against the same schema and scope rules used in
    /// production.
    /// </summary>
    public ToolRegistryScopeTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateContext(null);
        db.Database.EnsureCreated();
    }

    /// <summary>
    /// Verifies that installing a bucket tool without an explicit workspace
    /// parameter persists the tool and its tags as global rows, making the tool
    /// discoverable from a different workspace. This covers the live regression
    /// reported by the McpServerManager workspace.
    /// </summary>
    [Fact]
    public async Task InstallAsync_WithoutWorkspaceParameter_PersistsGlobalToolVisibleAcrossWorkspaces()
    {
        using (var seed = CreateContext(null))
        {
            seed.ToolBuckets.Add(new ToolBucketEntity
            {
                Name = "official",
                Owner = "sharpninja",
                Repo = "McpServerTools",
                Branch = "main",
                ManifestPath = "/",
                WorkspaceId = string.Empty,
                DateTimeCreated = DateTimeOffset.UtcNow,
            });
            seed.SaveChanges();
        }

        var processRunner = Substitute.For<IProcessRunner>();
        processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var arguments = callInfo.ArgAt<string>(1);
                if (arguments.Contains("/contents", StringComparison.Ordinal))
                {
                    return new ProcessRunResult(
                        0,
                        """[{"name":"mcp-session-module.json","download_url":"https://example.invalid/mcp-session-module.json"}]""",
                        null);
                }

                return new ProcessRunResult(
                    0,
                    """{"name":"mcp-session-module","description":"Download McpSession","tags":["mcp","session"],"parameterSchema":"{}","commandTemplate":"pwsh -NoLogo -NoProfile -NonInteractive"}""",
                    null);
            });

        using (var installDb = CreateContext(@"E:\github\McpServer"))
        {
            var registry = CreateRegistry(installDb);
            var bucketService = CreateBucketService(installDb, registry, processRunner);

            var installResult = await bucketService.InstallAsync("official", "mcp-session-module").ConfigureAwait(true);

            Assert.True(installResult.Success);
            Assert.NotNull(installResult.Tool);
        }

        using (var inspectDb = CreateContext(null))
        {
            var tool = inspectDb.ToolDefinitions
                .IgnoreQueryFilters()
                .Include(t => t.Tags)
                .Single(t => t.Name == "mcp-session-module");

            Assert.Equal(string.Empty, tool.WorkspaceId);
            Assert.Null(tool.WorkspacePath);
            Assert.All(tool.Tags, tag => Assert.Equal(string.Empty, tag.WorkspaceId));
        }

        using (var otherWorkspaceDb = CreateContext(@"E:\github\RequestTracker"))
        {
            var registry = CreateRegistry(otherWorkspaceDb);
            var searchResult = await registry.SearchAsync("mcp-session-module").ConfigureAwait(true);

            Assert.Single(searchResult.Tools);
            Assert.Equal("mcp-session-module", searchResult.Tools[0].Name);
        }
    }

    /// <summary>
    /// Verifies that an unscoped registry query only returns global tools, preventing empty-workspace
    /// visibility from surfacing workspace-specific tool definitions across tenants while keeping
    /// intentionally global tools discoverable.
    /// </summary>
    [Fact]
    public async Task ListAsync_WithoutWorkspaceParameter_ReturnsOnlyGlobalTools()
    {
        using (var seed = CreateContext(null))
        {
            seed.ToolDefinitions.Add(new ToolDefinitionEntity
            {
                Name = "global-tool",
                Description = "Global tool",
                WorkspaceId = string.Empty,
                WorkspacePath = null,
                DateTimeCreated = DateTimeOffset.UtcNow,
                DateTimeModified = DateTimeOffset.UtcNow,
            });
            seed.ToolDefinitions.Add(new ToolDefinitionEntity
            {
                Name = "workspace-tool",
                Description = "Workspace tool",
                WorkspaceId = @"E:\github\McpServer",
                WorkspacePath = @"E:\github\McpServer",
                DateTimeCreated = DateTimeOffset.UtcNow,
                DateTimeModified = DateTimeOffset.UtcNow,
            });
            seed.SaveChanges();
        }

        using var unscopedDb = CreateContext(null);
        var registry = CreateRegistry(unscopedDb);

        var result = await registry.ListAsync().ConfigureAwait(true);

        Assert.Single(result.Tools);
        Assert.Equal("global-tool", result.Tools[0].Name);
    }

    /// <summary>
    /// Releases the shared relational test database connection after the test
    /// class completes so temporary resources do not leak across runs.
    /// </summary>
    public void Dispose()
    {
        _connection.Dispose();
    }

    private McpDbContext CreateContext(string? workspacePath)
    {
        return new McpDbContext(
            _options,
            new WorkspaceContext
            {
                WorkspacePath = workspacePath,
            });
    }

    private static ToolRegistryService CreateRegistry(McpDbContext db)
    {
        return new ToolRegistryService(db, NullLogger<ToolRegistryService>.Instance);
    }

    private static ToolBucketService CreateBucketService(
        McpDbContext db,
        IToolRegistryService registry,
        IProcessRunner processRunner)
    {
        return new ToolBucketService(
            db,
            processRunner,
            registry,
            NullLogger<ToolBucketService>.Instance);
    }
}
