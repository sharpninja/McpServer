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

/// <summary>
/// Integration tests that exercise the full TODO lifecycle as used by both VS and VSCode extensions:
/// create a TODO → serialize to markdown → parse markdown back → update via API → verify round-trip.
/// Each test creates its own items and cleans up afterwards via DELETE.
/// Uses <see cref="LifecycleWebFactory"/> which seeds a minimal TODO.yaml.
/// </summary>
public sealed class TodoLifecycleIntegrationTests
    : IClassFixture<TodoLifecycleIntegrationTests.LifecycleWebFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly List<string> _createdIds = [];

    public TodoLifecycleIntegrationTests(LifecycleWebFactory factory)
    {
        _client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, factory.Services);
    }

    public void Dispose()
    {
        // Clean up any items created during the test
        foreach (var id in _createdIds)
        {
            _client.DeleteAsync(new Uri($"/mcpserver/todo/{Uri.EscapeDataString(id)}", UriKind.Relative))
                .GetAwaiter().GetResult();
        }
        _client.Dispose();
    }

    /// <summary>
    /// Full lifecycle: create → GET → serialize to markdown → parse markdown → PUT update → verify.
    /// This mirrors the VS extension flow: user opens a TODO (GET + ToMarkdown), edits the markdown,
    /// saves (FromMarkdown + PUT), and the list refreshes (GET all).
    /// </summary>
    [Fact]
    public async Task Lifecycle_Create_SerializeToMarkdown_ParseBack_Update_Verify()
    {
        const string id = "LIFECYCLE-TODO-001";

        // 1. CREATE the TODO via API (simulates "New Todo" in extension)
        var createRequest = new
        {
            id,
            title = "Initial title",
            section = "mvp-app",
            priority = "high",
            estimate = "4-8 hours",
            description = new[] { "First description line", "Second line" },
            technicalDetails = new[] { "Use xUnit", "Follow patterns" },
            implementationTasks = new[]
            {
                new { task = "Design", done = false },
                new { task = "Implement", done = false }
            },
            functionalRequirements = new[] { "FR-LOC-001" },
            technicalRequirements = new[] { "TR-API-001", "TR-API-002" }
        };

        var createResponse = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/todo", UriKind.Relative), createRequest).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        _createdIds.Add(id);

        // 2. GET the item (simulates extension opening the todo)
        var getResponse = await _client.GetAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.Equal(id, item.Id);
        Assert.Equal("Initial title", item.Title);
        Assert.Equal("high", item.Priority);

        // 3. Serialize to markdown (simulates TodoMarkdown.ToMarkdown / todoToMarkdown)
        var markdown = SerializeToMarkdown(item);
        Assert.Contains("---", markdown);
        Assert.Contains($"id: {id}", markdown);
        Assert.Contains("priority: high", markdown);
        Assert.Contains("# Initial title", markdown);
        Assert.Contains("- Use xUnit", markdown);
        Assert.Contains("- [ ] Design", markdown);
        Assert.Contains("FR-LOC-001", markdown);

        // 4. Simulate user editing the markdown: change title, mark a task done, update description
        var editedMarkdown = markdown
            .Replace("# Initial title", "# Updated title after edit")
            .Replace("- [ ] Design", "- [x] Design")
            .Replace("First description line", "Modified first description");

        // 5. Parse the edited markdown back (simulates TodoMarkdown.FromMarkdown / markdownToUpdateBody)
        var updateBody = ParseMarkdownToUpdateBody(editedMarkdown);
        Assert.Equal("Updated title after edit", updateBody.Title);
        Assert.NotNull(updateBody.ImplementationTasks);
        Assert.True(updateBody.ImplementationTasks[0].Done, "Design task should be marked done");
        Assert.NotNull(updateBody.Description);
        Assert.Contains("Modified first description", updateBody.Description);

        // 6. PUT the update (simulates extension save → MCP update)
        var putResponse = await _client.PutAsJsonAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative), updateBody).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var mutResult = await putResponse.Content.ReadFromJsonAsync<MutationResult>().ConfigureAwait(true);
        Assert.NotNull(mutResult);
        Assert.True(mutResult.Success);

        // 7. GET again to verify the update persisted (simulates list refresh / TodoSaved event)
        var verifyResponse = await _client.GetAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var updated = await verifyResponse.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(updated);
        Assert.Equal("Updated title after edit", updated.Title);
        Assert.NotNull(updated.Description);
        Assert.Contains("Modified first description", updated.Description);
    }

    /// <summary>
    /// Verifies that the list endpoint reflects newly created and updated items,
    /// simulating the tree view refresh that fires after the TodoSaved event.
    /// </summary>
    [Fact]
    public async Task Lifecycle_ListRefreshReflectsChanges()
    {
        const string id = "LIFECYCLE-LIST-001";

        // Create
        var createRequest = new
        {
            id,
            title = "List refresh test",
            section = "mvp-app",
            priority = "medium"
        };
        var createResponse = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/todo", UriKind.Relative), createRequest).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        _createdIds.Add(id);

        // Verify item appears in list (simulates tree refresh after TodoSaved)
        var listResponse = await _client.GetAsync(
            new Uri($"/mcpserver/todo?id={id}", UriKind.Relative)).ConfigureAwait(true);
        var listResult = await listResponse.Content.ReadFromJsonAsync<QueryResult>().ConfigureAwait(true);
        Assert.NotNull(listResult);
        Assert.Single(listResult.Items);
        Assert.Equal(id, listResult.Items[0].Id);
        Assert.Equal("List refresh test", listResult.Items[0].Title);

        // Update
        var updateBody = new { title = "Updated list refresh test", done = true };
        var putResponse = await _client.PutAsJsonAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative), updateBody).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        // Verify list reflects the update (simulates second TodoSaved → refresh)
        var listResponse2 = await _client.GetAsync(
            new Uri($"/mcpserver/todo?id={id}", UriKind.Relative)).ConfigureAwait(true);
        var listResult2 = await listResponse2.Content.ReadFromJsonAsync<QueryResult>().ConfigureAwait(true);
        Assert.NotNull(listResult2);
        Assert.Single(listResult2.Items);
        Assert.Equal("Updated list refresh test", listResult2.Items[0].Title);
        Assert.True(listResult2.Items[0].Done);
    }

    /// <summary>
    /// Exercises the markdown round-trip for FR/TR fields which are serialized
    /// into YAML front matter and must survive the serialize → edit → parse → update cycle.
    /// </summary>
    [Fact]
    public async Task Lifecycle_FrTrFieldsSurviveMarkdownRoundTrip()
    {
        const string id = "LIFECYCLE-FRTR-001";

        var createRequest = new
        {
            id,
            title = "FR/TR round-trip",
            section = "mvp-app",
            priority = "low",
            functionalRequirements = new[] { "FR-WF-005", "FR-LOC-001" },
            technicalRequirements = new[] { "TR-MOBILE-001" },
            dependsOn = new[] { "SEED-TODO-001" }
        };
        var createResponse = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/todo", UriKind.Relative), createRequest).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        _createdIds.Add(id);

        // GET and serialize to markdown
        var getResponse = await _client.GetAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative)).ConfigureAwait(true);
        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(item);

        var markdown = SerializeToMarkdown(item);

        // Verify FR/TR appear in YAML front matter
        Assert.Contains("functional-requirements:", markdown);
        Assert.Contains("  - FR-WF-005", markdown);
        Assert.Contains("  - FR-LOC-001", markdown);
        Assert.Contains("technical-requirements:", markdown);
        Assert.Contains("  - TR-MOBILE-001", markdown);
        Assert.Contains("depends-on:", markdown);
        Assert.Contains("  - SEED-TODO-001", markdown);

        // Parse back — FR/TR should survive
        var parsed = ParseMarkdownToUpdateBody(markdown);
        Assert.NotNull(parsed.FunctionalRequirements);
        Assert.Equal(2, parsed.FunctionalRequirements.Length);
        Assert.Contains("FR-WF-005", parsed.FunctionalRequirements);
        Assert.Contains("FR-LOC-001", parsed.FunctionalRequirements);
        Assert.NotNull(parsed.TechnicalRequirements);
        Assert.Single(parsed.TechnicalRequirements);
        Assert.Equal("TR-MOBILE-001", parsed.TechnicalRequirements[0]);
        Assert.NotNull(parsed.DependsOn);
        Assert.Contains("SEED-TODO-001", parsed.DependsOn);
    }

    /// <summary>
    /// Exercises the full two-save lifecycle: create → first save → modify → second save,
    /// verifying that each save produces the correct state and that the item can be
    /// re-fetched (simulating the list refresh that both extensions trigger).
    /// </summary>
    [Fact]
    public async Task Lifecycle_TwoSaves_EachProducesCorrectState()
    {
        const string id = "LIFECYCLE-SAVE-001";

        // Create via API
        var createRequest = new
        {
            id,
            title = "Two-save test",
            section = "mvp-app",
            priority = "high",
            estimate = "2-4 hours",
            description = new[] { "Original description" },
            technicalDetails = new[] { "Original detail" },
            implementationTasks = new[]
            {
                new { task = "Step 1", done = false },
                new { task = "Step 2", done = false },
                new { task = "Step 3", done = false }
            }
        };
        var createResponse = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/todo", UriKind.Relative), createRequest).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        _createdIds.Add(id);

        // First edit cycle: open → edit → save
        var get1 = await _client.GetAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative)).ConfigureAwait(true);
        var item1 = await get1.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(item1);

        var md1 = SerializeToMarkdown(item1);
        var editedMd1 = md1
            .Replace("- [ ] Step 1", "- [x] Step 1")
            .Replace("Original description", "After first save");

        var update1 = ParseMarkdownToUpdateBody(editedMd1);
        var put1 = await _client.PutAsJsonAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative), update1).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, put1.StatusCode);

        // Verify after first save (simulates TodoSaved → list refresh)
        var verify1 = await _client.GetAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative)).ConfigureAwait(true);
        var after1 = await verify1.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(after1);
        Assert.NotNull(after1.Description);
        Assert.Contains("After first save", after1.Description);

        // Second edit cycle: re-open → edit → save
        var md2 = SerializeToMarkdown(after1);
        var editedMd2 = md2
            .Replace("- [ ] Step 2", "- [x] Step 2")
            .Replace("After first save", "After second save")
            .Replace("Original detail", "Updated detail");

        var update2 = ParseMarkdownToUpdateBody(editedMd2);
        var put2 = await _client.PutAsJsonAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative), update2).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, put2.StatusCode);

        // Verify after second save
        var verify2 = await _client.GetAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative)).ConfigureAwait(true);
        var after2 = await verify2.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(after2);
        Assert.NotNull(after2.Description);
        Assert.Contains("After second save", after2.Description);
        Assert.NotNull(after2.TechnicalDetails);
        Assert.Contains("Updated detail", after2.TechnicalDetails);
    }

    /// <summary>
    /// Verifies that deleting a TODO after the lifecycle completes removes it from the list,
    /// ensuring clean teardown and validating the DELETE endpoint used in cleanup.
    /// </summary>
    [Fact]
    public async Task Lifecycle_DeleteRemovesFromList()
    {
        const string id = "LIFECYCLE-DEL-001";

        var createRequest = new
        {
            id,
            title = "Delete lifecycle test",
            section = "mvp-support",
            priority = "low"
        };
        await _client.PostAsJsonAsync(
            new Uri("/mcpserver/todo", UriKind.Relative), createRequest).ConfigureAwait(true);
        _createdIds.Add(id);

        // Verify it exists
        var getResp = await _client.GetAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        // Delete (mimics cleanup; also tests that post-delete refresh works)
        var delResp = await _client.DeleteAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, delResp.StatusCode);
        _createdIds.Remove(id); // already deleted

        // Verify it's gone from list
        var listResp = await _client.GetAsync(
            new Uri($"/mcpserver/todo?id={id}", UriKind.Relative)).ConfigureAwait(true);
        var list = await listResp.Content.ReadFromJsonAsync<QueryResult>().ConfigureAwait(true);
        Assert.NotNull(list);
        Assert.Empty(list.Items);
    }

    /// <summary>
    /// Verifies that priority and section fields survive the full markdown round-trip:
    /// create → GET → serialize → edit priority/section → parse → PUT → verify.
    /// This test catches the bug where FromMarkdown silently dropped priority/section.
    /// </summary>
    [Fact]
    public async Task Lifecycle_PrioritySectionSurviveMarkdownRoundTrip()
    {
        const string id = "LIFECYCLE-PRIO-001";

        // Create with high priority in mvp-app
        var createRequest = new
        {
            id,
            title = "Priority round-trip test",
            section = "mvp-app",
            priority = "high",
            description = new[] { "Test priority persistence" }
        };
        var createResponse = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/todo", UriKind.Relative), createRequest).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        _createdIds.Add(id);

        // GET and serialize to markdown
        var getResponse = await _client.GetAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative)).ConfigureAwait(true);
        var item = await getResponse.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.Equal("high", item.Priority);
        Assert.Equal("mvp-app", item.Section);

        var markdown = SerializeToMarkdown(item);
        Assert.Contains("priority: high", markdown);
        Assert.Contains("section: mvp-app", markdown);

        // Simulate user editing priority and section in the markdown
        var editedMarkdown = markdown
            .Replace("priority: high", "priority: low")
            .Replace("section: mvp-app", "section: mvp-support");

        // Parse back — priority and section must survive
        var updateBody = ParseMarkdownToUpdateBody(editedMarkdown);
        Assert.Equal("low", updateBody.Priority);
        Assert.Equal("mvp-support", updateBody.Section);

        // PUT the update
        var putResponse = await _client.PutAsJsonAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative), updateBody).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        // Verify the change persisted
        var verifyResponse = await _client.GetAsync(
            new Uri($"/mcpserver/todo/{id}", UriKind.Relative)).ConfigureAwait(true);
        var updated = await verifyResponse.Content.ReadFromJsonAsync<FlatItem>().ConfigureAwait(true);
        Assert.NotNull(updated);
        Assert.Equal("low", updated.Priority);
        Assert.Equal("mvp-support", updated.Section);
    }

    #region Markdown helpers (mirrors VS extension TodoMarkdown + VSCode todoMarkdown.ts)

    /// <summary>
    /// Serializes a FlatItem to the YAML front matter + markdown body format
    /// used by both VS and VSCode extensions. Mirrors TodoMarkdown.ToMarkdown / todoToMarkdown.
    /// </summary>
    private static string SerializeToMarkdown(FlatItem item)
    {
        var fm = new List<string> { "---" };
        fm.Add($"id: {item.Id}");
        fm.Add($"section: {item.Section ?? ""}");
        fm.Add($"priority: {item.Priority ?? ""}");
        if (item.Done) fm.Add("done: true");
        if (!string.IsNullOrEmpty(item.Estimate)) fm.Add($"estimate: {item.Estimate}");
        if (!string.IsNullOrEmpty(item.Note)) fm.Add($"note: {item.Note}");
        if (!string.IsNullOrEmpty(item.CompletedDate)) fm.Add($"completed: {item.CompletedDate}");
        if (!string.IsNullOrEmpty(item.DoneSummary)) fm.Add($"done-summary: {item.DoneSummary}");
        if (!string.IsNullOrEmpty(item.Remaining)) fm.Add($"remaining: {item.Remaining}");
        if (item.DependsOn?.Length > 0)
        {
            fm.Add("depends-on:");
            foreach (var d in item.DependsOn) fm.Add($"  - {d}");
        }
        if (item.FunctionalRequirements?.Length > 0)
        {
            fm.Add("functional-requirements:");
            foreach (var fr in item.FunctionalRequirements) fm.Add($"  - {fr}");
        }
        if (item.TechnicalRequirements?.Length > 0)
        {
            fm.Add("technical-requirements:");
            foreach (var tr in item.TechnicalRequirements) fm.Add($"  - {tr}");
        }
        fm.Add("---");

        var body = new List<string> { "" };
        body.Add($"# {item.Title ?? ""}");
        body.Add("");
        if (item.Description?.Length > 0)
        {
            body.AddRange(item.Description);
            body.Add("");
        }
        if (item.TechnicalDetails?.Length > 0)
        {
            body.Add("## Technical Details");
            body.Add("");
            foreach (var d in item.TechnicalDetails) body.Add($"- {d}");
            body.Add("");
        }
        if (item.ImplementationTasks?.Length > 0)
        {
            body.Add("## Implementation Tasks");
            body.Add("");
            foreach (var t in item.ImplementationTasks)
                body.Add($"- [{(t.Done ? 'x' : ' ')}] {t.Task}");
            body.Add("");
        }

        return string.Join("\n", fm) + string.Join("\n", body).TrimEnd();
    }

    /// <summary>
    /// Parses edited markdown back into an update body.
    /// Mirrors TodoMarkdown.FromMarkdown / markdownToUpdateBody.
    /// </summary>
    private static UpdateBody ParseMarkdownToUpdateBody(string markdown)
    {
        var body = new UpdateBody();
        var lines = markdown.Split('\n');

        // Split front matter from body
        int fmStart = -1, fmEnd = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                if (fmStart < 0) fmStart = i;
                else { fmEnd = i; break; }
            }
        }

        // Parse front matter
        if (fmStart >= 0 && fmEnd >= 0)
        {
            string? currentListKey = null;
            List<string>? currentList = null;
            for (int i = fmStart + 1; i < fmEnd; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();

                // Check for list item
                if (trimmed.StartsWith("- ") && currentListKey != null && currentList != null)
                {
                    currentList.Add(trimmed[2..].Trim());
                    continue;
                }

                // Flush any pending list
                if (currentListKey != null && currentList != null)
                {
                    AssignList(body, currentListKey, currentList);
                    currentListKey = null;
                    currentList = null;
                }

                var colon = trimmed.IndexOf(':');
                if (colon <= 0) continue;
                var key = trimmed[..colon].Trim().ToLowerInvariant();
                var value = trimmed[(colon + 1)..].Trim();

                // Skip id — not part of update body
                if (key == "id") continue;

                // Check for list start
                if (value == "" || value == "[]")
                {
                    currentListKey = key;
                    currentList = [];
                    continue;
                }

                switch (key)
                {
                    case "priority": body.Priority = value; break;
                    case "section": body.Section = value; break;
                    case "estimate": body.Estimate = value; break;
                    case "note": body.Note = value; break;
                    case "done": body.Done = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase); break;
                    case "completed": body.CompletedDate = value; break;
                    case "done-summary": body.DoneSummary = value; break;
                    case "remaining": body.Remaining = value; break;
                }
            }

            // Flush final list
            if (currentListKey != null && currentList != null)
                AssignList(body, currentListKey, currentList);
        }

        // Parse body
        var bodyLines = fmEnd >= 0 ? lines.Skip(fmEnd + 1).ToArray() : lines;
        var description = new List<string>();
        var technicalDetails = new List<string>();
        var tasks = new List<TaskItem>();
        var currentSection = "description";

        foreach (var line in bodyLines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("# ") && !trimmed.StartsWith("## "))
            {
                body.Title = trimmed[2..].Trim();
                currentSection = "description";
                continue;
            }

            if (trimmed.StartsWith("## "))
            {
                var heading = trimmed[3..].Trim().ToUpperInvariant();
                if (heading.Contains("TECHNICAL")) currentSection = "technical-details";
                else if (heading.Contains("IMPLEMENTATION") || heading.Contains("TASK")) currentSection = "implementation-tasks";
                else currentSection = "description";
                continue;
            }

            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            switch (currentSection)
            {
                case "technical-details":
                    var bulletTd = System.Text.RegularExpressions.Regex.Match(trimmed, @"^-\s+(.+)$");
                    technicalDetails.Add(bulletTd.Success ? bulletTd.Groups[1].Value : trimmed);
                    break;
                case "implementation-tasks":
                    var taskMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"^-\s*\[([ xX])\]\s+(.+)$");
                    if (taskMatch.Success)
                        tasks.Add(new TaskItem { Task = taskMatch.Groups[2].Value, Done = taskMatch.Groups[1].Value.Equals("x", StringComparison.OrdinalIgnoreCase) });
                    break;
                default:
                    description.Add(trimmed);
                    break;
            }
        }

        if (description.Count > 0) body.Description = [.. description];
        if (technicalDetails.Count > 0) body.TechnicalDetails = [.. technicalDetails];
        if (tasks.Count > 0) body.ImplementationTasks = [.. tasks];

        return body;
    }

    private static void AssignList(UpdateBody body, string key, List<string> items)
    {
        switch (key)
        {
            case "depends-on": body.DependsOn = [.. items]; break;
            case "functional-requirements": body.FunctionalRequirements = [.. items]; break;
            case "technical-requirements": body.TechnicalRequirements = [.. items]; break;
        }
    }

    #endregion

    #region Test DTOs

    private sealed record QueryResult(FlatItem[] Items, int TotalCount);
    private sealed record MutationResult(bool Success, string? Error);

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
        string[]? TechnicalRequirements,
        TaskItem[]? ImplementationTasks);

    private sealed class TaskItem
    {
        public string Task { get; set; } = "";
        public bool Done { get; set; }
    }

    private sealed class UpdateBody
    {
        public string? Title { get; set; }
        public string? Priority { get; set; }
        public string? Section { get; set; }
        public bool? Done { get; set; }
        public string? Estimate { get; set; }
        public string[]? Description { get; set; }
        public string[]? TechnicalDetails { get; set; }
        public TaskItem[]? ImplementationTasks { get; set; }
        public string? Note { get; set; }
        public string? CompletedDate { get; set; }
        public string? DoneSummary { get; set; }
        public string? Remaining { get; set; }
        public string[]? DependsOn { get; set; }
        public string[]? FunctionalRequirements { get; set; }
        public string[]? TechnicalRequirements { get; set; }
    }

    #endregion

    #region Test Factory

    /// <summary>WebApplicationFactory that seeds a minimal TODO.yaml for lifecycle tests.</summary>
    public sealed class LifecycleWebFactory : WebApplicationFactory<McpApiEntryPoint>, IDisposable
    {
        private readonly string _tempDir = Path.Combine(
            Path.GetTempPath(), "mcp-lifecycle-tests-" + Guid.NewGuid().ToString("N")[..8]);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var projectDir = Path.Combine(_tempDir, "docs", "Project");
            var databasePath = Path.Combine(_tempDir, "mcp.db");
            Directory.CreateDirectory(projectDir);
            File.WriteAllText(Path.Combine(projectDir, "TODO.yaml"), SeedYaml);

            builder.UseEnvironment("Test");
            builder.UseContentRoot(ResolveContentRoot());
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

        private static string ResolveContentRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                var solutionPath = Path.Combine(current.FullName, "McpServer.sln");
                if (File.Exists(solutionPath))
                    return Path.Combine(current.FullName, "src", "McpServer.Support.Mcp");

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the solution root for lifecycle integration tests.");
        }

        private new void Dispose()
        {
            base.Dispose();
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private const string SeedYaml = """
            mvp-app:
              high-priority:
                - id: SEED-TODO-001
                  title: Seed item for lifecycle tests
                  done: false
                  description:
                    - Seed description
            mvp-support:
              low-priority: []
            """;
    }

    #endregion
}
