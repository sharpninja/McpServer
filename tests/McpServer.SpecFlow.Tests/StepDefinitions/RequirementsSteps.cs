using McpServer.Support.Mcp.Services;
using Reqnroll;

namespace McpServer.SpecFlow.Tests.StepDefinitions;

/// <summary>Step definitions for AI requirements analysis feature.</summary>
[Binding]
public sealed class RequirementsSteps
{
    private readonly ScenarioContext _scenarioContext;

    public RequirementsSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private string CopilotResponseBody
    {
        get => _scenarioContext.Get<string>("CopilotResponseBody");
        set => _scenarioContext.Set(value, "CopilotResponseBody");
    }

    private RequirementsService CreateService()
    {
        return new RequirementsService(
            null!,
            null!,
            null!,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RequirementsService>.Instance);
    }

    private List<string> ExtractedIds
    {
        get => _scenarioContext.TryGetValue("ExtractedIds", out List<string>? ids) ? ids! : [];
        set => _scenarioContext.Set(value, "ExtractedIds");
    }

    [Given("a Copilot response containing:")]
    public void GivenACopilotResponseContaining(string responseBody)
    {
        CopilotResponseBody = responseBody;
    }

    [When("I extract requirement IDs from the JSON block response")]
    public void WhenIExtractRequirementIdsFromJsonBlock()
    {
        var (frIds, trIds) = CreateService().ExtractRequirementIds(CopilotResponseBody);
        var all = frIds.Concat(trIds).ToList();
        ExtractedIds = all;
        _scenarioContext.Set(frIds, "ExtractedFrIds");
        _scenarioContext.Set(trIds, "ExtractedTrIds");
    }

    [When("I extract requirement IDs using regex fallback")]
    public void WhenIExtractRequirementIdsUsingRegexFallback()
    {
        var (frIds, trIds) = CreateService().ExtractRequirementIds(CopilotResponseBody);
        var all = frIds.Concat(trIds).ToList();
        ExtractedIds = all;
        _scenarioContext.Set(frIds, "ExtractedFrIds");
        _scenarioContext.Set(trIds, "ExtractedTrIds");
    }

    [When("the discovered IDs are merged into the TODO")]
    public void WhenTheDiscoveredIdsAreMergedIntoTheTodo()
    {
        // Simulate merging: the existing TODO's FR/TR IDs are the seed.
        // The Copilot response is already stored, extraction happened above.
        var (frIds, trIds) = CreateService().ExtractRequirementIds(CopilotResponseBody);
        ExtractedIds = frIds.Concat(trIds).ToList();
    }

    [Then("the extracted IDs should contain {string}")]
    public void ThenExtractedIdsShouldContain(string expected)
    {
        Assert.Contains(expected, ExtractedIds, StringComparer.OrdinalIgnoreCase);
    }

    [Then("the extracted IDs should be distinct")]
    public void ThenExtractedIdsShouldBeDistinct()
    {
        var duplicates = ExtractedIds
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        Assert.Empty(duplicates);
    }

    [Then("the extracted IDs should be empty")]
    public void ThenExtractedIdsShouldBeEmpty()
    {
        Assert.Empty(ExtractedIds);
    }
}
