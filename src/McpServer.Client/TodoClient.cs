using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for TODO management endpoints (<c>/mcp/todo</c>). Provides full CRUD operations
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

    /// <summary>Query TODO items with optional filters.</summary>
    public async Task<TodoQueryResult> QueryAsync(
        string? keyword = null, string? priority = null, string? section = null,
        string? id = null, bool? done = null, CancellationToken cancellationToken = default)
    {
        var qs = BuildQueryString(keyword, priority, section, id, done);
        return await GetAsync<TodoQueryResult>($"mcp/todo{qs}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Get a single TODO item by ID.</summary>
    public async Task<TodoFlatItem> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TodoFlatItem>($"mcp/todo/{Encode(id)}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Create a new TODO item.</summary>
    public async Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TodoMutationResult>("mcp/todo", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Update an existing TODO item.</summary>
    public async Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<TodoMutationResult>($"mcp/todo/{Encode(id)}", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Delete a TODO item.</summary>
    public async Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<TodoMutationResult>($"mcp/todo/{Encode(id)}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Analyze requirements for a TODO item via Copilot.</summary>
    public async Task<RequirementsAnalysisResult> AnalyzeRequirementsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementsAnalysisResult>($"mcp/todo/{Encode(id)}/requirements", null, cancellationToken).ConfigureAwait(false);
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
        => StreamSseAsync($"mcp/todo/{Encode(id)}/prompt/status", cancellationToken);

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
        => StreamSseAsync($"mcp/todo/{Encode(id)}/prompt/implement", cancellationToken);

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
        => StreamSseAsync($"mcp/todo/{Encode(id)}/prompt/plan", cancellationToken);

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
