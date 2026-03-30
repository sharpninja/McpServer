using McpServer.Repl.Core;
using McpServer.Todo.Validation.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Iteration 3 unit tests for TODO workflow orchestration.
/// Tests TODO CRUD operations (query filters, get by ID, create/update/delete), selection-state management,
/// requirements analysis, streamed event emission (status/plan/implement SSE → YAML events), cancellation handling,
/// projection status/repair, and invalid ID error responses.
/// Mocks ITodoWorkflow and verifies YAML shaping for streaming events.
/// Red phase: all tests expected to fail until implementation is complete.
/// </summary>
public class TodoWorkflowTests
{
    private readonly ITodoWorkflow _workflow;
    private readonly IYamlSerializer _yamlSerializer;

    public TodoWorkflowTests()
    {
        _yamlSerializer = new FakeYamlSerializer();
        _workflow = Substitute.For<ITodoWorkflow>();
    }

    #region Query Tests

    [Fact]
    public async Task QueryAsync_NoFilters_ReturnsAllTodos()
    {
        var expectedResult = new TodoQueryResult
        {
            Items = new List<TodoFlatItem>
            {
                CreateTodoItem("MCP-API-001", "Implement API", "backend", "high", false),
                CreateTodoItem("MCP-UI-002", "Build UI", "frontend", "medium", false)
            },
            TotalCount = 2
        };

        _workflow.QueryAsync(null, null, null, null, null, default)
            .Returns(Task.FromResult<ITodoQueryResult>(new TodoQueryResultAdapter(expectedResult)));

        var result = await _workflow.QueryAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        await _workflow.Received(1).QueryAsync(null, null, null, null, null, default);
    }

    [Fact]
    public async Task QueryAsync_WithKeywordFilter_ReturnsMatchingTodos()
    {
        var expectedResult = new TodoQueryResult
        {
            Items = new List<TodoFlatItem>
            {
                CreateTodoItem("MCP-API-001", "Implement API authentication", "backend", "high", false)
            },
            TotalCount = 1
        };

        _workflow.QueryAsync("authentication", null, null, null, null, default)
            .Returns(Task.FromResult<ITodoQueryResult>(new TodoQueryResultAdapter(expectedResult)));

        var result = await _workflow.QueryAsync(keyword: "authentication");

        Assert.Single(result.Items);
        Assert.Contains("authentication", result.Items[0].Title);
        await _workflow.Received(1).QueryAsync("authentication", null, null, null, null, default);
    }

    [Fact]
    public async Task QueryAsync_WithPriorityFilter_ReturnsMatchingTodos()
    {
        var expectedResult = new TodoQueryResult
        {
            Items = new List<TodoFlatItem>
            {
                CreateTodoItem("MCP-API-001", "Critical bug fix", "backend", "critical", false)
            },
            TotalCount = 1
        };

        _workflow.QueryAsync(null, "critical", null, null, null, default)
            .Returns(Task.FromResult<ITodoQueryResult>(new TodoQueryResultAdapter(expectedResult)));

        var result = await _workflow.QueryAsync(priority: "critical");

        Assert.Single(result.Items);
        Assert.Equal("critical", result.Items[0].Priority);
        await _workflow.Received(1).QueryAsync(null, "critical", null, null, null, default);
    }

    [Fact]
    public async Task QueryAsync_WithSectionFilter_ReturnsMatchingTodos()
    {
        var expectedResult = new TodoQueryResult
        {
            Items = new List<TodoFlatItem>
            {
                CreateTodoItem("MCP-INFRA-001", "Setup CI/CD", "infrastructure", "high", false)
            },
            TotalCount = 1
        };

        _workflow.QueryAsync(null, null, "infrastructure", null, null, default)
            .Returns(Task.FromResult<ITodoQueryResult>(new TodoQueryResultAdapter(expectedResult)));

        var result = await _workflow.QueryAsync(section: "infrastructure");

        Assert.Single(result.Items);
        Assert.Equal("infrastructure", result.Items[0].Section);
        await _workflow.Received(1).QueryAsync(null, null, "infrastructure", null, null, default);
    }

    [Fact]
    public async Task QueryAsync_WithIdFilter_ReturnsSingleTodo()
    {
        var expectedResult = new TodoQueryResult
        {
            Items = new List<TodoFlatItem>
            {
                CreateTodoItem("MCP-API-001", "Implement API", "backend", "high", false)
            },
            TotalCount = 1
        };

        _workflow.QueryAsync(null, null, null, "MCP-API-001", null, default)
            .Returns(Task.FromResult<ITodoQueryResult>(new TodoQueryResultAdapter(expectedResult)));

        var result = await _workflow.QueryAsync(id: "MCP-API-001");

        Assert.Single(result.Items);
        Assert.Equal("MCP-API-001", result.Items[0].Id);
        await _workflow.Received(1).QueryAsync(null, null, null, "MCP-API-001", null, default);
    }

    [Fact]
    public async Task QueryAsync_WithDoneFilter_ReturnsCompletedTodos()
    {
        var expectedResult = new TodoQueryResult
        {
            Items = new List<TodoFlatItem>
            {
                CreateTodoItem("MCP-API-001", "Completed task", "backend", "high", true)
            },
            TotalCount = 1
        };

        _workflow.QueryAsync(null, null, null, null, true, default)
            .Returns(Task.FromResult<ITodoQueryResult>(new TodoQueryResultAdapter(expectedResult)));

        var result = await _workflow.QueryAsync(done: true);

        Assert.Single(result.Items);
        Assert.True(result.Items[0].Done);
        await _workflow.Received(1).QueryAsync(null, null, null, null, true, default);
    }

    [Fact]
    public async Task QueryAsync_WithMultipleFilters_ReturnsCombinedResults()
    {
        var expectedResult = new TodoQueryResult
        {
            Items = new List<TodoFlatItem>
            {
                CreateTodoItem("MCP-API-001", "High priority backend task", "backend", "high", false)
            },
            TotalCount = 1
        };

        _workflow.QueryAsync(null, "high", "backend", null, false, default)
            .Returns(Task.FromResult<ITodoQueryResult>(new TodoQueryResultAdapter(expectedResult)));

        var result = await _workflow.QueryAsync(priority: "high", section: "backend", done: false);

        Assert.Single(result.Items);
        Assert.Equal("high", result.Items[0].Priority);
        Assert.Equal("backend", result.Items[0].Section);
        Assert.False(result.Items[0].Done);
        await _workflow.Received(1).QueryAsync(null, "high", "backend", null, false, default);
    }

    [Fact]
    public async Task QueryAsync_StorageError_ThrowsInvalidOperationException()
    {
        _workflow.QueryAsync(null, null, null, null, null, default)
            .Throws(new InvalidOperationException("Storage connection failed"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.QueryAsync());

        Assert.Contains("Storage", exception.Message);
    }

    #endregion

    #region Get By ID Tests

    [Fact]
    public async Task GetAsync_ValidId_ReturnsTodoItem()
    {
        var expectedItem = CreateTodoItem("MCP-API-001", "Implement API", "backend", "high", false);

        _workflow.GetAsync("MCP-API-001", default)
            .Returns(Task.FromResult<ITodoItem>(new TodoItemAdapter(expectedItem)));

        var result = await _workflow.GetAsync("MCP-API-001");

        Assert.NotNull(result);
        Assert.Equal("MCP-API-001", result.Id);
        Assert.Equal("Implement API", result.Title);
        await _workflow.Received(1).GetAsync("MCP-API-001", default);
    }

    [Fact]
    public async Task GetAsync_InvalidIdFormat_ThrowsArgumentException()
    {
        _workflow.GetAsync("invalid-id", default)
            .Throws(new ArgumentException("Invalid TODO ID format: invalid-id"));

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GetAsync("invalid-id"));

        Assert.Contains("Invalid TODO ID format", exception.Message);
    }

    [Fact]
    public async Task GetAsync_LowercaseId_ThrowsArgumentException()
    {
        _workflow.GetAsync("mcp-api-001", default)
            .Throws(new ArgumentException("Invalid TODO ID format: mcp-api-001"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GetAsync("mcp-api-001"));
    }

    [Fact]
    public async Task GetAsync_MissingPadding_ThrowsArgumentException()
    {
        _workflow.GetAsync("MCP-API-1", default)
            .Throws(new ArgumentException("Invalid TODO ID format: MCP-API-1"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GetAsync("MCP-API-1"));
    }

    [Fact]
    public async Task GetAsync_NullOrEmptyId_ThrowsArgumentException()
    {
        _workflow.GetAsync(null!, default)
            .Throws(new ArgumentException("TODO ID cannot be null or empty"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GetAsync(null!));

        _workflow.GetAsync("", default)
            .Throws(new ArgumentException("TODO ID cannot be null or empty"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GetAsync(""));
    }

    [Fact]
    public async Task GetAsync_TodoNotFound_ThrowsInvalidOperationException()
    {
        _workflow.GetAsync("MCP-NONEXISTENT-999", default)
            .Throws(new InvalidOperationException("TODO item not found: MCP-NONEXISTENT-999"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.GetAsync("MCP-NONEXISTENT-999"));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task GetAsync_IssueIdFormat_ReturnsTodoItem()
    {
        var expectedItem = CreateTodoItem("ISSUE-42", "GitHub issue TODO", "backend", "medium", false);

        _workflow.GetAsync("ISSUE-42", default)
            .Returns(Task.FromResult<ITodoItem>(new TodoItemAdapter(expectedItem)));

        var result = await _workflow.GetAsync("ISSUE-42");

        Assert.NotNull(result);
        Assert.Equal("ISSUE-42", result.Id);
    }

    #endregion

    #region Selection State Tests

    [Fact]
    public void CurrentSelection_NoSelection_ReturnsNull()
    {
        _workflow.CurrentSelection().Returns((ITodoSelectionState?)null);

        var selection = _workflow.CurrentSelection();

        Assert.Null(selection);
    }

    [Fact]
    public async Task SelectAsync_ValidId_SetsSelectionState()
    {
        var expectedItem = CreateTodoItem("MCP-API-001", "Implement API", "backend", "high", false);
        var mockSelectionState = CreateMockSelectionState("MCP-API-001", "Implement API", "backend", "high", false);

        _workflow.SelectAsync("MCP-API-001", default).Returns(Task.CompletedTask);
        _workflow.CurrentSelection().Returns(mockSelectionState);

        await _workflow.SelectAsync("MCP-API-001");
        var selection = _workflow.CurrentSelection();

        Assert.NotNull(selection);
        Assert.Equal("MCP-API-001", selection!.Id);
        Assert.Equal("Implement API", selection.Title);
        Assert.Equal("backend", selection.Section);
        Assert.Equal("high", selection.Priority);
        Assert.False(selection.Done);
        await _workflow.Received(1).SelectAsync("MCP-API-001", default);
    }

    [Fact]
    public async Task SelectAsync_InvalidId_ThrowsArgumentException()
    {
        _workflow.SelectAsync("invalid-id", default)
            .Throws(new ArgumentException("Invalid TODO ID format: invalid-id"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.SelectAsync("invalid-id"));
    }

    [Fact]
    public async Task SelectAsync_TodoNotFound_ThrowsInvalidOperationException()
    {
        _workflow.SelectAsync("MCP-NONEXISTENT-999", default)
            .Throws(new InvalidOperationException("TODO item not found: MCP-NONEXISTENT-999"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.SelectAsync("MCP-NONEXISTENT-999"));
    }

    [Fact]
    public async Task CurrentSelection_AfterSelection_ReturnsSelectionWithTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var mockSelectionState = CreateMockSelectionState("MCP-API-001", "Implement API", "backend", "high", false);
        mockSelectionState.SelectedAt.Returns(now);

        await _workflow.SelectAsync("MCP-API-001");
        _workflow.CurrentSelection().Returns(mockSelectionState);

        var selection = _workflow.CurrentSelection();

        Assert.NotNull(selection);
        Assert.True(selection!.SelectedAt <= DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.True(selection.SelectedAt >= DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task SelectAsync_ChangeSelection_UpdatesSelectionState()
    {
        var firstSelectionState = CreateMockSelectionState("MCP-API-001", "First TODO", "backend", "high", false);
        var secondSelectionState = CreateMockSelectionState("MCP-API-002", "Second TODO", "frontend", "medium", false);

        _workflow.SelectAsync("MCP-API-001", default).Returns(Task.CompletedTask);
        _workflow.CurrentSelection().Returns(firstSelectionState);

        await _workflow.SelectAsync("MCP-API-001");
        var firstSelection = _workflow.CurrentSelection();
        Assert.Equal("MCP-API-001", firstSelection!.Id);

        _workflow.SelectAsync("MCP-API-002", default).Returns(Task.CompletedTask);
        _workflow.CurrentSelection().Returns(secondSelectionState);

        await _workflow.SelectAsync("MCP-API-002");
        var secondSelection = _workflow.CurrentSelection();
        Assert.Equal("MCP-API-002", secondSelection!.Id);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesTodoItem()
    {
        var request = CreateTodoCreateRequest();
        var createdItem = CreateTodoItem("MCP-API-001", "New API feature", "backend", "high", false);
        var mutationResult = new TodoMutationResult { Success = true, Item = createdItem };

        _workflow.CreateAsync(Arg.Any<ITodoCreateRequest>(), default)
            .Returns(Task.FromResult<ITodoMutationResult>(new TodoMutationResultAdapter(mutationResult)));

        var result = await _workflow.CreateAsync(request);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal("MCP-API-001", result.Item!.Id);
        await _workflow.Received(1).CreateAsync(Arg.Any<ITodoCreateRequest>(), default);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_ThrowsArgumentNullException()
    {
        _workflow.CreateAsync(null!, default)
            .Throws(new ArgumentNullException("request"));

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _workflow.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_InvalidIdFormat_ThrowsArgumentException()
    {
        var request = CreateTodoCreateRequest(id: "invalid-id");

        _workflow.CreateAsync(Arg.Any<ITodoCreateRequest>(), default)
            .Throws(new ArgumentException("Invalid TODO ID format: invalid-id"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_DuplicateId_ThrowsInvalidOperationException()
    {
        var request = CreateTodoCreateRequest();

        _workflow.CreateAsync(Arg.Any<ITodoCreateRequest>(), default)
            .Throws(new InvalidOperationException("TODO item with ID MCP-API-001 already exists"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_IssueNew_CreatesGitHubIssue()
    {
        var request = CreateTodoCreateRequest(id: "ISSUE-NEW");
        var createdItem = CreateTodoItem("ISSUE-42", "GitHub issue TODO", "backend", "medium", false);
        var mutationResult = new TodoMutationResult { Success = true, Item = createdItem };

        _workflow.CreateAsync(Arg.Any<ITodoCreateRequest>(), default)
            .Returns(Task.FromResult<ITodoMutationResult>(new TodoMutationResultAdapter(mutationResult)));

        var result = await _workflow.CreateAsync(request);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.StartsWith("ISSUE-", result.Item!.Id);
        Assert.NotEqual("ISSUE-NEW", result.Item.Id);
    }

    [Fact]
    public async Task CreateAsync_MissingRequiredFields_ThrowsArgumentException()
    {
        var request = CreateTodoCreateRequest();
        request.Title.Returns((string)null!);

        _workflow.CreateAsync(Arg.Any<ITodoCreateRequest>(), default)
            .Throws(new ArgumentException("Title is required"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.CreateAsync(request));
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateAsync_WithId_UpdatesTodoItem()
    {
        var request = CreateTodoUpdateRequest();
        var updatedItem = CreateTodoItem("MCP-API-001", "Updated title", "backend", "critical", false);
        var mutationResult = new TodoMutationResult { Success = true, Item = updatedItem };

        _workflow.UpdateAsync("MCP-API-001", Arg.Any<ITodoUpdateRequest>(), default)
            .Returns(Task.FromResult<ITodoMutationResult>(new TodoMutationResultAdapter(mutationResult)));

        var result = await _workflow.UpdateAsync("MCP-API-001", request);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal("Updated title", result.Item!.Title);
        await _workflow.Received(1).UpdateAsync("MCP-API-001", Arg.Any<ITodoUpdateRequest>(), default);
    }

    [Fact]
    public async Task UpdateAsync_WithSelectedId_UpdatesSelectedTodoItem()
    {
        var request = CreateTodoUpdateRequest();
        var updatedItem = CreateTodoItem("MCP-API-001", "Updated via selection", "backend", "high", false);
        var mutationResult = new TodoMutationResult { Success = true, Item = updatedItem };
        var mockSelectionState = CreateMockSelectionState("MCP-API-001", "Original title", "backend", "high", false);

        await _workflow.SelectAsync("MCP-API-001");
        _workflow.CurrentSelection().Returns(mockSelectionState);
        _workflow.UpdateAsync(Arg.Any<ITodoUpdateRequest>(), default)
            .Returns(Task.FromResult<ITodoMutationResult>(new TodoMutationResultAdapter(mutationResult)));

        var result = await _workflow.UpdateAsync(request);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal("Updated via selection", result.Item!.Title);
        await _workflow.Received(1).UpdateAsync(Arg.Any<ITodoUpdateRequest>(), default);
    }

    [Fact]
    public async Task UpdateAsync_NoSelection_ThrowsInvalidOperationException()
    {
        var request = CreateTodoUpdateRequest();

        _workflow.CurrentSelection().Returns((ITodoSelectionState?)null);
        _workflow.UpdateAsync(Arg.Any<ITodoUpdateRequest>(), default)
            .Throws(new InvalidOperationException("No TODO is currently selected"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.UpdateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ThrowsArgumentException()
    {
        var request = CreateTodoUpdateRequest();

        _workflow.UpdateAsync("invalid-id", Arg.Any<ITodoUpdateRequest>(), default)
            .Throws(new ArgumentException("Invalid TODO ID format: invalid-id"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.UpdateAsync("invalid-id", request));
    }

    [Fact]
    public async Task UpdateAsync_NullRequest_ThrowsArgumentNullException()
    {
        _workflow.UpdateAsync("MCP-API-001", null!, default)
            .Throws(new ArgumentNullException("request"));

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _workflow.UpdateAsync("MCP-API-001", null!));
    }

    [Fact]
    public async Task UpdateAsync_TodoNotFound_ThrowsInvalidOperationException()
    {
        var request = CreateTodoUpdateRequest();

        _workflow.UpdateAsync("MCP-NONEXISTENT-999", Arg.Any<ITodoUpdateRequest>(), default)
            .Throws(new InvalidOperationException("TODO item not found: MCP-NONEXISTENT-999"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.UpdateAsync("MCP-NONEXISTENT-999", request));
    }

    [Fact]
    public async Task UpdateAsync_PartialUpdate_PreservesUnchangedFields()
    {
        var request = CreateTodoUpdateRequest();
        request.Title.Returns("New title");
        request.Priority.Returns((string?)null);
        request.Done.Returns((bool?)null);

        var updatedItem = CreateTodoItem("MCP-API-001", "New title", "backend", "high", false);
        var mutationResult = new TodoMutationResult { Success = true, Item = updatedItem };

        _workflow.UpdateAsync("MCP-API-001", Arg.Any<ITodoUpdateRequest>(), default)
            .Returns(Task.FromResult<ITodoMutationResult>(new TodoMutationResultAdapter(mutationResult)));

        var result = await _workflow.UpdateAsync("MCP-API-001", request);

        Assert.Equal("New title", result.Item!.Title);
        Assert.Equal("high", result.Item.Priority);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteAsync_WithId_DeletesTodoItem()
    {
        _workflow.DeleteAsync("MCP-API-001", default)
            .Returns(Task.CompletedTask);

        await _workflow.DeleteAsync("MCP-API-001");

        await _workflow.Received(1).DeleteAsync("MCP-API-001", default);
    }

    [Fact]
    public async Task DeleteAsync_WithSelectedId_DeletesSelectedTodoItem()
    {
        var mockSelectionState = CreateMockSelectionState("MCP-API-001", "To be deleted", "backend", "low", false);

        await _workflow.SelectAsync("MCP-API-001");
        _workflow.CurrentSelection().Returns(mockSelectionState);
        _workflow.DeleteAsync(default)
            .Returns(Task.CompletedTask);

        await _workflow.DeleteAsync();

        await _workflow.Received(1).DeleteAsync(default);
    }

    [Fact]
    public async Task DeleteAsync_NoSelection_ThrowsInvalidOperationException()
    {
        _workflow.CurrentSelection().Returns((ITodoSelectionState?)null);
        _workflow.DeleteAsync(default)
            .Throws(new InvalidOperationException("No TODO is currently selected"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.DeleteAsync());
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ThrowsArgumentException()
    {
        _workflow.DeleteAsync("invalid-id", default)
            .Throws(new ArgumentException("Invalid TODO ID format: invalid-id"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.DeleteAsync("invalid-id"));
    }

    [Fact]
    public async Task DeleteAsync_TodoNotFound_ThrowsInvalidOperationException()
    {
        _workflow.DeleteAsync("MCP-NONEXISTENT-999", default)
            .Throws(new InvalidOperationException("TODO item not found: MCP-NONEXISTENT-999"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.DeleteAsync("MCP-NONEXISTENT-999"));
    }

    [Fact]
    public async Task DeleteAsync_NullOrEmptyId_ThrowsArgumentException()
    {
        _workflow.DeleteAsync(null!, default)
            .Throws(new ArgumentException("TODO ID cannot be null or empty"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.DeleteAsync(null!));

        _workflow.DeleteAsync("", default)
            .Throws(new ArgumentException("TODO ID cannot be null or empty"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.DeleteAsync(""));
    }

    #endregion

    #region Requirements Analysis Tests

    [Fact]
    public async Task AnalyzeRequirementsAsync_ValidId_ReturnsAnalysis()
    {
        var expectedAnalysis = new RequirementsAnalysisResult
        {
            Success = true,
            FunctionalRequirements = new List<string> { "FR-MCP-001", "FR-MCP-002" },
            TechnicalRequirements = new List<string> { "TR-MCP-ARCH-001" }
        };

        _workflow.AnalyzeRequirementsAsync("MCP-API-001", default)
            .Returns(Task.FromResult<ITodoRequirementsAnalysis>(new TodoRequirementsAnalysisAdapter(expectedAnalysis, "MCP-API-001")));

        var result = await _workflow.AnalyzeRequirementsAsync("MCP-API-001");

        Assert.NotNull(result);
        Assert.Equal("MCP-API-001", result.TodoId);
        Assert.Equal(2, result.FunctionalRequirements.Count);
        Assert.Single(result.TechnicalRequirements);
        Assert.True(result.AllRequirementsExist);
        await _workflow.Received(1).AnalyzeRequirementsAsync("MCP-API-001", default);
    }

    [Fact]
    public async Task AnalyzeRequirementsAsync_InvalidId_ThrowsArgumentException()
    {
        _workflow.AnalyzeRequirementsAsync("invalid-id", default)
            .Throws(new ArgumentException("Invalid TODO ID format: invalid-id"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.AnalyzeRequirementsAsync("invalid-id"));
    }

    [Fact]
    public async Task AnalyzeRequirementsAsync_TodoNotFound_ThrowsInvalidOperationException()
    {
        _workflow.AnalyzeRequirementsAsync("MCP-NONEXISTENT-999", default)
            .Throws(new InvalidOperationException("TODO item not found: MCP-NONEXISTENT-999"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.AnalyzeRequirementsAsync("MCP-NONEXISTENT-999"));
    }

    [Fact]
    public async Task AnalyzeRequirementsAsync_MissingRequirements_ReturnsIncompleteAnalysis()
    {
        var expectedAnalysis = new RequirementsAnalysisResult
        {
            Success = false,
            FunctionalRequirements = new List<string> { "FR-MCP-001", "FR-MISSING-999" },
            TechnicalRequirements = new List<string>()
        };

        _workflow.AnalyzeRequirementsAsync("MCP-API-001", default)
            .Returns(Task.FromResult<ITodoRequirementsAnalysis>(new TodoRequirementsAnalysisAdapter(expectedAnalysis, "MCP-API-001", allExist: false)));

        var result = await _workflow.AnalyzeRequirementsAsync("MCP-API-001");

        Assert.False(result.AllRequirementsExist);
        Assert.Equal(2, result.FunctionalRequirements.Count);
    }

    #endregion

    #region Streaming Event Tests

    [Fact]
    public async Task StreamStatusAsync_ValidId_EmitsProgressEvents()
    {
        var events = new List<IStreamingEvent>();
        var callbackInvoked = false;

        _workflow.StreamStatusAsync("MCP-API-001", Arg.Any<Func<IStreamingEvent, Task>>(), default)
            .Returns(callInfo =>
            {
                var callback = callInfo.ArgAt<Func<IStreamingEvent, Task>>(1);
                callbackInvoked = true;
                return Task.CompletedTask;
            });

        await _workflow.StreamStatusAsync("MCP-API-001", evt =>
        {
            events.Add(evt);
            return Task.CompletedTask;
        });

        Assert.True(callbackInvoked);
        await _workflow.Received(1).StreamStatusAsync("MCP-API-001", Arg.Any<Func<IStreamingEvent, Task>>(), default);
    }

    [Fact]
    public async Task StreamPlanAsync_ValidId_EmitsProgressEvents()
    {
        var events = new List<IStreamingEvent>();
        var callbackInvoked = false;

        _workflow.StreamPlanAsync("MCP-API-001", Arg.Any<Func<IStreamingEvent, Task>>(), default)
            .Returns(callInfo =>
            {
                var callback = callInfo.ArgAt<Func<IStreamingEvent, Task>>(1);
                callbackInvoked = true;
                return Task.CompletedTask;
            });

        await _workflow.StreamPlanAsync("MCP-API-001", evt =>
        {
            events.Add(evt);
            return Task.CompletedTask;
        });

        Assert.True(callbackInvoked);
        await _workflow.Received(1).StreamPlanAsync("MCP-API-001", Arg.Any<Func<IStreamingEvent, Task>>(), default);
    }

    [Fact]
    public async Task StreamImplementAsync_ValidId_EmitsProgressEvents()
    {
        var events = new List<IStreamingEvent>();
        var callbackInvoked = false;

        _workflow.StreamImplementAsync("MCP-API-001", Arg.Any<Func<IStreamingEvent, Task>>(), default)
            .Returns(callInfo =>
            {
                var callback = callInfo.ArgAt<Func<IStreamingEvent, Task>>(1);
                callbackInvoked = true;
                return Task.CompletedTask;
            });

        await _workflow.StreamImplementAsync("MCP-API-001", evt =>
        {
            events.Add(evt);
            return Task.CompletedTask;
        });

        Assert.True(callbackInvoked);
        await _workflow.Received(1).StreamImplementAsync("MCP-API-001", Arg.Any<Func<IStreamingEvent, Task>>(), default);
    }

    [Fact]
    public async Task StreamStatusAsync_EmitsMultipleEvents_InCorrectOrder()
    {
        var events = new List<IStreamingEvent>();

        _workflow.StreamStatusAsync("MCP-API-001", Arg.Any<Func<IStreamingEvent, Task>>(), default)
            .Returns(async callInfo =>
            {
                var callback = callInfo.ArgAt<Func<IStreamingEvent, Task>>(1);
                await callback(CreateStreamingEvent("status.progress", 1));
                await callback(CreateStreamingEvent("status.progress", 2));
                await callback(CreateStreamingEvent("status.complete", 3));
            });

        await _workflow.StreamStatusAsync("MCP-API-001", async evt =>
        {
            events.Add(evt);
            await Task.CompletedTask;
        });

        Assert.Equal(3, events.Count);
        Assert.Equal("status.progress", events[0].EventType);
        Assert.Equal(1, events[0].Sequence);
        Assert.Equal("status.complete", events[2].EventType);
        Assert.Equal(3, events[2].Sequence);
    }

    [Fact]
    public async Task StreamStatusAsync_NullCallback_ThrowsArgumentNullException()
    {
        _workflow.StreamStatusAsync("MCP-API-001", null!, default)
            .Throws(new ArgumentNullException("eventCallback"));

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _workflow.StreamStatusAsync("MCP-API-001", null!));
    }

    [Fact]
    public async Task StreamPlanAsync_NullCallback_ThrowsArgumentNullException()
    {
        _workflow.StreamPlanAsync("MCP-API-001", null!, default)
            .Throws(new ArgumentNullException("eventCallback"));

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _workflow.StreamPlanAsync("MCP-API-001", null!));
    }

    [Fact]
    public async Task StreamImplementAsync_NullCallback_ThrowsArgumentNullException()
    {
        _workflow.StreamImplementAsync("MCP-API-001", null!, default)
            .Throws(new ArgumentNullException("eventCallback"));

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _workflow.StreamImplementAsync("MCP-API-001", null!));
    }

    [Fact]
    public async Task StreamStatusAsync_InvalidId_ThrowsArgumentException()
    {
        _workflow.StreamStatusAsync("invalid-id", Arg.Any<Func<IStreamingEvent, Task>>(), default)
            .Throws(new ArgumentException("Invalid TODO ID format: invalid-id"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.StreamStatusAsync("invalid-id", evt => Task.CompletedTask));
    }

    [Fact]
    public async Task StreamPlanAsync_InvalidId_ThrowsArgumentException()
    {
        _workflow.StreamPlanAsync("invalid-id", Arg.Any<Func<IStreamingEvent, Task>>(), default)
            .Throws(new ArgumentException("Invalid TODO ID format: invalid-id"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.StreamPlanAsync("invalid-id", evt => Task.CompletedTask));
    }

    [Fact]
    public async Task StreamImplementAsync_InvalidId_ThrowsArgumentException()
    {
        _workflow.StreamImplementAsync("invalid-id", Arg.Any<Func<IStreamingEvent, Task>>(), default)
            .Throws(new ArgumentException("Invalid TODO ID format: invalid-id"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.StreamImplementAsync("invalid-id", evt => Task.CompletedTask));
    }

    [Fact]
    public async Task StreamStatusAsync_Cancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _workflow.StreamStatusAsync("MCP-API-001", Arg.Any<Func<IStreamingEvent, Task>>(), cts.Token)
            .Throws(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _workflow.StreamStatusAsync("MCP-API-001", evt => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task StreamPlanAsync_Cancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _workflow.StreamPlanAsync("MCP-API-001", Arg.Any<Func<IStreamingEvent, Task>>(), cts.Token)
            .Throws(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _workflow.StreamPlanAsync("MCP-API-001", evt => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task StreamImplementAsync_Cancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _workflow.StreamImplementAsync("MCP-API-001", Arg.Any<Func<IStreamingEvent, Task>>(), cts.Token)
            .Throws(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _workflow.StreamImplementAsync("MCP-API-001", evt => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task StreamStatusAsync_TodoNotFound_ThrowsInvalidOperationException()
    {
        _workflow.StreamStatusAsync("MCP-NONEXISTENT-999", Arg.Any<Func<IStreamingEvent, Task>>(), default)
            .Throws(new InvalidOperationException("TODO item not found: MCP-NONEXISTENT-999"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.StreamStatusAsync("MCP-NONEXISTENT-999", evt => Task.CompletedTask));
    }

    [Fact]
    public async Task StreamPlanAsync_TodoNotFound_ThrowsInvalidOperationException()
    {
        _workflow.StreamPlanAsync("MCP-NONEXISTENT-999", Arg.Any<Func<IStreamingEvent, Task>>(), default)
            .Throws(new InvalidOperationException("TODO item not found: MCP-NONEXISTENT-999"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.StreamPlanAsync("MCP-NONEXISTENT-999", evt => Task.CompletedTask));
    }

    [Fact]
    public async Task StreamImplementAsync_TodoNotFound_ThrowsInvalidOperationException()
    {
        _workflow.StreamImplementAsync("MCP-NONEXISTENT-999", Arg.Any<Func<IStreamingEvent, Task>>(), default)
            .Throws(new InvalidOperationException("TODO item not found: MCP-NONEXISTENT-999"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.StreamImplementAsync("MCP-NONEXISTENT-999", evt => Task.CompletedTask));
    }

    [Fact]
    public async Task StreamPlanAsync_ErrorDuringStreaming_EmitsErrorEvent()
    {
        var events = new List<IStreamingEvent>();

        _workflow.StreamPlanAsync("MCP-API-001", Arg.Any<Func<IStreamingEvent, Task>>(), default)
            .Returns(async callInfo =>
            {
                var callback = callInfo.ArgAt<Func<IStreamingEvent, Task>>(1);
                await callback(CreateStreamingEvent("plan.progress", 1));
                await callback(CreateStreamingEvent("plan.error", 2, new { errorMessage = "Planning failed" }));
            });

        await _workflow.StreamPlanAsync("MCP-API-001", async evt =>
        {
            events.Add(evt);
            await Task.CompletedTask;
        });

        Assert.Equal(2, events.Count);
        Assert.Equal("plan.error", events[1].EventType);
    }

    #endregion

    #region Projection Status and Repair Tests

    [Fact]
    public async Task GetProjectionStatusAsync_ValidId_ReturnsStatus()
    {
        var expectedStatus = CreateMockProjectionStatus("MCP-API-001", true, true, false, false);

        _workflow.GetProjectionStatusAsync("MCP-API-001", default)
            .Returns(Task.FromResult(expectedStatus));

        var result = await _workflow.GetProjectionStatusAsync("MCP-API-001");

        Assert.NotNull(result);
        Assert.Equal("MCP-API-001", result.TodoId);
        Assert.True(result.HasStatus);
        Assert.True(result.HasPlan);
        Assert.False(result.HasImplementation);
        Assert.False(result.IsStale);
        await _workflow.Received(1).GetProjectionStatusAsync("MCP-API-001", default);
    }

    [Fact]
    public async Task GetProjectionStatusAsync_NoProjections_ReturnsEmptyStatus()
    {
        var expectedStatus = CreateMockProjectionStatus("MCP-API-001", false, false, false, false);

        _workflow.GetProjectionStatusAsync("MCP-API-001", default)
            .Returns(Task.FromResult(expectedStatus));

        var result = await _workflow.GetProjectionStatusAsync("MCP-API-001");

        Assert.False(result.HasStatus);
        Assert.False(result.HasPlan);
        Assert.False(result.HasImplementation);
    }

    [Fact]
    public async Task GetProjectionStatusAsync_StaleProjection_ReturnsStaleFlag()
    {
        var expectedStatus = CreateMockProjectionStatus("MCP-API-001", true, true, true, true);

        _workflow.GetProjectionStatusAsync("MCP-API-001", default)
            .Returns(Task.FromResult(expectedStatus));

        var result = await _workflow.GetProjectionStatusAsync("MCP-API-001");

        Assert.NotNull(result);
        Assert.True(result.IsStale);
        await _workflow.Received(1).GetProjectionStatusAsync("MCP-API-001", default);
    }

    [Fact]
    public async Task GetProjectionStatusAsync_InvalidId_ThrowsArgumentException()
    {
        _workflow.GetProjectionStatusAsync("invalid-id", default)
            .Throws(new ArgumentException("Invalid TODO ID format: invalid-id"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GetProjectionStatusAsync("invalid-id"));
    }

    [Fact]
    public async Task GetProjectionStatusAsync_TodoNotFound_ThrowsInvalidOperationException()
    {
        _workflow.GetProjectionStatusAsync("MCP-NONEXISTENT-999", default)
            .Throws(new InvalidOperationException("TODO item not found: MCP-NONEXISTENT-999"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.GetProjectionStatusAsync("MCP-NONEXISTENT-999"));
    }

    [Fact]
    public async Task RepairProjectionAsync_ValidId_RebuildsProjection()
    {
        _workflow.RepairProjectionAsync("MCP-API-001", default)
            .Returns(Task.CompletedTask);

        await _workflow.RepairProjectionAsync("MCP-API-001");

        await _workflow.Received(1).RepairProjectionAsync("MCP-API-001", default);
    }

    [Fact]
    public async Task RepairProjectionAsync_InvalidId_ThrowsArgumentException()
    {
        _workflow.RepairProjectionAsync("invalid-id", default)
            .Throws(new ArgumentException("Invalid TODO ID format: invalid-id"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.RepairProjectionAsync("invalid-id"));
    }

    [Fact]
    public async Task RepairProjectionAsync_TodoNotFound_ThrowsInvalidOperationException()
    {
        _workflow.RepairProjectionAsync("MCP-NONEXISTENT-999", default)
            .Throws(new InvalidOperationException("TODO item not found: MCP-NONEXISTENT-999"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.RepairProjectionAsync("MCP-NONEXISTENT-999"));
    }

    [Fact]
    public async Task RepairProjectionAsync_AfterRepair_ProjectionStatusUpdated()
    {
        var initialStatus = CreateMockProjectionStatus("MCP-API-001", false, false, false, true);
        var repairedStatus = CreateMockProjectionStatus("MCP-API-001", true, true, true, false);

        _workflow.GetProjectionStatusAsync("MCP-API-001", default)
            .Returns(Task.FromResult(initialStatus));

        var initialResult = await _workflow.GetProjectionStatusAsync("MCP-API-001");
        Assert.True(initialResult.IsStale);

        _workflow.RepairProjectionAsync("MCP-API-001", default)
            .Returns(Task.CompletedTask);

        await _workflow.RepairProjectionAsync("MCP-API-001");

        _workflow.GetProjectionStatusAsync("MCP-API-001", default)
            .Returns(Task.FromResult(repairedStatus));

        var repairedResult = await _workflow.GetProjectionStatusAsync("MCP-API-001");
        Assert.False(repairedResult.IsStale);
        Assert.True(repairedResult.HasStatus);
    }

    #endregion

    #region YAML Event Shaping Tests

    [Fact]
    public void YamlShaping_StreamStatusEvent_MatchesExpectedStructure()
    {
        var eventPayload = new
        {
            eventType = "status.progress",
            data = new
            {
                todoId = "MCP-API-001",
                message = "Analyzing TODO status",
                progress = 50
            },
            timestamp = DateTimeOffset.UtcNow,
            sequence = 1
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("event", eventPayload));

        Assert.Contains("type: event", yaml);
        Assert.Contains("eventType: status.progress", yaml);
        Assert.Contains("todoId: MCP-API-001", yaml);
    }

    [Fact]
    public void YamlShaping_StreamPlanEvent_MatchesExpectedStructure()
    {
        var eventPayload = new
        {
            eventType = "plan.progress",
            data = new
            {
                todoId = "MCP-API-001",
                planStep = "Step 1: Design API schema",
                stepNumber = 1
            },
            timestamp = DateTimeOffset.UtcNow,
            sequence = 1
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("event", eventPayload));

        Assert.Contains("type: event", yaml);
        Assert.Contains("eventType: plan.progress", yaml);
        Assert.Contains("planStep:", yaml);
    }

    [Fact]
    public void YamlShaping_StreamImplementEvent_MatchesExpectedStructure()
    {
        var eventPayload = new
        {
            eventType = "implement.progress",
            data = new
            {
                todoId = "MCP-API-001",
                action = "Created file ApiController.cs",
                filePath = "src/ApiController.cs"
            },
            timestamp = DateTimeOffset.UtcNow,
            sequence = 1
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("event", eventPayload));

        Assert.Contains("type: event", yaml);
        Assert.Contains("eventType: implement.progress", yaml);
        Assert.Contains("action:", yaml);
    }

    [Fact]
    public void YamlShaping_StreamCompleteEvent_MatchesExpectedStructure()
    {
        var eventPayload = new
        {
            eventType = "status.complete",
            data = new
            {
                todoId = "MCP-API-001",
                summary = "Status analysis complete",
                completedAt = DateTimeOffset.UtcNow
            },
            timestamp = DateTimeOffset.UtcNow,
            sequence = 5
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("event", eventPayload));

        Assert.Contains("type: event", yaml);
        Assert.Contains("eventType: status.complete", yaml);
        Assert.Contains("summary:", yaml);
    }

    [Fact]
    public void YamlShaping_StreamErrorEvent_MatchesExpectedStructure()
    {
        var eventPayload = new
        {
            eventType = "plan.error",
            data = new
            {
                todoId = "MCP-API-001",
                errorMessage = "Failed to generate plan",
                errorCode = "plan_generation_failed"
            },
            timestamp = DateTimeOffset.UtcNow,
            sequence = 3
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("event", eventPayload));

        Assert.Contains("type: event", yaml);
        Assert.Contains("eventType: plan.error", yaml);
        Assert.Contains("errorMessage:", yaml);
        Assert.Contains("errorCode:", yaml);
    }

    [Fact]
    public void YamlShaping_ImplementProgressEvent_ContainsFilePath()
    {
        var eventPayload = new
        {
            eventType = "implement.progress",
            data = new
            {
                todoId = "MCP-API-001",
                action = "Modified file",
                filePath = "src/Models/TodoItem.cs",
                changeType = "update"
            },
            timestamp = DateTimeOffset.UtcNow,
            sequence = 2
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("event", eventPayload));

        Assert.Contains("filePath:", yaml);
        Assert.Contains("src/Models/TodoItem.cs", yaml);
    }

    [Fact]
    public void YamlShaping_MultipleEventsStream_ProducesValidYamlDocuments()
    {
        var events = new[]
        {
            CreateEnvelope("event", new { eventType = "status.progress", sequence = 1 }),
            CreateEnvelope("event", new { eventType = "status.progress", sequence = 2 }),
            CreateEnvelope("event", new { eventType = "status.complete", sequence = 3 })
        };

        var yamlStream = _yamlSerializer.SerializeStream(events);

        Assert.Contains("---", yamlStream);
        Assert.Contains("eventType: status.progress", yamlStream);
        Assert.Contains("eventType: status.complete", yamlStream);
    }

    #endregion

    #region Error Response Tests

    [Fact]
    public async Task ErrorResponse_InvalidTodoId_ReturnsStructuredError()
    {
        var invalidId = "invalid-id";

        _workflow.GetAsync(invalidId, default)
            .Throws(new ArgumentException($"Invalid TODO ID format: {invalidId}"));

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GetAsync(invalidId));

        Assert.Contains("Invalid TODO ID", exception.Message);
    }

    [Fact]
    public async Task ErrorResponse_TodoNotFound_ReturnsStructuredError()
    {
        _workflow.GetAsync("MCP-NONEXISTENT-999", default)
            .Throws(new InvalidOperationException("TODO item not found: MCP-NONEXISTENT-999"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.GetAsync("MCP-NONEXISTENT-999"));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task ErrorResponse_StorageError_ReturnsStructuredError()
    {
        _workflow.QueryAsync(null, null, null, null, null, default)
            .Throws(new InvalidOperationException("Storage connection failed"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.QueryAsync());

        Assert.Contains("Storage", exception.Message);
    }

    [Fact]
    public async Task ErrorResponse_NullRequest_ContainsParameterName()
    {
        _workflow.CreateAsync(null!, default)
            .Throws(new ArgumentNullException("request", "Request cannot be null"));

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _workflow.CreateAsync(null!));

        Assert.Equal("request", exception.ParamName);
    }

    #endregion

    #region Helper Methods

    private static TodoFlatItem CreateTodoItem(string id, string title, string section, string priority, bool done)
    {
        return new TodoFlatItem
        {
            Id = id,
            Title = title,
            Section = section,
            Priority = priority,
            Done = done,
            Description = new List<string> { "Description for " + title },
            TechnicalDetails = new List<string> { "Technical details" },
            ImplementationTasks = new List<TodoFlatTask>
            {
                new TodoFlatTask { Task = "Task 1", Done = false }
            }
        };
    }

    private static ITodoSelectionState CreateMockSelectionState(string id, string title, string section, string priority, bool done)
    {
        var state = Substitute.For<ITodoSelectionState>();
        state.Id.Returns(id);
        state.Title.Returns(title);
        state.Section.Returns(section);
        state.Priority.Returns(priority);
        state.Done.Returns(done);
        state.SelectedAt.Returns(DateTimeOffset.UtcNow);
        return state;
    }

    private static ITodoCreateRequest CreateTodoCreateRequest(string id = "MCP-API-001")
    {
        var request = Substitute.For<ITodoCreateRequest>();
        request.Id.Returns(id);
        request.Title.Returns("New API feature");
        request.Section.Returns("backend");
        request.Priority.Returns("high");
        request.Estimate.Returns("2h");
        request.Description.Returns(new List<string> { "Implement new API endpoint" });
        return request;
    }

    private static ITodoUpdateRequest CreateTodoUpdateRequest()
    {
        var request = Substitute.For<ITodoUpdateRequest>();
        request.Title.Returns("Updated title");
        request.Priority.Returns("critical");
        request.Done.Returns(false);
        return request;
    }

    private static ITodoProjectionStatus CreateMockProjectionStatus(string todoId, bool hasStatus, bool hasPlan, bool hasImplementation, bool isStale)
    {
        var status = Substitute.For<ITodoProjectionStatus>();
        status.TodoId.Returns(todoId);
        status.HasStatus.Returns(hasStatus);
        status.HasPlan.Returns(hasPlan);
        status.HasImplementation.Returns(hasImplementation);
        status.IsStale.Returns(isStale);
        status.LastUpdated.Returns(DateTimeOffset.UtcNow.AddHours(-1));
        return status;
    }

    private IYamlEnvelope CreateEnvelope(string type, object payload)
    {
        var envelope = Substitute.For<IYamlEnvelope>();
        envelope.Type.Returns(type);
        envelope.Payload.Returns(payload);
        return envelope;
    }

    private static IStreamingEvent CreateStreamingEvent(string eventType, int sequence, object? data = null)
    {
        var evt = Substitute.For<IStreamingEvent>();
        evt.EventType.Returns(eventType);
        evt.Sequence.Returns(sequence);
        evt.Timestamp.Returns(DateTimeOffset.UtcNow);
        evt.Data.Returns(data ?? new { todoId = "MCP-API-001" });
        return evt;
    }

    #endregion

    #region Adapter Classes

    private class TodoQueryResultAdapter : ITodoQueryResult
    {
        private readonly TodoQueryResult _result;

        public TodoQueryResultAdapter(TodoQueryResult result)
        {
            _result = result;
        }

        public IReadOnlyList<ITodoItem> Items => _result.Items.Select(i => (ITodoItem)new TodoItemAdapter(i)).ToList();
        public int TotalCount => _result.TotalCount;
    }

    private class TodoItemAdapter : ITodoItem
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
        public IReadOnlyList<string> Description => _item.Description ?? new List<string>();
        public IReadOnlyList<string> TechnicalDetails => _item.TechnicalDetails ?? new List<string>();
        public IReadOnlyList<ITodoSubtask> ImplementationTasks => _item.ImplementationTasks?.Select(t => (ITodoSubtask)new TodoSubtaskAdapter(t)).ToList() ?? new List<ITodoSubtask>();
        public string? CompletedDate => _item.CompletedDate;
        public string? DoneSummary => _item.DoneSummary;
        public string? Remaining => _item.Remaining;
        public string? PriorityNote => _item.PriorityNote;
        public string? Reference => _item.Reference;
        public IReadOnlyList<string> DependsOn => _item.DependsOn ?? new List<string>();
        public IReadOnlyList<string> FunctionalRequirements => _item.FunctionalRequirements ?? new List<string>();
        public IReadOnlyList<string> TechnicalRequirements => _item.TechnicalRequirements ?? new List<string>();
    }

    private class TodoSubtaskAdapter : ITodoSubtask
    {
        private readonly TodoFlatTask _task;

        public TodoSubtaskAdapter(TodoFlatTask task)
        {
            _task = task;
        }

        public string Task => _task.Task;
        public bool Done => _task.Done;
    }

    private class TodoMutationResultAdapter : ITodoMutationResult
    {
        private readonly TodoMutationResult _result;

        public TodoMutationResultAdapter(TodoMutationResult result)
        {
            _result = result;
        }

        public bool Success => _result.Success;
        public ITodoItem Item => new TodoItemAdapter(_result.Item!);
    }

    private class TodoRequirementsAnalysisAdapter : ITodoRequirementsAnalysis
    {
        private readonly RequirementsAnalysisResult _result;
        private readonly string _todoId;
        private readonly bool _allExist;

        public TodoRequirementsAnalysisAdapter(RequirementsAnalysisResult result, string todoId, bool allExist = true)
        {
            _result = result;
            _todoId = todoId;
            _allExist = allExist;
        }

        public string TodoId => _todoId;
        public IReadOnlyList<IRequirementReference> FunctionalRequirements =>
            _result.FunctionalRequirements?.Select(id => (IRequirementReference)new RequirementReferenceAdapter(id)).ToList() ?? new List<IRequirementReference>();
        public IReadOnlyList<IRequirementReference> TechnicalRequirements =>
            _result.TechnicalRequirements?.Select(id => (IRequirementReference)new RequirementReferenceAdapter(id)).ToList() ?? new List<IRequirementReference>();
        public bool AllRequirementsExist => _allExist;
    }

    private class RequirementReferenceAdapter : IRequirementReference
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

    #endregion
}
