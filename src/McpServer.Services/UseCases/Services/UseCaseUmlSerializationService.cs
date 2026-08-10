using System.Text;
using McpServer.Support.Mcp.UseCases.Models;

namespace McpServer.Support.Mcp.UseCases;

/// <summary>
/// FR-MCP-USECASE-013 / FR-MCP-USECASE-014 / TR-MCP-USECASE-014:
/// Pure UML use-case graph serializer (Mermaid schema v1 + PlantUML).
/// </summary>
public sealed class UseCaseUmlSerializationService : IUseCaseUmlSerializationService
{
    private const string MermaidSchemaHeader = "%% mcp-usecase-diagram-schema:1";

    /// <inheritdoc />
    public string ToMermaid(UseCaseDiagramGraphDto graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var nodes = (graph.Nodes ?? [])
            .OrderBy(n => n.Id, StringComparer.Ordinal)
            .ToList();
        var edges = (graph.Edges ?? [])
            .OrderBy(e => e.Id, StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine(MermaidSchemaHeader);
        sb.AppendLine("flowchart LR");

        var boundary = graph.SystemBoundary;
        var useCases = nodes.Where(n => string.Equals(n.Type, "usecase", StringComparison.OrdinalIgnoreCase)).ToList();
        var actors = nodes.Where(n => string.Equals(n.Type, "actor", StringComparison.OrdinalIgnoreCase)).ToList();

        if (boundary is not null && !string.IsNullOrWhiteSpace(boundary.Id))
        {
            var boundaryLabel = EscapeMermaidLabel(boundary.Label);
            sb.Append("  subgraph ").Append(SanitizeId(boundary.Id)).Append("[\"").Append(boundaryLabel).AppendLine("\"]");
            foreach (var uc in useCases)
            {
                AppendUseCaseNode(sb, uc, indent: "    ");
            }
            sb.AppendLine("  end");
        }
        else
        {
            foreach (var uc in useCases)
            {
                AppendUseCaseNode(sb, uc, indent: "  ");
            }
        }

        foreach (var actor in actors)
        {
            sb.Append("  ")
                .Append(SanitizeId(actor.Id))
                .Append("([\"")
                .Append(EscapeMermaidLabel(actor.Label))
                .AppendLine("\"])");
        }

        foreach (var edge in edges)
        {
            var src = SanitizeId(edge.Source);
            var tgt = SanitizeId(edge.Target);
            var line = edge.Type?.Trim().ToLowerInvariant() switch
            {
                "include" => $"  {src} -.->|include| {tgt}",
                "extend" => $"  {src} -.->|extend| {tgt}",
                "generalization" => $"  {src} -->|generalization| {tgt}",
                _ => $"  {src} --- {tgt}",
            };
            sb.AppendLine(line);
        }

        return sb.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public string ToPlantUml(UseCaseDiagramGraphDto graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var nodes = (graph.Nodes ?? [])
            .OrderBy(n => n.Id, StringComparer.Ordinal)
            .ToList();
        var edges = (graph.Edges ?? [])
            .OrderBy(e => e.Id, StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine("left to right direction");

        var boundary = graph.SystemBoundary;
        var useCases = nodes.Where(n => string.Equals(n.Type, "usecase", StringComparison.OrdinalIgnoreCase)).ToList();
        var actors = nodes.Where(n => string.Equals(n.Type, "actor", StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var actor in actors)
        {
            sb.Append("actor \"")
                .Append(EscapePlantUml(actor.Label))
                .Append("\" as ")
                .AppendLine(SanitizeId(actor.Id));
        }

        if (boundary is not null && !string.IsNullOrWhiteSpace(boundary.Id))
        {
            sb.Append("rectangle \"")
                .Append(EscapePlantUml(boundary.Label))
                .AppendLine("\" {");
            foreach (var uc in useCases)
            {
                sb.Append("  usecase \"")
                    .Append(EscapePlantUml(uc.Label))
                    .Append("\" as ")
                    .AppendLine(SanitizeId(uc.Id));
            }
            sb.AppendLine("}");
        }
        else
        {
            foreach (var uc in useCases)
            {
                sb.Append("usecase \"")
                    .Append(EscapePlantUml(uc.Label))
                    .Append("\" as ")
                    .AppendLine(SanitizeId(uc.Id));
            }
        }

        foreach (var edge in edges)
        {
            var src = SanitizeId(edge.Source);
            var tgt = SanitizeId(edge.Target);
            var line = edge.Type?.Trim().ToLowerInvariant() switch
            {
                "include" => $"{src} .> {tgt} : include",
                "extend" => $"{src} .> {tgt} : extend",
                "generalization" => $"{src} --|> {tgt}",
                _ => $"{src} --> {tgt}",
            };
            sb.AppendLine(line);
        }

        sb.AppendLine("@enduml");
        return sb.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static void AppendUseCaseNode(StringBuilder sb, UseCaseDiagramNodeDto uc, string indent)
    {
        sb.Append(indent)
            .Append(SanitizeId(uc.Id))
            .Append("([\"")
            .Append(EscapeMermaidLabel(uc.Label))
            .AppendLine("\"])");
    }

    private static string SanitizeId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "n_empty";
        var chars = id.Trim().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        var s = new string(chars);
        if (s.Length == 0 || char.IsDigit(s[0]))
            s = "n_" + s;
        return s;
    }

    private static string EscapeMermaidLabel(string? label)
        => (label ?? string.Empty).Replace("\"", "'", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    private static string EscapePlantUml(string? label)
        => (label ?? string.Empty).Replace("\"", "'", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
}
