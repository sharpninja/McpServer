using System.Globalization;
using System.Reflection;
using McpServer.Common.Copilot.Extensions;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Logging;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    public static WebApplication Create(string workspacePath, int port, ILoggerFactory loggerFactory, string? dataDirectory = null, WorkspaceTokenService? tokenService = null, WorkspaceConfigEntry? workspaceConfig = null)
    {
        var workspaceName = Path.GetFileName(
            workspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var effectiveDataDir = string.IsNullOrWhiteSpace(dataDirectory) ? workspacePath : dataDirectory;
        var dataSource = Path.Combine(effectiveDataDir, "mcp.db");
        var logger = loggerFactory.CreateLogger("McpServer.Support.Mcp.WorkspaceAppFactory");

        var builder = WebApplication.CreateSlimBuilder();

        // Set the content root to the workspace directory so relative paths resolve correctly.
        builder.Environment.ContentRootPath = workspacePath;

        // Kestrel listens on the workspace port only.
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(port);
        });

        // Load appsettings.json from the workspace directory (if present) so workspace-level
        // config (e.g. Mcp:Auth) is available to workspace controllers.
        var workspaceAppSettings = Path.Combine(workspacePath, "appsettings.json");
        if (File.Exists(workspaceAppSettings))
            builder.Configuration.AddJsonFile(workspaceAppSettings, optional: true, reloadOnChange: false);

        // Override configuration for this workspace.
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mcp:Port"] = port.ToString(CultureInfo.InvariantCulture),
            ["Mcp:RepoRoot"] = workspacePath,
            ["Mcp:DataDirectory"] = effectiveDataDir,
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
        builder.Services.Configure<OidcAuthOptions>(builder.Configuration.GetSection(OidcAuthOptions.SectionName));
        builder.Services.Configure<TodoPromptOptions>(options =>
        {
            options.BaseUrl = $"http://localhost:{port}";
            if (workspaceConfig is not null)
            {
                options.StatusPrompt = workspaceConfig.StatusPrompt;
                options.ImplementPrompt = workspaceConfig.ImplementPrompt;
                options.PlanPrompt = workspaceConfig.PlanPrompt;
                options.RunAs = workspaceConfig.RunAs;
                options.GitHubToken = workspaceConfig.GitHubToken;
                options.AgentPath = workspaceConfig.AgentPath;
            }
        });
        builder.Services.Configure<VectorIndexOptions>(options =>
        {
            options.IndexPath = Path.Combine(workspacePath, "mcp-data", "vector.idx");
        });

        var oidcAuthBootstrap = builder.Configuration.GetSection(OidcAuthOptions.SectionName).Get<OidcAuthOptions>()
            ?? new OidcAuthOptions();

        if (oidcAuthBootstrap.Enabled)
        {
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.Authority = oidcAuthBootstrap.Authority;
                    options.Audience = oidcAuthBootstrap.Audience;
                    options.RequireHttpsMetadata = oidcAuthBootstrap.RequireHttpsMetadata;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "preferred_username",
                        RoleClaimType = "realm_roles",
                        ValidateAudience = !string.IsNullOrWhiteSpace(oidcAuthBootstrap.Audience),
                    };
                });
        }
        else
        {
            builder.Services.AddAuthentication();
        }

        builder.Services.AddAuthorization();

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
        builder.Services.AddSingleton<ITodoPromptService, TodoPromptService>();
        builder.Services.AddCopilotClient();

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

        // Share the primary host's token service so child workspaces validate the same tokens.
        if (tokenService is not null)
            builder.Services.AddSingleton(tokenService);
        else
            builder.Services.AddSingleton<WorkspaceTokenService>();

        // MCP Streamable HTTP transport with the same tools as the primary host.
        builder.Services.AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly(typeof(FwhMcpTools).Assembly);

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(WorkspaceAppFactory).Assembly)
            .ConfigureApplicationPartManager(manager =>
            {
                // Exclude primary-host-only controllers from child workspace apps.
                manager.FeatureProviders.Add(new ExcludeControllerFeatureProvider(
                    typeof(Controllers.WorkspaceController),
                    typeof(Controllers.AgentController)));
            });

        var app = builder.Build();

        // Run workspace DB migrations only when needed. Calling Migrate() unconditionally can block
        // child-host startup on SQLite migration locks even when the schema is already current.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            var pendingMigrations = db.Database.GetPendingMigrations().ToArray();
            if (pendingMigrations.Length > 0)
            {
                logger.LogInformation(
                    "Applying {Count} pending workspace DB migration(s): Workspace={WorkspacePath}; Port={Port}",
                    pendingMigrations.Length,
                    workspacePath,
                    port);
                db.Database.Migrate();
            }
            else
            {
                logger.LogDebug(
                    "Workspace DB already up to date; skipping migration lock acquisition: Workspace={WorkspacePath}; Port={Port}",
                    workspacePath,
                    port);
            }
        }

        app.UseMiddleware<InteractionLoggingMiddleware>();
        app.UseAuthentication();
        app.UseMiddleware<WorkspaceAuthMiddleware>();
        app.UseAuthorization();
        app.MapControllers();
        app.MapMcp("/mcp-transport");
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", workspace = workspaceName, port }));

        // Unprotected endpoint returning the default (anonymous) API key for consumers without marker file access.
        app.MapGet("/api-key", (WorkspaceTokenService ts, IConfiguration cfg) =>
        {
            var wp = cfg["Mcp:RepoRoot"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(wp))
                return Results.Problem("No workspace configured.", statusCode: 503);

            var k = Path.GetFullPath(wp).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var dt = ts.GetDefaultToken(k);
            if (dt is null)
                return Results.Problem("Default token not yet generated. Retry shortly.", statusCode: 503);

            return Results.Ok(new { apiKey = dt });
        });

        return app;
    }
}

/// <summary>
/// Removes specified controller types from the MVC feature so they are not discovered.
/// Used by workspace hosts to exclude primary-only controllers (e.g. WorkspaceController).
/// </summary>
internal sealed class ExcludeControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly HashSet<TypeInfo> _excluded;

    public ExcludeControllerFeatureProvider(params Type[] excludedControllers)
    {
        _excluded = new HashSet<TypeInfo>(excludedControllers.Select(t => t.GetTypeInfo()));
    }

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var type in _excluded)
            feature.Controllers.Remove(type);
    }
}
