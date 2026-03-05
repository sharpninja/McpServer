using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for <see cref="FileMarkerPromptProvider"/>.</summary>
public sealed class FileMarkerPromptProviderTests
{
    private readonly IPromptTemplateService _templateService = Substitute.For<IPromptTemplateService>();
    private readonly ILogger<FileMarkerPromptProvider> _logger = Substitute.For<ILogger<FileMarkerPromptProvider>>();

    [Fact]
    public async Task GetGlobalPromptTemplateAsync_TemplateNotFound_ReturnsNull()
    {
        _templateService.GetByIdAsync(FileMarkerPromptProvider.TemplateId, Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var provider = new FileMarkerPromptProvider(_templateService, _logger);

        var result = await provider.GetGlobalPromptTemplateAsync();

        Assert.Null(result);
    }

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

    [Fact]
    public async Task GetGlobalPromptTemplateAsync_CachesResult()
    {
        _templateService.GetByIdAsync(FileMarkerPromptProvider.TemplateId, Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var provider = new FileMarkerPromptProvider(_templateService, _logger);

        var first = await provider.GetGlobalPromptTemplateAsync();
        var second = await provider.GetGlobalPromptTemplateAsync();

        Assert.Equal(first, second);
        // Should only call the service once due to caching
        await _templateService.Received(1).GetByIdAsync(FileMarkerPromptProvider.TemplateId, Arg.Any<CancellationToken>());
    }
}
