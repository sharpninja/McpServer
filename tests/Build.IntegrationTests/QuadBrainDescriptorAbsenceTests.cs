namespace NukeBuild.IntegrationTests;

/// <summary>
/// TEST-MCP-QBSURFACE-004 (historical): Guards the shared MCP descriptor registry under
/// <c>mcps/</c> against the return of agent-facing QuadBrain brain-slot tool
/// descriptors. QuadBrain is reachable only through the OpenAI-compatible
/// endpoint at <c>POST /v1/chat/completions</c>, so no <c>brain_slot_*.json</c>
/// descriptor may be published to agent plugins from the registry.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-QBSURFACE-004 (historical); FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole and unchanged from Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"); only the namespace changed.</remarks>
public sealed class QuadBrainDescriptorAbsenceTests
{
    /// <summary>
    /// TEST-MCP-QBSURFACE-004 (historical): Verifies that no file matching
    /// <c>brain_slot_*.json</c> exists anywhere beneath the repository
    /// <c>mcps/</c> descriptor registry. Test data is the live repository tree
    /// located from the test assembly base directory. Fails with the offending
    /// repository-relative paths so a reintroduced descriptor is named exactly.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void McpsRegistry_ContainsNoBrainSlotToolDescriptors()
    {
        var root = FindRepositoryRoot();
        var mcpsRoot = Path.Combine(root, "mcps");

        if (!Directory.Exists(mcpsRoot))
        {
            return;
        }

        var offenders = Directory
            .GetFiles(mcpsRoot, "brain_slot_*.json", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "QuadBrain brain-slot descriptors must not exist under mcps/. Found: "
                + string.Join(", ", offenders));
    }

    /// <summary>
    /// Locates the repository root by walking up from the test assembly base
    /// directory until the solution file is found.
    /// </summary>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "QBrainAi.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing QBrainAi.sln.");
    }
}
