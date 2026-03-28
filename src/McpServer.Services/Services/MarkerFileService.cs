using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
    internal const string MarkerSignatureCanonicalization = "marker-v1";
    internal const string MarkerSignatureVerifier = "workspace_api_key";
    private const string WorkspaceStateDirectoryGitIgnoreEntry = ".mcpServer/";

    private static readonly ISerializer s_yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private static readonly IHandlebars s_handlebars = Handlebars.Create();

    /// <summary>
    /// Writes the <c>AGENTS-README-FIRST.yaml</c> marker file to <paramref name="workspacePath"/>.
    /// </summary>
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
        IReadOnlyList<(string AgentId, string Content)>? agentAdditions = null,
        DateTimeOffset? serverStartedAtUtc = null)
    {
        var baseUrl = $"http://{System.Net.Dns.GetHostName()}:{port.ToString(CultureInfo.InvariantCulture)}";
        var markerPath = Path.Combine(workspacePath, MarkerFileName);
        var markerWrittenAtUtc = DateTimeOffset.UtcNow;
        var resolvedServerStartedAtUtc = (serverStartedAtUtc ?? markerWrittenAtUtc).ToUniversalTime();
        var markerWrittenAtUtcText = markerWrittenAtUtc.ToString("o", CultureInfo.InvariantCulture);
        var serverStartedAtUtcText = resolvedServerStartedAtUtc.ToString("o", CultureInfo.InvariantCulture);

        var templateContext = BuildTemplateContext(baseUrl, apiKey, workspace, workspacePath, workspaceName, agentAdditions);
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
                Desktop = "/mcpserver/desktop",
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
            Signature = new MarkerSignature
            {
                Algorithm = "HMAC-SHA256",
                Canonicalization = MarkerSignatureCanonicalization,
                Verifier = MarkerSignatureVerifier,
            },
            TrustBootstrap = new MarkerTrustBootstrap
            {
                Description = "This marker is self-verifiable when the workspace API key and rendered bootstrap payload still match the active MCP workspace contract.",
                Guarantees =
                [
                    "The marker signature can be recomputed from the workspace API key in this file.",
                    "The /health endpoint echoes a caller nonce exactly when one is supplied.",
                    "Agents must stop MCP usage after any signature or nonce mismatch."
                ],
                HealthNonceEndpoint = "/health",
                HealthNonceParameter = "nonce",
                Fallback = "If health check, nonce verification, or signature verification fails, log MCP_UNTRUSTED and continue without the MCP server. Do not probe additional endpoints.",
                RecommendedUsage = "Use /sessionlog, /todo, /context, and other MCP endpoints only after both signature and nonce verification succeed.",
            },
            Prompt = ResolvePrompt(templateContext, globalPromptTemplate, workspacePromptTemplate),
        };
        marker.Signature.Value = ComputeMarkerSignature(marker);

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
        RemoveSingleFile(Path.Combine(workspacePath, ".mcp-server.yaml"), logger);
        RemoveSingleFile(Path.Combine(workspacePath, ".mcp-server.json"), logger);
    }

    private static void EnsureGitIgnored(string workspacePath, ILogger? logger)
    {
        try
        {
            var gitignorePath = Path.Combine(workspacePath, ".gitignore");
            var lines = File.Exists(gitignorePath) ? File.ReadAllLines(gitignorePath) : [];
            var missingEntries = new[] { MarkerFileName, WorkspaceStateDirectoryGitIgnoreEntry }
                .Where(entry => !lines.Any(line => line.Trim().Equals(entry, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (missingEntries.Count == 0)
                return;

            var needsLeadingNewLine = lines.Length > 0 && !string.IsNullOrEmpty(lines[^1]);
            var content = string.Join(Environment.NewLine, missingEntries) + Environment.NewLine;
            if (needsLeadingNewLine)
                content = Environment.NewLine + content;

            File.AppendAllText(gitignorePath, content);
            logger?.LogInformation("Added {Entries} to .gitignore at {Path}", string.Join(", ", missingEntries), gitignorePath);
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

    internal static Dictionary<string, object?> BuildTemplateContext(
        string baseUrl,
        string? apiKey,
        WorkspaceDto? workspace,
        string workspacePath,
        string workspaceName,
        IReadOnlyList<(string AgentId, string Content)>? agentAdditions = null)
    {
        var version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";

        return new Dictionary<string, object?>
        {
            ["baseUrl"] = baseUrl,
            ["apiKey"] = apiKey ?? string.Empty,
            ["version"] = version,
            ["agentAdditions"] = agentAdditions is { Count: > 0 }
                ? agentAdditions.Select(x => new Dictionary<string, object?>
                {
                    ["agentId"] = x.AgentId,
                    ["content"] = x.Content,
                }).ToList()
                : null,
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

    internal static string ComputeMarkerSignature(MarkerFile marker)
    {
        ArgumentNullException.ThrowIfNull(marker);

        var keyBytes = Encoding.UTF8.GetBytes(marker.ApiKey ?? string.Empty);
        var payloadBytes = Encoding.UTF8.GetBytes(BuildSignaturePayload(marker));
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(payloadBytes));
    }

    internal static string BuildSignaturePayload(MarkerFile marker)
    {
        ArgumentNullException.ThrowIfNull(marker);

        var builder = new StringBuilder();
        AppendPayloadLine(builder, "canonicalization", marker.Signature.Canonicalization);
        AppendPayloadLine(builder, "port", marker.Port.ToString(CultureInfo.InvariantCulture));
        AppendPayloadLine(builder, "baseUrl", marker.BaseUrl);
        AppendPayloadLine(builder, "apiKey", marker.ApiKey);
        AppendPayloadLine(builder, "workspace", marker.Workspace);
        AppendPayloadLine(builder, "workspacePath", marker.WorkspacePath);
        AppendPayloadLine(builder, "pid", marker.Pid.ToString(CultureInfo.InvariantCulture));
        AppendPayloadLine(builder, "startedAt", marker.StartedAt);
        AppendPayloadLine(builder, "markerWrittenAtUtc", marker.MarkerWrittenAtUtc);
        AppendPayloadLine(builder, "serverStartedAtUtc", marker.ServerStartedAtUtc);
        AppendPayloadLine(builder, "endpoints.health", marker.Endpoints.Health);
        AppendPayloadLine(builder, "endpoints.swagger", marker.Endpoints.Swagger);
        AppendPayloadLine(builder, "endpoints.swaggerUi", marker.Endpoints.SwaggerUi);
        AppendPayloadLine(builder, "endpoints.mcpTransport", marker.Endpoints.McpTransport);
        AppendPayloadLine(builder, "endpoints.sessionLog", marker.Endpoints.SessionLog);
        AppendPayloadLine(builder, "endpoints.sessionLogDialog", marker.Endpoints.SessionLogDialog);
        AppendPayloadLine(builder, "endpoints.contextSearch", marker.Endpoints.ContextSearch);
        AppendPayloadLine(builder, "endpoints.contextPack", marker.Endpoints.ContextPack);
        AppendPayloadLine(builder, "endpoints.contextSources", marker.Endpoints.ContextSources);
        AppendPayloadLine(builder, "endpoints.todo", marker.Endpoints.Todo);
        AppendPayloadLine(builder, "endpoints.repo", marker.Endpoints.Repo);
        AppendPayloadLine(builder, "endpoints.desktop", marker.Endpoints.Desktop);
        AppendPayloadLine(builder, "endpoints.gitHub", marker.Endpoints.GitHub);
        AppendPayloadLine(builder, "endpoints.tools", marker.Endpoints.Tools);
        AppendPayloadLine(builder, "endpoints.workspace", marker.Endpoints.Workspace);
        AppendPayloadLine(builder, "endpoints.serverStartupUtc", marker.Endpoints.ServerStartupUtc);
        AppendPayloadLine(builder, "endpoints.markerFileTimestamp", marker.Endpoints.MarkerFileTimestamp);
        return builder.ToString();
    }

    private static void AppendPayloadLine(StringBuilder builder, string key, string? value)
    {
        builder.Append(key)
            .Append('=')
            .Append((value ?? string.Empty).ReplaceLineEndings("\n"))
            .Append('\n');
    }

    private static string RenderHandlebars(string template, Dictionary<string, object?> context)
    {
        var compiled = s_handlebars.Compile(template.ReplaceLineEndings("\n"));
        return compiled(context).ReplaceLineEndings("\n");
    }
}

internal sealed class MarkerFile
{
    public int Port { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public MarkerEndpoints Endpoints { get; set; } = new();
    public string Workspace { get; set; } = string.Empty;
    public string WorkspacePath { get; set; } = string.Empty;
    public int Pid { get; set; }
    public string StartedAt { get; set; } = string.Empty;
    public string MarkerWrittenAtUtc { get; set; } = string.Empty;
    public string ServerStartedAtUtc { get; set; } = string.Empty;
    [YamlMember(Alias = "signature", ApplyNamingConventions = false)]
    public MarkerSignature Signature { get; set; } = new();
    [YamlMember(Alias = "trust_bootstrap", ApplyNamingConventions = false)]
    public MarkerTrustBootstrap TrustBootstrap { get; set; } = new();
    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string Prompt { get; set; } = string.Empty;
}

internal sealed class MarkerSignature
{
    public string Algorithm { get; set; } = string.Empty;
    public string Canonicalization { get; set; } = string.Empty;
    public string Verifier { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

internal sealed class MarkerTrustBootstrap
{
    public string Description { get; set; } = string.Empty;
    public string[] Guarantees { get; set; } = [];
    [YamlMember(Alias = "health_nonce_endpoint", ApplyNamingConventions = false)]
    public string HealthNonceEndpoint { get; set; } = string.Empty;
    [YamlMember(Alias = "health_nonce_parameter", ApplyNamingConventions = false)]
    public string HealthNonceParameter { get; set; } = string.Empty;
    public string Fallback { get; set; } = string.Empty;
    [YamlMember(Alias = "recommended_usage", ApplyNamingConventions = false)]
    public string RecommendedUsage { get; set; } = string.Empty;
}

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
    public string Desktop { get; set; } = string.Empty;
    public string GitHub { get; set; } = string.Empty;
    public string Tools { get; set; } = string.Empty;
    public string Workspace { get; set; } = string.Empty;
    public string ServerStartupUtc { get; set; } = string.Empty;
    public string MarkerFileTimestamp { get; set; } = string.Empty;
}
