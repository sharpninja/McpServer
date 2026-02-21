using System.Globalization;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Logging;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.AspNetCore;
using Serilog;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Builds lightweight in-process <see cref="WebApplication"/> instances for workspace Kestrel hosts.
/// Each workspace gets its own DI container, DbContext, and Kestrel listener.
/// </summary>
public static class WorkspaceAppFactory
{
    /// <summary>
    /// Creates a <see cref="WebApplication"/> configured for the given workspace path and port.
    /// The host serves MCP tools and API controllers scoped to the workspace data.
    /// </summary>
    public static WebApplication Create(string workspacePath, int port, ILoggerFactory loggerFactory)
    {
        var workspaceName = Path.GetFileName(
            workspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var dataSource = Path.Combine(workspacePath, "mcp.db");

        var builder = WebApplication.CreateSlimBuilder();

        // Kestrel listens on the workspace port only.
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(port);
        });

        // Override configuration for this workspace.
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mcp:Port"] = port.ToString(CultureInfo.InvariantCulture),
            ["Mcp:RepoRoot"] = workspacePath,
            ["Mcp:DataDirectory"] = workspacePath,
            ["Mcp:DataSource"] = dataSource,
            ["Mcp:TodoFilePath"] = Path.Combine(workspacePath, "docs", "Project", "TODO.yaml"),
            ["Mcp:SessionsPath"] = Path.Combine(workspacePath, "docs", "sessions"),
            ["Mcp:ExternalDocsPath"] = Path.Combine(workspacePath, "docs", "external"),
        });

        // Reuse the primary host's Serilog logger.
        builder.Host.UseSerilog(Log.Logger);

        // EF Core — workspace-scoped SQLite database.
        builder.Services.AddDbContext<McpDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dataSource}");
        }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);

        // Options.
        builder.Services.Configure<IngestionOptions>(options =>
        {
            options.RepoRoot = workspacePath;
            options.TodoFilePath = Path.Combine(workspacePath, "docs", "Project", "TODO.yaml");
            options.SessionsPath = Path.Combine(workspacePath, "docs", "sessions");
            options.ExternalDocsPath = Path.Combine(workspacePath, "docs", "external");
        });
        builder.Services.Configure<TodoStorageOptions>(builder.Configuration.GetSection(TodoStorageOptions.SectionName));
        builder.Services.Configure<EmbeddingOptions>(builder.Configuration.GetSection("Embedding"));
        builder.Services.Configure<VectorIndexOptions>(options =>
        {
            options.IndexPath = Path.Combine(workspacePath, "mcp-data", "vector.idx");
        });

        // Core services required by MCP tools.
        builder.Services.AddSingleton<ISyncStatusStore, SyncStatusStore>();
        builder.Services.AddSingleton<IWriteAuditLog, WriteAuditLog>();
        builder.Services.AddSingleton<Chunker>();
        builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
        builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();
        builder.Services.AddSingleton<IVectorIndexService, VectorIndexService>();
        builder.Services.AddSingleton<IGitHubCliService, GitHubCliService>();
        builder.Services.AddSingleton<ITodoService>(sp =>
        {
            var provider = (sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TodoStorageOptions>>().Value.Provider ?? "yaml")
                .Trim().ToUpperInvariant();
            return provider switch
            {
                "SQLITE" => ActivatorUtilities.CreateInstance<SqliteTodoService>(sp),
                _ => ActivatorUtilities.CreateInstance<TodoService>(sp),
            };
        });
        builder.Services.AddSingleton<IIssueTodoSyncService, IssueTodoSyncService>();
        builder.Services.AddSingleton<IRequirementsService, RequirementsService>();

        builder.Services.AddScoped<RepoIngestor>();
        builder.Services.AddScoped<SessionLogIngestor>();
        builder.Services.AddScoped<ExternalDocsIngestor>();
        builder.Services.AddScoped<GitHubIngestor>();
        builder.Services.AddScoped<IssueIngestor>();
        builder.Services.AddScoped<IngestionCoordinator>();
        builder.Services.AddScoped<IRepoFileService, RepoFileService>();
        builder.Services.AddScoped<ISessionLogService, SessionLogService>();
        builder.Services.AddScoped<Fts5SearchService>();
        builder.Services.AddScoped<IContextSearchService, HybridSearchService>();

        // Interaction logging (workspace-local, no remote submission).
        builder.Services.Configure<McpInteractionLoggingOptions>(options =>
        {
            options.LoggingServiceUrl = null;
        });
        builder.Services.AddSingleton<IInteractionLogSubmissionChannel, InteractionLogSubmissionChannel>();

        // MCP Streamable HTTP transport with the same tools as the primary host.
        builder.Services.AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly(typeof(FwhMcpTools).Assembly);

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(WorkspaceAppFactory).Assembly);

        var app = builder.Build();

        // Run workspace DB migrations.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            db.Database.Migrate();
        }

        app.UseMiddleware<InteractionLoggingMiddleware>();
        app.MapMcp("/mcp-transport");
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", workspace = workspaceName, port }));

        return app;
    }
}
