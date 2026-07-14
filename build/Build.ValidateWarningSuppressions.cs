using Nuke.Common;
using Serilog;

partial class Build
{
    /// <summary>Validate warning suppression approvals against current source occurrences.</summary>
    public Target ValidateWarningSuppressions => _ => _
        .Executes(() =>
        {
            var approvalPath = RootDirectory / "config" / "warning-suppression-approvals.json";
            var artifactDirectory = ArtifactsDirectory / "warnings";
            var errors = WarningSuppressionApprovalValidator.ValidateRepository(
                RootDirectory.ToString(),
                approvalPath.ToString(),
                artifactDirectory.ToString());

            foreach (var error in errors)
            {
                Log.Error(
                    "{Location} {Code} {DiagnosticId} {Mechanism} {Message}",
                    FormatWarningSuppressionLocation(error),
                    error.Code,
                    error.DiagnosticId ?? "-",
                    error.Mechanism ?? "-",
                    error.Message);
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException($"Warning suppression validation failed with {errors.Count} error(s). See artifacts/warnings/warning-suppression-inventory.json.");
            }

            Log.Information("Warning suppression validation passed with inventory at {InventoryPath}.", artifactDirectory / "warning-suppression-inventory.json");
        });

    private static string FormatWarningSuppressionLocation(WarningSuppressionApprovalValidationError error)
    {
        if (error.Scope is null)
        {
            return "<unknown>";
        }

        return error.LineNumber is { } lineNumber ? $"{error.Scope}:{lineNumber}" : error.Scope;
    }
}
