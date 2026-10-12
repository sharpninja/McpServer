using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>Tests for <see cref="TodoPromptProvider"/>.</summary>
public sealed class TodoPromptProviderTests
{
    private readonly IPromptTemplateService _templateService = Substitute.For<IPromptTemplateService>();
    private readonly ILogger<TodoPromptProvider> _logger = Substitute.For<ILogger<TodoPromptProvider>>();

    [Fact]
    public async Task GetStatusPromptAsyncTemplateInStoreReturnsStoreContent()
    {
        var customContent = "Custom status prompt for {id}";
        _templateService.GetByIdAsync(TodoPromptProvider.StatusPromptId, Arg.Any<CancellationToken>())
            .Returns(new PromptTemplate { Id = TodoPromptProvider.StatusPromptId, Title = "test", Category = "system", Content = customContent });

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetStatusPromptAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(customContent, result);
    }

    [Fact]
    public async Task GetStatusPromptAsyncTemplateMissingReturnsFallback()
    {
        _templateService.GetByIdAsync(TodoPromptProvider.StatusPromptId, Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetStatusPromptAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TodoPromptDefaults.StatusPrompt, result);
    }

    [Fact]
    public async Task GetImplementPromptAsyncTemplateInStoreReturnsStoreContent()
    {
        var customContent = "Custom implement prompt for {id}: {title}";
        _templateService.GetByIdAsync(TodoPromptProvider.ImplementPromptId, Arg.Any<CancellationToken>())
            .Returns(new PromptTemplate { Id = TodoPromptProvider.ImplementPromptId, Title = "test", Category = "system", Content = customContent });

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetImplementPromptAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(customContent, result);
    }

    [Fact]
    public async Task GetImplementPromptAsyncTemplateMissingReturnsFallback()
    {
        _templateService.GetByIdAsync(TodoPromptProvider.ImplementPromptId, Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetImplementPromptAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TodoPromptDefaults.ImplementPrompt, result);
    }

    [Fact]
    public async Task GetPlanPromptAsyncTemplateInStoreReturnsStoreContent()
    {
        var customContent = "Custom plan prompt for {id}";
        _templateService.GetByIdAsync(TodoPromptProvider.PlanPromptId, Arg.Any<CancellationToken>())
            .Returns(new PromptTemplate { Id = TodoPromptProvider.PlanPromptId, Title = "test", Category = "system", Content = customContent });

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetPlanPromptAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(customContent, result);
    }

    [Fact]
    public async Task GetPlanPromptAsyncTemplateMissingReturnsFallback()
    {
        _templateService.GetByIdAsync(TodoPromptProvider.PlanPromptId, Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetPlanPromptAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TodoPromptDefaults.PlanPrompt, result);
    }

    [Fact]
    public async Task GetStatusPromptAsyncEmptyContentReturnsFallback()
    {
        _templateService.GetByIdAsync(TodoPromptProvider.StatusPromptId, Arg.Any<CancellationToken>())
            .Returns(new PromptTemplate { Id = TodoPromptProvider.StatusPromptId, Title = "test", Category = "system", Content = "   " });

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetStatusPromptAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TodoPromptDefaults.StatusPrompt, result);
    }

    [Fact]
    public async Task GetStatusPromptAsyncServiceThrowsReturnsFallback()
    {
        _templateService.GetByIdAsync(TodoPromptProvider.StatusPromptId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("disk error"));

        var provider = new TodoPromptProvider(_templateService, _logger);
        var result = await provider.GetStatusPromptAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TodoPromptDefaults.StatusPrompt, result);
    }
}
