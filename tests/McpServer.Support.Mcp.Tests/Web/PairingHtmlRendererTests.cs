using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Web;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Web;

/// <summary>Tests for <see cref="PairingHtmlRenderer"/>.</summary>
public sealed class PairingHtmlRendererTests
{
    private readonly IPromptTemplateService _templateService = Substitute.For<IPromptTemplateService>();
    private readonly ILogger<PairingHtmlRenderer> _logger = Substitute.For<ILogger<PairingHtmlRenderer>>();

    [Fact]
    public async Task RenderLoginPageAsync_TemplateInStore_SubstitutesErrorBanner()
    {
        var template = "<html>{errorBanner}<form></form></html>";
        _templateService.GetByIdAsync(PairingHtmlRenderer.LoginPageId, Arg.Any<CancellationToken>())
            .Returns(new PromptTemplate { Id = PairingHtmlRenderer.LoginPageId, Title = "test", Category = "system", Content = template });

        var renderer = new PairingHtmlRenderer(_templateService, _logger);
        var result = await renderer.RenderLoginPageAsync(error: false);

        Assert.Equal("<html><form></form></html>", result);
    }

    [Fact]
    public async Task RenderLoginPageAsync_WithError_InsertsErrorBanner()
    {
        var template = "<html>{errorBanner}<form></form></html>";
        _templateService.GetByIdAsync(PairingHtmlRenderer.LoginPageId, Arg.Any<CancellationToken>())
            .Returns(new PromptTemplate { Id = PairingHtmlRenderer.LoginPageId, Title = "test", Category = "system", Content = template });

        var renderer = new PairingHtmlRenderer(_templateService, _logger);
        var result = await renderer.RenderLoginPageAsync(error: true);

        Assert.Contains("Invalid username or password", result);
    }

    [Fact]
    public async Task RenderLoginPageAsync_TemplateMissing_ReturnsFallback()
    {
        _templateService.GetByIdAsync(PairingHtmlRenderer.LoginPageId, Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var renderer = new PairingHtmlRenderer(_templateService, _logger);
        var result = await renderer.RenderLoginPageAsync();

        Assert.Contains("MCP Server", result);
        Assert.Contains("<form", result);
    }

    [Fact]
    public async Task RenderKeyPageAsync_TemplateInStore_SubstitutesTokens()
    {
        var template = "<html><span>{apiKey}</span><a href=\"{serverUrl}/mcp\">{serverUrl}</a></html>";
        _templateService.GetByIdAsync(PairingHtmlRenderer.KeyPageId, Arg.Any<CancellationToken>())
            .Returns(new PromptTemplate { Id = PairingHtmlRenderer.KeyPageId, Title = "test", Category = "system", Content = template });

        var renderer = new PairingHtmlRenderer(_templateService, _logger);
        var result = await renderer.RenderKeyPageAsync("my-secret-key", "http://localhost:7147");

        Assert.Contains("my-secret-key", result);
        Assert.Contains("http://localhost:7147/mcp", result);
    }

    [Fact]
    public async Task RenderKeyPageAsync_TemplateMissing_ReturnsFallback()
    {
        _templateService.GetByIdAsync(PairingHtmlRenderer.KeyPageId, Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var renderer = new PairingHtmlRenderer(_templateService, _logger);
        var result = await renderer.RenderKeyPageAsync("test-key", "http://localhost:7147");

        Assert.Contains("test-key", result);
        Assert.Contains("http://localhost:7147", result);
    }

    [Fact]
    public async Task RenderNotConfiguredPageAsync_TemplateInStore_ReturnsCustomContent()
    {
        var template = "<html><h1>Custom Not Configured</h1></html>";
        _templateService.GetByIdAsync(PairingHtmlRenderer.NotConfiguredPageId, Arg.Any<CancellationToken>())
            .Returns(new PromptTemplate { Id = PairingHtmlRenderer.NotConfiguredPageId, Title = "test", Category = "system", Content = template });

        var renderer = new PairingHtmlRenderer(_templateService, _logger);
        var result = await renderer.RenderNotConfiguredPageAsync();

        Assert.Equal(template, result);
    }

    [Fact]
    public async Task RenderNotConfiguredPageAsync_TemplateMissing_ReturnsFallback()
    {
        _templateService.GetByIdAsync(PairingHtmlRenderer.NotConfiguredPageId, Arg.Any<CancellationToken>())
            .Returns((PromptTemplate?)null);

        var renderer = new PairingHtmlRenderer(_templateService, _logger);
        var result = await renderer.RenderNotConfiguredPageAsync();

        Assert.Contains("Pairing Not Configured", result);
    }

    [Fact]
    public async Task RenderLoginPageAsync_ServiceThrows_ReturnsFallback()
    {
        _templateService.GetByIdAsync(PairingHtmlRenderer.LoginPageId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("disk error"));

        var renderer = new PairingHtmlRenderer(_templateService, _logger);
        var result = await renderer.RenderLoginPageAsync();

        Assert.Contains("MCP Server", result);
    }
}
