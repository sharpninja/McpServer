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
    /// When <paramref name="overrideBaseUrl"/> is supplied the marker uses that URL (e.g. a
    /// federation upstream or ngrok tunnel) instead of the default <c>http://hostname:port</c>.
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
        DateTimeOffset? serverStartedAtUtc = null,
        string? overrideBaseUrl = null)
    {
        string baseUrl;
        if (!string.IsNullOrWhiteSpace(overrideBaseUrl))
        {
            baseUrl = overrideBaseUrl.TrimEnd('/');
            // Derive port from the override URL so the marker Port field is consistent.
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var overrideUri))
                port = overrideUri.IsDefaultPort
                    ? (string.Equals(overrideUri.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80)
                    : overrideUri.Port;
        }
        else
        {
            baseUrl = $"http://{System.Net.Dns.GetHostName()}:{port.ToString(CultureInfo.InvariantCulture)}";
        }
        var markerPath = Path.Combine(workspacePath, MarkerFileName);
        var markerWrittenAtUtc = DateTimeOffset.UtcNow;
        var resolvedServerStartedAtUtc = (serverStartedAtUtc ?? markerWrittenAtUtc).ToUniversalTime();
        var markerWrittenAtUtcText = markerWrittenAtUtc.ToString("o", CultureInfo.InvariantCulture);
        var serverStartedAtUtcText = resolvedServerStartedAtUtc.ToString("o", CultureInfo.InvariantCulture);

        var templateContext = BuildTemplateContext(baseUrl, apiKey, workspace, workspacePath, workspaceName, agentAdditions);
        templateContext["markerWrittenAtUtc"] = markerWrittenAtUtcText;
        templateContext["serverStartedAtUtc"] = serverStartedAtUtcText;

        var agentPlugins = BuildDefaultAgentPlugins(workspacePath);
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
            AgentPlugins = agentPlugins,
            Prompt = ResolvePrompt(templateContext, globalPromptTemplate, workspacePromptTemplate),
        };
        agentPlugins.ContractDigest = ComputeAgentPluginsDigest(agentPlugins);
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

        var agentPlugins = BuildDefaultAgentPlugins(workspacePath);
        agentPlugins.ContractDigest = ComputeAgentPluginsDigest(agentPlugins);

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
            ["agentPlugins"] = agentPlugins,
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
        if (marker.AgentPlugins is not null)
        {
            AppendPayloadLine(builder, "agentPlugins.policy", marker.AgentPlugins.Policy);
            AppendPayloadLine(builder, "agentPlugins.contractDigest", marker.AgentPlugins.ContractDigest);
        }
        return builder.ToString();
    }

    internal static MarkerAgentPlugins BuildDefaultAgentPlugins(string workspacePath)
    {
        var siblingRoot = Path.GetDirectoryName(Path.GetFullPath(workspacePath)) ?? string.Empty;
        string Sibling(string name) => string.IsNullOrWhiteSpace(siblingRoot) ? name : Path.Combine(siblingRoot, name);

        return new MarkerAgentPlugins
        {
            Policy = "required",
            Agents = new Dictionary<string, MarkerAgentPluginContract>(StringComparer.Ordinal)
            {
                ["Codex"] = new()
                {
                    SourceType = "Codex",
                    PluginName = "mcpserver-codex-plugin",
                    PluginVersion = "1.1.0",
                    Activation = "Codex hook lifecycle through .codex-plugin/plugin.json.",
                    StartupCommand = "lib/session-start.sh \"{workspacePath}\"",
                    UnavailableFailure = "MCP_PLUGIN_UNAVAILABLE:Codex",
                    RequiredEnvVars = ["CODEX_PLUGIN_ROOT", "PLUGIN_AGENT_NAME=Codex"],
                    HookExpectations = ["SessionStart", "UserPromptSubmit", "PostToolUse", "Stop"],
                    ToolExpectations = ["workflow.sessionlog.*", "workflow.todo.*", "workflow.requirements.*"],
                    RootHints = [Sibling("mcpserver-codex-plugin"), "$CODEX_PLUGIN_ROOT"],
                },
                ["Claude"] = new()
                {
                    SourceType = "Claude",
                    PluginName = "mcpserver-claude-code-plugin",
                    PluginVersion = "1.1.0",
                    Activation = "Claude Code plugin hooks and .mcp.json mcpserver entry.",
                    StartupCommand = "hooks/session-start.sh \"{workspacePath}\"",
                    UnavailableFailure = "MCP_PLUGIN_UNAVAILABLE:Claude",
                    RequiredEnvVars = ["CLAUDE_PLUGIN_ROOT", "PLUGIN_AGENT_NAME=Claude"],
                    HookExpectations = ["SessionStart", "UserPromptSubmit", "PostToolUse", "Stop"],
                    ToolExpectations = ["mcpserver session tools", "mcpserver todo tools", "mcpserver requirements tools"],
                    RootHints = [Sibling("mcpserver-claude-code-plugin"), "$CLAUDE_PLUGIN_ROOT"],
                },
                ["Copilot"] = new()
                {
                    SourceType = "Copilot",
                    PluginName = "mcpserver-copilot-plugin",
                    PluginVersion = "1.1.0",
                    Activation = "Copilot plugin hooks and .mcp.json mcpserver entry.",
                    StartupCommand = "hooks/session-start.sh \"{workspacePath}\"",
                    UnavailableFailure = "MCP_PLUGIN_UNAVAILABLE:Copilot",
                    RequiredEnvVars = ["COPILOT_PLUGIN_ROOT", "PLUGIN_AGENT_NAME=Copilot"],
                    HookExpectations = ["SessionStart", "UserPromptSubmit", "PostToolUse", "Stop"],
                    ToolExpectations = ["mcpserver session tools", "mcpserver todo tools", "mcpserver requirements tools"],
                    RootHints = [Sibling("mcpserver-copilot-plugin"), "$COPILOT_PLUGIN_ROOT"],
                },
                ["Cline"] = new()
                {
                    SourceType = "Cline",
                    PluginName = "mcpserver-cline-plugin",
                    PluginVersion = "1.1.0",
                    Activation = "Cline MCP server configured from server.json.",
                    StartupCommand = "npm run build && node dist/index.js",
                    UnavailableFailure = "MCP_PLUGIN_UNAVAILABLE:Cline",
                    RequiredEnvVars = ["CLINE_PLUGIN_ROOT", "PLUGIN_AGENT_NAME=Cline"],
                    HookExpectations = ["MCP server startup", "tool call audit"],
                    ToolExpectations = ["session_*", "req_*"],
                    RootHints = [Sibling("mcpserver-cline-plugin"), "$CLINE_PLUGIN_ROOT"],
                },
                ["Grok"] = new()
                {
                    SourceType = "GrokCode",
                    PluginName = "mcpserver-grok-plugin",
                    PluginVersion = "0.1.0",
                    Activation = "Grok 4.3 TUI/CLI loads skills/*.md from the plugin into ~/.grok/skills. Strong pwsh integration via McpSession.psm1 / McpTodo.psm1. Optional hooks/ for compatibility.",
                    StartupCommand = "",
                    UnavailableFailure = "MCP_PLUGIN_UNAVAILABLE:GrokCode",
                    RequiredEnvVars = ["GROK_PLUGIN_ROOT", "PLUGIN_AGENT_NAME=GrokCode"],
                    HookExpectations = ["SessionStart", "UserPromptSubmit", "PostToolUse", "Stop", "PlanMode"],
                    ToolExpectations = ["mcp_session_*", "mcp_todo_*", "mcp_requirements_*", "workflow.sessionlog.*", "workflow.todo.*", "workflow.requirements.*"],
                    RootHints = [Sibling("mcpserver-grok-plugin"), "$GROK_PLUGIN_ROOT"],
                },
            },
        };
    }

    internal static string ComputeAgentPluginsDigest(MarkerAgentPlugins agentPlugins)
    {
        ArgumentNullException.ThrowIfNull(agentPlugins);
        var builder = new StringBuilder();
        AppendPayloadLine(builder, "policy", agentPlugins.Policy);
        foreach (var (agentName, contract) in agentPlugins.Agents.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            AppendPayloadLine(builder, $"{agentName}.sourceType", contract.SourceType);
            AppendPayloadLine(builder, $"{agentName}.pluginName", contract.PluginName);
            AppendPayloadLine(builder, $"{agentName}.pluginVersion", contract.PluginVersion);
            AppendPayloadLine(builder, $"{agentName}.activation", contract.Activation);
            AppendPayloadLine(builder, $"{agentName}.startupCommand", contract.StartupCommand);
            AppendPayloadLine(builder, $"{agentName}.unavailableFailure", contract.UnavailableFailure);
            AppendPayloadLine(builder, $"{agentName}.requiredEnvVars", string.Join(",", contract.RequiredEnvVars));
            AppendPayloadLine(builder, $"{agentName}.hookExpectations", string.Join(",", contract.HookExpectations));
            AppendPayloadLine(builder, $"{agentName}.toolExpectations", string.Join(",", contract.ToolExpectations));
            AppendPayloadLine(builder, $"{agentName}.rootHints", string.Join(",", contract.RootHints));
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
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
    [YamlMember(Alias = "agent_plugins", ApplyNamingConventions = false)]
    public MarkerAgentPlugins? AgentPlugins { get; set; }
    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string Prompt { get; set; } = string.Empty;
}

internal sealed class MarkerAgentPlugins
{
    public string Policy { get; set; } = "required";
    [YamlMember(Alias = "contract_digest", ApplyNamingConventions = false)]
    public string ContractDigest { get; set; } = string.Empty;
    public Dictionary<string, MarkerAgentPluginContract> Agents { get; set; } = new(StringComparer.Ordinal);
}

internal sealed class MarkerAgentPluginContract
{
    [YamlMember(Alias = "source_type", ApplyNamingConventions = false)]
    public string SourceType { get; set; } = string.Empty;
    [YamlMember(Alias = "plugin_name", ApplyNamingConventions = false)]
    public string PluginName { get; set; } = string.Empty;
    [YamlMember(Alias = "plugin_version", ApplyNamingConventions = false)]
    public string PluginVersion { get; set; } = string.Empty;
    public string Activation { get; set; } = string.Empty;
    [YamlMember(Alias = "startup_command", ApplyNamingConventions = false)]
    public string StartupCommand { get; set; } = string.Empty;
    [YamlMember(Alias = "unavailable_failure", ApplyNamingConventions = false)]
    public string UnavailableFailure { get; set; } = string.Empty;
    [YamlMember(Alias = "required_env_vars", ApplyNamingConventions = false)]
    public string[] RequiredEnvVars { get; set; } = [];
    [YamlMember(Alias = "hook_expectations", ApplyNamingConventions = false)]
    public string[] HookExpectations { get; set; } = [];
    [YamlMember(Alias = "tool_expectations", ApplyNamingConventions = false)]
    public string[] ToolExpectations { get; set; } = [];
    [YamlMember(Alias = "root_hints", ApplyNamingConventions = false)]
    public string[] RootHints { get; set; } = [];
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
