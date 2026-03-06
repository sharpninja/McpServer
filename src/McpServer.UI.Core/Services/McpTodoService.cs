using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using McpServer.UI.Core.Models;
using ClientModels = McpServer.Client.Models;

namespace McpServer.UI.Core.Services;

public sealed class McpTodoService
{
    private readonly McpServerClient _client;
    private readonly McpServerClient _promptClient;

    /// <summary>Creates a todo service using pre-authenticated, shared MCP clients.</summary>
    /// <param name="client">Standard-timeout client for CRUD operations.</param>
    /// <param name="promptClient">Long-timeout client for SSE prompt streaming.</param>
    public McpTodoService(McpServerClient client, McpServerClient promptClient)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _promptClient = promptClient ?? throw new ArgumentNullException(nameof(promptClient));
    }

    /// <summary>List / filter todos.</summary>
    public async Task<McpTodoQueryResult> QueryAsync(
        string? keyword = null,
        string? priority = null,
        string? section = null,
        string? id = null,
        bool? done = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _client.Todo.QueryAsync(keyword, priority, section, id, done, cancellationToken)
            .ConfigureAwait(true);
        return Map(result);
    }

    /// <summary>Get a single todo by id.</summary>
    public async Task<McpTodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await _client.Todo.GetAsync(id, cancellationToken).ConfigureAwait(true);
        return Map(item);
    }

    /// <summary>Create a new todo.</summary>
    public async Task<McpTodoMutationResult> CreateAsync(McpTodoCreateRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _client.Todo.CreateAsync(Map(request), cancellationToken).ConfigureAwait(true);
        return Map(result);
    }

    /// <summary>Update an existing todo (partial update).</summary>
    public async Task<McpTodoMutationResult> UpdateAsync(string id, McpTodoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _client.Todo.UpdateAsync(id, Map(request), cancellationToken).ConfigureAwait(true);
        return Map(result);
    }

    /// <summary>Delete a todo by id.</summary>
    public async Task<McpTodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _client.Todo.DeleteAsync(id, cancellationToken).ConfigureAwait(true);
        return Map(result);
    }

    /// <summary>Run AI requirements analysis on a todo.</summary>
    public async Task<McpRequirementsAnalysisResult> AnalyzeRequirementsAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _client.Todo.AnalyzeRequirementsAsync(id, cancellationToken).ConfigureAwait(true);
        return new McpRequirementsAnalysisResult
        {
            Success = result.Success,
            FunctionalRequirements = result.FunctionalRequirements?.ToList(),
            TechnicalRequirements = result.TechnicalRequirements?.ToList(),
            Error = result.Error,
            CopilotResponse = result.CopilotResponse
        };
    }

    public IAsyncEnumerable<string> StreamStatusPromptAsync(string id, CancellationToken cancellationToken = default)
        => _promptClient.Todo.StreamStatusAsync(id, cancellationToken);

    public IAsyncEnumerable<string> StreamImplementPromptAsync(string id, CancellationToken cancellationToken = default)
        => _promptClient.Todo.StreamImplementAsync(id, cancellationToken);

    public IAsyncEnumerable<string> StreamPlanPromptAsync(string id, CancellationToken cancellationToken = default)
        => _promptClient.Todo.StreamPlanAsync(id, cancellationToken);

    private static McpTodoQueryResult Map(ClientModels.TodoQueryResult result)
    {
        return new McpTodoQueryResult
        {
            TotalCount = result.TotalCount,
            Items = result.Items?.Select(Map).ToList() ?? new List<McpTodoFlatItem>()
        };
    }

    private static McpTodoMutationResult Map(ClientModels.TodoMutationResult result)
    {
        return new McpTodoMutationResult
        {
            Success = result.Success,
            Error = result.Error,
            Item = result.Item == null ? null : Map(result.Item)
        };
    }

    private static McpTodoFlatItem Map(ClientModels.TodoFlatItem item)
    {
        return new McpTodoFlatItem
        {
            Id = item.Id,
            Title = item.Title,
            Section = item.Section,
            Priority = item.Priority,
            Done = item.Done,
            Estimate = item.Estimate,
            Note = item.Note,
            Description = item.Description?.ToList(),
            TechnicalDetails = item.TechnicalDetails?.ToList(),
            ImplementationTasks = item.ImplementationTasks?.Select(Map).ToList(),
            CompletedDate = item.CompletedDate,
            DoneSummary = item.DoneSummary,
            Remaining = item.Remaining,
            PriorityNote = item.PriorityNote,
            Reference = item.Reference,
            DependsOn = item.DependsOn?.ToList(),
            FunctionalRequirements = item.FunctionalRequirements?.ToList(),
            TechnicalRequirements = item.TechnicalRequirements?.ToList()
        };
    }

    private static McpTodoFlatTask Map(ClientModels.TodoFlatTask task)
    {
        return new McpTodoFlatTask
        {
            Task = task.Task,
            Done = task.Done
        };
    }

    private static ClientModels.TodoCreateRequest Map(McpTodoCreateRequest request)
    {
        return new ClientModels.TodoCreateRequest
        {
            Id = request.Id,
            Title = request.Title,
            Section = request.Section,
            Priority = request.Priority,
            Estimate = request.Estimate,
            Note = request.Note,
            Remaining = request.Remaining,
            Description = request.Description?.ToList(),
            TechnicalDetails = request.TechnicalDetails?.ToList(),
            ImplementationTasks = request.ImplementationTasks?.Select(Map).ToList(),
            DependsOn = request.DependsOn?.ToList(),
            FunctionalRequirements = request.FunctionalRequirements?.ToList(),
            TechnicalRequirements = request.TechnicalRequirements?.ToList()
        };
    }

    private static ClientModels.TodoUpdateRequest Map(McpTodoUpdateRequest request)
    {
        return new ClientModels.TodoUpdateRequest
        {
            Title = request.Title,
            Priority = request.Priority,
            Section = request.Section,
            Done = request.Done,
            Estimate = request.Estimate,
            Description = request.Description?.ToList(),
            TechnicalDetails = request.TechnicalDetails?.ToList(),
            ImplementationTasks = request.ImplementationTasks?.Select(Map).ToList(),
            Note = request.Note,
            CompletedDate = request.CompletedDate,
            DoneSummary = request.DoneSummary,
            Remaining = request.Remaining,
            DependsOn = request.DependsOn?.ToList(),
            FunctionalRequirements = request.FunctionalRequirements?.ToList(),
            TechnicalRequirements = request.TechnicalRequirements?.ToList()
        };
    }

    private static ClientModels.TodoFlatTask Map(McpTodoFlatTask task)
    {
        return new ClientModels.TodoFlatTask
        {
            Task = task.Task ?? string.Empty,
            Done = task.Done
        };
    }
}

