using System.Net;
using System.Text;
using System.Text.Json;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-BUGTRIAGE-040: Verifies TODO selection survives separate workflow instances.
/// </summary>
public sealed class TodoWorkflowSelectionStoreTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "mcpserver-todo-selection-" + Guid.NewGuid().ToString("N"));

    /// <summary>Removes temporary selection-store files created by the test.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    /// <summary>Selected TODO updates reload selection persisted by a previous workflow instance.</summary>
    [Fact]
    public async Task UpdateSelectedAsync_LoadsSelectionSavedByPreviousWorkflowInstance()
    {
        var storePath = Path.Combine(_tempDirectory, "todo-selection.json");
        var handler = new StatefulTodoHandler();
        var firstWorkflow = CreateWorkflow(handler, storePath);

        await firstWorkflow.SelectAsync("MCP-TODO-001", TestContext.Current.CancellationToken).ConfigureAwait(true);

        var secondWorkflow = CreateWorkflow(handler, storePath);
        var result = await secondWorkflow.UpdateAsync(
                new UpdateRequest { Done = true, Remaining = "Selection survived a wrapper restart." },
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal("MCP-TODO-001", handler.LastUpdatedId);
        Assert.True(secondWorkflow.CurrentSelection()?.Done);
        Assert.Equal("Selection survived a wrapper restart.", handler.Item.Remaining);
    }

    private static TodoWorkflow CreateWorkflow(StatefulTodoHandler handler, string storePath)
    {
        var http = new HttpClient(handler, disposeHandler: false);
        var client = new TodoClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = "F:\\GitHub\\McpServer",
        });
        return new TodoWorkflow(client, new FileTodoSelectionStore(storePath));
    }

    private sealed class StatefulTodoHandler : HttpMessageHandler
    {
        public TodoFlatItem Item { get; private set; } = new()
        {
            Id = "MCP-TODO-001",
            Title = "Persist selection",
            Section = "Backlog",
            Priority = "medium",
            Done = false,
        };

        public string? LastUpdatedId { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (request.Method == HttpMethod.Get && path.EndsWith("/mcpserver/todo/MCP-TODO-001", StringComparison.Ordinal))
                return JsonResponse(Item);

            if (request.Method == HttpMethod.Put && path.EndsWith("/mcpserver/todo/MCP-TODO-001", StringComparison.Ordinal))
            {
                LastUpdatedId = Uri.UnescapeDataString(path.Split('/', StringSplitOptions.RemoveEmptyEntries).Last());
                var body = request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var update = JsonSerializer.Deserialize<TodoUpdateRequest>(body, JsonOptions) ?? new TodoUpdateRequest();
                Item = new TodoFlatItem
                {
                    Id = Item.Id,
                    Title = update.Title ?? Item.Title,
                    Section = update.Section ?? Item.Section,
                    Priority = update.Priority ?? Item.Priority,
                    Done = update.Done ?? Item.Done,
                    Remaining = update.Remaining ?? Item.Remaining,
                };

                return JsonResponse(new TodoMutationResult { Success = true, Item = Item });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
        }

        private static HttpResponseMessage JsonResponse(object value)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json"),
            };
    }

    private sealed class UpdateRequest : ITodoUpdateRequest
    {
        public string? Title { get; init; }
        public string? Priority { get; init; }
        public string? Section { get; init; }
        public bool? Done { get; init; }
        public string? Estimate { get; init; }
        public IReadOnlyList<string>? Description { get; init; }
        public IReadOnlyList<string>? TechnicalDetails { get; init; }
        public IReadOnlyList<ITodoSubtask>? ImplementationTasks { get; init; }
        public string? Note { get; init; }
        public string? CompletedDate { get; init; }
        public string? DoneSummary { get; init; }
        public string? Remaining { get; init; }
        public IReadOnlyList<string>? DependsOn { get; init; }
        public IReadOnlyList<string>? FunctionalRequirements { get; init; }
        public IReadOnlyList<string>? TechnicalRequirements { get; init; }
    }
}
