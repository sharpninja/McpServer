using System.Data;
using System.Data.Common;
using System.Diagnostics;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.DatabaseMaintenance;

/// <summary>
/// TR-MCP-SEC-004: Builds and optionally executes provider-native encryption transition workflows.
/// </summary>
internal static class McpDatabaseEncryptionTransitionRunner
{
    private const string SqliteAlgorithm = "aes256ofb";

    /// <summary>
    /// Builds or executes the provider-specific transition workflow represented by <paramref name="options"/>.
    /// </summary>
    /// <param name="runtimeOptions">Resolved runtime provider and encryption settings.</param>
    /// <param name="options">Transition command options.</param>
    /// <param name="cancellationToken">Cancellation token for async work.</param>
    /// <returns>A structured report describing the transition.</returns>
    public static async Task<McpDatabaseEncryptionTransitionReport> RunAsync(
        McpDatabaseRuntimeOptions runtimeOptions,
        McpDatabaseEncryptionTransitionOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runtimeOptions);
        ArgumentNullException.ThrowIfNull(options);

        var report = new McpDatabaseEncryptionTransitionReport
        {
            Provider = runtimeOptions.ProviderOptions.ProviderName,
            Operation = options.Operation,
            Execute = options.Execute,
            InstanceName = options.InstanceName,
        };

        switch (options.Operation)
        {
            case McpDatabaseEncryptionTransitionOperation.Verify:
                await RunVerifyAsync(runtimeOptions, report, cancellationToken).ConfigureAwait(false);
                return report;
            case McpDatabaseEncryptionTransitionOperation.Enable:
                await RunTransitionAsync(runtimeOptions, options, report, true, cancellationToken).ConfigureAwait(false);
                return report;
            case McpDatabaseEncryptionTransitionOperation.Disable:
                await RunTransitionAsync(runtimeOptions, options, report, false, cancellationToken).ConfigureAwait(false);
                return report;
            default:
                throw new InvalidOperationException($"Unsupported encryption transition operation '{options.Operation}'.");
        }
    }

    private static async Task RunVerifyAsync(
        McpDatabaseRuntimeOptions runtimeOptions,
        McpDatabaseEncryptionTransitionReport report,
        CancellationToken cancellationToken)
    {
        var step = report.AddStep(
            "Validate live encryption state",
            "Reuse the same provider-aware validation path that normal startup uses so maintenance verification and runtime enforcement stay aligned.");

        await using var dbContext = CreateDbContext(runtimeOptions.ProviderOptions);
        await McpDatabaseEncryptionCoordinator.ValidateAsync(dbContext, runtimeOptions, cancellationToken).ConfigureAwait(false);
        step.Status = "completed";
        report.Summary = "Live encryption state matches the configured runtime intent.";
    }

    private static async Task RunTransitionAsync(
        McpDatabaseRuntimeOptions runtimeOptions,
        McpDatabaseEncryptionTransitionOptions options,
        McpDatabaseEncryptionTransitionReport report,
        bool targetEncrypted,
        CancellationToken cancellationToken)
    {
        switch (runtimeOptions.ProviderOptions.ProviderKind)
        {
            case McpDatabaseProviderKind.Sqlite:
                await RunSqliteAsync(runtimeOptions, options, report, targetEncrypted, cancellationToken).ConfigureAwait(false);
                return;
            case McpDatabaseProviderKind.PostgreSql:
                await RunPostgreSqlAsync(runtimeOptions, options, report, targetEncrypted, cancellationToken).ConfigureAwait(false);
                return;
            case McpDatabaseProviderKind.SqlServer:
                await RunSqlServerAsync(runtimeOptions, options, report, targetEncrypted, cancellationToken).ConfigureAwait(false);
                return;
            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider '{runtimeOptions.ProviderOptions.ProviderName}' for encryption transitions.");
        }
    }

    private static async Task RunSqliteAsync(
        McpDatabaseRuntimeOptions runtimeOptions,
        McpDatabaseEncryptionTransitionOptions options,
        McpDatabaseEncryptionTransitionReport report,
        bool targetEncrypted,
        CancellationToken cancellationToken)
    {
        var sourcePath = ResolveSqliteDatabasePath(runtimeOptions.ProviderOptions.ConnectionString);
        var seeToolPath = options.SqliteSeeToolPath ?? runtimeOptions.EncryptionOptions.SqliteSeeToolPath;
        var currentKey = options.CurrentKey;
        var targetKey = options.TargetKey ?? (targetEncrypted ? runtimeOptions.EncryptionOptions.SqliteKey : string.Empty);

        if (!targetEncrypted && string.IsNullOrWhiteSpace(currentKey))
        {
            currentKey = runtimeOptions.EncryptionOptions.SqliteKey;
        }

        if (string.IsNullOrWhiteSpace(seeToolPath))
        {
            throw new InvalidOperationException(
                "SQLite transitions require a SEE-capable sqlite3 CLI. Set Mcp:Database:Encryption:Sqlite:SeeToolPath, MCP_SQLITE_SEE_TOOL_PATH, or pass --sqlite-see-tool-path.");
        }

        if (targetEncrypted && string.IsNullOrWhiteSpace(targetKey))
        {
            throw new InvalidOperationException(
                "SQLite enable transitions require a target key. Set Mcp:Database:Encryption:Sqlite:Key, MCP_SQLITE_ENCRYPTION_KEY, or pass --target-key.");
        }

        if (!targetEncrypted && string.IsNullOrWhiteSpace(currentKey))
        {
            throw new InvalidOperationException(
                "SQLite disable transitions require the current encryption key because the disabled configuration no longer carries the old key. Pass --current-key.");
        }

        var backupPath = string.IsNullOrWhiteSpace(options.BackupPath)
            ? $"{sourcePath}.{DateTime.UtcNow:yyyyMMddHHmmss}.bak"
            : Path.GetFullPath(options.BackupPath);
        var workingPath = $"{sourcePath}.{Guid.NewGuid():N}.working";
        var rekeyCommand = targetEncrypted
            ? $".text-rekey \"\" \"{EscapeSqliteShellText(targetKey!)}\" \"{EscapeSqliteShellText(targetKey!)}\"\n.filectrl reserve_bytes 12\nVACUUM;\n.quit\n"
            : $".text-rekey \"{EscapeSqliteShellText(currentKey!)}\" \"\" \"\"\n.quit\n";
        var redactedRekeyCommand = targetEncrypted
            ? ".text-rekey \"\" \"<target-key>\" \"<target-key>\"\n.filectrl reserve_bytes 12\nVACUUM;\n.quit\n"
            : ".text-rekey \"<current-key>\" \"\" \"\"\n.quit\n";
        var verificationCommand = targetEncrypted
            ? "PRAGMA integrity_check;\n.dbinfo\n.quit\n"
            : "PRAGMA integrity_check;\n.quit\n";

        report.AddStep("Create a safety backup", $"Copy '{sourcePath}' to '{backupPath}' before mutating any pages.");
        report.AddStep(
            "Rekey a working copy with SEE",
            $"Create a working copy of '{sourcePath}', then use '{seeToolPath}' to {(targetEncrypted ? "encrypt" : "decrypt")} the working copy without touching the live file.",
            redactedRekeyCommand.Trim());
        report.AddStep(
            "Verify the working copy",
            targetEncrypted
                ? "Reopen the working copy with the target key, run integrity_check, and inspect .dbinfo so the nonce reserve bytes are non-zero before cutover."
                : "Reopen the working copy without a key and run integrity_check before cutover.",
            verificationCommand.Trim());
        report.AddStep(
            "Cut over and retain rollback material",
            $"Replace '{sourcePath}' with the verified working copy and keep '{backupPath}' until the server restarts cleanly with the new configuration.");
        report.Notes.Add("SQLite transitions use the SEE text-key path because the workspace configuration surface currently stores passphrases, not raw binary or hex keys.");

        if (!options.Execute)
        {
            report.Summary = targetEncrypted ? "SQLite enable transition plan generated." : "SQLite disable transition plan generated.";
            return;
        }

        var cutoverCompleted = false;
        File.Copy(sourcePath, backupPath, overwrite: false);
        File.Copy(sourcePath, workingPath, overwrite: true);
        try
        {
            await RunSqliteShellAsync(seeToolPath, workingPath, targetEncrypted ? null : currentKey, rekeyCommand, cancellationToken).ConfigureAwait(false);
            var verificationOutput = await RunSqliteShellAsync(seeToolPath, workingPath, targetEncrypted ? targetKey : null, verificationCommand, cancellationToken).ConfigureAwait(false);
            EnsureSqliteVerificationPassed(verificationOutput, targetEncrypted);

            File.Move(workingPath, sourcePath, overwrite: true);
            cutoverCompleted = true;
            SetAllStepStatuses(report, "completed");
            report.Summary = targetEncrypted
                ? "SQLite database encrypted successfully. Keep the backup until the server restarts cleanly with encryption enabled."
                : "SQLite database decrypted successfully. Keep the backup until the server restarts cleanly with encryption disabled.";
        }
        catch
        {
            report.Warnings.Add(
                cutoverCompleted
                    ? $"SQLite transition failed after cutover. Keep backup '{backupPath}' and inspect '{sourcePath}' before restarting."
                    : $"SQLite transition failed before cutover. Keep backup '{backupPath}' and inspect '{workingPath}' for recovery details.");
            throw;
        }
        finally
        {
            if (!cutoverCompleted && File.Exists(workingPath))
            {
                report.Warnings.Add($"SQLite retained the unfinished working copy at '{workingPath}' for manual inspection.");
            }

            if (cutoverCompleted && File.Exists(workingPath))
            {
                File.Delete(workingPath);
            }
        }
    }

    private static async Task RunPostgreSqlAsync(
        McpDatabaseRuntimeOptions runtimeOptions,
        McpDatabaseEncryptionTransitionOptions options,
        McpDatabaseEncryptionTransitionReport report,
        bool targetEncrypted,
        CancellationToken cancellationToken)
    {
        var dumpToolPath = options.PostgreSqlDumpToolPath ?? "pg_dump.exe";
        if (options.Execute && string.IsNullOrWhiteSpace(options.BackupPath))
        {
            throw new InvalidOperationException(
                "PostgreSQL transitions require --backup-path so pg_dump can capture rollback material before any table rewrite occurs.");
        }

        report.AddStep("Capture a pg_dump backup", "Run pg_dump in custom format before rewriting any application tables so rollback remains possible.", $"{dumpToolPath} -Fc -f <backup-path> -d <configured-connection-string>");
        report.AddStep(
            "Rewrite application tables",
            targetEncrypted
                ? "Rewrite each public application table from heap to tde_heap and run SELECT count(*) after each rewrite."
                : "Rewrite each encrypted public application table from tde_heap back to heap and run SELECT count(*) after each rewrite.",
            targetEncrypted
                ? "ALTER TABLE \"public\".\"<table>\" SET ACCESS METHOD tde_heap;\nSELECT count(*) FROM \"public\".\"<table>\";"
                : "ALTER TABLE \"public\".\"<table>\" SET ACCESS METHOD heap;\nSELECT count(*) FROM \"public\".\"<table>\";");
        report.AddStep("Verify final table state", "Use pg_tde_is_encrypted(...) to confirm every application table reached the requested state.", "SELECT schemaname, tablename, pg_tde_is_encrypted(format('%I.%I', schemaname, tablename)::regclass) FROM pg_tables WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory';");

        if (!options.Execute)
        {
            report.Summary = targetEncrypted ? "PostgreSQL enable transition plan generated." : "PostgreSQL disable transition plan generated.";
            return;
        }

        if (targetEncrypted
            && (string.IsNullOrWhiteSpace(runtimeOptions.EncryptionOptions.PostgreSqlKeyProvider)
                || string.IsNullOrWhiteSpace(runtimeOptions.EncryptionOptions.PostgreSqlPrincipalKey)))
        {
            throw new InvalidOperationException(
                "PostgreSQL enable transitions require pg_tde key-provider and principal-key configuration before table rewrites can begin.");
        }

        await RunProcessAsync(dumpToolPath, ["-Fc", "-f", Path.GetFullPath(options.BackupPath!), "-d", runtimeOptions.ProviderOptions.ConnectionString], null, cancellationToken).ConfigureAwait(false);

        await using var dbContext = CreateDbContext(runtimeOptions.ProviderOptions);
        await using var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var extensionInstalled = await ExecuteScalarAsync<bool>(
            connection,
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_extension
                WHERE extname = 'pg_tde');
            """,
            cancellationToken).ConfigureAwait(false);
        if (!extensionInstalled)
        {
            throw new InvalidOperationException("The connected PostgreSQL database does not expose pg_tde. Provision Percona Server for PostgreSQL with pg_tde before running a transition.");
        }

        var tables = await ReadPostgreSqlTableStatesAsync(connection, cancellationToken).ConfigureAwait(false);
        foreach (var table in tables.Where(t => targetEncrypted ? !t.IsEncrypted : t.IsEncrypted))
        {
            var rewriteSql = targetEncrypted
                ? $"ALTER TABLE {QuotePostgreSqlIdentifier(table.Schema)}.{QuotePostgreSqlIdentifier(table.Table)} SET ACCESS METHOD tde_heap;"
                : $"ALTER TABLE {QuotePostgreSqlIdentifier(table.Schema)}.{QuotePostgreSqlIdentifier(table.Table)} SET ACCESS METHOD heap;";
            await ExecuteNonQueryAsync(connection, rewriteSql, cancellationToken).ConfigureAwait(false);
            await ExecuteScalarAsync<long>(connection, $"SELECT count(*) FROM {QuotePostgreSqlIdentifier(table.Schema)}.{QuotePostgreSqlIdentifier(table.Table)};", cancellationToken).ConfigureAwait(false);
        }

        await ValidateAgainstTargetStateAsync(runtimeOptions, targetEncrypted, cancellationToken).ConfigureAwait(false);
        SetAllStepStatuses(report, "completed");
        report.Summary = targetEncrypted
            ? "PostgreSQL tables were rewritten to pg_tde successfully. Keep the pg_dump backup until the server restarts cleanly with encryption enabled."
            : "PostgreSQL tables were rewritten back to heap successfully. Keep the pg_dump backup until the server restarts cleanly with encryption disabled.";
    }

    private static async Task RunSqlServerAsync(
        McpDatabaseRuntimeOptions runtimeOptions,
        McpDatabaseEncryptionTransitionOptions options,
        McpDatabaseEncryptionTransitionReport report,
        bool targetEncrypted,
        CancellationToken cancellationToken)
    {
        if (options.Execute && string.IsNullOrWhiteSpace(options.BackupPath))
        {
            throw new InvalidOperationException("SQL Server transitions require --backup-path because BACKUP DATABASE must run before the TDE state changes.");
        }

        report.AddStep("Capture a copy-only SQL Server backup", "Take a copy-only database backup before changing TDE state so rollback material exists if the transition has to be reversed.", "BACKUP DATABASE [<current-database>] TO DISK = N'<backup-path>' WITH COPY_ONLY, INIT, CHECKSUM;");
        report.AddStep(
            "Validate SQL Server TDE prerequisites",
            targetEncrypted
                ? "Verify that the configured server certificate already exists in master before enabling TDE."
                : "Verify that the database is reachable and that existing TDE state can be queried before disabling TDE.",
            targetEncrypted
                ? "SELECT COUNT(*) FROM master.sys.certificates WHERE name = @certificateName;"
                : "SELECT TOP (1) encryption_state FROM sys.dm_database_encryption_keys WHERE database_id = DB_ID();");
        report.AddStep(
            "Change database TDE state",
            targetEncrypted
                ? "Create the database encryption key if needed, then enable TDE."
                : "Disable TDE and wait for decryption to complete.",
            targetEncrypted
                ? "CREATE DATABASE ENCRYPTION KEY WITH ALGORITHM = AES_256 ENCRYPTION BY SERVER CERTIFICATE [<certificate>];\nALTER DATABASE [<current-database>] SET ENCRYPTION ON;"
                : "ALTER DATABASE [<current-database>] SET ENCRYPTION OFF;");
        report.AddStep("Poll for the final encryption state", targetEncrypted ? "Poll sys.dm_database_encryption_keys until encryption_state reaches 3 (encrypted)." : "Poll sys.dm_database_encryption_keys until the database reports state 1 or no row remains.", "SELECT TOP (1) encryption_state FROM sys.dm_database_encryption_keys WHERE database_id = DB_ID();");
        report.Warnings.Add("SQL Server TDE transitions do not remove certificates or keys automatically. Retain all TDE certificates and private keys for backup and restore compatibility.");

        if (!options.Execute)
        {
            report.Summary = targetEncrypted ? "SQL Server enable transition plan generated." : "SQL Server disable transition plan generated.";
            return;
        }

        await using var dbContext = CreateDbContext(runtimeOptions.ProviderOptions);
        await using var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var databaseName = await ExecuteScalarAsync<string>(connection, "SELECT DB_NAME();", cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unable to resolve the current SQL Server database name.");
        await ExecuteNonQueryAsync(connection, $"BACKUP DATABASE {QuoteSqlServerIdentifier(databaseName)} TO DISK = N'{EscapeSqlLiteral(options.BackupPath!)}' WITH COPY_ONLY, INIT, CHECKSUM;", cancellationToken).ConfigureAwait(false);

        if (targetEncrypted)
        {
            var certificateName = runtimeOptions.EncryptionOptions.SqlServerCertificateName;
            if (string.IsNullOrWhiteSpace(certificateName))
            {
                throw new InvalidOperationException("SQL Server enable transitions require Mcp:Database:Encryption:SqlServer:CertificateName or MCP_SQLSERVER_TDE_CERTIFICATE.");
            }

            var certificateExists = await ExecuteScalarAsync<int>(connection, $"SELECT COUNT(*) FROM master.sys.certificates WHERE name = N'{EscapeSqlLiteral(certificateName)}';", cancellationToken).ConfigureAwait(false);
            if (certificateExists <= 0)
            {
                throw new InvalidOperationException($"SQL Server certificate '{certificateName}' was not found in master.sys.certificates. Provision the certificate before enabling TDE.");
            }

            var dekExists = await ExecuteScalarAsync<int>(connection, "SELECT COUNT(*) FROM sys.dm_database_encryption_keys WHERE database_id = DB_ID();", cancellationToken).ConfigureAwait(false) > 0;
            if (!dekExists)
            {
                await ExecuteNonQueryAsync(connection, $"CREATE DATABASE ENCRYPTION KEY WITH ALGORITHM = AES_256 ENCRYPTION BY SERVER CERTIFICATE {QuoteSqlServerIdentifier(certificateName)};", cancellationToken).ConfigureAwait(false);
            }

            await ExecuteNonQueryAsync(connection, $"ALTER DATABASE {QuoteSqlServerIdentifier(databaseName)} SET ENCRYPTION ON;", cancellationToken).ConfigureAwait(false);
            await WaitForSqlServerEncryptionStateAsync(connection, static state => state == 3, options.SqlServerTimeout, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await ExecuteNonQueryAsync(connection, $"ALTER DATABASE {QuoteSqlServerIdentifier(databaseName)} SET ENCRYPTION OFF;", cancellationToken).ConfigureAwait(false);
            await WaitForSqlServerEncryptionStateAsync(connection, static state => state is null or 1, options.SqlServerTimeout, cancellationToken).ConfigureAwait(false);
        }

        await ValidateAgainstTargetStateAsync(runtimeOptions, targetEncrypted, cancellationToken).ConfigureAwait(false);
        SetAllStepStatuses(report, "completed");
        report.Summary = targetEncrypted
            ? "SQL Server TDE was enabled successfully. Keep the database backup and retain all certificates and keys until the encrypted configuration is stable."
            : "SQL Server TDE was disabled successfully. Keep the database backup and retain the historical TDE certificates and keys for backup-restore compatibility.";
    }

    private static async Task ValidateAgainstTargetStateAsync(
        McpDatabaseRuntimeOptions runtimeOptions,
        bool targetEncrypted,
        CancellationToken cancellationToken)
    {
        var validationOptions = new McpDatabaseRuntimeOptions(
            runtimeOptions.ProviderOptions,
            new McpDatabaseEncryptionOptions(
                targetEncrypted,
                runtimeOptions.EncryptionOptions.SqliteKey,
                runtimeOptions.EncryptionOptions.SqliteSeeToolPath,
                runtimeOptions.EncryptionOptions.PostgreSqlKeyProvider,
                runtimeOptions.EncryptionOptions.PostgreSqlPrincipalKey,
                runtimeOptions.EncryptionOptions.SqlServerCertificateName,
                runtimeOptions.EncryptionOptions.SqlServerDatabaseEncryptionKeyName));

        await using var dbContext = CreateDbContext(runtimeOptions.ProviderOptions);
        await McpDatabaseEncryptionCoordinator.ValidateAsync(dbContext, validationOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> RunSqliteShellAsync(
        string toolPath,
        string databasePath,
        string? currentKey,
        string commandText,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string> { "-batch" };
        if (!string.IsNullOrWhiteSpace(currentKey))
        {
            arguments.Add("-textkey");
            arguments.Add(currentKey);
        }

        arguments.Add("--alg");
        arguments.Add(SqliteAlgorithm);
        arguments.Add(databasePath);

        return await RunProcessAsync(toolPath, arguments, commandText, cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureSqliteVerificationPassed(string output, bool targetEncrypted)
    {
        if (!output.Contains("ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"SQLite verification did not report integrity_check = ok. Output:{Environment.NewLine}{output}");
        }

        if (!targetEncrypted)
        {
            return;
        }

        var reservedBytesLine = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(line => line.Contains("reserved bytes", StringComparison.OrdinalIgnoreCase));
        if (reservedBytesLine is null)
        {
            throw new InvalidOperationException(
                $"SQLite verification did not include .dbinfo reserved-bytes output. Output:{Environment.NewLine}{output}");
        }

        var digits = new string(reservedBytesLine.Where(char.IsDigit).ToArray());
        if (!int.TryParse(digits, out var reservedBytes) || reservedBytes <= 0)
        {
            throw new InvalidOperationException(
                $"SQLite verification reported zero reserved bytes after encryption. Output:{Environment.NewLine}{output}");
        }
    }

    private static async Task<string> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? standardInput,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardInput = standardInput is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process '{fileName}'.");
        if (standardInput is not null)
        {
            await process.StandardInput.WriteAsync(standardInput.AsMemory(), cancellationToken).ConfigureAwait(false);
            process.StandardInput.Close();
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Process '{fileName}' failed with exit code {process.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}");
        }

        return stdout + Environment.NewLine + stderr;
    }

    private static async Task<IReadOnlyList<(string Schema, string Table, bool IsEncrypted)>> ReadPostgreSqlTableStatesAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT schemaname, tablename,
                   pg_tde_is_encrypted(format('%I.%I', schemaname, tablename)::regclass) AS is_encrypted
            FROM pg_tables
            WHERE schemaname = 'public'
              AND tablename <> '__EFMigrationsHistory'
            ORDER BY tablename;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var tables = new List<(string Schema, string Table, bool IsEncrypted)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            tables.Add((reader.GetString(0), reader.GetString(1), reader.GetBoolean(2)));
        }

        return tables;
    }

    private static async Task ExecuteNonQueryAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<T?> ExecuteScalarAsync<T>(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (result is null or DBNull)
        {
            return default;
        }

        return (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task WaitForSqlServerEncryptionStateAsync(
        DbConnection connection,
        Func<int?, bool> success,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var state = await ExecuteScalarAsync<int?>(
                connection,
                """
                SELECT TOP (1) encryption_state
                FROM sys.dm_database_encryption_keys
                WHERE database_id = DB_ID();
                """,
                cancellationToken).ConfigureAwait(false);
            if (success(state))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("Timed out waiting for SQL Server to reach the requested TDE state.");
    }

    private static McpDbContext CreateDbContext(McpDatabaseProviderOptions providerOptions)
    {
        var services = new ServiceCollection();
        services.AddDbContext<McpDbContext>(dbOptions =>
        {
            McpDatabaseProviderFactory.Configure(dbOptions, providerOptions);
        }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);

        return services.BuildServiceProvider(validateScopes: true).GetRequiredService<McpDbContext>();
    }

    private static string ResolveSqliteDatabasePath(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        if (!builder.TryGetValue("Data Source", out var value) || value is not string rawPath || string.IsNullOrWhiteSpace(rawPath))
        {
            throw new InvalidOperationException($"The SQLite connection string '{connectionString}' does not contain a usable Data Source value.");
        }

        return Path.GetFullPath(rawPath);
    }

    private static string QuotePostgreSqlIdentifier(string identifier)
        => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string QuoteSqlServerIdentifier(string identifier)
        => "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";

    private static string EscapeSqlLiteral(string value)
        => value.Replace("'", "''", StringComparison.Ordinal);

    private static string EscapeSqliteShellText(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\"\"", StringComparison.Ordinal);

    private static void SetAllStepStatuses(McpDatabaseEncryptionTransitionReport report, string status)
    {
        foreach (var step in report.Steps)
        {
            step.Status = status;
        }
    }
}
