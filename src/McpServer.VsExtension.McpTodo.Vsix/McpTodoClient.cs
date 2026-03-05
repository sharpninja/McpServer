using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.VsExtension.McpTodo.Models;

namespace McpServer.VsExtension.McpTodo;

public sealed class McpTodoClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string BaseUrl { get; }


    public McpTodoClient(string baseUrl = "http://localhost:7147")
    {
        BaseUrl = (baseUrl ?? "http://localhost:7147").TrimEnd('/');
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Checks if the MCP server is reachable. If not, attempts to start it
    /// via Start-McpServer.ps1 and waits for it to become healthy.
    /// </summary>
    /// <returns>True if the server had to be started; false if already running.</returns>
    public async Task<bool> EnsureServerRunningAsync(string? solutionDir = null, CancellationToken cancellationToken = default)
    {
        if (await IsHealthyAsync(cancellationToken).ConfigureAwait(false))
            return false;

        CopilotOutputPane.Log("MCP server is not running. Attempting to start...");

        var scriptPath = FindStartScript(solutionDir);
        if (scriptPath == null)
        {
            CopilotOutputPane.Log("Could not find scripts\\Start-McpServer.ps1. MCP server must be started manually.");
            return false;
        }

        CopilotOutputPane.Log($"Starting MCP server via {scriptPath}");
        var psi = new ProcessStartInfo
        {
            FileName = "pwsh",
            Arguments = $"-NoProfile -File \"{scriptPath}\"",
            WorkingDirectory = Path.GetDirectoryName(Path.GetDirectoryName(scriptPath)) ?? ".",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            CopilotOutputPane.Log($"Failed to start MCP server process: {ex}");
            CopilotOutputPane.Log($"Failed to start MCP server process: {ex.Message}");
            return false;
        }

        // Wait for the server to become healthy (up to ~30 seconds)
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
            if (await IsHealthyAsync(cancellationToken).ConfigureAwait(false))
            {
                CopilotOutputPane.Log("MCP server started successfully.");
                return true;
            }
        }

        CopilotOutputPane.Log("MCP server did not become healthy within 30 seconds.");
        return false;
    }

    private async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            var response = await _http.GetAsync("/health", cts.Token).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindStartScript(string? solutionDir)
    {
        if (string.IsNullOrEmpty(solutionDir))
            return null;

        var candidate = Path.Combine(solutionDir!, "scripts", "Start-McpServer.ps1");
        return File.Exists(candidate) ? candidate : null;
    }

    public async Task<TodoQueryResult> GetTodoListAsync(bool? done = false, string? priority = null, string? keyword = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (done.HasValue) query.Add("done=" + done.Value.ToString().ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(priority)) query.Add("priority=" + Uri.EscapeDataString(priority!));
        if (!string.IsNullOrWhiteSpace(keyword)) query.Add("keyword=" + Uri.EscapeDataString(keyword!));
        var path = query.Count > 0 ? "/mcpserver/todo?" + string.Join("&", query) : "/mcpserver/todo";
        var response = await _http.GetAsync(path, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<TodoQueryResult>(json, s_jsonOptions);
        return result ?? new TodoQueryResult { Items = new List<TodoFlatItem>(), TotalCount = 0 };
    }

    public async Task<TodoFlatItem?> GetTodoByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var path = "/mcpserver/todo/" + Uri.EscapeDataString(id);
        var response = await _http.GetAsync(path, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<TodoFlatItem>(json, s_jsonOptions);
    }

    public async Task<TodoMutationResult> UpdateTodoAsync(string id, TodoUpdateBody body, CancellationToken cancellationToken = default)
    {
        var path = "/mcpserver/todo/" + Uri.EscapeDataString(id);
        using var content = new StringContent(JsonSerializer.Serialize(body, s_jsonOptions), System.Text.Encoding.UTF8, "application/json");
        var response = await _http.PutAsync(path, content, cancellationToken).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var err = JsonSerializer.Deserialize<TodoMutationResult>(json, s_jsonOptions);
            return err ?? new TodoMutationResult { Success = false, Error = response.ReasonPhrase ?? response.StatusCode.ToString() };
        }
        return JsonSerializer.Deserialize<TodoMutationResult>(json, s_jsonOptions) ?? new TodoMutationResult { Success = true };
    }
}
