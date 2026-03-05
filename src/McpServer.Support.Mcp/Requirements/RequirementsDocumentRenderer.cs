using System.Text;
using McpServer.Support.Mcp.Requirements.Models;

namespace McpServer.Support.Mcp.Requirements;

internal static class RequirementsDocumentRenderer
{
    internal const string FunctionalFileName = "Functional-Requirements.md";
    internal const string TechnicalFileName = "Technical-Requirements.md";
    internal const string TestingFileName = "Testing-Requirements.md";
    internal const string MappingFileName = "TR-per-FR-Mapping.md";

    public static string RenderFunctional(IEnumerable<FrEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Functional Requirements (MCP Server)");
        sb.AppendLine();

        foreach (var entry in entries)
        {
            sb.Append("## ").Append(entry.Id).Append(' ').AppendLine(entry.Title);
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(entry.Body))
                sb.AppendLine(entry.Body.Trim());
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string RenderTechnical(IEnumerable<TrEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Technical Requirements (MCP Server)");
        sb.AppendLine();

        foreach (var entry in entries)
        {
            sb.Append("## ").AppendLine(entry.Id);
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(entry.Title))
            {
                sb.Append("**").Append(entry.Title.Trim()).Append("**");
                if (!string.IsNullOrWhiteSpace(entry.Body))
                    sb.Append(" — ").Append(entry.Body.Trim());
                sb.AppendLine();
            }
            else if (!string.IsNullOrWhiteSpace(entry.Body))
            {
                sb.AppendLine(entry.Body.Trim());
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string RenderTesting(IEnumerable<TestEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Testing Requirements (MCP Server)");
        sb.AppendLine();

        foreach (var entry in entries)
            sb.Append("- ").Append(entry.Id).Append(": ").AppendLine(entry.Condition.Trim());

        if (sb.Length > 0 && sb[^1] != '\n')
            sb.AppendLine();

        return sb.ToString();
    }

    public static string RenderMapping(IEnumerable<FrTrMapping> mappings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# TR per FR Mapping (MCP Server)");
        sb.AppendLine();
        sb.AppendLine("| FR | Primary TRs |");
        sb.AppendLine("| --- | --- |");

        foreach (var mapping in mappings)
        {
            var trCell = mapping.TrIds is { Count: > 0 }
                ? string.Join(", ", mapping.TrIds)
                : "*(Planned)*";
            sb.Append("| ").Append(mapping.FrId).Append(" | ").Append(trCell).AppendLine(" |");
        }

        return sb.ToString();
    }
}
