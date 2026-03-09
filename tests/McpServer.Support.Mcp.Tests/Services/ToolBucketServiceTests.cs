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
/// Tests workspace-aware tool bucket behavior so default buckets remain visible and duplicate names
/// fail cleanly instead of surfacing database exceptions.
/// </summary>
public sealed class ToolBucketServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>
    /// Creates an isolated relational database so tool-bucket uniqueness constraints and query filters
    /// behave the same way they do in production.
    /// </summary>
    public ToolBucketServiceTests()
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
    /// Verifies that a workspace-scoped add request returns a conflict when a global bucket with the same
    /// name already exists, preventing a hidden-row uniqueness violation from surfacing as a server error.
    /// Validates FR-MCP-022 and TR-MCP-MT-003.
    /// </summary>
    [Fact]
    public async Task AddBucketAsync_WhenGlobalBucketExistsOutsideWorkspaceScope_ReturnsConflict()
    {
        using (var seed = CreateContext(null))
        {
            seed.ToolBuckets.Add(CreateBucketEntity("official", string.Empty));
            seed.SaveChanges();
        }

        using var scopedDb = CreateContext(@"E:\github\McpServer");
        var sut = CreateSut(scopedDb);

        var result = await sut.AddBucketAsync(new BucketAddRequest("official", "sharpninja", "McpServerTools")).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("Bucket 'official' already exists.", result.Error);
    }

    /// <summary>
    /// Verifies that workspace-scoped bucket browsing can still access globally seeded buckets, which is
    /// required for default tool-bucket manifests to remain available across workspaces. Validates
    /// FR-MCP-022 and TR-MCP-MT-003.
    /// </summary>
    [Fact]
    public async Task BrowseAsync_WhenGlobalBucketExistsOutsideWorkspaceScope_ReturnsManifests()
    {
        using (var seed = CreateContext(null))
        {
            seed.ToolBuckets.Add(CreateBucketEntity("official", string.Empty));
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

        using var scopedDb = CreateContext(@"E:\github\McpServer");
        var sut = CreateSut(scopedDb, processRunner);

        var result = await sut.BrowseAsync("official").ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(result.Tools);
        Assert.Single(result.Tools!);
        Assert.Equal("mcp-session-module", result.Tools[0].Name);
    }

    /// <summary>
    /// Disposes the shared relational test database connection after each test class instance so temporary
    /// resources are released deterministically.
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

    private static ToolBucketService CreateSut(McpDbContext db, IProcessRunner? processRunner = null)
    {
        return new ToolBucketService(
            db,
            processRunner ?? Substitute.For<IProcessRunner>(),
            Substitute.For<IToolRegistryService>(),
            NullLogger<ToolBucketService>.Instance);
    }

    private static ToolBucketEntity CreateBucketEntity(string name, string workspaceId)
    {
        return new ToolBucketEntity
        {
            Name = name,
            Owner = "sharpninja",
            Repo = "McpServerTools",
            Branch = "main",
            ManifestPath = "/",
            WorkspaceId = workspaceId,
            DateTimeCreated = DateTimeOffset.UtcNow,
        };
    }
}
