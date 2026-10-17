using System.Data.Common;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Npgsql;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Database;

namespace QBrainAi.Support.Mcp.DatabaseMaintenance;

/// <summary>TR-MCP-PATHNORM-001: offline maintenance dispatch before HTTP or STDIO host construction.</summary>
internal static class WorkspacePathRekeyCommand
{
    internal sealed record Options(bool DryRun, string? CopyDatabase, string? BackupPath, string? ReportPath, string? Instance);
    private const string CommandName = "--workspace-path-rekey";
    internal const string Usage = "--workspace-path-rekey [--instance <name>] [--dry-run --copy-database <copy>] [--backup-path <new-backup>] [--report-path <new-report>]";

    /// <summary>Parses only known maintenance options and refuses unsafe defaults.</summary>
    public static bool TryParse(string[] args, out Options? options, out string? error)
    {
        options = null; error = null;
        if (!args.Contains(CommandName, StringComparer.OrdinalIgnoreCase)) return false;
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var dryRun = false;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals(CommandName, StringComparison.OrdinalIgnoreCase) || arg == "--apply") continue;
            if (arg == "--dry-run") { dryRun = true; continue; }
            if (arg is not ("--copy-database" or "--backup-path" or "--report-path" or "--instance")
                || i + 1 == args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal)
                || !values.TryAdd(arg, args[++i]))
            {
                error = "Invalid or duplicate rekey argument. " + Usage;
                return true;
            }
        }
        if (dryRun && !values.ContainsKey("--copy-database"))
            error = "Dry-run requires --copy-database; it never connects to the configured live database.";
        if (!dryRun && !values.ContainsKey("--backup-path"))
            error = "Apply requires --backup-path and an offline database.";
        if (error is not null) return true;
        options = new(dryRun, values.GetValueOrDefault("--copy-database"), values.GetValueOrDefault("--backup-path"),
            values.GetValueOrDefault("--report-path"), values.GetValueOrDefault("--instance")
                ?? Environment.GetEnvironmentVariable("MCP_INSTANCE"));
        return true;
    }

    /// <summary>Selects a distinct copy before constructing or opening any database connection.</summary>
    public static string SelectCopyConnectionString(string provider, string configured, string copyDatabase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(copyDatabase);
        switch (McpDatabaseProviderFactory.ResolveStrategy(provider).Kind)
        {
            case McpDatabaseProviderKind.Sqlite:
                var sqlite = new SqliteConnectionStringBuilder(configured);
                var source = Path.GetFullPath(sqlite.DataSource);
                var copy = Path.GetFullPath(copyDatabase);
                if (string.Equals(source, copy, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    throw new InvalidOperationException("The database copy must differ from the configured database.");
                if (!File.Exists(copy)) throw new InvalidOperationException("The database copy must already exist.");
                for (FileSystemInfo? item = new FileInfo(copy); item is not null;
                    item = item is FileInfo file ? file.Directory : ((DirectoryInfo)item).Parent)
                    if ((item.Attributes & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidOperationException("Database copy must not use symbolic links or reparse points.");
                sqlite.DataSource = copy; sqlite.Mode = SqliteOpenMode.ReadOnly; sqlite.Pooling = false;
                return sqlite.ConnectionString;
            case McpDatabaseProviderKind.SqlServer:
                var sql = new SqlConnectionStringBuilder(configured);
                if (string.Equals(sql.InitialCatalog, copyDatabase, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The copy catalog must differ from the configured database.");
                sql.InitialCatalog = copyDatabase; sql.Pooling = false;
                return sql.ConnectionString;
            case McpDatabaseProviderKind.PostgreSql:
                var pg = new NpgsqlConnectionStringBuilder(configured);
                if (string.Equals(pg.Database, copyDatabase, StringComparison.Ordinal))
                    throw new InvalidOperationException("The copy catalog must differ from the configured database.");
                pg.Database = copyDatabase; pg.Pooling = false;
                pg.Options = string.IsNullOrWhiteSpace(pg.Options) ? "-c default_transaction_read_only=on"
                    : pg.Options + " -c default_transaction_read_only=on";
                return pg.ConnectionString;
            default:
                throw new NotSupportedException("Unsupported rekey database provider.");
        }
    }

    /// <summary>Selects an explicit rehearsal copy for apply, retaining provider options without dry-run read-only flags.</summary>
    public static string SelectApplyCopyConnectionString(string provider, string configured, string copyDatabase)
    {
        var selected = SelectCopyConnectionString(provider, configured, copyDatabase);
        return McpDatabaseProviderFactory.ResolveStrategy(provider).Kind switch
        {
            McpDatabaseProviderKind.Sqlite => new SqliteConnectionStringBuilder(selected) { Mode = SqliteOpenMode.ReadWrite }.ConnectionString,
            McpDatabaseProviderKind.PostgreSql => new NpgsqlConnectionStringBuilder(selected)
            {
                Options = new NpgsqlConnectionStringBuilder(configured).Options
            }.ConnectionString,
            _ => selected
        };
    }

    /// <summary>Loads configuration without constructing a host, then operates only on the selected database.</summary>
    public static async Task<int> RunAsync(Options options, CancellationToken cancellationToken)
    {
        var databaseCommitted = false;
        try
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            var builder = Host.CreateApplicationBuilder([]);
            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                .AddYamlFile("appsettings.yaml", optional: true, reloadOnChange: false)
                .AddYamlFile($"appsettings.{environment}.yaml", optional: true, reloadOnChange: false)
                .AddYamlFile(Path.Combine("src", "QBrainAi.Support.Mcp", "appsettings.yaml"), optional: true, reloadOnChange: false)
                .AddYamlFile(Path.Combine("src", "QBrainAi.Support.Mcp", $"appsettings.{environment}.yaml"), optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
            McpInstanceResolver.ProjectCanonicalSectionOverLegacy(builder.Configuration);
            var runtime = McpDatabaseConfigurationResolver.ResolveRuntimeOptions(builder.Configuration, options.Instance);
            var provider = runtime.ProviderOptions;
            if (provider.ProviderKind == McpDatabaseProviderKind.Sqlite && runtime.EncryptionOptions.Enabled)
                throw new NotSupportedException("Encrypted SQLite requires the native SEE runtime before rekeying.");
            var connectionString = provider.ConnectionString;
            if (options.CopyDatabase is not null)
            {
                connectionString = options.DryRun
                    ? SelectCopyConnectionString(provider.ProviderName, connectionString, options.CopyDatabase)
                    : SelectApplyCopyConnectionString(provider.ProviderName, connectionString, options.CopyDatabase);
            }
            else if (options.DryRun)
                throw new InvalidOperationException("Dry-run requires an explicit copy.");
            var selected = new McpDatabaseProviderOptions(provider.ProviderKind, provider.ProviderName,
                connectionString, provider.MigrationsAssembly);
            var dbOptions = new DbContextOptionsBuilder<McpDbContext>();
            McpDatabaseProviderFactory.Configure(dbOptions, selected);
            await using var db = new McpDbContext(dbOptions.Options);
            var tokenPath = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, options.Instance, "GitHub:TokenStorePath");
            tokenPath = Path.GetFullPath(Path.IsPathRooted(tokenPath ?? "") ? tokenPath!
                : Path.Combine(AppContext.BaseDirectory, tokenPath ?? "mcp-data/github-token-store.json"));
            // A database-copy rehearsal never changes the configured token file.
            var includeTokens = !options.DryRun && options.CopyDatabase is null;
            if (includeTokens) await WorkspacePathRekeyTokenStore.RunAsync(tokenPath, true, cancellationToken).ConfigureAwait(false);
            var report = await WorkspacePathRekey.RunAsync(db.Database.GetDbConnection(),
                options.DryRun, options.BackupPath, cancellationToken).ConfigureAwait(false);
            databaseCommitted = !options.DryRun;
            if (includeTokens) await WorkspacePathRekeyTokenStore.RunAsync(tokenPath, false, cancellationToken).ConfigureAwait(false);
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(json);
            if (options.ReportPath is not null)
            {
                await using var file = new FileStream(Path.GetFullPath(options.ReportPath), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await JsonSerializer.SerializeAsync(file, report, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            return 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Provider exceptions may contain connection strings, row payloads, or credentials.
            Console.Error.WriteLine(databaseCommitted
                ? "Database rekey committed, but token/report finalization failed. Retain backups; resolve the file failure and retry the idempotent command."
                : "Workspace rekey failed; no successful apply is reported. Check copy selection, convergence, schema and backup permissions. " + Usage);
            Console.Error.WriteLine("Failure type: " + ex.GetType().Name);
            return 1;
        }
    }
}
