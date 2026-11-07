using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MsOptions = Microsoft.Extensions.Options.Options;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>TEST-HANDOFF-006: one canonical workspace path is pushed into every handoff scope.</summary>
/// <remarks>2026-11-07, TEST-HANDOFF-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/HandoffWorkspacePathsTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; only the namespace changed.</remarks>
public sealed class HandoffWorkspacePathsTests
{
    /// <summary>P1-8: relative and nested paths canonicalize to the same absolute value.</summary>
    /// <remarks>Integration: binds physical current-directory resolution (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Canonicalize_RelativeAndNested_MatchGetFullPath()
    {
        var relative = Path.Combine(".", "nested", "..", "workspace");
        var canonical = HandoffWorkspacePaths.Canonicalize(relative);
        Assert.Equal(ExpectedWorkspaceKey.Of(Path.GetFullPath(relative)), canonical);
        Assert.True(Path.IsPathRooted(canonical));
    }
}
