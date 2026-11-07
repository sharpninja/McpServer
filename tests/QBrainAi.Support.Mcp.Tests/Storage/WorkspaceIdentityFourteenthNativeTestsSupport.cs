using System.Diagnostics;
using System.Reflection;
using QBrainAi.Client;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Database;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-018: the helpers that both halves of WorkspaceIdentityFourteenthNativeTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-018; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from WorkspaceIdentityFourteenthNativeTests (ResolveCounter and ReadCounter) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class WorkspaceIdentityFourteenthNativeTestsSupport
{
    internal static FieldInfo ResolveCounter(string name)
    {
        var field = typeof(WorkspaceIdentityPath).GetField(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        return field!;
    }

    internal static int ReadCounter(FieldInfo field) =>
        Assert.IsType<int>(field.GetValue(null));
}
