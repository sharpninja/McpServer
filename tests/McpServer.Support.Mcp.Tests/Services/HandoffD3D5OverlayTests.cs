using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Overlay G3 D3: shipped <see cref="RequirementsDocumentService.GenerateDocumentAsync"/>,
/// <see cref="RequirementsDocumentService.GenerateAllAsync"/>, and
/// <see cref="RequirementsDocumentService.GenerateWikiAsync"/> against live Handoff family
/// entries in docs/Project. TEST-HANDOFF-006 / TEST-HANDOFF-007. Writes only to a temp export.
/// </summary>
public sealed class HandoffD3D5OverlayTests : IDisposable
{
    private readonly string _exportRoot = Path.Combine(
        Path.GetTempPath(),
        "mcp-g3-handoff-d3-" + Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_exportRoot))
            {
                foreach (var path in Directory.GetFiles(_exportRoot, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                }

                Directory.Delete(_exportRoot, recursive: true);
            }
        }
        catch
        {
            // best effort
        }
    }

    /// <summary>
    /// D3: GenerateDocumentAsync renders every live FR-HANDOFF / TR-HANDOFF / TEST-HANDOFF id
    /// that the shipped parser loaded from docs/Project.
    /// </summary>
    [Fact]
    public async Task D3_GenerateDocument_ShippedFileBackedService_RendersLiveHandoffFamily()
    {
        var service = CreateLiveService();
        var frIds = (await service.GetAllFrAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Select(item => item.Id)
            .Where(IsHandoffFr)
            .ToArray();
        var trIds = (await service.GetAllTrAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Select(item => item.Id)
            .Where(IsHandoffTr)
            .ToArray();
        var testIds = (await service.GetAllTestAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Select(item => item.Id)
            .Where(IsHandoffTest)
            .ToArray();

        Assert.NotEmpty(frIds);
        Assert.NotEmpty(trIds);
        Assert.NotEmpty(testIds);

        var functional = await service.GenerateDocumentAsync(RequirementsDocType.Functional, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var technical = await service.GenerateDocumentAsync(RequirementsDocType.Technical, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var testing = await service.GenerateDocumentAsync(RequirementsDocType.Testing, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var mapping = await service.GenerateDocumentAsync(RequirementsDocType.Mapping, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var matrix = await service.GenerateDocumentAsync(RequirementsDocType.Matrix, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("text/markdown", functional.MimeType);
        foreach (var id in frIds)
        {
            Assert.Contains(id, functional.Content, StringComparison.Ordinal);
            Assert.Contains(id, mapping.Content, StringComparison.Ordinal);
            Assert.Contains(id, matrix.Content, StringComparison.Ordinal);
        }

        foreach (var id in trIds)
        {
            Assert.Contains(id, technical.Content, StringComparison.Ordinal);
            Assert.Contains(id, matrix.Content, StringComparison.Ordinal);
        }

        foreach (var id in testIds)
        {
            Assert.Contains(id, testing.Content, StringComparison.Ordinal);
            Assert.Contains(id, matrix.Content, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// D3: GenerateAllAsync and GenerateWikiAsync write Handoff family ids into temp markdown/wiki
    /// exports using the shipped export writers. Does not mutate docs/Project.
    /// </summary>
    [Fact]
    public async Task D3_GenerateAllAndWiki_ShippedService_WritesHandoffFamilyToTempExport()
    {
        var service = CreateLiveService();
        var frIds = (await service.GetAllFrAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Select(item => item.Id)
            .Where(IsHandoffFr)
            .ToArray();
        Assert.NotEmpty(frIds);

        var markdownRoot = Path.Combine(_exportRoot, "markdown");
        var wikiRoot = Path.Combine(_exportRoot, "wiki");
        var generatedAt = new DateTimeOffset(2026, 9, 10, 2, 5, 0, TimeSpan.Zero);

        var markdown = await service.GenerateAllAsync(markdownRoot, generatedAt, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var wiki = await service.GenerateWikiAsync(wikiRoot, generatedAt, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(markdown.Success);
        Assert.True(wiki.Success);
        Assert.Contains(markdown.Files, file => file.RelativePath == "Functional-Requirements.md");
        Assert.Contains(wiki.Files, file => file.RelativePath.Replace('\\', '/') == "azure/Functional-Requirements.md");
        Assert.Contains(wiki.Files, file => file.RelativePath.Replace('\\', '/') == "github/Functional-Requirements.md");

        var markdownFunctional = await File.ReadAllTextAsync(
            Path.Combine(markdownRoot, "Functional-Requirements.md"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        var azureFunctional = await File.ReadAllTextAsync(
            Path.Combine(wikiRoot, "azure", "Functional-Requirements.md"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        var githubFunctional = await File.ReadAllTextAsync(
            Path.Combine(wikiRoot, "github", "Functional-Requirements.md"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        foreach (var id in frIds)
        {
            Assert.Contains(id, markdownFunctional, StringComparison.Ordinal);
            Assert.Contains(id, azureFunctional, StringComparison.Ordinal);
            Assert.Contains(id, githubFunctional, StringComparison.Ordinal);
        }
    }

    private static RequirementsDocumentService CreateLiveService()
    {
        var project = Path.Combine(FindRepoRoot(), "docs", "Project");
        var options = Microsoft.Extensions.Options.Options.Create(new RequirementsOptions
        {
            FunctionalRequirementsPath = Path.Combine(project, "Functional-Requirements.md"),
            TechnicalRequirementsPath = Path.Combine(project, "Technical-Requirements.md"),
            TestingRequirementsPath = Path.Combine(project, "Testing-Requirements.md"),
            MappingPath = Path.Combine(project, "TR-per-FR-Mapping.md"),
            MatrixPath = Path.Combine(project, "Requirements-Matrix.md"),
        });
        return new RequirementsDocumentService(options, NullLogger<RequirementsDocumentService>.Instance);
    }

    private static bool IsHandoffFr(string id) =>
        id.StartsWith("FR-HANDOFF-", StringComparison.Ordinal);

    private static bool IsHandoffTr(string id) =>
        id.StartsWith("TR-HANDOFF-", StringComparison.Ordinal);

    private static bool IsHandoffTest(string id) =>
        id.StartsWith("TEST-HANDOFF-", StringComparison.Ordinal);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "docs", "Project")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
