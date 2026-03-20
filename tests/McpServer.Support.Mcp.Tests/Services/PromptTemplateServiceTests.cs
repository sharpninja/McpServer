using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

public sealed class PromptTemplateServiceTests : IDisposable
{
    private readonly string _tempFilePath;
    private readonly PromptTemplateService _sut;

    public PromptTemplateServiceTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"prompt-template-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(
            _tempFilePath,
            """
            templates:
              alpha-template:
                title: Alpha Release
                category: system
                description: Handles beta rollout
                content: "Alpha {{value}}"
              gamma-template:
                title: Gamma Review
                category: system
                description: Handles production validation
                content: "Gamma {{value}}"
            """);

        var renderer = new PromptTemplateRenderer(NullLogger<PromptTemplateRenderer>.Instance);
        _sut = new PromptTemplateService(
            global::Microsoft.Extensions.Options.Options.Create(new TemplateStorageOptions { FilePath = _tempFilePath }),
            renderer,
            NullLogger<PromptTemplateService>.Instance);
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (File.Exists(_tempFilePath))
            File.Delete(_tempFilePath);
    }

    [Fact]
    public async Task QueryAsync_WithBooleanKeyword_CanMatchAcrossFields()
    {
        var result = await _sut.QueryAsync(keyword: "alpha && beta").ConfigureAwait(true);

        var template = Assert.Single(result.Items);
        Assert.Equal("alpha-template", template.Id);
    }

    [Fact]
    public async Task QueryAsync_WithQuotedBooleanKeyword_MatchesExactPhrase()
    {
        var result = await _sut.QueryAsync(keyword: "\"Alpha Release\" && !gamma").ConfigureAwait(true);

        var template = Assert.Single(result.Items);
        Assert.Equal("alpha-template", template.Id);
    }
}
