/// <summary>
/// TR-MCP-NUKE-019 AC-4: where MigrationIntegrationTests finds or places the Ollama executable on a host. An installed
/// executable is used when one exists, tried in order (one per PATH directory, then the host's usual install, then
/// the portable executable in the user's QBrainAi test-tools folder); otherwise the host's portable release asset is
/// placed in that folder and its executable is used.
/// </summary>
interface IOllamaInstallLayout
{
    /// <summary>Returns the file name of the portable Ollama release asset for <paramref name="host"/>.</summary>
    /// <param name="host">Host the lane runs on.</param>
    string ReleaseAsset(IntegrationLaneHost host);

    /// <summary>Returns the portable executable inside <paramref name="testToolsDirectory"/> once the release is placed.</summary>
    /// <param name="host">Host the lane runs on.</param>
    /// <param name="testToolsDirectory">The user's QBrainAi test-tools folder.</param>
    string PortableExecutable(IntegrationLaneHost host, string testToolsDirectory);

    /// <summary>Returns the executables to try, in order; blank PATH entries are skipped and entries are trimmed.</summary>
    /// <param name="host">Host the lane runs on.</param>
    /// <param name="pathDirectories">The PATH directories, in PATH order.</param>
    /// <param name="localApplicationData">The user's local application data folder.</param>
    /// <param name="testToolsDirectory">The user's QBrainAi test-tools folder.</param>
    IReadOnlyList<string> Candidates(
        IntegrationLaneHost host,
        IReadOnlyList<string> pathDirectories,
        string localApplicationData,
        string testToolsDirectory);
}
