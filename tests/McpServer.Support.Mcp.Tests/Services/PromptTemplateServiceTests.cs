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
        var result = await _sut.QueryAsync(keyword: "alpha && beta", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var template = Assert.Single(result.Items);
        Assert.Equal("alpha-template", template.Id);
    }

    [Fact]
    public async Task QueryAsync_WithQuotedBooleanKeyword_MatchesExactPhrase()
    {
        var result = await _sut.QueryAsync(keyword: "\"Alpha Release\" && !gamma", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var template = Assert.Single(result.Items);
        Assert.Equal("alpha-template", template.Id);
    }

    [Fact]
    public async Task CaptureFileAsync_WhenFileExists_ReturnsRawSnapshot()
    {
        var snapshot = await _sut.CaptureFileAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(snapshot.Exists);
        Assert.Contains("alpha-template", snapshot.Content, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.ContentSha256));
    }

    [Fact]
    public async Task RestoreFileAsync_WhenSnapshotExists_RestoresPriorContent()
    {
        var snapshot = await _sut.CaptureFileAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var update = await _sut.UpdateAsync("alpha-template", new PromptTemplateUpdateRequest { Title = "Changed" }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.True(update.Success);
        var after = await _sut.CaptureFileAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await _sut.RestoreFileAsync(snapshot, after.ContentSha256, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var restored = await _sut.GetByIdAsync("alpha-template", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("Alpha Release", restored?.Title);
    }

    [Fact]
    public async Task RestoreFileAsync_WhenSnapshotDidNotExist_DeletesCreatedFile()
    {
        File.Delete(_tempFilePath);
        var snapshot = await _sut.CaptureFileAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var create = await _sut.CreateAsync(new PromptTemplateCreateRequest
        {
            Id = "new-template",
            Title = "New",
            Category = "system",
            Content = "New {{value}}",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(create.Success);
        var after = await _sut.CaptureFileAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await _sut.RestoreFileAsync(snapshot, after.ContentSha256, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(File.Exists(_tempFilePath));
    }

    [Fact]
    public async Task RestoreFileAsync_WhenFileChangedAfterMutation_RefusesOverwrite()
    {
        var snapshot = await _sut.CaptureFileAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var update = await _sut.UpdateAsync("alpha-template", new PromptTemplateUpdateRequest { Title = "Changed" }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.True(update.Success);
        var after = await _sut.CaptureFileAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        File.WriteAllText(_tempFilePath, "human edit");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.RestoreFileAsync(snapshot, after.ContentSha256, cancellationToken: TestContext.Current.CancellationToken))
            .ConfigureAwait(true);

        Assert.Contains("changed after transactional write", ex.Message, StringComparison.Ordinal);
        Assert.Equal("human edit", File.ReadAllText(_tempFilePath));
    }
}
