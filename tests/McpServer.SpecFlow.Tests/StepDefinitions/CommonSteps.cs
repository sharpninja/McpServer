using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Reqnroll;

namespace McpServer.SpecFlow.Tests.StepDefinitions;

/// <summary>Common step definitions reused across all feature files.</summary>
[Binding]
public sealed class CommonSteps
{
    private readonly ScenarioContext _scenarioContext;

    public CommonSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private HttpClient Client => _scenarioContext.Get<HttpClient>("HttpClient");

    private HttpResponseMessage? LastResponse
    {
        get => _scenarioContext.TryGetValue("LastResponse", out HttpResponseMessage r) ? r : null;
        set => _scenarioContext.Set(value!, "LastResponse");
    }

    private string? LastResponseBody
    {
        get => _scenarioContext.TryGetValue("LastResponseBody", out string b) ? b : null;
        set => _scenarioContext.Set(value!, "LastResponseBody");
    }

    [Given("the MCP server is running")]
    public async Task GivenTheMcpServerIsRunning()
    {
        var response = await Client.GetAsync(new Uri("/health", UriKind.Relative)).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    [When("I send a GET request to {string}")]
    public async Task WhenISendGetRequest(string path)
    {
        path = ResolveTokens(path);
        var response = await Client.GetAsync(new Uri(path, UriKind.Relative)).ConfigureAwait(false);
        LastResponse = response;
        LastResponseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    [When("I send a GET request to {string} without an API key")]
    public async Task WhenISendGetRequestWithoutApiKey(string path)
    {
        path = ResolveTokens(path);
        var response = await Client.GetAsync(new Uri(path, UriKind.Relative)).ConfigureAwait(false);
        LastResponse = response;
        LastResponseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    [When("I send a GET request to {string} without Accept header")]
    public async Task WhenISendGetRequestWithoutAcceptHeader(string path)
    {
        path = ResolveTokens(path);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        var response = await Client.SendAsync(request).ConfigureAwait(false);
        LastResponse = response;
        LastResponseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    [When("I send a DELETE request to {string}")]
    public async Task WhenISendDeleteRequest(string path)
    {
        path = ResolveTokens(path);
        var response = await Client.DeleteAsync(new Uri(path, UriKind.Relative)).ConfigureAwait(false);
        LastResponse = response;
        LastResponseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    [When(@"I POST to ""(.*)"" with body:")]
    public async Task WhenIPostWithBody(string path, string body)
    {
        path = ResolveTokens(path);
        body = ResolveTokens(body);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(new Uri(path, UriKind.Relative), content).ConfigureAwait(false);
        LastResponse = response;
        LastResponseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    [When(@"I POST to ""(.*)"" with empty body")]
    public async Task WhenIPostWithEmptyBody(string path)
    {
        path = ResolveTokens(path);
        var response = await Client.PostAsync(new Uri(path, UriKind.Relative), null).ConfigureAwait(false);
        LastResponse = response;
        LastResponseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    [When(@"I POST to ""(.*)"" without an API key with body:")]
    public async Task WhenIPostWithoutApiKeyWithBody(string path, string body)
    {
        path = ResolveTokens(path);
        body = ResolveTokens(body);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(new Uri(path, UriKind.Relative), content).ConfigureAwait(false);
        LastResponse = response;
        LastResponseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    [When(@"I PUT to ""(.*)"" with body:")]
    public async Task WhenIPutWithBody(string path, string body)
    {
        path = ResolveTokens(path);
        body = ResolveTokens(body);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PutAsync(new Uri(path, UriKind.Relative), content).ConfigureAwait(false);
        LastResponse = response;
        LastResponseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    [Then("the response status code should be {int}")]
    public void ThenResponseStatusShouldBe(int expected)
    {
        var response = GetRequiredResponse();
        Assert.Equal((System.Net.HttpStatusCode)expected, response.StatusCode);
    }

    [Then("the response status code is {int} or {int}")]
    public void ThenResponseStatusIsEither(int status1, int status2)
    {
        var response = GetRequiredResponse();
        var actual = (int)response.StatusCode;
        Assert.True(actual == status1 || actual == status2,
            $"Expected {status1} or {status2} but got {actual}. Body: {LastResponseBody}");
    }

    [Then("the response status code is {int} or {int} or {int}")]
    public void ThenResponseStatusIsEitherThree(int status1, int status2, int status3)
    {
        var response = GetRequiredResponse();
        var actual = (int)response.StatusCode;
        Assert.True(actual == status1 || actual == status2 || actual == status3,
            $"Expected {status1}, {status2}, or {status3} but got {actual}. Body: {LastResponseBody}");
    }

    [Then("the response status code is not {int}")]
    public void ThenResponseStatusIsNot(int unexpected)
    {
        var response = GetRequiredResponse();
        Assert.NotEqual((System.Net.HttpStatusCode)unexpected, response.StatusCode);
    }

    [Then("the response body should contain {string}")]
    public void ThenResponseBodyShouldContain(string expected)
    {
        expected = ResolveTokens(expected);
        var body = GetRequiredResponseBody();
        Assert.Contains(expected, body, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the response body is a JSON array")]
    public void ThenResponseBodyIsJsonArray()
    {
        var body = GetRequiredResponseBody();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Then("the response body is valid JSON")]
    public void ThenResponseBodyIsValidJson()
    {
        var body = GetRequiredResponseBody();
        Assert.True(IsValidJson(body), $"Response body is not valid JSON: {body}");
    }

    [Then("the response content type should contain {string}")]
    public void ThenResponseContentTypeContains(string expected)
    {
        var response = GetRequiredResponse();
        Assert.Contains(expected, response.Content.Headers.ContentType?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Then("the response body should contain a {string} field")]
    public void ThenResponseBodyShouldContainField(string expected)
    {
        ThenResponseBodyShouldContain(expected);
    }

    [Then("the response status code is {int} or {int} or {int} or {int}")]
    public void ThenResponseStatusIsEitherFour(int s1, int s2, int s3, int s4)
    {
        var response = GetRequiredResponse();
        var actual = (int)response.StatusCode;
        Assert.True(actual == s1 || actual == s2 || actual == s3 || actual == s4,
            $"Expected {s1}, {s2}, {s3}, or {s4} but got {actual}. Body: {LastResponseBody}");
    }

    /// <summary>Resolves {token} placeholders in strings from the scenario context.</summary>
    internal string ResolveTokens(string input)
    {
        if (!input.Contains('{'))
            return input;

        foreach (var key in _scenarioContext.Keys)
        {
            var token = "{" + key + "}";
            if (input.Contains(token) && _scenarioContext.TryGetValue(key, out object? value))
            {
                input = input.Replace(token, value?.ToString() ?? "");
            }
        }
        return input;
    }

    internal HttpResponseMessage GetRequiredResponse() =>
        LastResponse ?? throw new InvalidOperationException("No HTTP response has been made yet.");

    internal string GetRequiredResponseBody() =>
        LastResponseBody ?? throw new InvalidOperationException("No HTTP response body has been captured yet.");

    private static bool IsValidJson(string text)
    {
        try
        {
            JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
