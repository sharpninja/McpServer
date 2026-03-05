using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for <see cref="TodoPromptProvider"/>.</summary>
public sealed class TodoPromptProviderTests
{
    private readonly IPromptTemplateService _templateService = Substitute.For<IPromptTemplateService>();
    private readonly ILogger<TodoPromptProvider> _logger = Substitute.For<ILogger<TodoPromptProvider>>();

    [Fact]
    public async Task GetStatusPromptAsync_TemplateInStore_ReturnsStoreContent()
    {
        var customContent = "Custom status prompt for {id}";
        _templateService.GetByIdAsync(TodoPromptProvider.StatusPromptId, Arg.Any<CancellationToken>())
            .Returns(new PromptTemplate { Id = TodoPromptProvider.StatusPromptId, Title = "test", Category = "system", Content = customContent });

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetStatusPromptAsync();

        Assert.Equal(customContent, result);
    }

    [Fact]
    public async Task GetStatusPromptAsync_TemplateMissing_ReturnsFallback()
    {
        _templateService.GetByIdAsync(TodoPromptProvider.StatusPromptId, Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetStatusPromptAsync();

        Assert.Equal(TodoPromptDefaults.StatusPrompt, result);
    }

    [Fact]
    public async Task GetImplementPromptAsync_TemplateInStore_ReturnsStoreContent()
    {
        var customContent = "Custom implement prompt for {id}: {title}";
        _templateService.GetByIdAsync(TodoPromptProvider.ImplementPromptId, Arg.Any<CancellationToken>())
            .Returns(new PromptTemplate { Id = TodoPromptProvider.ImplementPromptId, Title = "test", Category = "system", Content = customContent });

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetImplementPromptAsync();

        Assert.Equal(customContent, result);
    }

    [Fact]
    public async Task GetImplementPromptAsync_TemplateMissing_ReturnsFallback()
    {
        _templateService.GetByIdAsync(TodoPromptProvider.ImplementPromptId, Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetImplementPromptAsync();

        Assert.Equal(TodoPromptDefaults.ImplementPrompt, result);
    }

    [Fact]
    public async Task GetPlanPromptAsync_TemplateInStore_ReturnsStoreContent()
    {
        var customContent = "Custom plan prompt for {id}";
        _templateService.GetByIdAsync(TodoPromptProvider.PlanPromptId, Arg.Any<CancellationToken>())
            .Returns(new PromptTemplate { Id = TodoPromptProvider.PlanPromptId, Title = "test", Category = "system", Content = customContent });

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetPlanPromptAsync();

        Assert.Equal(customContent, result);
    }

    [Fact]
    public async Task GetPlanPromptAsync_TemplateMissing_ReturnsFallback()
    {
        _templateService.GetByIdAsync(TodoPromptProvider.PlanPromptId, Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetPlanPromptAsync();

        Assert.Equal(TodoPromptDefaults.PlanPrompt, result);
    }

    [Fact]
    public async Task GetStatusPromptAsync_EmptyContent_ReturnsFallback()
    {
        _templateService.GetByIdAsync(TodoPromptProvider.StatusPromptId, Arg.Any<CancellationToken>())
            .Returns(new PromptTemplate { Id = TodoPromptProvider.StatusPromptId, Title = "test", Category = "system", Content = "   " });

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetStatusPromptAsync();

        Assert.Equal(TodoPromptDefaults.StatusPrompt, result);
    }

    [Fact]
    public async Task GetStatusPromptAsync_ServiceThrows_ReturnsFallback()
    {
        _templateService.GetByIdAsync(TodoPromptProvider.StatusPromptId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("disk error"));

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetStatusPromptAsync();

        Assert.Equal(TodoPromptDefaults.StatusPrompt, result);
    }
}
