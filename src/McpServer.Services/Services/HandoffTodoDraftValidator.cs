using System.Text.RegularExpressions;

namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-VALIDATE-001: Pure draft validation and normalization.</summary>
public interface IHandoffTodoDraftValidator
{
    /// <summary>Validate and normalize a draft. Has no storage side effects.</summary>
    HandoffValidationResult Validate(HandoffTodoDraft? draft);
}

/// <summary>TR-HANDOFF-VALIDATE-001: Validation result.</summary>
public sealed class HandoffValidationResult
{
    /// <summary>Whether the draft is usable for TODO creation.</summary>
    public bool IsValid { get; init; }

    /// <summary>Normalized draft when one was supplied.</summary>
    public HandoffTodoDraft? Draft { get; init; }

    /// <summary>Field-specific diagnostics.</summary>
    public IReadOnlyList<HandoffDiagnostic> Diagnostics { get; init; } = [];
}

/// <inheritdoc />
public sealed class HandoffTodoDraftValidator : IHandoffTodoDraftValidator
{
    private static readonly Regex TodoIdRegex = new("^[A-Z]+-[A-Z0-9]+-\\d{3}$", RegexOptions.Compiled);
    private static readonly Regex IssueIdRegex = new("^ISSUE-\\d+$", RegexOptions.Compiled);
    private static readonly Regex RequirementIdRegex = new(
        "^(FR|TR|TEST)-[A-Z0-9]+(-[A-Z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> Priorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "critical",
        "high",
        "medium",
        "low",
    };

    /// <inheritdoc />
    public HandoffValidationResult Validate(HandoffTodoDraft? draft)
    {
        if (draft is null)
        {
            return new HandoffValidationResult
            {
                IsValid = false,
                Diagnostics =
                [
                    new HandoffDiagnostic
                    {
                        Code = "draft_invalid_id",
                        Severity = HandoffDiagnosticSeverity.Error,
                        Field = "id",
                        Message = "A TODO draft is required.",
                    },
                ],
            };
        }

        var diagnostics = new List<HandoffDiagnostic>();
        var normalized = new HandoffTodoDraft
        {
            Id = NormalizeRequired(draft.Id, "id", "draft_invalid_id", "TODO id is required and must match the canonical TODO or ISSUE format.", diagnostics, value =>
                TodoIdRegex.IsMatch(value) || IssueIdRegex.IsMatch(value)),
            Title = NormalizeRequired(draft.Title, "title", "draft_invalid_title", "Title is required.", diagnostics),
            Section = NormalizeRequired(draft.Section, "section", "draft_invalid_section", "Section is required.", diagnostics),
            Priority = NormalizePriority(draft.Priority, diagnostics),
            Estimate = NormalizeOptional(draft.Estimate, "estimate", "draft_invalid_estimate", diagnostics),
            Description = NormalizeLines(draft.Description, "description", "draft_invalid_description", diagnostics),
            TechnicalDetails = NormalizeLines(draft.TechnicalDetails, "technicalDetails", "draft_invalid_technicalDetails", diagnostics),
            ImplementationTasks = NormalizeTasks(draft.ImplementationTasks, diagnostics),
            DependsOn = NormalizeIds(draft.DependsOn, "dependsOn", "draft_invalid_dependsOn", diagnostics, value =>
                TodoIdRegex.IsMatch(value) || IssueIdRegex.IsMatch(value)),
            FunctionalRequirements = NormalizeIds(draft.FunctionalRequirements, "functionalRequirements", "draft_invalid_functionalRequirements", diagnostics, value => IsRequirementId(value, "FR")),
            TechnicalRequirements = NormalizeIds(draft.TechnicalRequirements, "technicalRequirements", "draft_invalid_technicalRequirements", diagnostics, value => IsRequirementId(value, "TR")),
            Confidence = NormalizeConfidence(draft.Confidence, diagnostics),
            UnknownSourceNotes = draft.UnknownSourceNotes
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
        };

        return new HandoffValidationResult
        {
            IsValid = diagnostics.TrueForAll(item => item.Severity != HandoffDiagnosticSeverity.Error),
            Draft = normalized,
            Diagnostics = diagnostics,
        };
    }

    private static string? NormalizeRequired(
        string? value,
        string field,
        string code,
        string message,
        List<HandoffDiagnostic> diagnostics,
        Func<string, bool>? extra = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(Error(code, field, message));
            return null;
        }

        var trimmed = value.Trim();
        if (extra is not null && !extra(trimmed))
        {
            diagnostics.Add(Error(code, field, message));
            return trimmed;
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, string field, string code, List<HandoffDiagnostic> diagnostics)
    {
        if (value is null)
            return null;
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(Error(code, field, "Estimate cannot be blank when supplied."));
            return null;
        }

        return value.Trim();
    }

    private static string? NormalizePriority(string? value, List<HandoffDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value) || !Priorities.Contains(value.Trim()))
        {
            diagnostics.Add(Error("draft_invalid_priority", "priority", "Priority must be critical, high, medium, or low."));
            return value?.Trim();
        }

        return value.Trim().ToLowerInvariant();
    }

    private static IReadOnlyList<string> NormalizeLines(IReadOnlyList<string>? values, string field, string code, List<HandoffDiagnostic> diagnostics)
    {
        var items = (values ?? []).Select(item => item?.Trim() ?? string.Empty).Where(item => item.Length > 0).ToArray();
        if (values is not null && values.Count > 0 && items.Length == 0)
            diagnostics.Add(Error(code, field, $"{field} contains no usable text."));
        return items;
    }

    private static IReadOnlyList<HandoffTodoDraftTask> NormalizeTasks(IReadOnlyList<HandoffTodoDraftTask>? tasks, List<HandoffDiagnostic> diagnostics)
    {
        var normalized = new List<HandoffTodoDraftTask>();
        foreach (var task in tasks ?? [])
        {
            if (string.IsNullOrWhiteSpace(task.Task))
            {
                diagnostics.Add(Error("draft_invalid_implementationTasks", "implementationTasks", "Implementation tasks must include task text."));
                continue;
            }

            normalized.Add(new HandoffTodoDraftTask { Task = task.Task.Trim(), Done = task.Done });
        }

        return normalized;
    }

    private static IReadOnlyList<string> NormalizeIds(
        IReadOnlyList<string>? values,
        string field,
        string code,
        List<HandoffDiagnostic> diagnostics,
        Func<string, bool> isValid)
    {
        var items = new List<string>();
        foreach (var value in values ?? [])
        {
            if (string.IsNullOrWhiteSpace(value) || !isValid(value.Trim()))
            {
                diagnostics.Add(Error(code, field, $"{field} contains an invalid identifier."));
                continue;
            }

            items.Add(value.Trim());
        }

        return items;
    }

    private static bool IsRequirementId(string value, string prefix)
        => RequirementIdRegex.IsMatch(value) && value.StartsWith(prefix + "-", StringComparison.Ordinal);

    private static double? NormalizeConfidence(double? confidence, List<HandoffDiagnostic> diagnostics)
    {
        if (confidence is null)
        {
            diagnostics.Add(Error("draft_invalid_confidence", "confidence", "Confidence is required."));
            return null;
        }

        if (double.IsNaN(confidence.Value) || confidence.Value < 0 || confidence.Value > 1)
        {
            diagnostics.Add(Error("draft_invalid_confidence", "confidence", "Confidence must be between 0.0 and 1.0."));
            return confidence;
        }

        return confidence;
    }

    private static HandoffDiagnostic Error(string code, string field, string message)
        => new()
        {
            Code = code,
            Severity = HandoffDiagnosticSeverity.Error,
            Field = field,
            Message = message,
        };
}
