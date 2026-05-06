using System.IO.Compression;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Acceptance tests for DB-backed, workspace-scoped requirements storage.</summary>
public sealed class RequirementsDatabaseDocumentServiceTests
{
    /// <summary>Overlapping requirement ids do not leak between workspaces.</summary>
    [Fact]
    public async Task ListsAndExports_AreScopedToActiveWorkspace()
    {
        using var fixture = new RequirementsDbFixture();
        var workspaceA = fixture.CreateWorkspace("a");
        var workspaceB = fixture.CreateWorkspace("b");

        var service = fixture.CreateService();
        fixture.SetWorkspace(workspaceA);
        await service.AddFrAsync(new FrEntry("FR-MCP-900", "Workspace A", "A body"));
        await service.AddTrAsync(new TrEntry("TR-MCP-900", "A TR", "A TR body"));
        await service.AddTestAsync(new TestEntry("TEST-MCP-900", "A test"));
        Assert.Contains(await fixture.GetRequirementRowsAsync(), x => x.Id == "FR-MCP-900");
        await service.UpsertMappingAsync(new FrTrMapping("FR-MCP-900", ["TR-MCP-900"], ["TEST-MCP-900"]));

        fixture.SetWorkspace(workspaceB);
        await service.AddFrAsync(new FrEntry("FR-MCP-900", "Workspace B", "B body"));
        await service.AddTrAsync(new TrEntry("TR-MCP-900", "B TR", "B TR body"));
        await service.AddTestAsync(new TestEntry("TEST-MCP-900", "B test"));
        await service.UpsertMappingAsync(new FrTrMapping("FR-MCP-900", ["TR-MCP-900"], ["TEST-MCP-900"]));

        fixture.SetWorkspace(workspaceA);
        var fr = Assert.Single(await service.GetAllFrAsync());
        Assert.Equal("Workspace A", fr.Title);

        var (mappingMarkdown, _) = await service.GenerateDocumentAsync(RequirementsDocType.Mapping);
        Assert.Contains("TEST-MCP-900", mappingMarkdown);
        Assert.DoesNotContain("Workspace B", mappingMarkdown);

        await using var zipStream = await service.GenerateAllAsync();
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var functional = await ReadZipEntryAsync(zip, "Functional-Requirements.md");
        Assert.Contains("Workspace A", functional);
        Assert.DoesNotContain("Workspace B", functional);
    }

    /// <summary>Mapping validation rejects missing FR/TR/TEST ids before storing links.</summary>
    [Fact]
    public async Task UpsertMapping_ValidatesFrTrAndTestIds()
    {
        using var fixture = new RequirementsDbFixture();
        fixture.SetWorkspace(fixture.CreateWorkspace("validate"));
        var service = fixture.CreateService();

        await service.AddFrAsync(new FrEntry("FR-MCP-901", "FR", "FR body"));
        await service.AddTrAsync(new TrEntry("TR-MCP-901", "TR", "TR body"));
        await service.AddTestAsync(new TestEntry("TEST-MCP-901", "Test body"));
        Assert.Contains(await fixture.GetRequirementRowsAsync(), x => x.Id == "FR-MCP-901");

        await service.UpsertMappingAsync(new FrTrMapping("FR-MCP-901", ["TR-MCP-901"], ["TEST-MCP-901"]));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpsertMappingAsync(new FrTrMapping("FR-MCP-901", ["TR-MCP-MISSING"], [])));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpsertMappingAsync(new FrTrMapping("FR-MCP-901", [], ["TEST-MCP-MISSING"])));
    }

    private static async Task<string> ReadZipEntryAsync(ZipArchive zip, string name)
    {
        var entry = zip.GetEntry(name) ?? throw new InvalidOperationException($"Missing zip entry {name}.");
        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private sealed class RequirementsDbFixture : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _requestScope;
        private readonly DefaultHttpContext _httpContext;
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly string _root = Path.Combine(Path.GetTempPath(), "mcp-reqdb-tests-" + Guid.NewGuid().ToString("N"));

        public RequirementsDbFixture()
        {
            var services = new ServiceCollection();
            _connection.Open();
            services.AddDbContext<McpDbContext>(options => options.UseSqlite(_connection));
            services.AddScoped<WorkspaceContext>();
            services.AddHttpContextAccessor();
            services.AddSingleton<IOptions<RequirementsOptions>>(Microsoft.Extensions.Options.Options.Create(new RequirementsOptions()));
            services.AddSingleton(NullLogger<RequirementsDatabaseDocumentService>.Instance);
            _provider = services.BuildServiceProvider();
            using (var schemaScope = _provider.CreateScope())
            {
                schemaScope.ServiceProvider.GetRequiredService<McpDbContext>().Database.EnsureCreated();
            }
            _requestScope = _provider.CreateScope();
            _httpContext = new DefaultHttpContext { RequestServices = _requestScope.ServiceProvider };
            _provider.GetRequiredService<IHttpContextAccessor>().HttpContext = _httpContext;
        }

        public string CreateWorkspace(string name)
        {
            var path = Path.Combine(_root, name);
            Directory.CreateDirectory(Path.Combine(path, "docs", "Project"));
            return path;
        }

        public RequirementsDatabaseDocumentService CreateService() =>
            new(
                _provider.GetRequiredService<IServiceScopeFactory>(),
                _provider.GetRequiredService<IOptions<RequirementsOptions>>(),
                NullLogger<RequirementsDatabaseDocumentService>.Instance,
                _provider.GetRequiredService<IHttpContextAccessor>());

        public async Task<IReadOnlyList<RequirementEntity>> GetRequirementRowsAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<McpDbContext>()
                .Requirements
                .IgnoreQueryFilters()
                .OrderBy(x => x.WorkspaceId)
                .ThenBy(x => x.Kind)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public void SetWorkspace(string workspacePath)
        {
            _provider.GetRequiredService<IHttpContextAccessor>().HttpContext = _httpContext;
            var ctx = _httpContext.RequestServices.GetRequiredService<WorkspaceContext>();
            ctx.WorkspacePath = workspacePath;
            ctx.WorkspaceName = Path.GetFileName(workspacePath);
        }

        public void Dispose()
        {
            _requestScope.Dispose();
            _provider.Dispose();
            _connection.Dispose();
            try { Directory.Delete(_root, recursive: true); } catch { }
        }
    }
}
