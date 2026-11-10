using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MsOptions = Microsoft.Extensions.Options.Options;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TEST-HANDOFF-002 through TEST-HANDOFF-007: real-service tests for source security,
/// extraction, validation, modes, TODO persistence, replay, and audit storage.
/// Uses an in-memory SQLite workspace and a mocked one-shot extractor.
/// </summary>
/// <remarks>2026-11-07, TEST-HANDOFF-002; TEST-HANDOFF-007; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/HandoffIngestionServiceTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class HandoffIngestionServiceTestsBase in Services/HandoffIngestionServiceTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class HandoffIngestionServiceTests : HandoffIngestionServiceTestsBase, IDisposable
{
    /// <summary>TEST-HANDOFF-002: reparse-point escapes fail closed.</summary>
    /// <remarks>Integration: creates a real directory link and checks physical containment (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task IngestAsync_ReparseEscape_IsRejected()
    {
        var outsideRoot = Path.Combine(Path.GetTempPath(), "handoff-outside-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outsideRoot);
        var outsideFile = Path.Combine(outsideRoot, "secret.md");
        File.WriteAllText(outsideFile, "secret-outside");
        var linkPath = Path.Combine(_workspace, "escape.md");
        try
        {
            File.CreateSymbolicLink(linkPath, outsideFile);
            var todo = Substitute.For<ITodoService>();
            var sut = CreateService(SuccessfulExtractor(ValidDraftJson("MCP-HANDOFFDEMO-016")), todo);

            var result = await sut.IngestAsync(new HandoffIngestionRequest
            {
                SourceKind = HandoffSourceKind.Path,
                Path = "escape.md",
            }, TestContext.Current.CancellationToken);

            Assert.Contains(result.Diagnostics, item => item.Code == "source_reparse" || item.Code == "source_external");
            await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(linkPath))
                File.Delete(linkPath);
            if (Directory.Exists(outsideRoot))
                Directory.Delete(outsideRoot, recursive: true);
        }
    }
}
