// TR-PLANNED-CORE-013: Runs the MCP server over STDIO (stdin/stdout JSON-RPC) when --transport stdio.

using McpServer.Support.Mcp.Extensions;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Common.AgentCli;
using McpServer.Common.AgentCli.Extensions;
using McpServer.Cqrs;
using McpServer.GraphRag;
using McpServer.Support.Mcp.UseCases;
using McpServer.Support.Mcp.Products;
using McpServer.SessionLog.Transcripts;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.McpStdio;

/// <summary>
/// TR-PLANNED-CORE-013: Host for MCP STDIO transport; registers shared services and runs MCP server.
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

        builder.Services.AddConfiguredMcpDbContext(builder.Configuration, instanceName, builder.Environment.IsEnvironment("Test"));

        builder.Services.Configure<IngestionOptions>(builder.Configuration.GetSection("Mcp"));
        builder.Services.Configure<GraphRagOptions>(builder.Configuration.GetSection(GraphRagOptions.SectionName));
        builder.Services.Configure<TodoStorageOptions>(builder.Configuration.GetSection(TodoStorageOptions.SectionName));
        builder.Services.Configure<GitHubIntegrationOptions>(builder.Configuration.GetSection(GitHubIntegrationOptions.SectionName));
        builder.Services.Configure<TodoPromptOptions>(builder.Configuration.GetSection(TodoPromptOptions.SectionName));
        builder.Services.Configure<TemplateStorageOptions>(builder.Configuration.GetSection(TemplateStorageOptions.SectionName));
        builder.Services.Configure<RequirementsOptions>(builder.Configuration.GetSection(RequirementsOptions.SectionName));
        builder.Services.Configure<BrainSlotOptions>(builder.Configuration.GetSection(BrainSlotOptions.SectionName));
        builder.Services.Configure<TriageOptions>(builder.Configuration.GetSection(TriageOptions.SectionName));
        builder.Services.Configure<DesktopLaunchOptions>(builder.Configuration.GetSection(DesktopLaunchOptions.SectionName));
        builder.Services.Configure<AgentPoolOptions>(builder.Configuration.GetSection(AgentPoolOptions.SectionName));
        builder.Services.Configure<VoiceConversationOptions>(builder.Configuration.GetSection(VoiceConversationOptions.SectionName));
        builder.Services.Configure<TodoPromptOptions>(builder.Configuration.GetSection(TodoPromptOptions.SectionName));
        builder.Services.AddOptions<SessionLogSanitizationOptions>()
            .Bind(builder.Configuration.GetSection(SessionLogSanitizationOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<SessionLogSanitizationOptions>, SessionLogSanitizationOptionsValidator>();
        var requiredRepoAllowlistPatterns = new[]
        {
            "src/McpServer.Cqrs/**/*.cs",
            "src/McpServer.Cqrs.Mvvm/**/*.cs",
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
            if (string.Equals(options.Provider, TodoStorageOptions.LegacySqliteAlias, StringComparison.OrdinalIgnoreCase))
                options.Provider = TodoStorageOptions.DatabaseProvider;
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
        builder.Services.AddSingleton<IChangeEventBus, ChannelChangeEventBus>();
        builder.Services.AddSingleton<WorkspaceTokenService>();
        builder.Services.AddSingleton<ApiKeyIssuanceGuard>();
        builder.Services.AddSingleton(new ServerRuntimeInfo(DateTimeOffset.UtcNow, 0));
        AddStdioTransactionSecurity(builder.Services, builder.Configuration);
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
        builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();
        builder.Services.AddSingleton<IVectorIndexService, VectorIndexService>();
        builder.Services.AddDataProtection();
        builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
        builder.Services.AddSingleton<IRequirementsDocFxWorkflowRunner, RequirementsDocFxWorkflowRunner>();
        builder.Services.AddSingleton<IRequirementsWikiExportOrchestrator, RequirementsWikiExportOrchestrator>();
        builder.Services.AddSingleton<IProcessSpawner, DefaultProcessSpawner>();
        builder.Services.AddSingleton<FileGitHubWorkspaceTokenStore>();
        builder.Services.AddSingleton<IGitHubWorkspaceTokenStore>(sp =>
            new TransactionGatedGitHubWorkspaceTokenStore(
                sp.GetRequiredService<FileGitHubWorkspaceTokenStore>(),
                sp.GetService<ITurnTransactionCoordinator>(),
                sp.GetService<IOptions<TurnTransactionOptions>>()));
        builder.Services.AddSingleton<GitHubCliService>();
        builder.Services.AddSingleton<IGitHubCliService>(sp =>
            new TransactionGatedGitHubCliService(
                sp.GetRequiredService<GitHubCliService>(),
                sp.GetService<ITurnTransactionCoordinator>(),
                sp.GetService<IOptions<TurnTransactionOptions>>()));
        builder.Services.AddSingleton<ITodoServiceFactory, TodoServiceFactory>();
        builder.Services.AddSingleton<ITodoService>(sp => sp.GetRequiredService<ITodoServiceFactory>().CreatePrimary());
        builder.Services.AddSingleton<TodoServiceResolver>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<WorkspaceServiceAccessor>();
        builder.Services.AddSingleton<TodoCreationService>();
        builder.Services.AddSingleton<IssueTodoSyncService>();
        builder.Services.AddSingleton<IIssueTodoSyncService>(sp =>
            new TransactionGatedIssueTodoSyncService(
                sp.GetRequiredService<IssueTodoSyncService>(),
                sp.GetService<ITurnTransactionCoordinator>(),
                sp.GetService<IOptions<TurnTransactionOptions>>()));
        builder.Services.AddSingleton<TodoUpdateService>();
        builder.Services.AddScoped<ITransactionGatedTodoMutationService, TransactionGatedTodoMutationService>();
        builder.Services.AddScoped<ITriageTodoCreator, TransactionGatedTriageTodoCreator>();
        builder.Services.AddScoped<TodoExecutionService>();
        builder.Services.AddScoped<ITodoExecutionService>(sp =>
        {
            var service = sp.GetRequiredService<TodoExecutionService>();
            return new TransactionGatedTodoExecutionService(
                service,
                service,
                sp.GetService<ITurnTransactionCoordinator>(),
                sp.GetService<IOptions<TurnTransactionOptions>>());
        });
        builder.Services.AddSingleton<IRequirementsService, RequirementsService>();
        builder.Services.AddSingleton<RequirementsDatabaseDocumentService>();
        builder.Services.AddSingleton<IRequirementsDocumentService>(sp =>
        {
            var service = sp.GetRequiredService<RequirementsDatabaseDocumentService>();
            return new TransactionGatedRequirementsDocumentService(
                service,
                service,
                sp.GetService<ITurnTransactionCoordinator>(),
                sp.GetService<IOptions<TurnTransactionOptions>>());
        });
        builder.Services.AddSingleton<IRequirementsRepository>(sp => sp.GetRequiredService<IRequirementsDocumentService>());
        builder.Services.AddSingleton<PromptTemplateRenderer>();
        builder.Services.AddSingleton<PromptTemplateService>();
        builder.Services.AddSingleton<IPromptTemplateService>(sp =>
        {
            var service = sp.GetRequiredService<PromptTemplateService>();
            return new TransactionGatedPromptTemplateService(
                service,
                service,
                sp.GetService<ITurnTransactionCoordinator>(),
                sp.GetService<IOptions<TurnTransactionOptions>>());
        });
        builder.Services.AddSingleton<ITodoPromptProvider, TodoPromptProvider>();
        builder.Services.AddSingleton<ITodoPromptService, TodoPromptService>();
        builder.Services.AddAgentCliClient();
        builder.Services.RemoveAll<IAgentCliClient>();
        builder.Services.AddSingleton<IAgentCliClient>(sp =>
            new AuditedAgentCliClient(
                sp.GetRequiredService<AgentCliClient>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IHttpContextAccessor>(),
                sp.GetRequiredService<IOptions<IngestionOptions>>(),
                sp.GetRequiredService<ILogger<AuditedAgentCliClient>>()));
        builder.Services.AddAgentExecutionStrategies();
        builder.Services.AddAgentHelpServices(builder.Configuration);
        builder.Services.AddTriageServices();
        builder.Services.AddSingleton<VoiceConversationService>();
        builder.Services.AddSingleton<IVoiceConversationService>(sp =>
            new TransactionGatedVoiceConversationService(
                sp.GetRequiredService<VoiceConversationService>(),
                sp.GetService<ITurnTransactionCoordinator>(),
                sp.GetService<IOptions<TurnTransactionOptions>>()));
        builder.Services.AddSingleton<AgentPoolService>();
        builder.Services.AddSingleton<IAgentPoolService>(sp =>
            new TransactionGatedAgentPoolService(
                sp.GetRequiredService<AgentPoolService>(),
                sp.GetService<ITurnTransactionCoordinator>(),
                sp.GetService<IOptions<TurnTransactionOptions>>()));
        builder.Services.AddHandoffServices();
        builder.Services.AddScoped<RepoIngestor>();
        builder.Services.AddScoped<SessionLogIngestor>();
        builder.Services.AddScoped<ITranscriptSessionPersister, TranscriptSessionLogPersister>();
        builder.Services.AddScoped<ITranscriptIngestionService>(sp => TranscriptIngestionService.CreateDefault(sp.GetRequiredService<ITranscriptSessionPersister>()));
        builder.Services.AddScoped<ExternalDocsIngestor>();
        builder.Services.AddScoped<GitHubIngestor>();
        builder.Services.AddScoped<IssueIngestor>();
        builder.Services.AddScoped<IWebsiteIngestor, WebsiteIngestor>();
        builder.Services.AddScoped<IngestionCoordinator>();
        builder.Services.AddScoped<RepoFileService>();
        builder.Services.AddScoped<IRepoFileService>(sp =>
        {
            var service = sp.GetRequiredService<RepoFileService>();
            return new TransactionGatedRepoFileService(
                service,
                service,
                sp.GetService<ITurnTransactionCoordinator>(),
                sp.GetService<IOptions<TurnTransactionOptions>>());
        });
        builder.Services.AddScoped<DesktopLaunchService>();
        builder.Services.AddScoped<ISessionLogSanitizer, SessionLogSanitizer>();
        builder.Services.AddSingleton<SessionLogTurnContextExtractor>();
        builder.Services.AddScoped<ISessionLogTurnContextBackfill, SessionLogTurnContextBackfill>();
        builder.Services.AddScoped<ISessionLogService>(sp =>
        {
            var inner = ActivatorUtilities.CreateInstance<SessionLogService>(sp);
            var gated = new TransactionGatedSessionLogService(
                inner,
                sp.GetRequiredService<McpDbContext>(),
                sp.GetService<ITurnTransactionCoordinator>(),
                sp.GetService<WorkspaceContext>(),
                sp.GetService<IOptions<TurnTransactionOptions>>());
            return new SessionLogSanitizingService(
                gated,
                sp.GetRequiredService<ISessionLogSanitizer>());
        });
        builder.Services.AddScoped<IMemoryService, MemoryService>();
        builder.Services.AddScoped<ITransactionGatedMemoryService, TransactionGatedMemoryService>();
        builder.Services.AddScoped<IBrainSlotCredentialResolver, BrainSlotCredentialResolver>();
        builder.Services.AddScoped<IBrainSlotChatClientFactory, BrainSlotChatClientFactory>();
        builder.Services.AddScoped<IBrainSlotRegistryService, BrainSlotRegistryService>();
        builder.Services.AddScoped<IBrainSlotContextAdmissionService, BrainSlotContextAdmissionService>();
        builder.Services.AddScoped<IBrainSlotInvocationService, BrainSlotInvocationService>();
        builder.Services.AddScoped<IQuadBrainOrchestrationService, QuadBrainOrchestrationService>();
        builder.Services.AddScoped<Fts5SearchService>();
        builder.Services.AddScoped<IContextSearchService, Fts5SearchService>();
        builder.Services.AddMcpGraphRag();
        // TR-MCP-USECASE-002 / TR-MCP-CQRS-001: Dispatcher required by usecase_* tools and handlers.
        builder.Services.AddCqrsDispatcher();
        builder.Services.AddUseCaseCqrs();
        builder.Services.AddProductCqrs();
        DecorateGraphRagService(builder.Services);
        builder.Services.AddScoped<WorkspaceContext>();
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
            var runtimeOptions = scope.ServiceProvider.GetRequiredService<McpDatabaseRuntimeOptions>();
            await McpDatabaseMigrationCoordinator.ApplyMigrationsAsync(db, runtimeOptions.ProviderOptions, cancellationToken).ConfigureAwait(false);
            await McpDatabaseEncryptionCoordinator.ValidateAsync(db, runtimeOptions, cancellationToken).ConfigureAwait(false);
            await SessionLogTurnContextBackfillStartup.TryRunAsync(
                db,
                scope.ServiceProvider.GetRequiredService<SessionLogTurnContextExtractor>(),
                scope.ServiceProvider.GetRequiredService<ILogger<SessionLogTurnContextBackfill>>(),
                cancellationToken).ConfigureAwait(false);
        }

        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void DecorateGraphRagService(IServiceCollection services)
    {
        var innerGraphRag = services.Single(d => d.ServiceType == typeof(IGraphRagService));
        var innerType = innerGraphRag.ImplementationType!;
        services.Remove(innerGraphRag);
        services.AddScoped<IGraphRagService>(sp =>
        {
            var inner = (IGraphRagService)ActivatorUtilities.CreateInstance(sp, innerType);
            return new TransactionGatedGraphRagService(
                inner,
                sp.GetService<ITurnTransactionCoordinator>(),
                sp.GetService<IOptions<TurnTransactionOptions>>());
        });
    }

    /// <summary>TR-MCP-TXN-001: Registers transaction services required by stdio mutation gates.</summary>
    internal static void AddStdioTransactionSecurity(IServiceCollection services, IConfiguration configuration)
        => services.AddInProcessTransactionSecurity(configuration);
}
