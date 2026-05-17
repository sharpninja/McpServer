using System.Net.Http.Json;
using System.Text.Json;
using McpServer.Support.Mcp.GraphRag;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp;

/// <summary>
/// FR-MCP-082/083/084/085: Production implementation of both <see cref="IFederationDataClient"/>
/// and <see cref="IGraphRagFederationClient"/> that calls remote MCP server REST endpoints
/// via <see cref="IHttpClientFactory"/>. Uses the named HttpClient <c>"FederationData"</c>.
/// Returns <c>null</c> on any remote failure so decorators fall back to local-only results.
/// </summary>
public sealed class FederationDataClient : IFederationDataClient, IGraphRagFederationClient
{
    /// <summary>Named HttpClient identifier used by this client.</summary>
    public const string HttpClientName = "FederationData";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FederationDataClient> _logger;

    /// <summary>Initializes a new instance of the <see cref="FederationDataClient"/> class.</summary>
    /// <param name="httpClientFactory">Factory for creating named HTTP clients.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FederationDataClient(IHttpClientFactory httpClientFactory, ILogger<FederationDataClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ── IFederationDataClient (Todo + SessionLog) ──

    /// <inheritdoc />
    public async Task<TodoQueryResult?> QueryTodosAsync(FederationTarget target, TodoQueryRequest request, CancellationToken ct = default)
    {
        var qs = BuildTodoQueryString(request);
        return await GetAsync<TodoQueryResult>(target, $"mcpserver/todo{qs}", ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TodoFlatItem?> GetTodoByIdAsync(FederationTarget target, string id, CancellationToken ct = default)
        => await GetAsync<TodoFlatItem>(target, $"mcpserver/todo/{Uri.EscapeDataString(id)}", ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<SessionLogQueryResult?> QuerySessionLogsAsync(FederationTarget target, SessionLogQueryRequest request, CancellationToken ct = default)
    {
        var qs = BuildSessionLogQueryString(request);
        return await GetAsync<SessionLogQueryResult>(target, $"mcpserver/sessionlog{qs}", ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FederationPushResult> PushTodosAsync(FederationTarget target, IReadOnlyList<TodoFlatItem> items, CancellationToken ct = default)
    {
        var succeeded = 0;
        var errors = new List<string>();

        foreach (var item in items)
        {
            try
            {
                var createReq = new TodoCreateRequest
                {
                    Id = item.Id,
                    Title = item.Title,
                    Section = item.Section,
                    Priority = item.Priority,
                    Estimate = item.Estimate,
                    Description = item.Description,
                    TechnicalDetails = item.TechnicalDetails,
                    ImplementationTasks = item.ImplementationTasks,
                    Note = item.Note,
                };
                await PostAsync(target, "mcpserver/todo", createReq, ct).ConfigureAwait(false);
                succeeded++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errors.Add($"TODO {item.Id}: {ex.Message}");
            }
        }

        return new FederationPushResult(succeeded, errors.Count, errors);
    }

    /// <inheritdoc />
    public async Task<FederationPushResult> PushSessionLogsAsync(FederationTarget target, IReadOnlyList<UnifiedSessionLogDto> items, CancellationToken ct = default)
    {
        var succeeded = 0;
        var errors = new List<string>();

        foreach (var item in items)
        {
            try
            {
                await PostAsync(target, "mcpserver/sessionlog", item, ct).ConfigureAwait(false);
                succeeded++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errors.Add($"SessionLog {item.SourceType}/{item.SessionId}: {ex.Message}");
            }
        }

        return new FederationPushResult(succeeded, errors.Count, errors);
    }

    // ── IGraphRagFederationClient ──

    /// <inheritdoc />
    public async Task<GraphEntityListResponse?> QueryEntitiesAsync(FederationTarget target, int skip, int take, string? entityType, CancellationToken ct = default)
    {
        var qs = $"?skip={skip}&take={take}";
        if (entityType is not null)
            qs += $"&entityType={Uri.EscapeDataString(entityType)}";
        return await GetAsync<GraphEntityListResponse>(target, $"mcpserver/graphrag/entities{qs}", ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphRelationshipListResponse?> QueryRelationshipsAsync(FederationTarget target, int skip, int take, string? entityId, string? relationshipType, CancellationToken ct = default)
    {
        var qs = $"?skip={skip}&take={take}";
        if (entityId is not null)
            qs += $"&entityId={Uri.EscapeDataString(entityId)}";
        if (relationshipType is not null)
            qs += $"&relationshipType={Uri.EscapeDataString(relationshipType)}";
        return await GetAsync<GraphRelationshipListResponse>(target, $"mcpserver/graphrag/relationships{qs}", ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphRagDocumentListResponse?> QueryDocumentsAsync(FederationTarget target, int skip, int take, string? sourceType, CancellationToken ct = default)
    {
        var qs = $"?skip={skip}&take={take}";
        if (sourceType is not null)
            qs += $"&sourceType={Uri.EscapeDataString(sourceType)}";
        return await GetAsync<GraphRagDocumentListResponse>(target, $"mcpserver/graphrag/documents{qs}", ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphRagQueryResponse?> QueryGraphRagAsync(FederationTarget target, GraphRagQueryRequest request, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient(target);
            var response = await client.PostAsJsonAsync($"{target.BaseUrl}/mcpserver/graphrag/query", request, JsonOptions, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Federation GraphRAG query returned {StatusCode} from {Target}", (int)response.StatusCode, target.Name);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<GraphRagQueryResponse>(JsonOptions, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Federation GraphRAG query to {Target} failed", target.Name);
            return null;
        }
    }

    // ── HTTP helpers ──

    private async Task<T?> GetAsync<T>(FederationTarget target, string path, CancellationToken ct) where T : class
    {
        try
        {
            var client = CreateClient(target);
            var response = await client.GetAsync($"{target.BaseUrl}/{path}", ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Federation GET {Path} returned {StatusCode} from {Target}", path, (int)response.StatusCode, target.Name);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Federation GET {Path} to {Target} failed", path, target.Name);
            return null;
        }
    }

    private async Task PostAsync<T>(FederationTarget target, string path, T body, CancellationToken ct)
    {
        var client = CreateClient(target);
        var response = await client.PostAsJsonAsync($"{target.BaseUrl}/{path}", body, JsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private HttpClient CreateClient(FederationTarget target)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        if (target.ApiKey is not null)
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", target.ApiKey);
        return client;
    }

    private static string BuildTodoQueryString(TodoQueryRequest request)
    {
        var parts = new List<string>();
        if (request.Keyword is not null) parts.Add($"keyword={Uri.EscapeDataString(request.Keyword)}");
        if (request.Priority is not null) parts.Add($"priority={Uri.EscapeDataString(request.Priority)}");
        if (request.Section is not null) parts.Add($"section={Uri.EscapeDataString(request.Section)}");
        if (request.Id is not null) parts.Add($"id={Uri.EscapeDataString(request.Id)}");
        if (request.Done is not null) parts.Add($"done={request.Done.Value.ToString().ToLowerInvariant()}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }

    private static string BuildSessionLogQueryString(SessionLogQueryRequest request)
    {
        var parts = new List<string>();
        if (request.Agent is not null) parts.Add($"agent={Uri.EscapeDataString(request.Agent)}");
        if (request.Model is not null) parts.Add($"model={Uri.EscapeDataString(request.Model)}");
        if (request.Text is not null) parts.Add($"text={Uri.EscapeDataString(request.Text)}");
        if (request.From is not null) parts.Add($"from={Uri.EscapeDataString(request.From.Value.ToString("O"))}");
        if (request.To is not null) parts.Add($"to={Uri.EscapeDataString(request.To.Value.ToString("O"))}");
        parts.Add($"limit={request.Limit}");
        parts.Add($"offset={request.Offset}");
        return "?" + string.Join("&", parts);
    }
}
