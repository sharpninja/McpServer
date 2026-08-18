namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-MODES-001: Confidence gating and review decisions.</summary>
public interface IHandoffModePolicy
{
    /// <summary>Decide whether a validated draft may create a TODO or must wait for review.</summary>
    HandoffModeDecision Decide(HandoffIngestionMode mode, HandoffValidationResult validation, IReadOnlyList<HandoffDiagnostic> diagnostics);
}

/// <summary>TR-HANDOFF-MODES-001: Mode decision.</summary>
public sealed class HandoffModeDecision
{
    /// <summary>Whether TODO creation is permitted now.</summary>
    public bool CanCreate { get; init; }

    /// <summary>Whether the run should remain approvable.</summary>
    public bool RequiresReview { get; init; }

    /// <summary>Resulting review state.</summary>
    public HandoffReviewState ReviewState { get; init; }

    /// <summary>Policy diagnostics.</summary>
    public IReadOnlyList<HandoffDiagnostic> Diagnostics { get; init; } = [];
}

/// <inheritdoc />
public sealed class HandoffModePolicy : IHandoffModePolicy
{
    /// <inheritdoc />
    public HandoffModeDecision Decide(HandoffIngestionMode mode, HandoffValidationResult validation, IReadOnlyList<HandoffDiagnostic> diagnostics)
    {
        var extras = new List<HandoffDiagnostic>();
        var hasError = diagnostics.Any(item => item.Severity == HandoffDiagnosticSeverity.Error)
            || validation.Diagnostics.Any(item => item.Severity == HandoffDiagnosticSeverity.Error)
            || !validation.IsValid;
        var confidence = validation.Draft?.Confidence ?? 0;

        if (mode == HandoffIngestionMode.DraftOnly)
        {
            return new HandoffModeDecision
            {
                CanCreate = false,
                RequiresReview = false,
                ReviewState = hasError ? HandoffReviewState.Failed : HandoffReviewState.None,
            };
        }

        if (hasError)
        {
            extras.Add(new HandoffDiagnostic
            {
                Code = "mode_has_errors",
                Severity = HandoffDiagnosticSeverity.Error,
                Message = "Invalid or malformed drafts cannot be approved and do not remain pending review.",
            });
            return new HandoffModeDecision
            {
                CanCreate = false,
                RequiresReview = false,
                ReviewState = HandoffReviewState.Failed,
                Diagnostics = extras,
            };
        }

        if (mode == HandoffIngestionMode.RequireReview)
        {
            return new HandoffModeDecision
            {
                CanCreate = false,
                RequiresReview = true,
                ReviewState = HandoffReviewState.PendingReview,
            };
        }

        if (confidence < HandoffPromptDefaults.CreateWhenConfidentThreshold)
        {
            extras.Add(new HandoffDiagnostic
            {
                Code = "mode_low_confidence",
                Severity = HandoffDiagnosticSeverity.Warning,
                Field = "confidence",
                Message = "CreateWhenConfident requires confidence of at least 0.75.",
            });
            return new HandoffModeDecision
            {
                CanCreate = false,
                RequiresReview = true,
                ReviewState = HandoffReviewState.PendingReview,
                Diagnostics = extras,
            };
        }

        return new HandoffModeDecision
        {
            CanCreate = true,
            RequiresReview = false,
            ReviewState = HandoffReviewState.None,
        };
    }
}
