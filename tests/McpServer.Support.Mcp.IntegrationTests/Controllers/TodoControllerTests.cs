using System.Net;
using System.Net.Http.Json;
using McpServer.Support.Mcp.IntegrationTests;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>TR-PLANNED-CORE-013: Integration tests for TODO CRUD endpoints.</summary>
[Trait("Category", "Integration")]
public sealed class TodoControllerTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly TodoWebFactory _factory;

    public TodoControllerTests()
    {
        _factory = new TodoWebFactory();
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", _factory.GetFullWorkspaceApiKey());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    /// <summary>GET /mcpserver/todo returns 200 with items from seed YAML.</summary>
    [Fact]
    public async Task Query_ReturnsOkWithItems()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0, "Expected at least one TODO item from seed file.");
    }

    /// <summary>GET /mcpserver/todo?keyword=Blazor filters by keyword.</summary>
    [Fact]
    public async Task Query_ByKeyword_FiltersResults()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo?keyword=Blazor", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
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
        var response = await _client.GetAsync(new Uri("/mcpserver/todo?priority=high", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.All(result.Items, item =>
            Assert.Equal("high", item.Priority, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>GET /mcpserver/todo?id=TEST-001 filters by id.</summary>
    [Fact]
    public async Task Query_ById_FiltersResults()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo?id=TEST-001", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("TEST-001", result.Items[0].Id);
    }

    /// <summary>GET /mcpserver/todo?section=mvp-app filters by section.</summary>
    [Fact]
    public async Task Query_BySection_FiltersResults()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo?section=mvp-app", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.All(result.Items, item =>
            Assert.Equal("mvp-app", item.Section, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>GET /mcpserver/todo?done=false filters by done status.</summary>
    [Fact]
    public async Task Query_ByDoneStatus_FiltersResults()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo?done=false", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.False(item.Done));
    }

    /// <summary>GET /mcpserver/todo/{id} returns 200 for existing item.</summary>
    [Fact]
    public async Task GetById_ExistingItem_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo/TEST-001", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<FlatItem>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.Equal("TEST-001", item.Id);
    }

    /// <summary>GET /mcpserver/todo/{id} returns 404 for missing item.</summary>
    [Fact]
    public async Task GetById_MissingItem_ReturnsNotFound()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo/NONEXISTENT-999", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
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

        var createResponse = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), createRequest, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var getResponse = await _client.GetAsync(new Uri("/mcpserver/todo/NEW-TODO-001", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.Equal("NEW-TODO-001", item.Id);
        Assert.Equal("New test item", item.Title);
        Assert.Equal("mvp-app", item.Section);
        Assert.Equal("low", item.Priority);
        Assert.Equal("Create note", item.Note);
        Assert.Equal("Remaining from create", item.Remaining);
    }

    /// <summary>POST /mcpserver/todo with the default /api-key token returns 403 Forbidden.</summary>
    [Fact]
    public async Task Create_WithDefaultApiKey_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var tokenService = _factory.Services.GetRequiredService<WorkspaceTokenService>();
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var defaultToken = tokenService.GetDefaultToken(config["Mcp:RepoRoot"]!)
                           ?? throw new InvalidOperationException("Workspace default API key was not generated for test host.");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", defaultToken);

        var createRequest = new
        {
            id = "DEFAULT-TODO-001",
            title = "Default key write should fail",
            section = "mvp-app",
            priority = "low"
        };

        var response = await client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), createRequest, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>POST /mcpserver/todo with duplicate id returns 400 Bad Request.</summary>
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

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), createRequest, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>PUT /mcpserver/todo/{id} updates the item fields.</summary>
    [Fact]
    public async Task Update_ExistingItem_ReturnsOk()
    {
        var updateRequest = new { title = "Updated Title", done = true };

        var response = await _client.PutAsJsonAsync(new Uri("/mcpserver/todo/TEST-002", UriKind.Relative), updateRequest, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await _client.GetAsync(new Uri("/mcpserver/todo/TEST-002", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.Equal("Updated Title", item.Title);
        Assert.True(item.Done);
    }

    /// <summary>PUT /mcpserver/todo/{id} for missing item returns 404.</summary>
    [Fact]
    public async Task Update_MissingItem_ReturnsNotFound()
    {
        var updateRequest = new { title = "Does not matter" };
        var response = await _client.PutAsJsonAsync(new Uri("/mcpserver/todo/NONEXISTENT-999", UriKind.Relative), updateRequest, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
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
        await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), createRequest, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var deleteResponse = await _client.DeleteAsync(new Uri("/mcpserver/todo/DEL-TODO-001", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync(new Uri("/mcpserver/todo/DEL-TODO-001", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>
    /// TEST-MCP-097: Verifies that the audit endpoint returns append-only ordered history after a full
    /// create → update → delete lifecycle against the SQLite-authoritative TODO store used by integration tests.
    /// The fixture uses one isolated TODO id so create, update, and delete actions can be asserted by version.
    /// </summary>
    [Fact]
    public async Task AuditEndpoint_AfterCreateUpdateDelete_ReturnsOrderedHistory()
    {
        var createRequest = new
        {
            id = "AUDIT-TODO-001",
            title = "Audit item",
            section = "mvp-app",
            priority = "medium"
        };

        var createResponse = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), createRequest, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var updateResponse = await _client.PutAsJsonAsync(
            new Uri("/mcpserver/todo/AUDIT-TODO-001", UriKind.Relative),
            new { title = "Audit item updated", done = true }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync(new Uri("/mcpserver/todo/AUDIT-TODO-001", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var auditResponse = await _client.GetAsync(new Uri("/mcpserver/todo/AUDIT-TODO-001/audit", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

        var audit = await auditResponse.Content.ReadFromJsonAsync<AuditQueryResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(audit);
        Assert.Equal(3, audit.TotalCount);
        Assert.Collection(
            audit.Entries,
            entry =>
            {
                Assert.Equal(1, entry.Version);
                Assert.Equal("created", entry.Action);
                Assert.Equal("Audit item", entry.Snapshot?.Title);
            },
            entry =>
            {
                Assert.Equal(2, entry.Version);
                Assert.Equal("updated", entry.Action);
                Assert.Equal("Audit item updated", entry.Snapshot?.Title);
                Assert.Equal("Audit item", entry.PreviousSnapshot?.Title);
            },
            entry =>
             {
                 Assert.Equal(3, entry.Version);
                 Assert.Equal("deleted", entry.Action);
                 Assert.Equal("Audit item updated", entry.Snapshot?.Title);
                 Assert.Equal("Audit item updated", entry.PreviousSnapshot?.Title);
             });
    }

    /// <summary>DELETE /mcpserver/todo/{id} for missing item returns 404.</summary>
    [Fact]
    public async Task Delete_MissingItem_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync(new Uri("/mcpserver/todo/NONEXISTENT-999", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
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

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
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

        var createResponse = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var getResponse = await _client.GetAsync(new Uri("/mcpserver/todo/FRTR-TEST-001", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
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
        await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), createRequest, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Update with FR/TR
        var updateRequest = new
        {
            functionalRequirements = new[] { "FR-WF-005" },
            technicalRequirements = new[] { "TR-MOBILE-001", "TR-MOBILE-002" }
        };
        var response = await _client.PutAsJsonAsync(new Uri("/mcpserver/todo/FRTR-UPD-001", UriKind.Relative), updateRequest, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await _client.GetAsync(new Uri("/mcpserver/todo/FRTR-UPD-001", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.NotNull(item.FunctionalRequirements);
        Assert.Single(item.FunctionalRequirements);
        Assert.Equal("FR-WF-005", item.FunctionalRequirements[0]);
        Assert.NotNull(item.TechnicalRequirements);
        Assert.Equal(2, item.TechnicalRequirements.Length);
    }

    /// <summary>POST /mcpserver/todo with unknown priority returns 400.</summary>
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

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>GET /mcpserver/todo/{id} returns FR/TR for item with requirements in seed YAML.</summary>
    [Fact]
    public async Task GetById_ItemWithFrTr_ReturnsRequirements()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/todo/TEST-001", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<FlatItem>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
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
        var response = await _client.GetAsync(new Uri("/mcpserver/todo/TEST-002", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<FlatItem>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
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

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var getResponse = await _client.GetAsync(new Uri("/mcpserver/todo/DEP-VALID-001", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(item?.DependsOn);
        Assert.Contains("TEST-001", item.DependsOn);
    }

    /// <summary>POST /mcpserver/todo with self-dependency returns 400.</summary>
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

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>POST /mcpserver/todo with nonexistent dependency returns 400.</summary>
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

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/todo", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>PUT /mcpserver/todo/{id} with circular dependency returns 400 (rejected).</summary>
    [Fact]
    public async Task Update_WithCircularDependency_ReturnsNotFound()
    {
        // TEST-002 depends on TEST-001. If we make TEST-001 depend on TEST-002, that's circular.
        var request = new { dependsOn = new[] { "TEST-002" } };
        var response = await _client.PutAsJsonAsync(new Uri("/mcpserver/todo/TEST-001", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MutationResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Circular", result.Error ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// BUG-APPVISIBILITY-001: API dashboard/count/list callers that switch workspaces must see only the
    /// TODO and session-log rows for the requested workspace. The fixture uses two configured workspaces
    /// in one test host and separate authenticated clients so stale request workspace state cannot pass.
    /// </summary>
    [Fact]
    public async Task WhenTwoWorkspacesQueryTodoAndSessionLogsThenEachWorkspaceSeesOnlyItsOwnRows()
    {
        var secondaryWorkspacePath = Path.Combine(
            Path.GetTempPath(),
            $"mcp-todo-secondary-{Guid.NewGuid():N}",
            "workspace");
        var secondaryDataPath = Path.Combine(Path.GetTempPath(), $"mcp-todo-secondary-data-{Guid.NewGuid():N}");
        SeedMinimalWorkspaceFiles(secondaryWorkspacePath);
        Directory.CreateDirectory(secondaryDataPath);

        try
        {
            var overrides = new Dictionary<string, string?>
            {
                { "Mcp:Workspaces:1:WorkspacePath", secondaryWorkspacePath },
                { "Mcp:Workspaces:1:Name", "todo-secondary" },
                { "Mcp:Workspaces:1:TodoPath", Path.Combine(secondaryWorkspacePath, "docs", "Project", "TODO.yaml") },
                { "Mcp:Workspaces:1:DataDirectory", secondaryDataPath },
                { "Mcp:Workspaces:1:IsPrimary", "false" },
                { "Mcp:Workspaces:1:IsEnabled", "true" },
            };

            using var factory = new CustomWebApplicationFactory(null, overrides);
            using var primaryClient = factory.CreateClient();
            using var secondaryClient = factory.CreateClient();
            AddWorkspaceAuth(primaryClient, factory.Services, factory.WorkspacePath);
            AddWorkspaceAuth(secondaryClient, factory.Services, secondaryWorkspacePath);

            var primaryTodoId = "BUG-PRIMARY-001";
            var secondaryTodoId = "BUG-SECONDARY-001";
            var primarySessionId = BuildSessionId("Codex", $"primary-{Guid.NewGuid():N}");
            var secondarySessionId = BuildSessionId("Cursor", $"secondary-{Guid.NewGuid():N}");

            var primaryTodo = await primaryClient.PostAsJsonAsync(
                new Uri("/mcpserver/todo", UriKind.Relative),
                new { id = primaryTodoId, title = "Primary workspace TODO", section = "bug-appvisibility", priority = "high" }, cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            var secondaryTodo = await secondaryClient.PostAsJsonAsync(
                new Uri("/mcpserver/todo", UriKind.Relative),
                new { id = secondaryTodoId, title = "Secondary workspace TODO", section = "bug-appvisibility", priority = "high" }, cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            var primarySession = await primaryClient.PostAsJsonAsync(
                new Uri("/mcpserver/sessionlog", UriKind.Relative),
                CreateSessionLog("Codex", primarySessionId), cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            var secondarySession = await secondaryClient.PostAsJsonAsync(
                new Uri("/mcpserver/sessionlog", UriKind.Relative),
                CreateSessionLog("Cursor", secondarySessionId), cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            Assert.Equal(HttpStatusCode.Created, primaryTodo.StatusCode);
            Assert.Equal(HttpStatusCode.Created, secondaryTodo.StatusCode);
            Assert.Equal(HttpStatusCode.Created, primarySession.StatusCode);
            Assert.Equal(HttpStatusCode.Created, secondarySession.StatusCode);

            var primaryTodos = await primaryClient.GetFromJsonAsync<QueryResult>(
                new Uri("/mcpserver/todo?section=bug-appvisibility", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            var secondaryTodos = await secondaryClient.GetFromJsonAsync<QueryResult>(
                new Uri("/mcpserver/todo?section=bug-appvisibility", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            var primaryLogs = await primaryClient.GetFromJsonAsync<SessionLogQueryResult>(
                new Uri("/mcpserver/sessionlog?limit=20", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            var secondaryLogs = await secondaryClient.GetFromJsonAsync<SessionLogQueryResult>(
                new Uri("/mcpserver/sessionlog?limit=20", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            Assert.NotNull(primaryTodos);
            Assert.Equal(1, primaryTodos!.TotalCount);
            Assert.Equal(primaryTodoId, primaryTodos.Items.Single().Id);
            Assert.DoesNotContain(primaryTodos.Items, item => item.Id == secondaryTodoId);

            Assert.NotNull(secondaryTodos);
            Assert.Equal(1, secondaryTodos!.TotalCount);
            Assert.Equal(secondaryTodoId, secondaryTodos.Items.Single().Id);
            Assert.DoesNotContain(secondaryTodos.Items, item => item.Id == primaryTodoId);

            Assert.NotNull(primaryLogs);
            Assert.Contains(primaryLogs!.Items, item => item.SessionId == primarySessionId);
            Assert.DoesNotContain(primaryLogs.Items, item => item.SessionId == secondarySessionId);

            Assert.NotNull(secondaryLogs);
            Assert.Contains(secondaryLogs!.Items, item => item.SessionId == secondarySessionId);
            Assert.DoesNotContain(secondaryLogs.Items, item => item.SessionId == primarySessionId);

            var secondaryTodoFromPrimary = await primaryClient.GetAsync(
                new Uri($"/mcpserver/todo/{secondaryTodoId}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var primaryTodoFromSecondary = await secondaryClient.GetAsync(
                new Uri($"/mcpserver/todo/{primaryTodoId}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var secondarySessionFromPrimary = await primaryClient.GetAsync(
                new Uri($"/mcpserver/sessionlog/Cursor/{secondarySessionId}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var primarySessionFromSecondary = await secondaryClient.GetAsync(
                new Uri($"/mcpserver/sessionlog/Codex/{primarySessionId}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(HttpStatusCode.NotFound, secondaryTodoFromPrimary.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, primaryTodoFromSecondary.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, secondarySessionFromPrimary.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, primarySessionFromSecondary.StatusCode);
        }
        finally
        {
            TryDeleteDirectory(secondaryWorkspacePath);
            TryDeleteDirectory(secondaryDataPath);
            TryDeleteDirectory(Path.GetDirectoryName(secondaryWorkspacePath));
        }
    }

    private sealed record MutationResult(bool Success, string? Error, string? FailureKind = null);
    private sealed record ProjectionStatusResult(
        string AuthoritativeStore,
        string AuthoritativeDataSource,
        string ProjectionTargetPath,
        bool ProjectionTargetExists,
        bool ProjectionConsistent,
        bool RepairRequired,
        string VerifiedAtUtc,
        string? LastImportedFromYamlUtc,
        string? LastProjectedToYamlUtc,
        string? LastProjectionFailureUtc,
        string? LastProjectionFailure,
        string? Message);
    private sealed record ProjectionRepairResult(bool Success, string? Error, ProjectionStatusResult Status);

    #region Test DTOs (for deserialization)

    private sealed record QueryResult(FlatItem[] Items, int TotalCount);
    private sealed record AuditQueryResult(AuditEntry[] Entries, int TotalCount);
    private sealed record AuditEntry(long AuditId, string TodoId, int Version, string Action, string RecordedAtUtc, FlatItem? Snapshot, FlatItem? PreviousSnapshot, string? Source);
    private sealed record SessionLogQueryResult(SessionLogItem[] Items, int TotalCount);
    private sealed record SessionLogItem(string? SourceType, string? SessionId, string? Title);

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

    /// <summary>TR-PLANNED-CORE-013: WebApplicationFactory that seeds a temporary TODO.yaml.</summary>
    public sealed class TodoWebFactory : WebApplicationFactory<McpApiEntryPoint>, IDisposable
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "mcp-todo-tests-" + Guid.NewGuid().ToString("N")[..8]);

        public string TodoYamlPath => Path.Combine(_tempDir, "docs", "Project", "TODO.yaml");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Create seed TODO.yaml
            var projectDir = Path.Combine(_tempDir, "docs", "Project");
            var databasePath = Path.Combine(_tempDir, "mcp.db");
            Directory.CreateDirectory(projectDir);
            File.WriteAllText(TodoYamlPath, SeedYaml);

            builder.UseEnvironment("Test");
            builder.UseContentRoot(CustomWebApplicationFactory.ResolveContentRoot());
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Mcp:DataSource", databasePath },
                    { "Mcp:Database:Provider", "sqlite" },
                    { "Mcp:Database:Sqlite:DataSource", databasePath },
                    { "Mcp:UseInMemoryDatabaseForTests", "false" },
                    { "DataFolder", _tempDir },
                    { "Mcp:RepoRoot", _tempDir },
                    { "Mcp:TodoFilePath", "docs/Project/TODO.yaml" },
                    { "Mcp:TodoStorage:Provider", "sqlite" },
                    { "Mcp:TodoStorage:SqliteDataSource", "mcp.db" },
                    { "Mcp:Workspaces:0:WorkspacePath", _tempDir },
                    { "Mcp:Workspaces:0:Name", Path.GetFileName(_tempDir) },
                    { "Mcp:Workspaces:0:TodoPath", "docs/Project/TODO.yaml" },
                    { "Mcp:Workspaces:0:IsPrimary", "true" },
                    { "Mcp:Workspaces:0:IsEnabled", "true" }
                });
            });
            builder.ConfigureServices(services =>
            {
                IntegrationTestDatabase.ConfigureSqlite(services, databasePath);
                services.RemoveAll<IWorkspaceProjectionWriter>();
                services.AddSingleton<IWorkspaceProjectionWriter, NoOpWorkspaceProjectionWriter>();
                services.AddHostedService<IntegrationTestDatabase.Initializer>();
            });
        }

        public string GetFullWorkspaceApiKey()
        {
            var tokenService = Services.GetRequiredService<WorkspaceTokenService>();
            return tokenService.GetToken(_tempDir)
                   ?? throw new InvalidOperationException("Workspace full API key was not generated for test host.");
        }

        public new void Dispose()
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

    private static void AddWorkspaceAuth(HttpClient client, IServiceProvider services, string workspacePath)
    {
        using var scope = services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<WorkspaceTokenService>();
        var token = tokenService.GetToken(workspacePath) ?? tokenService.GenerateToken(workspacePath);

        client.DefaultRequestHeaders.Remove("X-Api-Key");
        client.DefaultRequestHeaders.Add("X-Api-Key", token);
        client.DefaultRequestHeaders.Remove("X-Workspace-Path");
        client.DefaultRequestHeaders.Add("X-Workspace-Path", workspacePath);
    }

    private static object CreateSessionLog(string sourceType, string sessionId)
    {
        return new
        {
            sourceType,
            sessionId,
            title = "Workspace visibility session",
            model = "codex",
            started = "2026-05-27T12:00:00Z",
            lastUpdated = "2026-05-27T12:01:00Z",
            status = "completed",
            turnCount = 1,
            turns = new[]
            {
                new
                {
                    requestId = "req-20260527T120000Z-visibility",
                    timestamp = "2026-05-27T12:00:00Z",
                    queryText = "workspace visibility",
                    response = "ok",
                    status = "completed"
                }
            }
        };
    }

    private static string BuildSessionId(string agent, string suffix)
    {
        var normalized = new string((suffix ?? string.Empty)
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray())
            .Trim('-');
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "session";

        return $"{agent}-20260527T120000Z-{normalized}";
    }

    private static void SeedMinimalWorkspaceFiles(string workspacePath)
    {
        var projectPath = Path.Combine(workspacePath, "docs", "Project");
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "TODO.yaml"), """
            mvp-app:
              high-priority: []
            """);
    }

    private static void TryDeleteDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    #endregion
}
