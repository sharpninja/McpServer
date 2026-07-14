using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Loads warning suppression approval registers from JSON.
/// </summary>
static class WarningSuppressionApprovalLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static IReadOnlyList<WarningSuppressionApproval> Load(string approvalPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalPath);

        using var stream = File.OpenRead(approvalPath);
        return JsonSerializer.Deserialize<IReadOnlyList<WarningSuppressionApproval>>(stream, SerializerOptions) ?? [];
    }
}

/// <summary>
/// Validates warning suppression approvals against current scanner inventory.
/// </summary>
static class WarningSuppressionApprovalValidator
{
    private static readonly JsonSerializerOptions InventorySerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static IReadOnlyList<WarningSuppressionApprovalValidationError> Validate(
        IReadOnlyList<WarningSuppressionApproval> approvals,
        IReadOnlyList<WarningSuppressionOccurrence> occurrences)
    {
        ArgumentNullException.ThrowIfNull(approvals);
        ArgumentNullException.ThrowIfNull(occurrences);

        var errors = new List<WarningSuppressionApprovalValidationError>();
        foreach (var approval in approvals)
        {
            ValidateRequiredFields(approval, errors);
        }

        AddDuplicateApprovalErrors(approvals, errors);
        AddScopeAndOccurrenceErrors(approvals, occurrences, errors);
        return errors;
    }

    public static string WriteInventory(
        string artifactDirectory,
        IReadOnlyList<WarningSuppressionOccurrence> occurrences,
        IReadOnlyList<WarningSuppressionApproval> approvals,
        IReadOnlyList<WarningSuppressionApprovalValidationError> errors)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactDirectory);
        ArgumentNullException.ThrowIfNull(occurrences);
        ArgumentNullException.ThrowIfNull(approvals);
        ArgumentNullException.ThrowIfNull(errors);

        Directory.CreateDirectory(artifactDirectory);
        var inventoryPath = Path.Combine(artifactDirectory, "warning-suppression-inventory.json");
        var inventory = new WarningSuppressionInventory(occurrences, approvals, errors);
        File.WriteAllText(inventoryPath, JsonSerializer.Serialize(inventory, InventorySerializerOptions));
        return inventoryPath;
    }

    public static IReadOnlyList<WarningSuppressionApprovalValidationError> ValidateRepository(
        string repositoryRoot,
        string approvalPath,
        string artifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactDirectory);

        var root = Path.GetFullPath(repositoryRoot);
        IReadOnlyList<WarningSuppressionApproval> approvals;
        try
        {
            approvals = WarningSuppressionApprovalLoader.Load(approvalPath);
        }
        catch (JsonException ex)
        {
            var errors = new[]
            {
                new WarningSuppressionApprovalValidationError(
                    "approval_parse_error",
                    $"Could not parse warning suppression approval register: {ex.Message}",
                    null,
                    NormalizeScope(Path.GetRelativePath(root, approvalPath))),
            };
            WriteInventory(artifactDirectory, [], [], errors);
            return errors;
        }

        var occurrences = WarningSuppressionScanner.Scan(root);
        var validationErrors = Validate(approvals, occurrences).ToList();
        AddUnapprovedOccurrenceErrors(approvals, occurrences, validationErrors);
        WriteInventory(artifactDirectory, occurrences, approvals, validationErrors);
        return validationErrors;
    }

    private static void ValidateRequiredFields(
        WarningSuppressionApproval approval,
        List<WarningSuppressionApprovalValidationError> errors)
    {
        AddMissingFieldError(approval, approval.DiagnosticId, "missing_diagnostic", "diagnosticId", errors);
        AddMissingFieldError(approval, approval.Scope, "missing_scope", "scope", errors);
        AddMissingFieldError(approval, approval.Mechanism, "missing_mechanism", "mechanism", errors);
        AddMissingFieldError(approval, approval.Justification, "missing_justification", "justification", errors);
        AddMissingFieldError(approval, approval.Owner, "missing_owner", "owner", errors);
        AddMissingFieldError(approval, approval.Permanence, "missing_permanence", "permanence", errors);
        AddMissingFieldError(approval, approval.ReviewCondition, "missing_review_condition", "reviewCondition", errors);
    }

    private static void AddMissingFieldError(
        WarningSuppressionApproval approval,
        string? value,
        string code,
        string fieldName,
        List<WarningSuppressionApprovalValidationError> errors)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        errors.Add(new WarningSuppressionApprovalValidationError(
            code,
            $"Approval is missing required field '{fieldName}'.",
            approval.DiagnosticId,
            approval.Scope));
    }

    private static void AddDuplicateApprovalErrors(
        IReadOnlyList<WarningSuppressionApproval> approvals,
        List<WarningSuppressionApprovalValidationError> errors)
    {
        foreach (var group in approvals
            .Where(HasRequiredIdentity)
            .GroupBy(approval => $"{NormalizeDiagnosticId(approval.DiagnosticId!)}|{NormalizeScope(approval.Scope!)}|{approval.Mechanism}", StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1))
        {
            var approval = group.First();
            errors.Add(new WarningSuppressionApprovalValidationError(
                "duplicate_approval",
                "Approval duplicates another approval with the same diagnostic, scope, and mechanism.",
                NormalizeDiagnosticId(approval.DiagnosticId!),
                NormalizeScope(approval.Scope!)));
        }
    }

    private static void AddScopeAndOccurrenceErrors(
        IReadOnlyList<WarningSuppressionApproval> approvals,
        IReadOnlyList<WarningSuppressionOccurrence> occurrences,
        List<WarningSuppressionApprovalValidationError> errors)
    {
        foreach (var approval in approvals.Where(HasRequiredIdentity))
        {
            var scope = NormalizeScope(approval.Scope!);
            var diagnosticId = NormalizeDiagnosticId(approval.DiagnosticId!);
            if (IsBroadScope(scope))
            {
                errors.Add(new WarningSuppressionApprovalValidationError(
                    "broad_scope",
                    "Approval scope must identify an exact repository-relative file path.",
                    diagnosticId,
                    scope));
                continue;
            }

            if (!Enum.TryParse<WarningSuppressionMechanism>(approval.Mechanism, ignoreCase: true, out var mechanism))
            {
                errors.Add(new WarningSuppressionApprovalValidationError(
                    "invalid_mechanism",
                    $"Approval mechanism '{approval.Mechanism}' is not supported.",
                    diagnosticId,
                    scope));
                continue;
            }

            var matchingOccurrences = occurrences
                .Where(occurrence => occurrence.DiagnosticId.Equals(diagnosticId, StringComparison.OrdinalIgnoreCase))
                .Where(occurrence => occurrence.RelativePath.Equals(scope, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matchingOccurrences.Length == 0)
            {
                errors.Add(new WarningSuppressionApprovalValidationError(
                    "stale_approval",
                    "Approval no longer matches any current scanner occurrence.",
                    diagnosticId,
                    scope));
                continue;
            }

            if (!matchingOccurrences.Any(occurrence => occurrence.Mechanism == mechanism))
            {
                errors.Add(new WarningSuppressionApprovalValidationError(
                    "changed_mechanism",
                    "Approval diagnostic and scope still exist, but the current suppression mechanism changed.",
                    diagnosticId,
                    scope));
            }
        }
    }

    private static void AddUnapprovedOccurrenceErrors(
        IReadOnlyList<WarningSuppressionApproval> approvals,
        IReadOnlyList<WarningSuppressionOccurrence> occurrences,
        List<WarningSuppressionApprovalValidationError> errors)
    {
        var approvalKeys = approvals
            .Where(HasRequiredIdentity)
            .Where(approval => !IsBroadScope(NormalizeScope(approval.Scope!)))
            .Where(approval => Enum.TryParse<WarningSuppressionMechanism>(approval.Mechanism, ignoreCase: true, out _))
            .Select(approval => CreateApprovalKey(
                NormalizeDiagnosticId(approval.DiagnosticId!),
                NormalizeScope(approval.Scope!),
                Enum.Parse<WarningSuppressionMechanism>(approval.Mechanism!, ignoreCase: true)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var occurrence in occurrences)
        {
            var key = CreateApprovalKey(occurrence.DiagnosticId, occurrence.RelativePath, occurrence.Mechanism);
            if (approvalKeys.Contains(key))
            {
                continue;
            }

            var location = $"{occurrence.RelativePath}:{occurrence.LineNumber}";
            errors.Add(new WarningSuppressionApprovalValidationError(
                "unapproved_occurrence",
                $"{location} {occurrence.DiagnosticId} {occurrence.Mechanism} is not approved.",
                occurrence.DiagnosticId,
                occurrence.RelativePath,
                occurrence.LineNumber,
                occurrence.Mechanism.ToString()));
        }
    }

    private static string CreateApprovalKey(string diagnosticId, string scope, WarningSuppressionMechanism mechanism)
    {
        return $"{diagnosticId}|{scope}|{mechanism}";
    }

    private static bool HasRequiredIdentity(WarningSuppressionApproval approval)
    {
        return !string.IsNullOrWhiteSpace(approval.DiagnosticId) &&
            !string.IsNullOrWhiteSpace(approval.Scope) &&
            !string.IsNullOrWhiteSpace(approval.Mechanism);
    }

    private static bool IsBroadScope(string scope)
    {
        return scope.Contains('*', StringComparison.Ordinal) || scope.Contains('?', StringComparison.Ordinal);
    }

    private static string NormalizeDiagnosticId(string diagnosticId)
    {
        var trimmed = diagnosticId.Trim().TrimEnd(':').ToUpperInvariant();
        var colonIndex = trimmed.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex >= 0)
        {
            trimmed = trimmed[..colonIndex];
        }

        return int.TryParse(trimmed, out var numericId) ? $"CS{numericId:D4}" : trimmed;
    }

    private static string NormalizeScope(string scope)
    {
        return scope.Trim().Replace('\\', '/');
    }

    private sealed record WarningSuppressionInventory(
        IReadOnlyList<WarningSuppressionOccurrence> Occurrences,
        IReadOnlyList<WarningSuppressionApproval> Approvals,
        IReadOnlyList<WarningSuppressionApprovalValidationError> Errors);
}

/// <summary>
/// Approval for a specific warning suppression occurrence.
/// </summary>
sealed class WarningSuppressionApproval
{
    public string? DiagnosticId { get; init; }

    public string? Scope { get; init; }

    public string? Mechanism { get; init; }

    public string? Justification { get; init; }

    public string? Owner { get; init; }

    public string? Permanence { get; init; }

    public string? ReviewCondition { get; init; }
}

/// <summary>
/// Validation error emitted for a warning suppression approval.
/// </summary>
sealed record WarningSuppressionApprovalValidationError(
    string Code,
    string Message,
    string? DiagnosticId,
    string? Scope,
    int? LineNumber = null,
    string? Mechanism = null);
