using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using Reqnroll;

namespace McpServer.SpecFlow.Tests.StepDefinitions;

/// <summary>Step definitions for auth/pairing feature files.</summary>
[Binding]
public sealed class AuthSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly CommonSteps _common;

    public AuthSteps(ScenarioContext scenarioContext, CommonSteps common)
    {
        _scenarioContext = scenarioContext;
        _common = common;
    }

    private HttpClient Client => _scenarioContext.Get<HttpClient>("HttpClient");

    [When("I POST to {string} with form fields:")]
    public async Task WhenIPostWithFormFields(string path, Table table)
    {
        path = _common.ResolveTokens(path);
        var formData = new Dictionary<string, string>();
        foreach (var row in table.Rows)
        {
            formData[row["Field"]] = row["Value"];
        }

        using var content = new FormUrlEncodedContent(formData);
        var response = await Client.PostAsync(new Uri(path, UriKind.Relative), content).ConfigureAwait(false);
        _scenarioContext.Set(response, "LastResponse");
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        _scenarioContext.Set(body, "LastResponseBody");
    }
}
