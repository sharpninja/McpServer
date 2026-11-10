using System.Reflection;
using System.Text.Json;
using ModelContextProtocol.Server;
using QBrainAi.Tests.Shared;
using static QBrainAi.Support.Mcp.Tests.McpStdio.BrainSlotToolAbsenceTestsSupport;

namespace QBrainAi.Support.Mcp.Tests.McpStdio;

/// <summary>
/// FR-MCP-129 and FR-MCP-130 agent-surface removal: QuadBrain brain-slot capabilities are
/// reachable only through the workspace-token REST controller and the OpenAI-compatible
/// chat-completions endpoint. No MCP tool on the Support.Mcp assembly may expose them,
/// and the durable STDIO tool contract must not advertise them.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-129; FR-MCP-130; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (StdioToolContract_ContainsNoBrainSlotTools) and the helpers only it uses (FindRepoRoot) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (BrainSlotToolPrefix) moved once into McpStdio/BrainSlotToolAbsenceTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class BrainSlotToolAbsenceTests
{
    /// <summary>
    /// Scans every <see cref="McpServerToolAttribute"/>-decorated method on the
    /// QBrainAi.Support.Mcp assembly by reflection (so a reappearance under any file or
    /// type name is still caught) and asserts no registered tool name starts with
    /// <c>brain_slot</c> and no declaring method is named after a brain slot.
    /// </summary>
    [Fact]
    public void SupportMcpAssembly_RegistersNoBrainSlotMcpTools()
    {
        var assembly = typeof(global::QBrainAi.Support.Mcp.McpStdio.FwhMcpTools).Assembly;

        var offenders = GetToolMethods(assembly)
            .Select(method => new
            {
                Method = method,
                ToolName = method.GetCustomAttribute<McpServerToolAttribute>()!.Name ?? method.Name,
            })
            .Where(candidate =>
                candidate.ToolName.StartsWith(BrainSlotToolPrefix, StringComparison.OrdinalIgnoreCase)
                || candidate.Method.Name.StartsWith("BrainSlot", StringComparison.Ordinal))
            .Select(candidate => $"{candidate.Method.DeclaringType?.FullName}.{candidate.Method.Name} => '{candidate.ToolName}'")
            .OrderBy(description => description, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "QuadBrain brain-slot MCP tools must not be registered on the Support.Mcp assembly. Found: "
                + string.Join("; ", offenders));
    }

    private static IEnumerable<MethodInfo> GetToolMethods(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(type => type is not null).Select(type => type!).ToArray();
        }

        return types.SelectMany(type => type.GetMethods(
                BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null);
    }
}
