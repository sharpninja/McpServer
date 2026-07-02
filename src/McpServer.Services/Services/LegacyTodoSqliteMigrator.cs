using System.Text.Json;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TODO-007 (provider-agnostic): one-shot idempotent migrator that copies
/// TODO rows from the legacy <c>mcp.db</c> SQLite store into the configured
/// authoritative database selected by <c>Mcp:Database:Provider</c>.
/// </summary>
/// <remarks>
/// <para>
/// Runs on host start. The migrator is a no-op when any of the following hold:
/// <list type="bullet">
///   <item><description><c>Mcp:TodoStorage:MigrateFromLegacySqlite</c> is false.</description></item>
///   <item><description>The legacy SQLite file does not exist.</description></item>
///   <item><description>A sentinel marker file is adjacent to the legacy database.</description></item>
///   <item><description>The target database already contains TODO rows (idempotency).</description></item>
/// </list>
/// On successful import a marker file is written next to the legacy database to
/// short-circuit subsequent boots, ensuring the migration is genuinely one-shot.
/// </para>
/// </remarks>
internal sealed class LegacyTodoSqliteMigrator : IHostedService
{
    internal const string MarkerFileSuffix = ".migrated";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<TodoStorageOptions> _storageOptions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LegacyTodoSqliteMigrator> _logger;

    /// <summary>Initializes a new instance of the <see cref="LegacyTodoSqliteMigrator"/> class.</summary>
    public LegacyTodoSqliteMigrator(
        IServiceScopeFactory scopeFactory,
        IOptions<TodoStorageOptions> storageOptions,
        IConfiguration configuration,
        ILogger<LegacyTodoSqliteMigrator> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RunAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LegacyTodoSqliteMigrator failed; continuing startup without migration.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Exposes the core migration logic for unit testing; identical semantics to
    /// <see cref="StartAsync"/> but without the try/catch wrapper so test assertions
    /// observe the original exception.
    /// </summary>
    internal async Task<MigrationOutcome> RunAsync(CancellationToken cancellationToken)
    {
        var opts = _storageOptions.Value;
        if (!opts.MigrateFromLegacySqlite)
        {
            _logger.LogInformation("LegacyTodoSqliteMigrator: disabled via Mcp:TodoStorage:MigrateFromLegacySqlite=false; no-op.");
            return MigrationOutcome.SkippedByFlag;
        }

        var legacyPath = ResolveLegacyPath(opts);
        if (string.IsNullOrWhiteSpace(legacyPath) || !File.Exists(legacyPath))
        {
            _logger.LogInformation("LegacyTodoSqliteMigrator: no legacy SQLite file at '{Path}'; no-op.", legacyPath);
            return MigrationOutcome.SkippedMissingFile;
        }

        var markerPath = legacyPath + MarkerFileSuffix;
        if (File.Exists(markerPath))
        {
            _logger.LogInformation("LegacyTodoSqliteMigrator: marker '{Marker}' present; no-op.", markerPath);
            return MigrationOutcome.SkippedMarkerPresent;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();

        if (await ctx.TodoItems.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            await WriteMarkerAsync(markerPath, "target non-empty", cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("LegacyTodoSqliteMigrator: target already has TODO rows; wrote marker and exited.");
            return MigrationOutcome.SkippedTargetPopulated;
        }

        var (items, history, metadata) = await ReadLegacyAsync(legacyPath, cancellationToken).ConfigureAwait(false);

        foreach (var item in items)
        {
            ctx.TodoItems.Add(item);
            ctx.TodoItemListItems.AddRange(item.ListItems);
            ctx.TodoItemTasks.AddRange(item.ImplementationTaskRows);
        }

        foreach (var row in history)
            ctx.TodoAuditHistory.Add(row);
        if (metadata is not null)
            ctx.TodoDocumentMetadata.Add(metadata);

        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await WriteMarkerAsync(markerPath, $"imported items={items.Count} history={history.Count} meta={(metadata is null ? 0 : 1)}", cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "LegacyTodoSqliteMigrator: imported {Items} TODO items, {History} audit rows, {Meta} metadata row(s) from '{Path}'.",
            items.Count, history.Count, metadata is null ? 0 : 1, legacyPath);
        return MigrationOutcome.Migrated;
    }

    private string? ResolveLegacyPath(TodoStorageOptions opts)
    {
        var configured = opts.SqliteDataSource;
        if (string.IsNullOrWhiteSpace(configured))
            return null;

        if (Path.IsPathRooted(configured))
            return configured;

        var dataFolder = _configuration["DataFolder"] ?? _configuration["Mcp:DataFolder"];
        var root = string.IsNullOrWhiteSpace(dataFolder) ? AppContext.BaseDirectory : dataFolder;
        return Path.GetFullPath(Path.Combine(root, configured));
    }

    private static async Task WriteMarkerAsync(string path, string reason, CancellationToken cancellationToken)
    {
        var payload = $"migrated_at_utc={DateTimeOffset.UtcNow:O}\nreason={reason}\n";
        await File.WriteAllTextAsync(path, payload, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<(List<TodoItemEntity> Items, List<TodoAuditHistoryEntity> History, TodoDocumentMetadataEntity? Metadata)> ReadLegacyAsync(
        string legacyPath,
        CancellationToken cancellationToken)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = legacyPath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var items = await ReadItemsAsync(connection, cancellationToken).ConfigureAwait(false);
        var history = await ReadHistoryAsync(connection, cancellationToken).ConfigureAwait(false);
        var metadata = await ReadMetadataAsync(connection, cancellationToken).ConfigureAwait(false);

        return (items, history, metadata);
    }

    private static async Task<List<TodoItemEntity>> ReadItemsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var result = new List<TodoItemEntity>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT id, title, section, priority, done, estimate, note,
                   description_json, technical_details_json, implementation_tasks_json,
                   completed_date, done_summary, remaining, priority_note, reference,
                   depends_on_json, functional_requirements_json, technical_requirements_json,
                   COALESCE(item_kind,'standard') AS item_kind,
                   COALESCE(section_order,0) AS section_order,
                   COALESCE(item_order,0) AS item_order,
                   phase_label
            FROM todo_items;";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = reader.GetString(0);
            var entity = new TodoItemEntity
            {
                Id = id,
                Title = reader.GetString(1),
                Section = reader.GetString(2),
                Priority = reader.GetString(3),
                Done = reader.GetInt64(4) != 0,
                Estimate = reader.IsDBNull(5) ? null : reader.GetString(5),
                Note = reader.IsDBNull(6) ? null : reader.GetString(6),
                CompletedDate = reader.IsDBNull(10) ? null : reader.GetString(10),
                DoneSummary = reader.IsDBNull(11) ? null : reader.GetString(11),
                Remaining = reader.IsDBNull(12) ? null : reader.GetString(12),
                PriorityNote = reader.IsDBNull(13) ? null : reader.GetString(13),
                Reference = reader.IsDBNull(14) ? null : reader.GetString(14),
                ItemKind = reader.GetString(18),
                SectionOrder = (int)reader.GetInt64(19),
                ItemOrder = (int)reader.GetInt64(20),
                PhaseLabel = reader.IsDBNull(21) ? null : reader.GetString(21),
            };

            // TR-MCP-TODO-005: legacy JSON list columns land as 4NF child rows.
            AddListRows(entity, id, "Description", reader.IsDBNull(7) ? null : reader.GetString(7));
            AddListRows(entity, id, "TechnicalDetail", reader.IsDBNull(8) ? null : reader.GetString(8));
            AddListRows(entity, id, "DependsOn", reader.IsDBNull(15) ? null : reader.GetString(15));
            AddListRows(entity, id, "FunctionalRequirement", reader.IsDBNull(16) ? null : reader.GetString(16));
            AddListRows(entity, id, "TechnicalRequirement", reader.IsDBNull(17) ? null : reader.GetString(17));
            AddTaskRows(entity, id, reader.IsDBNull(9) ? null : reader.GetString(9));

            result.Add(entity);
        }
        return result;
    }

    private static void AddListRows(TodoItemEntity entity, string todoId, string listType, string? json)
    {
        var values = DeserializeStringList(json);
        if (values is null)
            return;
        for (var i = 0; i < values.Count; i++)
        {
            entity.ListItems.Add(new TodoItemListItemEntity
            {
                TodoId = todoId,
                ListType = listType,
                Ordinal = i,
                Value = values[i],
            });
        }
    }

    private static void AddTaskRows(TodoItemEntity entity, string todoId, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;
        List<LegacyTask>? tasks;
        try
        {
            tasks = JsonSerializer.Deserialize<List<LegacyTask>>(json, s_legacyJson);
        }
        catch (JsonException)
        {
            return;
        }

        if (tasks is null)
            return;
        for (var i = 0; i < tasks.Count; i++)
        {
            entity.ImplementationTaskRows.Add(new TodoItemTaskEntity
            {
                TodoId = todoId,
                Ordinal = i,
                Task = tasks[i].Task ?? string.Empty,
                Done = tasks[i].Done,
            });
        }
    }

    private static List<string>? DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions s_legacyJson = new(JsonSerializerDefaults.Web);

    private sealed record LegacyTask
    {
        public string? Task { get; init; }

        public bool Done { get; init; }
    }

    private static async Task<List<TodoAuditHistoryEntity>> ReadHistoryAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var result = new List<TodoAuditHistoryEntity>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT audit_id, todo_id, version, action, recorded_at_utc,
                   snapshot_json, previous_snapshot_json, source
            FROM todo_item_history
            ORDER BY todo_id, version;";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result.Add(new TodoAuditHistoryEntity
            {
                // AuditId intentionally omitted: downstream EF provider assigns its own identity value.
                TodoId = reader.GetString(1),
                Version = (int)reader.GetInt64(2),
                Action = reader.GetString(3),
                RecordedAtUtc = reader.GetString(4),
                SnapshotJson = reader.IsDBNull(5) ? null : reader.GetString(5),
                PreviousSnapshotJson = reader.IsDBNull(6) ? null : reader.GetString(6),
                Source = reader.IsDBNull(7) ? null : reader.GetString(7),
            });
        }
        return result;
    }

    private static async Task<TodoDocumentMetadataEntity?> ReadMetadataAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT notes_json, completed_json, code_review_reference,
                   last_imported_from_yaml_utc, last_projected_to_yaml_utc,
                   last_projection_failure_utc, last_projection_failure_message
            FROM todo_document_metadata WHERE singleton_id = 1;";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return null;

        return new TodoDocumentMetadataEntity
        {
            SingletonId = 1,
            NotesJson = reader.IsDBNull(0) ? null : reader.GetString(0),
            CompletedJson = reader.IsDBNull(1) ? null : reader.GetString(1),
            CodeReviewReference = reader.IsDBNull(2) ? null : reader.GetString(2),
            LastImportedFromYamlUtc = reader.IsDBNull(3) ? null : reader.GetString(3),
            LastProjectedToYamlUtc = reader.IsDBNull(4) ? null : reader.GetString(4),
            LastProjectionFailureUtc = reader.IsDBNull(5) ? null : reader.GetString(5),
            LastProjectionFailureMessage = reader.IsDBNull(6) ? null : reader.GetString(6),
        };
    }

    /// <summary>Deterministic outcome enum for unit-test assertions.</summary>
    internal enum MigrationOutcome
    {
        SkippedByFlag,
        SkippedMissingFile,
        SkippedMarkerPresent,
        SkippedTargetPopulated,
        Migrated,
    }
}
