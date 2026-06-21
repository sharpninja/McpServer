using System.Collections.Concurrent;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-CORE-013: Hosted service that monitors docs/sessions/ for new and updated JSON and Markdown files.
/// When a file change is detected, the session log is re-imported into the 4NF tables.
/// Uses a debounce window to coalesce rapid successive writes into a single import.
/// </summary>
public sealed class SessionLogFileWatcher : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionLogFileWatcher> _logger;
    private readonly IngestionOptions _options;
    private FileSystemWatcher? _watcher;
    private FileSystemWatcher? _mdWatcher;
    private Timer? _debounceTimer;
    private readonly ConcurrentDictionary<string, byte> _pendingFiles = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan s_debounceInterval = TimeSpan.FromSeconds(2);

    /// <summary>TR-PLANNED-CORE-013: Constructor.</summary>
    public SessionLogFileWatcher(
        IServiceScopeFactory scopeFactory,
        IOptions<IngestionOptions> options,
        ILogger<SessionLogFileWatcher> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new IngestionOptions();
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var repoRoot = Path.GetFullPath(_options.RepoRoot);
        var sessionsDir = Path.IsPathRooted(_options.SessionsPath)
            ? Path.GetFullPath(_options.SessionsPath)
            : Path.GetFullPath(Path.Combine(repoRoot, _options.SessionsPath.TrimStart('.', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

        if (!Directory.Exists(sessionsDir))
        {
            _logger.LogWarning("Sessions directory not found for file watcher: {SessionsDir}. Watcher not started.", sessionsDir);
            return Task.CompletedTask;
        }

        _watcher = new FileSystemWatcher(sessionsDir, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileChanged;
        _watcher.Changed += OnFileChanged;
        _watcher.Renamed += OnFileRenamed;

        _mdWatcher = new FileSystemWatcher(sessionsDir, "*.md")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _mdWatcher.Created += OnFileChanged;
        _mdWatcher.Changed += OnFileChanged;
        _mdWatcher.Renamed += OnFileRenamed;

        _logger.LogInformation("Session log file watcher started on {SessionsDir}", sessionsDir);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher?.Dispose();
        _watcher = null;
        _mdWatcher?.Dispose();
        _mdWatcher = null;
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        _logger.LogInformation("Session log file watcher stopped");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _watcher?.Dispose();
        _mdWatcher?.Dispose();
        _debounceTimer?.Dispose();
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _pendingFiles.TryAdd(e.FullPath, 0);
        ScheduleDebounce();
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (e.FullPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
            e.FullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            _pendingFiles.TryAdd(e.FullPath, 0);
            ScheduleDebounce();
        }
    }

    private void ScheduleDebounce()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(OnDebounceElapsed, null, s_debounceInterval, Timeout.InfiniteTimeSpan);
    }

    private async void OnDebounceElapsed(object? state)
    {
        // Drain pending files
        var files = new List<string>();
        foreach (var key in _pendingFiles.Keys)
        {
            if (_pendingFiles.TryRemove(key, out _))
                files.Add(key);
        }

        if (files.Count == 0)
            return;

        _logger.LogInformation("File watcher detected {Count} changed session log file(s), triggering import", files.Count);

        try
        {
            var scope = _scopeFactory.CreateAsyncScope();
            try
            {
                var ingestor = scope.ServiceProvider.GetRequiredService<SessionLogIngestor>();
                var result = await ingestor.ImportToSessionLogTablesAsync().ConfigureAwait(false);
                _logger.LogInformation(
                    "File watcher import complete: {FilesScanned} files scanned, {Imported} imported ({TotalTurns} turns), {Skipped} unchanged, {Failed} failed",
                    result.FilesScanned, result.Imported, result.TotalTurns, result.Skipped, result.Failed);
            }
            finally
            {
                await scope.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File watcher import failed");
        }
    }
}
