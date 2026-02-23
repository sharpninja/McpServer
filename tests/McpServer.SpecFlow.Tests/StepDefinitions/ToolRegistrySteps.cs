using System.Text;
using System.Text.Json;
using Reqnroll;

namespace McpServer.SpecFlow.Tests.StepDefinitions;

/// <summary>Step definitions for tool registry feature files.</summary>
[Binding]
public sealed class ToolRegistrySteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly CommonSteps _common;

    public ToolRegistrySteps(ScenarioContext scenarioContext, CommonSteps common)
    {
        _scenarioContext = scenarioContext;
        _common = common;
    }

    private HttpClient Client => _scenarioContext.Get<HttpClient>("HttpClient");

    [Given("a tool exists with name {string} and tags {string}")]
    public async Task GivenAToolExistsWithNameAndTags(string name, string tags)
    {
        var tagList = tags.Split(',').Select(t => t.Trim()).ToList();
        var body = JsonSerializer.Serialize(new { name, description = $"SpecFlow tool {name}", tags = tagList });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(new Uri("/mcp/tools", UriKind.Relative), content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    [Given("a tool exists with name {string} and tags {string} and id stored as {string}")]
    public async Task GivenAToolExistsWithNameTagsAndId(string name, string tags, string idKey)
    {
        var tagList = tags.Split(',').Select(t => t.Trim()).ToList();
        var body = JsonSerializer.Serialize(new { name, description = $"SpecFlow tool {name}", tags = tagList });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(new Uri("/mcp/tools", UriKind.Relative), content).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(responseBody);
        // Response shape: { "success": true, "tool": { "id": 1, ... } }
        if (doc.RootElement.TryGetProperty("tool", out var tool) && tool.TryGetProperty("id", out var toolId))
        {
            _scenarioContext.Set(toolId.GetInt64().ToString(), idKey);
        }
    }

    [Given("a bucket exists with name {string}")]
    public async Task GivenABucketExistsWithName(string name)
    {
        var body = JsonSerializer.Serialize(new
        {
            name,
            owner = "sharpninja",
            repo = "McpServer",
            branch = "main"
        });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(new Uri("/mcp/tools/buckets", UriKind.Relative), content).ConfigureAwait(false);
        var status = (int)response.StatusCode;
        Assert.True(status is 201 or 409, $"Expected 201 or 409, got {status}");
    }

    [Then("I delete the tool named {string}")]
    public async Task ThenIDeleteToolNamed(string name)
    {
        // Find tool by searching, then delete by id
        var searchResponse = await Client.GetAsync(
            new Uri($"/mcp/tools/search?keyword={Uri.EscapeDataString(name)}", UriKind.Relative)).ConfigureAwait(false);
        var searchBody = await searchResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!searchResponse.IsSuccessStatusCode) return;

        using var doc = JsonDocument.Parse(searchBody);
        // Handle both array and paginated object responses
        var items = doc.RootElement.ValueKind == JsonValueKind.Array
            ? doc.RootElement
            : (doc.RootElement.TryGetProperty("items", out var arr) ? arr : doc.RootElement);

        if (items.ValueKind != JsonValueKind.Array) return;

        foreach (var tool in items.EnumerateArray())
        {
            var toolName = tool.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (string.Equals(toolName, name, StringComparison.OrdinalIgnoreCase) &&
                tool.TryGetProperty("id", out var id))
            {
                await Client.DeleteAsync(new Uri($"/mcp/tools/{id.GetInt64()}", UriKind.Relative)).ConfigureAwait(false);
                break;
            }
        }
    }
}
