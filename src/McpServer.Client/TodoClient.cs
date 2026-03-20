using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for TODO management endpoints (<c>/mcpserver/todo</c>). Provides full CRUD operations
/// on TODO items and a Copilot-powered requirements analysis endpoint.
///
/// <para>All methods read <see cref="McpClientBase.ApiKey"/> and <see cref="McpClientBase.Port"/>
/// at call time, allowing runtime re-targeting without recreating the client.</para>
/// </summary>
/// <seealso cref="McpServerClient.Todo"/>
public sealed class TodoClient : McpClientBase
{
    /// <inheritdoc />
    public TodoClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal TodoClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Query TODO items with optional filters.</summary>
    public async Task<TodoQueryResult> QueryAsync(
        string? keyword = null, string? priority = null, string? section = null,
        string? id = null, bool? done = null, CancellationToken cancellationToken = default)
    {
        var qs = BuildQueryString(keyword, priority, section, id, done);
        return await GetAsync<TodoQueryResult>($"mcpserver/todo{qs}", cancellationToken);
    }

    /// <summary>Get a single TODO item by ID.</summary>
    public async Task<TodoFlatItem> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TodoFlatItem>($"mcpserver/todo/{Encode(id)}", cancellationToken);
    }

    /// <summary>Get append-only audit history for a TODO item.</summary>
    public async Task<TodoAuditQueryResult> GetAuditAsync(
        string id,
        int? limit = null,
        int? offset = null,
        CancellationToken cancellationToken = default)
    {
        var parts = new List<string>();
        if (limit.HasValue) parts.Add($"limit={limit.Value}");
        if (offset.HasValue) parts.Add($"offset={offset.Value}");
        var suffix = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
        return await GetAsync<TodoAuditQueryResult>($"mcpserver/todo/{Encode(id)}/audit{suffix}", cancellationToken);
    }

    /// <summary>Create a new TODO item.</summary>
    public async Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TodoMutationResult>("mcpserver/todo", request, cancellationToken);
    }

    /// <summary>Update an existing TODO item.</summary>
    public async Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<TodoMutationResult>($"mcpserver/todo/{Encode(id)}", request, cancellationToken);
    }

    /// <summary>Delete a TODO item.</summary>
    public async Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<TodoMutationResult>($"mcpserver/todo/{Encode(id)}", cancellationToken);
    }

    /// <summary>Analyze requirements for a TODO item via Copilot.</summary>
    public async Task<RequirementsAnalysisResult> AnalyzeRequirementsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementsAnalysisResult>($"mcpserver/todo/{Encode(id)}/requirements", null, cancellationToken);
    }

    /// <summary>
    /// Streams a Copilot-generated status report for the specified TODO item via SSE.
    /// Each yielded string is one line of the report, delivered in real-time as the
    /// server generates it.
    /// </summary>
    /// <param name="id">TODO item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of status-report lines.</returns>
    /// <example>
    /// <code>
    /// await foreach (var line in client.Todo.StreamStatusAsync("MVP-APP-001"))
    ///     Console.WriteLine(line);
    /// </code>
    /// </example>
    /// <seealso cref="StreamImplementAsync"/>
    /// <seealso cref="StreamPlanAsync"/>
    public IAsyncEnumerable<string> StreamStatusAsync(string id, CancellationToken cancellationToken = default)
        => StreamSseAsync($"mcpserver/todo/{Encode(id)}/prompt/status", cancellationToken);

    /// <summary>
    /// Streams a Copilot-generated implementation guide for the specified TODO item via SSE.
    /// Each yielded string is one line of the guide, delivered in real-time as the
    /// server generates it.
    /// </summary>
    /// <param name="id">TODO item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of implementation-guide lines.</returns>
    /// <example>
    /// <code>
    /// await foreach (var line in client.Todo.StreamImplementAsync("MVP-APP-001"))
    ///     Console.WriteLine(line);
    /// </code>
    /// </example>
    /// <seealso cref="StreamStatusAsync"/>
    /// <seealso cref="StreamPlanAsync"/>
    public IAsyncEnumerable<string> StreamImplementAsync(string id, CancellationToken cancellationToken = default)
        => StreamSseAsync($"mcpserver/todo/{Encode(id)}/prompt/implement", cancellationToken);

    /// <summary>
    /// Streams a Copilot-generated plan for the specified TODO item via SSE.
    /// Each yielded string is one line of the plan, delivered in real-time as the
    /// server generates it.
    /// </summary>
    /// <param name="id">TODO item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of plan lines.</returns>
    /// <example>
    /// <code>
    /// await foreach (var line in client.Todo.StreamPlanAsync("MVP-APP-001"))
    ///     Console.WriteLine(line);
    /// </code>
    /// </example>
    /// <seealso cref="StreamStatusAsync"/>
    /// <seealso cref="StreamImplementAsync"/>
    public IAsyncEnumerable<string> StreamPlanAsync(string id, CancellationToken cancellationToken = default)
        => StreamSseAsync($"mcpserver/todo/{Encode(id)}/prompt/plan", cancellationToken);

    private static string Encode(string value) => System.Uri.EscapeDataString(value);

    private static string BuildQueryString(string? keyword, string? priority, string? section, string? id, bool? done)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (keyword is not null) parts.Add($"keyword={Encode(keyword)}");
        if (priority is not null) parts.Add($"priority={Encode(priority)}");
        if (section is not null) parts.Add($"section={Encode(section)}");
        if (id is not null) parts.Add($"id={Encode(id)}");
        if (done.HasValue) parts.Add($"done={done.Value.ToString().ToLowerInvariant()}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }
}
