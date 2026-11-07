using QBrainAi.Support.Mcp.DatabaseMaintenance;
using QBrainAi.Support.Mcp.Storage.Database;
using QBrainAi.Support.Mcp.Tests.TestSupport;

namespace QBrainAi.Support.Mcp.Tests.DatabaseMaintenance;

/// <summary>
/// Tests argument parsing and dry-run transition planning for the database-encryption maintenance command.
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-077, TR-MCP-SEC-004, TR-MCP-CFG-007.
/// Test data uses deterministic provider connection strings, placeholder keys, and dry-run command arguments so
/// the maintenance workflow can be validated without requiring live SEE, pg_tde, or SQL Server TDE infrastructure.
/// The class runs in <see cref="ProcessEnvironmentCollection"/> because one of its integration tests writes the process
/// environment (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
/// <para>2026-11-07, FR-MCP-077; TR-MCP-SEC-004; TR-MCP-CFG-007; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (TryParse_UsesEnvironmentInstance_WhenCliInstanceIsOmitted) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</para>
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class McpDatabaseEncryptionTransitionCommandTests
{
    /// <summary>
    /// Verifies that the maintenance command parser extracts operation mode, provider-independent options, and passthrough configuration arguments correctly.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-077, TR-MCP-CFG-007.
    /// Test data: explicit enable command, instance name, report path, and a leftover configuration override argument.
    /// This data is used to prove that the command parser can separate maintenance-only switches from configuration overrides safely.
    /// </remarks>
    [Fact]
    public void TryParse_EnableCommand_ReturnsExpectedOptions()
    {
        var parsed = McpDatabaseEncryptionTransitionCommand.TryParse(
            [
                "--database-encryption-transition",
                "enable",
                "--execute",
                "--instance",
                "alt-local",
                "--backup-path",
                "E:\\backup\\mcp.bak",
                "--report-path=E:\\backup\\transition.json",
                "--Mcp:Database:Provider=sqlserver",
            ],
            out var options,
            out var error);

        Assert.True(parsed);
        Assert.Null(error);
        Assert.NotNull(options);
        Assert.Equal(McpDatabaseEncryptionTransitionOperation.Enable, options.Operation);
        Assert.True(options.Execute);
        Assert.Equal("alt-local", options.InstanceName);
        Assert.Equal("E:\\backup\\mcp.bak", options.BackupPath);
        Assert.Equal("E:\\backup\\transition.json", options.ReportPath);
        Assert.Single(options.ConfigurationArguments);
        Assert.Equal("--Mcp:Database:Provider=sqlserver", options.ConfigurationArguments[0]);
    }

    /// <summary>
    /// Verifies that missing option values fail parsing instead of being silently treated as configuration arguments.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-077, TR-MCP-CFG-007.
    /// Test data: an enable command with a missing backup-path value.
    /// This data is used to ensure the maintenance command reports invalid CLI usage clearly and prevents accidental execution against the wrong settings.
    /// </remarks>
    [Fact]
    public void TryParse_MissingOptionValue_ReturnsError()
    {
        var parsed = McpDatabaseEncryptionTransitionCommand.TryParse(
            [
                "--database-encryption-transition",
                "enable",
                "--backup-path",
                "--execute",
            ],
            out var options,
            out var error);

        Assert.True(parsed);
        Assert.Null(options);
        Assert.Equal("The --backup-path option requires a value.", error);
    }

    /// <summary>
    /// Verifies that a dry-run SQLite enable transition emits the SEE-specific backup, rekey, nonce, and verification procedure.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-077, TR-MCP-SEC-004.
    /// Test data: SQLite provider options with a deterministic file path, SEE CLI path, and target key.
    /// This data is used to validate the no-data-loss copy-and-verify workflow that will later be executed against a real SEE-enabled runtime.
    /// </remarks>
    [Fact]
    public async Task RunAsync_DryRunSqliteEnable_BuildsSeeWorkflow()
    {
        var runtimeOptions = CreateRuntimeOptions(
            "sqlite",
            "Data Source=E:\\data\\mcp.db",
            encryptionEnabled: true,
            sqliteKey: "new-secret",
            sqliteSeeToolPath: "E:\\tools\\sqlite3-see.exe");

        var report = await McpDatabaseEncryptionTransitionRunner.RunAsync(
            runtimeOptions,
            new McpDatabaseEncryptionTransitionOptions
            {
                Operation = McpDatabaseEncryptionTransitionOperation.Enable,
                Execute = false,
            },
            CancellationToken.None);

        Assert.Equal("SQLite enable transition plan generated.", report.Summary);
        Assert.Contains(report.Steps, step => step.Title.Contains("backup", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.Steps, step => step.CommandText?.Contains(".text-rekey", StringComparison.Ordinal) == true);
        Assert.Contains(report.Steps, step => step.CommandText?.Contains(".filectrl reserve_bytes 12", StringComparison.Ordinal) == true);
        Assert.Contains(report.Steps, step => step.CommandText?.Contains("PRAGMA integrity_check;", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(report.Steps, step => step.CommandText?.Contains("new-secret", StringComparison.Ordinal) == true);
        Assert.Contains(report.Steps, step => step.CommandText?.Contains("<target-key>", StringComparison.Ordinal) == true);
    }

    /// <summary>
    /// Verifies that a dry-run PostgreSQL disable transition emits pg_dump backup and table-rewrite guidance back to heap.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-077, TR-MCP-SEC-004.
    /// Test data: PostgreSQL provider options plus a deterministic pg_dump path.
    /// This data is used to confirm the transition plan captures both rollback material and pg_tde table rewrite steps without requiring a live PostgreSQL server.
    /// </remarks>
    [Fact]
    public async Task RunAsync_DryRunPostgreSqlDisable_BuildsRewritePlan()
    {
        var runtimeOptions = CreateRuntimeOptions(
            "postgresql",
            "Host=localhost;Database=mcp;Username=test;Password=test",
            encryptionEnabled: false,
            postgreSqlKeyProvider: "vault",
            postgreSqlPrincipalKey: "mcp-main");

        var report = await McpDatabaseEncryptionTransitionRunner.RunAsync(
            runtimeOptions,
            new McpDatabaseEncryptionTransitionOptions
            {
                Operation = McpDatabaseEncryptionTransitionOperation.Disable,
                Execute = false,
                PostgreSqlDumpToolPath = "E:\\pgsql\\pg_dump.exe",
            },
            CancellationToken.None);

        Assert.Equal("PostgreSQL disable transition plan generated.", report.Summary);
        Assert.Contains(report.Steps, step => step.CommandText?.Contains("pg_dump", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(report.Steps, step => step.CommandText?.Contains("SET ACCESS METHOD heap", StringComparison.Ordinal) == true);
        Assert.Contains(report.Steps, step => step.CommandText?.Contains("pg_tde_is_encrypted", StringComparison.Ordinal) == true);
    }

    /// <summary>
    /// Verifies that a dry-run SQL Server enable transition emits copy-only backup, certificate validation, and TDE enablement guidance.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-077, TR-MCP-SEC-004.
    /// Test data: SQL Server provider options with a deterministic certificate name and timeout.
    /// This data is used to validate the maintenance command’s TDE transition plan without requiring a non-LocalDB SQL Server target during unit testing.
    /// </remarks>
    [Fact]
    public async Task RunAsync_DryRunSqlServerEnable_BuildsTdePlan()
    {
        var runtimeOptions = CreateRuntimeOptions(
            "sqlserver",
            "Server=(localdb)\\MSSQLLocalDB;Database=McpTest;Integrated Security=true;TrustServerCertificate=true;",
            encryptionEnabled: true,
            sqlServerCertificateName: "QBrainAiTdeCert");

        var report = await McpDatabaseEncryptionTransitionRunner.RunAsync(
            runtimeOptions,
            new McpDatabaseEncryptionTransitionOptions
            {
                Operation = McpDatabaseEncryptionTransitionOperation.Enable,
                Execute = false,
                SqlServerTimeout = TimeSpan.FromSeconds(30),
            },
            CancellationToken.None);

        Assert.Equal("SQL Server enable transition plan generated.", report.Summary);
        Assert.Contains(report.Steps, step => step.CommandText?.Contains("BACKUP DATABASE", StringComparison.Ordinal) == true);
        Assert.Contains(report.Steps, step => step.CommandText?.Contains("master.sys.certificates", StringComparison.Ordinal) == true);
        Assert.Contains(report.Steps, step => step.CommandText?.Contains("CREATE DATABASE ENCRYPTION KEY", StringComparison.Ordinal) == true);
        Assert.Contains(report.Steps, step => step.CommandText?.Contains("SET ENCRYPTION ON", StringComparison.Ordinal) == true);
        Assert.Contains(report.Warnings, warning => warning.Contains("Retain all TDE certificates", StringComparison.Ordinal));
    }

    private static McpDatabaseRuntimeOptions CreateRuntimeOptions(
        string providerName,
        string connectionString,
        bool encryptionEnabled,
        string? sqliteKey = null,
        string? sqliteSeeToolPath = null,
        string? postgreSqlKeyProvider = null,
        string? postgreSqlPrincipalKey = null,
        string? sqlServerCertificateName = null)
        => new(
            McpDatabaseProviderFactory.CreateOptions(providerName, connectionString, migrationsAssembly: null),
            new McpDatabaseEncryptionOptions(
                encryptionEnabled,
                sqliteKey,
                sqliteSeeToolPath,
                postgreSqlKeyProvider,
                postgreSqlPrincipalKey,
                sqlServerCertificateName,
                sqlServerDatabaseEncryptionKeyName: null));
}
