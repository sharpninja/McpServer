using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    internal const string SyncedAgentPluginVersion = "1.26.0";
    private const string WorkspaceStateDirectoryGitIgnoreEntry = ".mcpServer/";

    /// <summary>
    /// Test seam: when set, plugin-version resolution scans this directory for user plugin caches
    /// instead of the real user profile, so tests are hermetic against locally installed plugins.
    /// </summary>
    internal static string? AgentPluginUserProfileOverride { get; set; }

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
                var archivePath = BuildArchivedMarkerPath(markerPath);
                File.Move(markerPath, archivePath);
                logger?.LogInformation("Archived MCP marker file from {Path} to {ArchivePath}", markerPath, archivePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger?.LogWarning(ex, "Failed to remove MCP marker file: {Path}", markerPath);
        }
    }

    private static string BuildArchivedMarkerPath(string markerPath)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ", CultureInfo.InvariantCulture);
        var candidate = $"{markerPath}.deleted-{timestamp}";
        for (var attempt = 1; File.Exists(candidate); attempt++)
            candidate = $"{markerPath}.deleted-{timestamp}-{attempt}";

        return candidate;
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
        string Version(string pluginName, string environmentVariableName) =>
            ResolveAgentPluginVersion(workspacePath, pluginName, environmentVariableName);

        return new MarkerAgentPlugins
        {
            Policy = "required",
            Agents = new Dictionary<string, MarkerAgentPluginContract>(StringComparer.Ordinal)
            {
                ["Codex"] = new()
                {
                    SourceType = "Codex",
                    PluginName = "mcpserver-codex-plugin",
                    PluginVersion = Version("mcpserver-codex-plugin", "CODEX_PLUGIN_ROOT"),
                    Activation = "Codex hook lifecycle through .codex-plugin/plugin.json.",
                    StartupCommand = "lib/session-start.sh \"{workspacePath}\"",
                    UnavailableFailure = "MCP_PLUGIN_UNAVAILABLE:Codex",
                    RequiredEnvVars = ["CODEX_PLUGIN_ROOT", "PLUGIN_AGENT_NAME=Codex"],
                    HookExpectations = ["SessionStart", "UserPromptSubmit", "PostToolUse", "Stop"],
                    ToolExpectations = ["workflow.sessionlog.*", "workflow.todo.*", "workflow.requirements.*", "workflow.triage.*"],
                    RootHints = [Sibling("mcpserver-codex-plugin"), "$CODEX_PLUGIN_ROOT"],
                },
                ["Claude"] = new()
                {
                    SourceType = "Claude",
                    PluginName = "mcpserver-claude-code-plugin",
                    PluginVersion = Version("mcpserver-claude-code-plugin", "CLAUDE_PLUGIN_ROOT"),
                    Activation = "Claude Code plugin hooks and .mcp.json mcpserver entry.",
                    StartupCommand = "hooks/session-start.sh \"{workspacePath}\"",
                    UnavailableFailure = "MCP_PLUGIN_UNAVAILABLE:Claude",
                    RequiredEnvVars = ["CLAUDE_PLUGIN_ROOT", "PLUGIN_AGENT_NAME=Claude"],
                    HookExpectations = ["SessionStart", "UserPromptSubmit", "PostToolUse", "Stop"],
                    ToolExpectations = ["mcpserver session tools", "mcpserver todo tools", "mcpserver requirements tools", "mcpserver triage tools"],
                    RootHints = [Sibling("mcpserver-claude-code-plugin"), "$CLAUDE_PLUGIN_ROOT"],
                },
                ["Copilot"] = new()
                {
                    SourceType = "Copilot",
                    PluginName = "mcpserver-copilot-plugin",
                    PluginVersion = Version("mcpserver-copilot-plugin", "COPILOT_PLUGIN_ROOT"),
                    Activation = "Copilot plugin hooks and .mcp.json mcpserver entry.",
                    StartupCommand = "hooks/session-start.sh \"{workspacePath}\"",
                    UnavailableFailure = "MCP_PLUGIN_UNAVAILABLE:Copilot",
                    RequiredEnvVars = ["COPILOT_PLUGIN_ROOT", "PLUGIN_AGENT_NAME=Copilot"],
                    HookExpectations = ["SessionStart", "UserPromptSubmit", "PostToolUse", "Stop"],
                    ToolExpectations = ["mcpserver session tools", "mcpserver todo tools", "mcpserver requirements tools", "mcpserver triage tools"],
                    RootHints = [Sibling("mcpserver-copilot-plugin"), "$COPILOT_PLUGIN_ROOT"],
                },
                ["Cline"] = new()
                {
                    SourceType = "Cline",
                    PluginName = "mcpserver-cline-plugin",
                    PluginVersion = Version("mcpserver-cline-plugin", "CLINE_PLUGIN_ROOT"),
                    Activation = "Cline MCP server configured from server.json.",
                    StartupCommand = "npm run build && node dist/index.js",
                    UnavailableFailure = "MCP_PLUGIN_UNAVAILABLE:Cline",
                    RequiredEnvVars = ["CLINE_PLUGIN_ROOT", "PLUGIN_AGENT_NAME=Cline"],
                    HookExpectations = ["MCP server startup", "tool call audit"],
                    ToolExpectations = ["session_*", "req_*", "triage_*"],
                    RootHints = [Sibling("mcpserver-cline-plugin"), "$CLINE_PLUGIN_ROOT"],
                },
                ["Grok"] = new()
                {
                    SourceType = "GrokCode",
                    PluginName = "mcpserver-grok-plugin",
                    PluginVersion = Version("mcpserver-grok-plugin", "GROK_PLUGIN_ROOT"),
                    Activation = "Grok Build loads enabled plugin skills, hooks, and MCP servers from the Grok/Claude-compatible plugin manifests. Use sessionlog_*, todo_*, and requirements_* tool names when the Streamable HTTP MCP server is discoverable; mcp_* names are hosted-agent aliases, and workflow.* names are plugin shim/REPL method names invoked through the Grok plugin skills or repl-invoke helpers, not literal Grok search_tool results.",
                    StartupCommand = "",
                    UnavailableFailure = "MCP_PLUGIN_UNAVAILABLE:GrokCode",
                    RequiredEnvVars = ["GROK_PLUGIN_ROOT", "PLUGIN_AGENT_NAME=GrokCode"],
                    HookExpectations = ["SessionStart", "UserPromptSubmit", "PostToolUse", "Stop", "PlanMode"],
                    ToolExpectations = ["sessionlog_*", "todo_*", "requirements_*", "triage_*"],
                    RootHints = [Sibling("mcpserver-grok-plugin"), "$GROK_PLUGIN_ROOT"],
                },
            },
        };
    }

    internal static string ResolveAgentPluginVersion(
        string workspacePath,
        string pluginName,
        string environmentVariableName)
    {
        foreach (var root in EnumerateAgentPluginVersionRoots(workspacePath, pluginName, environmentVariableName))
        {
            var version = TryReadAgentPluginVersion(root);
            if (!string.IsNullOrWhiteSpace(version))
                return version;
        }

        return SyncedAgentPluginVersion;
    }

    private static IEnumerable<string> EnumerateAgentPluginVersionRoots(
        string workspacePath,
        string pluginName,
        string environmentVariableName)
    {
        var environmentRoot = Environment.GetEnvironmentVariable(environmentVariableName);
        if (!string.IsNullOrWhiteSpace(environmentRoot))
            yield return environmentRoot;

        if (!string.IsNullOrWhiteSpace(workspacePath))
        {
            var siblingRoot = Path.GetDirectoryName(Path.GetFullPath(workspacePath));
            if (!string.IsNullOrWhiteSpace(siblingRoot))
                yield return Path.Combine(siblingRoot, pluginName);
        }

        var userProfile = AgentPluginUserProfileOverride
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            foreach (var candidate in EnumerateUserPluginCacheRoots(userProfile, pluginName))
                yield return candidate;
        }
    }

    private static IEnumerable<string> EnumerateUserPluginCacheRoots(string userProfile, string pluginName)
    {
        var codexCache = Path.Combine(userProfile, ".codex", "plugins", "cache", pluginName);
        if (Directory.Exists(codexCache))
        {
            foreach (var candidate in OrderPluginVersionRoots(Directory.EnumerateDirectories(codexCache, "*", SearchOption.AllDirectories)))
                yield return candidate;
        }

        var claudeCache = Path.Combine(userProfile, ".claude", "plugins", "cache");
        if (Directory.Exists(claudeCache))
        {
            var candidates = Directory.EnumerateDirectories(claudeCache, "*", SearchOption.AllDirectories)
                .Where(path => path.Contains(pluginName, StringComparison.OrdinalIgnoreCase)
                    || Path.GetFileName(path).Contains("mcpserver", StringComparison.OrdinalIgnoreCase));
            foreach (var candidate in OrderPluginVersionRoots(candidates))
                yield return candidate;
        }

        var grokCache = Path.Combine(userProfile, ".grok", "installed-plugins");
        if (Directory.Exists(grokCache))
        {
            var candidates = Directory.EnumerateDirectories(grokCache, "*", SearchOption.AllDirectories)
                .Where(path => path.Contains(pluginName, StringComparison.OrdinalIgnoreCase)
                    || Path.GetFileName(path).Contains("mcpserver", StringComparison.OrdinalIgnoreCase));
            foreach (var candidate in OrderPluginVersionRoots(candidates))
                yield return candidate;
        }
    }

    private static IEnumerable<string> OrderPluginVersionRoots(IEnumerable<string> candidates)
    {
        return candidates
            .Select(path => new { Path = path, Version = GetPluginVersionSortKey(path) })
            .OrderByDescending(candidate => candidate.Version)
            .ThenByDescending(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .Select(candidate => candidate.Path);
    }

    private static System.Version GetPluginVersionSortKey(string root)
    {
        var versionText = TryReadAgentPluginVersion(root);
        if (string.IsNullOrWhiteSpace(versionText))
            return new System.Version(0, 0);

        var normalized = versionText.Split('+')[0].Split('-')[0];
        return System.Version.TryParse(normalized, out var parsed)
            ? parsed
            : new System.Version(0, 0);
    }

    private static string? TryReadAgentPluginVersion(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return null;

        var versionFile = Path.Combine(root, ".version");
        if (File.Exists(versionFile))
        {
            var version = File.ReadAllText(versionFile).Trim();
            if (!string.IsNullOrWhiteSpace(version))
                return version;
        }

        foreach (var manifest in new[]
                 {
                     "plugin.json",
                     Path.Combine(".codex-plugin", "plugin.json"),
                     Path.Combine(".claude-plugin", "plugin.json"),
                     Path.Combine(".grok-plugin", "plugin.json"),
                     "package.json",
                 })
        {
            var path = Path.Combine(root, manifest);
            if (!File.Exists(path))
                continue;

            var version = TryReadVersionFromJson(path);
            if (!string.IsNullOrWhiteSpace(version))
                return version;
        }

        return null;
    }

    private static string? TryReadVersionFromJson(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.TryGetProperty("version", out var version)
                && version.ValueKind == JsonValueKind.String)
            {
                return version.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
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
