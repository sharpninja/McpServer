// TR-PLANNED-013: Runs the MCP server over STDIO (stdin/stdout JSON-RPC) when --transport stdio.

using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.McpStdio;

/// <summary>
/// TR-PLANNED-013: Host for MCP STDIO transport; registers shared services and runs MCP server.
/// </summary>
public static class McpStdioHost
{
    /// <summary>Run MCP server over STDIO. Logs go to stderr.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var instanceName = McpInstanceResolver.GetRequestedInstanceName(args);
        McpInstanceResolver.ValidateInstances(builder.Configuration);
        McpInstanceResolver.ValidateTodoStorage(builder.Configuration, instanceName);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(consoleOptions =>
        {
            consoleOptions.LogToStandardErrorThreshold = LogLevel.Information;
        });

        var databaseProvider = (McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "DatabaseProvider") ?? "sqlite")
            .Trim()
            .ToUpperInvariant();

        if (databaseProvider is "POSTGRES" or "POSTGRESQL" or "NPGSQL")
        {
            var postgresConnectionString = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "PostgresConnectionString")
                ?? builder.Configuration.GetConnectionString("Mcp");

            if (string.IsNullOrWhiteSpace(postgresConnectionString))
                throw new InvalidOperationException("Mcp:PostgresConnectionString (or ConnectionStrings:Mcp) is required when Mcp:DatabaseProvider is postgres.");

            builder.Services.AddDbContext<McpDbContext>(options =>
            {
                options.UseNpgsql(postgresConnectionString);
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
        }
        else
        {
            var dataSource = McpInstanceResolver.ResolveSqliteDataSource(builder.Configuration, instanceName);
            builder.Services.AddDbContext<McpDbContext>(options =>
            {
                options.UseSqlite($"Data Source={dataSource}");
            }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
        }

        builder.Services.Configure<IngestionOptions>(builder.Configuration.GetSection("Mcp"));
        builder.Services.Configure<GraphRagOptions>(builder.Configuration.GetSection(GraphRagOptions.SectionName));
        builder.Services.Configure<TodoStorageOptions>(builder.Configuration.GetSection(TodoStorageOptions.SectionName));
        builder.Services.PostConfigure<VectorIndexOptions>(options =>
        {
            var instanceIndexPath = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "IndexPath");
            if (!string.IsNullOrWhiteSpace(instanceIndexPath))
                options.IndexPath = instanceIndexPath;

            if (!Path.IsPathRooted(options.IndexPath))
            {
                var repoRoot = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "RepoRoot") ?? ".";
                options.IndexPath = Path.GetFullPath(Path.Combine(repoRoot, options.IndexPath));
            }
        });
        builder.Services.PostConfigure<IngestionOptions>(options =>
        {
            options.RepoRoot = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "RepoRoot") ?? options.RepoRoot;
            options.TodoFilePath = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "TodoFilePath") ?? options.TodoFilePath;
            options.SessionsPath = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "SessionsPath") ?? options.SessionsPath;
            options.UnifiedModelSchemaPath = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "UnifiedModelSchemaPath") ?? options.UnifiedModelSchemaPath;
            options.ExternalDocsPath = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "ExternalDocsPath") ?? options.ExternalDocsPath;

            options.TodoFilePath = McpInstanceResolver.ResolveDataPath(builder.Configuration, instanceName, options.TodoFilePath);
            options.SessionsPath = McpInstanceResolver.ResolveDataPath(builder.Configuration, instanceName, options.SessionsPath);
            options.UnifiedModelSchemaPath = McpInstanceResolver.ResolveDataPath(builder.Configuration, instanceName, options.UnifiedModelSchemaPath);
        });
        builder.Services.PostConfigure<TodoStorageOptions>(options =>
        {
            options.Provider = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "TodoStorage:Provider") ?? options.Provider;
            options.SqliteDataSource = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "TodoStorage:SqliteDataSource") ?? options.SqliteDataSource;
            options.SqliteDataSource = McpInstanceResolver.ResolveDataPath(builder.Configuration, instanceName, options.SqliteDataSource);
        });
        builder.Services.AddSingleton<ISyncStatusStore, SyncStatusStore>();
        builder.Services.AddSingleton<IWriteAuditLog, WriteAuditLog>();
        builder.Services.AddSingleton<Chunker>();
        builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
        builder.Services.AddSingleton<IGitHubCliService, GitHubCliService>();
        builder.Services.AddSingleton<ITodoService>(sp =>
        {
            var provider = (sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TodoStorageOptions>>().Value.Provider ?? "yaml")
                .Trim()
                .ToUpperInvariant();

            return provider switch
            {
                "SQLITE" => ActivatorUtilities.CreateInstance<SqliteTodoService>(sp),
                _ => ActivatorUtilities.CreateInstance<TodoService>(sp),
            };
        });
        builder.Services.AddSingleton<TodoServiceResolver>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<WorkspaceServiceAccessor>();
        builder.Services.AddScoped<RepoIngestor>();
        builder.Services.AddScoped<SessionLogIngestor>();
        builder.Services.AddScoped<ExternalDocsIngestor>();
        builder.Services.AddScoped<GitHubIngestor>();
        builder.Services.AddScoped<IngestionCoordinator>();
        builder.Services.AddScoped<IRepoFileService, RepoFileService>();
        builder.Services.AddScoped<ISessionLogService, SessionLogService>();
        builder.Services.AddScoped<Fts5SearchService>();
        builder.Services.AddScoped<IContextSearchService, Fts5SearchService>();
        builder.Services.AddScoped<IGraphRagService, GraphRagService>();
        builder.Services.AddScoped<FwhMcpTools>();

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(typeof(FwhMcpTools).Assembly);

        var host = builder.Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }

        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }
}
