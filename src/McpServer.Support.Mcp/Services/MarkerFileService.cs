using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Writes and removes <c>.mcp-server.json</c> marker files in workspace roots so that
/// agents can discover the correct port and endpoints for calling the MCP server.
/// </summary>
public static class MarkerFileService
{
    /// <summary>Well-known marker file name placed at the workspace root.</summary>
    public const string MarkerFileName = ".mcp-server.json";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Writes the <c>.mcp-server.json</c> marker file to <paramref name="workspacePath"/>.
    /// </summary>
    public static async Task WriteMarkerAsync(
        string workspacePath,
        int port,
        string workspaceName,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        var baseUrl = $"http://localhost:{port.ToString(CultureInfo.InvariantCulture)}";
        var markerPath = Path.Combine(workspacePath, MarkerFileName);

        var marker = new MarkerFile
        {
            Port = port,
            BaseUrl = baseUrl,
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
            Prompt = BuildPrompt(baseUrl),
        };

        try
        {
            var json = JsonSerializer.Serialize(marker, s_jsonOptions);
            await File.WriteAllTextAsync(markerPath, json, ct).ConfigureAwait(false);
            logger?.LogInformation("Wrote MCP marker file: {Path}", markerPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger?.LogWarning(ex, "Failed to write MCP marker file: {Path}", markerPath);
        }
    }

    /// <summary>
    /// Removes the <c>.mcp-server.json</c> marker file from <paramref name="workspacePath"/>.
    /// </summary>
    public static void RemoveMarker(string workspacePath, ILogger? logger = null)
    {
        var markerPath = Path.Combine(workspacePath, MarkerFileName);
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

    private static string BuildPrompt(string baseUrl) =>
$@"You are connected to an MCP Context Server at {baseUrl}. Use this server to coordinate your work.

## Server Health
Before making API calls, verify the server is running: GET {baseUrl}/health — returns {{""status"":""healthy""}}.

## API Discovery
The full OpenAPI spec is at GET {baseUrl}/swagger/v1/swagger.json. The interactive Swagger UI is at {baseUrl}/swagger. Use the spec to understand request/response schemas before calling endpoints.

## Session Logging (REQUIRED)
You MUST log every request you process to the session log endpoint. This is critical for audit, continuity, and team visibility.

For each task or conversation turn:
1. POST {baseUrl}/mcp/sessionlog with a JSON body containing:
   - sourceType: your agent identifier (e.g. ""Cline"", ""Cursor"", ""Copilot"")
   - sessionId: a stable identifier for your current session
   - title: a brief summary of the session's purpose
   - model: the AI model you are using (e.g. ""claude-sonnet-4-20250514"")
   - started: ISO 8601 timestamp when the session began
   - lastUpdated: ISO 8601 timestamp of the latest activity
   - status: ""in_progress"" or ""completed""
   - entries: array of request entries, each with:
     - requestId: unique ID for this request within the session
     - timestamp: ISO 8601 timestamp
     - queryText: the full user query or task description
     - queryTitle: short summary of the query
     - response: your response text (can be a summary for long responses)
     - interpretation: your understanding of what was asked
     - status: ""completed"" or ""in_progress""
     - actions: array of {{ order, description, type, status, filePath }} for each action taken
     - model: the model used for this specific entry
     - tokenCount: approximate token count if available
     - tags: relevant tags (e.g. [""refactor"", ""bugfix"", ""feature""])
     - contextList: files or resources referenced

2. For long-running requests, stream your reasoning in real-time via:
   POST {baseUrl}/mcp/sessionlog/{{agent}}/{{sessionId}}/{{requestId}}/dialog
   Send an array of dialog items, each with:
   - timestamp: ISO 8601
   - role: ""model"", ""tool"", ""system"", or ""user""
   - content: the reasoning text, tool output, or observation
   - category: ""reasoning"", ""tool_call"", ""tool_result"", ""observation"", or ""decision""

3. At the end of each session or task, POST the final session log with status ""completed"" and all entries filled in.

## Available Capabilities
- Context Search: POST {baseUrl}/mcp/context/search — semantic + full-text hybrid search over indexed project documents
- Context Pack: POST {baseUrl}/mcp/context/pack — retrieve ordered context chunks for a topic
- Context Sources: GET {baseUrl}/mcp/context/sources — list all indexed document sources
- Todo Management: GET/POST/PUT/DELETE {baseUrl}/mcp/todo — query, create, update, and delete project tasks
- Repo Files: GET {baseUrl}/mcp/repo/file, POST {baseUrl}/mcp/repo/file, GET {baseUrl}/mcp/repo/list — read, write, and list repository files
- GitHub Integration: {baseUrl}/mcp/gh/issues, {baseUrl}/mcp/gh/pulls, {baseUrl}/mcp/gh/labels — issue, PR, and label management
- Sync: POST {baseUrl}/mcp/sync/run — trigger full ingestion sync; GET {baseUrl}/mcp/sync/status — check sync status
- Tool Registry: GET {baseUrl}/mcp/tools/search — discover available tools; GET/POST {baseUrl}/mcp/tools — manage tool definitions
- MCP Protocol: {baseUrl}/mcp-transport — Model Context Protocol streamable HTTP transport endpoint";
}

/// <summary>Serialization model for the <c>.mcp-server.json</c> marker file.</summary>
internal sealed class MarkerFile
{
    public int Port { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
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
