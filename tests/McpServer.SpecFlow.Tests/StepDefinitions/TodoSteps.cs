using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Reqnroll;

namespace McpServer.SpecFlow.Tests.StepDefinitions;

/// <summary>Step definitions for TODO management feature.</summary>
[Binding]
public sealed class TodoSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly CommonSteps _common;

    public TodoSteps(ScenarioContext scenarioContext, CommonSteps common)
    {
        _scenarioContext = scenarioContext;
        _common = common;
    }

    private HttpClient Client => _scenarioContext.Get<HttpClient>("HttpClient");

    [Given("a TODO item exists with title {string}")]
    public async Task GivenATodoItemExistsWithTitle(string title)
    {
        var uniqueId = $"SF-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var body = JsonSerializer.Serialize(new
        {
            id = uniqueId,
            title,
            section = "mvp-support",
            priority = "medium",
            done = false
        });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(new Uri("/mcp/todo", UriKind.Relative), content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    [Given("a TODO item exists with title {string} and id stored as {string}")]
    public async Task GivenATodoItemExistsWithTitleAndIdStoredAs(string title, string idKey)
    {
        var uniqueId = $"SF-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var body = JsonSerializer.Serialize(new
        {
            id = uniqueId,
            title,
            section = "mvp-support",
            priority = "medium",
            done = false
        });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(new Uri("/mcp/todo", UriKind.Relative), content).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        _scenarioContext.Set(uniqueId, idKey);
    }

    [Given("a TODO item with existing FR IDs {string}")]
    public async Task GivenATodoItemWithExistingFrIds(string existingIds)
    {
        var uniqueId = $"SF-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var body = JsonSerializer.Serialize(new
        {
            id = uniqueId,
            title = "Requirements Base Item",
            section = "mvp-support",
            priority = "high",
            done = false,
            functionalRequirements = existingIds.Split(',').Select(s => s.Trim()).ToList()
        });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(new Uri("/mcp/todo", UriKind.Relative), content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        _scenarioContext.Set(uniqueId, "reqBaseId");
    }

    [Then("every returned item should have section {string}")]
    public void ThenEveryItemShouldHaveSection(string expectedSection)
    {
        var body = _common.GetRequiredResponseBody();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.GetProperty("items");
        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("section", out var section))
            {
                Assert.Equal(expectedSection, section.GetString(), StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    [Then("every returned item should have priority {string}")]
    public void ThenEveryItemShouldHavePriority(string expectedPriority)
    {
        var body = _common.GetRequiredResponseBody();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.GetProperty("items");
        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("priority", out var priority))
            {
                Assert.Equal(expectedPriority, priority.GetString(), StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    [Then(@"every returned item should have done ""(.*)""")]
    public void ThenEveryItemShouldHaveDone(string expectedDone)
    {
        var expected = bool.Parse(expectedDone);
        var body = _common.GetRequiredResponseBody();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.GetProperty("items");
        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("done", out var done))
            {
                Assert.Equal(expected, done.GetBoolean());
            }
        }
    }

    [Then("every returned item title or description should contain {string}")]
    public void ThenEveryItemTitleOrDescriptionContains(string keyword)
    {
        var body = _common.GetRequiredResponseBody();
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("items", out var items) ||
            items.ValueKind == JsonValueKind.Null ||
            items.ValueKind != JsonValueKind.Array)
        {
            return; // No items to validate
        }
        foreach (var item in items.EnumerateArray())
        {
            var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            var description = item.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.Array
                ? string.Join(" ", d.EnumerateArray().Select(x => x.GetString() ?? ""))
                : "";
            var combined = title + " " + description;
            Assert.Contains(keyword, combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Then("the TODO should contain {string} exactly once")]
    public async Task ThenTodoShouldContainIdExactlyOnce(string expectedId)
    {
        var itemId = _scenarioContext.Get<string>("reqBaseId");
        var response = await Client.GetAsync(new Uri($"/mcp/todo/{itemId}", UriKind.Relative)).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var count = CountOccurrences(body, expectedId);
        Assert.Equal(1, count);
    }

    [Then("the TODO should contain {string}")]
    public async Task ThenTodoShouldContainId(string expectedId)
    {
        var itemId = _scenarioContext.Get<string>("reqBaseId");
        var response = await Client.GetAsync(new Uri($"/mcp/todo/{itemId}", UriKind.Relative)).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.Contains(expectedId, body, StringComparison.OrdinalIgnoreCase);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var idx = 0;
        while ((idx = text.IndexOf(pattern, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }
}
