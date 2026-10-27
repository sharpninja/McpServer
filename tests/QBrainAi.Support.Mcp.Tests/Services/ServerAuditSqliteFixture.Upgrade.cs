// SPDX-License-Identifier: Apache-2.0
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Database;

namespace QBrainAi.Support.Mcp.Tests.Services;

public sealed partial class ServerAuditSqliteFixture
{
    private static readonly string[] LegacyTables =
    [
        "SessionLogs", "SessionLogTurns", "SessionLogActions", "SessionLogProcessingDialogs",
        "SessionLogTurnStringLists", "SessionLogTags", "SessionLogTurnTags", "SessionLogTurnContexts",
        "SessionLogCommits", "SessionLogCommitFiles",
    ];
    private readonly Dictionary<string, string[]> _legacyColumns = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long[]> _legacyIds = new(StringComparer.Ordinal);

    /// <summary>Creates an empty retained file fixture for the supported preceding-schema migration path.</summary>
    /// <param name="artifactRoot">Task-owned fixture directory.</param>
    /// <returns>An adapter whose schema is created only through provider-owned migrations.</returns>
    public static ServerAuditSqliteFixture CreateForUpgrade(string artifactRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);
        var directory = Path.Combine(artifactRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return new ServerAuditSqliteFixture(Path.Combine(directory, "upgrade.sqlite"));
    }

    /// <summary>Runs the frozen upgrade dependency operation through real provider migration/readiness boundaries.</summary>
    /// <param name="operation">Frozen provider operation.</param>
    /// <param name="inputJson">Native serialized scenario data.</param>
    /// <returns>Native JSON from actual database reads and service results.</returns>
    public string InvokeUpgrade(string operation, string inputJson)
        => JsonSerializer.Serialize(InvokeUpgradeAsync(operation, inputJson).GetAwaiter().GetResult(), JsonOptions);

    private async Task<object?> InvokeUpgradeAsync(string operation, string inputJson)
    {
        using var input = JsonDocument.Parse(inputJson);
        var body = input.RootElement;
        _trace.Enqueue(new { operation, input = body.Clone() });
        await using var db = OpenMigrationDb();
        switch (operation)
        {
            case "SeedLegacy":
            {
                var defined = db.Database.GetMigrations().ToArray();
                var target = Array.FindIndex(defined, id => id.EndsWith("_AddSessionAuditEvents", StringComparison.Ordinal));
                if (target <= 0)
                    throw new InvalidOperationException("Exactly preceded provider-owned audit event migration is required");
                await db.GetService<IMigrator>().MigrateAsync(defined[target - 1]).ConfigureAwait(false);
                // Register the fixture parent through the existing tracked workspace boundary;
                // raw old-row seeding below intentionally does not invoke SaveChanges stamping.
                db.Workspaces.Add(new QBrainAi.Support.Mcp.Storage.Entities.WorkspaceEntity
                {
                    WorkspaceId = db.CurrentWorkspaceId, WorkspacePath = "F:/fixture/qb",
                    Name = "upgrade-fixture", TodoPath = "None", IsEnabled = true,
                    DateTimeCreated = DateTimeOffset.UnixEpoch, DateTimeModified = DateTimeOffset.UnixEpoch,
                });
                _ = await db.SaveChangesAsync().ConfigureAwait(false);
                await db.Database.OpenConnectionAsync().ConfigureAwait(false);
                var tables = body.GetProperty("tables");
                if (tables.EnumerateObject().Count() != LegacyTables.Length)
                    throw new InvalidOperationException("All frozen legacy tables must be seeded");
                foreach (var table in LegacyTables)
                {
                    _legacyColumns[table] = await ReadColumnNamesAsync(db.Database.GetDbConnection(), table).ConfigureAwait(false);
                    var rows = tables.GetProperty(table).EnumerateArray().ToArray();
                    _legacyIds[table] = rows.Select(row => row.GetProperty("Id").GetInt64()).ToArray();
                    foreach (var row in rows)
                    {
                        var fields = row.EnumerateObject().ToArray();
                        if (fields.Any(field => !_legacyColumns[table].Contains(field.Name, StringComparer.Ordinal)))
                            throw new InvalidOperationException("Seed names a field absent from the actual preceding schema");
                        await using var command = db.Database.GetDbConnection().CreateCommand();
                        command.CommandText = $"INSERT INTO {Quote(table)} ({string.Join(",", fields.Select(field => Quote(field.Name)))}) VALUES ({string.Join(",", fields.Select((_, index) => "@p" + index))})";
                        for (var index = 0; index < fields.Length; index++)
                        {
                            var parameter = command.CreateParameter();
                            parameter.ParameterName = "@p" + index;
                            parameter.Value = fields[index].Value.ValueKind switch
                            {
                                JsonValueKind.String => fields[index].Value.GetString()!,
                                JsonValueKind.Number => fields[index].Value.GetInt64(),
                                JsonValueKind.Null => DBNull.Value,
                                _ => throw new InvalidOperationException("Unsupported frozen legacy field type"),
                            };
                            command.Parameters.Add(parameter);
                        }
                        _ = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                }
                return new { seeded = true, precedingMigration = defined[target - 1] };
            }
            case "ReadLegacy":
                return await ReadLegacyAsync(db).ConfigureAwait(false);
            case "EnsureReady":
            {
                var provider = McpDatabaseProviderFactory.CreateOptions("sqlite", _connectionString);
                var runtime = new McpDatabaseRuntimeOptions(provider,
                    new McpDatabaseEncryptionOptions(false, null, null, null, null, null, null),
                    body.GetProperty("autoMigrate").GetBoolean());
                await McpDatabaseMigrationCoordinator.EnsureReadyAsync(db, runtime).ConfigureAwait(false);
                return new { ready = true };
            }
            case "SchemaAdminUpgrade":
                await McpDatabaseMigrationCoordinator.ApplyMigrationsAsync(db,
                    McpDatabaseProviderFactory.CreateOptions("sqlite", _connectionString)).ConfigureAwait(false);
                return new { applied = true };
            case "Submit":
                return await Service(db).AppendAuditEventAsync(
                    JsonSerializer.Deserialize<SessionAuditEventRequest>(inputJson, JsonOptions)!).ConfigureAwait(false);
            case "Read":
                return await Service(db).ReadAuditEventAsync(Text(body, "agentIdentity"),
                    Text(body, "mcpSessionId"), Text(body, "requestId"), Text(body, "eventId")).ConfigureAwait(false);
            case "Reopen":
                await db.Database.OpenConnectionAsync().ConfigureAwait(false);
                _ = await db.SessionAuditEvents.AsNoTracking().CountAsync().ConfigureAwait(false);
                _trace.Enqueue(new { operation = "ReopenDatabase", contextId = db.ContextId.ToString() });
                return new { reopened = true };
            case "Snapshot":
            {
                var applied = (await db.Database.GetAppliedMigrationsAsync().ConfigureAwait(false)).ToArray();
                var pending = (await db.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).ToArray();
                return new
                {
                    schema = pending.Length == 0 && applied.Any(IsAuditMigration) ? "current" : "old",
                    history = applied.Select(id => IsAuditMigration(id) ? "C7" : id).ToArray(),
                    trace = _trace.ToArray(), databasePath = DatabasePath,
                    legacyMetadataClear = await LegacyMetadataClearAsync(db).ConfigureAwait(false),
                };
            }
            default:
                throw new ArgumentException("Unknown upgrade dependency operation", nameof(operation));
        }
    }

    private McpDbContext OpenMigrationDb()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>();
        McpDatabaseProviderFactory.Configure(options, McpDatabaseProviderFactory.CreateOptions("sqlite", _connectionString));
        return new McpDbContext(options.Options, new WorkspaceContext { WorkspacePath = "F:/fixture/qb" });
    }

    private static bool IsAuditMigration(string id) => id.EndsWith("_AddSessionAuditEvents", StringComparison.Ordinal);

    private async Task<object> ReadLegacyAsync(McpDbContext db)
    {
        if (_legacyColumns.Count != LegacyTables.Length)
            throw new InvalidOperationException("Preceding schema/column evidence is missing");
        await db.Database.OpenConnectionAsync().ConfigureAwait(false);
        var tables = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var table in LegacyTables)
        {
            var currentColumns = await ReadColumnNamesAsync(db.Database.GetDbConnection(), table).ConfigureAwait(false);
            await using var command = db.Database.GetDbConnection().CreateCommand();
            // New event projections carry a UUID. Every legacy row, including an
            // accidental duplicated legacy row, remains in this comparison.
            var filter = currentColumns.Contains("AuditEventId", StringComparer.Ordinal)
                ? " WHERE \"AuditEventId\" IS NULL" : string.Empty;
            command.CommandText = $"SELECT {string.Join(",", _legacyColumns[table].Select(Quote))} FROM {Quote(table)}{filter} ORDER BY \"Id\"";
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var row = new Dictionary<string, object?>(StringComparer.Ordinal);
                for (var index = 0; index < reader.FieldCount; index++)
                    row.Add(reader.GetName(index), reader.IsDBNull(index) ? null : reader.GetValue(index));
                rows.Add(row);
            }
            tables.Add(table, rows);
        }
        return new { tables };
    }

    private async Task<bool> LegacyMetadataClearAsync(McpDbContext db)
    {
        await db.Database.OpenConnectionAsync().ConfigureAwait(false);
        foreach (var table in LegacyTables.Where(table => table is
            "SessionLogActions" or "SessionLogProcessingDialogs" or "SessionLogTurnStringLists"))
        {
            var columns = await ReadColumnNamesAsync(db.Database.GetDbConnection(), table).ConfigureAwait(false);
            var added = columns.Where(column => column is "AuditEventId" or "AuditEventOrder" or "OriginalTimestamp").ToArray();
            if (added.Length == 0)
                continue;
            await using var command = db.Database.GetDbConnection().CreateCommand();
            var ids = _legacyIds[table];
            for (var index = 0; index < ids.Length; index++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@id" + index;
                parameter.Value = ids[index];
                command.Parameters.Add(parameter);
            }
            command.CommandText = $"SELECT COUNT(*) FROM {Quote(table)} WHERE \"Id\" IN ({string.Join(",", ids.Select((_, index) => "@id" + index))}) AND ({string.Join(" OR ", added.Select(column => Quote(column) + " IS NOT NULL"))})";
            if (Convert.ToInt64(await command.ExecuteScalarAsync().ConfigureAwait(false),
                System.Globalization.CultureInfo.InvariantCulture) != 0)
                return false;
        }
        return true;
    }

    private static async Task<string[]> ReadColumnNamesAsync(DbConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {Quote(table)} LIMIT 0";
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        return Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
    }

    private static string Quote(string identifier)
    {
        if (identifier.Length == 0 || identifier.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
            throw new InvalidOperationException("Invalid fixture identifier");
        return "\"" + identifier + "\"";
    }
}
