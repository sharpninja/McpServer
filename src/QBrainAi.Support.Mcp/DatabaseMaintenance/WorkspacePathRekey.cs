using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Database;

namespace QBrainAi.Support.Mcp.DatabaseMaintenance;

/// <summary>TR-MCP-PATHNORM-001: raw, provider-portable rekey planning and transactional execution.</summary>
internal static class WorkspacePathRekey
{
    internal sealed record Mapping(string OldKey, string NewKey);
    internal sealed record TableCount(string Table, long Before, long After, IReadOnlyDictionary<string, long> Workspaces);
    internal sealed record Report(bool DryRun, long ChangedRows, IReadOnlyList<Mapping> Mappings,
        IReadOnlyList<TableCount> Tables, int ManualTriageGroupsRetained);
    private sealed record Table(string Schema, string Name, HashSet<string> Columns)
    {
        internal string Sql => string.IsNullOrEmpty(Schema) ? Quote(Name) : Quote(Schema) + "." + Quote(Name);
    }
    private sealed record Change(Table Table, string Column, Dictionary<string, string> Values);
    private sealed record Plan(List<Change> Changes, List<Mapping> Mappings, List<TableCount> Tables, int Manual);
    private static readonly HashSet<string> IdentityColumns = new(StringComparer.Ordinal)
    {
        "WorkspaceId", "OwnerWorkspaceId", "WorkspacePath", "OriginalWorkspacePath",
        "EffectiveWorkspacePath", "GlobalWorkspaceId", "CanonicalWorkspaceId"
    };

    /// <summary>Policy A normalization, shared with the raw startup guard.</summary>
    public static string Normalize(string value) => WorkspacePathRekeyGuard.Normalize(value);

    /// <summary>Rejects legacy rows before canonical EF predicates can hide them.</summary>
    public static Task ValidateStartupAsync(DbConnection connection, CancellationToken cancellationToken) =>
        WorkspacePathRekeyGuard.ValidateStartupAsync(connection, cancellationToken);

    /// <summary>Plans against an explicitly selected connection; apply requires a fresh, verified backup.</summary>
    public static async Task<Report> RunAsync(DbConnection connection, bool dryRun, string? backupPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var backupConnectionSettings = connection.ConnectionString;
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var tables = await InventoryAsync(connection, null, cancellationToken).ConfigureAwait(false);
        var plan = await PlanAsync(connection, null, tables, cancellationToken).ConfigureAwait(false);
        if (dryRun) return new(true, 0, plan.Mappings, plan.Tables, plan.Manual);
        if (string.IsNullOrWhiteSpace(backupPath))
            throw new InvalidOperationException("Apply requires --backup-path and a successful verified backup.");
        await BackupAsync(connection, backupPath, cancellationToken, backupConnectionSettings).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        try
        {
            // Replan under the transaction: no update uses a stale preflight.
            plan = await PlanAsync(connection, transaction, tables, cancellationToken).ConfigureAwait(false);
            var restore = await DeferConstraintsAsync(connection, transaction, tables, cancellationToken).ConfigureAwait(false);
            long changed = 0;
            foreach (var change in plan.Changes)
            {
                foreach (var batch in change.Values.Where(x => x.Key != x.Value).Chunk(200))
                {
                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandTimeout = 600;
                    var arms = new List<string>();
                    var keys = new List<string>();
                    for (var i = 0; i < batch.Length; i++)
                    {
                        Add(command, "@o" + i, batch[i].Key);
                        Add(command, "@n" + i, batch[i].Value);
                        arms.Add($"WHEN @o{i} THEN @n{i}");
                        keys.Add("@o" + i);
                    }
                    command.CommandText = $"UPDATE {change.Table.Sql} SET {Quote(change.Column)} = CASE {Exact(connection, change.Column)} "
                        + string.Join(" ", arms) + $" ELSE {Quote(change.Column)} END WHERE {Exact(connection, change.Column)} IN ({string.Join(",", keys)})";
                    changed += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            foreach (var sql in restore)
                await ExecuteAsync(connection, transaction, sql, cancellationToken).ConfigureAwait(false);
            if (connection is SqliteConnection)
            {
                var violations = await RowsAsync(connection, transaction, "PRAGMA foreign_key_check", cancellationToken).ConfigureAwait(false);
                if (violations.Count != 0) throw new InvalidOperationException("Rekey foreign-key validation failed.");
            }
            var after = await PlanAsync(connection, transaction, tables, cancellationToken).ConfigureAwait(false);
            if (after.Changes.Any(x => x.Values.Any(v => v.Key != v.Value)))
                throw new InvalidOperationException("Rekey validation found remaining noncanonical identities.");
            for (var i = 0; i < plan.Tables.Count; i++)
            {
                if (plan.Tables[i].Before != after.Tables[i].Before)
                    throw new InvalidOperationException("Rekey changed a table row count.");
                var expected = plan.Tables[i].Workspaces.ToDictionary(
                    x => plan.Mappings.FirstOrDefault(m => m.OldKey == x.Key)?.NewKey ?? x.Key, x => x.Value, StringComparer.Ordinal);
                if (expected.Count != after.Tables[i].Workspaces.Count
                    || expected.Any(x => !after.Tables[i].Workspaces.TryGetValue(x.Key, out var count) || count != x.Value))
                    throw new InvalidOperationException("Rekey changed a per-workspace row count.");
            }
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(false, changed, plan.Mappings, plan.Tables, plan.Manual);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<Plan> PlanAsync(DbConnection connection, DbTransaction? transaction, List<Table> tables, CancellationToken ct)
    {
        var identities = new Dictionary<string, string>(StringComparer.Ordinal);
        var changes = new List<Change>();
        var counts = new List<TableCount>();
        foreach (var table in tables)
        {
            foreach (var column in table.Columns.Where(c => IdentityColumns.Contains(c)
                || table.Name == "Workspaces" && c == "DataDirectory"))
            {
                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var row in await RowsAsync(connection, transaction,
                    $"SELECT DISTINCT {Exact(connection, column)} FROM {table.Sql} WHERE {Quote(column)} IS NOT NULL", ct).ConfigureAwait(false))
                {
                    var old = (string)row[0];
                    values[old] = Normalize(old);
                    // DataDirectory is an auxiliary path, not a workspace key.
                    if (column != "DataDirectory") identities[old] = Normalize(old);
                }
                changes.Add(new(table, column, values));
            }
            var workspaceCounts = new Dictionary<string, long>(StringComparer.Ordinal);
            if (table.Columns.Contains("WorkspaceId"))
                foreach (var row in await RowsAsync(connection, transaction,
                    $"SELECT {Exact(connection, "WorkspaceId")}, COUNT(*) FROM {table.Sql} GROUP BY {Exact(connection, "WorkspaceId")}", ct).ConfigureAwait(false))
                    if (row[0] is string key) workspaceCounts[key] = Convert.ToInt64(row[1]);
            var count = Convert.ToInt64((await RowsAsync(connection, transaction,
                $"SELECT COUNT(*) FROM {table.Sql}", ct).ConfigureAwait(false))[0][0]);
            counts.Add(new(table.Name, count, count, workspaceCounts));
        }
        CheckConvergence(identities);
        var federation = tables.FirstOrDefault(t => t.Name == "FederationWorkspaces");
        if (federation is not null)
        {
            var columns = new[] { "ProxyId", "WorkspacePath", "GlobalWorkspaceId", "CanonicalWorkspaceId" };
            RequireColumns(federation, columns);
            foreach (var row in await ReadAsync(connection, transaction, federation, columns, ct).ConfigureAwait(false))
            {
                var path = Text(row[1]);
                if (path == Normalize(path)) continue;
                var storage = WorkspaceIdentity.GetStorageKey(Normalize(path));
                var canonical = Digest($"{Text(row[0])}:{storage}".ToLowerInvariant());
                var oldDigest = Digest($"{Text(row[0])}:{storage.Replace('/', '\\')}".ToLowerInvariant());
                for (var i = 2; i < 4; i++)
                {
                    var old = Text(row[i]);
                    // Explicit custom federation identities are not reproducible and remain unchanged.
                    if (old == oldDigest) identities[old] = canonical;
                }
            }
        }
        CheckConvergence(identities);
        foreach (var change in changes)
            foreach (var old in change.Values.Keys.ToArray())
                if (identities.TryGetValue(old, out var replacement)) change.Values[old] = replacement;

        var manual = 0;
        var groups = tables.FirstOrDefault(t => t.Name == "TriageGroups");
        var reports = tables.FirstOrDefault(t => t.Name == "TriageReports");
        if (groups is not null)
        {
            RequireColumns(groups, ["GroupId", "GroupKey", "EffectiveWorkspacePath"]);
            var groupIds = new Dictionary<string, string>(StringComparer.Ordinal);
            var groupKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            var reportRows = reports is null ? [] : await ReadAsync(connection, transaction, reports,
                ["GroupId", "Fingerprint"], ct).ConfigureAwait(false);
            foreach (var row in await ReadAsync(connection, transaction, groups,
                ["GroupId", "GroupKey", "EffectiveWorkspacePath"], ct).ConfigureAwait(false))
            {
                var id = Text(row[0]); var key = Text(row[1]); var path = Text(row[2]);
                groupIds[id] = id; groupKeys[key] = key;
                var fingerprint = reportRows.Where(r => Text(r[0]) == id)
                    .Select(r => Text(r[1])).FirstOrDefault(f => Digest($"{path}|{f}").ToLowerInvariant() == key);
                if (fingerprint is null || id != "triage-group-" + key[..Math.Min(16, key.Length)])
                {
                    manual++;
                    continue;
                }
                var next = Digest($"{Normalize(path)}|{fingerprint}").ToLowerInvariant();
                groupKeys[key] = next;
                groupIds[id] = "triage-group-" + next[..16];
            }
            CheckConvergence(groupIds); CheckConvergence(groupKeys);
            changes.Add(new(groups, "GroupKey", groupKeys));
            foreach (var table in tables.Where(t => t.Columns.Contains("GroupId")
                && t.Name is "TriageGroups" or "TriageReports" or "TriageResearchRuns"))
                changes.Add(new(table, "GroupId", groupIds));
        }
        var handoff = tables.FirstOrDefault(t => t.Name == "HandoffIngestionRuns");
        if (handoff is not null)
        {
            var columns = new[] { "WorkspaceId", "ContentSha256", "PromptVersion", "Force", "RunId", "ReplayIdentity" };
            RequireColumns(handoff, columns);
            var replay = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in await ReadAsync(connection, transaction, handoff, columns, ct).ConfigureAwait(false))
            {
                var workspace = Text(row[0]);
                var mapped = identities.GetValueOrDefault(workspace, workspace);
                var old = Text(row[5]);
                replay[old] = workspace == mapped ? old
                    : HandoffReplayKeys.Create(mapped, Text(row[1]), Text(row[2]), Convert.ToBoolean(row[3]), Text(row[4]));
            }
            CheckConvergence(replay);
            changes.Add(new(handoff, "ReplayIdentity", replay));
        }
        return new(changes, identities.Where(x => x.Key != x.Value).OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => new Mapping(x.Key, x.Value)).ToList(), counts, manual);
    }

    private static void CheckConvergence(Dictionary<string, string> mapping)
    {
        if (mapping.GroupBy(x => x.Value, StringComparer.Ordinal).Any(g => g.Select(x => x.Key).Distinct(StringComparer.Ordinal).Skip(1).Any()))
            throw new InvalidOperationException("Distinct old keys converge to the same canonical key; rekey aborted before mutation.");
    }

    private static async Task<List<Table>> InventoryAsync(DbConnection connection, DbTransaction? transaction, CancellationToken ct)
    {
        var result = new List<Table>();
        if (connection is SqliteConnection)
        {
            var names = await RowsAsync(connection, transaction,
                "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'", ct).ConfigureAwait(false);
            foreach (var row in names)
            {
                var name = Text(row[0]);
                var columns = await RowsAsync(connection, transaction, $"PRAGMA table_info({Quote(name)})", ct).ConfigureAwait(false);
                result.Add(new("", name, columns.Select(c => Text(c[1])).ToHashSet(StringComparer.Ordinal)));
            }
        }
        else
        {
            var sql = connection.GetType().Name == "SqlConnection"
                ? "SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo'"
                : "SELECT table_schema,table_name,column_name FROM information_schema.columns WHERE table_schema=current_schema()";
            foreach (var group in (await RowsAsync(connection, transaction, sql, ct).ConfigureAwait(false))
                .GroupBy(r => (Schema: Text(r[0]), Name: Text(r[1]))))
                result.Add(new(group.Key.Schema, group.Key.Name, group.Select(r => Text(r[2])).ToHashSet(StringComparer.Ordinal)));
        }
        return result.Where(t => t.Columns.Any(IdentityColumns.Contains)
            || t.Name is "TriageResearchRuns" or "HandoffIngestionRuns").OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
    }

    private static async Task<List<string>> DeferConstraintsAsync(DbConnection connection, DbTransaction transaction, List<Table> tables, CancellationToken ct)
    {
        var restore = new List<string>();
        if (connection is SqliteConnection)
            await ExecuteAsync(connection, transaction, "PRAGMA defer_foreign_keys=ON", ct).ConfigureAwait(false);
        else if (connection.GetType().Name == "SqlConnection")
        {
            foreach (var table in tables)
            {
                await ExecuteAsync(connection, transaction, $"ALTER TABLE {table.Sql} NOCHECK CONSTRAINT ALL", ct).ConfigureAwait(false);
                restore.Add($"ALTER TABLE {table.Sql} WITH CHECK CHECK CONSTRAINT ALL");
            }
        }
        else
        {
            var foreignKeys = await RowsAsync(connection, transaction, """
                SELECT n.nspname,t.relname,c.conname,c.condeferrable,c.condeferred
                FROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace
                WHERE c.contype='f' AND n.nspname=current_schema()
                """, ct).ConfigureAwait(false);
            foreach (var row in foreignKeys)
            {
                var prefix = $"ALTER TABLE {Quote(Text(row[0]))}.{Quote(Text(row[1]))} ALTER CONSTRAINT {Quote(Text(row[2]))} ";
                await ExecuteAsync(connection, transaction, prefix + "DEFERRABLE INITIALLY DEFERRED", ct).ConfigureAwait(false);
                restore.Add(prefix + (Convert.ToBoolean(row[3])
                    ? "DEFERRABLE INITIALLY " + (Convert.ToBoolean(row[4]) ? "DEFERRED" : "IMMEDIATE")
                    : "NOT DEFERRABLE"));
            }
            await ExecuteAsync(connection, transaction, "SET CONSTRAINTS ALL DEFERRED", ct).ConfigureAwait(false);
            restore.Insert(0, "SET CONSTRAINTS ALL IMMEDIATE");
        }
        return restore;
    }

    private static async Task BackupAsync(DbConnection connection, string path, CancellationToken ct, string backupConnectionSettings)
    {
        if (connection is SqliteConnection sqlite)
        {
            path = Path.GetFullPath(path);
            using (new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
            var settings = new SqliteConnectionStringBuilder(sqlite.ConnectionString)
            {
                DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false
            };
            await using var backup = new SqliteConnection(settings.ConnectionString);
            await backup.OpenAsync(ct).ConfigureAwait(false);
            sqlite.BackupDatabase(backup);
            var integrity = await RowsAsync(backup, null, "PRAGMA integrity_check", ct).ConfigureAwait(false);
            if (integrity.Count != 1 || Text(integrity[0][0]) != "ok")
                throw new InvalidOperationException("Backup verification failed; no data was changed.");
            return;
        }
        // Provider-native backup is deliberately required; a supplied old file is not proof of a backup.
        if (connection.GetType().Name == "SqlConnection")
        {
            await using var check = connection.CreateCommand();
            check.CommandText = "EXEC master.dbo.xp_fileexist @path";
            Add(check, "@path", path);
            if (Convert.ToInt32(await check.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 0)
                throw new InvalidOperationException("Backup target already exists.");
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 600;
            Add(command, "@path", path);
            command.CommandText = $"BACKUP DATABASE {Quote(connection.Database)} TO DISK=@path WITH COPY_ONLY,CHECKSUM";
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            command.CommandText = "RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM";
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            return;
        }
        if (connection is Npgsql.NpgsqlConnection postgres)
        {
            path = Path.GetFullPath(path);
            using (new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
            var settings = new Npgsql.NpgsqlConnectionStringBuilder(backupConnectionSettings);
            var environment = new Dictionary<string, string>
            {
                ["PGDATABASE"] = postgres.Database,
                ["PGHOST"] = settings.Host ?? "localhost",
                ["PGPORT"] = settings.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["PGUSER"] = settings.Username ?? "",
                ["PGPASSWORD"] = settings.Password ?? "",
                ["PGSSLMODE"] = settings.SslMode switch { Npgsql.SslMode.VerifyCA => "verify-ca", Npgsql.SslMode.VerifyFull => "verify-full", _ => settings.SslMode.ToString().ToLowerInvariant() },
            };
            await RunBackupToolAsync("pg_dump", ["--no-password", "--format=custom", "--file", path], environment, ct).ConfigureAwait(false);
            await RunBackupToolAsync("pg_restore", ["--list", path], environment, ct).ConfigureAwait(false);
            await using var verified = File.OpenRead(path);
            var signature = new byte[5];
            await verified.ReadExactlyAsync(signature, ct).ConfigureAwait(false);
            if (!signature.AsSpan().SequenceEqual("PGDMP"u8))
                throw new InvalidOperationException("PostgreSQL backup verification failed.");
            return;
        }
        throw new NotSupportedException("Unsupported rekey database provider.");
    }

    private static async Task RunBackupToolAsync(string tool, string[] arguments, Dictionary<string, string> environment, CancellationToken ct)
    {
        var start = new System.Diagnostics.ProcessStartInfo(tool)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        foreach (var variable in environment) start.Environment[variable.Key] = variable.Value;
        using var process = System.Diagnostics.Process.Start(start)
            ?? throw new InvalidOperationException("Could not start the native backup tool.");
        // Drain output, but never emit it: native errors can disclose connection details.
        var output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var error = process.StandardError.ReadToEndAsync(CancellationToken.None);
        try
        {
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            await Task.WhenAll(output, error).ConfigureAwait(false);
            if (process.ExitCode != 0)
                throw new InvalidOperationException("Native backup or verification failed; no data was changed.");
        }
        catch
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            await Task.WhenAll(output, error).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<List<object[]>> ReadAsync(DbConnection c, DbTransaction? t, Table table, string[] columns, CancellationToken ct)
    {
        RequireColumns(table, columns);
        return await RowsAsync(c, t, $"SELECT {string.Join(",", columns.Select(Quote))} FROM {table.Sql}", ct).ConfigureAwait(false);
    }

    private static void RequireColumns(Table table, string[] columns)
    {
        if (columns.Any(c => !table.Columns.Contains(c)))
            throw new InvalidOperationException($"Rekey cannot interpret the stored schema of {table.Name}.");
    }

    private static async Task<List<object[]>> RowsAsync(DbConnection connection, DbTransaction? transaction, string sql, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = 600;
        var result = new List<object[]>();
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var row = new object[reader.FieldCount];
            reader.GetValues(row);
            result.Add(row);
        }
        return result;
    }

    private static async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, string sql, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = 600;
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static void Add(DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter);
    }
    private static string Exact(DbConnection connection, string column) => Quote(column) + (connection.GetType().Name switch
    {
        "SqlConnection" => " COLLATE Latin1_General_100_BIN2",
        "NpgsqlConnection" => " COLLATE \"C\"",
        _ => " COLLATE BINARY"
    });
    private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    private static string Text(object value) => value is DBNull ? "" : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
