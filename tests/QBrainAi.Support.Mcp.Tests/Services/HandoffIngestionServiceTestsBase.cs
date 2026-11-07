using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MsOptions = Microsoft.Extensions.Options.Options;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-HANDOFF-002; TEST-HANDOFF-007: the per-test state and helpers that both halves of HandoffIngestionServiceTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-HANDOFF-002; TEST-HANDOFF-007; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from HandoffIngestionServiceTests (_workspace, _connection, _dbOptions, _time, .ctor, Dispose, CreateService, DurabilityDbFactory, SeedCanonicalRequirements, CreateDb, SuccessfulExtractor, ValidDraftJson and ManualTimeProvider) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class HandoffIngestionServiceTestsBase : IDisposable
{
    internal readonly string _workspace;
    internal readonly SqliteConnection _connection;
    internal readonly DbContextOptions<McpDbContext> _dbOptions;
    internal readonly ManualTimeProvider _time = new(new DateTimeOffset(2026, 8, 16, 18, 0, 0, TimeSpan.Zero));

    /// <summary>Creates an isolated workspace and SQLite database for handoff tests.</summary>
    protected HandoffIngestionServiceTestsBase()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "handoff-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var db = CreateDb(_workspace);
        db.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_workspace))
            Directory.Delete(_workspace, recursive: true);
    }

    internal IHandoffIngestionService CreateService(IHandoffOneShotExtractor extractor, ITodoService todo, string? workspacePath = null)
    {
        var workspace = workspacePath ?? _workspace;
        var db = CreateDb(workspace);
        var ingestionOptions = MsOptions.Create(new IngestionOptions { RepoRoot = workspace });
        var resolver = new TodoServiceResolver(todo, ingestionOptions, Substitute.For<ITodoServiceFactory>());
        var accessor = new WorkspaceServiceAccessor(resolver, Substitute.For<IHttpContextAccessor>(), ingestionOptions);
        SeedCanonicalRequirements(db, workspace);
        return new HandoffIngestionService(
            new HandoffSourceResolver(db),
            extractor,
            new HandoffTodoDraftParser(),
            new HandoffTodoDraftValidator(),
            new HandoffModePolicy(),
            accessor,
            db,
            new SessionLogSanitizer(MsOptions.Create(new SessionLogSanitizationOptions { RegexTimeoutMilliseconds = 5000 })),
            _time,
            new DurabilityDbFactory(_dbOptions, workspace));
    }

    internal sealed class DurabilityDbFactory : IDbContextFactory<McpDbContext>
    {
        private readonly DbContextOptions<McpDbContext> _options;
        private readonly string _workspace;
        public DurabilityDbFactory(DbContextOptions<McpDbContext> options, string workspace)
        {
            _options = options;
            _workspace = workspace;
        }

        public McpDbContext CreateDbContext()
            => new(_options, new WorkspaceContext { WorkspacePath = _workspace });
    }

    internal static void SeedCanonicalRequirements(McpDbContext db, string workspace)
    {
        if (db.Requirements.Any())
            return;

        db.Requirements.AddRange(
            new RequirementEntity
            {
                WorkspaceId = workspace,
                Kind = "fr",
                Id = "FR-HANDOFF-001",
                Title = "Handoff FR",
                Body = "Seeded",
                Priority = "high",
                Status = "pending",
            },
            new RequirementEntity
            {
                WorkspaceId = workspace,
                Kind = "tr",
                Id = "TR-HANDOFF-CONTRACT-001",
                Title = "Handoff TR",
                Body = "Seeded",
                Priority = "high",
                Status = "pending",
            });
        db.SaveChanges();
    }

    internal McpDbContext CreateDb(string workspacePath)
        => new(_dbOptions, new WorkspaceContext { WorkspacePath = workspacePath });

    internal static IHandoffOneShotExtractor SuccessfulExtractor(string json)
    {
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffExtractionResult
            {
                Success = true,
                ResponseText = json,
                AgentName = "plan-agent",
                PromptVersion = HandoffPromptDefaults.PromptVersion,
                TemplateVersion = HandoffPromptDefaults.TemplateId,
            });
        return extractor;
    }

    internal static string ValidDraftJson(string id, double confidence = 0.8, string? unknownNotes = null)
        => $$"""
            {"id":"{{id}}","title":"Demo","section":"QBrain.AI","priority":"high","estimate":"2h","description":["Do the work"],"technicalDetails":["Use the service"],"implementationTasks":[{"task":"Write tests","done":false}],"dependsOn":[],"functionalRequirements":["FR-HANDOFF-001"],"technicalRequirements":["TR-HANDOFF-CONTRACT-001"],"confidence":{{confidence.ToString(System.Globalization.CultureInfo.InvariantCulture)}},"unknownSourceNotes":[{{(unknownNotes is null ? "" : $"\"{unknownNotes}\"")}}]}
            """;

    internal sealed class ManualTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;
        public ManualTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
