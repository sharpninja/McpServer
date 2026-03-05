using System.Text.Json;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: SQLite-backed TODO storage.
/// Preserves the same ITodoService API contract as YAML-backed storage.
/// </summary>
internal sealed class SqliteTodoService : ITodoService, ITodoStore, IDisposable
{
    private readonly string _dataSource;
    private readonly IWriteAuditLog _auditLog;
    private readonly IChangeEventBus? _eventBus;
    private readonly ILogger<SqliteTodoService> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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
        EnsureSchema();
    }

    internal SqliteTodoService(string dataSource, IWriteAuditLog auditLog, ILogger<SqliteTodoService> logger, IChangeEventBus? eventBus = null)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus;
        EnsureSchema();
    }

    public void Dispose() => _writeLock.Dispose();

    public async Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        var filtered = ApplyFilters(all, request);
        return new TodoQueryResult(filtered, filtered.Count);
    }

    public async Task<TodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM todo_items WHERE id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return null;

        return ReadTodo(reader);
    }

    public async Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var idError = TodoValidator.ValidateTodoId(request.Id);
        if (idError is not null)
            return new TodoMutationResult(false, idError);

        var priorityError = TodoValidator.ValidatePriority(request.Priority);
        if (priorityError is not null)
            return new TodoMutationResult(false, priorityError);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = await GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
                return new TodoMutationResult(false, $"Item with id '{request.Id}' already exists.");

            var candidate = new TodoFlatItem
            {
                Id = request.Id,
                Title = request.Title,
                Section = request.Section,
                Priority = request.Priority,
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
            };

            var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
            var depIdError = TodoValidator.ValidateDependencyIds(request.DependsOn, all, "dependsOn");
            if (depIdError is not null)
                return new TodoMutationResult(false, depIdError);
            var depError = TodoValidator.ValidateDependencies(request.Id, request.DependsOn?.ToList() ?? [], all);
            if (depError is not null)
                return new TodoMutationResult(false, depError);

            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO todo_items (
                    id, title, section, priority, done, estimate, note, description_json,
                    technical_details_json, implementation_tasks_json, completed_date, done_summary,
                    remaining, priority_note, reference, depends_on_json,
                    functional_requirements_json, technical_requirements_json
                ) VALUES (
                    $id, $title, $section, $priority, $done, $estimate, $note, $description,
                    $technical, $implementation, $completed, $doneSummary,
                    $remaining, $priorityNote, $reference, $dependsOn,
                    $functionalRequirements, $technicalRequirements
                );
                """;
            BindParameters(command, candidate);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            _auditLog.RecordWrite(_dataSource, DateTime.UtcNow);
            _logger.LogInformation("Created TODO item {Id} in sqlite", request.Id);
            await PublishChangeSafeAsync(ChangeEventActions.Created, request.Id, cancellationToken).ConfigureAwait(false);
            return new TodoMutationResult(true, Item: candidate);
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

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                return new TodoMutationResult(false, $"Item with id '{id}' not found.");

            var updated = existing with
            {
                Title = request.Title ?? existing.Title,
                Section = request.Section ?? existing.Section,
                Priority = request.Priority ?? existing.Priority,
                Done = request.Done ?? existing.Done,
                Estimate = request.Estimate ?? existing.Estimate,
                Note = request.Note ?? existing.Note,
                Description = request.Description ?? existing.Description,
                TechnicalDetails = request.TechnicalDetails ?? existing.TechnicalDetails,
                ImplementationTasks = request.ImplementationTasks ?? existing.ImplementationTasks,
                CompletedDate = request.CompletedDate ?? existing.CompletedDate,
                DoneSummary = request.DoneSummary ?? existing.DoneSummary,
                Remaining = request.Remaining ?? existing.Remaining,
                DependsOn = request.DependsOn ?? existing.DependsOn,
                FunctionalRequirements = request.FunctionalRequirements ?? existing.FunctionalRequirements,
                TechnicalRequirements = request.TechnicalRequirements ?? existing.TechnicalRequirements,
            };

            var priorityError = TodoValidator.ValidatePriority(updated.Priority);
            if (priorityError is not null)
                return new TodoMutationResult(false, priorityError);

            var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
            var depIdError = TodoValidator.ValidateDependencyIds(updated.DependsOn, all, "dependsOn");
            if (depIdError is not null)
                return new TodoMutationResult(false, depIdError);
            var depError = TodoValidator.ValidateDependencies(id, updated.DependsOn?.ToList() ?? [], all);
            if (depError is not null)
                return new TodoMutationResult(false, depError);

            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
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
                    technical_requirements_json = $technicalRequirements
                WHERE id = $id;
                """;
            BindParameters(command, updated);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            _auditLog.RecordWrite(_dataSource, DateTime.UtcNow);
            _logger.LogInformation("Updated TODO item {Id} in sqlite", id);
            await PublishChangeSafeAsync(ChangeEventActions.Updated, id, cancellationToken).ConfigureAwait(false);
            return new TodoMutationResult(true, Item: updated);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM todo_items WHERE id = $id;";
            command.Parameters.AddWithValue("$id", id);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            if (affected == 0)
                return new TodoMutationResult(false, $"Item with id '{id}' not found.");

            _auditLog.RecordWrite(_dataSource, DateTime.UtcNow);
            _logger.LogInformation("Deleted TODO item {Id} from sqlite", id);
            await PublishChangeSafeAsync(ChangeEventActions.Deleted, id, cancellationToken).ConfigureAwait(false);
            return new TodoMutationResult(true);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private SqliteConnection CreateConnection() => new($"Data Source={_dataSource};Pooling=False");

    private void EnsureSchema()
    {
        var dir = Path.GetDirectoryName(_dataSource);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var connection = CreateConnection();
        connection.Open();
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
                technical_requirements_json TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_todo_items_section ON todo_items(section);
            CREATE INDEX IF NOT EXISTS idx_todo_items_priority ON todo_items(priority);
            CREATE INDEX IF NOT EXISTS idx_todo_items_done ON todo_items(done);
            """;
        command.ExecuteNonQuery();
    }

    private async Task<List<TodoFlatItem>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = new List<TodoFlatItem>();
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM todo_items;";
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            items.Add(ReadTodo(reader));
        return items;
    }

    private TodoFlatItem ReadTodo(SqliteDataReader reader)
    {
        var id = reader.GetString(reader.GetOrdinal("id"));
        var title = reader.GetString(reader.GetOrdinal("title"));
        var section = reader.GetString(reader.GetOrdinal("section"));
        var priority = reader.GetString(reader.GetOrdinal("priority"));
        var done = reader.GetInt64(reader.GetOrdinal("done")) == 1;
        var estimate = GetNullableString(reader, "estimate");
        var note = GetNullableString(reader, "note");
        var completedDate = GetNullableString(reader, "completed_date");
        var doneSummary = GetNullableString(reader, "done_summary");
        var remaining = GetNullableString(reader, "remaining");
        var priorityNote = GetNullableString(reader, "priority_note");
        var reference = GetNullableString(reader, "reference");
        var description = DeserializeList(GetNullableString(reader, "description_json"));
        var technicalDetails = DeserializeList(GetNullableString(reader, "technical_details_json"));
        var implementationTasks = DeserializeTasks(GetNullableString(reader, "implementation_tasks_json"));
        var dependsOn = DeserializeList(GetNullableString(reader, "depends_on_json"));
        var functionalRequirements = DeserializeList(GetNullableString(reader, "functional_requirements_json"));
        var technicalRequirements = DeserializeList(GetNullableString(reader, "technical_requirements_json"));

        return new TodoFlatItem
        {
            Id = id,
            Title = title,
            Section = section,
            Priority = priority,
            Done = done,
            Estimate = estimate,
            Note = note,
            Description = description,
            TechnicalDetails = technicalDetails,
            ImplementationTasks = implementationTasks,
            CompletedDate = completedDate,
            DoneSummary = doneSummary,
            Remaining = remaining,
            PriorityNote = priorityNote,
            Reference = reference,
            DependsOn = dependsOn,
            FunctionalRequirements = functionalRequirements,
            TechnicalRequirements = technicalRequirements,
        };
    }

    private void BindParameters(SqliteCommand command, TodoFlatItem item)
    {
        command.Parameters.AddWithValue("$id", item.Id);
        command.Parameters.AddWithValue("$title", item.Title);
        command.Parameters.AddWithValue("$section", item.Section);
        command.Parameters.AddWithValue("$priority", item.Priority);
        command.Parameters.AddWithValue("$done", item.Done ? 1 : 0);
        command.Parameters.AddWithValue("$estimate", (object?)item.Estimate ?? DBNull.Value);
        command.Parameters.AddWithValue("$note", (object?)item.Note ?? DBNull.Value);
        command.Parameters.AddWithValue("$description", (object?)Serialize(item.Description) ?? DBNull.Value);
        command.Parameters.AddWithValue("$technical", (object?)Serialize(item.TechnicalDetails) ?? DBNull.Value);
        command.Parameters.AddWithValue("$implementation", (object?)Serialize(item.ImplementationTasks) ?? DBNull.Value);
        command.Parameters.AddWithValue("$completed", (object?)item.CompletedDate ?? DBNull.Value);
        command.Parameters.AddWithValue("$doneSummary", (object?)item.DoneSummary ?? DBNull.Value);
        command.Parameters.AddWithValue("$remaining", (object?)item.Remaining ?? DBNull.Value);
        command.Parameters.AddWithValue("$priorityNote", (object?)item.PriorityNote ?? DBNull.Value);
        command.Parameters.AddWithValue("$reference", (object?)item.Reference ?? DBNull.Value);
        command.Parameters.AddWithValue("$dependsOn", (object?)Serialize(item.DependsOn) ?? DBNull.Value);
        command.Parameters.AddWithValue("$functionalRequirements", (object?)Serialize(item.FunctionalRequirements) ?? DBNull.Value);
        command.Parameters.AddWithValue("$technicalRequirements", (object?)Serialize(item.TechnicalRequirements) ?? DBNull.Value);
    }

    private string? Serialize<T>(IReadOnlyList<T>? value) => value is null ? null : JsonSerializer.Serialize(value, _json);

    private static string? GetNullableString(SqliteDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetString(index);
    }

    private List<string>? DeserializeList(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : JsonSerializer.Deserialize<List<string>>(value, _json);

    private List<TodoFlatTask>? DeserializeTasks(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : JsonSerializer.Deserialize<List<TodoFlatTask>>(value, _json);

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
            var kw = request.Keyword;
            filtered = filtered.Where(i => MatchesKeyword(i, kw));
        }

        return filtered.ToList();
    }

    private static bool MatchesKeyword(TodoFlatItem item, string keyword)
    {
        if (item.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase)) return true;
        if (item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) return true;
        if (item.Description?.Any(d => d.Contains(keyword, StringComparison.OrdinalIgnoreCase)) == true) return true;
        if (item.TechnicalDetails?.Any(d => d.Contains(keyword, StringComparison.OrdinalIgnoreCase)) == true) return true;
        if (item.Note?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true) return true;
        if (item.DoneSummary?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true) return true;
        if (item.Remaining?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true) return true;
        if (item.ImplementationTasks?.Any(t => t.Task.Contains(keyword, StringComparison.OrdinalIgnoreCase)) == true) return true;
        return false;
    }

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
}
