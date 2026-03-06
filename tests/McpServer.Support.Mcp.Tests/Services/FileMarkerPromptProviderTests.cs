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
/// to verify required-template enforcement and caching behavior.
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
    /// Verifies that marker template lookup is cached after the first successful load.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-050, TR-MCP-TPL-005.
    /// Test data: one template payload and two provider reads.
    /// This data is used to prove the provider avoids redundant template-service calls while returning stable content.
    /// </remarks>
    [Fact]
    public async Task GetGlobalPromptTemplateAsync_CachesResult()
    {
        var template = new PromptTemplate
        {
            Id = FileMarkerPromptProvider.TemplateId,
            Title = "Test",
            Category = "system",
            Content = "Cached Content",
            Engine = "handlebars",
        };
        _templateService.GetByIdAsync(FileMarkerPromptProvider.TemplateId, Arg.Any<CancellationToken>())
            .Returns(template);

        var provider = new FileMarkerPromptProvider(_templateService, _logger);

        var first = await provider.GetGlobalPromptTemplateAsync();
        var second = await provider.GetGlobalPromptTemplateAsync();

        Assert.Equal("Cached Content", first);
        Assert.Equal(first, second);
        // Should only call the service once due to caching
        await _templateService.Received(1).GetByIdAsync(FileMarkerPromptProvider.TemplateId, Arg.Any<CancellationToken>());
    }
}
