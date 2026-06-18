using System.Text.Json;
using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-QBEXEC-002 / TR-MCP-QBEXEC-002: Concrete <see cref="IQuadBrainInternalToolExecutor"/> that executes the
/// MCP-internal mutating tools QuadBrain elects (mcp_todo_create/update/delete, mcp_repo_write, mcp_repo_edit)
/// server-side by routing them through the transaction-gated services, so every mutation commits through the turn
/// transaction coordinator. Read-only and unknown internal tools return <see cref="InternalToolExecutionOutcome.Unhandled"/>
/// (never throwing), so they are surfaced to the agent as a note rather than executed here.
/// </summary>
public sealed class QuadBrainInternalToolExecutor : IQuadBrainInternalToolExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ITransactionGatedTodoMutationService _todo;
    private readonly IRepoFileService _repo;

    /// <summary>Initializes a new instance of the <see cref="QuadBrainInternalToolExecutor"/> class.</summary>
    /// <param name="todo">Transaction-gated TODO mutation service.</param>
    /// <param name="repo">Transaction-gated repository file service (the DI-registered <see cref="IRepoFileService"/>).</param>
    public QuadBrainInternalToolExecutor(ITransactionGatedTodoMutationService todo, IRepoFileService repo)
    {
        _todo = todo ?? throw new ArgumentNullException(nameof(todo));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    /// <inheritdoc />
    public async Task<InternalToolExecutionOutcome> TryExecuteAsync(
        OpenAiToolCall toolCall,
        string? turnId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        _ = turnId;
        var name = toolCall.Function.Name?.Trim() ?? string.Empty;
        var arguments = string.IsNullOrWhiteSpace(toolCall.Function.Arguments) ? "{}" : toolCall.Function.Arguments;

        try
        {
            return name switch
            {
                "mcp_todo_create" => await CreateTodoAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_todo_update" => await UpdateTodoAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_todo_delete" => await DeleteTodoAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_repo_write" => await WriteRepoAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_repo_edit" => await EditRepoAsync(arguments, cancellationToken).ConfigureAwait(false),
                _ => InternalToolExecutionOutcome.Unhandled,
            };
        }
        catch (JsonException ex)
        {
            return InternalToolExecutionOutcome.Fail($"invalid arguments for '{name}': {ex.Message}");
        }
    }

    private async Task<InternalToolExecutionOutcome> CreateTodoAsync(string arguments, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<TodoCreateRequest>(arguments, JsonOptions);
        if (request is null)
            return InternalToolExecutionOutcome.Fail("mcp_todo_create requires id, title, section, and priority.");

        var result = await _todo.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return ToOutcome(result.Success, result.Error, result);
    }

    private async Task<InternalToolExecutionOutcome> UpdateTodoAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_todo_update requires an 'id'.");

        var request = JsonSerializer.Deserialize<TodoUpdateRequest>(arguments, JsonOptions) ?? new TodoUpdateRequest();
        var result = await _todo.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ToOutcome(result.Success, result.Error, result);
    }

    private async Task<InternalToolExecutionOutcome> DeleteTodoAsync(string arguments, CancellationToken cancellationToken)
    {
        var id = ReadId(arguments);
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail("mcp_todo_delete requires an 'id'.");

        var result = await _todo.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return ToOutcome(result.Success, result.Error, result);
    }

    private async Task<InternalToolExecutionOutcome> WriteRepoAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var path = GetString(root, "path");
        if (string.IsNullOrWhiteSpace(path))
            return InternalToolExecutionOutcome.Fail("mcp_repo_write requires a 'path'.");

        var content = GetString(root, "content") ?? string.Empty;
        var result = await _repo.WriteAsync(path, content, cancellationToken).ConfigureAwait(false);
        return result.Written
            ? InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(new { path, written = true }, JsonOptions))
            : InternalToolExecutionOutcome.Fail(result.Error ?? "repo write failed");
    }

    private async Task<InternalToolExecutionOutcome> EditRepoAsync(string arguments, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var path = GetString(root, "path");
        var oldString = GetString(root, "oldString");
        var newString = GetString(root, "newString");
        if (string.IsNullOrWhiteSpace(path) || oldString is null || newString is null)
            return InternalToolExecutionOutcome.Fail("mcp_repo_edit requires 'path', 'oldString', and 'newString'.");

        var replaceAll = root.TryGetProperty("replaceAll", out var ra) && ra.ValueKind == JsonValueKind.True;
        int? expected = root.TryGetProperty("expectedOccurrences", out var eo) && eo.ValueKind == JsonValueKind.Number
            ? eo.GetInt32()
            : null;

        var result = await _repo.EditAsync(path, oldString, newString, replaceAll, expected, cancellationToken).ConfigureAwait(false);
        return result.Written
            ? InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(new { path, written = true, replacements = result.Replacements }, JsonOptions))
            : InternalToolExecutionOutcome.Fail(result.Error ?? "repo edit failed");
    }

    private static InternalToolExecutionOutcome ToOutcome(bool success, string? error, object result)
        => success
            ? InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(result, JsonOptions))
            : InternalToolExecutionOutcome.Fail(error ?? "mutation failed");

    private static string? ReadId(string arguments)
    {
        using var document = JsonDocument.Parse(arguments);
        return GetString(document.RootElement, "id");
    }

    private static string? GetString(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object
           && element.TryGetProperty(property, out var value)
           && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
