namespace NukeBuild.Tests;

/// <summary>
/// TEST-NUKE-005: Verifies MsixHelper generates valid AppxManifest XML
/// and creates correct placeholder PNG bytes.
/// </summary>
public sealed class MsixHelperTests
{
    [Fact]
    public void GenerateManifest_ContainsPackageName()
    {
        var manifest = MsixHelper.GenerateManifest("TestApp", "CN=Test", "1.0.0.0");
        Assert.Contains("Name=\"TestApp\"", manifest);
    }

    [Fact]
    public void GenerateManifest_ContainsPublisher()
    {
        var manifest = MsixHelper.GenerateManifest("TestApp", "CN=Test", "1.0.0.0");
        Assert.Contains("Publisher=\"CN=Test\"", manifest);
    }

    [Fact]
    public void GenerateManifest_ContainsVersion()
    {
        var manifest = MsixHelper.GenerateManifest("TestApp", "CN=Test", "2.0.1.0");
        Assert.Contains("Version=\"2.0.1.0\"", manifest);
    }

    [Fact]
    public void GenerateManifest_IsValidXml()
    {
        var manifest = MsixHelper.GenerateManifest("TestApp", "CN=Test", "1.0.0.0");
        var doc = System.Xml.Linq.XDocument.Parse(manifest);
        Assert.NotNull(doc.Root);
    }

    [Fact]
    public void GenerateManifest_ContainsRunFullTrustCapability()
    {
        var manifest = MsixHelper.GenerateManifest("TestApp", "CN=Test", "1.0.0.0");
        Assert.Contains("runFullTrust", manifest);
    }

    [Fact]
    public void GenerateManifest_ContainsExecutable()
    {
        var manifest = MsixHelper.GenerateManifest("TestApp", "CN=Test", "1.0.0.0");
        Assert.Contains("McpServer.Support.Mcp.exe", manifest);
    }

    [Fact]
    public void CreatePlaceholderPng_ReturnsValidPngBytes()
    {
        var bytes = MsixHelper.CreatePlaceholderPng();
        Assert.NotEmpty(bytes);
        // PNG magic bytes: 89 50 4E 47
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x4E, bytes[2]);
        Assert.Equal(0x47, bytes[3]);
    }
}
