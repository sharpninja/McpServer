using System.Diagnostics;
using System.Globalization;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Database;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace QBrainAi.Support.Mcp.IntegrationTests;

/// <summary>
/// TEST-MCP-101, TEST-MCP-102: Shared test-only helpers for provider-backed clean-database integration tests.
/// The helpers create isolated workspace trees, build provider-specific configuration overrides, and verify
/// that each provider can apply migrations and persist a simple EF Core entity on a fresh database.
/// </summary>
internal static class ProviderIntegrationTestSupport
{
    /// <summary>
    /// Creates a new isolated workspace tree that contains the minimal files needed for the hosted app to start
    /// without touching the repository working tree.
    /// </summary>
    /// <returns>An isolated workspace sandbox that deletes itself when disposed.</returns>
    internal static ProviderWorkspaceSandbox CreateWorkspace() => new();

    /// <summary>
    /// Creates a provider-configured <see cref="CustomWebApplicationFactory"/> using the supplied overrides.
    /// </summary>
    /// <param name="workspace">Workspace sandbox providing repo and data roots.</param>
    /// <param name="providerOverrides">Additional configuration overrides for the selected provider.</param>
    /// <returns>A hosted test application factory.</returns>
    internal static WebApplicationFactory<McpApiEntryPoint> CreateFactory(
        ProviderWorkspaceSandbox workspace,
        IReadOnlyDictionary<string, string?> providerOverrides)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(providerOverrides);

        var configuration = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["DataFolder"] = workspace.RootPath,
            ["Mcp:RepoRoot"] = workspace.RootPath,
            ["Mcp:TodoFilePath"] = "docs/Project/TODO.yaml",
            ["Mcp:TodoStorage:Provider"] = "sqlite",
            ["Mcp:TodoStorage:SqliteDataSource"] = "mcp.db",
            ["Mcp:UseInMemoryDatabaseForTests"] = "false",
        };

        foreach (var pair in providerOverrides)
            configuration[pair.Key] = pair.Value;

        var providerOptions = ResolveProviderOptions(configuration);
        var internalServiceProvider = BuildProviderInternalServiceProvider(providerOptions.ProviderKind);
        return new CustomWebApplicationFactory(
            services =>
            {
                services.RemoveAll<McpDbContext>();
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<DbContextOptions<McpDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<McpDbContext>>();
                services.RemoveAll<McpDatabaseProviderOptions>();
                services.RemoveAll<McpDatabaseRuntimeOptions>();
                services.AddSingleton(providerOptions);
                services.AddSingleton(new McpDatabaseRuntimeOptions(
                    providerOptions,
                    new McpDatabaseEncryptionOptions(
                        enabled: false,
                        sqliteKey: null,
                        sqliteSeeToolPath: null,
                        postgreSqlKeyProvider: null,
                        postgreSqlPrincipalKey: null,
                        sqlServerCertificateName: null,
                        sqlServerDatabaseEncryptionKeyName: null)));
                services.AddDbContext<McpDbContext>(options =>
                {
                    options.UseInternalServiceProvider(internalServiceProvider);
                    McpDatabaseProviderFactory.Configure(options, providerOptions);
                }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
            },
            configuration,
            configureDefaultTestDatabase: false).WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(configuration);
            });
        });
    }

    /// <summary>
    /// Applies migrations and verifies a simple persistence round-trip against the configured provider.
    /// </summary>
    /// <param name="factory">Hosted application factory.</param>
    /// <param name="expectedProviderName">Provider name substring expected in the EF Core provider name.</param>
    /// <param name="workspaceId">Workspace identifier to stamp on the added entity. Use an empty string to keep the entity globally visible to the default test context.</param>
    /// <returns>A task that completes when the round-trip succeeds.</returns>
    internal static async Task AssertDatabaseRoundTripAsync(
        WebApplicationFactory<McpApiEntryPoint> factory,
        string expectedProviderName,
        string workspaceId)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedProviderName);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<McpDbContext>();

        Assert.Contains(expectedProviderName, context.Database.ProviderName ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(3));
        await context.Database.MigrateAsync().ConfigureAwait(false);

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync().ConfigureAwait(false);
        Assert.NotEmpty(appliedMigrations);

        var entityId = $"agent-{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;
        context.AgentDefinitions.Add(new AgentDefinitionEntity
        {
            Id = entityId,
            WorkspaceId = workspaceId,
            DisplayName = "Provider integration test agent",
            DefaultLaunchCommand = "dotnet",
            DefaultInstructionFile = "AGENTS.md",
            Models = { new AgentDefinitionModelEntity { Ordinal = 0, Model = "gpt-5-codex" } },
            DefaultBranchStrategy = "feature/{agent}/{task}",
            DefaultSeedPrompt = "Test seed prompt",
            IsBuiltIn = false,
            CreatedAt = now,
            ModifiedAt = now,
        });

        await context.SaveChangesAsync().ConfigureAwait(false);
        context.ChangeTracker.Clear();

        var loaded = await context.AgentDefinitions.SingleAsync(x => x.Id == entityId).ConfigureAwait(false);
        Assert.Equal("Provider integration test agent", loaded.DisplayName);
        Assert.Equal("dotnet", loaded.DefaultLaunchCommand);
        Assert.Equal(workspaceId, loaded.WorkspaceId);
    }

    /// <summary>
    /// Asserts that AddHandoffIngestionStorage is applied and HandoffIngestionRuns/HandoffDiagnostics
    /// can round-trip on the hosted provider database.
    /// </summary>
    internal static async Task AssertHandoffIngestionStorageAsync(WebApplicationFactory<McpApiEntryPoint> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var applied = await context.Database.GetAppliedMigrationsAsync().ConfigureAwait(false);
        Assert.Contains(applied, name => name.Contains("AddHandoffIngestionStorage", StringComparison.Ordinal));

        var workspaceId = string.IsNullOrWhiteSpace(context.CurrentWorkspaceId)
            ? context.Workspaces.Select(item => item.WorkspaceId).FirstOrDefault() ?? "handoff-provider-test"
            : context.CurrentWorkspaceId;
        context.OverrideWorkspaceId(workspaceId);
        if (!await context.Workspaces.AnyAsync(item => item.WorkspaceId == workspaceId).ConfigureAwait(false))
        {
            context.Workspaces.Add(new WorkspaceEntity
            {
                WorkspaceId = workspaceId,
                WorkspacePath = workspaceId,
                Name = "handoff-provider-test",
                TodoPath = "docs/todo.yaml",
                IsEnabled = true,
            });
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        var runId = $"handoff-run-{Guid.NewGuid():N}";
        context.HandoffIngestionRuns.Add(new HandoffIngestionRunEntity
        {
            RunId = runId,
            WorkspaceId = workspaceId,
            SourceKind = "Content",
            SourceLocator = "content",
            ContentSha256 = new string('a', 64),
            ExtractedAtUtc = DateTimeOffset.UtcNow,
            PromptVersion = "handoff-todo-draft/v1",
            Mode = "DraftOnly",
            ReviewState = "None",
            ReplayIdentity = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64],
            ProcessingState = "Terminal",
            Succeeded = true,
        });
        context.HandoffDiagnostics.Add(new HandoffDiagnosticEntity
        {
            WorkspaceId = workspaceId,
            RunId = runId,
            Code = "provider_roundtrip",
            Severity = "Info",
            Message = "round-trip",
            Ordinal = 0,
        });
        await context.SaveChangesAsync().ConfigureAwait(false);
        context.ChangeTracker.Clear();

        var loadedRun = await context.HandoffIngestionRuns.IgnoreQueryFilters().SingleAsync(item => item.RunId == runId).ConfigureAwait(false);
        var loadedDiagnostic = await context.HandoffDiagnostics.IgnoreQueryFilters().SingleAsync(item => item.RunId == runId).ConfigureAwait(false);
        Assert.Equal("Content", loadedRun.SourceKind);
        Assert.Equal("provider_roundtrip", loadedDiagnostic.Code);
    }

    private static McpDatabaseProviderOptions ResolveProviderOptions(IReadOnlyDictionary<string, string?> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var providerName = GetRequiredValue(configuration, "Mcp:DatabaseProvider");
        var migrationsAssembly = configuration.TryGetValue("Mcp:DatabaseMigrationsAssembly", out var configuredAssembly)
            ? configuredAssembly
            : null;

        var strategy = McpDatabaseProviderFactory.ResolveStrategy(providerName);
        var connectionString = strategy.Kind switch
        {
            McpDatabaseProviderKind.Sqlite => NormalizeSqliteConnectionString(GetRequiredValue(configuration, "Mcp:DataSource")),
            McpDatabaseProviderKind.PostgreSql => GetRequiredValue(configuration, "Mcp:PostgresConnectionString"),
            McpDatabaseProviderKind.SqlServer => GetRequiredValue(configuration, "Mcp:SqlServerConnectionString"),
            _ => throw new InvalidOperationException($"Unsupported provider '{providerName}' in provider integration test configuration."),
        };

        return McpDatabaseProviderFactory.CreateOptions(providerName, connectionString, migrationsAssembly);
    }

    private static string GetRequiredValue(IReadOnlyDictionary<string, string?> configuration, string key)
    {
        if (configuration.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            return value;

        throw new InvalidOperationException($"Provider integration test configuration key '{key}' is required.");
    }

    private static string NormalizeSqliteConnectionString(string dataSource)
        => dataSource.Contains('=', StringComparison.Ordinal)
            ? dataSource
            : $"Data Source={dataSource}";

    private static IServiceProvider BuildProviderInternalServiceProvider(McpDatabaseProviderKind providerKind)
    {
        var services = new ServiceCollection();

        switch (providerKind)
        {
            case McpDatabaseProviderKind.Sqlite:
                services.AddEntityFrameworkSqlite();
                break;
            case McpDatabaseProviderKind.PostgreSql:
                services.AddEntityFrameworkNpgsql();
                break;
            case McpDatabaseProviderKind.SqlServer:
                services.AddEntityFrameworkSqlServer();
                break;
            default:
                throw new InvalidOperationException($"Unsupported provider kind '{providerKind}' in provider integration test services.");
        }

        return services.BuildServiceProvider(validateScopes: true);
    }
}

/// <summary>
/// Represents an isolated workspace tree used by provider integration tests.
/// </summary>
internal sealed class ProviderWorkspaceSandbox : IAsyncDisposable
{
    private readonly string _rootPath;

    /// <summary>Initializes a new isolated workspace tree.</summary>
    public ProviderWorkspaceSandbox()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), $"mcp-provider-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_rootPath, "docs", "Project"));
        File.WriteAllText(Path.Combine(_rootPath, "docs", "Project", "TODO.yaml"), """
            mvp-app:
              high-priority: []
            mvp-support:
              high-priority: []
            """);
    }

    /// <summary>Gets the isolated workspace root path.</summary>
    public string RootPath => _rootPath;

    /// <summary>
    /// Returns a SQLite database path rooted in the isolated workspace tree.
    /// </summary>
    /// <param name="fileName">SQLite file name to create.</param>
    /// <returns>An absolute file path under the sandbox root.</returns>
    public string GetDatabasePath(string fileName) => Path.Combine(_rootPath, fileName);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        try
        {
            Directory.Delete(_rootPath, recursive: true);
        }
        catch
        {
            // Best-effort cleanup only.
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>TEST-MCP-102 AC-001 (PLAN-SESSIONTRIAGEFIX-001 B11, triage-report-ead2fdaa): where a SQL Server integration case gets its server.</summary>
internal enum SqlServerSandboxKind
{
    /// <summary>No server: the case has nothing to run against.</summary>
    None,

    /// <summary>A private SQL Server LocalDB instance the harness creates and deletes.</summary>
    LocalDb,

    /// <summary>A temporary SQL Server 2025 Developer container the harness starts for the case and removes afterwards.</summary>
    TemporaryInstance,

    /// <summary>An already running server other cases share, such as the endpoint in MCP_TEST_SQLSERVER_CONNECTION.</summary>
    Shared,

    /// <summary>The case is skipped.</summary>
    Skip,
}

/// <summary>
/// TEST-MCP-102 AC-001 (PLAN-SESSIONTRIAGEFIX-001 B11, triage-report-ead2fdaa): the server a SQL Server integration case uses and, for a
/// temporary instance, how it is started (operator decisions 2026-10-15: "allow a temporary sql server 2025 dev instance when necessary." and
/// "Yes, accept for tests (Recommended)": ACCEPT_EULA=Y, MSSQL_PID=Developer, the port on 127.0.0.1, the image never pulled, removed afterwards).
/// </summary>
/// <param name="Kind">Where the server comes from.</param>
/// <param name="Image">Container image of a temporary instance.</param>
/// <param name="ProductId">MSSQL_PID of a temporary instance.</param>
/// <param name="AcceptEula">Whether a temporary instance starts with ACCEPT_EULA=Y.</param>
/// <param name="HostAddress">Host address a temporary instance publishes its port on.</param>
/// <param name="PullImage">Whether starting a temporary instance may pull its image.</param>
/// <param name="RemovedAfterCase">Whether the harness removes the server when the case ends.</param>
internal sealed record SqlServerSandboxPlan(
    SqlServerSandboxKind Kind,
    string? Image,
    string? ProductId,
    bool AcceptEula,
    string? HostAddress,
    bool PullImage,
    bool RemovedAfterCase)
{
    /// <summary>
    /// TEST-MCP-102 AC-001: chooses the server for this machine: a private LocalDB instance where SqlLocalDB is installed, otherwise a temporary
    /// SQL Server 2025 Developer instance started with the license accepted, published on 127.0.0.1 and never pulled; either is removed when
    /// the case ends.
    /// </summary>
    /// <param name="localDbInstalled">Whether SqlLocalDB.exe is installed (only Windows has it).</param>
    /// <returns>The plan the harness starts.</returns>
    internal static SqlServerSandboxPlan Choose(bool localDbInstalled)
        => localDbInstalled
            ? new(SqlServerSandboxKind.LocalDb, null, null, false, null, false, true)
            : new(SqlServerSandboxKind.TemporaryInstance, "mcr.microsoft.com/mssql/server:2025-latest", "Developer", true, "127.0.0.1", false, true);

    /// <summary>
    /// TEST-MCP-102 AC-001: asserts a plan the requirement allows: a private LocalDB instance, or a temporary SQL Server 2025 Developer
    /// instance started with the license accepted, published on 127.0.0.1 and never pulled; either is removed when the case ends. A shared
    /// server, no server and a skip fail.
    /// </summary>
    /// <param name="plan">The plan a SQL Server case is about to use.</param>
    internal static void AssertAllowed(SqlServerSandboxPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Assert.True(
            plan.Kind is SqlServerSandboxKind.LocalDb or SqlServerSandboxKind.TemporaryInstance,
            $"SQL Server sandbox kind {plan.Kind} is not allowed: TEST-MCP-102 requires LocalDB or a temporary SQL Server 2025 Developer instance.");
        Assert.True(plan.RemovedAfterCase, "The SQL Server sandbox must be removed when the case ends (TEST-MCP-102).");
        if (plan.Kind != SqlServerSandboxKind.TemporaryInstance)
            return;

        Assert.StartsWith("mcr.microsoft.com/mssql/server:2025", plan.Image ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal("Developer", plan.ProductId);
        Assert.True(plan.AcceptEula, "The temporary SQL Server instance starts with ACCEPT_EULA=Y (operator decision 2026-10-15).");
        Assert.Equal("127.0.0.1", plan.HostAddress);
        Assert.False(plan.PullImage, "The temporary SQL Server instance never pulls its image (operator decision 2026-10-15).");
    }
}

/// <summary>
/// TEST-MCP-102 AC-001 (PLAN-SESSIONTRIAGEFIX-001 B11, triage-report-ead2fdaa): the private SQL Server a SQL Server integration case runs
/// against. <see cref="CreateAsync"/> chooses the plan for this machine and asserts it allowed before starting it: a LocalDB instance where
/// SqlLocalDB is installed, otherwise a temporary SQL Server 2025 Developer container. The databases a case names through
/// <see cref="DatabaseConnectionString"/> are dropped and the server is removed when the case ends. Cases run one at a time, so at most one
/// temporary instance exists.
/// </summary>
internal sealed class SqlServerSandbox : IAsyncDisposable
{
    private static readonly SemaphoreSlim OneCaseAtATime = new(1, 1);
    private readonly List<string> _databases = [];
    private readonly SqlLocalDbSandbox? _localDb;
    private readonly string? _containerName;
    private bool _disposed;

    private SqlServerSandbox(SqlServerSandboxPlan plan, string connectionString, SqlLocalDbSandbox? localDb, string? containerName)
    {
        Plan = plan;
        ConnectionString = connectionString;
        _localDb = localDb;
        _containerName = containerName;
    }

    /// <summary>Gets the plan this sandbox started.</summary>
    public SqlServerSandboxPlan Plan { get; }

    /// <summary>Gets the server-level connection string; it ends with a separator so a database can follow.</summary>
    public string ConnectionString { get; }

    /// <summary>Chooses the server for this machine, asserts the plan allowed (TEST-MCP-102 AC-001), and starts it.</summary>
    /// <returns>The started sandbox; dispose it to drop its databases and remove the server.</returns>
    public static async Task<SqlServerSandbox> CreateAsync()
    {
        var plan = SqlServerSandboxPlan.Choose(IsLocalDbInstalled());
        SqlServerSandboxPlan.AssertAllowed(plan);
        await OneCaseAtATime.WaitAsync().ConfigureAwait(false);
        try
        {
            if (plan.Kind == SqlServerSandboxKind.LocalDb)
            {
                var localDb = await SqlLocalDbSandbox.CreateAsync().ConfigureAwait(false);
                return new SqlServerSandbox(plan, localDb.ConnectionString, localDb, null);
            }

            var (name, connectionString) = await SqlServerContainer.StartAsync(plan).ConfigureAwait(false);
            return new SqlServerSandbox(plan, connectionString, null, name);
        }
        catch
        {
            OneCaseAtATime.Release();
            throw;
        }
    }

    /// <summary>Returns the connection string of a uniquely named database on this server and records the database for dropping.</summary>
    /// <param name="databaseName">Database name of letters, digits and underscores.</param>
    /// <returns>The database connection string with a 180-second command timeout.</returns>
    public string DatabaseConnectionString(string databaseName)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(databaseName, "^[A-Za-z0-9_]+$"))
            throw new ArgumentException("SQL Server case databases are named with letters, digits and underscores.", nameof(databaseName));

        _databases.Add(databaseName);
        return $"{ConnectionString}Database={databaseName};Command Timeout=180;";
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        try
        {
            await DropDatabasesAsync().ConfigureAwait(false);
        }
        finally
        {
            // Both removals are best effort and never throw, so the next case is always released.
            if (_localDb is not null)
                await _localDb.DisposeAsync().ConfigureAwait(false);
            if (_containerName is not null)
                await SqlServerContainer.RemoveAsync(_containerName).ConfigureAwait(false);
            OneCaseAtATime.Release();
        }
    }

    private async Task DropDatabasesAsync()
    {
        if (_databases.Count == 0)
            return;

        Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
        try
        {
            await using var connection = new Microsoft.Data.SqlClient.SqlConnection($"{ConnectionString}Database=master;");
            await connection.OpenAsync().ConfigureAwait(false);
            foreach (var name in _databases)
            {
                await using var drop = connection.CreateCommand();
                drop.CommandText =
                    $"IF DB_ID(N'{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END";
                await drop.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            // Best effort: the server itself is removed next.
        }
    }

    private static bool IsLocalDbInstalled()
        => OperatingSystem.IsWindows()
            && (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(directory => File.Exists(Path.Combine(directory.Trim('"'), "SqlLocalDB.exe")));
}

/// <summary>
/// TEST-MCP-102 AC-001 (PLAN-SESSIONTRIAGEFIX-001 B11): starts and removes the temporary SQL Server 2025 Developer container. The generated SA
/// password reaches docker through the docker process's environment (-e MSSQL_SA_PASSWORD without a value), never its command line, and is
/// never written to any output.
/// </summary>
internal static class SqlServerContainer
{
    /// <summary>Starts the container the plan describes, waits until it answers, and returns its name and server-level connection string.</summary>
    /// <param name="plan">An allowed temporary-instance plan.</param>
    /// <returns>The container name and the connection string, which ends with a separator.</returns>
    internal static async Task<(string Name, string ConnectionString)> StartAsync(SqlServerSandboxPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var name = $"mcp-sqlsandbox-{Guid.NewGuid():N}";
        var password = $"Qb1!{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16))}a";
        var run = await RunDockerAsync(
            [
                "run", "-d", "--rm", "--pull", "never", "--name", name,
                "-e", "ACCEPT_EULA=Y", "-e", $"MSSQL_PID={plan.ProductId}", "-e", "MSSQL_SA_PASSWORD",
                "-p", $"{plan.HostAddress}::1433", plan.Image!,
            ],
            password).ConfigureAwait(false);
        if (run.ExitCode != 0)
            throw new InvalidOperationException($"docker run for the temporary SQL Server instance failed with exit code {run.ExitCode}: {run.StandardError.Trim()}");

        try
        {
            var port = await RunDockerAsync(["port", name, "1433/tcp"], null).ConfigureAwait(false);
            var mapped = port.StandardOutput
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .First(line => line.StartsWith($"{plan.HostAddress}:", StringComparison.Ordinal));
            var hostPort = int.Parse(mapped[(mapped.LastIndexOf(':') + 1)..], CultureInfo.InvariantCulture);
            var connectionString = $"Server={plan.HostAddress},{hostPort};User ID=sa;Password={password};TrustServerCertificate=True;";
            await WaitUntilReadyAsync(name, connectionString).ConfigureAwait(false);
            return (name, connectionString);
        }
        catch
        {
            await RemoveAsync(name).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Removes the container (best effort).</summary>
    /// <param name="name">Container name.</param>
    /// <returns>A task that completes when docker returns.</returns>
    internal static async Task RemoveAsync(string name)
    {
        try
        {
            await RunDockerAsync(["rm", "-f", name], null).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // Best effort: --rm also removes the container when it stops.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Best effort: docker is not startable here.
        }
    }

    private static async Task WaitUntilReadyAsync(string name, string connectionString)
    {
        var deadline = DateTime.UtcNow.AddMinutes(3);
        string? lastError = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await using var connection = new Microsoft.Data.SqlClient.SqlConnection($"{connectionString}Database=master;Connect Timeout=5;");
                await connection.OpenAsync().ConfigureAwait(false);
                await using var probe = connection.CreateCommand();
                probe.CommandText = "SELECT 1";
                await probe.ExecuteScalarAsync().ConfigureAwait(false);
                return;
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                lastError = ex.Message;
                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
        }

        var logs = await RunDockerAsync(["logs", "--tail", "20", name], null).ConfigureAwait(false);
        throw new InvalidOperationException(
            $"The temporary SQL Server instance did not accept connections within 3 minutes. Last error: {lastError}\nContainer log tail:\n{logs.StandardOutput}{logs.StandardError}");
    }

    private static async Task<(int ExitCode, string StandardOutput, string StandardError)> RunDockerAsync(IReadOnlyList<string> arguments, string? saPassword)
    {
        var psi = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);
        if (saPassword is not null)
            psi.Environment["MSSQL_SA_PASSWORD"] = saPassword;

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("docker could not be started.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        return (process.ExitCode, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false));
    }
}

/// <summary>
/// Manages a private SQL Server LocalDB instance for integration testing.
/// </summary>
internal sealed class SqlLocalDbSandbox : IAsyncDisposable
{
    private readonly string _instanceName;
    private bool _disposed;

    private SqlLocalDbSandbox(string instanceName)
    {
        _instanceName = instanceName;
        ConnectionString = $"Server=(localdb)\\{instanceName};Integrated Security=true;TrustServerCertificate=True;";
    }

    /// <summary>Gets the LocalDB connection string for the private instance.</summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Creates, starts, and returns a new private SQL Server LocalDB instance.
    /// </summary>
    /// <returns>An initialized LocalDB sandbox.</returns>
    public static async Task<SqlLocalDbSandbox> CreateAsync()
    {
        // Requires LocalDB 15.0 (SQL Server 2019) or newer; the instance itself is created
        // without a version pin so the newest installed engine is used (a pinned version that is
        // absent makes SqlLocalDB.exe report failure on stdout while still exiting 0).
        var versionsOutput = await RunSqlLocalDbAsync("versions").ConfigureAwait(false);
        var installed = System.Text.RegularExpressions.Regex.Matches(versionsOutput, @"\((\d+)\.(\d+)")
            .Select(m => new Version(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)))
            .DefaultIfEmpty(new Version(0, 0))
            .Max();
        if (installed < new Version(15, 0))
        {
            throw new InvalidOperationException(
                $"SQL Server LocalDB 15.0 or newer is required; newest installed engine is {installed}. " +
                "Run the 'InstallTestDependencies' Nuke target.");
        }

        var instanceName = $"mcp-provider-{Guid.NewGuid():N}";
        await RunSqlLocalDbAsync("create", instanceName).ConfigureAwait(false);
        await RunSqlLocalDbAsync("start", instanceName).ConfigureAwait(false);
        return new SqlLocalDbSandbox(instanceName);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            await RunSqlLocalDbAsync("stop", _instanceName, "-k").ConfigureAwait(false);
        }
        catch
        {
            // Best-effort cleanup only.
        }

        try
        {
            await RunSqlLocalDbAsync("delete", _instanceName).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private static async Task<string> RunSqlLocalDbAsync(params string[] arguments)
    {
        var psi = new ProcessStartInfo("SqlLocalDB.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("SqlLocalDB.exe could not be started.");
        var stdout = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);

        // SqlLocalDB.exe reports some failures (e.g. unknown version) on stdout with exit code 0.
        if (process.ExitCode != 0 || stdout.Contains("failed because of the following error", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"SqlLocalDB.exe {string.Join(" ", arguments)} failed with exit code {process.ExitCode}.\nSTDOUT: {stdout}\nSTDERR: {stderr}");
        }

        return stdout;
    }
}

/// <summary>
/// Adopts the repository PostgreSQL test cluster (MCP_TEST_POSTGRES_CONNECTION or ephemeral binaries)
/// and creates uniquely named scratch databases for clean-head handoff migration tests.
/// </summary>
internal sealed class EphemeralPostgresSandbox : IAsyncDisposable
{
    private readonly QBrainAi.Support.Mcp.Tests.Storage.EphemeralPostgresFixture _fixture = new();

    /// <summary>Creates a scratch database on the fixture server.</summary>
    public void CreateDatabase(string databaseName)
    {
        using var admin = new Npgsql.NpgsqlConnection(_fixture.ServerConnectionString);
        admin.Open();
        using var create = admin.CreateCommand();
        create.CommandText = $"CREATE DATABASE \"{databaseName}\";";
        create.ExecuteNonQuery();
    }

    /// <summary>Returns a connection string targeting the scratch database.</summary>
    public string GetDatabaseConnectionString(string databaseName)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(_fixture.ServerConnectionString) { Database = databaseName };
        return builder.ToString();
    }

    /// <summary>Drops the scratch database.</summary>
    public void DropDatabase(string databaseName)
    {
        try
        {
            using var admin = new Npgsql.NpgsqlConnection(_fixture.ServerConnectionString);
            admin.Open();
            using var terminate = admin.CreateCommand();
            terminate.CommandText =
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{databaseName}' AND pid <> pg_backend_pid();";
            terminate.ExecuteNonQuery();
            using var drop = admin.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\";";
            drop.ExecuteNonQuery();
        }
        catch (Npgsql.NpgsqlException)
        {
            // Best-effort cleanup; scratch names are unique.
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _fixture.Dispose();
        return ValueTask.CompletedTask;
    }
}
