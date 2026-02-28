using McpServer.Launcher.Models;

namespace McpServer.Launcher.Validation;

/// <summary>
/// Validates a <see cref="ProcessLaunchRequest"/> before attempting to launch.
/// </summary>
internal static class RequestValidator
{
    /// <summary>
    /// Validates the specified request and returns a list of validation errors.
    /// An empty list indicates the request is valid.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A list of validation error messages (empty if valid).</returns>
    internal static List<string> Validate(ProcessLaunchRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ExecutablePath))
        {
            errors.Add("ExecutablePath is required.");
        }

        if (!string.IsNullOrEmpty(request.WorkingDirectory) && !Directory.Exists(request.WorkingDirectory))
        {
            errors.Add($"WorkingDirectory does not exist: {request.WorkingDirectory}");
        }

        if (request.TimeoutMs.HasValue)
        {
            if (request.TimeoutMs.Value <= 0)
            {
                errors.Add("TimeoutMs must be a positive integer.");
            }

            if (!request.WaitForExit)
            {
                errors.Add("TimeoutMs requires WaitForExit to be true.");
            }
        }

        if (!Enum.IsDefined(request.WindowStyle))
        {
            errors.Add($"Invalid WindowStyle value: {request.WindowStyle}");
        }

        return errors;
    }
}
