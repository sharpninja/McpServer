/// <summary>
/// TR-MCP-NUKE-019 AC-4: Production <see cref="IOllamaInstallLayout"/>. The portable release assets are those the
/// ollama/ollama releases ship (ollama-windows-amd64.zip with the executable at its root, ollama-linux-amd64.tar.zst
/// with the executable under bin/); an installed executable is looked for on PATH first, then in the per-user Windows
/// install, then at the portable location.
/// </summary>
sealed class OllamaInstallLayout : IOllamaInstallLayout
{
    /// <inheritdoc />
    public string ReleaseAsset(IntegrationLaneHost host) => host switch
    {
        IntegrationLaneHost.Windows => "ollama-windows-amd64.zip",
        IntegrationLaneHost.Linux => "ollama-linux-amd64.tar.zst",
        _ => throw new ArgumentOutOfRangeException(nameof(host), host, "Unknown integration lane host."),
    };

    /// <inheritdoc />
    public string PortableExecutable(IntegrationLaneHost host, string testToolsDirectory) => host switch
    {
        IntegrationLaneHost.Windows => Path.Combine(testToolsDirectory, "ollama", "ollama.exe"),
        IntegrationLaneHost.Linux => Path.Combine(testToolsDirectory, "ollama", "bin", "ollama"),
        _ => throw new ArgumentOutOfRangeException(nameof(host), host, "Unknown integration lane host."),
    };

    /// <inheritdoc />
    public IReadOnlyList<string> Candidates(
        IntegrationLaneHost host,
        IReadOnlyList<string> pathDirectories,
        string localApplicationData,
        string testToolsDirectory)
    {
        ArgumentNullException.ThrowIfNull(pathDirectories);
        var executableName = host == IntegrationLaneHost.Windows ? "ollama.exe" : "ollama";
        var candidates = pathDirectories
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(directory => Path.Combine(directory.Trim(), executableName))
            .ToList();
        if (host == IntegrationLaneHost.Windows)
            candidates.Add(Path.Combine(localApplicationData, "Programs", "Ollama", "ollama.exe"));
        candidates.Add(PortableExecutable(host, testToolsDirectory));
        return candidates;
    }
}
