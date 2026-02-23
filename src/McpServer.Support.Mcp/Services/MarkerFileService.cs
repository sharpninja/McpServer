using System.Diagnostics;
using System.Globalization;
using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
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

    /// <summary>
    /// Built-in default prompt template using Handlebars syntax.
    /// Available context: <c>{{baseUrl}}</c>, <c>{{apiKey}}</c>, <c>{{workspace.*}}</c> (all WorkspaceDto fields).
    /// Used when no <c>Mcp:MarkerPromptTemplate</c> is configured.
    /// </summary>
    public const string DefaultPromptTemplate =
        """
        You are connected to an MCP Context Server at {{baseUrl}}. Use this server to coordinate your work.

        ## Glossary

        | Term | Definition |
        |------|-----------|
        | MCP | Model Context Protocol — an open standard for tool-calling between AI agents and context servers. |
        | Workspace | A project directory registered with the MCP server. Each workspace has its own port, data directory, and auth token. |
        | Marker File | The `AGENTS-README-FIRST.yaml` file placed at each workspace root. Contains connection details, auth token, and this prompt. |
        | API Key | A per-workspace cryptographic token that rotates on each server restart. Required for all `/mcp/*` REST endpoints. |
        | Streamable HTTP | The MCP wire protocol transport at `/mcp-transport`. Carries JSON-RPC tool calls over HTTP POST with streaming responses. |
        | Session Log | An audit record of every agent interaction, stored per-session with full request/response history and reasoning dialog. |
        | Context Pack | An ordered set of document chunks retrieved by semantic + full-text hybrid search, scoped to the workspace. |
        | Tool Bucket | A GitHub repository containing tool manifest files, similar to a Scoop package bucket. |

        ## Workspace Definition

        | Property | Value |
        |----------|-------|
        | Name | {{workspace.Name}} |
        | Path | {{workspace.WorkspacePath}} |
        | Port | {{workspace.WorkspacePort}} |
        | Primary | {{workspace.IsPrimary}} |
        | Enabled | {{workspace.IsEnabled}} |
        | Data Directory | {{workspace.DataDirectory}} |
        | Todo Path | {{workspace.TodoPath}} |
        | Tunnel Provider | {{workspace.TunnelProvider}} |
        | Created | {{workspace.DateTimeCreated}} |
        | Modified | {{workspace.DateTimeModified}} |

        ## Authentication (REQUIRED)
        All `/mcp/*` REST endpoints require a per-workspace auth token. Include it with every request:
        - **Header**: `X-Api-Key: {{apiKey}}`
        - **Or query param**: `?api_key={{apiKey}}`
        If you receive a 401 response, re-read this AGENTS-README-FIRST.yaml marker file — the token rotates on each server restart.

        ## Available Protocols
        This server supports multiple connection protocols:
        - **REST API**: All `/mcp/*` endpoints (requires `X-Api-Key` header). Full OpenAPI spec at GET {{baseUrl}}/swagger/v1/swagger.json. Interactive Swagger UI at {{baseUrl}}/swagger.
        - **MCP Streamable HTTP**: POST {{baseUrl}}/mcp-transport — Model Context Protocol transport for tool-calling agents. No API key required for MCP transport.
        - **Health Check**: GET {{baseUrl}}/health — returns {"status":"healthy"}. No API key required.

        ## Server Health
        Before making API calls, verify the server is running: GET {{baseUrl}}/health — returns {"status":"healthy"}.

        ## Session Logging (REQUIRED)
        You MUST log every request you process to the session log endpoint. This is critical for audit, continuity, and team visibility.

        For each task or conversation turn:
        1. POST {{baseUrl}}/mcp/sessionlog with a JSON body containing:
           - sourceType: YOUR agent identifier (e.g. "Cline", "Cursor", "Copilot")
           - sessionId: a stable identifier for your current session that is prefixed with YOUR agent identifier.  Do not reuse sessions from different agent sessions.
           - title: a brief summary of the session's purpose.  Keep up-to-date.
           - model: the AI model you are using (e.g. "claude-sonnet-4-20250514").  Create a new session log if changing models.
           - started: ISO 8601 timestamp when the session began
           - lastUpdated: ISO 8601 timestamp of the latest activity
           - status: "in_progress" or "completed"
           - entries: array of request entries, each with:
             - [REQUIRED] requestId: unique ID for this request within the session
             - [REQUIRED] timestamp: ISO 8601 timestamp
             - [REQUIRED] queryText: the full user query or task description
             - [REQUIRED] queryTitle: short summary of the query
             - [REQUIRED] response: your response text (verbatim, not summarized)
             - [REQUIRED] interpretation: your understanding of what was asked
             - [REQUIRED] status: "completed" or "in_progress"
             - [REQUIRED] actions: array of { order, description, type, status, filePath } for each action taken
             - [REQUIRED] model: the model used for this specific entry
             - [RECOMMENDED] tokenCount: approximate token count if available
             - [REQUIRED] tags: relevant tags (e.g. ["refactor", "bugfix", "feature"]) Update as needed.
             - [REQUIRED] contextList: files or resources referenced
             - [REQUIRED] Processing Dialog/Decisions.  See #2 below

        2. For all requests, stream your reasoning in real-time via:
           POST {{baseUrl}}/mcp/sessionlog/{agent}/{sessionId}/{requestId}/dialog
           Send an array of dialog items, each with:
           - timestamp: ISO 8601
           - role: "model", "tool", "system", or "user"
           - content: the reasoning text, tool output, or observation
           - category: "reasoning", "tool_call", "tool_result", "observation", or "decision"

        3. At the end of each session or task, POST the final session log with status "completed" and all entries filled in.

        ## Available Capabilities
        - Context Search: POST {{baseUrl}}/mcp/context/search — semantic + full-text hybrid search over indexed project documents
        - Context Pack: POST {{baseUrl}}/mcp/context/pack — retrieve ordered context chunks for a topic
        - Context Sources: GET {{baseUrl}}/mcp/context/sources — list all indexed document sources
        - Todo Management: GET/POST/PUT/DELETE {{baseUrl}}/mcp/todo — query, create, update, and delete project tasks
        - Repo Files: GET {{baseUrl}}/mcp/repo/file, POST {{baseUrl}}/mcp/repo/file, GET {{baseUrl}}/mcp/repo/list — read, write, and list repository files
        - GitHub Integration: {{baseUrl}}/mcp/gh/issues, {{baseUrl}}/mcp/gh/pulls, {{baseUrl}}/mcp/gh/labels — issue, PR, and label management
        - Sync: POST {{baseUrl}}/mcp/sync/run — trigger full ingestion sync; GET {{baseUrl}}/mcp/sync/status — check sync status
        - Tool Registry: GET {{baseUrl}}/mcp/tools/search — discover available tools; GET/POST {{baseUrl}}/mcp/tools — manage tool definitions
        - MCP Protocol: {{baseUrl}}/mcp-transport — Model Context Protocol streamable HTTP transport endpoint

        **THESE RULES MUST BE ADHERED TO AND THIS MARKER READ ON EACH NEW REQUEST BY THE USER.**
        """;

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
    /// Optional global Handlebars prompt template.
    /// When <see langword="null"/> or empty, the built-in default prompt is used.
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
    public static async Task WriteMarkerAsync(
        string workspacePath,
        int port,
        string workspaceName,
        ILogger? logger = null,
        CancellationToken ct = default,
        string? globalPromptTemplate = null,
        string? workspacePromptTemplate = null,
        string? apiKey = null,
        WorkspaceDto? workspace = null)
    {
        var baseUrl = $"http://localhost:{port.ToString(CultureInfo.InvariantCulture)}";
        var markerPath = Path.Combine(workspacePath, MarkerFileName);

        var templateContext = BuildTemplateContext(baseUrl, apiKey, workspace, workspacePath, workspaceName, port);

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
                SessionLog = "/mcp/sessionlog",
                SessionLogDialog = "/mcp/sessionlog/{agent}/{sessionId}/{requestId}/dialog",
                ContextSearch = "/mcp/context/search",
                ContextPack = "/mcp/context/pack",
                ContextSources = "/mcp/context/sources",
                Todo = "/mcp/todo",
                Repo = "/mcp/repo",
                Sync = "/mcp/sync",
                GitHub = "/mcp/gh",
                Tools = "/mcp/tools",
                Workspace = "/mcp/workspace",
            },
            Workspace = workspaceName,
            WorkspacePath = workspacePath,
            Pid = Environment.ProcessId,
            StartedAt = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
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
        var globalSource = string.IsNullOrWhiteSpace(globalPromptTemplate)
            ? DefaultPromptTemplate
            : globalPromptTemplate;

        var global = RenderHandlebars(globalSource, templateContext);

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
        string workspaceName,
        int port)
    {
        return new Dictionary<string, object?>
        {
            ["baseUrl"] = baseUrl,
            ["apiKey"] = apiKey ?? string.Empty,
            ["workspace"] = workspace is not null ? new Dictionary<string, object?>
            {
                ["Name"] = workspace.Name,
                ["WorkspacePath"] = workspace.WorkspacePath,
                ["TodoPath"] = workspace.TodoPath,
                ["DataDirectory"] = workspace.DataDirectory ?? workspace.WorkspacePath,
                ["WorkspacePort"] = workspace.WorkspacePort,
                ["TunnelProvider"] = workspace.TunnelProvider ?? "none",
                ["IsPrimary"] = workspace.IsPrimary,
                ["IsEnabled"] = workspace.IsEnabled,
                ["DateTimeCreated"] = workspace.DateTimeCreated.ToString("o", CultureInfo.InvariantCulture),
                ["DateTimeModified"] = workspace.DateTimeModified.ToString("o", CultureInfo.InvariantCulture),
                ["RunAs"] = workspace.RunAs ?? "default",
                ["PromptTemplate"] = workspace.PromptTemplate ?? string.Empty,
            } : new Dictionary<string, object?>
            {
                ["Name"] = workspaceName,
                ["WorkspacePath"] = workspacePath,
                ["TodoPath"] = string.Empty,
                ["DataDirectory"] = workspacePath,
                ["WorkspacePort"] = port,
                ["TunnelProvider"] = "none",
                ["IsPrimary"] = false,
                ["IsEnabled"] = true,
                ["DateTimeCreated"] = string.Empty,
                ["DateTimeModified"] = string.Empty,
                ["RunAs"] = "default",
                ["PromptTemplate"] = string.Empty,
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
    public string StartedAt { get; set; } = string.Empty;
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
    public string Sync { get; set; } = string.Empty;
    public string GitHub { get; set; } = string.Empty;
    public string Tools { get; set; } = string.Empty;
    public string Workspace { get; set; } = string.Empty;
}
