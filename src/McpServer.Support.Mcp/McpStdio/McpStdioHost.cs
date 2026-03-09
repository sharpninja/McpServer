// TR-PLANNED-013: Runs the MCP server over STDIO (stdin/stdout JSON-RPC) when --transport stdio.

using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Common.Copilot;
using McpServer.Common.Copilot.Extensions;
using McpServer.GraphRag;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        builder.Services.Configure<GitHubIntegrationOptions>(builder.Configuration.GetSection(GitHubIntegrationOptions.SectionName));
        builder.Services.Configure<TodoPromptOptions>(builder.Configuration.GetSection(TodoPromptOptions.SectionName));
        builder.Services.Configure<TemplateStorageOptions>(builder.Configuration.GetSection(TemplateStorageOptions.SectionName));
        var requiredRepoAllowlistPatterns = new[]
        {
            "src/McpServer.Cqrs/**/*.cs",
            "src/McpServer.Cqrs.Mvvm/**/*.cs",
            "src/McpServer.UI.Core/**/*.cs",
            "src/McpServer.Director/**/*.cs",
            "docs/README.md",
            "docs/MCP-SERVER.md",
            "docs/USER-GUIDE.md",
            "docs/FAQ.md",
            "docs/CLIENT-INTEGRATION.md",
            "docs/RELEASE-CHECKLIST.md",
            "docs/Operations/**/*.md",
        };
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

            var allowlist = options.RepoAllowlist?.ToList() ?? [];
            foreach (var pattern in requiredRepoAllowlistPatterns)
            {
                if (!allowlist.Contains(pattern, StringComparer.OrdinalIgnoreCase))
                    allowlist.Add(pattern);
            }

            options.RepoAllowlist = allowlist;
        });
        builder.Services.PostConfigure<TodoStorageOptions>(options =>
        {
            options.Provider = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "TodoStorage:Provider") ?? options.Provider;
            options.SqliteDataSource = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "TodoStorage:SqliteDataSource") ?? options.SqliteDataSource;
            options.SqliteDataSource = McpInstanceResolver.ResolveDataPath(builder.Configuration, instanceName, options.SqliteDataSource);
        });
        builder.Services.PostConfigure<GitHubIntegrationOptions>(options =>
        {
            options.TokenStorePath = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "GitHub:TokenStorePath")
                ?? options.TokenStorePath;
            options.TokenStorePath = McpInstanceResolver.ResolveDataPath(builder.Configuration, instanceName, options.TokenStorePath);
        });

        builder.Services.AddSingleton<IPostConfigureOptions<TemplateStorageOptions>>(_ =>
            new TemplateStorageOptionsPostConfigure(builder.Configuration, instanceName));
        builder.Services.AddSingleton<ISyncStatusStore, SyncStatusStore>();
        builder.Services.AddSingleton<IWriteAuditLog, WriteAuditLog>();
        builder.Services.AddHttpClient(WebsiteIngestor.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<IngestionOptions>>().Value;
            var timeoutSeconds = Math.Clamp(options.WebsiteRequestTimeoutSeconds, 5, 600);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("McpServer-WebsiteIngestor/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });
        builder.Services.Configure<HttpStandardResilienceOptions>(WebsiteIngestor.HttpClientName, options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(180);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(180);
        });
        builder.Services.AddSingleton<Chunker>();
        builder.Services.AddDataProtection();
        builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
        builder.Services.AddSingleton<IProcessSpawner, DefaultProcessSpawner>();
        builder.Services.AddSingleton<IGitHubWorkspaceTokenStore, FileGitHubWorkspaceTokenStore>();
        builder.Services.AddSingleton<IGitHubCliService, GitHubCliService>();
        builder.Services.AddSingleton<ITodoServiceFactory, TodoServiceFactory>();
        builder.Services.AddSingleton<ITodoService>(sp => sp.GetRequiredService<ITodoServiceFactory>().CreatePrimary());
        builder.Services.AddSingleton<TodoServiceResolver>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<WorkspaceServiceAccessor>();
        builder.Services.AddSingleton<IRequirementsService, RequirementsService>();
        builder.Services.AddSingleton<RequirementsDocumentService>();
        builder.Services.AddSingleton<IRequirementsRepository>(sp => sp.GetRequiredService<RequirementsDocumentService>());
        builder.Services.AddSingleton<IRequirementsDocumentService>(sp => sp.GetRequiredService<RequirementsDocumentService>());
        builder.Services.AddSingleton<PromptTemplateRenderer>();
        builder.Services.AddSingleton<IPromptTemplateService, PromptTemplateService>();
        builder.Services.AddSingleton<ITodoPromptProvider, TodoPromptProvider>();
        builder.Services.AddSingleton<ITodoPromptService, TodoPromptService>();
        builder.Services.AddCopilotClient();
        builder.Services.RemoveAll<ICopilotClient>();
        builder.Services.AddSingleton<ICopilotClient>(sp =>
            new AuditedCopilotClient(
                sp.GetRequiredService<CopilotClient>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IHttpContextAccessor>(),
                sp.GetRequiredService<IOptions<IngestionOptions>>(),
                sp.GetRequiredService<ILogger<AuditedCopilotClient>>()));
        builder.Services.AddScoped<RepoIngestor>();
        builder.Services.AddScoped<SessionLogIngestor>();
        builder.Services.AddScoped<ExternalDocsIngestor>();
        builder.Services.AddScoped<GitHubIngestor>();
        builder.Services.AddScoped<IssueIngestor>();
        builder.Services.AddScoped<IWebsiteIngestor, WebsiteIngestor>();
        builder.Services.AddScoped<IngestionCoordinator>();
        builder.Services.AddScoped<IRepoFileService, RepoFileService>();
        builder.Services.AddScoped<ISessionLogService, SessionLogService>();
        builder.Services.AddScoped<Fts5SearchService>();
        builder.Services.AddScoped<IContextSearchService, Fts5SearchService>();
        builder.Services.AddMcpGraphRag();
        builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
        builder.Services.AddScoped<IWorkspacePolicyDirectiveParser, WorkspacePolicyDirectiveParser>();
        builder.Services.AddScoped<IWorkspacePolicyService, WorkspacePolicyService>();
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

