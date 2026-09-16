using System.Collections.Generic;

namespace McpServer.Client.Models;

/// <summary>
/// Exposes every text-bearing field in a use-case creation request to the shared validator.
/// </summary>
public interface IUseCaseCreationTextGraph
{
    /// <summary>Use-case title.</summary>
    string? Title { get; }

    /// <summary>Optional brief description.</summary>
    string? BriefDescription { get; }

    /// <summary>Optional precondition.</summary>
    string? Precondition { get; }

    /// <summary>Optional postcondition.</summary>
    string? Postcondition { get; }

    /// <summary>Optional scope.</summary>
    string? Scope { get; }

    /// <summary>Optional functional-requirement identifier.</summary>
    string? FrId { get; }

    /// <summary>Optional requirement-link type.</summary>
    string? LinkType { get; }

    /// <summary>Optional requirement-link notes.</summary>
    string? Notes { get; }

    /// <summary>Optional durable request identifier.</summary>
    string? RequestId { get; }

    /// <summary>Initial step text graphs.</summary>
    IEnumerable<IUseCaseCreationStepTextGraph>? ValidationSteps { get; }
}

/// <summary>Exposes every text-bearing field in an initial use-case step.</summary>
public interface IUseCaseCreationStepTextGraph
{
    /// <summary>Actor action.</summary>
    string? Action { get; }

    /// <summary>Optional system response.</summary>
    string? SystemResponse { get; }

    /// <summary>Optional data-entity text.</summary>
    string? DataEntities { get; }
}

/// <summary>Exposes create-from-FR override text to the shared validator.</summary>
public interface IUseCaseFromFrTextGraph
{
    /// <summary>Optional durable request identifier.</summary>
    string? RequestId { get; }

    /// <summary>Optional title override.</summary>
    string? Title { get; }

    /// <summary>Optional brief-description override.</summary>
    string? BriefDescription { get; }
}

/// <summary>
/// Validates the complete public use-case creation text graph before serialization, hashing, or
/// persistence can replace malformed UTF-16 code units.
/// </summary>
public static class UseCaseCreationTextValidator
{
    /// <summary>Returns whether a value contains only correctly paired UTF-16 surrogate code units.</summary>
    /// <param name="value">The optional value to inspect.</param>
    /// <returns><see langword="true"/> for null or well-formed UTF-16.</returns>
    public static bool IsWellFormedUtf16(string? value)
    {
        if (value is null)
            return true;

        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (char.IsLowSurrogate(current))
                return false;
            if (!char.IsHighSurrogate(current))
                continue;
            if (++index >= value.Length || !char.IsLowSurrogate(value[index]))
                return false;
        }

        return true;
    }

    /// <summary>Validates a complete normal-create request graph.</summary>
    /// <param name="request">Request graph to inspect.</param>
    /// <param name="invalidField">Stable public field name when validation fails.</param>
    /// <returns><see langword="true"/> when every text field is well formed.</returns>
    public static bool TryValidate(
        IUseCaseCreationTextGraph request,
        out string? invalidField)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryValidateField(request.Title, "Title", out invalidField) ||
            !TryValidateField(request.BriefDescription, "BriefDescription", out invalidField) ||
            !TryValidateField(request.Precondition, "Precondition", out invalidField) ||
            !TryValidateField(request.Postcondition, "Postcondition", out invalidField) ||
            !TryValidateField(request.Scope, "Scope", out invalidField) ||
            !TryValidateField(request.FrId, "FrId", out invalidField) ||
            !TryValidateField(request.LinkType, "LinkType", out invalidField) ||
            !TryValidateField(request.Notes, "Notes", out invalidField) ||
            !TryValidateField(request.RequestId, "RequestId", out invalidField))
        {
            return false;
        }

        if (request.ValidationSteps is null)
            return true;

        var stepIndex = 0;
        foreach (var step in request.ValidationSteps)
        {
            if (step is null)
            {
                invalidField = $"InitialSteps[{stepIndex}]";
                return false;
            }

            if (!TryValidateField(
                    step.Action,
                    $"InitialSteps[{stepIndex}].Action",
                    out invalidField) ||
                !TryValidateField(
                    step.SystemResponse,
                    $"InitialSteps[{stepIndex}].SystemResponse",
                    out invalidField) ||
                !TryValidateField(
                    step.DataEntities,
                    $"InitialSteps[{stepIndex}].DataEntities",
                    out invalidField))
            {
                return false;
            }

            stepIndex++;
        }

        invalidField = null;
        return true;
    }

    /// <summary>Validates create-from-FR route and override text.</summary>
    /// <param name="frId">Functional-requirement route identifier.</param>
    /// <param name="request">Optional override graph.</param>
    /// <param name="invalidField">Stable public field name when validation fails.</param>
    /// <returns><see langword="true"/> when every text field is well formed.</returns>
    public static bool TryValidateFromFr(
        string? frId,
        IUseCaseFromFrTextGraph? request,
        out string? invalidField)
    {
        if (!TryValidateField(frId, "FrId", out invalidField))
            return false;
        if (request is null)
            return true;
        if (!TryValidateField(request.RequestId, "RequestId", out invalidField) ||
            !TryValidateField(request.Title, "Title", out invalidField) ||
            !TryValidateField(request.BriefDescription, "BriefDescription", out invalidField))
        {
            return false;
        }

        invalidField = null;
        return true;
    }

    /// <summary>Throws a deterministic client validation exception for an invalid create graph.</summary>
    /// <param name="request">Request graph to validate.</param>
    public static void ValidateOrThrow(IUseCaseCreationTextGraph request)
    {
        if (!TryValidate(request, out var invalidField))
        {
            throw new ArgumentException(
                $"{invalidField} must be well-formed UTF-16.",
                invalidField);
        }
    }

    /// <summary>Throws a deterministic client validation exception for invalid create-from-FR text.</summary>
    /// <param name="frId">Functional-requirement route identifier.</param>
    /// <param name="request">Optional override graph.</param>
    public static void ValidateFromFrOrThrow(
        string? frId,
        IUseCaseFromFrTextGraph? request)
    {
        if (!TryValidateFromFr(frId, request, out var invalidField))
        {
            throw new ArgumentException(
                $"{invalidField} must be well-formed UTF-16.",
                invalidField);
        }
    }

    private static bool TryValidateField(
        string? value,
        string fieldName,
        out string? invalidField)
    {
        if (IsWellFormedUtf16(value))
        {
            invalidField = null;
            return true;
        }

        invalidField = fieldName;
        return false;
    }
}
