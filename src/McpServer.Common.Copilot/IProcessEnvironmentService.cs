using System.Diagnostics;

namespace McpServer.Common.Copilot;

/// <summary>
/// Applies user-profile environment variables and GitHub tokens to a
/// <see cref="ProcessStartInfo"/> so that processes spawned from a Windows
/// service (Session 0) can locate CLIs on the user's PATH and authenticate
/// with GitHub via <c>GH_TOKEN</c>.
/// </summary>
public interface IProcessEnvironmentService
{
    /// <summary>
    /// Sets <c>GH_TOKEN</c> on <paramref name="psi"/> if <paramref name="token"/>
    /// is provided, falling back to the current process's <c>GH_TOKEN</c> variable.
    /// </summary>
    void ApplyGitHubToken(ProcessStartInfo psi, string? token);

    /// <summary>
    /// Loads the specified user's profile environment (<c>USERPROFILE</c>,
    /// <c>APPDATA</c>, <c>LOCALAPPDATA</c>, <c>PATH</c>) into
    /// <paramref name="psi"/> so the spawned process inherits the user's
    /// tool paths and auth caches. No-op on non-Windows or when
    /// <paramref name="runAsUser"/> is null/empty.
    /// </summary>
    void ApplyRunAsEnvironment(ProcessStartInfo psi, string? runAsUser);

    /// <summary>
    /// Convenience method that applies both <see cref="ApplyGitHubToken"/>
    /// and <see cref="ApplyRunAsEnvironment"/> in a single call.
    /// </summary>
    void ApplyAll(ProcessStartInfo psi, string? runAsUser, string? gitHubToken);

    /// <summary>
    /// Resolves the full path to an executable by searching the PATH entries
    /// in <paramref name="psi"/>. Required because <c>Process.Start</c> with
    /// <c>UseShellExecute=false</c> uses the parent process's PATH, not the
    /// child's <c>ProcessStartInfo.Environment["PATH"]</c>.
    /// Returns the original <paramref name="fileName"/> if no match is found.
    /// </summary>
    string ResolveExecutable(ProcessStartInfo psi, string fileName);
}
