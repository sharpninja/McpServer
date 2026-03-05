using System.Net;
using System.Net.Http.Json;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>TR-PLANNED-013: Integration tests for TODO CRUD endpoints.</summary>
public sealed class TodoControllerTests : IClassFixture<TodoControllerTests.TodoWebFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly TodoWebFactory _factory;

    public TodoControllerTests(TodoWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", factory.GetFullWorkspaceApiKey());
    }

    public void Dispose() => _client.Dispose();

    /// <summary>GET /mcpserver/todo returns 200 with items from seed YAML.</summary>
    [Fact]
    public async Task Query_ReturnsOkWithItems()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0, "Expected at least one TODO item from seed file.");
    }

    /// <summary>GET /mcpserver/todo?keyword=Blazor filters by keyword.</summary>
    [Fact]
    public async Task Query_ByKeyword_FiltersResults()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo?keyword=Blazor", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.All(result.Items, item =>
        {
            var combined = string.Join(" ", item.Title ?? "",
                string.Join(" ", item.Description ?? []),
                string.Join(" ", item.TechnicalDetails ?? []));
            Assert.Contains("Blazor", combined, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>GET /mcpserver/todo?priority=high filters by priority.</summary>
    [Fact]
    public async Task Query_ByPriority_FiltersResults()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo?priority=high", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.All(result.Items, item =>
            Assert.Equal("high", item.Priority, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>GET /mcpserver/todo?id=TEST-001 filters by id.</summary>
    [Fact]
    public async Task Query_ById_FiltersResults()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo?id=TEST-001", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("TEST-001", result.Items[0].Id);
    }

    /// <summary>GET /mcpserver/todo?section=mvp-app filters by section.</summary>
    [Fact]
    public async Task Query_BySection_FiltersResults()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo?section=mvp-app", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.All(result.Items, item =>
            Assert.Equal("mvp-app", item.Section, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>GET /mcpserver/todo?done=false filters by done status.</summary>
    [Fact]
    public async Task Query_ByDoneStatus_FiltersResults()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo?done=false", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.False(item.Done));
    }

    /// <summary>GET /mcpserver/todo/{id} returns 200 for existing item.</summary>
    [Fact]
    public async Task GetById_ExistingItem_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo/TEST-001", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.Equal("TEST-001", item.Id);
    }

    /// <summary>GET /mcpserver/todo/{id} returns 404 for missing item.</summary>
    [Fact]
    public async Task GetById_MissingItem_ReturnsNotFound()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo/NONEXISTENT-999", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>POST /mcpserver/todo creates a new item and GET retrieves it.</summary>
    [Fact]
    public async Task Create_ThenGetById_ReturnsCreatedItem()
    {
        var createRequest = new
        {
            id = "NEW-TODO-001",
            title = "New test item",
            section = "mvp-app",
            priority = "low",
            estimate = "8-16 hours",
            note = "Create note",
            remaining = "Remaining from create",
            description = new[] { "First line", "Second line" }
        };

        var createResponse = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), createRequest).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var getResponse = await _client.GetAsync(new Uri("/mcpserver/todo/NEW-TODO-001", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.Equal("NEW-TODO-001", item.Id);
        Assert.Equal("New test item", item.Title);
        Assert.Equal("mvp-app", item.Section);
        Assert.Equal("low", item.Priority);
        Assert.Equal("Create note", item.Note);
        Assert.Equal("Remaining from create", item.Remaining);
    }

    /// <summary>POST /mcpserver/todo with duplicate id returns 409 Conflict.</summary>
    [Fact]
    public async Task Create_DuplicateId_ReturnsConflict()
    {
        var createRequest = new
        {
            id = "TEST-001",
            title = "Duplicate",
            section = "mvp-app",
            priority = "high"
        };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), createRequest).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>PUT /mcpserver/todo/{id} updates the item fields.</summary>
    [Fact]
    public async Task Update_ExistingItem_ReturnsOk()
    {
        var updateRequest = new { title = "Updated Title", done = true };

        var response = await _client.PutAsJsonAsync(new Uri("/mcpserver/todo/TEST-002", UriKind.Relative), updateRequest).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await _client.GetAsync(new Uri("/mcpserver/todo/TEST-002", UriKind.Relative)).ConfigureAwait(true);
        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.Equal("Updated Title", item.Title);
        Assert.True(item.Done);
    }

    /// <summary>PUT /mcpserver/todo/{id} for missing item returns 404.</summary>
    [Fact]
    public async Task Update_MissingItem_ReturnsNotFound()
    {
        var updateRequest = new { title = "Does not matter" };
        var response = await _client.PutAsJsonAsync(new Uri("/mcpserver/todo/NONEXISTENT-999", UriKind.Relative), updateRequest).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>DELETE /mcpserver/todo/{id} removes the item.</summary>
    [Fact]
    public async Task Delete_ExistingItem_ReturnsOkAndRemoves()
    {
        // First create an item to delete
        var createRequest = new
        {
            id = "DEL-TODO-001",
            title = "To be deleted",
            section = "mvp-support",
            priority = "low"
        };
        await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), createRequest).ConfigureAwait(true);

        var deleteResponse = await _client.DeleteAsync(new Uri("/mcpserver/todo/DEL-TODO-001", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync(new Uri("/mcpserver/todo/DEL-TODO-001", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>DELETE /mcpserver/todo/{id} for missing item returns 404.</summary>
    [Fact]
    public async Task Delete_MissingItem_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync(new Uri("/mcpserver/todo/NONEXISTENT-999", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>POST /mcpserver/todo with any section creates item (sections are now arbitrary).</summary>
    [Fact]
    public async Task Create_UnknownSection_ReturnsConflict()
    {
        var request = new
        {
            id = "BAD-SEC-001",
            title = "Bad section",
            section = "unknown-section",
            priority = "high"
        };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>POST /mcpserver/todo with FR/TR creates item with requirement IDs.</summary>
    [Fact]
    public async Task Create_WithFrTr_ReturnsCreatedItemWithRequirements()
    {
        var request = new
        {
            id = "FRTR-TEST-001",
            title = "FR/TR test item",
            section = "mvp-app",
            priority = "low",
            functionalRequirements = new[] { "FR-LOC-001", "FR-LOC-002" },
            technicalRequirements = new[] { "TR-API-001" }
        };

        var createResponse = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var getResponse = await _client.GetAsync(new Uri("/mcpserver/todo/FRTR-TEST-001", UriKind.Relative)).ConfigureAwait(true);
        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.NotNull(item.FunctionalRequirements);
        Assert.Equal(2, item.FunctionalRequirements.Length);
        Assert.Contains("FR-LOC-001", item.FunctionalRequirements);
        Assert.Contains("FR-LOC-002", item.FunctionalRequirements);
        Assert.NotNull(item.TechnicalRequirements);
        Assert.Single(item.TechnicalRequirements);
        Assert.Equal("TR-API-001", item.TechnicalRequirements[0]);
    }

    /// <summary>PUT /mcpserver/todo/{id} updates FR/TR fields.</summary>
    [Fact]
    public async Task Update_WithFrTr_UpdatesRequirements()
    {
        // Create item first
        var createRequest = new
        {
            id = "FRTR-UPD-001",
            title = "Update FR/TR test",
            section = "mvp-app",
            priority = "low"
        };
        await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), createRequest).ConfigureAwait(true);

        // Update with FR/TR
        var updateRequest = new
        {
            functionalRequirements = new[] { "FR-WF-005" },
            technicalRequirements = new[] { "TR-MOBILE-001", "TR-MOBILE-002" }
        };
        var response = await _client.PutAsJsonAsync(new Uri("/mcpserver/todo/FRTR-UPD-001", UriKind.Relative), updateRequest).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await _client.GetAsync(new Uri("/mcpserver/todo/FRTR-UPD-001", UriKind.Relative)).ConfigureAwait(true);
        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.NotNull(item.FunctionalRequirements);
        Assert.Single(item.FunctionalRequirements);
        Assert.Equal("FR-WF-005", item.FunctionalRequirements[0]);
        Assert.NotNull(item.TechnicalRequirements);
        Assert.Equal(2, item.TechnicalRequirements.Length);
    }

    /// <summary>POST /mcpserver/todo with unknown priority returns 409.</summary>
    [Fact]
    public async Task Create_UnknownPriority_ReturnsConflict()
    {
        var request = new
        {
            id = "BAD-PRI-002",
            title = "Bad priority",
            section = "mvp-app",
            priority = "critical"
        };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>GET /mcpserver/todo/{id} returns FR/TR for item with requirements in seed YAML.</summary>
    [Fact]
    public async Task GetById_ItemWithFrTr_ReturnsRequirements()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo/TEST-001", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.NotNull(item.FunctionalRequirements);
        Assert.Single(item.FunctionalRequirements);
        Assert.Equal("FR-LOC-001", item.FunctionalRequirements[0]);
        Assert.NotNull(item.TechnicalRequirements);
        Assert.Equal(2, item.TechnicalRequirements.Length);
        Assert.Contains("TR-API-001", item.TechnicalRequirements);
        Assert.Contains("TR-API-002", item.TechnicalRequirements);
    }

    /// <summary>GET /mcpserver/todo/{id} returns DependsOn for item with dependencies.</summary>
    [Fact]
    public async Task GetById_ItemWithDependsOn_ReturnsDeps()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo/TEST-002", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.NotNull(item.DependsOn);
        Assert.Contains("TEST-001", item.DependsOn);
    }

    /// <summary>POST /mcpserver/todo with valid depends-on succeeds.</summary>
    [Fact]
    public async Task Create_WithValidDependsOn_Succeeds()
    {
        var request = new
        {
            id = "DEP-VALID-001",
            title = "Valid dependency",
            section = "mvp-app",
            priority = "low",
            dependsOn = new[] { "TEST-001" }
        };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var getResponse = await _client.GetAsync(new Uri("/mcpserver/todo/DEP-VALID-001", UriKind.Relative)).ConfigureAwait(true);
        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(item?.DependsOn);
        Assert.Contains("TEST-001", item.DependsOn);
    }

    /// <summary>POST /mcpserver/todo with self-dependency returns 409.</summary>
    [Fact]
    public async Task Create_WithSelfDependency_ReturnsConflict()
    {
        var request = new
        {
            id = "DEP-SELF-001",
            title = "Self dependency",
            section = "mvp-app",
            priority = "low",
            dependsOn = new[] { "DEP-SELF-001" }
        };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>POST /mcpserver/todo with nonexistent dependency returns 409.</summary>
    [Fact]
    public async Task Create_WithNonexistentDependency_ReturnsConflict()
    {
        var request = new
        {
            id = "DEP-BAD-001",
            title = "Bad dependency",
            section = "mvp-app",
            priority = "low",
            dependsOn = new[] { "DOES-NOT-EXIST" }
        };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>PUT /mcpserver/todo/{id} with circular dependency returns 404 (rejected).</summary>
    [Fact]
    public async Task Update_WithCircularDependency_ReturnsNotFound()
    {
        // TEST-002 depends on TEST-001. If we make TEST-001 depend on TEST-002, that's circular.
        var request = new { dependsOn = new[] { "TEST-002" } };
        var response = await _client.PutAsJsonAsync(new Uri("/mcpserver/todo/TEST-001", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MutationResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Circular", result.Error ?? "", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record MutationResult(bool Success, string? Error);

    #region Test DTOs (for deserialization)

    private sealed record QueryResult(FlatItem[] Items, int TotalCount);

    private sealed record FlatItem(
        string? Id,
        string? Title,
        string? Section,
        string? Priority,
        bool Done,
        string? Estimate,
        string? Note,
        string[]? Description,
        string[]? TechnicalDetails,
        string? CompletedDate,
        string? DoneSummary,
        string? Remaining,
        string? PriorityNote,
        string? Reference,
        string[]? DependsOn,
        string[]? FunctionalRequirements,
        string[]? TechnicalRequirements);

    #endregion

    #region Test Factory

    /// <summary>TR-PLANNED-013: WebApplicationFactory that seeds a temporary TODO.yaml.</summary>
    public sealed class TodoWebFactory : WebApplicationFactory<McpApiEntryPoint>, IDisposable
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "mcp-todo-tests-" + Guid.NewGuid().ToString("N")[..8]);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Create seed TODO.yaml
            var projectDir = Path.Combine(_tempDir, "docs", "Project");
            Directory.CreateDirectory(projectDir);
            File.WriteAllText(Path.Combine(projectDir, "TODO.yaml"), SeedYaml);

            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Mcp:DataSource", ":memory:" },
                    { "DataFolder", _tempDir },
                    { "Mcp:RepoRoot", _tempDir },
                    { "Mcp:TodoFilePath", "docs/Project/TODO.yaml" }
                });
            });
        }

        public string GetFullWorkspaceApiKey()
        {
            var tokenService = Services.GetRequiredService<WorkspaceTokenService>();
            return tokenService.GetToken(_tempDir)
                   ?? throw new InvalidOperationException("Workspace full API key was not generated for test host.");
        }

        private new void Dispose()
        {
            base.Dispose();
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private const string SeedYaml = """
            mvp-app:
              high-priority:
                - id: TEST-001
                  title: Test item one
                  estimate: "8-16 hours"
                  done: false
                  description:
                    - First description line
                    - Blazor component work
                  technical-details:
                    - Use xUnit for testing
                  functional-requirements:
                    - FR-LOC-001
                  technical-requirements:
                    - TR-API-001
                    - TR-API-002
                  implementation-tasks:
                    - task: Planning
                      done: true
                    - task: Implementation
                      done: false
              medium-priority:
                - id: TEST-002
                  title: Test item two
                  estimate: "16-24 hours"
                  done: false
                  depends-on:
                    - TEST-001
                  description:
                    - Second item description
            mvp-support:
              high-priority:
                - id: TEST-003
                  title: Support item
                  done: true
                  completed: "2026-01-15"
            """;
    }

    #endregion
}
