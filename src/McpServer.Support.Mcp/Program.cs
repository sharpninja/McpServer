// TR-PLANNED-013 / FR-SUPPORT-010: MCP Context Unification - local MCP server for Cursor and Copilot.

using System.Globalization;
using McpServer.Common.Copilot.Extensions;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Logging;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
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
    builder.Host.UseWindowsService();
}

var instanceName = McpInstanceResolver.GetRequestedInstanceName(args);
McpInstanceResolver.ValidateInstances(builder.Configuration);

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
builder.Services.AddCopilotClient();
builder.Services.AddScoped<ISessionLogService, SessionLogService>();
builder.Services.AddScoped<Fts5SearchService>();
builder.Services.AddScoped<IContextSearchService, HybridSearchService>();

if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddHostedService<SessionLogFileWatcher>();
    builder.Services.AddHostedService<VectorIndexStartupService>();
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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
}

// TR-PLANNED-013: Structured interaction logging for all requests; optional async submission to LoggingServiceUrl.
app.UseMiddleware<InteractionLoggingMiddleware>();

app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Context API v1"));

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapControllers();

try
{
    await app.RunAsync().ConfigureAwait(false);
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}
