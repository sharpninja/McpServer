using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Requirements.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-USECASE-018 AC-006: the helpers that both halves of RequirementsDatabaseDocumentServiceTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from RequirementsDatabaseDocumentServiceTests (RequirementsDbFixture) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class RequirementsDatabaseDocumentServiceTestsSupport
{
    internal sealed class RequirementsDbFixture : IDisposable
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

        public async Task<IReadOnlyList<RequirementTraceabilityLinkEntity>> GetTraceabilityRowsAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<McpDbContext>()
                .RequirementTraceabilityLinks
                .IgnoreQueryFilters()
                .OrderBy(x => x.WorkspaceId)
                .ThenBy(x => x.FrId)
                .ThenBy(x => x.TargetKind)
                .ThenBy(x => x.TargetId)
                .ToListAsync();
        }

        public async Task SeedBadPlaceholderAsync(string workspacePath, string badId, string body)
        {
            await using var scope = _provider.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            if (await ctx.Requirements.IgnoreQueryFilters().AnyAsync(x => x.WorkspaceId == workspacePath && x.Kind == "fr" && x.Id == badId)) return;
            var now = DateTimeOffset.UtcNow.ToString("O");
            ctx.Requirements.Add(new RequirementEntity
            {
                WorkspaceId = workspacePath,
                Kind = "fr",
                Id = badId,
                Title = badId,
                Body = body,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            await ctx.SaveChangesAsync();
        }

        public async Task SeedTraceabilityLinkAsync(string workspacePath, string frId, string targetKind, string targetId)
        {
            await using var scope = _provider.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            await EnsureRequirementAsync(ctx, workspacePath, "fr", frId);
            await EnsureRequirementAsync(ctx, workspacePath, targetKind, targetId);
            ctx.RequirementTraceabilityLinks.Add(new RequirementTraceabilityLinkEntity
            {
                WorkspaceId = workspacePath,
                FrId = frId,
                TargetKind = targetKind,
                TargetId = targetId,
                CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            });
            await ctx.SaveChangesAsync();
        }

        private static async Task EnsureRequirementAsync(McpDbContext ctx, string workspacePath, string kind, string id)
        {
            if (await ctx.Requirements.IgnoreQueryFilters()
                    .AnyAsync(x => x.WorkspaceId == workspacePath && x.Kind == kind && x.Id == id))
            {
                return;
            }

            var now = DateTimeOffset.UtcNow.ToString("O");
            ctx.Requirements.Add(new RequirementEntity
            {
                WorkspaceId = workspacePath,
                Kind = kind,
                Id = id,
                Title = id,
                Body = "Seeded requirement for traceability FK coverage.",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }

        public void SetWorkspace(string workspacePath)
        {
            _provider.GetRequiredService<IHttpContextAccessor>().HttpContext = _httpContext;
            var ctx = _httpContext.RequestServices.GetRequiredService<WorkspaceContext>();
            ctx.WorkspacePath = workspacePath;
            ctx.WorkspaceName = Path.GetFileName(workspacePath);
        }

        /// <summary>Creates a DI scope for direct DbContext seeding in tests.</summary>
        public IServiceScope CreateRawScope() => _provider.CreateScope();

        public void Dispose()
        {
            _requestScope.Dispose();
            _provider.Dispose();
            _connection.Dispose();
            try { Directory.Delete(_root, recursive: true); } catch { }
        }
    }
}
