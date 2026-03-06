using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.UI.Core.Services;

internal sealed class UiCoreTodoApiClientAdapter : ITodoApiClient
{
    private readonly McpTodoService _service;
    private readonly ILogger<UiCoreTodoApiClientAdapter> _logger;

    public UiCoreTodoApiClientAdapter(
        McpTodoService service,
        ILogger<UiCoreTodoApiClientAdapter>? logger = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? NullLogger<UiCoreTodoApiClientAdapter>.Instance;
    }

    public async Task<ListTodosResult> ListTodosAsync(ListTodosQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var result = await _service.QueryAsync(
                keyword: query.Keyword,
                priority: query.Priority,
                section: query.Section,
                id: query.Id,
                done: query.Done,
                cancellationToken: cancellationToken)
            ;

        return UiCoreMessageMapper.ToListTodosResult(result);
    }

    public async Task<TodoDetail?> GetTodoAsync(string todoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _service.GetByIdAsync(todoId, cancellationToken);
            return item is null ? null : UiCoreMessageMapper.ToTodoDetail(item);
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
            var result = await _service.CreateAsync(
                    UiCoreMessageMapper.ToTodoCreateRequest(command),
                    cancellationToken)
                ;

            return UiCoreMessageMapper.ToTodoMutationOutcome(result);
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
            var result = await _service.UpdateAsync(
                    command.TodoId,
                    UiCoreMessageMapper.ToTodoUpdateRequest(command),
                    cancellationToken)
                ;

            return UiCoreMessageMapper.ToTodoMutationOutcome(result);
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
            var result = await _service.DeleteAsync(command.TodoId, cancellationToken);
            return UiCoreMessageMapper.ToTodoMutationOutcome(result);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new TodoMutationOutcome(false, ex.Message, null);
        }
    }

    public async Task<TodoRequirementsAnalysis> AnalyzeTodoRequirementsAsync(string todoId, CancellationToken cancellationToken = default)
    {
        var result = await _service.AnalyzeRequirementsAsync(todoId, cancellationToken);
        return UiCoreMessageMapper.ToTodoRequirementsAnalysis(result);
    }

    public Task<TodoPromptOutput> GenerateTodoStatusPromptAsync(string todoId, CancellationToken cancellationToken = default)
        => AggregatePromptAsync(todoId, "status", _service.StreamStatusPromptAsync(todoId, cancellationToken), cancellationToken);

    public Task<TodoPromptOutput> GenerateTodoImplementPromptAsync(string todoId, CancellationToken cancellationToken = default)
        => AggregatePromptAsync(todoId, "implement", _service.StreamImplementPromptAsync(todoId, cancellationToken), cancellationToken);

    public Task<TodoPromptOutput> GenerateTodoPlanPromptAsync(string todoId, CancellationToken cancellationToken = default)
        => AggregatePromptAsync(todoId, "plan", _service.StreamPlanPromptAsync(todoId, cancellationToken), cancellationToken);

    public IAsyncEnumerable<string> StreamTodoStatusPromptAsync(string todoId, CancellationToken cancellationToken = default)
        => _service.StreamStatusPromptAsync(todoId, cancellationToken);

    public IAsyncEnumerable<string> StreamTodoImplementPromptAsync(string todoId, CancellationToken cancellationToken = default)
        => _service.StreamImplementPromptAsync(todoId, cancellationToken);

    public IAsyncEnumerable<string> StreamTodoPlanPromptAsync(string todoId, CancellationToken cancellationToken = default)
        => _service.StreamPlanPromptAsync(todoId, cancellationToken);

    private static async Task<TodoPromptOutput> AggregatePromptAsync(
        string todoId,
        string promptType,
        IAsyncEnumerable<string> stream,
        CancellationToken cancellationToken)
    {
        var lines = new List<string>();
        await foreach (var line in stream.WithCancellation(cancellationToken))
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

