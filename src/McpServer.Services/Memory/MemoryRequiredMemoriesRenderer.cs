using System.Text;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-010-44 / FR-MCP-MEMORY-010-45: Renders the REQUIRED MEMORIES injection block
/// from Effective memories using raw Content (or legacy Text) only.
/// </summary>
public static class MemoryRequiredMemoriesRenderer
{
    /// <summary>Builds the REQUIRED MEMORIES block. Empty sets render <c>- None</c>.</summary>
    public static string RenderRequiredMemories(IReadOnlyList<MemoryItem> effective)
    {
        ArgumentNullException.ThrowIfNull(effective);
        var builder = new StringBuilder();
        builder.AppendLine("REQUIRED MEMORIES");
        if (effective.Count == 0)
        {
            builder.AppendLine("- None");
            return builder.ToString();
        }

        foreach (var item in effective)
        {
            var raw = item.Content ?? item.Text ?? string.Empty;
            builder.Append("- ");
            builder.AppendLine(raw);
        }

        return builder.ToString();
    }
}
