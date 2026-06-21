using System.Text.Json;
using McpServer.Cqrs.Search;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using YamlDotNet.Core;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TODO-005: SQLite-backed authoritative TODO storage with deterministic TODO.yaml projection.
/// </summary>
internal sealed class SqliteTodoService : ITodoService, ITodoStore, IDisposable
{
    private const string DefaultTodoRelativePath = "docs/Project/TODO.yaml";
    private const string StandardItemKind = "standard";
    private const string CodeReviewPhaseItemKind = "code_review_phase";
    private const string CodeReviewSectionKey = "code-review-remediation";
    private const string YamlBootstrapSource = "yaml-bootstrap";
    private const string SqliteBackfillSource = "sqlite-backfill";
    private const string TodoDeleteSource = "api";
    private const string TodoDeleteReason = "todo_delete";

    private readonly string _dataSource;
    private readonly string _todoFilePath;
    private readonly IWriteAuditLog _auditLog;
    private readonly IChangeEventBus? _eventBus;
    private readonly ILogger<SqliteTodoService> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly object _initializationSync = new();
    private Task? _initializationTask;

    public SqliteTodoService(
        IOptions<IngestionOptions> ingestionOptions,
        IOptions<TodoStorageOptions> storageOptions,
        IWriteAuditLog auditLog,
        ILogger<SqliteTodoService> logger,
        IChangeEventBus? eventBus = null)
    {
        ArgumentNullException.ThrowIfNull(ingestionOptions);
        ArgumentNullException.ThrowIfNull(storageOptions);
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus;

        var repoRoot = ingestionOptions.Value.RepoRoot ?? ".";
        var source = string.IsNullOrWhiteSpace(storageOptions.Value.SqliteDataSource) ? "mcp.db" : storageOptions.Value.SqliteDataSource;
        _dataSource = Path.GetFullPath(Path.IsPathRooted(source) ? source : Path.Combine(repoRoot, source));

        var todoPath = string.IsNullOrWhiteSpace(ingestionOptions.Value.TodoFilePath) ? DefaultTodoRelativePath : ingestionOptions.Value.TodoFilePath;
        _todoFilePath = Path.GetFullPath(Path.IsPathRooted(todoPath) ? todoPath : Path.Combine(repoRoot, todoPath));
    }

    internal SqliteTodoService(string dataSource, IWriteAuditLog auditLog, ILogger<SqliteTodoService> logger, IChangeEventBus? eventBus = null)
        : this(
            dataSource,
            Path.Combine(Path.GetDirectoryName(Path.GetFullPath(dataSource)) ?? ".", DefaultTodoRelativePath),
            auditLog,
            logger,
            eventBus)
    {
    }

    internal SqliteTodoService(string dataSource, string todoFilePath, IWriteAuditLog auditLog, ILogger<SqliteTodoService> logger, IChangeEventBus? eventBus = null)
    {
        _dataSource = Path.GetFullPath(dataSource ?? throw new ArgumentNullException(nameof(dataSource)));
        _todoFilePath = Path.GetFullPath(todoFilePath ?? throw new ArgumentNullException(nameof(todoFilePath)));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus;
    }

    public void Dispose() => _writeLock.Dispose();

    public async Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        var all = (await GetAllStoredAsync(cancellationToken).ConfigureAwait(false)).Select(static item => item.ToFlatItem()).ToList();
        var filtered = ApplyFilters(all, request);
        return new TodoQueryResult(filtered, filtered.Count);
    }

    public async Task<TodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        var item = await GetStoredByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return item?.ToFlatItem();
    }

    public async Task<TodoAuditQueryResult> GetAuditAsync(string id, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var effectiveLimit = Math.Clamp(limit <= 0 ? 50 : limit, 1, 500);
        var effectiveOffset = Math.Max(offset, 0);

        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM todo_item_history WHERE todo_id = $id;";
        countCommand.Parameters.AddWithValue("$id", id);
        var totalCount = Convert.ToInt32((long)(await countCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? 0L));

        if (totalCount == 0)
        {
            var current = await GetStoredByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (current is null)
                return new TodoAuditQueryResult([], 0);

            return new TodoAuditQueryResult(
            [
                new TodoAuditEntry
                {
                    AuditId = 0,
                    TodoId = current.Id,
                    Version = 1,
                    Action = "imported",
                    RecordedAtUtc = DateTime.UtcNow.ToString("O"),
                    Snapshot = current.ToFlatItem(),
                    Source = SqliteBackfillSource,
                }
            ],
            1);
        }

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT audit_id, todo_id, version, action, recorded_at_utc, snapshot_json, previous_snapshot_json, source
            FROM todo_item_history
            WHERE todo_id = $id
            ORDER BY version ASC
            LIMIT $limit OFFSET $offset;
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$limit", effectiveLimit);
        command.Parameters.AddWithValue("$offset", effectiveOffset);

        var entries = new List<TodoAuditEntry>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            entries.Add(new TodoAuditEntry
            {
                AuditId = reader.GetInt64(reader.GetOrdinal("audit_id")),
                TodoId = reader.GetString(reader.GetOrdinal("todo_id")),
                Version = reader.GetInt32(reader.GetOrdinal("version")),
                Action = reader.GetString(reader.GetOrdinal("action")),
                RecordedAtUtc = reader.GetString(reader.GetOrdinal("recorded_at_utc")),
                Snapshot = DeserializeFlatItem(GetNullableString(reader, "snapshot_json")),
                PreviousSnapshot = DeserializeFlatItem(GetNullableString(reader, "previous_snapshot_json")),
                Source = GetNullableString(reader, "source"),
            });
        }

        return new TodoAuditQueryResult(entries, totalCount);
    }

    /// <inheritdoc />
    public async Task<TodoProjectionStatusResult> GetProjectionStatusAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await GetProjectionStatusCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            try
            {
                await ProjectDatabaseToYamlAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Repaired TODO.yaml projection from authoritative SQLite storage at {TodoFilePath}.", _todoFilePath);
                var status = await GetProjectionStatusCoreAsync(cancellationToken).ConfigureAwait(false);
                return new TodoProjectionRepairResult(true, null, status);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or YamlException or InvalidOperationException or SqliteException)
            {
                _logger.LogError(ex, "Operator-requested TODO projection repair failed for {TodoFilePath}.", _todoFilePath);
                await TryRecordProjectionFailureAsync(ex).ConfigureAwait(false);
                var status = await GetProjectionStatusCoreAsync(cancellationToken).ConfigureAwait(false);
                return new TodoProjectionRepairResult(false, $"Failed to repair TODO projection at '{_todoFilePath}': {ex.Message}", status);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var idError = TodoValidator.ValidateTodoId(request.Id);
        if (idError is not null)
            return new TodoMutationResult(false, idError, FailureKind: TodoMutationFailureKind.Validation);

        var normalizedSection = NormalizeSection(request.Section);
        var normalizedPriority = NormalizePriority(normalizedSection, request.Priority);
        var priorityError = TodoValidator.ValidatePriority(normalizedPriority);
        if (priorityError is not null)
            return new TodoMutationResult(false, priorityError, FailureKind: TodoMutationFailureKind.Validation);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = await GetStoredByIdAsync(request.Id, cancellationToken, includeDeleted: true).ConfigureAwait(false);
            if (existing is not null)
                return new TodoMutationResult(false, $"Item with id '{request.Id}' already exists.", FailureKind: TodoMutationFailureKind.Conflict);

            var all = (await GetAllStoredAsync(cancellationToken).ConfigureAwait(false)).Select(static item => item.ToFlatItem()).ToList();
            var depIdError = TodoValidator.ValidateDependencyIds(request.DependsOn, all, "dependsOn");
            if (depIdError is not null)
                return new TodoMutationResult(false, depIdError, FailureKind: TodoMutationFailureKind.Validation);
            var depError = TodoValidator.ValidateDependencies(request.Id, request.DependsOn?.ToList() ?? [], all);
            if (depError is not null)
                return new TodoMutationResult(false, depError, FailureKind: TodoMutationFailureKind.Validation);

            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var transaction = connection.BeginTransaction();

            var itemKind = DetermineItemKind(normalizedSection);
            var sectionOrder = await ResolveSectionOrderAsync(connection, normalizedSection, cancellationToken).ConfigureAwait(false);
            var itemOrder = await GetNextItemOrderAsync(connection, normalizedSection, normalizedPriority, itemKind, cancellationToken).ConfigureAwait(false);
            var candidate = new StoredTodoItem
            {
                Id = request.Id,
                Title = request.Title,
                Section = normalizedSection,
                Priority = normalizedPriority,
                Done = false,
                Estimate = request.Estimate,
                Note = request.Note,
                Description = request.Description,
                TechnicalDetails = request.TechnicalDetails,
                ImplementationTasks = request.ImplementationTasks,
                Remaining = request.Remaining,
                DependsOn = request.DependsOn,
                FunctionalRequirements = request.FunctionalRequirements,
                TechnicalRequirements = request.TechnicalRequirements,
                ItemKind = itemKind,
                SectionOrder = sectionOrder,
                ItemOrder = itemOrder,
                Phase = itemKind == CodeReviewPhaseItemKind ? request.Phase ?? request.Title : null,
            };

            await InsertCurrentItemAsync(connection, candidate, cancellationToken).ConfigureAwait(false);
            await InsertHistoryEntryAsync(connection, candidate.Id, "created", candidate.ToFlatItem(), null, "api", cancellationToken).ConfigureAwait(false);
            transaction.Commit();

            return await FinalizeMutationAsync(ChangeEventActions.Created, candidate.Id, candidate.ToFlatItem(), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(request);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = await GetStoredByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                return new TodoMutationResult(false, $"Item with id '{id}' not found.", FailureKind: TodoMutationFailureKind.NotFound);

            var updatedSection = NormalizeSection(request.Section ?? existing.Section);
            var updatedPriority = NormalizePriority(updatedSection, request.Priority ?? existing.Priority);
            var priorityError = TodoValidator.ValidatePriority(updatedPriority);
            if (priorityError is not null)
                return new TodoMutationResult(false, priorityError, FailureKind: TodoMutationFailureKind.Validation);

            var updatedKind = DetermineItemKind(updatedSection);
            var updated = existing with
            {
                Title = request.Title ?? existing.Title,
                Section = updatedSection,
                Priority = updatedPriority,
                Done = request.Done ?? existing.Done,
                Estimate = request.Estimate ?? existing.Estimate,
                Note = request.Note ?? existing.Note,
                Description = request.Description ?? existing.Description,
                TechnicalDetails = request.TechnicalDetails ?? existing.TechnicalDetails,
                ImplementationTasks = request.ImplementationTasks ?? existing.ImplementationTasks,
                CompletedDate = request.CompletedDate ?? existing.CompletedDate,
                DoneSummary = request.DoneSummary ?? existing.DoneSummary,
                Remaining = request.Remaining ?? existing.Remaining,
                Reference = request.Reference ?? existing.Reference,
                DependsOn = request.DependsOn ?? existing.DependsOn,
                FunctionalRequirements = request.FunctionalRequirements ?? existing.FunctionalRequirements,
                TechnicalRequirements = request.TechnicalRequirements ?? existing.TechnicalRequirements,
                ItemKind = updatedKind,
                Phase = updatedKind == CodeReviewPhaseItemKind ? request.Phase ?? existing.Phase ?? existing.Title : null,
            };

            var all = (await GetAllStoredAsync(cancellationToken).ConfigureAwait(false)).Select(static item => item.ToFlatItem()).ToList();
            var depIdError = TodoValidator.ValidateDependencyIds(updated.DependsOn, all, "dependsOn");
            if (depIdError is not null)
                return new TodoMutationResult(false, depIdError, FailureKind: TodoMutationFailureKind.Validation);
            var depError = TodoValidator.ValidateDependencies(id, updated.DependsOn?.ToList() ?? [], all);
            if (depError is not null)
                return new TodoMutationResult(false, depError, FailureKind: TodoMutationFailureKind.Validation);

            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var transaction = connection.BeginTransaction();

            updated = updated with
            {
                SectionOrder = await ResolveSectionOrderAsync(connection, updated.Section, cancellationToken).ConfigureAwait(false),
                ItemOrder = RequiresNewItemOrder(existing, updated)
                    ? await GetNextItemOrderAsync(connection, updated.Section, updated.Priority, updated.ItemKind, cancellationToken).ConfigureAwait(false)
                    : existing.ItemOrder,
            };

            await UpdateCurrentItemAsync(connection, updated, cancellationToken).ConfigureAwait(false);
            if (updated.ItemKind == CodeReviewPhaseItemKind && request.Reference is not null)
                await UpdateCodeReviewReferenceAsync(connection, request.Reference, cancellationToken).ConfigureAwait(false);
            await InsertHistoryEntryAsync(connection, updated.Id, "updated", updated.ToFlatItem(), existing.ToFlatItem(), "api", cancellationToken).ConfigureAwait(false);
            transaction.Commit();

            return await FinalizeMutationAsync(ChangeEventActions.Updated, updated.Id, updated.ToFlatItem(), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = await GetStoredByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                return new TodoMutationResult(false, $"Item with id '{id}' not found.", FailureKind: TodoMutationFailureKind.NotFound);

            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var transaction = connection.BeginTransaction();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    UPDATE todo_items
                    SET is_deleted = 1,
                        deleted_at_utc = $deletedAtUtc,
                        deleted_by = $deletedBy,
                        delete_reason = $deleteReason
                    WHERE id = $id
                      AND is_deleted = 0;
                    """;
                command.Parameters.AddWithValue("$id", id);
                command.Parameters.AddWithValue("$deletedAtUtc", DateTime.UtcNow.ToString("O"));
                command.Parameters.AddWithValue("$deletedBy", TodoDeleteSource);
                command.Parameters.AddWithValue("$deleteReason", TodoDeleteReason);
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await InsertHistoryEntryAsync(connection, id, "deleted", existing.ToFlatItem(), existing.ToFlatItem(), TodoDeleteSource, cancellationToken).ConfigureAwait(false);
            transaction.Commit();

            return await FinalizeMutationAsync(ChangeEventActions.Deleted, id, null, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        Task initializationTask;
        lock (_initializationSync)
        {
            _initializationTask ??= InitializeAsync();
            initializationTask = _initializationTask;
        }

        return initializationTask.WaitAsync(cancellationToken);
    }

    private async Task InitializeAsync()
    {
        var directory = Path.GetDirectoryName(_dataSource);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        using var connection = CreateConnection();
        await connection.OpenAsync().ConfigureAwait(false);

        await EnsureSchemaAsync(connection).ConfigureAwait(false);
        await EnsureMetadataRowAsync(connection).ConfigureAwait(false);

        var currentCount = await GetCurrentItemCountAsync(connection).ConfigureAwait(false);
        var shouldProject = currentCount > 0;
        if (currentCount == 0)
        {
            var file = await TodoYamlFileSerializer.ReadIfExistsAsync(_todoFilePath, CancellationToken.None).ConfigureAwait(false);
            if (file is not null)
            {
                using var transaction = connection.BeginTransaction();
                await ImportFromYamlAsync(connection, file, DateTime.UtcNow, CancellationToken.None).ConfigureAwait(false);
                transaction.Commit();
                shouldProject = true;
            }
        }
        else
        {
            await ImportMetadataIfMissingAsync(connection, CancellationToken.None).ConfigureAwait(false);
            using var transaction = connection.BeginTransaction();
            await BackfillHistoryAsync(connection, CancellationToken.None).ConfigureAwait(false);
            await NormalizeOrderingAsync(connection, CancellationToken.None).ConfigureAwait(false);
            transaction.Commit();
            shouldProject = true;
        }

        if (shouldProject)
            await ProjectDatabaseToYamlAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task EnsureSchemaAsync(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS todo_items (
                id TEXT NOT NULL PRIMARY KEY COLLATE NOCASE,
                title TEXT NOT NULL,
                section TEXT NOT NULL,
                priority TEXT NOT NULL,
                done INTEGER NOT NULL,
                estimate TEXT NULL,
                note TEXT NULL,
                description_json TEXT NULL,
                technical_details_json TEXT NULL,
                implementation_tasks_json TEXT NULL,
                completed_date TEXT NULL,
                done_summary TEXT NULL,
                remaining TEXT NULL,
                priority_note TEXT NULL,
                reference TEXT NULL,
                depends_on_json TEXT NULL,
                functional_requirements_json TEXT NULL,
                technical_requirements_json TEXT NULL,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                deleted_at_utc TEXT NULL,
                deleted_by TEXT NULL,
                delete_reason TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_todo_items_section ON todo_items(section);
            CREATE INDEX IF NOT EXISTS idx_todo_items_priority ON todo_items(priority);
            CREATE INDEX IF NOT EXISTS idx_todo_items_done ON todo_items(done);
            CREATE TABLE IF NOT EXISTS todo_item_history (
                audit_id INTEGER PRIMARY KEY AUTOINCREMENT,
                todo_id TEXT NOT NULL COLLATE NOCASE,
                version INTEGER NOT NULL,
                action TEXT NOT NULL,
                recorded_at_utc TEXT NOT NULL,
                snapshot_json TEXT NULL,
                previous_snapshot_json TEXT NULL,
                source TEXT NULL,
                UNIQUE(todo_id, version)
            );
            CREATE INDEX IF NOT EXISTS idx_todo_item_history_todo_id_recorded_at_utc ON todo_item_history(todo_id, recorded_at_utc);
            CREATE INDEX IF NOT EXISTS idx_todo_item_history_action ON todo_item_history(action);
            CREATE TABLE IF NOT EXISTS todo_document_metadata (
                singleton_id INTEGER PRIMARY KEY CHECK (singleton_id = 1),
                notes_json TEXT NULL,
                completed_json TEXT NULL,
                code_review_reference TEXT NULL,
                last_imported_from_yaml_utc TEXT NULL,
                last_projected_to_yaml_utc TEXT NULL,
                last_projection_failure_utc TEXT NULL,
                last_projection_failure_message TEXT NULL
            );
            """;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);

        var columns = await GetTableColumnsAsync(connection, "todo_items").ConfigureAwait(false);
        if (!columns.Contains("item_kind"))
            await ExecuteNonQueryAsync(connection, "ALTER TABLE todo_items ADD COLUMN item_kind TEXT NOT NULL DEFAULT 'standard';").ConfigureAwait(false);
        if (!columns.Contains("section_order"))
            await ExecuteNonQueryAsync(connection, "ALTER TABLE todo_items ADD COLUMN section_order INTEGER NOT NULL DEFAULT 0;").ConfigureAwait(false);
        if (!columns.Contains("item_order"))
            await ExecuteNonQueryAsync(connection, "ALTER TABLE todo_items ADD COLUMN item_order INTEGER NOT NULL DEFAULT 0;").ConfigureAwait(false);
        if (!columns.Contains("phase_label"))
            await ExecuteNonQueryAsync(connection, "ALTER TABLE todo_items ADD COLUMN phase_label TEXT NULL;").ConfigureAwait(false);
        if (!columns.Contains("is_deleted"))
            await ExecuteNonQueryAsync(connection, "ALTER TABLE todo_items ADD COLUMN is_deleted INTEGER NOT NULL DEFAULT 0;").ConfigureAwait(false);
        if (!columns.Contains("deleted_at_utc"))
            await ExecuteNonQueryAsync(connection, "ALTER TABLE todo_items ADD COLUMN deleted_at_utc TEXT NULL;").ConfigureAwait(false);
        if (!columns.Contains("deleted_by"))
            await ExecuteNonQueryAsync(connection, "ALTER TABLE todo_items ADD COLUMN deleted_by TEXT NULL;").ConfigureAwait(false);
        if (!columns.Contains("delete_reason"))
            await ExecuteNonQueryAsync(connection, "ALTER TABLE todo_items ADD COLUMN delete_reason TEXT NULL;").ConfigureAwait(false);
        await ExecuteNonQueryAsync(connection, "CREATE INDEX IF NOT EXISTS idx_todo_items_is_deleted ON todo_items(is_deleted);").ConfigureAwait(false);

        var metadataColumns = await GetTableColumnsAsync(connection, "todo_document_metadata").ConfigureAwait(false);
        if (!metadataColumns.Contains("last_projection_failure_utc"))
            await ExecuteNonQueryAsync(connection, "ALTER TABLE todo_document_metadata ADD COLUMN last_projection_failure_utc TEXT NULL;").ConfigureAwait(false);
        if (!metadataColumns.Contains("last_projection_failure_message"))
            await ExecuteNonQueryAsync(connection, "ALTER TABLE todo_document_metadata ADD COLUMN last_projection_failure_message TEXT NULL;").ConfigureAwait(false);
    }

    private async Task EnsureMetadataRowAsync(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO todo_document_metadata(singleton_id) SELECT 1 WHERE NOT EXISTS (SELECT 1 FROM todo_document_metadata WHERE singleton_id = 1);";
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private async Task ImportFromYamlAsync(SqliteConnection connection, TodoFile file, DateTime importedAtUtc, CancellationToken cancellationToken)
    {
        var documentMetadata = new TodoDocumentMetadata(
            file.Notes is null ? null : JsonSerializer.Serialize(file.Notes, _json),
            file.Completed is null ? null : JsonSerializer.Serialize(file.Completed, _json),
            file.CodeReviewRemediation?.Reference,
            importedAtUtc.ToString("O"),
            null,
            null,
            null);

        await UpdateDocumentMetadataAsync(connection, documentMetadata, cancellationToken).ConfigureAwait(false);

        var sectionOrder = 0;
        foreach (var (sectionKey, section) in file.Sections)
        {
            await ImportPriorityListAsync(connection, sectionKey, section.HighPriority, "high", sectionOrder, importedAtUtc, cancellationToken).ConfigureAwait(false);
            await ImportPriorityListAsync(connection, sectionKey, section.MediumPriority, "medium", sectionOrder, importedAtUtc, cancellationToken).ConfigureAwait(false);
            await ImportPriorityListAsync(connection, sectionKey, section.LowPriority, "low", sectionOrder, importedAtUtc, cancellationToken).ConfigureAwait(false);
            sectionOrder++;
        }

        if (file.CodeReviewRemediation?.Phases is { Count: > 0 } phases)
        {
            for (var index = 0; index < phases.Count; index++)
            {
                var phase = phases[index];
                if (phase?.Id is null)
                    continue;

                var storedItem = new StoredTodoItem
                {
                    Id = phase.Id,
                    Title = phase.Title ?? phase.Phase ?? string.Empty,
                    Section = CodeReviewSectionKey,
                    Priority = "high",
                    Done = phase.Done,
                    Estimate = phase.Estimate,
                    ImplementationTasks = phase.ImplementationTasks?
                        .Where(static task => task is not null)
                        .Select(static task => new TodoFlatTask(task.Task ?? string.Empty, task.Done))
                        .ToList(),
                    ItemKind = CodeReviewPhaseItemKind,
                    SectionOrder = sectionOrder,
                    ItemOrder = index,
                    Phase = phase.Phase,
                };

                await InsertCurrentItemAsync(connection, storedItem, cancellationToken).ConfigureAwait(false);
                await InsertHistoryEntryAsync(connection, storedItem.Id, "imported", storedItem.ToFlatItem(), null, YamlBootstrapSource, importedAtUtc, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ImportPriorityListAsync(
        SqliteConnection connection,
        string sectionKey,
        List<TodoItem>? items,
        string priority,
        int sectionOrder,
        DateTime importedAtUtc,
        CancellationToken cancellationToken)
    {
        if (items is null)
            return;

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            if (item?.Id is null)
                continue;

            var storedItem = new StoredTodoItem
            {
                Id = item.Id,
                Title = item.Title ?? string.Empty,
                Section = sectionKey,
                Priority = priority,
                Done = item.Done,
                Estimate = item.Estimate,
                Note = item.Note,
                Description = item.Description,
                TechnicalDetails = item.TechnicalDetails,
                ImplementationTasks = item.ImplementationTasks?
                    .Where(static task => task is not null)
                    .Select(static task => new TodoFlatTask(task.Task ?? string.Empty, task.Done))
                    .ToList(),
                CompletedDate = item.CompletedDate,
                DoneSummary = item.DoneSummary,
                Remaining = item.Remaining,
                PriorityNote = item.PriorityNote,
                Reference = item.Reference,
                DependsOn = item.DependsOn,
                FunctionalRequirements = item.FunctionalRequirements,
                TechnicalRequirements = item.TechnicalRequirements,
                ItemKind = StandardItemKind,
                SectionOrder = sectionOrder,
                ItemOrder = index,
            };

            await InsertCurrentItemAsync(connection, storedItem, cancellationToken).ConfigureAwait(false);
            await InsertHistoryEntryAsync(connection, storedItem.Id, "imported", storedItem.ToFlatItem(), null, YamlBootstrapSource, importedAtUtc, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ImportMetadataIfMissingAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var currentMetadata = await GetDocumentMetadataAsync(connection, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(currentMetadata.NotesJson)
            || !string.IsNullOrWhiteSpace(currentMetadata.CompletedJson)
            || !string.IsNullOrWhiteSpace(currentMetadata.CodeReviewReference))
        {
            return;
        }

        var file = await TodoYamlFileSerializer.ReadIfExistsAsync(_todoFilePath, cancellationToken).ConfigureAwait(false);
        if (file is null)
            return;

        var updatedMetadata = currentMetadata with
        {
            NotesJson = file.Notes is null ? currentMetadata.NotesJson : JsonSerializer.Serialize(file.Notes, _json),
            CompletedJson = file.Completed is null ? currentMetadata.CompletedJson : JsonSerializer.Serialize(file.Completed, _json),
            CodeReviewReference = file.CodeReviewRemediation?.Reference ?? currentMetadata.CodeReviewReference,
            LastImportedFromYamlUtc = currentMetadata.LastImportedFromYamlUtc ?? DateTime.UtcNow.ToString("O"),
        };

        await UpdateDocumentMetadataAsync(connection, updatedMetadata, cancellationToken).ConfigureAwait(false);
    }

    private async Task BackfillHistoryAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var items = await GetAllStoredAsync(cancellationToken).ConfigureAwait(false);
        foreach (var item in items)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM todo_item_history WHERE todo_id = $id;";
            command.Parameters.AddWithValue("$id", item.Id);
            var count = Convert.ToInt32((long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? 0L));
            if (count > 0)
                continue;

            await InsertHistoryEntryAsync(connection, item.Id, "imported", item.ToFlatItem(), null, SqliteBackfillSource, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task NormalizeOrderingAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var items = await GetAllStoredAsync(cancellationToken).ConfigureAwait(false);
        var sectionOrders = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var bucketOrders = new Dictionary<(string Section, string Priority, string Kind), int>();
        var nextSectionOrder = 0;

        foreach (var item in items)
        {
            if (!sectionOrders.TryGetValue(item.Section, out var sectionOrder))
            {
                sectionOrder = nextSectionOrder++;
                sectionOrders[item.Section] = sectionOrder;
            }

            var bucketKey = (item.Section, item.Priority, item.ItemKind);
            if (!bucketOrders.TryGetValue(bucketKey, out var itemOrder))
                itemOrder = 0;

            bucketOrders[bucketKey] = itemOrder + 1;
            if (item.SectionOrder == sectionOrder && item.ItemOrder == itemOrder)
                continue;

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE todo_items SET section_order = $sectionOrder, item_order = $itemOrder WHERE id = $id;";
            command.Parameters.AddWithValue("$sectionOrder", sectionOrder);
            command.Parameters.AddWithValue("$itemOrder", itemOrder);
            command.Parameters.AddWithValue("$id", item.Id);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<TodoMutationResult> FinalizeMutationAsync(string action, string id, TodoFlatItem? item, CancellationToken cancellationToken)
    {
        _auditLog.RecordWrite(_dataSource, DateTime.UtcNow);
        await PublishChangeSafeAsync(action, id, cancellationToken).ConfigureAwait(false);

        try
        {
            await ProjectDatabaseToYamlAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or YamlException or InvalidOperationException or SqliteException)
        {
            await TryRecordProjectionFailureAsync(ex).ConfigureAwait(false);
            _logger.LogError(ex, "TODO mutation for {Id} committed in SQLite but projection to {TodoFilePath} failed.", id, _todoFilePath);
            var message = $"TODO '{id}' was committed to authoritative SQLite storage, but projection to '{_todoFilePath}' failed: {ex.Message}";
            return new TodoMutationResult(false, message, item, TodoMutationFailureKind.ProjectionFailed);
        }

        return new TodoMutationResult(true, Item: item);
    }

    private async Task ProjectDatabaseToYamlAsync(CancellationToken cancellationToken)
    {
        var file = await BuildProjectedTodoFileAsync(cancellationToken).ConfigureAwait(false);
        await TodoYamlFileSerializer.WriteAtomicallyAsync(_todoFilePath, file, cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            var metadata = await GetDocumentMetadataAsync(connection, CancellationToken.None).ConfigureAwait(false);
            var updatedMetadata = metadata with
            {
                LastProjectedToYamlUtc = DateTime.UtcNow.ToString("O"),
                LastProjectionFailureUtc = null,
                LastProjectionFailureMessage = null,
            };
            await UpdateDocumentMetadataAsync(connection, updatedMetadata, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or SqliteException)
        {
            _logger.LogWarning(ex, "TODO.yaml projection succeeded for {TodoFilePath}, but projection metadata could not be updated.", _todoFilePath);
        }
    }

    private async Task<TodoFile> BuildProjectedTodoFileAsync(CancellationToken cancellationToken)
    {
        var items = await GetAllStoredAsync(cancellationToken).ConfigureAwait(false);

        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var metadata = await GetDocumentMetadataAsync(connection, cancellationToken).ConfigureAwait(false);
        return BuildProjectedTodoFile(items, metadata);
    }

    private TodoFile BuildProjectedTodoFile(IReadOnlyList<StoredTodoItem> items, TodoDocumentMetadata metadata)
    {
        var file = new TodoFile();
        foreach (var sectionGroup in items
            .Where(static item => item.ItemKind == StandardItemKind)
            .GroupBy(static item => item.Section, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Min(static item => item.SectionOrder))
            .ThenBy(static group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var section = new TodoSection
            {
                HighPriority = BuildPriorityItems(sectionGroup, "high"),
                MediumPriority = BuildPriorityItems(sectionGroup, "medium"),
                LowPriority = BuildPriorityItems(sectionGroup, "low"),
            };

            file.Sections[sectionGroup.Key] = section;
        }

        var phases = items
            .Where(static item => item.ItemKind == CodeReviewPhaseItemKind)
            .OrderBy(static item => item.ItemOrder)
            .ThenBy(static item => item.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static item => new CodeReviewPhase
            {
                Id = item.Id,
                Phase = item.Phase,
                Estimate = item.Estimate,
                Done = item.Done,
                Title = item.Title,
                ImplementationTasks = item.ImplementationTasks?
                    .Select(static task => new ImplementationTask { Task = task.Task, Done = task.Done })
                    .ToList(),
            })
            .ToList();

        if (phases.Count > 0 || !string.IsNullOrWhiteSpace(metadata.CodeReviewReference))
        {
            file.CodeReviewRemediation = new CodeReviewSection
            {
                Reference = metadata.CodeReviewReference,
                Phases = phases.Count == 0 ? null : phases,
            };
        }

        file.Completed = DeserializeJson<List<CompletedGroup>>(metadata.CompletedJson);
        file.Notes = DeserializeJson<List<string>>(metadata.NotesJson);
        return file;
    }

    private async Task<TodoProjectionStatusResult> GetProjectionStatusCoreAsync(CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var metadata = await GetDocumentMetadataAsync(connection, cancellationToken).ConfigureAwait(false);
        var projectedFile = await BuildProjectedTodoFileAsync(cancellationToken).ConfigureAwait(false);

        var projectionTargetExists = File.Exists(_todoFilePath);
        var projectionConsistent = false;
        string? consistencyMessage = null;

        if (!projectionTargetExists)
        {
            consistencyMessage = Directory.Exists(_todoFilePath)
                ? $"Projected TODO target '{_todoFilePath}' is a directory instead of a file."
                : $"Projected TODO file '{_todoFilePath}' does not exist.";
        }
        else
        {
            try
            {
                var actualFile = await TodoYamlFileSerializer.ReadIfExistsAsync(_todoFilePath, cancellationToken).ConfigureAwait(false);
                if (actualFile is null)
                {
                    consistencyMessage = $"Projected TODO file '{_todoFilePath}' could not be loaded for consistency verification.";
                }
                else
                {
                    projectionConsistent = string.Equals(
                        NormalizeYaml(TodoYamlFileSerializer.Serialize(actualFile)),
                        NormalizeYaml(TodoYamlFileSerializer.Serialize(projectedFile)),
                        StringComparison.Ordinal);

                    if (!projectionConsistent)
                        consistencyMessage = $"Projected TODO file '{_todoFilePath}' does not match authoritative SQLite state.";
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or YamlException)
            {
                consistencyMessage = $"Projected TODO file '{_todoFilePath}' could not be read for consistency verification: {ex.Message}";
            }
        }

        var repairRequired = !projectionTargetExists || !projectionConsistent;
        var historicalFailureMessage = string.IsNullOrWhiteSpace(metadata.LastProjectionFailureMessage)
            ? null
            : $"Last recorded projection failure at {metadata.LastProjectionFailureUtc ?? "an unknown time"}: {metadata.LastProjectionFailureMessage}";

        var message = consistencyMessage
            ?? (repairRequired
                ? historicalFailureMessage ?? "TODO.yaml requires repair to match authoritative SQLite state."
                : historicalFailureMessage is null
                    ? "TODO.yaml matches authoritative SQLite state."
                    : $"TODO.yaml matches authoritative SQLite state. {historicalFailureMessage}");

        return new TodoProjectionStatusResult(
            AuthoritativeStore: "sqlite",
            AuthoritativeDataSource: _dataSource,
            ProjectionTargetPath: _todoFilePath,
            ProjectionTargetExists: projectionTargetExists,
            ProjectionConsistent: projectionConsistent,
            RepairRequired: repairRequired,
            VerifiedAtUtc: DateTime.UtcNow.ToString("O"),
            LastImportedFromYamlUtc: metadata.LastImportedFromYamlUtc,
            LastProjectedToYamlUtc: metadata.LastProjectedToYamlUtc,
            LastProjectionFailureUtc: metadata.LastProjectionFailureUtc,
            LastProjectionFailure: metadata.LastProjectionFailureMessage,
            Message: message);
    }

    private async Task TryRecordProjectionFailureAsync(Exception ex)
    {
        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            var metadata = await GetDocumentMetadataAsync(connection, CancellationToken.None).ConfigureAwait(false);
            var updatedMetadata = metadata with
            {
                LastProjectionFailureUtc = DateTime.UtcNow.ToString("O"),
                LastProjectionFailureMessage = ex.Message,
            };
            await UpdateDocumentMetadataAsync(connection, updatedMetadata, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception recordEx) when (recordEx is InvalidOperationException or SqliteException)
        {
            _logger.LogWarning(recordEx, "Failed to persist TODO projection failure metadata for {TodoFilePath}.", _todoFilePath);
        }
    }

    private static List<TodoItem>? BuildPriorityItems(IGrouping<string, StoredTodoItem> group, string priority)
    {
        var items = group
            .Where(item => string.Equals(item.Priority, priority, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static item => item.ItemOrder)
            .ThenBy(static item => item.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static item => new TodoItem
            {
                Id = item.Id,
                Title = item.Title,
                Estimate = item.Estimate,
                Note = item.Note,
                Done = item.Done,
                CompletedDate = item.CompletedDate,
                Description = item.Description?.ToList(),
                DoneSummary = item.DoneSummary,
                Remaining = item.Remaining,
                TechnicalDetails = item.TechnicalDetails?.ToList(),
                PriorityNote = item.PriorityNote,
                Reference = item.Reference,
                DependsOn = item.DependsOn?.ToList(),
                FunctionalRequirements = item.FunctionalRequirements?.ToList(),
                TechnicalRequirements = item.TechnicalRequirements?.ToList(),
                ImplementationTasks = item.ImplementationTasks?
                    .Select(static task => new ImplementationTask { Task = task.Task, Done = task.Done })
                    .ToList(),
            })
            .ToList();

        return items.Count == 0 ? null : items;
    }

    private async Task<List<StoredTodoItem>> GetAllStoredAsync(CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT *
            FROM todo_items
            WHERE is_deleted = 0
            ORDER BY section_order ASC,
                     section COLLATE NOCASE ASC,
                     CASE LOWER(priority) WHEN 'high' THEN 0 WHEN 'medium' THEN 1 ELSE 2 END ASC,
                     item_order ASC,
                     id COLLATE NOCASE ASC;
            """;
        var items = new List<StoredTodoItem>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            items.Add(ReadStoredTodo(reader));
        return items;
    }

    private async Task<StoredTodoItem?> GetStoredByIdAsync(string id, CancellationToken cancellationToken, bool includeDeleted = false)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = includeDeleted
            ? "SELECT * FROM todo_items WHERE id = $id LIMIT 1;"
            : "SELECT * FROM todo_items WHERE id = $id AND is_deleted = 0 LIMIT 1;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadStoredTodo(reader) : null;
    }

    private StoredTodoItem ReadStoredTodo(SqliteDataReader reader)
    {
        return new StoredTodoItem
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            Section = reader.GetString(reader.GetOrdinal("section")),
            Priority = reader.GetString(reader.GetOrdinal("priority")),
            Done = reader.GetInt64(reader.GetOrdinal("done")) == 1,
            Estimate = GetNullableString(reader, "estimate"),
            Note = GetNullableString(reader, "note"),
            Description = DeserializeList(GetNullableString(reader, "description_json")),
            TechnicalDetails = DeserializeList(GetNullableString(reader, "technical_details_json")),
            ImplementationTasks = DeserializeTasks(GetNullableString(reader, "implementation_tasks_json")),
            CompletedDate = GetNullableString(reader, "completed_date"),
            DoneSummary = GetNullableString(reader, "done_summary"),
            Remaining = GetNullableString(reader, "remaining"),
            PriorityNote = GetNullableString(reader, "priority_note"),
            Reference = GetNullableString(reader, "reference"),
            DependsOn = DeserializeList(GetNullableString(reader, "depends_on_json")),
            FunctionalRequirements = DeserializeList(GetNullableString(reader, "functional_requirements_json")),
            TechnicalRequirements = DeserializeList(GetNullableString(reader, "technical_requirements_json")),
            ItemKind = GetNullableString(reader, "item_kind") ?? StandardItemKind,
            SectionOrder = GetNullableInt32(reader, "section_order"),
            ItemOrder = GetNullableInt32(reader, "item_order"),
            Phase = GetNullableString(reader, "phase_label"),
        };
    }

    private async Task InsertCurrentItemAsync(SqliteConnection connection, StoredTodoItem item, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO todo_items (
                id, title, section, priority, done, estimate, note, description_json,
                technical_details_json, implementation_tasks_json, completed_date, done_summary,
                remaining, priority_note, reference, depends_on_json,
                functional_requirements_json, technical_requirements_json,
                item_kind, section_order, item_order, phase_label
            ) VALUES (
                $id, $title, $section, $priority, $done, $estimate, $note, $description,
                $technical, $implementation, $completed, $doneSummary,
                $remaining, $priorityNote, $reference, $dependsOn,
                $functionalRequirements, $technicalRequirements,
                $itemKind, $sectionOrder, $itemOrder, $phase
            );
            """;
        BindParameters(command, item);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateCurrentItemAsync(SqliteConnection connection, StoredTodoItem item, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE todo_items
            SET
                title = $title,
                section = $section,
                priority = $priority,
                done = $done,
                estimate = $estimate,
                note = $note,
                description_json = $description,
                technical_details_json = $technical,
                implementation_tasks_json = $implementation,
                completed_date = $completed,
                done_summary = $doneSummary,
                remaining = $remaining,
                priority_note = $priorityNote,
                reference = $reference,
                depends_on_json = $dependsOn,
                functional_requirements_json = $functionalRequirements,
                technical_requirements_json = $technicalRequirements,
                item_kind = $itemKind,
                section_order = $sectionOrder,
                item_order = $itemOrder,
                phase_label = $phase
            WHERE id = $id;
            """;
        BindParameters(command, item);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private void BindParameters(SqliteCommand command, StoredTodoItem item)
    {
        command.Parameters.AddWithValue("$id", item.Id);
        command.Parameters.AddWithValue("$title", item.Title);
        command.Parameters.AddWithValue("$section", item.Section);
        command.Parameters.AddWithValue("$priority", item.Priority);
        command.Parameters.AddWithValue("$done", item.Done ? 1 : 0);
        command.Parameters.AddWithValue("$estimate", (object?)item.Estimate ?? DBNull.Value);
        command.Parameters.AddWithValue("$note", (object?)item.Note ?? DBNull.Value);
        command.Parameters.AddWithValue("$description", (object?)SerializeValue(item.Description) ?? DBNull.Value);
        command.Parameters.AddWithValue("$technical", (object?)SerializeValue(item.TechnicalDetails) ?? DBNull.Value);
        command.Parameters.AddWithValue("$implementation", (object?)SerializeValue(item.ImplementationTasks) ?? DBNull.Value);
        command.Parameters.AddWithValue("$completed", (object?)item.CompletedDate ?? DBNull.Value);
        command.Parameters.AddWithValue("$doneSummary", (object?)item.DoneSummary ?? DBNull.Value);
        command.Parameters.AddWithValue("$remaining", (object?)item.Remaining ?? DBNull.Value);
        command.Parameters.AddWithValue("$priorityNote", (object?)item.PriorityNote ?? DBNull.Value);
        command.Parameters.AddWithValue("$reference", (object?)item.Reference ?? DBNull.Value);
        command.Parameters.AddWithValue("$dependsOn", (object?)SerializeValue(item.DependsOn) ?? DBNull.Value);
        command.Parameters.AddWithValue("$functionalRequirements", (object?)SerializeValue(item.FunctionalRequirements) ?? DBNull.Value);
        command.Parameters.AddWithValue("$technicalRequirements", (object?)SerializeValue(item.TechnicalRequirements) ?? DBNull.Value);
        command.Parameters.AddWithValue("$itemKind", item.ItemKind);
        command.Parameters.AddWithValue("$sectionOrder", item.SectionOrder);
        command.Parameters.AddWithValue("$itemOrder", item.ItemOrder);
        command.Parameters.AddWithValue("$phase", (object?)item.Phase ?? DBNull.Value);
    }

    private async Task InsertHistoryEntryAsync(
        SqliteConnection connection,
        string id,
        string action,
        TodoFlatItem? snapshot,
        TodoFlatItem? previousSnapshot,
        string? source,
        CancellationToken cancellationToken)
        => await InsertHistoryEntryAsync(connection, id, action, snapshot, previousSnapshot, source, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);

    private async Task InsertHistoryEntryAsync(
        SqliteConnection connection,
        string id,
        string action,
        TodoFlatItem? snapshot,
        TodoFlatItem? previousSnapshot,
        string? source,
        DateTime recordedAtUtc,
        CancellationToken cancellationToken)
    {
        using var versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "SELECT COALESCE(MAX(version), 0) FROM todo_item_history WHERE todo_id = $id;";
        versionCommand.Parameters.AddWithValue("$id", id);
        var currentVersion = Convert.ToInt32((long)(await versionCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? 0L));

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO todo_item_history (
                todo_id, version, action, recorded_at_utc, snapshot_json, previous_snapshot_json, source)
            VALUES (
                $id, $version, $action, $recordedAtUtc, $snapshotJson, $previousSnapshotJson, $source);
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$version", currentVersion + 1);
        command.Parameters.AddWithValue("$action", action);
        command.Parameters.AddWithValue("$recordedAtUtc", recordedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$snapshotJson", (object?)SerializeFlatItem(snapshot) ?? DBNull.Value);
        command.Parameters.AddWithValue("$previousSnapshotJson", (object?)SerializeFlatItem(previousSnapshot) ?? DBNull.Value);
        command.Parameters.AddWithValue("$source", (object?)source ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<TodoDocumentMetadata> GetDocumentMetadataAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT notes_json, completed_json, code_review_reference, last_imported_from_yaml_utc, last_projected_to_yaml_utc, last_projection_failure_utc, last_projection_failure_message FROM todo_document_metadata WHERE singleton_id = 1 LIMIT 1;";
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return new TodoDocumentMetadata(null, null, null, null, null, null, null);

        return new TodoDocumentMetadata(
            GetNullableString(reader, "notes_json"),
            GetNullableString(reader, "completed_json"),
            GetNullableString(reader, "code_review_reference"),
            GetNullableString(reader, "last_imported_from_yaml_utc"),
            GetNullableString(reader, "last_projected_to_yaml_utc"),
            GetNullableString(reader, "last_projection_failure_utc"),
            GetNullableString(reader, "last_projection_failure_message"));
    }

    private async Task UpdateDocumentMetadataAsync(SqliteConnection connection, TodoDocumentMetadata metadata, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE todo_document_metadata
            SET notes_json = $notesJson,
                completed_json = $completedJson,
                code_review_reference = $codeReviewReference,
                last_imported_from_yaml_utc = $lastImported,
                last_projected_to_yaml_utc = $lastProjected,
                last_projection_failure_utc = $lastProjectionFailureUtc,
                last_projection_failure_message = $lastProjectionFailureMessage
            WHERE singleton_id = 1;
            """;
        command.Parameters.AddWithValue("$notesJson", (object?)metadata.NotesJson ?? DBNull.Value);
        command.Parameters.AddWithValue("$completedJson", (object?)metadata.CompletedJson ?? DBNull.Value);
        command.Parameters.AddWithValue("$codeReviewReference", (object?)metadata.CodeReviewReference ?? DBNull.Value);
        command.Parameters.AddWithValue("$lastImported", (object?)metadata.LastImportedFromYamlUtc ?? DBNull.Value);
        command.Parameters.AddWithValue("$lastProjected", (object?)metadata.LastProjectedToYamlUtc ?? DBNull.Value);
        command.Parameters.AddWithValue("$lastProjectionFailureUtc", (object?)metadata.LastProjectionFailureUtc ?? DBNull.Value);
        command.Parameters.AddWithValue("$lastProjectionFailureMessage", (object?)metadata.LastProjectionFailureMessage ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateCodeReviewReferenceAsync(SqliteConnection connection, string? reference, CancellationToken cancellationToken)
    {
        var metadata = await GetDocumentMetadataAsync(connection, cancellationToken).ConfigureAwait(false);
        await UpdateDocumentMetadataAsync(connection, metadata with { CodeReviewReference = reference }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> ResolveSectionOrderAsync(SqliteConnection connection, string section, CancellationToken cancellationToken)
    {
        using var existingCommand = connection.CreateCommand();
        existingCommand.CommandText = "SELECT MIN(section_order) FROM todo_items WHERE section = $section AND is_deleted = 0;";
        existingCommand.Parameters.AddWithValue("$section", section);
        var existingValue = await existingCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (existingValue is long longValue)
            return Convert.ToInt32(longValue);

        using var nextCommand = connection.CreateCommand();
        nextCommand.CommandText = "SELECT COALESCE(MAX(section_order), -1) + 1 FROM todo_items WHERE is_deleted = 0;";
        return Convert.ToInt32((long)(await nextCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? 0L));
    }

    private async Task<int> GetNextItemOrderAsync(SqliteConnection connection, string section, string priority, string itemKind, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(item_order), -1) + 1 FROM todo_items WHERE section = $section AND priority = $priority AND item_kind = $itemKind AND is_deleted = 0;";
        command.Parameters.AddWithValue("$section", section);
        command.Parameters.AddWithValue("$priority", priority);
        command.Parameters.AddWithValue("$itemKind", itemKind);
        return Convert.ToInt32((long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? 0L));
    }

    private async Task<int> GetCurrentItemCountAsync(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM todo_items WHERE is_deleted = 0;";
        return Convert.ToInt32((long)(await command.ExecuteScalarAsync().ConfigureAwait(false) ?? 0L));
    }

    private async Task<HashSet<string>> GetTableColumnsAsync(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync().ConfigureAwait(false))
            columns.Add(reader.GetString(reader.GetOrdinal("name")));
        return columns;
    }

    private async Task ExecuteNonQueryAsync(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private SqliteConnection CreateConnection() => new($"Data Source={_dataSource};Pooling=False");

    private string? SerializeValue<T>(IReadOnlyList<T>? value) => value is null ? null : JsonSerializer.Serialize(value, _json);

    private string? SerializeFlatItem(TodoFlatItem? item) => item is null ? null : JsonSerializer.Serialize(item, _json);

    private T? DeserializeJson<T>(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? default
            : JsonSerializer.Deserialize<T>(value, _json);

    private static string NormalizeYaml(string yaml)
        => yaml.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

    private TodoFlatItem? DeserializeFlatItem(string? value) => DeserializeJson<TodoFlatItem>(value);

    private List<string>? DeserializeList(string? value) => DeserializeJson<List<string>>(value);

    private List<TodoFlatTask>? DeserializeTasks(string? value) => DeserializeJson<List<TodoFlatTask>>(value);

    private static string NormalizeSection(string section)
        => string.IsNullOrWhiteSpace(section) ? string.Empty : section.Trim();

    private static string NormalizePriority(string section, string priority)
        => string.Equals(section, CodeReviewSectionKey, StringComparison.OrdinalIgnoreCase) ? "high" : priority;

    private static string DetermineItemKind(string section)
        => string.Equals(section, CodeReviewSectionKey, StringComparison.OrdinalIgnoreCase) ? CodeReviewPhaseItemKind : StandardItemKind;

    private static bool RequiresNewItemOrder(StoredTodoItem existing, StoredTodoItem updated)
        => !string.Equals(existing.Section, updated.Section, StringComparison.OrdinalIgnoreCase)
           || !string.Equals(existing.Priority, updated.Priority, StringComparison.OrdinalIgnoreCase)
           || !string.Equals(existing.ItemKind, updated.ItemKind, StringComparison.OrdinalIgnoreCase);

    private static string? GetNullableString(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int GetNullableInt32(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
    }

    private static List<TodoFlatItem> ApplyFilters(List<TodoFlatItem> items, TodoQueryRequest request)
    {
        IEnumerable<TodoFlatItem> filtered = items;

        if (!string.IsNullOrWhiteSpace(request.Id))
            filtered = filtered.Where(i => string.Equals(i.Id, request.Id, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Section))
            filtered = filtered.Where(i => string.Equals(i.Section, request.Section, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Priority))
            filtered = filtered.Where(i => string.Equals(i.Priority, request.Priority, StringComparison.OrdinalIgnoreCase));

        if (request.Done.HasValue)
            filtered = filtered.Where(i => i.Done == request.Done.Value);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var matcher = BooleanSearchParser.Parse(request.Keyword);
            filtered = filtered.Where(i => matcher(BuildKeywordSearchText(i)));
        }

        return filtered.ToList();
    }

    private static string BuildKeywordSearchText(TodoFlatItem item)
        => string.Join(
            " ",
            new[] { item.Id, item.Title, item.Note, item.DoneSummary, item.Remaining, item.Phase }
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Concat(item.Description ?? Array.Empty<string>())
                .Concat(item.TechnicalDetails ?? Array.Empty<string>())
                .Concat(item.ImplementationTasks?.Select(static task => task.Task) ?? Array.Empty<string>())
                .Where(static value => !string.IsNullOrWhiteSpace(value)));

    private async Task PublishChangeSafeAsync(string action, string entityId, CancellationToken cancellationToken)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.Todo,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = $"mcp://workspace/todo/{entityId}",
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing sqlite TODO change event for {EntityId}", entityId);
        }
    }

    private sealed record StoredTodoItem
    {
        public required string Id { get; init; }
        public required string Title { get; init; }
        public required string Section { get; init; }
        public required string Priority { get; init; }
        public required bool Done { get; init; }
        public string? Estimate { get; init; }
        public string? Note { get; init; }
        public IReadOnlyList<string>? Description { get; init; }
        public IReadOnlyList<string>? TechnicalDetails { get; init; }
        public IReadOnlyList<TodoFlatTask>? ImplementationTasks { get; init; }
        public string? CompletedDate { get; init; }
        public string? DoneSummary { get; init; }
        public string? Remaining { get; init; }
        public string? PriorityNote { get; init; }
        public string? Reference { get; init; }
        public string? Phase { get; init; }
        public IReadOnlyList<string>? DependsOn { get; init; }
        public IReadOnlyList<string>? FunctionalRequirements { get; init; }
        public IReadOnlyList<string>? TechnicalRequirements { get; init; }
        public required string ItemKind { get; init; }
        public required int SectionOrder { get; init; }
        public required int ItemOrder { get; init; }

        public TodoFlatItem ToFlatItem() => new()
        {
            Id = Id,
            Title = Title,
            Section = Section,
            Priority = Priority,
            Done = Done,
            Estimate = Estimate,
            Note = Note,
            Description = Description,
            TechnicalDetails = TechnicalDetails,
            ImplementationTasks = ImplementationTasks,
            CompletedDate = CompletedDate,
            DoneSummary = DoneSummary,
            Remaining = Remaining,
            PriorityNote = PriorityNote,
            Reference = Reference,
            Phase = Phase,
            DependsOn = DependsOn,
            FunctionalRequirements = FunctionalRequirements,
            TechnicalRequirements = TechnicalRequirements,
        };
    }

    private sealed record TodoDocumentMetadata(
        string? NotesJson,
        string? CompletedJson,
        string? CodeReviewReference,
        string? LastImportedFromYamlUtc,
        string? LastProjectedToYamlUtc,
        string? LastProjectionFailureUtc,
        string? LastProjectionFailureMessage);
}
