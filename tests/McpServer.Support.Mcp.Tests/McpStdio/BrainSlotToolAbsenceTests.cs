using System.Reflection;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.Tests.McpStdio;

/// <summary>
/// FR-MCP-129 and FR-MCP-130 agent-surface removal: QuadBrain brain-slot capabilities are
/// reachable only through the workspace-token REST controller and the OpenAI-compatible
/// chat-completions endpoint. No MCP tool on the Support.Mcp assembly may expose them,
/// and the durable STDIO tool contract must not advertise them.
/// </summary>
public sealed class BrainSlotToolAbsenceTests
{
    private const string BrainSlotToolPrefix = "brain_slot";

    /// <summary>
    /// Scans every <see cref="McpServerToolAttribute"/>-decorated method on the
    /// McpServer.Support.Mcp assembly by reflection (so a reappearance under any file or
    /// type name is still caught) and asserts no registered tool name starts with
    /// <c>brain_slot</c> and no declaring method is named after a brain slot.
    /// </summary>
    [Fact]
    public void SupportMcpAssembly_RegistersNoBrainSlotMcpTools()
    {
        var assembly = typeof(global::McpServer.Support.Mcp.McpStdio.FwhMcpTools).Assembly;

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

    /// <summary>
    /// Asserts the durable STDIO tool contract artifact advertises no brain-slot tool,
    /// so agent tooling generated from the manifest cannot discover the surface.
    /// </summary>
    [Fact]
    public void StdioToolContract_ContainsNoBrainSlotTools()
    {
        var contractPath = Path.Combine(FindRepoRoot(), "docs", "stdio-tool-contract.json");
        using var document = JsonDocument.Parse(File.ReadAllText(contractPath));

        var offenders = document.RootElement
            .GetProperty("tools")
            .EnumerateArray()
            .Select(tool => tool.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty)
            .Where(name => name.StartsWith(BrainSlotToolPrefix, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "docs/stdio-tool-contract.json must not advertise brain-slot tools. Found: " + string.Join("; ", offenders));
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

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "docs", "stdio-tool-contract.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
