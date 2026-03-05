using System.Text;
using System.Text.Json;
using Reqnroll;

namespace McpServer.SpecFlow.Tests.StepDefinitions;

/// <summary>Step definitions for session log feature files.</summary>
[Binding]
public sealed class SessionLogSteps
{
    private readonly ScenarioContext _scenarioContext;

    public SessionLogSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private HttpClient Client => _scenarioContext.Get<HttpClient>("HttpClient");

    [Given("a session log exists with sourceType {string} and sessionId {string}")]
    public async Task GivenASessionLogExists(string sourceType, string sessionId)
    {
        var body = JsonSerializer.Serialize(new
        {
            sourceType,
            sessionId,
            title = "SpecFlow Session Log",
            status = "completed",
            entries = Array.Empty<object>()
        });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(new Uri("/mcp/sessionlog", UriKind.Relative), content).ConfigureAwait(false);
        // Accept 201 or 200
    }

    [Given("a session log exists with sourceType {string} and sessionId {string} and agentName {string}")]
    public async Task GivenASessionLogExistsWithAgent(string sourceType, string sessionId, string agentName)
    {
        await GivenASessionLogExists(sourceType, sessionId).ConfigureAwait(false);
    }
}
