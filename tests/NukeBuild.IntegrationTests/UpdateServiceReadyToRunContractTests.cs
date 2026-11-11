namespace NukeBuild.IntegrationTests;

/// <summary>Proves the ReadyToRun service-publish contract independently of the production Nuke source.</summary>
/// <remarks>
/// 2026-11-10, TR-MCP-SERVICEUPDATE-001 ready-to-run-publish and TEST-MCP-SERVICEUPDATE-001
/// ready-to-run-publish: adds a green mock and three negative controls before the production Nuke target changes.
/// </remarks>
public sealed class UpdateServiceReadyToRunContractTests
{
    private const string ReadyToRun = ".SetProperty(\"PublishReadyToRun\", \"true\")";

    /// <summary>Correct Windows and Linux service publishes pass while incomplete or launcher-only settings fail.</summary>
    [Trait("Category", "Integration")]
    [Fact]
    public void ReadyToRunContract_AcceptsCorrectMockAndRejectsIncompleteMocks()
    {
        var windows = $"DotNetPublish service-win {ReadyToRun}";
        var linux = $"DotNetPublish service-linux {ReadyToRun}";
        const string launcher = "DotNetPublish launcher";

        AssertReadyToRunContract(windows, linux, launcher);

        Assert.Throws<InvalidDataException>(() =>
            AssertReadyToRunContract("DotNetPublish service-win", linux, launcher));
        Assert.Throws<InvalidDataException>(() =>
            AssertReadyToRunContract(windows, "DotNetPublish service-linux", launcher));
        Assert.Throws<InvalidDataException>(() =>
            AssertReadyToRunContract("DotNetPublish service-win", "DotNetPublish service-linux", $"{launcher} {ReadyToRun}"));
    }

    /// <summary>Requires ReadyToRun for both service publishes and excludes the launcher publish.</summary>
    /// <param name="windowsPublish">Windows service publish source.</param>
    /// <param name="linuxPublish">Linux service publish source.</param>
    /// <param name="launcherPublish">Launcher publish source.</param>
    /// <exception cref="InvalidDataException">The service publish contract is incomplete or applied to the launcher.</exception>
    internal static void AssertReadyToRunContract(string windowsPublish, string linuxPublish, string launcherPublish)
    {
        if (!windowsPublish.Contains(ReadyToRun, StringComparison.Ordinal))
            throw new InvalidDataException("Windows service publish must enable ReadyToRun.");
        if (!linuxPublish.Contains(ReadyToRun, StringComparison.Ordinal))
            throw new InvalidDataException("Linux service publish must enable ReadyToRun.");
        if (launcherPublish.Contains(ReadyToRun, StringComparison.Ordinal))
            throw new InvalidDataException("Launcher publish is outside the ReadyToRun service requirement.");
    }
}
