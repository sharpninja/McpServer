/// <summary>TR-MCP-NUKE-016: where InstallOllama gets the ollama executable it runs.</summary>
internal enum OllamaExecutableSource
{
    /// <summary>No step: a server already answers on localhost:11434 with the required model.</summary>
    None,

    /// <summary>An installed ollama: on PATH, or on Windows in Ollama's install folder LocalAppData/Programs/Ollama.</summary>
    Installed,

    /// <summary>A previous portable install under the test-tools folder.</summary>
    PortableInstall,

    /// <summary>A download of the operating system's portable release into the test-tools folder.</summary>
    Download,
}

/// <summary>TR-MCP-NUKE-016 (PLAN-SESSIONTRIAGEFIX-001 B13): what InstallOllama observes on a machine before it acts.</summary>
/// <param name="IsWindows">Whether the build host is Windows (otherwise Linux).</param>
/// <param name="ServerAnswers">Whether a server answers on localhost:11434.</param>
/// <param name="ModelPresent">Whether that server lists the required model.</param>
/// <param name="InstalledExecutable">An installed ollama found on PATH or, on Windows, in LocalAppData/Programs/Ollama; null when none.</param>
/// <param name="PortableInstallExists">Whether a previous portable install's executable exists under the test-tools folder.</param>
/// <param name="TestToolsDirectory">The test-tools folder.</param>
/// <param name="Model">The required model.</param>
internal sealed record OllamaInstallFacts(
    bool IsWindows,
    bool ServerAnswers,
    bool ModelPresent,
    string? InstalledExecutable,
    bool PortableInstallExists,
    string TestToolsDirectory,
    string Model);

/// <summary>TR-MCP-NUKE-016 (PLAN-SESSIONTRIAGEFIX-001 B13): the steps InstallOllama takes on one machine.</summary>
/// <param name="Source">Where the executable comes from; <see cref="OllamaExecutableSource.None"/> when nothing is needed.</param>
/// <param name="Executable">The ollama executable the steps run; null when nothing is needed.</param>
/// <param name="DownloadUrl">The portable release downloaded when <paramref name="Source"/> is Download; otherwise null.</param>
/// <param name="StartServer">Whether the server is started detached (it does not answer).</param>
/// <param name="PullModel">Whether the required model is pulled (it is absent).</param>
/// <param name="Model">The required model.</param>
internal sealed record OllamaInstallPlan(
    OllamaExecutableSource Source,
    string? Executable,
    string? DownloadUrl,
    bool StartServer,
    bool PullModel,
    string Model)
{
    /// <summary>Windows portable release: the windows-amd64 zip.</summary>
    internal const string WindowsReleaseUrl = "https://github.com/ollama/ollama/releases/latest/download/ollama-windows-amd64.zip";

    /// <summary>Linux portable release: the linux-amd64 archive, published as .tar.zst (the .tgz name answers 404).</summary>
    internal const string LinuxReleaseUrl = "https://ollama.com/download/ollama-linux-amd64.tar.zst";

    /// <summary>The portable executable under the test-tools folder: ollama/ollama.exe on Windows, ollama/bin/ollama on Linux.</summary>
    /// <param name="isWindows">Whether the host is Windows.</param>
    /// <param name="testToolsDirectory">The test-tools folder.</param>
    /// <returns>The executable path.</returns>
    internal static string PortableExecutable(bool isWindows, string testToolsDirectory)
        => isWindows
            ? Path.Combine(testToolsDirectory, "ollama", "ollama.exe")
            : Path.Combine(testToolsDirectory, "ollama", "bin", "ollama");

    /// <summary>
    /// TR-MCP-NUKE-016 AC-001 and AC-002: nothing when a server answers with the required model; otherwise an installed ollama, else a previous
    /// portable install, else that operating system's portable release; then a start when the server does not answer and a pull when the
    /// model is absent.
    /// </summary>
    /// <param name="facts">What the machine shows.</param>
    /// <returns>The steps to take.</returns>
    internal static OllamaInstallPlan Choose(OllamaInstallFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.ServerAnswers && facts.ModelPresent)
            return new(OllamaExecutableSource.None, null, null, false, false, facts.Model);

        var portable = PortableExecutable(facts.IsWindows, facts.TestToolsDirectory);
        var (source, executable, downloadUrl) = facts.InstalledExecutable is { Length: > 0 } installed
            ? (OllamaExecutableSource.Installed, installed, (string?)null)
            : facts.PortableInstallExists
                ? (OllamaExecutableSource.PortableInstall, portable, (string?)null)
                : (OllamaExecutableSource.Download, portable, facts.IsWindows ? WindowsReleaseUrl : LinuxReleaseUrl);
        return new(source, executable, downloadUrl, !facts.ServerAnswers, !facts.ModelPresent, facts.Model);
    }
}
