// TR-PLANNED-013 / FR-SUPPORT-010: MCP Context Unification - local MCP server for Cursor and Copilot.

using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using McpServer.Common.Copilot;
using McpServer.Common.Copilot.Extensions;
using McpServer.GraphRag;
using McpServer.Support.Mcp.DatabaseMaintenance;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Logging;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using McpServer.Support.Mcp.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Http.Resilience;
using NetEscapades.Configuration.Yaml;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File;

if (IsStdioTransportRequested(args))
{
    await McpStdioHost.RunAsync(args, default).ConfigureAwait(false);
    return;
}

if (McpDatabaseEncryptionTransitionCommand.TryParse(args, out var transitionCommand, out var transitionParseError))
{
    if (transitionCommand is null)
    {
        Console.Error.WriteLine(transitionParseError ?? McpDatabaseEncryptionTransitionCommand.GetUsageText());
        Environment.ExitCode = 1;
        return;
    }

    var exitCode = await McpDatabaseEncryptionTransitionCommand
        .RunAsync(transitionCommand, default)
        .ConfigureAwait(false);
    Environment.ExitCode = exitCode;
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
EnsureApprovedWindowsServiceDeployment();
DisableEnvironmentSpecificJsonConfigForWindowsService(builder);

builder.Configuration.AddYamlFile("appsettings.yaml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", optional: true, reloadOnChange: true);
// Re-apply operational overrides after YAML so env vars and CLI args beat repo defaults.
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

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
    {
        builder.Environment.ContentRootPath = Path.GetFullPath(primaryWorkspaceEntry.WorkspacePath);
        Directory.SetCurrentDirectory(builder.Environment.ContentRootPath);
    }
}

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

    // if (parseable.Enabled && !string.IsNullOrWhiteSpace(parseable.Url) && !context.HostingEnvironment.IsEnvironment("Test"))
    // {
    //     var ingestUri = $"{parseable.Url!.TrimEnd('/')}/api/v1/ingest";
    //     var httpClient = new ParseableHttpClient(parseable.StreamName, parseable.Username, parseable.Password);
    //     config.WriteTo.Logger(lc => lc
    //         .Filter.ByExcluding(e => e.Properties.TryGetValue(ParseableHttpClient.ParseableMetaPropertyName, out var v) && v is ScalarValue s && (s.Value is true or "True"))
    //         .WriteTo.Http(requestUri: ingestUri, queueLimitBytes: null, textFormatter: new ParseableEventFormatter(), batchFormatter: new ParseableBatchFormatter(), httpClient: httpClient, restrictedToMinimumLevel: LogEventLevel.Verbose));
    // }
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
builder.Services.AddConfiguredMcpDbContext(builder.Configuration, instanceName, builder.Environment.IsEnvironment("Test"));

builder.Services.Configure<IngestionOptions>(builder.Configuration.GetSection("Mcp"));
builder.Services.Configure<GraphRagOptions>(builder.Configuration.GetSection(GraphRagOptions.SectionName));
builder.Services.Configure<MarkerPromptOptions>(builder.Configuration.GetSection(MarkerPromptOptions.SectionName));
builder.Services.Configure<McpParseableOptions>(builder.Configuration.GetSection(McpParseableOptions.SectionName));
builder.Services.Configure<McpInteractionLoggingOptions>(builder.Configuration.GetSection(McpInteractionLoggingOptions.SectionName));
builder.Services.Configure<TodoStorageOptions>(builder.Configuration.GetSection(TodoStorageOptions.SectionName));
builder.Services.Configure<GitHubIntegrationOptions>(builder.Configuration.GetSection(GitHubIntegrationOptions.SectionName));
builder.Services.Configure<AgentPoolOptions>(builder.Configuration.GetSection(AgentPoolOptions.SectionName));
builder.Services.Configure<VoiceConversationOptions>(builder.Configuration.GetSection(VoiceConversationOptions.SectionName));
builder.Services.Configure<RequirementsOptions>(builder.Configuration.GetSection(RequirementsOptions.SectionName));
builder.Services.Configure<AgentProcessManagerOptions>(builder.Configuration.GetSection(AgentProcessManagerOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<AgentPoolOptions>, AgentPoolOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<VoiceConversationOptions>, VoiceConversationOptionsValidator>();
builder.Services.AddSingleton<AppSettingsFileService>();
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
// builder.Services.AddHostedService<InteractionLogSubmissionService>();
builder.Services.AddHttpClient("InteractionLogSubmission");
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
builder.Services.AddSingleton<ISyncStatusStore, SyncStatusStore>();
builder.Services.AddSingleton<IWriteAuditLog, WriteAuditLog>();
builder.Services.AddSingleton<IChangeEventBus, ChannelChangeEventBus>();
builder.Services.AddSingleton<Chunker>();
builder.Services.AddDataProtection();
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<IAgentProcessManager, AgentProcessManager>();
builder.Services.AddSingleton<IAgentIsolationStrategy, NoneAgentIsolationStrategy>();
builder.Services.AddSingleton<IAgentIsolationStrategy, WorktreeAgentIsolationStrategy>();
builder.Services.AddSingleton<IAgentIsolationStrategy, CloneAgentIsolationStrategy>();
builder.Services.AddSingleton<AgentIsolationStrategyResolver>();
builder.Services.AddSingleton<IAgentBranchStrategy, DirectAgentBranchStrategy>();
builder.Services.AddSingleton<IAgentBranchStrategy, FeatureAgentBranchStrategy>();
builder.Services.AddSingleton<IAgentBranchStrategy, WorktreeAgentBranchStrategy>();
builder.Services.AddSingleton<AgentBranchStrategyResolver>();
builder.Services.AddHostedService<AgentHealthMonitorService>();
builder.Services.AddSingleton<IGitHubWorkspaceTokenStore, FileGitHubWorkspaceTokenStore>();
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
builder.Services.AddScoped<IWebsiteIngestor, WebsiteIngestor>();
builder.Services.AddScoped<IngestionCoordinator>();
builder.Services.AddScoped<IRepoFileService, RepoFileService>();
builder.Services.AddScoped<DesktopLaunchService>();
builder.Services.AddSingleton<IGitHubCliService, GitHubCliService>();
builder.Services.AddSingleton<ITodoServiceFactory, TodoServiceFactory>();
builder.Services.AddSingleton<ITodoService>(sp => sp.GetRequiredService<ITodoServiceFactory>().CreatePrimary());
builder.Services.AddSingleton<TodoServiceResolver>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<WorkspaceServiceAccessor>();
builder.Services.AddSingleton<TodoCreationService>();
builder.Services.AddSingleton<IIssueTodoSyncService, IssueTodoSyncService>();
builder.Services.AddSingleton<TodoUpdateService>();
builder.Services.AddSingleton<IRequirementsService, RequirementsService>();
builder.Services.AddSingleton<RequirementsDocumentService>();
builder.Services.AddSingleton<IRequirementsRepository>(sp => sp.GetRequiredService<RequirementsDocumentService>());
builder.Services.AddSingleton<IRequirementsDocumentService>(sp => sp.GetRequiredService<RequirementsDocumentService>());
builder.Services.AddSingleton<ITodoPromptService, TodoPromptService>();
builder.Services.AddAgentExecutionStrategies();
builder.Services.AddSingleton<IVoiceConversationService, VoiceConversationService>();
builder.Services.AddSingleton<IAgentPoolService, AgentPoolService>();
builder.Services.AddSingleton<PromptTemplateRenderer>();
builder.Services.Configure<TemplateStorageOptions>(builder.Configuration.GetSection(TemplateStorageOptions.SectionName));
builder.Services.AddSingleton<IPromptTemplateService, PromptTemplateService>();
builder.Services.AddSingleton<IMarkerPromptProvider, FileMarkerPromptProvider>();
builder.Services.AddSingleton<ITodoPromptProvider, TodoPromptProvider>();
builder.Services.AddSingleton<PairingHtmlRenderer>();
builder.Services.Configure<TodoPromptOptions>(options =>
{
    if (primaryWorkspaceEntry is not null)
    {
        options.StatusPrompt = string.IsNullOrWhiteSpace(primaryWorkspaceEntry.StatusPrompt) ? null : primaryWorkspaceEntry.StatusPrompt;
        options.ImplementPrompt = string.IsNullOrWhiteSpace(primaryWorkspaceEntry.ImplementPrompt) ? null : primaryWorkspaceEntry.ImplementPrompt;
        options.PlanPrompt = string.IsNullOrWhiteSpace(primaryWorkspaceEntry.PlanPrompt) ? null : primaryWorkspaceEntry.PlanPrompt;
        options.BaseUrl = $"http://{System.Net.Dns.GetHostName()}:{listenPort}";
        options.RunAs = primaryWorkspaceEntry.RunAs;
        options.GitHubToken = primaryWorkspaceEntry.GitHubToken;
        options.AgentPath = primaryWorkspaceEntry.AgentPath;
    }
});
builder.Services.AddSingleton<IProcessSpawner, DesktopProcessSpawner>();
builder.Services.AddCopilotClient();
builder.Services.RemoveAll<ICopilotClient>();
builder.Services.AddSingleton<ICopilotClient>(sp =>
    new AuditedCopilotClient(
        sp.GetRequiredService<CopilotClient>(),
        sp.GetRequiredService<IServiceScopeFactory>(),
        sp.GetRequiredService<IHttpContextAccessor>(),
        sp.GetRequiredService<IOptions<IngestionOptions>>(),
        sp.GetRequiredService<ILogger<AuditedCopilotClient>>()));
builder.Services.AddScoped<ISessionLogService, SessionLogService>();
builder.Services.AddScoped<Fts5SearchService>();
builder.Services.AddScoped<IContextSearchService, HybridSearchService>();
builder.Services.AddMcpGraphRag();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IWorkspacePolicyDirectiveParser, WorkspacePolicyDirectiveParser>();
builder.Services.AddScoped<IWorkspacePolicyService, WorkspacePolicyService>();
builder.Services.AddScoped<IToolRegistryService, ToolRegistryService>();
builder.Services.AddScoped<IToolBucketService, ToolBucketService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddSingleton<WorkspaceTokenService>();
builder.Services.AddSingleton<ApiKeyIssuanceGuard>();
builder.Services.AddScoped<WorkspaceContext>();
builder.Services.AddSingleton(new ServerRuntimeInfo(serverStartupUtc, listenPort));
builder.Services.AddSingleton<IWorkspaceProcessManager, WorkspaceProcessManager>();
builder.Services.Configure<DesktopLaunchOptions>(builder.Configuration.GetSection(DesktopLaunchOptions.SectionName));
builder.Services.Configure<PairingOptions>(builder.Configuration.GetSection(PairingOptions.SectionName));
builder.Services.Configure<OidcAuthOptions>(builder.Configuration.GetSection(OidcAuthOptions.SectionName));
builder.Services.Configure<ToolRegistryOptions>(builder.Configuration.GetSection(ToolRegistryOptions.SectionName));
builder.Services.AddSingleton<PairingLoginAttemptGuard>();
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
    builder.Services.AddHostedService<AgentPoolSeedService>();
}

var mvcBuilder = builder.Services.AddControllers();
#if !DEBUG
if (!builder.Environment.IsStaging())
    mvcBuilder.ConfigureApplicationPartManager(mgr =>
        mgr.FeatureProviders.Add(new ExcludeControllerFeatureProvider(typeof(DiagnosticController))));
#endif
builder.Services.AddEndpointsApiExplorer();

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

app.LogApplicationVersion();
app.Logger.LogInformation(
    "Server startup event: PID={ProcessId}; Command={CommandLine}",
    serverProcessId,
    serverCommandLine);

if (!app.Environment.IsEnvironment("Test"))
{
    var parseableOpts = app.Configuration.GetSection(McpParseableOptions.SectionName).Get<McpParseableOptions>() ?? new McpParseableOptions();
    Log.Information("[Serilog] File sink enabled, path: {Path}", ResolveSerilogFilePath(parseableOpts.FallbackLogPath));
    if (parseableOpts.Enabled && !string.IsNullOrWhiteSpace(parseableOpts.Url))
        Log.Information("[Parseable] Sink enabled, ingestion URL: {Url}/api/v1/ingest (X-P-Stream: {Stream})", parseableOpts.Url.TrimEnd('/'), parseableOpts.StreamName);
    else
        Log.Information("[Parseable] Sink disabled (Enabled={Enabled}, Url configured: {HasUrl}).", parseableOpts.Enabled, !string.IsNullOrWhiteSpace(parseableOpts.Url));
}

if (!app.Environment.IsEnvironment("Test"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var runtimeOptions = scope.ServiceProvider.GetRequiredService<McpDatabaseRuntimeOptions>();
        await McpDatabaseMigrationCoordinator.ApplyMigrationsAsync(db, runtimeOptions.ProviderOptions).ConfigureAwait(false);
        await McpDatabaseEncryptionCoordinator.ValidateAsync(db, runtimeOptions).ConfigureAwait(false);
    }

    using (var scope = app.Services.CreateScope())
    {
        var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();
        var seededCount = await agentService.SeedBuiltInDefaultsAsync().ConfigureAwait(false);
        if (seededCount > 0)
            Log.Information("[Agents] Seeded {Count} built-in agent definitions", seededCount);
    }

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

app.UseGlobalExceptionHandler();
app.UseMiddleware<InteractionLoggingMiddleware>();

app.UseAuthentication();
app.UseMiddleware<WorkspaceResolutionMiddleware>();
app.UseMiddleware<WorkspaceAuthMiddleware>();
app.UseAuthorization();

app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Context API v1"));

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapGet("/server-startup-utc", (ServerRuntimeInfo runtimeInfo) =>
    MarkerDiagnosticsEndpointHelper.GetServerStartupResult(runtimeInfo))
    .ExcludeFromDescription();

app.MapGet("/marker-file-timestamp", (string? repoPath, IConfiguration configuration) =>
    MarkerDiagnosticsEndpointHelper.GetMarkerFileTimestampResult(
        repoPath,
        configuration,
        app.Environment.ContentRootPath,
        restrictToCurrentRepoRoot: false))
    .ExcludeFromDescription();

app.MapGet("/api-key", (HttpContext context, WorkspaceTokenService tokenService, ApiKeyIssuanceGuard apiKeyIssuanceGuard) =>
{
    if (!IsLoopbackRequest(context))
    {
        app.Logger.LogWarning(
            "Rejected non-loopback /api-key request: RemoteIp={RemoteIp}",
            context.Connection.RemoteIpAddress?.ToString() ?? "(none)");
        return Results.NotFound();
    }

    context.Response.Headers.CacheControl = "no-store, no-cache";
    context.Response.Headers.Pragma = "no-cache";

    var workspacePath = ResolvePrimaryApiKeyWorkspacePath(app.Configuration, app.Environment, instanceName) ?? string.Empty;
    if (string.IsNullOrWhiteSpace(workspacePath))
        return Results.Problem("No workspace configured.", statusCode: 503);

    var defaultToken = tokenService.GetDefaultToken(workspacePath);
    if (defaultToken is null)
    {
        app.Logger.LogError(
            "Default API token unavailable during /api-key request: Workspace={WorkspacePath}",
            workspacePath);
        return Results.Problem("Default API token unavailable.", statusCode: 503);
    }

    if (!apiKeyIssuanceGuard.TryAcquire(context.Connection.RemoteIpAddress, out var retryAfter))
    {
        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        app.Logger.LogWarning(
            "Default API token issuance throttled: RemoteIp={RemoteIp}; Workspace={WorkspacePath}; RetryAfterSeconds={RetryAfterSeconds}",
            context.Connection.RemoteIpAddress?.ToString() ?? "loopback",
            workspacePath,
            retryAfterSeconds);
        return Results.Problem("Default API token issuance is temporarily rate-limited.", statusCode: 429);
    }

    app.Logger.LogInformation(
        "Default API token issued: RemoteIp={RemoteIp}; Workspace={WorkspacePath}",
        context.Connection.RemoteIpAddress?.ToString() ?? "loopback",
        workspacePath);
    return Results.Ok(new { apiKey = defaultToken });
}).ExcludeFromDescription();

app.MapMcp("/mcp-transport");
app.MapControllers();

app.MapGet("/pair", async (IOptions<PairingOptions> opts, PairingHtmlRenderer pairingRenderer) =>
{
    var o = opts.Value;
    if (o.PairingUsers.Count == 0 || string.IsNullOrEmpty(o.ApiKey))
        return Results.Content(await pairingRenderer.RenderNotConfiguredPageAsync().ConfigureAwait(false), "text/html");
    return Results.Content(await pairingRenderer.RenderLoginPageAsync().ConfigureAwait(false), "text/html");
}).ExcludeFromDescription();

app.MapPost("/pair", async (HttpContext context, IOptions<PairingOptions> opts, PairingSessionService sessions, PairingLoginAttemptGuard attemptGuard, PairingHtmlRenderer pairingRenderer) =>
{
    var o = opts.Value;
    if (o.PairingUsers.Count == 0 || string.IsNullOrEmpty(o.ApiKey))
        return Results.Content(await pairingRenderer.RenderNotConfiguredPageAsync().ConfigureAwait(false), "text/html");

    var form = await context.Request.ReadFormAsync().ConfigureAwait(false);
    var username = form["username"].ToString().Trim();
    var password = form["password"].ToString();
    var remoteIp = context.Connection.RemoteIpAddress;

    if (!attemptGuard.TryAcquire(username, remoteIp, out var retryAfter))
    {
        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        app.Logger.LogWarning(
            "Pairing sign-in blocked after repeated failures: RemoteIp={RemoteIp}; Username={Username}; RetryAfterSeconds={RetryAfterSeconds}",
            remoteIp?.ToString() ?? "loopback",
            username,
            retryAfterSeconds);
        return Results.Content(
            await pairingRenderer.RenderLoginPageAsync("Too many failed sign-in attempts. Please wait and try again.").ConfigureAwait(false),
            "text/html",
            Encoding.UTF8,
            StatusCodes.Status429TooManyRequests);
    }

    var user = o.PairingUsers.Find(u =>
        string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

    if (user is null || !VerifyPairingPassword(password, user.PasswordHash))
    {
        attemptGuard.RecordFailure(username, remoteIp);
        app.Logger.LogWarning(
            "Pairing sign-in failed: RemoteIp={RemoteIp}; Username={Username}",
            remoteIp?.ToString() ?? "loopback",
            username);
        return Results.Content(
            await pairingRenderer.RenderLoginPageAsync("Invalid username or password.").ConfigureAwait(false),
            "text/html");
    }

    attemptGuard.RecordSuccess(username, remoteIp);
    app.Logger.LogInformation(
        "Pairing sign-in succeeded: RemoteIp={RemoteIp}; Username={Username}",
        remoteIp?.ToString() ?? "loopback",
        username);
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

app.MapGet("/pair/key", async (HttpContext context, IOptions<PairingOptions> opts, PairingSessionService sessions, PairingHtmlRenderer pairingRenderer) =>
{
    var token = context.Request.Cookies["mcp_pair"];
    if (!sessions.Validate(token))
        return Results.Redirect("/pair");

    var o = opts.Value;
    var request = context.Request;
    var serverUrl = $"{request.Scheme}://{request.Host}";
    return Results.Content(await pairingRenderer.RenderKeyPageAsync(o.ApiKey, serverUrl).ConfigureAwait(false), "text/html");
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

static bool IsLoopbackRequest(HttpContext context)
{
    var remoteIp = context.Connection.RemoteIpAddress;
    return remoteIp is null || IPAddress.IsLoopback(remoteIp);
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

static void EnsureApprovedWindowsServiceDeployment()
{
    if (!OperatingSystem.IsWindows())
        return;

    if (!WindowsServiceHelpers.IsWindowsService() &&
        !WindowsServiceDeploymentGuard.HasDeploymentManifest(AppContext.BaseDirectory))
        return;
    WindowsServiceDeploymentGuard.EnsureApprovedDeployment(AppContext.BaseDirectory, WriteWindowsServiceDeploymentFailure);
}

[SupportedOSPlatform("windows")]
static void WriteWindowsServiceDeploymentFailure(string message)
{
    try
    {
#pragma warning disable CA1416
        if (!System.Diagnostics.EventLog.SourceExists("McpServer"))
        {
            System.Diagnostics.EventLog.CreateEventSource("McpServer", "Application");
        }

        System.Diagnostics.EventLog.WriteEntry(
            "McpServer",
            message,
            System.Diagnostics.EventLogEntryType.Error,
            1001);
#pragma warning restore CA1416
    }
    catch
    {
        Console.Error.WriteLine(message);
    }
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
