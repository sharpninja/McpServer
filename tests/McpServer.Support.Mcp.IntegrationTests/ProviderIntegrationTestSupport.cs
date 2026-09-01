using System.Diagnostics;
using System.Globalization;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace McpServer.Support.Mcp.IntegrationTests;

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
    private readonly McpServer.Support.Mcp.Tests.Storage.EphemeralPostgresFixture _fixture = new();

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
