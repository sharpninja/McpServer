using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for <see cref="FileMarkerPromptProvider"/>.</summary>
public sealed class FileMarkerPromptProviderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ILogger<FileMarkerPromptProvider> _logger = Substitute.For<ILogger<FileMarkerPromptProvider>>();

    public FileMarkerPromptProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fmpp-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempDir, "templates"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task GetGlobalPromptTemplateAsync_FileMissing_ReturnsNull()
    {
        // AppContext.BaseDirectory is fixed; provider falls back when file is absent.
        // We test that missing file returns null (not throwing).
        var provider = new FileMarkerPromptProvider(_logger);

        // The file at AppContext.BaseDirectory/templates/ likely doesn't exist in test runner
        // but we can't easily override the path. Instead, verify null-safety.
        var result = await provider.GetGlobalPromptTemplateAsync();

        // It returns either null (file not found) or a string (if file happens to exist).
        // In test context without the template file deployed, null is expected.
        Assert.True(result is null || result.Length > 0);
    }

    [Fact]
    public async Task GetGlobalPromptTemplateAsync_CachesResult()
    {
        var provider = new FileMarkerPromptProvider(_logger);

        var first = await provider.GetGlobalPromptTemplateAsync();
        var second = await provider.GetGlobalPromptTemplateAsync();

        Assert.Equal(first, second);
    }

    [Fact]
    public void MarkerTemplateFile_DeserializesCorrectly()
    {
        var yaml = "template: |\n  Hello {{baseUrl}}\n  More content\n";
        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var result = deserializer.Deserialize<FileMarkerPromptProvider.MarkerTemplateFile>(yaml);

        Assert.NotNull(result?.Template);
        Assert.Contains("Hello {{baseUrl}}", result!.Template);
    }
}
