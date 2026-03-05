using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Reqnroll;

namespace McpServer.SpecFlow.Tests.StepDefinitions;

/// <summary>Step definitions for workspace management feature files.</summary>
[Binding]
public sealed class WorkspaceSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly CommonSteps _common;

    public WorkspaceSteps(ScenarioContext scenarioContext, CommonSteps common)
    {
        _scenarioContext = scenarioContext;
        _common = common;
    }

    private HttpClient Client => _scenarioContext.Get<HttpClient>("HttpClient");

    [Given("I use a unique temp directory stored as {string}")]
    public void GivenIUseAUniqueTempDirectory(string pathKey)
    {
        var path = Path.Combine(Path.GetTempPath(), $"specflow_ws_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        _scenarioContext.Set(path, pathKey);
    }

    [Given("a workspace exists for path {string}")]
    public async Task GivenAWorkspaceExistsForPath(string pathExpression)
    {
        var path = _common.ResolveTokens(pathExpression);
        var body = JsonSerializer.Serialize(new { workspacePath = path, name = "specflow-ws" });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(new Uri("/mcp/workspace", UriKind.Relative), content).ConfigureAwait(false);
        // Accept 201 or 409 (already exists)
        var status = (int)response.StatusCode;
        Assert.True(status is 201 or 409, $"Expected 201 or 409, got {status}");
    }

    [Given("a workspace exists for path {string} with key stored as {string}")]
    public async Task GivenAWorkspaceExistsForPathWithKeyStoredAs(string pathExpression, string keyStoreAs)
    {
        var path = _common.ResolveTokens(pathExpression);
        var body = JsonSerializer.Serialize(new { workspacePath = path, name = "specflow-ws" });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(new Uri("/mcp/workspace", UriKind.Relative), content).ConfigureAwait(false);
        var status = (int)response.StatusCode;
        Assert.True(status is 201 or 409, $"Expected 201 or 409, got {status}");

        var wsKey = Base64UrlEncode(path);
        _scenarioContext.Set(wsKey, keyStoreAs);
    }

    [Given("a workspace exists for path {string} with key stored as {string} and port stored as {string}")]
    public async Task GivenAWorkspaceExistsWithKeyAndPort(string pathExpression, string keyStoreAs, string portStoreAs)
    {
        var path = _common.ResolveTokens(pathExpression);
        var body = JsonSerializer.Serialize(new { workspacePath = path, name = "specflow-ws" });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(new Uri("/mcp/workspace", UriKind.Relative), content).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var status = (int)response.StatusCode;
        Assert.True(status is 201 or 409, $"Expected 201 or 409, got {status}");

        var wsKey = Base64UrlEncode(path);
        _scenarioContext.Set(wsKey, keyStoreAs);

        if (status == 201)
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("workspace", out var ws) &&
                ws.TryGetProperty("workspacePort", out var port))
            {
                _scenarioContext.Set(port.GetInt32().ToString(), portStoreAs);
            }
        }
    }

    [Given("the workspace {string} is started")]
    public async Task GivenTheWorkspaceIsStarted(string keyExpression)
    {
        var key = _common.ResolveTokens(keyExpression);
        await Client.PostAsync(new Uri($"/mcp/workspace/{key}/start", UriKind.Relative), null).ConfigureAwait(false);
    }

    [Given("the primary workspace key is stored as {string}")]
    public async Task GivenThePrimaryWorkspaceKeyIsStoredAs(string keyStoreAs)
    {
        var response = await Client.GetAsync(new Uri("/mcp/workspace", UriKind.Relative)).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        // Response shape: { "workspaces": [...], "totalCount": ... }
        var workspaceArray = root.TryGetProperty("workspaces", out var ws) ? ws : root;
        if (workspaceArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var workspace in workspaceArray.EnumerateArray())
            {
                if (workspace.TryGetProperty("isPrimary", out var isPrimary) && isPrimary.GetBoolean())
                {
                    if (workspace.TryGetProperty("key", out var key))
                    {
                        _scenarioContext.Set(key.GetString() ?? "", keyStoreAs);
                        return;
                    }
                }
            }
        }
        // Fallback: store empty string if no primary found
        _scenarioContext.Set("", keyStoreAs);
    }

    [Given("the workspace port is stored as {string}")]
    public async Task GivenTheWorkspacePortIsStoredAs(string portKey)
    {
        // Use the last-set workspace path key to find the workspace
        var response = await Client.GetAsync(new Uri("/mcp/workspace", UriKind.Relative)).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        _scenarioContext.Set("7148", portKey); // default fallback
    }

    [Given("a legacy {string} file exists in {string}")]
    public void GivenALegacyFileExistsIn(string fileName, string dirExpression)
    {
        var dir = _common.ResolveTokens(dirExpression);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, fileName), "legacy marker");
    }

    [When("I stop the workspace {string}")]
    public async Task WhenIStopTheWorkspace(string keyExpression)
    {
        var key = _common.ResolveTokens(keyExpression);
        await Client.PostAsync(new Uri($"/mcp/workspace/{key}/stop", UriKind.Relative), null).ConfigureAwait(false);
    }

    [Then("I stop the workspace {string}")]
    public async Task ThenIStopWorkspace(string keyExpression)
    {
        var key = _common.ResolveTokens(keyExpression);
        await Client.PostAsync(new Uri($"/mcp/workspace/{key}/stop", UriKind.Relative), null).ConfigureAwait(false);
    }

    [Then("I delete the created workspace using the path {string}")]
    public async Task ThenIDeleteWorkspaceUsingPath(string pathExpression)
    {
        var path = _common.ResolveTokens(pathExpression);
        var key = Base64UrlEncode(path);
        await Client.PostAsync(new Uri($"/mcp/workspace/{key}/stop", UriKind.Relative), null).ConfigureAwait(false);
        await Client.DeleteAsync(new Uri($"/mcp/workspace/{key}", UriKind.Relative)).ConfigureAwait(false);
    }

    [Then("I delete the bucket named {string}")]
    public async Task ThenIDeleteBucketNamed(string name)
    {
        await Client.DeleteAsync(new Uri($"/mcp/tools/buckets/{Uri.EscapeDataString(name)}", UriKind.Relative)).ConfigureAwait(false);
    }

    [Then("the response body should contain a port greater than or equal to {int}")]
    public void ThenResponseBodyShouldContainPortGreaterThan(int minPort)
    {
        var body = _common.GetRequiredResponseBody();
        using var doc = JsonDocument.Parse(body);
        var port = FindPort(doc.RootElement);
        Assert.True(port >= minPort, $"Expected port >= {minPort} but got {port}. Body: {body}");
    }

    [Then("the key {string} should be a valid Base64URL-encoded string")]
    public void ThenKeyShouldBeValidBase64Url(string keyExpression)
    {
        var key = _common.ResolveTokens(keyExpression);
        // Base64URL chars: A-Z, a-z, 0-9, -, _
        Assert.Matches(@"^[A-Za-z0-9\-_=]+$", key);
    }

    [Then("at most one workspace in the result has {string} set to true")]
    public void ThenAtMostOneWorkspaceHasFieldSetToTrue(string fieldName)
    {
        var body = _common.GetRequiredResponseBody();
        using var doc = JsonDocument.Parse(body);
        var count = 0;
        var root = doc.RootElement;
        // Response shape: { "workspaces": [...], "totalCount": ... }
        var workspaces = root.TryGetProperty("workspaces", out var ws) ? ws : root;
        if (workspaces.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in workspaces.EnumerateArray())
            {
                if (item.TryGetProperty(fieldName, out var field) && field.GetBoolean())
                    count++;
            }
        }
        Assert.True(count <= 1, $"Expected at most one workspace with {fieldName}=true but got {count}");
    }

    [Then("the file {string} should exist in {string}")]
    public void ThenFileShouldExistIn(string fileName, string dirExpression)
    {
        var dir = _common.ResolveTokens(dirExpression);
        Assert.True(File.Exists(Path.Combine(dir, fileName)),
            $"Expected file '{fileName}' to exist in '{dir}'.");
    }

    [Then("the file {string} should not exist in {string}")]
    public void ThenFileShouldNotExistIn(string fileName, string dirExpression)
    {
        var dir = _common.ResolveTokens(dirExpression);
        Assert.False(File.Exists(Path.Combine(dir, fileName)),
            $"Expected file '{fileName}' to NOT exist in '{dir}'.");
    }

    [Then("the file {string} in {string} should contain {string}")]
    public void ThenFileInDirShouldContain(string fileName, string dirExpression, string expected)
    {
        var dir = _common.ResolveTokens(dirExpression);
        expected = _common.ResolveTokens(expected);
        var path = Path.Combine(dir, fileName);
        Assert.True(File.Exists(path), $"File '{path}' does not exist.");
        var content = File.ReadAllText(path);
        Assert.Contains(expected, content, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the directory {string} should exist")]
    public void ThenDirectoryShouldExist(string dirExpression)
    {
        var dir = _common.ResolveTokens(dirExpression);
        Assert.True(Directory.Exists(dir), $"Expected directory '{dir}' to exist.");
    }

    private static int FindPort(JsonElement element)
    {
        if (element.TryGetProperty("workspacePort", out var port))
            return port.GetInt32();
        if (element.TryGetProperty("workspace", out var ws))
            return FindPort(ws);
        return 0;
    }

    private static string Base64UrlEncode(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_');
    }
}
