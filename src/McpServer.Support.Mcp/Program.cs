// TR-PLANNED-013 / FR-SUPPORT-010: MCP Context Unification - local MCP server for Cursor and Copilot.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using McpServer.Common.Copilot.Extensions;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Logging;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        .OrderBy(w => w.WorkspacePort)
        .FirstOrDefault();
    primaryWorkspaceEntry ??= workspaces
        .Where(w => w.IsEnabled)
        .OrderBy(w => w.WorkspacePort)
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
    if (!string.IsNullOrWhiteSpace(parseable.Url) && !context.HostingEnvironment.IsEnvironment("Test"))
    {
        var ingestUri = $"{parseable.Url!.TrimEnd('/')}/api/v1/ingest";
        var httpClient = new ParseableHttpClient(parseable.StreamName, parseable.Username, parseable.Password);
        // Exclude Parseable meta-logs (success/failure of push) so they are not republished to Parseable.
        config.WriteTo.Logger(lc => lc
            .Filter.ByExcluding(e => e.Properties.TryGetValue(ParseableHttpClient.ParseableMetaPropertyName, out var v) && v is ScalarValue s && (s.Value is true or "True"))
            .WriteTo.Http(requestUri: ingestUri, queueLimitBytes: null, textFormatter: new ParseableEventFormatter(), batchFormatter: new ParseableBatchFormatter(), httpClient: httpClient, restrictedToMinimumLevel: LogEventLevel.Verbose));

        // TR-PLANNED-013: File-based fallback when publishing to Parseable fails (e.g. Parseable down).
        var fallbackPath = !string.IsNullOrWhiteSpace(parseable.FallbackLogPath) ? parseable.FallbackLogPath!.Trim() : "logs/mcp-.log";
        config.WriteTo.File(
            path: fallbackPath,
            rollingInterval: RollingInterval.Day,
            formatProvider: CultureInfo.InvariantCulture);
    }
});

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
builder.Services.Configure<EmbeddingOptions>(builder.Configuration.GetSection("Embedding"));
builder.Services.Configure<VectorIndexOptions>(builder.Configuration.GetSection("VectorIndex"));
builder.Services.AddSingleton<IInteractionLogSubmissionChannel, InteractionLogSubmissionChannel>();
builder.Services.AddHostedService<InteractionLogSubmissionService>();
builder.Services.AddHttpClient("InteractionLogSubmission");
builder.Services.AddSingleton<ISyncStatusStore, SyncStatusStore>();
builder.Services.AddSingleton<IWriteAuditLog, WriteAuditLog>();
builder.Services.AddSingleton<Chunker>();
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
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
builder.Services.AddSingleton<IIssueTodoSyncService, IssueTodoSyncService>();
builder.Services.AddSingleton<IRequirementsService, RequirementsService>();
builder.Services.AddSingleton<ITodoPromptService, TodoPromptService>();
builder.Services.Configure<TodoPromptOptions>(options =>
{
    if (primaryWorkspaceEntry is not null)
    {
        options.StatusPrompt = primaryWorkspaceEntry.StatusPrompt;
        options.ImplementPrompt = primaryWorkspaceEntry.ImplementPrompt;
        options.PlanPrompt = primaryWorkspaceEntry.PlanPrompt;
        options.BaseUrl = $"http://localhost:{primaryWorkspaceEntry.WorkspacePort}";
    }
});
builder.Services.AddCopilotClient();
builder.Services.AddScoped<ISessionLogService, SessionLogService>();
builder.Services.AddScoped<Fts5SearchService>();
builder.Services.AddScoped<IContextSearchService, HybridSearchService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IToolRegistryService, ToolRegistryService>();
builder.Services.AddScoped<IToolBucketService, ToolBucketService>();
builder.Services.AddSingleton<WorkspaceTokenService>();
builder.Services.AddSingleton<IWorkspaceProcessManager, WorkspaceProcessManager>();
builder.Services.Configure<PairingOptions>(builder.Configuration.GetSection(PairingOptions.SectionName));
builder.Services.Configure<ToolRegistryOptions>(builder.Configuration.GetSection(ToolRegistryOptions.SectionName));
builder.Services.AddSingleton<PairingSessionService>();

// Tunnel strategy pattern — follows ITodoService provider-switch convention.
var tunnelProvider = (builder.Configuration
    .GetSection(TunnelOptions.SectionName)
    .Get<TunnelOptions>()?.Provider ?? "")
    .Trim().ToUpperInvariant();

if (!string.IsNullOrEmpty(tunnelProvider))
{
    builder.Services.Configure<TunnelOptions>(
        builder.Configuration.GetSection(TunnelOptions.SectionName));
    builder.Services.AddSingleton<ITunnelProvider>(sp => tunnelProvider switch
    {
        "NGROK" => ActivatorUtilities.CreateInstance<NgrokTunnelProvider>(sp),
        "CLOUDFLARE" => ActivatorUtilities.CreateInstance<CloudflareTunnelProvider>(sp),
        "FRP" => ActivatorUtilities.CreateInstance<FrpTunnelProvider>(sp),
        _ => throw new InvalidOperationException($"Unknown tunnel provider: {tunnelProvider}"),
    });
}

if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddHostedService<SessionLogFileWatcher>();
    builder.Services.AddHostedService<VectorIndexStartupService>();
    builder.Services.AddHostedService(sp => (WorkspaceProcessManager)sp.GetRequiredService<IWorkspaceProcessManager>());

    if (!string.IsNullOrEmpty(tunnelProvider))
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ITunnelProvider>());
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

// Log application version at startup for deployment verification.
app.LogApplicationVersion();

if (!app.Environment.IsEnvironment("Test"))
{
    var parseableOpts = app.Configuration.GetSection(McpParseableOptions.SectionName).Get<McpParseableOptions>() ?? new McpParseableOptions();
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
        MarkerFileService.RemoveMarker(primaryWorkspacePath);
    });
}

// TR-PLANNED-013: Structured interaction logging for all requests; optional async submission to LoggingServiceUrl.
app.UseMiddleware<InteractionLoggingMiddleware>();

// Per-workspace auth tokens: protect all /mcp/* REST routes.
app.UseMiddleware<WorkspaceAuthMiddleware>();

app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Context API v1"));

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

// Unprotected endpoint returning the default (anonymous) API key for consumers without marker file access.
app.MapGet("/api-key", (WorkspaceTokenService tokenService, IConfiguration configuration) =>
{
    var workspacePath = configuration["Mcp:RepoRoot"] ?? string.Empty;
    if (string.IsNullOrWhiteSpace(workspacePath))
        return Results.Problem("No workspace configured.", statusCode: 503);

    var key = Path.GetFullPath(workspacePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    var defaultToken = tokenService.GetDefaultToken(key);
    if (defaultToken is null)
        return Results.Problem("Default token not yet generated. Retry shortly.", statusCode: 503);

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
