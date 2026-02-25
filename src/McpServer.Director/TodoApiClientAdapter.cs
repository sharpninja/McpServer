using McpServer.Client;
using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="ITodoApiClient"/> backed by <see cref="McpServerClient"/>.
/// </summary>
internal sealed class TodoApiClientAdapter : ITodoApiClient
{
    private readonly McpServerClient? _client;

    public TodoApiClientAdapter(McpServerClient? client)
    {
        _client = client;
    }

    public async Task<ListTodosResult> ListTodosAsync(ListTodosQuery query, CancellationToken cancellationToken = default)
    {
        var response = await GetRequiredClient().Todo.QueryAsync(
            keyword: query.Keyword,
            priority: query.Priority,
            section: query.Section,
            id: query.Id,
            done: query.Done,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var items = response.Items
            .Select(item => new TodoListItem(
                Id: item.Id,
                Title: item.Title,
                Section: item.Section,
                Priority: item.Priority,
                Done: item.Done,
                Estimate: item.Estimate))
            .ToList();

        return new ListTodosResult(items, response.TotalCount);
    }

    public async Task<TodoDetail?> GetTodoAsync(string todoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await GetRequiredClient().Todo.GetAsync(todoId, cancellationToken).ConfigureAwait(false);
            return MapTodoDetail(item);
        }
        catch (McpNotFoundException)
        {
            return null;
        }
    }

    public async Task<TodoMutationOutcome> CreateTodoAsync(CreateTodoCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await GetRequiredClient().Todo.CreateAsync(new TodoCreateRequest
            {
                Id = command.Id,
                Title = command.Title,
                Section = command.Section,
                Priority = command.Priority,
                Estimate = command.Estimate,
                Description = command.Description,
                TechnicalDetails = command.TechnicalDetails,
                ImplementationTasks = command.ImplementationTasks?.Select(t => new TodoFlatTask
                {
                    Task = t.Task,
                    Done = t.Done,
                }).ToList(),
            }, cancellationToken).ConfigureAwait(false);

            return MapMutationOutcome(result);
        }
        catch (McpConflictException ex)
        {
            return new TodoMutationOutcome(false, ex.Message, null);
        }
        catch (McpValidationException ex)
        {
            return new TodoMutationOutcome(false, ex.Message, null);
        }
    }

    public async Task<TodoMutationOutcome> UpdateTodoAsync(UpdateTodoCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await GetRequiredClient().Todo.UpdateAsync(command.TodoId, new TodoUpdateRequest
            {
                Title = command.Title,
                Section = command.Section,
                Priority = command.Priority,
                Done = command.Done,
                Estimate = command.Estimate,
                Note = command.Note,
                Description = command.Description,
                TechnicalDetails = command.TechnicalDetails,
                ImplementationTasks = command.ImplementationTasks?.Select(t => new TodoFlatTask
                {
                    Task = t.Task,
                    Done = t.Done,
                }).ToList(),
            }, cancellationToken).ConfigureAwait(false);

            return MapMutationOutcome(result);
        }
        catch (McpNotFoundException ex)
        {
            return new TodoMutationOutcome(false, ex.Message, null);
        }
        catch (McpValidationException ex)
        {
            return new TodoMutationOutcome(false, ex.Message, null);
        }
    }

    public async Task<TodoMutationOutcome> DeleteTodoAsync(DeleteTodoCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await GetRequiredClient().Todo.DeleteAsync(command.TodoId, cancellationToken).ConfigureAwait(false);
            return MapMutationOutcome(result);
        }
        catch (McpNotFoundException ex)
        {
            return new TodoMutationOutcome(false, ex.Message, null);
        }
    }

    public async Task<TodoRequirementsAnalysis> AnalyzeTodoRequirementsAsync(string todoId, CancellationToken cancellationToken = default)
    {
        var result = await GetRequiredClient().Todo.AnalyzeRequirementsAsync(todoId, cancellationToken).ConfigureAwait(false);
        return new TodoRequirementsAnalysis(
            Success: result.Success,
            FunctionalRequirements: result.FunctionalRequirements?.ToList() ?? [],
            TechnicalRequirements: result.TechnicalRequirements?.ToList() ?? [],
            Error: result.Error,
            CopilotResponse: result.CopilotResponse);
    }

    public Task<TodoPromptOutput> GenerateTodoStatusPromptAsync(string todoId, CancellationToken cancellationToken = default)
        => AggregatePromptAsync(todoId, "status", GetRequiredClient().Todo.StreamStatusAsync(todoId, cancellationToken), cancellationToken);

    public Task<TodoPromptOutput> GenerateTodoImplementPromptAsync(string todoId, CancellationToken cancellationToken = default)
        => AggregatePromptAsync(todoId, "implement", GetRequiredClient().Todo.StreamImplementAsync(todoId, cancellationToken), cancellationToken);

    public Task<TodoPromptOutput> GenerateTodoPlanPromptAsync(string todoId, CancellationToken cancellationToken = default)
        => AggregatePromptAsync(todoId, "plan", GetRequiredClient().Todo.StreamPlanAsync(todoId, cancellationToken), cancellationToken);

    private McpServerClient GetRequiredClient()
    {
        if (_client is null)
        {
            throw new InvalidOperationException(
                "No MCP workspace connection is available. Ensure AGENTS-README-FIRST.yaml exists in the workspace root " +
                "or launch Director with --workspace pointing to a workspace that contains the marker file.");
        }

        return _client;
    }

    private static TodoDetail MapTodoDetail(TodoFlatItem item)
    {
        return new TodoDetail(
            Id: item.Id,
            Title: item.Title,
            Section: item.Section,
            Priority: item.Priority,
            Done: item.Done,
            Estimate: item.Estimate,
            Note: item.Note,
            Description: item.Description?.ToList() ?? [],
            TechnicalDetails: item.TechnicalDetails?.ToList() ?? [],
            ImplementationTasks: item.ImplementationTasks?.Select(t => new TodoTaskDetail(t.Task, t.Done)).ToList() ?? [],
            CompletedDate: item.CompletedDate,
            DoneSummary: item.DoneSummary,
            Remaining: item.Remaining,
            PriorityNote: item.PriorityNote,
            Reference: item.Reference,
            DependsOn: item.DependsOn?.ToList() ?? [],
            FunctionalRequirements: item.FunctionalRequirements?.ToList() ?? [],
            TechnicalRequirements: item.TechnicalRequirements?.ToList() ?? []);
    }

    private static TodoMutationOutcome MapMutationOutcome(McpServer.Client.Models.TodoMutationResult result)
        => new(
            Success: result.Success,
            Error: result.Error,
            Item: result.Item is null ? null : MapTodoDetail(result.Item));

    private static async Task<TodoPromptOutput> AggregatePromptAsync(
        string todoId,
        string promptType,
        IAsyncEnumerable<string> stream,
        CancellationToken cancellationToken)
    {
        var lines = new List<string>();
        await foreach (var line in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            lines.Add(line);
        }

        return new TodoPromptOutput(
            TodoId: todoId,
            PromptType: promptType,
            Lines: lines,
            Text: string.Join(Environment.NewLine, lines));
    }
}
