using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>Suppresses appsettings workspace projection writes from integration-test hosts.</summary>
internal sealed class NoOpWorkspaceProjectionWriter : IWorkspaceProjectionWriter
{
    /// <inheritdoc />
    public Task WriteProjectionAsync(IReadOnlyList<WorkspaceConfigEntry> workspaces, CancellationToken ct)
        => Task.CompletedTask;
}

/// <summary>Shared relational database wiring for custom integration-test factories.</summary>
internal static class IntegrationTestDatabase
{
    /// <summary>Replaces any prior MCP database registration with isolated SQLite storage.</summary>
    public static void ConfigureSqlite(IServiceCollection services, string databasePath)
    {
        var connectionString = $"Data Source={databasePath}";
        var providerOptions = McpDatabaseProviderFactory.CreateOptions("sqlite", connectionString);

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
            McpDatabaseProviderFactory.Configure(options, providerOptions);
            options.EnableSensitiveDataLogging();
        }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
    }

    /// <summary>Creates the isolated integration-test database schema on host startup.</summary>
    public sealed class Initializer : IHostedService
    {
        private readonly IServiceProvider _services;

        /// <summary>Initializes a new instance of the <see cref="Initializer"/> class.</summary>
        public Initializer(IServiceProvider services)
        {
            _services = services;
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

            var importer = ActivatorUtilities.CreateInstance<TodoBootstrapImporter>(_services);
            await importer.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
