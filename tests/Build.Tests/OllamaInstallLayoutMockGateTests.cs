namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-4), BDPv4 mock gate: proves the <see cref="OllamaInstallLayoutContract"/> assertions accept a
/// scripted layout that produces the contract-derived values, and reject deliberately wrong layouts (negative
/// controls), before any production layout code is written.
/// </summary>
public sealed class OllamaInstallLayoutMockGateTests
{
    /// <summary>TR-MCP-NUKE-019 AC-4: every host passes against the scripted layout.</summary>
    /// <param name="host">Host name from <see cref="OllamaInstallLayoutContract"/>.</param>
    [Theory]
    [InlineData("windows")]
    [InlineData("linux")]
    public void ScriptedLayout_PassesHost(string host)
        => OllamaInstallLayoutContract.AssertHost(host, new ScriptedLayout(Fault.None));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-4 negative controls, each rejected on the host it breaks: Windows names on Linux, a Linux
    /// portable executable without bin/, the Windows asset everywhere, PATH ignored, the portable executable tried
    /// first, and a blank PATH entry kept.
    /// </summary>
    /// <param name="host">Host the fault is checked on.</param>
    /// <param name="fault">Injected fault.</param>
    [Theory]
    [InlineData("linux", Fault.WindowsNameOnLinux)]
    [InlineData("linux", Fault.PortableWithoutBin)]
    [InlineData("linux", Fault.WindowsAssetEverywhere)]
    [InlineData("windows", Fault.IgnoresPath)]
    [InlineData("linux", Fault.IgnoresPath)]
    [InlineData("windows", Fault.PortableFirst)]
    [InlineData("linux", Fault.PortableFirst)]
    [InlineData("windows", Fault.KeepsBlankEntry)]
    [InlineData("linux", Fault.KeepsBlankEntry)]
    public void WrongLayout_IsRejected(string host, Fault fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => OllamaInstallLayoutContract.AssertHost(host, new ScriptedLayout(fault)));

    /// <summary>Faults injected into the scripted layout for negative controls.</summary>
    public enum Fault
    {
        /// <summary>No fault: the contract-derived layout.</summary>
        None,

        /// <summary>Uses the Windows executable name on Linux.</summary>
        WindowsNameOnLinux,

        /// <summary>Places the Linux portable executable at the archive root instead of under bin/.</summary>
        PortableWithoutBin,

        /// <summary>Uses the Windows zip on every host.</summary>
        WindowsAssetEverywhere,

        /// <summary>Tries no PATH directory.</summary>
        IgnoresPath,

        /// <summary>Tries the portable executable before PATH.</summary>
        PortableFirst,

        /// <summary>Keeps blank PATH entries and does not trim them.</summary>
        KeepsBlankEntry,
    }

    /// <summary>Scripted layout: the contract-derived values, with an optional injected fault.</summary>
    private sealed class ScriptedLayout(Fault fault) : IOllamaInstallLayout
    {
        /// <inheritdoc />
        public string ReleaseAsset(IntegrationLaneHost host)
            => host == IntegrationLaneHost.Windows || fault == Fault.WindowsAssetEverywhere
                ? "ollama-windows-amd64.zip"
                : "ollama-linux-amd64.tar.zst";

        /// <inheritdoc />
        public string PortableExecutable(IntegrationLaneHost host, string testToolsDirectory)
            => host == IntegrationLaneHost.Windows
                ? Path.Combine(testToolsDirectory, "ollama", "ollama.exe")
                : fault == Fault.PortableWithoutBin
                    ? Path.Combine(testToolsDirectory, "ollama", "ollama")
                    : Path.Combine(testToolsDirectory, "ollama", "bin", Name(host));

        /// <inheritdoc />
        public IReadOnlyList<string> Candidates(
            IntegrationLaneHost host,
            IReadOnlyList<string> pathDirectories,
            string localApplicationData,
            string testToolsDirectory)
        {
            var portable = PortableExecutable(host, testToolsDirectory);
            var fromPath = fault == Fault.IgnoresPath
                ? new List<string>()
                : pathDirectories
                    .Where(directory => fault == Fault.KeepsBlankEntry || !string.IsNullOrWhiteSpace(directory))
                    .Select(directory => Path.Combine(fault == Fault.KeepsBlankEntry ? directory : directory.Trim(), Name(host)))
                    .ToList();
            var candidates = new List<string>();
            if (fault == Fault.PortableFirst)
                candidates.Add(portable);
            candidates.AddRange(fromPath);
            if (host == IntegrationLaneHost.Windows)
                candidates.Add(Path.Combine(localApplicationData, "Programs", "Ollama", "ollama.exe"));
            if (fault != Fault.PortableFirst)
                candidates.Add(portable);
            return candidates;
        }

        private string Name(IntegrationLaneHost host)
            => host == IntegrationLaneHost.Windows || fault == Fault.WindowsNameOnLinux ? "ollama.exe" : "ollama";
    }
}
