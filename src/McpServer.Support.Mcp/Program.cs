// TR-PLANNED-013 / FR-SUPPORT-010: MCP Context Unification - local MCP server for Cursor and Copilot.

using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using McpServer.Common.Copilot.Extensions;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Logging;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File;

// TR-PLANNED-013: When --transport stdio, run MCP over stdin/stdout and exit (no HTTP).
if (IsStdioTransportRequested(args))
{
    await McpStdioHost.RunAsync(args, default).ConfigureAwait(false);
    return;
}

var serverStartupUtc = DateTimeOffset.UtcNow;

bool IsStdioTransportRequested(string[] a)
{
    for (var i = 0; i < a.Length; i++)
    {
        if (a[i].StartsWith("--transport=", StringComparison.OrdinalIgnoreCase))
            return a[i].AsSpan("--transport=".Length).Equals("stdio", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(a[i], "--transport", StringComparison.OrdinalIgnoreCase) && i + 1 < a.Length)
            return string.Equals(a[i + 1], "stdio", StringComparison.OrdinalIgnoreCase);
    }
    return false;
}

var builder = WebApplication.CreateBuilder(args);
DisableEnvironmentSpecificJsonConfigForWindowsService(builder);
if (OperatingSystem.IsWindows())
{
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = "McpServer";
    });
}

var instanceName = McpInstanceResolver.GetRequestedInstanceName(args);
McpInstanceResolver.ValidateInstances(builder.Configuration);
McpInstanceResolver.ValidateTodoStorage(builder.Configuration, instanceName);

// Resolve the primary workspace from Mcp:Workspaces config (FR-MCP-025).
// Set ContentRootPath to the primary workspace's path so relative paths resolve correctly
// and WorkspaceProcessManager can identify it.
WorkspaceConfigEntry? primaryWorkspaceEntry = null;
{
    var workspaces = builder.Configuration.GetSection("Mcp:Workspaces").Get<List<WorkspaceConfigEntry>>() ?? [];
    primaryWorkspaceEntry = workspaces
        .Where(w => w.IsPrimary && w.IsEnabled)
        .FirstOrDefault();
    primaryWorkspaceEntry ??= workspaces
        .Where(w => w.IsEnabled)
        .FirstOrDefault();
    if (primaryWorkspaceEntry is not null)
        builder.Environment.ContentRootPath = Path.GetFullPath(primaryWorkspaceEntry.WorkspacePath);
}

// TR-PLANNED-013: Serilog with optional Parseable (local Docker) sink.
builder.Host.UseSerilog((context, _, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);

    var parseable = context.Configuration.GetSection(McpParseableOptions.SectionName).Get<McpParseableOptions>()
        ?? new McpParseableOptions();
    if (!context.HostingEnvironment.IsEnvironment("Test"))
    {
        var fileLogPath = ResolveSerilogFilePath(parseable.FallbackLogPath);
        EnsureSerilogFileDirectory(fileLogPath);
        config.WriteTo.File(
            path: fileLogPath,
            rollingInterval: RollingInterval.Day,
            formatProvider: CultureInfo.InvariantCulture,
            shared: true);
    }

    if (!string.IsNullOrWhiteSpace(parseable.Url) && !context.HostingEnvironment.IsEnvironment("Test"))
    {
        var ingestUri = $"{parseable.Url!.TrimEnd('/')}/api/v1/ingest";
        var httpClient = new ParseableHttpClient(parseable.StreamName, parseable.Username, parseable.Password);
        // Exclude Parseable meta-logs (success/failure of push) so they are not republished to Parseable.
        config.WriteTo.Logger(lc => lc
            .Filter.ByExcluding(e => e.Properties.TryGetValue(ParseableHttpClient.ParseableMetaPropertyName, out var v) && v is ScalarValue s && (s.Value is true or "True"))
            .WriteTo.Http(requestUri: ingestUri, queueLimitBytes: null, textFormatter: new ParseableEventFormatter(), batchFormatter: new ParseableBatchFormatter(), httpClient: httpClient, restrictedToMinimumLevel: LogEventLevel.Verbose));
    }
}, writeToProviders: true);

if (OperatingSystem.IsWindows())
{
    ConfigureWindowsEventLogSource(builder);
}

var portFromEnv = Environment.GetEnvironmentVariable("PORT");
var configuredPort = McpInstanceResolver.GetEffectiveMcpInt(builder.Configuration, instanceName, "Port", 7147);
var listenPort = int.TryParse(portFromEnv, out var envPort) ? envPort : configuredPort;

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(listenPort);
    });
}
else
{
    builder.WebHost.UseUrls($"http://+:{listenPort}");
}

builder.AddServiceDefaults();

if (builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddDbContext<McpDbContext>(options =>
    {
        options.UseInMemoryDatabase("mcp-tests");
        options.EnableSensitiveDataLogging();
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
builder.Services.Configure<MarkerPromptOptions>(builder.Configuration.GetSection(MarkerPromptOptions.SectionName));
builder.Services.Configure<McpParseableOptions>(builder.Configuration.GetSection(McpParseableOptions.SectionName));
builder.Services.Configure<McpInteractionLoggingOptions>(builder.Configuration.GetSection(McpInteractionLoggingOptions.SectionName));
builder.Services.Configure<TodoStorageOptions>(builder.Configuration.GetSection(TodoStorageOptions.SectionName));
builder.Services.Configure<VoiceConversationOptions>(builder.Configuration.GetSection(VoiceConversationOptions.SectionName));
builder.Services.Configure<RequirementsOptions>(builder.Configuration.GetSection(RequirementsOptions.SectionName));
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
});
builder.Services.PostConfigure<TodoStorageOptions>(options =>
{
    options.Provider = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "TodoStorage:Provider") ?? options.Provider;
    options.SqliteDataSource = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "TodoStorage:SqliteDataSource") ?? options.SqliteDataSource;
    if (!Path.IsPathRooted(options.SqliteDataSource))
    {
        var dataDirectory = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "DataDirectory") ?? ".";
        options.SqliteDataSource = Path.GetFullPath(Path.Combine(dataDirectory, options.SqliteDataSource));
    }
});
builder.Services.PostConfigure<RequirementsOptions>(options =>
{
    var repoRoot = McpInstanceResolver.GetEffectiveMcpValue(builder.Configuration, instanceName, "RepoRoot")
                  ?? builder.Environment.ContentRootPath;
    repoRoot = Path.GetFullPath(repoRoot);

    static string ResolvePath(string repoRootPath, string path) =>
        Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(repoRootPath, path));

    options.FunctionalRequirementsPath = ResolvePath(repoRoot, options.FunctionalRequirementsPath);
    options.TechnicalRequirementsPath = ResolvePath(repoRoot, options.TechnicalRequirementsPath);
    options.TestingRequirementsPath = ResolvePath(repoRoot, options.TestingRequirementsPath);
    options.MappingPath = ResolvePath(repoRoot, options.MappingPath);
});
builder.Services.Configure<EmbeddingOptions>(builder.Configuration.GetSection("Embedding"));
builder.Services.Configure<VectorIndexOptions>(builder.Configuration.GetSection("VectorIndex"));
builder.Services.AddSingleton<IInteractionLogSubmissionChannel, InteractionLogSubmissionChannel>();
builder.Services.AddHostedService<InteractionLogSubmissionService>();
builder.Services.AddHttpClient("InteractionLogSubmission");
builder.Services.AddSingleton<ISyncStatusStore, SyncStatusStore>();
builder.Services.AddSingleton<IWriteAuditLog, WriteAuditLog>();
builder.Services.AddSingleton<Chunker>();
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.Configure<ProcessRunnerOptions>(options =>
{
    if (primaryWorkspaceEntry is not null)
    {
        options.GitHubToken = primaryWorkspaceEntry.GitHubToken;
    }
});
builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();
builder.Services.AddSingleton<IVectorIndexService, VectorIndexService>();
builder.Services.AddScoped<RepoIngestor>();
builder.Services.AddScoped<SessionLogIngestor>();
builder.Services.AddScoped<ExternalDocsIngestor>();
builder.Services.AddScoped<GitHubIngestor>();
builder.Services.AddScoped<IssueIngestor>();
builder.Services.AddScoped<IngestionCoordinator>();
builder.Services.AddScoped<IRepoFileService, RepoFileService>();
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
builder.Services.AddSingleton<IIssueTodoSyncService, IssueTodoSyncService>();
builder.Services.AddSingleton<IRequirementsService, RequirementsService>();
builder.Services.AddSingleton<RequirementsDocumentService>();
builder.Services.AddSingleton<IRequirementsRepository>(sp => sp.GetRequiredService<RequirementsDocumentService>());
builder.Services.AddSingleton<IRequirementsDocumentService>(sp => sp.GetRequiredService<RequirementsDocumentService>());
builder.Services.AddSingleton<ITodoPromptService, TodoPromptService>();
builder.Services.AddSingleton<IVoiceConversationService, VoiceConversationService>();
builder.Services.Configure<TodoPromptOptions>(options =>
{
    if (primaryWorkspaceEntry is not null)
    {
        options.StatusPrompt = primaryWorkspaceEntry.StatusPrompt;
        options.ImplementPrompt = primaryWorkspaceEntry.ImplementPrompt;
        options.PlanPrompt = primaryWorkspaceEntry.PlanPrompt;
        options.BaseUrl = $"http://localhost:{listenPort}";
        options.RunAs = primaryWorkspaceEntry.RunAs;
        options.GitHubToken = primaryWorkspaceEntry.GitHubToken;
        options.AgentPath = primaryWorkspaceEntry.AgentPath;
    }
});
builder.Services.AddCopilotClient();
builder.Services.AddScoped<ISessionLogService, SessionLogService>();
builder.Services.AddScoped<Fts5SearchService>();
builder.Services.AddScoped<IContextSearchService, HybridSearchService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IToolRegistryService, ToolRegistryService>();
builder.Services.AddScoped<IToolBucketService, ToolBucketService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddSingleton<WorkspaceTokenService>();
builder.Services.AddScoped<WorkspaceContext>();
builder.Services.AddSingleton(new ServerRuntimeInfo(serverStartupUtc, listenPort));
builder.Services.AddSingleton<IWorkspaceProcessManager, WorkspaceProcessManager>();
builder.Services.Configure<PairingOptions>(builder.Configuration.GetSection(PairingOptions.SectionName));
builder.Services.Configure<OidcAuthOptions>(builder.Configuration.GetSection(OidcAuthOptions.SectionName));
builder.Services.Configure<ToolRegistryOptions>(builder.Configuration.GetSection(ToolRegistryOptions.SectionName));
builder.Services.AddSingleton<PairingSessionService>();

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
    // Keep authorization available so [Authorize(Policy="AgentManager")] can fall back to API-key-only mode.
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AgentManager", policy =>
    {
        if (!oidcAuthBootstrap.Enabled)
        {
            policy.RequireAssertion(_ => true);
            return;
        }

        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(ctx => HasAnyRole(ctx.User, "agent-manager", "admin"));
    });
});

// Tunnel registry — providers registered via DI and started by the hosted service lifecycle.
builder.Services.Configure<TunnelOptions>(
    builder.Configuration.GetSection(TunnelOptions.SectionName));
builder.Services.AddSingleton<NgrokTunnelProvider>();
builder.Services.AddSingleton<ITunnelProvider>(sp => sp.GetRequiredService<NgrokTunnelProvider>());
builder.Services.AddSingleton<CloudflareTunnelProvider>();
builder.Services.AddSingleton<ITunnelProvider>(sp => sp.GetRequiredService<CloudflareTunnelProvider>());
builder.Services.AddSingleton<FrpTunnelProvider>();
builder.Services.AddSingleton<ITunnelProvider>(sp => sp.GetRequiredService<FrpTunnelProvider>());
builder.Services.AddSingleton<TunnelRegistry>();

if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddHostedService<SessionLogFileWatcher>();
    builder.Services.AddHostedService<VectorIndexStartupService>();
    builder.Services.AddHostedService(sp => (WorkspaceProcessManager)sp.GetRequiredService<IWorkspaceProcessManager>());
    builder.Services.AddHostedService(sp => sp.GetRequiredService<TunnelRegistry>());
}

var mvcBuilder = builder.Services.AddControllers();
#if !DEBUG
if (!builder.Environment.IsStaging())
    mvcBuilder.ConfigureApplicationPartManager(mgr =>
        mgr.FeatureProviders.Add(new ExcludeControllerFeatureProvider(typeof(DiagnosticController))));
#endif
builder.Services.AddEndpointsApiExplorer();

// MCP Streamable HTTP transport — shares FwhMcpTools with STDIO transport.
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(FwhMcpTools).Assembly);
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "MCP Context API", Version = "v1" });
});

var app = builder.Build();

var serverProcessId = Environment.ProcessId;
var serverCommandLine = Environment.CommandLine;

// Log application version at startup for deployment verification.
app.LogApplicationVersion();
app.Logger.LogInformation(
    "Server startup event: PID={ProcessId}; Command={CommandLine}",
    serverProcessId,
    serverCommandLine);

if (!app.Environment.IsEnvironment("Test"))
{
    var parseableOpts = app.Configuration.GetSection(McpParseableOptions.SectionName).Get<McpParseableOptions>() ?? new McpParseableOptions();
    Log.Information("[Serilog] File sink enabled, path: {Path}", ResolveSerilogFilePath(parseableOpts.FallbackLogPath));
    if (!string.IsNullOrWhiteSpace(parseableOpts.Url))
        Log.Information("[Parseable] Sink enabled, ingestion URL: {Url}/api/v1/ingest (X-P-Stream: {Stream})", parseableOpts.Url.TrimEnd('/'), parseableOpts.StreamName);
    else
        Log.Information("[Parseable] Sink disabled (no Url configured).");
}

if (!app.Environment.IsEnvironment("Test"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        await db.Database.MigrateAsync().ConfigureAwait(false);
    }

    // Seed built-in agent definitions on startup (idempotent).
    using (var scope = app.Services.CreateScope())
    {
        var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();
        var seededCount = await agentService.SeedBuiltInDefaultsAsync().ConfigureAwait(false);
        if (seededCount > 0)
            Log.Information("[Agents] Seeded {Count} built-in agent definitions", seededCount);
    }

    // Seed default tool buckets from configuration (idempotent — skips existing).
    using (var scope = app.Services.CreateScope())
    {
        var bucketService = scope.ServiceProvider.GetRequiredService<IToolBucketService>();
        var toolRegistryOpts = scope.ServiceProvider.GetRequiredService<IOptions<ToolRegistryOptions>>().Value;
        foreach (var entry in toolRegistryOpts.DefaultBuckets)
        {
            if (string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.Owner) || string.IsNullOrWhiteSpace(entry.Repo))
                continue;

            var result = await bucketService.AddBucketAsync(
                new BucketAddRequest(entry.Name, entry.Owner, entry.Repo, entry.Branch, entry.ManifestPath),
                default).ConfigureAwait(false);

            if (result.Success)
                Log.Information("[ToolRegistry] Seeded default bucket '{Name}' ({Owner}/{Repo})", entry.Name, entry.Owner, entry.Repo);
            else
                Log.Debug("[ToolRegistry] Default bucket '{Name}' already exists, skipping.", entry.Name);
        }
    }
}

// Marker files are written by WorkspaceProcessManager during auto-start (including the primary workspace).
// Register cleanup for the primary workspace marker on shutdown.
{
    var primaryRepoRoot = McpInstanceResolver.GetEffectiveMcpValue(app.Configuration, instanceName, "RepoRoot") ?? ".";
    var primaryWorkspacePath = Path.IsPathRooted(primaryRepoRoot)
        ? Path.GetFullPath(primaryRepoRoot)
        : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, primaryRepoRoot));

    app.Lifetime.ApplicationStopping.Register(() =>
    {
        app.Logger.LogInformation(
            "Graceful shutdown initiated: PID={ProcessId}; Command={CommandLine}",
            serverProcessId,
            serverCommandLine);
        MarkerFileService.RemoveMarker(primaryWorkspacePath);
    });

    app.Lifetime.ApplicationStopped.Register(() =>
    {
        app.Logger.LogInformation(
            "Graceful shutdown completed: PID={ProcessId}; Command={CommandLine}",
            serverProcessId,
            serverCommandLine);
    });
}

// Seed primary-host API tokens eagerly so /api-key is ready even if workspace auto-start lags.
{
    var apiKeyWorkspacePath = ResolvePrimaryApiKeyWorkspacePath(app.Configuration, app.Environment, instanceName);
    if (!string.IsNullOrWhiteSpace(apiKeyWorkspacePath))
    {
        var tokenService = app.Services.GetRequiredService<WorkspaceTokenService>();
        var fullTokenExisted = tokenService.GetToken(apiKeyWorkspacePath) is not null;
        var defaultTokenExisted = tokenService.GetDefaultToken(apiKeyWorkspacePath) is not null;

        _ = tokenService.GetToken(apiKeyWorkspacePath) ?? tokenService.GenerateToken(apiKeyWorkspacePath);
        _ = tokenService.GetDefaultToken(apiKeyWorkspacePath) ?? tokenService.GenerateDefaultToken(apiKeyWorkspacePath);

        if (!fullTokenExisted || !defaultTokenExisted)
        {
            app.Logger.LogInformation(
                "Primary host API tokens seeded: Workspace={WorkspacePath}; FullTokenExisted={FullTokenExisted}; DefaultTokenExisted={DefaultTokenExisted}",
                apiKeyWorkspacePath,
                fullTokenExisted,
                defaultTokenExisted);
        }
    }
}

// Tunnel lifecycle is managed by TunnelRegistry as an IHostedService.
// Only the shutdown hook remains for cleanup outside the hosted service scope.
app.Lifetime.ApplicationStopping.Register(() =>
    app.Services.GetRequiredService<TunnelRegistry>().StopAllAsync().GetAwaiter().GetResult());

// TR-PLANNED-013: Structured interaction logging for all requests; optional async submission to LoggingServiceUrl.
app.UseMiddleware<InteractionLoggingMiddleware>();

// Per-workspace auth tokens: protect all /mcp/* REST routes.
app.UseAuthentication();
app.UseMiddleware<WorkspaceResolutionMiddleware>();
app.UseMiddleware<WorkspaceAuthMiddleware>();
app.UseAuthorization();

app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Context API v1"));

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

// Unprotected diagnostics endpoint for stale-marker detection and client troubleshooting.
app.MapGet("/server-startup-utc", (ServerRuntimeInfo runtimeInfo) =>
    MarkerDiagnosticsEndpointHelper.GetServerStartupResult(runtimeInfo))
    .ExcludeFromDescription();

// Unprotected diagnostics endpoint returning marker file timestamps for configured workspaces.
app.MapGet("/marker-file-timestamp", (string? repoPath, IConfiguration configuration) =>
    MarkerDiagnosticsEndpointHelper.GetMarkerFileTimestampResult(
        repoPath,
        configuration,
        app.Environment.ContentRootPath,
        restrictToCurrentRepoRoot: false))
    .ExcludeFromDescription();

// Unprotected endpoint returning the default (anonymous) API key for consumers without marker file access.
app.MapGet("/api-key", (WorkspaceTokenService tokenService) =>
{
    var workspacePath = ResolvePrimaryApiKeyWorkspacePath(app.Configuration, app.Environment, instanceName) ?? string.Empty;
    if (string.IsNullOrWhiteSpace(workspacePath))
        return Results.Problem("No workspace configured.", statusCode: 503);

    var defaultToken = tokenService.GetDefaultToken(workspacePath);
    if (defaultToken is null)
    {
        defaultToken = tokenService.GenerateDefaultToken(workspacePath);
        app.Logger.LogWarning(
            "Default API token was missing during /api-key request and was generated on demand: Workspace={WorkspacePath}",
            workspacePath);
    }

    return Results.Ok(new { apiKey = defaultToken });
}).ExcludeFromDescription();

app.MapMcp("/mcp-transport");
app.MapControllers();

// /pair web login flow — authenticate to view the API key.
app.MapGet("/pair", (IOptions<PairingOptions> opts) =>
{
    var o = opts.Value;
    if (o.PairingUsers.Count == 0 || string.IsNullOrEmpty(o.ApiKey))
        return Results.Content(PairingHtml.NotConfiguredPage(), "text/html");
    return Results.Content(PairingHtml.LoginPage(), "text/html");
}).ExcludeFromDescription();

app.MapPost("/pair", async (HttpContext context, IOptions<PairingOptions> opts, PairingSessionService sessions) =>
{
    var o = opts.Value;
    if (o.PairingUsers.Count == 0 || string.IsNullOrEmpty(o.ApiKey))
        return Results.Content(PairingHtml.NotConfiguredPage(), "text/html");

    var form = await context.Request.ReadFormAsync().ConfigureAwait(false);
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    var user = o.PairingUsers.Find(u =>
        string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

    if (user is null || !VerifyPairingPassword(password, user.PasswordHash))
        return Results.Content(PairingHtml.LoginPage(error: true), "text/html");

    var token = sessions.CreateToken();
    context.Response.Cookies.Append("mcp_pair", token, new CookieOptions
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddHours(1),
    });
    return Results.Redirect("/pair/key");
}).ExcludeFromDescription();

app.MapGet("/pair/key", (HttpContext context, IOptions<PairingOptions> opts, PairingSessionService sessions) =>
{
    var token = context.Request.Cookies["mcp_pair"];
    if (!sessions.Validate(token))
        return Results.Redirect("/pair");

    var o = opts.Value;
    var request = context.Request;
    var serverUrl = $"{request.Scheme}://{request.Host}";
    return Results.Content(PairingHtml.KeyPage(o.ApiKey, serverUrl), "text/html");
}).ExcludeFromDescription();

try
{
    await app.RunAsync().ConfigureAwait(false);
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

static bool VerifyPairingPassword(string plaintext, string expectedHash)
{
    var computed = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
    var expected = Convert.FromHexString(expectedHash);
    return CryptographicOperations.FixedTimeEquals(computed, expected);
}

static void DisableEnvironmentSpecificJsonConfigForWindowsService(WebApplicationBuilder builder)
{
    if (!OperatingSystem.IsWindows() || !WindowsServiceHelpers.IsWindowsService())
        return;

    var environmentFileName = $"appsettings.{builder.Environment.EnvironmentName}.json";
    var toRemove = builder.Configuration.Sources
        .OfType<JsonConfigurationSource>()
        .Where(source =>
            string.Equals(
                Path.GetFileName(source.Path ?? string.Empty),
                environmentFileName,
                StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (toRemove.Count == 0)
        return;

    foreach (var source in toRemove)
        builder.Configuration.Sources.Remove(source);

    if (builder.Configuration is IConfigurationRoot configurationRoot)
        configurationRoot.Reload();
}

[SupportedOSPlatform("windows")]
static void ConfigureWindowsEventLogSource(WebApplicationBuilder builder)
{
#pragma warning disable CA1416
    builder.Logging.AddEventLog(settings =>
    {
        settings.SourceName = "McpServer";
        settings.LogName = "Application";
        settings.Filter = (_, level) => level >= LogLevel.Information;
    });
#pragma warning restore CA1416
}

static string ResolveSerilogFilePath(string? configuredPath)
{
    var rawPath = !string.IsNullOrWhiteSpace(configuredPath) ? configuredPath.Trim() : "logs/mcp-.log";
    return Path.IsPathRooted(rawPath)
        ? rawPath
        : Path.GetFullPath(rawPath, AppContext.BaseDirectory);
}

static void EnsureSerilogFileDirectory(string filePath)
{
    var directory = Path.GetDirectoryName(filePath);
    if (!string.IsNullOrWhiteSpace(directory))
        Directory.CreateDirectory(directory);
}

static bool HasAnyRole(ClaimsPrincipal user, params string[] requiredRoles)
{
    if (user.Identity?.IsAuthenticated != true)
        return false;

    var required = new HashSet<string>(requiredRoles, StringComparer.OrdinalIgnoreCase);
    foreach (var claim in user.Claims)
    {
        if (!string.Equals(claim.Type, "realm_roles", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(claim.Type, "roles", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (ContainsRequiredRole(claim.Value, required))
            return true;
    }

    return false;
}

static string? ResolvePrimaryApiKeyWorkspacePath(IConfiguration configuration, IHostEnvironment environment, string? instanceName)
{
    var effectiveRepoRoot = McpInstanceResolver.GetEffectiveMcpValue(configuration, instanceName, "RepoRoot");
    if (!string.IsNullOrWhiteSpace(effectiveRepoRoot))
        return NormalizeWorkspacePathForToken(effectiveRepoRoot, environment.ContentRootPath);

    var workspaces = configuration.GetSection("Mcp:Workspaces").Get<List<WorkspaceConfigEntry>>() ?? [];
    var primary = workspaces
        .Where(w => w.IsPrimary && w.IsEnabled)
        .FirstOrDefault();
    primary ??= workspaces
        .Where(w => w.IsEnabled)
        .FirstOrDefault();

    return string.IsNullOrWhiteSpace(primary?.WorkspacePath)
        ? null
        : NormalizeWorkspacePathForToken(primary.WorkspacePath, environment.ContentRootPath);
}

static string NormalizeWorkspacePathForToken(string workspacePath, string contentRootPath)
{
    var trimmed = workspacePath.Trim();
    var absolute = Path.IsPathRooted(trimmed)
        ? Path.GetFullPath(trimmed)
        : Path.GetFullPath(Path.Combine(contentRootPath, trimmed));

    return absolute.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}

static bool ContainsRequiredRole(string? claimValue, ISet<string> requiredRoles)
{
    if (string.IsNullOrWhiteSpace(claimValue))
        return false;

    var trimmed = claimValue.Trim();
    if (requiredRoles.Contains(trimmed))
        return true;

    if (trimmed.StartsWith("[", StringComparison.Ordinal))
    {
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var role = element.GetString();
                    if (!string.IsNullOrWhiteSpace(role) && requiredRoles.Contains(role))
                        return true;
                }
            }
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Trace.TraceWarning(ex.ToString());
            // Fall back to delimited parsing below.
        }
    }

    foreach (var token in trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        var normalized = token.Trim('"');
        if (requiredRoles.Contains(normalized))
            return true;
    }

    return false;
}
