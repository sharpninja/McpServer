using System.Text.Json;
using FWH.Common.Copilot;

namespace FWH.Support.Mcp.Services;

/// <summary>
/// Invokes Copilot CLI to analyze a TODO item's title, description, and
/// technical details, then returns FR/TR IDs. Copilot is instructed to
/// update the project docs and return the assigned IDs as JSON.
/// </summary>
internal sealed class RequirementsService(
    ICopilotClient copilotClient,
    ITodoService todoService,
    ILogger<RequirementsService> logger) : IRequirementsService
{
    /// <inheritdoc />
    public async Task<RequirementsAnalysisResult> AnalyzeAsync(
        string todoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(todoId);

        var todo = await todoService.GetByIdAsync(todoId, cancellationToken).ConfigureAwait(false);
        if (todo is null)
            return new RequirementsAnalysisResult(false, Error: $"TODO item '{todoId}' not found.");

        var prompt = BuildPrompt(todo);

        logger.LogInformation("Invoking Copilot to analyze requirements for TODO {Id}", todoId);

        var options = new CopilotClientOptions
        {
            OutputFormat = "text",
            Timeout = TimeSpan.FromMinutes(5),
        };

        var result = await copilotClient.InvokeAsync(prompt, options, cancellationToken).ConfigureAwait(false);

        if (result.State != CopilotResultState.Success)
        {
            logger.LogWarning(
                "Copilot invocation failed for TODO {Id}: {State} (exit={ExitCode})",
                todoId, result.State, result.ExitCode);
            return new RequirementsAnalysisResult(
                false,
                Error: $"Copilot invocation failed: {result.State}. {result.Stderr}",
                CopilotResponse: result.Body);
        }

        // Extract FR/TR IDs from the Copilot response body
        var (frIds, trIds) = ExtractRequirementIds(result.Body);

        if (frIds.Count == 0 && trIds.Count == 0)
        {
            logger.LogWarning("Copilot did not return any FR/TR IDs for TODO {Id}", todoId);
            return new RequirementsAnalysisResult(
                false,
                Error: "Copilot did not return any FR/TR IDs in the response.",
                CopilotResponse: result.Body);
        }

        // Update the TODO item with the discovered FR/TR IDs
        var mergedFrs = MergeIds(todo.FunctionalRequirements, frIds);
        var mergedTrs = MergeIds(todo.TechnicalRequirements, trIds);

        var updateRequest = new TodoUpdateRequest
        {
            FunctionalRequirements = mergedFrs,
            TechnicalRequirements = mergedTrs,
        };

        var updateResult = await todoService.UpdateAsync(todoId, updateRequest, cancellationToken).ConfigureAwait(false);
        if (!updateResult.Success)
        {
            logger.LogWarning("Failed to update TODO {Id} with FR/TR: {Error}", todoId, updateResult.Error);
            return new RequirementsAnalysisResult(
                false,
                FunctionalRequirements: mergedFrs,
                TechnicalRequirements: mergedTrs,
                Error: $"Requirements identified but TODO update failed: {updateResult.Error}",
                CopilotResponse: result.Body);
        }

        logger.LogInformation(
            "Updated TODO {Id} with {FrCount} FRs and {TrCount} TRs",
            todoId, mergedFrs.Count, mergedTrs.Count);

        return new RequirementsAnalysisResult(
            true,
            FunctionalRequirements: mergedFrs,
            TechnicalRequirements: mergedTrs,
            CopilotResponse: result.Body);
    }

    private static string BuildPrompt(TodoFlatItem todo)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("You are a requirements analyst for the FunWasHad project.");
        sb.AppendLine();
        sb.AppendLine("Analyze the following TODO item and perform TWO tasks:");
        sb.AppendLine();
        sb.AppendLine("TASK 1: Identify all existing Functional Requirements (FR-XXX-###) and Technical Requirements (TR-XXX-###)");
        sb.AppendLine("from docs/Project/Functional-Requirements.md and docs/Project/Technical-Requirements.md that are");
        sb.AppendLine("associated with this TODO item. Search by topic, keywords, and feature area.");
        sb.AppendLine();
        sb.AppendLine("TASK 2: If the TODO describes functionality not yet covered by existing FRs or TRs, create new entries:");
        sb.AppendLine("  - Add new FR entries to docs/Project/Functional-Requirements.md following the format:");
        sb.AppendLine("    #### FR-{DOMAIN}-{###}: {Title}");
        sb.AppendLine("    The system SHALL {description}.");
        sb.AppendLine("    **Technical Implementation:** [TR-XXX-###](./Technical-Requirements.md#tr-xxx-###) | [Details](./TR-per-FR-Mapping.md#fr-xxx-###)");
        sb.AppendLine();
        sb.AppendLine("  - Add new TR entries to docs/Project/Technical-Requirements.md following the format:");
        sb.AppendLine("    ### TR-{DOMAIN}-{###}: {Title}");
        sb.AppendLine("    {Description with bullet points}");
        sb.AppendLine("    **Status:** 🔴 Planned");
        sb.AppendLine();
        sb.AppendLine("  - Update docs/Project/TR-per-FR-Mapping.md with new mappings.");
        sb.AppendLine();
        sb.AppendLine("IMPORTANT: After updating the docs, output a JSON block with the complete list of FR and TR IDs");
        sb.AppendLine("(both existing and newly created) that are associated with this TODO item.");
        sb.AppendLine("Use this exact format on its own line:");
        sb.AppendLine();
        sb.AppendLine("```json");
        sb.AppendLine("{");
        sb.AppendLine("  \"functionalRequirements\": [\"FR-XXX-001\", \"FR-XXX-002\"],");
        sb.AppendLine("  \"technicalRequirements\": [\"TR-XXX-001\", \"TR-XXX-002\"]");
        sb.AppendLine("}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("--- TODO ITEM ---");
        sb.Append("ID: ").AppendLine(todo.Id);
        sb.Append("Title: ").AppendLine(todo.Title);
        sb.Append("Section: ").AppendLine(todo.Section);
        sb.Append("Priority: ").AppendLine(todo.Priority);

        if (todo.Description?.Count > 0)
        {
            sb.AppendLine("Description:");
            foreach (var line in todo.Description)
                sb.Append("  - ").AppendLine(line);
        }

        if (todo.TechnicalDetails?.Count > 0)
        {
            sb.AppendLine("Technical Details:");
            foreach (var line in todo.TechnicalDetails)
                sb.Append("  - ").AppendLine(line);
        }

        if (todo.ImplementationTasks?.Count > 0)
        {
            sb.AppendLine("Implementation Tasks:");
            foreach (var task in todo.ImplementationTasks)
                sb.Append("  - [").Append(task.Done ? "x" : " ").Append("] ").AppendLine(task.Task);
        }

        if (todo.FunctionalRequirements?.Count > 0)
        {
            sb.AppendLine("Existing FRs already assigned:");
            foreach (var fr in todo.FunctionalRequirements)
                sb.Append("  - ").AppendLine(fr);
        }

        if (todo.TechnicalRequirements?.Count > 0)
        {
            sb.AppendLine("Existing TRs already assigned:");
            foreach (var tr in todo.TechnicalRequirements)
                sb.Append("  - ").AppendLine(tr);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Extract FR-XXX-### and TR-XXX-### IDs from the Copilot response.
    /// First tries to parse a JSON block; falls back to regex extraction.
    /// </summary>
    internal static (List<string> FrIds, List<string> TrIds) ExtractRequirementIds(string body)
    {
        // Try to find a JSON block in the response
        var jsonMatch = System.Text.RegularExpressions.Regex.Match(
            body,
            @"\{[^{}]*""functionalRequirements""[^{}]*\}",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (jsonMatch.Success)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonMatch.Value);
                var frIds = ExtractJsonArray(doc, "functionalRequirements");
                var trIds = ExtractJsonArray(doc, "technicalRequirements");
                if (frIds.Count > 0 || trIds.Count > 0)
                    return (frIds, trIds);
            }
            catch (JsonException)
            {
                // Fall through to regex
            }
        }

        // Fallback: regex extraction of all FR-XXX-### and TR-XXX-### patterns
        var frMatches = System.Text.RegularExpressions.Regex.Matches(body, @"FR-[A-Z]+-\d{3}");
        var trMatches = System.Text.RegularExpressions.Regex.Matches(body, @"TR-[A-Z]+-\d{3}");

        var frs = frMatches.Select(m => m.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var trs = trMatches.Select(m => m.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return (frs, trs);
    }

    private static List<string> ExtractJsonArray(JsonDocument doc, string propertyName)
    {
        if (doc.RootElement.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
            return arr.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        return [];
    }

    /// <summary>Merge new IDs into existing list, preserving order and deduplicating.</summary>
    private static List<string> MergeIds(IReadOnlyList<string>? existing, List<string> newIds)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<string>();

        if (existing is not null)
        {
            foreach (var id in existing)
            {
                if (set.Add(id))
                    merged.Add(id);
            }
        }

        foreach (var id in newIds)
        {
            if (set.Add(id))
                merged.Add(id);
        }

        return merged;
    }
}
