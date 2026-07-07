using System.Text.Json;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>TR-PLANNED-CORE-013: Tests for SessionLogFileWatcher detecting file changes (MVP-SUPPORT-011).</summary>
public sealed class SessionLogFileWatcherTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ServiceProvider _serviceProvider;
    private readonly string _dbName;

    public SessionLogFileWatcherTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fwh-watcher-{Guid.NewGuid():N}");
        _dbName = $"WatcherTests_{Guid.NewGuid():N}";
        Directory.CreateDirectory(Path.Combine(_tempDir, "docs", "sessions"));

        var services = new ServiceCollection();
        var dbName = _dbName;
        services.AddDbContext<McpDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            SessionsPath = "docs/sessions"
        }));
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<McpServer.Support.Mcp.Indexing.Chunker>();
        services.AddScoped<ISessionLogService, SessionLogService>();
        services.AddScoped(_ => new WorkspaceContext
        {
            WorkspacePath = _tempDir,
            SessionsPath = "docs/sessions"
        });
        services.AddScoped<SessionLogIngestor>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { /* best-effort */ }
    }

    [Fact]
    public async Task WhenFileCreatedThenWatcherTriggersImport()
    {
        // Ensure DB is initialized by creating a scope first
        using (var initScope = _serviceProvider.CreateScope())
        {
            var initDb = initScope.ServiceProvider.GetRequiredService<McpDbContext>();
            await initDb.Database.EnsureCreatedAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        var opts = _serviceProvider.GetRequiredService<IOptions<IngestionOptions>>();
        var watcher = new SessionLogFileWatcher(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            opts,
            NullLogger<SessionLogFileWatcher>.Instance);

        await watcher.StartAsync(CancellationToken.None).ConfigureAwait(true);

        // Write a session log file to the watched directory
        var dto = new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = "watcher-1",
            Title = "Watched Session",
            TurnCount = 0
        };
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        var filePath = Path.Combine(_tempDir, "docs", "sessions", "watcher-test.json");
        File.WriteAllText(filePath, json);

        // Wait for debounce (2s) + async processing; use retry loop for robustness
        SessionLogEntity? stored = null;
        for (var attempt = 0; attempt < 15 && stored is null; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            using var checkScope = _serviceProvider.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<McpDbContext>();
            stored = await checkDb.SessionLogs.FirstOrDefaultAsync(s => s.SessionId == "watcher-1", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        await watcher.StopAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.NotNull(stored);
        Assert.Equal("Watched Session", stored!.Title);
    }
}
