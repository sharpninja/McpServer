using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services.AgentHelp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-GRAPHRAG-GLOBAL-001: Seeds canonical McpServer docs into the host-global GraphRAG input corpus at startup.
/// </summary>
public sealed class GraphRagGlobalCorpusStartupSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<GraphRagOptions> _options;
    private readonly ILogger<GraphRagGlobalCorpusStartupSeeder> _logger;

    /// <summary>Initializes a new instance of the <see cref="GraphRagGlobalCorpusStartupSeeder"/> class.</summary>
    /// <param name="scopeFactory">Scope factory for resolving scoped services.</param>
    /// <param name="options">GraphRAG options.</param>
    /// <param name="logger">Diagnostic logger.</param>
    public GraphRagGlobalCorpusStartupSeeder(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<GraphRagOptions> options,
        ILogger<GraphRagGlobalCorpusStartupSeeder> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SeedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Global GraphRAG corpus seeding failed; continuing startup without global corpus.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Seeds and optionally indexes the global corpus; surface for unit tests.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of canonical documents copied into the global input corpus.</returns>
    internal async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (!options.Enabled || !options.SeedCanonicalDocsOnStartup)
        {
            _logger.LogDebug("Global GraphRAG seeding disabled; skipping startup seed.");
            return 0;
        }

        using var scope = _scopeFactory.CreateScope();
        var graphRagService = scope.ServiceProvider.GetRequiredService<IGraphRagService>();
        var pinnedPathResolver = scope.ServiceProvider.GetRequiredService<AgentHelpPinnedPathResolver>();
        var workspaceService = scope.ServiceProvider.GetService<IWorkspaceService>();

        var primaryWorkspacePath = ResolvePrimaryWorkspacePath(workspaceService);
        if (string.IsNullOrWhiteSpace(primaryWorkspacePath))
        {
            _logger.LogWarning("Global GraphRAG seeding skipped because the primary workspace could not be resolved.");
            return 0;
        }

        var status = await graphRagService.InitializeAsync(GraphRagStorageScope.Global, cancellationToken)
            .ConfigureAwait(false);
        var inputRoot = Path.Combine(status.GraphRoot, "input", "canonical");
        Directory.CreateDirectory(inputRoot);

        var copied = 0;
        foreach (var relativePath in options.CanonicalDocPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var token = $"primary:{relativePath.Trim().TrimStart('/', '\\')}";
            var resolved = pinnedPathResolver.TryResolve(token, primaryWorkspacePath);
            if (resolved is null)
                continue;

            var destinationRelative = resolved.Value.SourceKey
                .Replace("primary:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace('\\', '/');
            var destinationPath = Path.Combine(inputRoot, destinationRelative.Replace('/', Path.DirectorySeparatorChar));
            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            File.Copy(resolved.Value.FullPath, destinationPath, overwrite: true);
            copied++;
        }

        _logger.LogInformation(
            "Global GraphRAG corpus seed complete: copied {Copied}/{Total} canonical docs into {InputRoot}.",
            copied,
            options.CanonicalDocPaths.Count,
            inputRoot);

        if (copied > 0 && options.IndexGlobalCorpusOnStartup)
        {
            await graphRagService.IndexAsync(
                new GraphRagIndexRequest
                {
                    Scope = GraphRagStorageScope.Global,
                    Force = false,
                },
                cancellationToken).ConfigureAwait(false);
        }

        return copied;
    }

    private static string? ResolvePrimaryWorkspacePath(IWorkspaceService? workspaceService)
    {
        if (workspaceService is not null)
        {
            try
            {
                var items = workspaceService.ListAsync().GetAwaiter().GetResult().Items;
                var primary = items.FirstOrDefault(item => item.IsPrimary && item.IsEnabled)
                    ?? items.FirstOrDefault(item => item.IsEnabled);
                if (!string.IsNullOrWhiteSpace(primary?.WorkspacePath))
                    return Path.GetFullPath(primary.WorkspacePath);
            }
            catch (InvalidOperationException)
            {
                // Fall through for hosts without a ready workspace registry.
            }
        }

        return null;
    }
}