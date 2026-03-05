using McpServer.Client;
using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Web.Adapters;

internal sealed class TodoApiClientAdapter : ITodoApiClient
{
    private readonly WebMcpContext _context;
    private readonly ILogger<TodoApiClientAdapter> _logger;

    public TodoApiClientAdapter(WebMcpContext context, ILogger<TodoApiClientAdapter>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<TodoApiClientAdapter>.Instance;
    }

    public async Task<ListTodosResult> ListTodosAsync(ListTodosQuery query, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client.Todo.QueryAsync(
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
            var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
            var item = await client.Todo.GetAsync(todoId, cancellationToken).ConfigureAwait(false);
            return MapTodoDetail(item);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<TodoMutationOutcome> CreateTodoAsync(CreateTodoCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
            var result = await client.Todo.CreateAsync(new TodoCreateRequest
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
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new TodoMutationOutcome(false, ex.Message, null);
        }
        catch (McpValidationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new TodoMutationOutcome(false, ex.Message, null);
        }
    }

    public async Task<TodoMutationOutcome> UpdateTodoAsync(UpdateTodoCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
            var result = await client.Todo.UpdateAsync(command.TodoId, new TodoUpdateRequest
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
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new TodoMutationOutcome(false, ex.Message, null);
        }
        catch (McpValidationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new TodoMutationOutcome(false, ex.Message, null);
        }
    }

    public async Task<TodoMutationOutcome> DeleteTodoAsync(DeleteTodoCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
            var result = await client.Todo.DeleteAsync(command.TodoId, cancellationToken).ConfigureAwait(false);
            return MapMutationOutcome(result);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new TodoMutationOutcome(false, ex.Message, null);
        }
    }

    public async Task<TodoRequirementsAnalysis> AnalyzeTodoRequirementsAsync(string todoId, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Todo.AnalyzeRequirementsAsync(todoId, cancellationToken).ConfigureAwait(false);
        return new TodoRequirementsAnalysis(
            Success: result.Success,
            FunctionalRequirements: result.FunctionalRequirements?.ToList() ?? [],
            TechnicalRequirements: result.TechnicalRequirements?.ToList() ?? [],
            Error: result.Error,
            CopilotResponse: result.CopilotResponse);
    }

    public Task<TodoPromptOutput> GenerateTodoStatusPromptAsync(string todoId, CancellationToken cancellationToken = default)
        => AggregatePromptAsync(todoId, "status", GetPromptStreamAsync(c => c.Todo.StreamStatusAsync(todoId, cancellationToken), cancellationToken), cancellationToken);

    public Task<TodoPromptOutput> GenerateTodoImplementPromptAsync(string todoId, CancellationToken cancellationToken = default)
        => AggregatePromptAsync(todoId, "implement", GetPromptStreamAsync(c => c.Todo.StreamImplementAsync(todoId, cancellationToken), cancellationToken), cancellationToken);

    public Task<TodoPromptOutput> GenerateTodoPlanPromptAsync(string todoId, CancellationToken cancellationToken = default)
        => AggregatePromptAsync(todoId, "plan", GetPromptStreamAsync(c => c.Todo.StreamPlanAsync(todoId, cancellationToken), cancellationToken), cancellationToken);

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

    private async IAsyncEnumerable<string> GetPromptStreamAsync(
        Func<McpServerClient, IAsyncEnumerable<string>> getStream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        await foreach (var line in getStream(client).WithCancellation(cancellationToken).ConfigureAwait(false))
            yield return line;
    }

    private static async Task<TodoPromptOutput> AggregatePromptAsync(
        string todoId,
        string promptType,
        IAsyncEnumerable<string> stream,
        CancellationToken cancellationToken)
    {
        var lines = new List<string>();
        await foreach (var line in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
            lines.Add(line);

        return new TodoPromptOutput(
            TodoId: todoId,
            PromptType: promptType,
            Lines: lines,
            Text: string.Join(Environment.NewLine, lines));
    }
}
