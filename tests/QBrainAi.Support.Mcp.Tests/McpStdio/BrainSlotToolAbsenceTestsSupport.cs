using System.Reflection;
using System.Text.Json;
using ModelContextProtocol.Server;
using QBrainAi.Tests.Shared;

namespace QBrainAi.Support.Mcp.Tests.McpStdio;

/// <summary>
/// FR-MCP-129; FR-MCP-130: the helpers that both halves of BrainSlotToolAbsenceTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-129; FR-MCP-130; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from BrainSlotToolAbsenceTests (BrainSlotToolPrefix) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class BrainSlotToolAbsenceTestsSupport
{
    internal const string BrainSlotToolPrefix = "brain_slot";
}
