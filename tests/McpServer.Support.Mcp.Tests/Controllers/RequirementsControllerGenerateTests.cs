using System.Text.Json;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-BUGTRIAGE-025: Requirements wiki generation returns structured errors.
/// </summary>
public sealed class RequirementsControllerGenerateTests
{
    /// <summary>Invalid wiki configuration returns a structured 400 instead of an opaque 500.</summary>
    [Fact]
    public async Task GenerateAsync_WikiConfigFailure_ReturnsStructuredBadRequest()
    {
        var requirements = Substitute.For<IRequirementsDocumentService>();
        requirements.GenerateWikiAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RequirementsDocumentExportResult>(new InvalidOperationException("invalid docs/wiki.yaml")));
        var controller = CreateController(requirements);

        var result = await controller.GenerateAsync("all", "wiki", CancellationToken.None).ConfigureAwait(true);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequest.Value);
        Assert.Contains("invalid docs/wiki.yaml", json, StringComparison.Ordinal);
        Assert.Contains("\"stage\":\"config load\"", json, StringComparison.Ordinal);
    }

    /// <summary>Transaction-gated export conflicts return a structured 409.</summary>
    [Fact]
    public async Task GenerateAsync_WikiConflictFailure_ReturnsStructuredConflict()
    {
        var requirements = Substitute.For<IRequirementsDocumentService>();
        requirements.GenerateWikiAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RequirementsDocumentExportResult>(new RequirementsConflictException("transaction coordinator degraded")));
        var controller = CreateController(requirements);

        var result = await controller.GenerateAsync("all", "wiki", CancellationToken.None).ConfigureAwait(true);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var json = JsonSerializer.Serialize(conflict.Value);
        Assert.Contains("transaction coordinator degraded", json, StringComparison.Ordinal);
        Assert.Contains("\"stage\":\"transaction\"", json, StringComparison.Ordinal);
    }

    /// <summary>ZIP assembly failures identify the ZIP stage instead of escaping as opaque 500s.</summary>
    [Fact]
    public async Task GenerateAsync_WikiZipAssemblyFailure_ReturnsStructuredConflict()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"mcp-wiki-generate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            var missingFile = Path.Combine(tempRoot, "missing.md");
            var requirements = Substitute.For<IRequirementsDocumentService>();
            requirements.GenerateWikiAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new RequirementsDocumentExportResult
                {
                    Success = true,
                    Format = "wiki",
                    DocType = "all",
                    GeneratedAtUtc = DateTimeOffset.UtcNow,
                    OutputRoot = tempRoot,
                    Files =
                    [
                        new RequirementsDocumentExportFile
                        {
                            RelativePath = "missing.md",
                            FullPath = missingFile,
                            ContentType = "text/markdown",
                            LastModifiedUtc = DateTimeOffset.UtcNow
                        }
                    ]
                }));
            var controller = CreateController(requirements);

            var result = await controller.GenerateAsync("all", "wiki", CancellationToken.None).ConfigureAwait(true);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var json = JsonSerializer.Serialize(conflict.Value);
            Assert.Contains("Generated wiki file was not found", json, StringComparison.Ordinal);
            Assert.Contains("\"stage\":\"zip assembly\"", json, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static RequirementsController CreateController(IRequirementsDocumentService requirements)
        => new(
            requirements,
            MsOptions.Options.Create(new RequirementsOptions()),
            new WorkspaceContext { WorkspacePath = @"F:\GitHub\McpServer" },
            Substitute.For<ITodoExecutionService>(),
            NullLogger<RequirementsController>.Instance,
            transactionCoordinator: null,
            transactionOptions: null);
}
