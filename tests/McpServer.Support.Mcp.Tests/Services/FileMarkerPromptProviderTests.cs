using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Tests marker prompt loading behavior for <see cref="FileMarkerPromptProvider"/>.
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-050, TR-MCP-TPL-005.
/// Test data uses substituted template-service responses (missing template, concrete template, repeated reads)
/// to verify required-template enforcement and fresh reload behavior for marker regeneration.
/// </remarks>
public sealed class FileMarkerPromptProviderTests
{
    private readonly IPromptTemplateService _templateService = Substitute.For<IPromptTemplateService>();
    private readonly ILogger<FileMarkerPromptProvider> _logger = Substitute.For<ILogger<FileMarkerPromptProvider>>();

    /// <summary>
    /// Verifies that a missing marker template causes an <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-050, TR-MCP-TPL-005.
    /// Test data: template service returns <see langword="null"/> for <c>default-marker-prompt</c>.
    /// This data is used to validate fail-fast behavior when required external template content is unavailable.
    /// </remarks>
    [Fact]
    public async Task GetGlobalPromptTemplateAsync_TemplateNotFound_Throws()
    {
        _templateService.GetByIdAsync(FileMarkerPromptProvider.TemplateId, Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var provider = new FileMarkerPromptProvider(_templateService, _logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetGlobalPromptTemplateAsync());
    }

    /// <summary>
    /// Verifies that template content is returned when the configured marker template exists.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-050, TR-MCP-TPL-005.
    /// Test data: a concrete <see cref="PromptTemplate"/> with handlebars content <c>Hello {{baseUrl}}</c>.
    /// This data is used to confirm pass-through of persisted template text used by marker rendering.
    /// </remarks>
    [Fact]
    public async Task GetGlobalPromptTemplateAsync_TemplateFound_ReturnsContent()
    {
        var template = new PromptTemplate
        {
            Id = FileMarkerPromptProvider.TemplateId,
            Title = "Test",
            Category = "system",
            Content = "Hello {{baseUrl}}",
            Engine = "handlebars",
        };
        _templateService.GetByIdAsync(FileMarkerPromptProvider.TemplateId, Arg.Any<CancellationToken>())
            .Returns(template);

        var provider = new FileMarkerPromptProvider(_templateService, _logger);

        var result = await provider.GetGlobalPromptTemplateAsync();

        Assert.NotNull(result);
        Assert.Contains("Hello {{baseUrl}}", result);
    }

    /// <summary>
    /// Verifies that marker template lookup reloads the current template content on each call.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-050, TR-MCP-TPL-005.
    /// Test data: two successive template payloads returned by the substituted template service.
    /// This data is used to prove marker regeneration observes live prompt-template updates instead of reusing stale cached text.
    /// </remarks>
    [Fact]
    public async Task GetGlobalPromptTemplateAsync_ReloadsUpdatedTemplate()
    {
        _templateService.GetByIdAsync(FileMarkerPromptProvider.TemplateId, Arg.Any<CancellationToken>())
            .Returns(
                new PromptTemplate
                {
                    Id = FileMarkerPromptProvider.TemplateId,
                    Title = "Test",
                    Category = "system",
                    Content = "First Content",
                    Engine = "handlebars",
                },
                new PromptTemplate
                {
                    Id = FileMarkerPromptProvider.TemplateId,
                    Title = "Test",
                    Category = "system",
                    Content = "Updated Content",
                    Engine = "handlebars",
                });

        var provider = new FileMarkerPromptProvider(_templateService, _logger);

        var first = await provider.GetGlobalPromptTemplateAsync();
        var second = await provider.GetGlobalPromptTemplateAsync();

        Assert.Equal("First Content", first);
        Assert.Equal("Updated Content", second);
        await _templateService.Received(2).GetByIdAsync(FileMarkerPromptProvider.TemplateId, Arg.Any<CancellationToken>());
    }
}
