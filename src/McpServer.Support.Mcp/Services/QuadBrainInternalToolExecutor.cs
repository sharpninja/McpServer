using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-QBEXEC-002 / TR-MCP-QBEXEC-002: Concrete <see cref="IQuadBrainInternalToolExecutor"/> that executes the
/// MCP-internal mutating tools QuadBrain elects (mcp_todo_create/update/delete, mcp_repo_write, mcp_repo_edit, and
/// the mcp_requirements_create_/update_ FR/TR/TEST mutations) server-side by routing them through the
/// transaction-gated services, so every mutation commits through the turn transaction coordinator. Read-only and
/// unknown internal tools return <see cref="InternalToolExecutionOutcome.Unhandled"/> (never throwing), so they are
/// surfaced to the agent as a note rather than executed here.
/// </summary>
public sealed class QuadBrainInternalToolExecutor : IQuadBrainInternalToolExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ITransactionGatedTodoMutationService _todo;
    private readonly IRepoFileService _repo;
    private readonly IRequirementsDocumentService _requirements;

    /// <summary>Initializes a new instance of the <see cref="QuadBrainInternalToolExecutor"/> class.</summary>
    /// <param name="todo">Transaction-gated TODO mutation service.</param>
    /// <param name="repo">Transaction-gated repository file service (the DI-registered <see cref="IRepoFileService"/>).</param>
    /// <param name="requirements">Transaction-gated requirements document service (the DI-registered <see cref="IRequirementsDocumentService"/>).</param>
    public QuadBrainInternalToolExecutor(
        ITransactionGatedTodoMutationService todo,
        IRepoFileService repo,
        IRequirementsDocumentService requirements)
    {
        _todo = todo ?? throw new ArgumentNullException(nameof(todo));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _requirements = requirements ?? throw new ArgumentNullException(nameof(requirements));
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
                "mcp_requirements_create_fr" => await CreateFrAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_update_fr" => await UpdateFrAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_create_tr" => await CreateTrAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_update_tr" => await UpdateTrAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_create_test" => await CreateTestAsync(arguments, cancellationToken).ConfigureAwait(false),
                "mcp_requirements_update_test" => await UpdateTestAsync(arguments, cancellationToken).ConfigureAwait(false),
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

    // FR-MCP-QBEXEC-001 (AC-4) / FR-MCP-QBEXEC-002: requirements mutations routed through the transaction-gated
    // IRequirementsDocumentService so the AoT commit applies FR/TR/TEST add/update server-side. Read-only
    // requirements tools (mcp_requirements_list_*, mcp_requirements_get_*) are NOT handled here and fall through
    // to Unhandled so they are surfaced to the agent.

    private Task<InternalToolExecutionOutcome> CreateFrAsync(string arguments, CancellationToken cancellationToken)
        => MutateRequirementAsync(arguments, "create_fr", (entry, ct) => _requirements.AddFrAsync(ReadFrEntry(entry), ct), cancellationToken);

    private Task<InternalToolExecutionOutcome> UpdateFrAsync(string arguments, CancellationToken cancellationToken)
        => MutateRequirementAsync(arguments, "update_fr", (entry, ct) => _requirements.UpdateFrAsync(ReadFrEntry(entry), ct), cancellationToken);

    private Task<InternalToolExecutionOutcome> CreateTrAsync(string arguments, CancellationToken cancellationToken)
        => MutateRequirementAsync(arguments, "create_tr", (entry, ct) => _requirements.AddTrAsync(ReadTrEntry(entry), ct), cancellationToken);

    private Task<InternalToolExecutionOutcome> UpdateTrAsync(string arguments, CancellationToken cancellationToken)
        => MutateRequirementAsync(arguments, "update_tr", (entry, ct) => _requirements.UpdateTrAsync(ReadTrEntry(entry), ct), cancellationToken);

    private Task<InternalToolExecutionOutcome> CreateTestAsync(string arguments, CancellationToken cancellationToken)
        => MutateRequirementAsync(arguments, "create_test", (entry, ct) => _requirements.AddTestAsync(ReadTestEntry(entry), ct), cancellationToken);

    private Task<InternalToolExecutionOutcome> UpdateTestAsync(string arguments, CancellationToken cancellationToken)
        => MutateRequirementAsync(arguments, "update_test", (entry, ct) => _requirements.UpdateTestAsync(ReadTestEntry(entry), ct), cancellationToken);

    private static async Task<InternalToolExecutionOutcome> MutateRequirementAsync(
        string arguments,
        string operation,
        Func<JsonElement, CancellationToken, Task> mutation,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(arguments);
        var root = document.RootElement;
        var id = GetString(root, "id");
        if (string.IsNullOrWhiteSpace(id))
            return InternalToolExecutionOutcome.Fail($"mcp_requirements_{operation} requires an 'id'.");

        try
        {
            await mutation(root, cancellationToken).ConfigureAwait(false);
            return InternalToolExecutionOutcome.Ok(JsonSerializer.Serialize(new { id, operation, applied = true }, JsonOptions));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return InternalToolExecutionOutcome.Fail($"mcp_requirements_{operation} failed: {ex.Message}");
        }
    }

    private static FrEntry ReadFrEntry(JsonElement root)
        => new(
            Id: GetString(root, "id") ?? string.Empty,
            Title: GetString(root, "title") ?? string.Empty,
            Body: GetString(root, "body") ?? string.Empty,
            Priority: GetString(root, "priority") ?? "medium",
            Status: GetString(root, "status") ?? "pending",
            Notes: GetString(root, "notes"));

    private static TrEntry ReadTrEntry(JsonElement root)
        => new(
            Id: GetString(root, "id") ?? string.Empty,
            Title: GetString(root, "title") ?? string.Empty,
            Body: GetString(root, "body") ?? string.Empty,
            Priority: GetString(root, "priority") ?? "medium",
            Status: GetString(root, "status") ?? "pending",
            Notes: GetString(root, "notes"));

    private static TestEntry ReadTestEntry(JsonElement root)
        => new(
            Id: GetString(root, "id") ?? string.Empty,
            Condition: GetString(root, "condition") ?? string.Empty,
            Title: GetString(root, "title") ?? string.Empty,
            Priority: GetString(root, "priority") ?? "medium",
            Status: GetString(root, "status") ?? "pending",
            Notes: GetString(root, "notes"));

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
