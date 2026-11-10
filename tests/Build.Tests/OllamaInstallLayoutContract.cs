namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019: Contract cases and assertions for <see cref="IOllamaInstallLayout"/>. The explicit fixtures
/// pin the retained build design in plan section 22, not a removed environment-specific criterion: use an installed
/// executable or the portable amd64 layout. The pinned asset names are ollama-windows-amd64.zip (root executable)
/// and ollama-linux-amd64.tar.zst (bin/ executable); this is not a claim about the latest release version.
/// The same assertions run against the scripted layout
/// (BDPv4 mock gate), wrong layouts (negative controls), and the production layout. Paths are built with
/// <see cref="Path.Combine(string[])"/> from relative literals on both sides, so the contract reads no host folder and
/// holds on every host.
/// </summary>
internal static class OllamaInstallLayoutContract
{
    /// <summary>The scripted test-tools folder.</summary>
    internal const string TestTools = "tools-root";

    /// <summary>The scripted local application data folder.</summary>
    internal const string LocalApplicationData = "local-app-data";

    /// <summary>The scripted PATH directories: a plain entry, a blank entry, and an entry with trailing blanks.</summary>
    internal static readonly string[] PathDirectories = ["path-a", "  ", "path-b "];

    /// <summary>The release asset the release requires for <paramref name="host"/>.</summary>
    /// <param name="host">Host the lane runs on.</param>
    internal static string ExpectedAsset(IntegrationLaneHost host) => host switch
    {
        IntegrationLaneHost.Windows => "ollama-windows-amd64.zip",
        IntegrationLaneHost.Linux => "ollama-linux-amd64.tar.zst",
        _ => throw new ArgumentOutOfRangeException(nameof(host), host, "Unknown host."),
    };

    /// <summary>The portable executable for <paramref name="host"/> inside <see cref="TestTools"/>.</summary>
    /// <param name="host">Host the lane runs on.</param>
    internal static string ExpectedPortable(IntegrationLaneHost host) => host switch
    {
        IntegrationLaneHost.Windows => Path.Combine(TestTools, "ollama", "ollama.exe"),
        IntegrationLaneHost.Linux => Path.Combine(TestTools, "ollama", "bin", "ollama"),
        _ => throw new ArgumentOutOfRangeException(nameof(host), host, "Unknown host."),
    };

    /// <summary>
    /// The candidates for <paramref name="host"/>, in order: one per non-blank PATH directory (trimmed), then on Windows
    /// the usual per-user install, then the portable executable.
    /// </summary>
    /// <param name="host">Host the lane runs on.</param>
    internal static string[] ExpectedCandidates(IntegrationLaneHost host) => host switch
    {
        IntegrationLaneHost.Windows =>
        [
            Path.Combine("path-a", "ollama.exe"),
            Path.Combine("path-b", "ollama.exe"),
            Path.Combine(LocalApplicationData, "Programs", "Ollama", "ollama.exe"),
            ExpectedPortable(IntegrationLaneHost.Windows),
        ],
        IntegrationLaneHost.Linux =>
        [
            Path.Combine("path-a", "ollama"),
            Path.Combine("path-b", "ollama"),
            ExpectedPortable(IntegrationLaneHost.Linux),
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(host), host, "Unknown host."),
    };

    /// <summary>Asserts that <paramref name="layout"/> meets the contract for the named host.</summary>
    /// <param name="hostName">windows or linux.</param>
    /// <param name="layout">Layout under test.</param>
    internal static void AssertHost(string hostName, IOllamaInstallLayout layout)
    {
        var host = IntegrationLaneFilterContract.Parse(hostName);

        Assert.Equal(ExpectedAsset(host), layout.ReleaseAsset(host));
        Assert.Equal(ExpectedPortable(host), layout.PortableExecutable(host, TestTools));
        Assert.Equal(ExpectedCandidates(host), layout.Candidates(host, PathDirectories, LocalApplicationData, TestTools));
    }
}
