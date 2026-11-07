using System.Diagnostics;

/// <summary>TR-MCP-NUKE-019: launches a Linux PowerShell logging worker without inheriting the build caller's pipes.</summary>
internal static class OllamaDetachedLaunch
{
    /// <summary>Composes a detached session with all standard handles isolated; the worker writes server output to its log.</summary>
    /// <param name="ollamaPath">The executable passed as one literal argument.</param>
    /// <param name="logPath">The worker's output file.</param>
    /// <param name="workerPath">The checked-in PowerShell worker script.</param>
    /// <returns>Start information that retains no caller standard handle.</returns>
    internal static ProcessStartInfo CreateLinuxStartInfo(string ollamaPath, string logPath, string workerPath)
    {
        var info = new ProcessStartInfo
        {
            FileName = "setsid", UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (var argument in new[] { "pwsh.exe", "-NoProfile", "-NonInteractive", "-File", workerPath,
                     "-OllamaPath", ollamaPath, "-LogPath", logPath })
            info.ArgumentList.Add(argument);
        return info;
    }

    /// <summary>
    /// Starts the logging worker in its own session and releases the parent-side handles without waiting for the
    /// long-lived server. The worker sends server output directly to its log, never back through these handles.
    /// </summary>
    /// <param name="ollamaPath">The server executable.</param>
    /// <param name="logPath">The persistent worker log.</param>
    /// <param name="workerPath">The PowerShell script used by the worker.</param>
    /// <returns>The detached worker's process id.</returns>
    internal static int StartLinux(string ollamaPath, string logPath, string workerPath)
    {
        using var worker = Process.Start(CreateLinuxStartInfo(ollamaPath, logPath, workerPath))
            ?? throw new InvalidOperationException("Failed to start the detached Ollama logging worker.");
        worker.StandardInput.Close();
        return worker.Id;
    }
}
