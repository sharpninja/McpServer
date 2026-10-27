namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-4): Binds the <see cref="OllamaInstallLayoutContract"/> cases and assertions, unchanged from the
/// BDPv4 mock gate, to the production <see cref="OllamaInstallLayout"/>. Pure path composition from relative literals;
/// no host folder, process or network is touched.
/// </summary>
public sealed class OllamaInstallLayoutTests
{
    private readonly OllamaInstallLayout layout = new();

    /// <summary>TR-MCP-NUKE-019 AC-4: the production layout meets the contract on every host.</summary>
    /// <param name="host">Host name from <see cref="OllamaInstallLayoutContract"/>.</param>
    [Theory]
    [InlineData("windows")]
    [InlineData("linux")]
    public void Layout_MeetsContract(string host)
        => OllamaInstallLayoutContract.AssertHost(host, layout);
}
