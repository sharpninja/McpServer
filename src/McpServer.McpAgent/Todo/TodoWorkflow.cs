using System.Text;
using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.McpAgent.Todo;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Default TODO workflow implementation that delegates directly to
/// <see cref="McpServer.Client.TodoClient"/> while exposing both streaming and buffered prompt helpers.
/// Existing server-side TODO identifiers, including legacy non-canonical IDs, are passed through
/// unchanged so the workflow matches the underlying transport surface.
/// </summary>
public sealed class TodoWorkflow : ITodoWorkflow
{
    private readonly TodoClient _client;

    /// <summary>
    /// Initializes a new <see cref="TodoWorkflow"/> using the shared hosted-agent transport client.
    /// </summary>
    /// <param name="client">The MCP Server client whose <see cref="McpServerClient.Todo"/> surface backs this workflow.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> is <see langword="null"/>.</exception>
    public TodoWorkflow(McpServerClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client.Todo;
    }

    /// <inheritdoc />
    public Task<TodoQueryResult> QueryAsync(
        string? keyword = null,
        string? priority = null,
        string? section = null,
        string? id = null,
        bool? done = null,
        CancellationToken cancellationToken = default)
    {
        if (id is not null)
            ValidateTodoIdentifier(id, nameof(id));

        return _client.QueryAsync(keyword, priority, section, id, done, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TodoFlatItem> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateTodoIdentifier(id, nameof(id));
        return _client.GetAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TodoMutationResult> UpdateAsync(
        string id,
        TodoUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTodoIdentifier(id, nameof(id));
        ArgumentNullException.ThrowIfNull(request);
        return _client.UpdateAsync(id, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<RequirementsAnalysisResult> AnalyzeRequirementsAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ValidateTodoIdentifier(id, nameof(id));
        return _client.AnalyzeRequirementsAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<string> StreamPlanAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateTodoIdentifier(id, nameof(id));
        return _client.StreamPlanAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<string> StreamStatusAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateTodoIdentifier(id, nameof(id));
        return _client.StreamStatusAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<string> StreamImplementAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateTodoIdentifier(id, nameof(id));
        return _client.StreamImplementAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> GetPlanAsync(string id, CancellationToken cancellationToken = default) =>
        BufferAsync(StreamPlanAsync(id, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<string> GetStatusReportAsync(string id, CancellationToken cancellationToken = default) =>
        BufferAsync(StreamStatusAsync(id, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<string> GetImplementationGuideAsync(string id, CancellationToken cancellationToken = default) =>
        BufferAsync(StreamImplementAsync(id, cancellationToken), cancellationToken);

    private static async Task<string> BufferAsync(
        IAsyncEnumerable<string> lines,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();

        await foreach (var line in lines.WithCancellation(cancellationToken))
        {
            if (builder.Length > 0)
                builder.Append('\n');

            builder.Append(line);
        }

        return builder.ToString();
    }

    private static void ValidateTodoIdentifier(string id, string paramName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Todo id is required.", paramName);
        }
    }
}
