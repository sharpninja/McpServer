using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;

namespace McpServer.Support.Mcp.UseCases;

/// <summary>
/// TR-MCP-USECASE-004: Minimal Mermaid sequenceDiagram generator for use cases.
/// </summary>
public sealed partial class MermaidUseCaseDiagramService : IUseCaseDiagramService
{
    /// <inheritdoc />
    public string Generate(UseCaseDetailDto useCase, string format)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        var normalized = string.IsNullOrWhiteSpace(format) ? "mermaid" : format.Trim().ToLowerInvariant();
        return normalized switch
        {
            "mermaid" => GenerateMermaid(useCase),
            "plantuml" => GeneratePlantUml(useCase),
            _ => throw new ArgumentException($"Unsupported diagram format '{format}'. Supported: mermaid, plantuml."),
        };
    }

    /// <inheritdoc />
    public string GenerateMermaid(UseCaseDetailDto useCase)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        var sb = new StringBuilder();
        sb.AppendLine("sequenceDiagram");

        var participants = new List<(string Alias, string Label)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var actor in useCase.Actors.OrderByDescending(a => a.IsPrimary).ThenBy(a => a.Name, StringComparer.Ordinal))
        {
            var alias = SanitizeParticipant(actor.Name);
            if (!seen.Add(alias))
                continue;
            participants.Add((alias, actor.Name));
        }

        const string systemAlias = "System";
        if (seen.Add(systemAlias))
            participants.Add((systemAlias, "System"));

        foreach (var (alias, label) in participants)
        {
            if (string.Equals(alias, label, StringComparison.Ordinal))
                sb.AppendLine(CultureInfo.InvariantCulture, $"    participant {alias}");
            else
                sb.AppendLine(CultureInfo.InvariantCulture, $"    participant {alias} as {EscapeLabel(label)}");
        }

        if (participants.Count == 0)
        {
            sb.AppendLine("    participant System");
        }

        var flows = useCase.Flows.OrderBy(f => f.SequenceNumber).ThenBy(f => f.FlowId).ToList();
        if (flows.Count == 0)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"    Note over System: {EscapeLabel(useCase.Title)} (no flows)");
            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        foreach (var flow in flows)
        {
            var flowLabel = string.IsNullOrWhiteSpace(flow.Name)
                ? flow.FlowType
                : $"{flow.FlowType}: {flow.Name}";
            sb.AppendLine(CultureInfo.InvariantCulture, $"    Note over System: {EscapeLabel(flowLabel)}");

            foreach (var step in flow.Steps.OrderBy(s => s.StepNumber).ThenBy(s => s.StepId))
            {
                var from = ResolveActorAlias(step, useCase);
                var action = string.IsNullOrWhiteSpace(step.Action) ? "(no action)" : step.Action.Trim();
                sb.AppendLine(CultureInfo.InvariantCulture, $"    {from}->>System: {EscapeLabel(action)}");
                if (!string.IsNullOrWhiteSpace(step.SystemResponse))
                    sb.AppendLine(CultureInfo.InvariantCulture, $"    System-->>{from}: {EscapeLabel(step.SystemResponse)}");
            }
        }

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    /// <summary>TR-MCP-USECASE-004: PlantUML sequence diagram for the same aggregate.</summary>
    public string GeneratePlantUml(UseCaseDetailDto useCase)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        var sb = new StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine($"title {EscapeLabel(useCase.Title)}");

        foreach (var actor in useCase.Actors.OrderByDescending(a => a.IsPrimary).ThenBy(a => a.Name, StringComparer.Ordinal))
        {
            var alias = SanitizeParticipant(actor.Name);
            sb.AppendLine(CultureInfo.InvariantCulture, $"actor \"{EscapeLabel(actor.Name)}\" as {alias}");
        }

        sb.AppendLine("participant System");

        foreach (var flow in useCase.Flows.OrderBy(f => f.SequenceNumber).ThenBy(f => f.FlowId))
        {
            var flowLabel = string.IsNullOrWhiteSpace(flow.Name) ? flow.FlowType : $"{flow.FlowType}: {flow.Name}";
            sb.AppendLine(CultureInfo.InvariantCulture, $"note over System: {EscapeLabel(flowLabel)}");
            foreach (var step in flow.Steps.OrderBy(s => s.StepNumber).ThenBy(s => s.StepId))
            {
                var from = ResolveActorAlias(step, useCase);
                var action = string.IsNullOrWhiteSpace(step.Action) ? "(no action)" : step.Action.Trim();
                sb.AppendLine(CultureInfo.InvariantCulture, $"{from} -> System: {EscapeLabel(action)}");
                if (!string.IsNullOrWhiteSpace(step.SystemResponse))
                    sb.AppendLine(CultureInfo.InvariantCulture, $"System --> {from}: {EscapeLabel(step.SystemResponse)}");
            }
        }

        sb.AppendLine("@enduml");
        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string ResolveActorAlias(UseCaseStepDto step, UseCaseDetailDto useCase)
    {
        if (!string.IsNullOrWhiteSpace(step.ActorName))
            return SanitizeParticipant(step.ActorName);

        if (step.ActorId is long actorId)
        {
            var match = useCase.Actors.FirstOrDefault(a => a.ActorId == actorId);
            if (match is not null)
                return SanitizeParticipant(match.Name);
        }

        var primary = useCase.Actors.FirstOrDefault(a => a.IsPrimary) ?? useCase.Actors.FirstOrDefault();
        return primary is null ? "System" : SanitizeParticipant(primary.Name);
    }

    private static string SanitizeParticipant(string name)
    {
        var trimmed = string.IsNullOrWhiteSpace(name) ? "Actor" : name.Trim();
        var cleaned = ParticipantSanitizeRegex().Replace(trimmed, "_");
        if (cleaned.Length == 0 || char.IsDigit(cleaned[0]))
            cleaned = "A_" + cleaned;
        return cleaned;
    }

    private static string EscapeLabel(string text)
    {
        return text
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\"", "'", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"[^A-Za-z0-9_]", RegexOptions.CultureInvariant)]
    private static partial Regex ParticipantSanitizeRegex();
}
