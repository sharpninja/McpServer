using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Writes and removes <c>AGENTS-README-FIRST.yaml</c> marker files in workspace roots so that
/// agents can discover the correct port and endpoints for calling the MCP server.
/// Prompt templates use Handlebars syntax with the full workspace definition available as context.
/// </summary>
public static class MarkerFileService
{
    /// <summary>Well-known marker file name placed at the workspace root.</summary>
    public const string MarkerFileName = "AGENTS-README-FIRST.yaml";

    private static readonly ISerializer s_yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private static readonly IHandlebars s_handlebars = Handlebars.Create();

    /// <summary>
    /// Writes the <c>AGENTS-README-FIRST.yaml</c> marker file to <paramref name="workspacePath"/>.
    /// </summary>
    /// <param name="workspacePath">Absolute path to the workspace root directory.</param>
    /// <param name="port">HTTP port the workspace is served on.</param>
    /// <param name="workspaceName">Human-readable workspace name.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="globalPromptTemplate">
    /// Global Handlebars prompt template.
    /// Must be provided; otherwise an exception is thrown.
    /// </param>
    /// <param name="workspacePromptTemplate">
    /// Optional per-workspace Handlebars prompt template.
    /// When non-null, the resolved text is appended to the global prompt.
    /// </param>
    /// <param name="apiKey">
    /// Per-workspace auth token to include in the marker file.
    /// Agents read this value and send it as the <c>X-Api-Key</c> header.
    /// </param>
    /// <param name="workspace">
    /// Full workspace definition. All properties are available in Handlebars templates as <c>{{workspace.*}}</c>.
    /// </param>
    /// <param name="serverStartedAtUtc">
    /// Optional server startup UTC timestamp to embed in the marker for stale-marker detection.
    /// When omitted, the current UTC timestamp is used.
    /// </param>
    public static async Task WriteMarkerAsync(
        string workspacePath,
        int port,
        string workspaceName,
        ILogger? logger = null,
        CancellationToken ct = default,
        string? globalPromptTemplate = null,
        string? workspacePromptTemplate = null,
        string? apiKey = null,
        WorkspaceDto? workspace = null,
        DateTimeOffset? serverStartedAtUtc = null)
    {
        var baseUrl = $"http://{System.Net.Dns.GetHostName()}:{port.ToString(CultureInfo.InvariantCulture)}";
        var markerPath = Path.Combine(workspacePath, MarkerFileName);
        var markerWrittenAtUtc = DateTimeOffset.UtcNow;
        var resolvedServerStartedAtUtc = (serverStartedAtUtc ?? markerWrittenAtUtc).ToUniversalTime();
        var markerWrittenAtUtcText = markerWrittenAtUtc.ToString("o", CultureInfo.InvariantCulture);
        var serverStartedAtUtcText = resolvedServerStartedAtUtc.ToString("o", CultureInfo.InvariantCulture);

        var templateContext = BuildTemplateContext(baseUrl, apiKey, workspace, workspacePath, workspaceName);
        templateContext["markerWrittenAtUtc"] = markerWrittenAtUtcText;
        templateContext["serverStartedAtUtc"] = serverStartedAtUtcText;

        var marker = new MarkerFile
        {
            Port = port,
            BaseUrl = baseUrl,
            ApiKey = apiKey ?? string.Empty,
            Endpoints = new MarkerEndpoints
            {
                Health = "/health",
                Swagger = "/swagger/v1/swagger.json",
                SwaggerUi = "/swagger",
                McpTransport = "/mcp-transport",
                SessionLog = "/mcpserver/sessionlog",
                SessionLogDialog = "/mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog",
                ContextSearch = "/mcpserver/context/search",
                ContextPack = "/mcpserver/context/pack",
                ContextSources = "/mcpserver/context/sources",
                Todo = "/mcpserver/todo",
                Repo = "/mcpserver/repo",
                GitHub = "/mcpserver/gh",
                Tools = "/mcpserver/tools",
                Workspace = "/mcpserver/workspace",
                ServerStartupUtc = "/server-startup-utc",
                MarkerFileTimestamp = "/marker-file-timestamp?repoPath={workspacePath}",
            },
            Workspace = workspaceName,
            WorkspacePath = workspacePath,
            Pid = Environment.ProcessId,
            StartedAt = markerWrittenAtUtcText,
            MarkerWrittenAtUtc = markerWrittenAtUtcText,
            ServerStartedAtUtc = serverStartedAtUtcText,
            Prompt = ResolvePrompt(templateContext, globalPromptTemplate, workspacePromptTemplate),
        };

        try
        {
            EnsureGitIgnored(workspacePath, logger);
            var yaml = s_yamlSerializer.Serialize(marker);
            await File.WriteAllTextAsync(markerPath, yaml, ct).ConfigureAwait(false);
            logger?.LogInformation("Wrote MCP marker file: {Path}", markerPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            logger?.LogWarning(ex, "Failed to write MCP marker file: {Path}", markerPath);
        }
    }

    /// <summary>
    /// Removes the <c>AGENTS-README-FIRST.yaml</c> marker file from <paramref name="workspacePath"/>.
    /// Also removes any legacy <c>.mcp-server.json</c> and <c>.mcp-server.yaml</c> files if present.
    /// </summary>
    public static void RemoveMarker(string workspacePath, ILogger? logger = null)
    {
        RemoveSingleFile(Path.Combine(workspacePath, MarkerFileName), logger);
        // Clean up legacy markers if they exist.
        RemoveSingleFile(Path.Combine(workspacePath, ".mcp-server.yaml"), logger);
        RemoveSingleFile(Path.Combine(workspacePath, ".mcp-server.json"), logger);
    }

    /// <summary>Ensures <see cref="MarkerFileName"/> is listed in the workspace root's <c>.gitignore</c>.</summary>
    private static void EnsureGitIgnored(string workspacePath, ILogger? logger)
    {
        try
        {
            var gitignorePath = Path.Combine(workspacePath, ".gitignore");
            if (File.Exists(gitignorePath))
            {
                var lines = File.ReadAllLines(gitignorePath);
                if (lines.Any(l => l.Trim().Equals(MarkerFileName, StringComparison.OrdinalIgnoreCase)))
                    return;
            }

            File.AppendAllText(gitignorePath, $"{Environment.NewLine}{MarkerFileName}{Environment.NewLine}");
            logger?.LogInformation("Added {Marker} to .gitignore at {Path}", MarkerFileName, gitignorePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger?.LogWarning(ex, "Failed to update .gitignore at {Path}", workspacePath);
        }
    }

    private static void RemoveSingleFile(string markerPath, ILogger? logger)
    {
        try
        {
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
                logger?.LogInformation("Removed MCP marker file: {Path}", markerPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger?.LogWarning(ex, "Failed to remove MCP marker file: {Path}", markerPath);
        }
    }

    /// <summary>
    /// Resolves the final prompt by compiling global and workspace Handlebars templates
    /// against the supplied context. Visible for testing.
    /// </summary>
    internal static string ResolvePrompt(
        Dictionary<string, object?> templateContext,
        string? globalPromptTemplate,
        string? workspacePromptTemplate)
    {
        if (string.IsNullOrWhiteSpace(globalPromptTemplate))
            throw new ArgumentException("Global prompt template must be provided.", nameof(globalPromptTemplate));

        var global = RenderHandlebars(globalPromptTemplate, templateContext);

        if (string.IsNullOrWhiteSpace(workspacePromptTemplate))
            return global;

        var workspace = RenderHandlebars(workspacePromptTemplate, templateContext);
        return global + "\n\n" + workspace;
    }

    /// <summary>
    /// Builds the Handlebars template context dictionary from the workspace definition and runtime values.
    /// </summary>
    internal static Dictionary<string, object?> BuildTemplateContext(
        string baseUrl,
        string? apiKey,
        WorkspaceDto? workspace,
        string workspacePath,
        string workspaceName)
    {
        var version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";

        return new Dictionary<string, object?>
        {
            ["baseUrl"] = baseUrl,
            ["apiKey"] = apiKey ?? string.Empty,
            ["version"] = version,
            ["workspace"] = workspace is not null ? new Dictionary<string, object?>
            {
                ["Name"] = workspace.Name,
                ["WorkspacePath"] = workspace.WorkspacePath,
                ["TodoPath"] = workspace.TodoPath,
                ["DataDirectory"] = workspace.DataDirectory ?? workspace.WorkspacePath,
                ["TunnelProvider"] = workspace.TunnelProvider ?? "none",
                ["IsPrimary"] = workspace.IsPrimary,
                ["IsEnabled"] = workspace.IsEnabled,
                ["DateTimeCreated"] = workspace.DateTimeCreated.ToString("o", CultureInfo.InvariantCulture),
                ["DateTimeModified"] = workspace.DateTimeModified.ToString("o", CultureInfo.InvariantCulture),
                ["RunAs"] = workspace.RunAs ?? "default",
                ["PromptTemplate"] = workspace.PromptTemplate ?? string.Empty,
                ["BannedLicenses"] = workspace.BannedLicenses.Count > 0 ? workspace.BannedLicenses : null,
                ["BannedCountriesOfOrigin"] = workspace.BannedCountriesOfOrigin.Count > 0 ? workspace.BannedCountriesOfOrigin : null,
                ["BannedOrganizations"] = workspace.BannedOrganizations.Count > 0 ? workspace.BannedOrganizations : null,
                ["BannedIndividuals"] = workspace.BannedIndividuals.Count > 0 ? workspace.BannedIndividuals : null,
            } : new Dictionary<string, object?>
            {
                ["Name"] = workspaceName,
                ["WorkspacePath"] = workspacePath,
                ["TodoPath"] = string.Empty,
                ["DataDirectory"] = workspacePath,
                ["TunnelProvider"] = "none",
                ["IsPrimary"] = false,
                ["IsEnabled"] = true,
                ["DateTimeCreated"] = string.Empty,
                ["DateTimeModified"] = string.Empty,
                ["RunAs"] = "default",
                ["PromptTemplate"] = string.Empty,
                ["BannedLicenses"] = null,
                ["BannedCountriesOfOrigin"] = null,
                ["BannedOrganizations"] = null,
                ["BannedIndividuals"] = null,
            },
        };
    }

    private static string RenderHandlebars(string template, Dictionary<string, object?> context)
    {
        // Normalize to LF before and after — CRLF in templates confuses Handlebars
        // standalone-line detection, and YAML folded scalars treat \r as extra blank lines.
        var compiled = s_handlebars.Compile(template.ReplaceLineEndings("\n"));
        return compiled(context).ReplaceLineEndings("\n");
    }
}

/// <summary>Serialization model for the <c>AGENTS-README-FIRST.yaml</c> marker file.</summary>
internal sealed class MarkerFile
{
    public int Port { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public MarkerEndpoints Endpoints { get; set; } = new();
    public string Workspace { get; set; } = string.Empty;
    public string WorkspacePath { get; set; } = string.Empty;
    public int Pid { get; set; }
    // Backward-compatible marker write timestamp retained for existing consumers.
    public string StartedAt { get; set; } = string.Empty;
    public string MarkerWrittenAtUtc { get; set; } = string.Empty;
    public string ServerStartedAtUtc { get; set; } = string.Empty;
    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string Prompt { get; set; } = string.Empty;
}

/// <summary>Well-known endpoint paths exposed by the MCP server.</summary>
internal sealed class MarkerEndpoints
{
    public string Health { get; set; } = string.Empty;
    public string Swagger { get; set; } = string.Empty;
    public string SwaggerUi { get; set; } = string.Empty;
    public string McpTransport { get; set; } = string.Empty;
    public string SessionLog { get; set; } = string.Empty;
    public string SessionLogDialog { get; set; } = string.Empty;
    public string ContextSearch { get; set; } = string.Empty;
    public string ContextPack { get; set; } = string.Empty;
    public string ContextSources { get; set; } = string.Empty;
    public string Todo { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
    public string GitHub { get; set; } = string.Empty;
    public string Tools { get; set; } = string.Empty;
    public string Workspace { get; set; } = string.Empty;
    public string ServerStartupUtc { get; set; } = string.Empty;
    public string MarkerFileTimestamp { get; set; } = string.Empty;
}
