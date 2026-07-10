using Microsoft.Data.Sqlite;

namespace McpServer.SessionLog.Transcripts;

internal static class OpenCodeSqliteUtilities
{
    private static readonly string[] SnapshotExtensions = [".db", ".sqlite", ".sqlite3"];

    internal static bool IsSnapshotPath(string path)
        => SnapshotExtensions.Any(extension => Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase));

    internal static async Task<bool> LooksLikeOpenCodeStoreAsync(string path, CancellationToken cancellationToken)
    {
        if (!IsSnapshotPath(path) || !File.Exists(path))
            return false;

        string? snapshotPath = null;
        try
        {
            snapshotPath = await CreateSnapshotAsync(path, cancellationToken).ConfigureAwait(false);
            await using var connection = await OpenReadOnlyAsync(snapshotPath, cancellationToken).ConfigureAwait(false);
            var tables = await ReadTableNamesAsync(connection, cancellationToken).ConfigureAwait(false);
            return tables.Contains("session")
                && tables.Contains("message")
                && (tables.Contains("part") || tables.Contains("tool_event"));
        }
        catch (InvalidDataException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (SqliteException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteSnapshotDirectory(snapshotPath);
        }
    }

    internal static async Task<string> CreateSnapshotAsync(string sourcePath, CancellationToken cancellationToken)
    {
        var sourceFullPath = Path.GetFullPath(sourcePath);
        var sourceFileName = Path.GetFileName(sourceFullPath);
        var snapshotDirectory = Path.Combine(Path.GetTempPath(), "mcp-opencode-sqlite-snapshot", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(snapshotDirectory);
        var snapshotPath = Path.Combine(snapshotDirectory, sourceFileName);

        await CopyFileAsync(sourceFullPath, snapshotPath, cancellationToken).ConfigureAwait(false);
        foreach (var suffix in new[] { "-wal", "-shm" })
        {
            var sidecarPath = sourceFullPath + suffix;
            if (File.Exists(sidecarPath))
                await CopyFileAsync(sidecarPath, snapshotPath + suffix, cancellationToken).ConfigureAwait(false);
        }

        return snapshotPath;
    }

    internal static async Task<SqliteConnection> OpenReadOnlyAsync(string snapshotPath, CancellationToken cancellationToken)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = snapshotPath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    internal static async Task<HashSet<string>> ReadTableNamesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            tables.Add(reader.GetString(0));
        return tables;
    }

    internal static async Task<HashSet<string>> ReadColumnNamesAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(" + QuoteIdentifier(tableName) + ");";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            columns.Add(reader.GetString(1));
        return columns;
    }

    internal static string QuoteIdentifier(string identifier)
        => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    internal static string SelectColumnOrNull(IReadOnlySet<string> columns, string columnName, string alias)
        => columns.Contains(columnName)
            ? QuoteIdentifier(columnName) + " AS " + QuoteIdentifier(alias)
            : "NULL AS " + QuoteIdentifier(alias);

    internal static string OrderColumnOrFallback(IReadOnlySet<string> columns, params string[] columnNames)
    {
        foreach (var columnName in columnNames)
        {
            if (columns.Contains(columnName))
                return QuoteIdentifier(columnName);
        }

        return QuoteIdentifier("id");
    }

    internal static void DeleteSnapshotDirectory(string? snapshotPath)
    {
        if (string.IsNullOrWhiteSpace(snapshotPath))
            return;

        var directory = Path.GetDirectoryName(snapshotPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return;

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }
}
