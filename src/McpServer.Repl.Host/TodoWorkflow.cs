// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - TODO workflow operations
// FR-MCP-REPL-003: Command Namespace Parity - TODO operations via REPL commands  
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - TODO command handlers
// TEST-MCP-REPL-006: TODO workflow operations match REST endpoint behavior

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - TODO workflow implementation
// FR-MCP-REPL-003: Command Namespace Parity - TODO operation implementation
// TR-MCP-REPL-002: DI-Integrated REPL Host - TODO workflow DI registration
// TR-MCP-REPL-004: Command Registry and Dispatcher - TODO workflow handler
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - TODO workflow delegation
// TEST-MCP-REPL-006: TODO REPL commands match REST endpoint semantics
// TEST-MCP-REPL-012: Streaming TODO operations emit events correctly
// TEST-MCP-REPL-019: Workflows delegate to typed client contracts without duplicating logic
// TEST-MCP-REPL-020: TODO selection state properly isolated

using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;

namespace McpServer.Repl.Host;

/// <summary>
/// Production TODO workflow implementation for iteration 3.
/// Wires real TodoClient operations, implements selection state management,
/// converts SSE streams to YAML event envelopes, and propagates cancellation tokens.
/// </summary>
public sealed class TodoWorkflow : ITodoWorkflow
{
    private readonly TodoClient _client;
    private TodoSelectionState? _currentSelection;

    /// <summary>
    /// Initializes a new instance of the <see cref="TodoWorkflow"/> class.
    /// </summary>
    /// <param name="client">The TodoClient to use for operations.</param>
    public TodoWorkflow(TodoClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public async Task<ITodoQueryResult> QueryAsync(
        string? keyword = null,
        string? priority = null,
        string? section = null,
        string? id = null,
        bool? done = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _client.QueryAsync(keyword, priority, section, id, done, cancellationToken);
        return new TodoQueryResultAdapter(result);
    }

    /// <inheritdoc />
    public async Task<ITodoItem> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("TODO ID cannot be null or empty", nameof(id));

        ValidateTodoId(id);

        try
        {
            var item = await _client.GetAsync(id, cancellationToken);
            return new TodoItemAdapter(item);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"TODO item not found: {id}", ex);
        }
    }

    /// <inheritdoc />
    public async Task SelectAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("TODO ID cannot be null or empty", nameof(id));

        ValidateTodoId(id);

        try
        {
            var item = await _client.GetAsync(id, cancellationToken);
            _currentSelection = new TodoSelectionState(
                item.Id,
                item.Title,
                item.Section,
                item.Priority,
                item.Done,
                DateTimeOffset.UtcNow
            );
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"TODO item not found: {id}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<ITodoMutationResult> CreateAsync(ITodoCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var clientRequest = new TodoCreateRequest
        {
            Id = request.Id,
            Title = request.Title,
            Section = request.Section,
            Priority = request.Priority,
            Estimate = request.Estimate,
            Description = request.Description?.ToList(),
            TechnicalDetails = request.TechnicalDetails?.ToList(),
            ImplementationTasks = request.ImplementationTasks?.Select(t => new TodoFlatTask
            {
                Task = t.Task,
                Done = t.Done
            }).ToList(),
            Note = request.Note,
            Remaining = request.Remaining,
            DependsOn = request.DependsOn?.ToList(),
            FunctionalRequirements = request.FunctionalRequirements?.ToList(),
            TechnicalRequirements = request.TechnicalRequirements?.ToList()
        };

        try
        {
            var result = await _client.CreateAsync(clientRequest, cancellationToken);
            return new TodoMutationResultAdapter(result);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException($"TODO item with ID {request.Id} already exists", ex);
        }
    }

    /// <inheritdoc />
    public async Task<ITodoMutationResult> UpdateAsync(string id, ITodoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("TODO ID cannot be null or empty", nameof(id));

        ValidateTodoId(id);
        ArgumentNullException.ThrowIfNull(request);

        var clientRequest = MapUpdateRequest(request);
        var result = await _client.UpdateAsync(id, clientRequest, cancellationToken);
        return new TodoMutationResultAdapter(result);
    }

    /// <inheritdoc />
    public async Task<ITodoMutationResult> UpdateAsync(ITodoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_currentSelection == null)
            throw new InvalidOperationException("No TODO is currently selected");

        var clientRequest = MapUpdateRequest(request);
        var result = await _client.UpdateAsync(_currentSelection.Id, clientRequest, cancellationToken);
        
        // Update selection state with new values
        if (result.Success && result.Item != null)
        {
            _currentSelection = new TodoSelectionState(
                result.Item.Id,
                result.Item.Title,
                result.Item.Section,
                result.Item.Priority,
                result.Item.Done,
                _currentSelection.SelectedAt
            );
        }

        return new TodoMutationResultAdapter(result);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("TODO ID cannot be null or empty", nameof(id));

        ValidateTodoId(id);

        var result = await _client.DeleteAsync(id, cancellationToken);
        if (!result.Success)
            throw new InvalidOperationException(result.Error ?? "Failed to delete TODO item");

        // Clear selection if we deleted the selected item
        if (_currentSelection?.Id == id)
            _currentSelection = null;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (_currentSelection == null)
            throw new InvalidOperationException("No TODO is currently selected");

        var result = await _client.DeleteAsync(_currentSelection.Id, cancellationToken);
        if (!result.Success)
            throw new InvalidOperationException(result.Error ?? "Failed to delete TODO item");

        _currentSelection = null;
    }

    /// <inheritdoc />
    public async Task<ITodoRequirementsAnalysis> AnalyzeRequirementsAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("TODO ID cannot be null or empty", nameof(id));

        ValidateTodoId(id);

        try
        {
            var result = await _client.AnalyzeRequirementsAsync(id, cancellationToken);
            return new TodoRequirementsAnalysisAdapter(result, id);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"TODO item not found: {id}", ex);
        }
    }

    /// <inheritdoc />
    public async Task StreamStatusAsync(string id, Func<IStreamingEvent, Task> eventCallback, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("TODO ID cannot be null or empty", nameof(id));

        ValidateTodoId(id);
        ArgumentNullException.ThrowIfNull(eventCallback);

        int sequence = 0;

        try
        {
            await foreach (var line in _client.StreamStatusAsync(id, cancellationToken))
            {
                var evt = new StreamingEvent(
                    "status.progress",
                    new { message = line, todoId = id },
                    DateTimeOffset.UtcNow,
                    ++sequence
                );

                await eventCallback(evt);
            }

            var completeEvent = new StreamingEvent(
                "status.complete",
                new { todoId = id },
                DateTimeOffset.UtcNow,
                ++sequence
            );

            await eventCallback(completeEvent);
        }
        catch (OperationCanceledException)
        {
            var cancelledEvent = new StreamingEvent(
                "status.cancelled",
                new { message = "Stream cancelled by user request", todoId = id },
                DateTimeOffset.UtcNow,
                ++sequence
            );

            await eventCallback(cancelledEvent);
            throw;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var errorEvent = new StreamingEvent(
                "status.error",
                new { message = $"TODO item not found: {id}", todoId = id },
                DateTimeOffset.UtcNow,
                ++sequence
            );

            await eventCallback(errorEvent);
            throw new InvalidOperationException($"TODO item not found: {id}", ex);
        }
        catch (Exception ex)
        {
            var errorEvent = new StreamingEvent(
                "status.error",
                new { message = ex.Message, todoId = id },
                DateTimeOffset.UtcNow,
                ++sequence
            );

            await eventCallback(errorEvent);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StreamPlanAsync(string id, Func<IStreamingEvent, Task> eventCallback, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("TODO ID cannot be null or empty", nameof(id));

        ValidateTodoId(id);
        ArgumentNullException.ThrowIfNull(eventCallback);

        int sequence = 0;

        try
        {
            await foreach (var line in _client.StreamPlanAsync(id, cancellationToken))
            {
                var evt = new StreamingEvent(
                    "plan.progress",
                    new { message = line, todoId = id },
                    DateTimeOffset.UtcNow,
                    ++sequence
                );

                await eventCallback(evt);
            }

            var completeEvent = new StreamingEvent(
                "plan.complete",
                new { todoId = id },
                DateTimeOffset.UtcNow,
                ++sequence
            );

            await eventCallback(completeEvent);
        }
        catch (OperationCanceledException)
        {
            var cancelledEvent = new StreamingEvent(
                "plan.cancelled",
                new { message = "Stream cancelled by user request", todoId = id },
                DateTimeOffset.UtcNow,
                ++sequence
            );

            await eventCallback(cancelledEvent);
            throw;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var errorEvent = new StreamingEvent(
                "plan.error",
                new { message = $"TODO item not found: {id}", todoId = id },
                DateTimeOffset.UtcNow,
                ++sequence
            );

            await eventCallback(errorEvent);
            throw new InvalidOperationException($"TODO item not found: {id}", ex);
        }
        catch (Exception ex)
        {
            var errorEvent = new StreamingEvent(
                "plan.error",
                new { message = ex.Message, todoId = id },
                DateTimeOffset.UtcNow,
                ++sequence
            );

            await eventCallback(errorEvent);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StreamImplementAsync(string id, Func<IStreamingEvent, Task> eventCallback, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("TODO ID cannot be null or empty", nameof(id));

        ValidateTodoId(id);
        ArgumentNullException.ThrowIfNull(eventCallback);

        int sequence = 0;

        try
        {
            await foreach (var line in _client.StreamImplementAsync(id, cancellationToken))
            {
                var evt = new StreamingEvent(
                    "implement.progress",
                    new { message = line, todoId = id },
                    DateTimeOffset.UtcNow,
                    ++sequence
                );

                await eventCallback(evt);
            }

            var completeEvent = new StreamingEvent(
                "implement.complete",
                new { todoId = id },
                DateTimeOffset.UtcNow,
                ++sequence
            );

            await eventCallback(completeEvent);
        }
        catch (OperationCanceledException)
        {
            var cancelledEvent = new StreamingEvent(
                "implement.cancelled",
                new { message = "Stream cancelled by user request", todoId = id },
                DateTimeOffset.UtcNow,
                ++sequence
            );

            await eventCallback(cancelledEvent);
            throw;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var errorEvent = new StreamingEvent(
                "implement.error",
                new { message = $"TODO item not found: {id}", todoId = id },
                DateTimeOffset.UtcNow,
                ++sequence
            );

            await eventCallback(errorEvent);
            throw new InvalidOperationException($"TODO item not found: {id}", ex);
        }
        catch (Exception ex)
        {
            var errorEvent = new StreamingEvent(
                "implement.error",
                new { message = ex.Message, todoId = id },
                DateTimeOffset.UtcNow,
                ++sequence
            );

            await eventCallback(errorEvent);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ITodoProjectionStatus> GetProjectionStatusAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("TODO ID cannot be null or empty", nameof(id));

        ValidateTodoId(id);

        try
        {
            var status = await _client.GetProjectionStatusAsync(cancellationToken);
            
            // Map the workspace-level projection status to a TODO-specific status
            return new TodoProjectionStatusAdapter(id, status);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"TODO item not found: {id}", ex);
        }
    }

    /// <inheritdoc />
    public async Task RepairProjectionAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("TODO ID cannot be null or empty", nameof(id));

        ValidateTodoId(id);

        try
        {
            var result = await _client.RepairProjectionAsync(cancellationToken);
            if (!result.Success)
                throw new InvalidOperationException(result.Error ?? "Failed to repair projection");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"TODO item not found: {id}", ex);
        }
    }

    /// <inheritdoc />
    public ITodoSelectionState? CurrentSelection()
    {
        return _currentSelection;
    }

    private static void ValidateTodoId(string id)
    {
        // Canonical format: <PHASE>-<AREA>-### or ISSUE-{number}
        // Pattern: ^[A-Z]+-[A-Z0-9]+-\d{3}$ or ^ISSUE-\d+$
        var validPattern1 = System.Text.RegularExpressions.Regex.IsMatch(id, @"^[A-Z]+-[A-Z0-9]+-\d{3}$");
        var validPattern2 = System.Text.RegularExpressions.Regex.IsMatch(id, @"^ISSUE-\d+$");
        var validPattern3 = id == "ISSUE-NEW";

        if (!validPattern1 && !validPattern2 && !validPattern3)
        {
            throw new ArgumentException($"Invalid TODO ID format: {id}", nameof(id));
        }
    }

    private static TodoUpdateRequest MapUpdateRequest(ITodoUpdateRequest request)
    {
        return new TodoUpdateRequest
        {
            Title = request.Title,
            Priority = request.Priority,
            Section = request.Section,
            Done = request.Done,
            Estimate = request.Estimate,
            Description = request.Description?.ToList(),
            TechnicalDetails = request.TechnicalDetails?.ToList(),
            ImplementationTasks = request.ImplementationTasks?.Select(t => new TodoFlatTask
            {
                Task = t.Task,
                Done = t.Done
            }).ToList(),
            Note = request.Note,
            CompletedDate = request.CompletedDate,
            DoneSummary = request.DoneSummary,
            Remaining = request.Remaining,
            DependsOn = request.DependsOn?.ToList(),
            FunctionalRequirements = request.FunctionalRequirements?.ToList(),
            TechnicalRequirements = request.TechnicalRequirements?.ToList()
        };
    }
}

/// <summary>
/// Immutable TODO selection state implementation.
/// </summary>
internal sealed class TodoSelectionState : ITodoSelectionState
{
    public TodoSelectionState(string id, string title, string section, string priority, bool done, DateTimeOffset selectedAt)
    {
        Id = id;
        Title = title;
        Section = section;
        Priority = priority;
        Done = done;
        SelectedAt = selectedAt;
    }

    public string Id { get; }
    public string Title { get; }
    public string Section { get; }
    public string Priority { get; }
    public bool Done { get; }
    public DateTimeOffset SelectedAt { get; }
}

/// <summary>
/// Adapter for TodoQueryResult to ITodoQueryResult.
/// </summary>
internal sealed class TodoQueryResultAdapter : ITodoQueryResult
{
    private readonly TodoQueryResult _result;

    public TodoQueryResultAdapter(TodoQueryResult result)
    {
        _result = result;
    }

    public IReadOnlyList<ITodoItem> Items => _result.Items.Select(i => (ITodoItem)new TodoItemAdapter(i)).ToList();
    public int TotalCount => _result.TotalCount;
}

/// <summary>
/// Adapter for TodoFlatItem to ITodoItem.
/// </summary>
internal sealed class TodoItemAdapter : ITodoItem
{
    private readonly TodoFlatItem _item;

    public TodoItemAdapter(TodoFlatItem item)
    {
        _item = item;
    }

    public string Id => _item.Id;
    public string Title => _item.Title;
    public string Section => _item.Section;
    public string Priority => _item.Priority;
    public bool Done => _item.Done;
    public string? Estimate => _item.Estimate;
    public string? Note => _item.Note;
    public IReadOnlyList<string> Description => _item.Description ?? Array.Empty<string>();
    public IReadOnlyList<string> TechnicalDetails => _item.TechnicalDetails ?? Array.Empty<string>();
    public IReadOnlyList<ITodoSubtask> ImplementationTasks => 
        _item.ImplementationTasks?.Select(t => (ITodoSubtask)new TodoSubtaskAdapter(t)).ToArray() ?? Array.Empty<ITodoSubtask>();
    public string? CompletedDate => _item.CompletedDate;
    public string? DoneSummary => _item.DoneSummary;
    public string? Remaining => _item.Remaining;
    public string? PriorityNote => _item.PriorityNote;
    public string? Reference => _item.Reference;
    public IReadOnlyList<string> DependsOn => _item.DependsOn ?? Array.Empty<string>();
    public IReadOnlyList<string> FunctionalRequirements => _item.FunctionalRequirements ?? Array.Empty<string>();
    public IReadOnlyList<string> TechnicalRequirements => _item.TechnicalRequirements ?? Array.Empty<string>();
}

/// <summary>
/// Adapter for TodoFlatTask to ITodoSubtask.
/// </summary>
internal sealed class TodoSubtaskAdapter : ITodoSubtask
{
    private readonly TodoFlatTask _task;

    public TodoSubtaskAdapter(TodoFlatTask task)
    {
        _task = task;
    }

    public string Task => _task.Task;
    public bool Done => _task.Done;
}

/// <summary>
/// Adapter for TodoMutationResult to ITodoMutationResult.
/// </summary>
internal sealed class TodoMutationResultAdapter : ITodoMutationResult
{
    private readonly TodoMutationResult _result;

    public TodoMutationResultAdapter(TodoMutationResult result)
    {
        _result = result;
    }

    public bool Success => _result.Success;
    public ITodoItem Item => new TodoItemAdapter(_result.Item ?? throw new InvalidOperationException("Mutation result has no item"));
}

/// <summary>
/// Adapter for RequirementsAnalysisResult to ITodoRequirementsAnalysis.
/// </summary>
internal sealed class TodoRequirementsAnalysisAdapter : ITodoRequirementsAnalysis
{
    private readonly RequirementsAnalysisResult _result;
    private readonly string _todoId;

    public TodoRequirementsAnalysisAdapter(RequirementsAnalysisResult result, string todoId)
    {
        _result = result;
        _todoId = todoId;
    }

    public string TodoId => _todoId;
    public IReadOnlyList<IRequirementReference> FunctionalRequirements =>
        _result.FunctionalRequirements?.Select(id => (IRequirementReference)new RequirementReferenceAdapter(id)).ToArray() 
        ?? Array.Empty<IRequirementReference>();
    public IReadOnlyList<IRequirementReference> TechnicalRequirements =>
        _result.TechnicalRequirements?.Select(id => (IRequirementReference)new RequirementReferenceAdapter(id)).ToArray()
        ?? Array.Empty<IRequirementReference>();
    public bool AllRequirementsExist => _result.Success;
}

/// <summary>
/// Adapter for requirement reference.
/// </summary>
internal sealed class RequirementReferenceAdapter : IRequirementReference
{
    public RequirementReferenceAdapter(string id)
    {
        Id = id;
        Title = $"Requirement {id}";
        Exists = true;
    }

    public string Id { get; }
    public string? Title { get; }
    public bool Exists { get; }
}

/// <summary>
/// Adapter for projection status.
/// </summary>
internal sealed class TodoProjectionStatusAdapter : ITodoProjectionStatus
{
    private readonly string _todoId;
    private readonly TodoProjectionStatusResult _status;

    public TodoProjectionStatusAdapter(string todoId, TodoProjectionStatusResult status)
    {
        _todoId = todoId;
        _status = status;
    }

    public string TodoId => _todoId;
    public bool HasStatus => _status.ProjectionConsistent;
    public bool HasPlan => _status.ProjectionConsistent;
    public bool HasImplementation => false;
    public DateTimeOffset? LastUpdated => DateTimeOffset.TryParse(_status.LastProjectedToYamlUtc, out var dt) ? dt : null;
    public bool IsStale => _status.RepairRequired;
}

/// <summary>
/// Streaming event implementation.
/// </summary>
internal sealed class StreamingEvent : IStreamingEvent
{
    public StreamingEvent(string eventType, object? data, DateTimeOffset timestamp, int sequence)
    {
        EventType = eventType;
        Data = data;
        Timestamp = timestamp;
        Sequence = sequence;
    }

    public string EventType { get; }
    public object? Data { get; }
    public DateTimeOffset Timestamp { get; }
    public int Sequence { get; }
}
