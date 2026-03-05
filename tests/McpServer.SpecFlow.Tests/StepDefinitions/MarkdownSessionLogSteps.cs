using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using Reqnroll;

namespace McpServer.SpecFlow.Tests.StepDefinitions;

/// <summary>Step definitions for Markdown session log ingestion feature.</summary>
[Binding]
public sealed class MarkdownSessionLogSteps
{
    private readonly ScenarioContext _scenarioContext;

    public MarkdownSessionLogSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    private string MarkdownFilePath
    {
        get => _scenarioContext.Get<string>("MarkdownFilePath");
        set => _scenarioContext.Set(value, "MarkdownFilePath");
    }

    private string MarkdownContent
    {
        get => _scenarioContext.Get<string>("MarkdownContent");
        set => _scenarioContext.Set(value, "MarkdownContent");
    }

    private UnifiedSessionLogDto? ParseResult
    {
        get => _scenarioContext.TryGetValue("ParseResult", out UnifiedSessionLogDto? r) ? r : null;
        set => _scenarioContext.Set(value!, "ParseResult");
    }

    private string? NormalizedText
    {
        get => _scenarioContext.TryGetValue("NormalizedText", out string? t) ? t : null;
        set => _scenarioContext.Set(value!, "NormalizedText");
    }

    [Given("a Markdown file with header {string}")]
    public void GivenAMarkdownFileWithHeader(string header)
    {
        var content = header + "\n\n";
        MarkdownContent = content;
        var tempPath = Path.Combine(Path.GetTempPath(), $"specflow-md-{Guid.NewGuid():N}.md");
        File.WriteAllText(tempPath, content);
        MarkdownFilePath = tempPath;
        _scenarioContext.Set(tempPath, "MarkdownTempFile");
    }

    [Given("the file contains section {string} with content {string}")]
    public void GivenFileContainsSectionWithContent(string sectionName, string sectionContent)
    {
        var current = MarkdownContent;
        current += $"\n## {sectionName}\n\n{sectionContent}\n\n";
        MarkdownContent = current;
        File.WriteAllText(MarkdownFilePath, current);
    }

    [Given(@"the file contains a ""(.*)"" subsection with prompt ""(.*)""")]
    public void GivenFileContainsRequestSubsection(string subsectionType, string prompt)
    {
        var current = MarkdownContent;
        current += $"\n### {subsectionType}\n\n{prompt}\n\n";
        MarkdownContent = current;
        File.WriteAllText(MarkdownFilePath, current);
    }

    [When("I call MarkdownSessionLogParser.TryParse on the file")]
    public void WhenICallTryParseOnFile()
    {
        var content = File.ReadAllText(MarkdownFilePath);
        ParseResult = MarkdownSessionLogParser.TryParse(content, MarkdownFilePath);
    }

    [When("I call NormalizeToStructuredText on the result")]
    public void WhenICallNormalizeToStructuredText()
    {
        var content = File.ReadAllText(MarkdownFilePath);
        NormalizedText = MarkdownSessionLogParser.NormalizeToStructuredText(content);
    }

    [Then("the result should not be null")]
    public void ThenResultShouldNotBeNull()
    {
        Assert.NotNull(ParseResult);
    }

    [Then("the result should be null")]
    public void ThenResultShouldBeNull()
    {
        Assert.Null(ParseResult);
    }

    [Then("the result title should be {string}")]
    public void ThenResultTitleShouldBe(string expected)
    {
        Assert.NotNull(ParseResult);
        Assert.Equal(expected, ParseResult.Title, StringComparer.OrdinalIgnoreCase);
    }

    [Then("the result model should be {string}")]
    public void ThenResultModelShouldBe(string expected)
    {
        Assert.NotNull(ParseResult);
        Assert.Equal(expected, ParseResult.Model, StringComparer.OrdinalIgnoreCase);
    }

    [Then("the result status should be {string}")]
    public void ThenResultStatusShouldBe(string expected)
    {
        Assert.NotNull(ParseResult);
        Assert.Equal(expected, ParseResult.Status?.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    [Then("the result should contain at least one entry")]
    public void ThenResultShouldContainAtLeastOneEntry()
    {
        Assert.NotNull(ParseResult);
        Assert.NotEmpty(ParseResult.Entries ?? []);
    }

    [Then("the result should contain at least {int} entries")]
    public void ThenResultShouldContainAtLeastNEntries(int minCount)
    {
        Assert.NotNull(ParseResult);
        var count = (ParseResult.Entries ?? []).Count;
        Assert.True(count >= minCount,
            $"Expected at least {minCount} entries but got {count}.");
    }

    [Then("the normalized text should not be empty")]
    public void ThenNormalizedTextShouldNotBeEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(NormalizedText),
            "NormalizeToStructuredText returned empty text.");
    }

    [Then("the normalized text should contain {string}")]
    public void ThenNormalizedTextShouldContain(string expected)
    {
        Assert.Contains(expected, NormalizedText ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [AfterScenario]
    public void CleanupTempFile()
    {
        if (_scenarioContext.TryGetValue("MarkdownTempFile", out string tempFile) && File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }
    }
}
