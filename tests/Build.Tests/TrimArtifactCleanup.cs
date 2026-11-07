// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-010 AC-001 (BUG-BUILD-009): the trim-analysis artifact cleanup, shared by link between the build's
/// infrastructure tests (TrimArtifactCleanupTests, which proves it) and the build's integration tests
/// (TrimAnalysisWarningInventoryTests, which uses it after each trim run).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-NUKE-010 AC-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from TrimAnalysisWarningInventoryTests.TryDeleteArtifacts when that class moved to the integration-owned Build.IntegrationTests (slice S4, hostile validation finding R14-F4), so the infrastructure case that proves the cleanup keeps calling the one definition instead of a type in another test assembly or a copy (DRY); behavior unchanged.</remarks>
internal static class TrimArtifactCleanup
{
    /// <summary>
    /// Best-effort artifact cleanup on every OS (TR-MCP-NUKE-010 AC-001, BUG-BUILD-009). On Windows a recursive
    /// <see cref="Directory.Delete(string, bool)"/> of a trimmed self-contained publish tree can block testhost for
    /// tens of minutes under AV scan, so the delete runs in a detached cmd.exe there; elsewhere it runs in place.
    /// </summary>
    /// <param name="artifactsRoot">The artifacts root to remove; a missing root is ignored.</param>
    /// <remarks>2026-11-07, TR-MCP-NUKE-010 AC-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved here unchanged from TrimAnalysisWarningInventoryTests (slice S4).</remarks>
    internal static void TryDeleteArtifacts(string artifactsRoot)
    {
        if (!Directory.Exists(artifactsRoot))
        {
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            try
            {
                Directory.Delete(artifactsRoot, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return;
        }

        try
        {
            var start = new ProcessStartInfo("cmd.exe")
            {
                Arguments = $"/c rmdir /s /q \"{artifactsRoot}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            Process.Start(start)?.Dispose();
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }
}
