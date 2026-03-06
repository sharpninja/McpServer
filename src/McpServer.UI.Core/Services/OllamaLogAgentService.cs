using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Services;

/// <summary>Log agent that uses a local Ollama server (http://localhost:11434).</summary>
public sealed class OllamaLogAgentService : ILogAgentService
{
    private static readonly ILogger _logger = AppLogService.Instance.CreateLogger("Ollama");
    private static readonly Regex AnsiEscapePattern = new(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);
    private const string DefaultModel = "llama3:latest";
    private const string DefaultBaseUrl = "http://localhost:11434";
    private static readonly string SystemPrompt = "You are an assistant for a log viewer app. The user sees request logs (Cursor, Copilot, or unified JSON). You receive a summary of the current view (filtered list and optionally the selected request). Help them query, filter, and understand the logs. Be concise and practical.";

    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _baseUrl;

    public OllamaLogAgentService(string? model = null, string? baseUrl = null)
    {
        _model = model ?? DefaultModel;
        _baseUrl = (baseUrl ?? DefaultBaseUrl).TrimEnd('/');
        _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl), Timeout = TimeSpan.FromMinutes(2) };
    }

    public async Task<string> SendMessageAsync(string userMessage, string contextSummary, string? model = null, IProgress<string>? contentProgress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return "Please enter a message.";

        var messages = new List<object>
        {
            new { role = "system", content = SystemPrompt + "\n\nCurrent log context:\n" + (string.IsNullOrWhiteSpace(contextSummary) ? "(No log loaded or no entries)" : contextSummary) },
            new { role = "user", content = userMessage }
        };

        var modelToUse = !string.IsNullOrWhiteSpace(model) ? model!.Trim() : _model;
        var stream = contentProgress != null;
        var payload = new
        {
            model = modelToUse,
            messages,
            stream
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            if (stream)
                return await SendMessageStreamingAsync(content, modelToUse, contentProgress!, cancellationToken).ConfigureAwait(true);

            var response = await _httpClient.PostAsync("/api/chat", content, cancellationToken).ConfigureAwait(true);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
                return $"Ollama error ({(int)response.StatusCode}): {response.ReasonPhrase}. Ensure Ollama is running (e.g. run 'ollama serve' and 'ollama run " + modelToUse + "').";
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("message", out var msgEl) && msgEl.TryGetProperty("content", out var contentEl))
                return contentEl.GetString() ?? "";
            return "Unexpected response from Ollama.";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "[Ollama] Cannot reach Ollama server");
            return "Cannot reach Ollama. Is it running? (e.g. 'ollama serve' and 'ollama run " + modelToUse + "'). " + ex.Message;
        }
        catch (TaskCanceledException)
        {
            return "[Cancelled]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Ollama] Unexpected error during chat");
            return "Error: " + ex.Message;
        }
    }

    private async Task<string> SendMessageStreamingAsync(StringContent content, string modelToUse, IProgress<string> contentProgress, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/api/chat") { Content = content };
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(true);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
            return $"Ollama error ({(int)response.StatusCode}): {response.ReasonPhrase}. Ensure Ollama is running (e.g. run 'ollama serve' and 'ollama run " + modelToUse + "').";
        }

        var accumulated = new StringBuilder();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(true);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024);

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(true) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (root.TryGetProperty("message", out var msgEl) && msgEl.TryGetProperty("content", out var contentEl))
                {
                    var chunk = contentEl.GetString();
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        accumulated.Append(AnsiEscapePattern.Replace(chunk, ""));
                        contentProgress.Report(accumulated.ToString());
                    }
                }
                if (root.TryGetProperty("done", out var doneEl) && doneEl.GetBoolean())
                    break;
            }
            catch (JsonException)
            {
                // Skip malformed line
            }
        }

        return accumulated.ToString();
    }

    /// <summary>Fetches available model names from the Ollama server (GET /api/tags).</summary>
    public static async Task<string[]> GetAvailableModelsAsync(string? baseUrl = null, CancellationToken cancellationToken = default)
    {
        var url = (baseUrl ?? DefaultBaseUrl).TrimEnd('/');
        using var client = new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(10) };
        var response = await client.GetAsync("/api/tags", cancellationToken).ConfigureAwait(true);
        if (!response.IsSuccessStatusCode) return Array.Empty<string>();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("models", out var modelsEl)) return Array.Empty<string>();
        var list = new List<string>();
        foreach (var m in modelsEl.EnumerateArray())
        {
            if (m.TryGetProperty("name", out var nameEl))
            {
                var name = nameEl.GetString();
                if (!string.IsNullOrEmpty(name)) list.Add(name);
            }
        }
        return list.ToArray();
    }

    /// <summary>If the Ollama server is not reachable, starts "ollama serve" and returns. Call on app startup.</summary>
    public static void TryStartOllamaIfNeeded()
    {
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(DefaultBaseUrl), Timeout = TimeSpan.FromSeconds(2) };
            var response = client.GetAsync("/api/tags").GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode) return;
        }
        catch
        {
            // Not running or not reachable – expected on cold start
            _logger.LogDebug("[Ollama] Server not reachable; will attempt to start");
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = "serve",
                UseShellExecute = true,
                CreateNoWindow = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not start Ollama: {Message}", ex.Message);
        }
    }
}

