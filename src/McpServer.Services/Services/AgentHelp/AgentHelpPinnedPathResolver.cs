using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace McpServer.Support.Mcp.Services.AgentHelp;

/// <summary>
/// FR-MCP-HELP-005: Resolves Agent Help pinned document paths across workspace, data, and install roots.
/// TR-MCP-HELP-006: Supports scoped path tokens so corpus bootstrap can load workspace and canonical server sources.
/// </summary>
public sealed class AgentHelpPinnedPathResolver
{
    /// <summary>Scope prefix for paths relative to the active workspace root.</summary>
    public const string WorkspaceScope = "workspace";

    /// <summary>Scope prefix for paths relative to the effective MCP data folder.</summary>
    public const string DataScope = "data";

    /// <summary>Scope prefix for paths relative to the server install/content root.</summary>
    public const string InstallScope = "install";

    /// <summary>Scope prefix for paths relative to the MCP Server primary workspace (canonical McpServer source).</summary>
    public const string PrimaryScope = "primary";

    private static readonly string[] s_defaultPinnedPaths =
    [
        "workspace:AGENTS-README-FIRST.yaml",
        "workspace:AGENTS.md",
        "workspace:.github/copilot-instructions.md",
    ];

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// TR-MCP-HELP-006: Creates a pinned path resolver.
    /// </summary>
    /// <param name="configuration">Application configuration used to resolve the effective data folder.</param>
    /// <param name="hostEnvironment">Host environment used to normalize content-root-relative paths.</param>
    /// <param name="serviceProvider">Service provider used to resolve the DB-backed primary workspace when available.</param>
    public AgentHelpPinnedPathResolver(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// FR-MCP-HELP-005: Returns the configured pinned path tokens, or the built-in defaults.
    /// </summary>
    /// <param name="options">Agent Help options.</param>
    public static IReadOnlyList<string> GetPinnedPathTokens(AgentHelpOptions options)
        => options.PinnedPaths.Count > 0 ? options.PinnedPaths : s_defaultPinnedPaths;

    /// <summary>
    /// TR-MCP-GRAPHRAG-GLOBAL-001: Resolves the MCP Server primary workspace root when available.
    /// </summary>
    /// <returns>Absolute primary workspace path, or <see langword="null"/> when unavailable.</returns>
    public string? TryGetPrimaryWorkspacePath()
        => ResolvePrimaryWorkspacePath();

    /// <summary>
    /// FR-MCP-HELP-005: Resolves a pinned path token to an on-disk file path when the file exists.
    /// </summary>
    /// <param name="token">Pinned path token. Supports <c>workspace:</c>, <c>data:</c>, <c>install:</c>, or absolute paths.</param>
    /// <param name="workspacePath">Active workspace root.</param>
    /// <returns>Resolved absolute file path and display source key, or <see langword="null"/> when missing.</returns>
    public (string FullPath, string SourceKey)? TryResolve(string token, string workspacePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);

        var trimmed = token.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            var absolute = Path.GetFullPath(trimmed);
            return File.Exists(absolute) ? (absolute, $"absolute:{trimmed.Replace('\\', '/')}") : null;
        }

        var separatorIndex = trimmed.IndexOf(':', StringComparison.Ordinal);
        string scope;
        string relativePath;
        if (separatorIndex > 0)
        {
            scope = trimmed[..separatorIndex];
            relativePath = trimmed[(separatorIndex + 1)..].TrimStart('/', '\\');
        }
        else
        {
            scope = WorkspaceScope;
            relativePath = trimmed.TrimStart('/', '\\');
        }

        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var root = ResolveRoot(scope, workspacePath);
        if (string.IsNullOrWhiteSpace(root))
            return null;

        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        return File.Exists(fullPath) ? (fullPath, $"{scope}:{relativePath.Replace('\\', '/')}") : null;
    }

    private string? ResolveRoot(string scope, string workspacePath)
    {
        return scope.ToLowerInvariant() switch
        {
            WorkspaceScope => Path.GetFullPath(workspacePath),
            PrimaryScope => ResolvePrimaryWorkspacePath(),
            DataScope => McpInstanceResolver.GetEffectiveDataFolder(_configuration, instanceName: null),
            InstallScope => AppContext.BaseDirectory,
            _ => null,
        };
    }

    private string? ResolvePrimaryWorkspacePath()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var workspaceService = scope.ServiceProvider.GetService<IWorkspaceService>();
            if (workspaceService is not null)
            {
                var items = workspaceService.ListAsync().GetAwaiter().GetResult().Items;
                var primary = items.FirstOrDefault(item => item.IsPrimary && item.IsEnabled)
                    ?? items.FirstOrDefault(item => item.IsEnabled);
                if (!string.IsNullOrWhiteSpace(primary?.WorkspacePath))
                    return NormalizePath(primary.WorkspacePath);
            }
        }
        catch (InvalidOperationException)
        {
            // Fall through to configuration-based resolution for unit tests and bare hosts.
        }

        var configuredWorkspaces = _configuration.GetSection("Mcp:Workspaces").GetChildren().ToList();
        var configuredPrimary = configuredWorkspaces
            .FirstOrDefault(section => bool.TryParse(section["IsPrimary"], out var isPrimary) && isPrimary)
            ?? configuredWorkspaces.FirstOrDefault();
        var configuredPath = configuredPrimary?["WorkspacePath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return NormalizePath(configuredPath);

        var repoRoot = McpInstanceResolver.GetEffectiveMcpValue(_configuration, instanceName: null, "RepoRoot");
        if (!string.IsNullOrWhiteSpace(repoRoot))
            return NormalizePath(repoRoot);

        return null;
    }

    private string NormalizePath(string path)
    {
        var trimmed = path.Trim();
        return Path.IsPathRooted(trimmed)
            ? Path.GetFullPath(trimmed)
            : Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, trimmed));
    }
}